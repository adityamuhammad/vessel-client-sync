using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DxSync.Common;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class RequestFormSync
    {
        private readonly RequestFormRepository _requestFormRepository;
        private readonly ILogger _logger;
        private string _token;
        public RequestFormSync()
        {
            _requestFormRepository = RepositoryFactory.RequestFormRepository;
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
        }

        public void InitializeData()
        {
             _requestFormRepository.InitializeData();
        }
        public void SetToken(string token)
        {
            _token = token;
        }

        public void SyncOut()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.UN_SYNC);
            RequestSyncOut(data);
        }
        public void SyncOutConfirmation()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.SYNC_PROCESSED);
            RequestSyncOutConfirmation(data);
        }

        private void RequestSyncOutConfirmation(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                if (!row.IsFile) ConfirmData(row);
                else ConfirmFile(row);

                RequestSyncOutConfirmation(list, row.RecordStageId);
            }
        }


        private void RequestSyncOut(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                if (!row.IsFile) SyncData(row);
                else SyncFile(row);
                SyncProcessed(row.RecordStageId);
                RequestSyncOut(list, row.RecordStageId);
            }

        }

        private void SyncFile(DxSyncRecordStage row)
        {
            var fileUpload = EnvClass.Client.UploadPath + row.Filename;
            if (File.Exists(fileUpload))
            {
                int currentSize = CheckFileData(row);

                //if fetch is not failed
                if (currentSize != -1)
                {

                    var extensions = Path.GetExtension(fileUpload);
                    byte[] file = File.ReadAllBytes(fileUpload);

                    int fileSizeClient = file.Length;
                    int remainSize = fileSizeClient - currentSize;
                    const int chunkSize = EnvClass.HelperValue.ChunkSize;

                    while (remainSize > 0)
                    {
                        int endSize = Math.Min(remainSize, chunkSize);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(file, currentSize, endSize);
                            byte[] fileChunk = ms.ToArray();
                            string binaryFileString = Convert.ToBase64String(fileChunk);

                            DxSyncFile syncFile = new DxSyncFile();
                            syncFile.FilePart = binaryFileString;
                            syncFile.FileFormat = extensions;
                            syncFile.IsNewFile = (currentSize == 0) ? true : false;

                            int fileSizeServer = SendFileData(row, syncFile);

                            if (fileSizeServer == -1) break;

                            currentSize = fileSizeServer;
                            remainSize = fileSizeClient - fileSizeServer;
                        }
                    }
                }

            }
        }

        private void SyncData(DxSyncRecordStage row)
        {
            object data = null;
            if (row.EntityName == EnvClass.EntityName.RequestForm)
                data = _requestFormRepository.GetRequestForm(row.ReferenceId);
            else if (row.EntityName == EnvClass.EntityName.RequestFormItem)
                data = _requestFormRepository.GetRequestFormItem(row.ReferenceId);

            SendData(row, data);
        }

        private void ConfirmData(DxSyncRecordStage syncRecordStage)
        {
            string endpoint = APISyncEndpoint.SyncOutConfirmation;
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var requestAPI = new RequestAPI(endpoint);
                SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                return await requestAPI.PostAsync();
            });
            var result = responseData.GetAwaiter().GetResult();

            WriteLog(endpoint, result, syncRecordStage);

            if (result == null) return;

            if (result.StatusCode == HttpResponseCode.OK)
            {
                SyncComplete(syncRecordStage.RecordStageId);
            } else if( result.StatusCode == HttpResponseCode.NOT_FOUND)
            {
                UnSync(syncRecordStage.RecordStageId);
            }
        }

        private void ConfirmFile(DxSyncRecordStage syncRecordStage)
        {
            var fileUpload = EnvClass.Client.UploadPath + syncRecordStage.Filename;
            if (File.Exists(fileUpload))
            {
                string endpoint = APISyncEndpoint.SyncOutFileConfirmation;
                byte[] file = File.ReadAllBytes(fileUpload);
                int totalFileSize = file.Length;
                DxSyncFile syncFile = new DxSyncFile();
                syncFile.TotalFileSize = totalFileSize;

                Task<ResponseData> responseData = Task.Run(async () =>
                {
                    var requestAPI = new RequestAPI(endpoint);
                    SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                    requestAPI.Body(syncFile);
                    return await requestAPI.PostAsync();
                });
                var result = responseData.GetAwaiter().GetResult();

                WriteLog(endpoint, result, syncRecordStage);

                if (result == null) return;
                if (result.StatusCode == HttpResponseCode.OK)
                {
                    int remainFileSize = (int)((JObject)result.Data).SelectToken("RemainFileSize");
                    if(remainFileSize == 0)
                    {
                        SyncComplete(syncRecordStage.RecordStageId);
                    } else
                    {
                        UnSync(syncRecordStage.RecordStageId);
                    }
                }
            }
        }

        private void SendData(DxSyncRecordStage syncRecordStage, object data)
        {

            string endpoint = APISyncEndpoint.SyncOut;
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var requestAPI = new RequestAPI(endpoint);
                SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                requestAPI.Body(data);
                return await requestAPI.PostAsync();
            });
            var result = responseData.GetAwaiter().GetResult();
            WriteLog(endpoint, result, data);
        }

        //return -1 if fetch is failed otherwise return currentfilesize on the server 
        private int CheckFileData(DxSyncRecordStage syncRecordStage)
        {
            string endpoint = APISyncEndpoint.SyncOutFileCheck;
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var requestAPI = new RequestAPI(endpoint);
                SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                return await requestAPI.PostAsync();
            });
            var result = responseData.GetAwaiter().GetResult();

            WriteLog(endpoint, result, null);

            if (result == null) return -1;

            if (result.StatusCode == HttpResponseCode.OK)
            {
                int currentSize = (int)((JObject)result.Data).SelectToken("CurrentFileSize");
                return currentSize;
            }

            return -1;
        }

        //return -1 if fetch is failed otherwise return currentfilesize on the server 
        private int SendFileData(DxSyncRecordStage syncRecordStage, object data)
        {
            string endpoint = APISyncEndpoint.SyncOutFile;
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var requestAPI = new RequestAPI(endpoint);
                SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                requestAPI.Body(data);
                return await requestAPI.PostAsync();
            });
            var result = responseData.GetAwaiter().GetResult();

            WriteLog(endpoint, result, null);

            if (result == null) return -1;

            if (result.StatusCode == HttpResponseCode.OK)
            {
                int currentSize = (int)((JObject)result.Data).SelectToken("CurrentFileSize");
                return currentSize;
            }
            return -1;
        }

        private void SyncProcessed(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_PROCESSED);
        }

        private void SyncComplete(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_COMPLETE);
        }

        private void UnSync(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.UN_SYNC);
        }

        private void WriteLog(string endpoint, ResponseData responseData, object data)
        {
            string applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            string body = JsonConvert.SerializeObject(data);
            string response = JsonConvert.SerializeObject(responseData);
            string logMessage = @"" + applicationName +" "
                                + DateTime.Now + "\n " + endpoint + "\n " + body  + "\n" + response ;
            _logger.Write(logMessage);
        }

        private void SetQueryParamsAndHeader(RequestAPI requestAPI, DxSyncRecordStage syncRecordStage)
        {
            requestAPI.AddHeader("X-Token", _token);
            requestAPI.AddQueryParam("DomainName", EnvClass.Client.ApplicationName);
            requestAPI.AddQueryParam("ClientId", EnvClass.Client.ClientId.ToString());
            requestAPI.AddQueryParam("EntityName", syncRecordStage.EntityName);
            requestAPI.AddQueryParam("ReferenceId", syncRecordStage.ReferenceId);
            requestAPI.AddQueryParam("RecordStageId", syncRecordStage.RecordStageId);
            requestAPI.AddQueryParam("RecordStageParentId", syncRecordStage.RecordStageParentId);
        }
    }
}
