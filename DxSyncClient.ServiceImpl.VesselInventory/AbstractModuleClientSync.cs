using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DxSync.Common;
using DxSync.FxLib;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public abstract class AbstractModuleClientSync
    {
        private readonly ILogger _logger;
        private readonly SyncRecordStageRepository _syncRecordStageRepository;
        protected string Token;
        public AbstractModuleClientSync()
        {
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
            _syncRecordStageRepository = RepositoryFactory.SyncRecordStageRepository;
        }
        protected abstract object GetReferenceData(DxSyncRecordStage syncRecordStage);
        protected void SetSyncComplete(string recordStageId)
        {
            _syncRecordStageRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_COMPLETE);
        }
        protected void SetUnSync(string recordStageId)
        {
            _syncRecordStageRepository.UpdateSync(recordStageId, DxSyncStatusStage.UN_SYNC);
        }
        protected void SetSyncProcessed(string recordStageId)
        {
            _syncRecordStageRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_PROCESSED);
        }

        protected void SyncOut<THeader,TDetail>() where THeader : class where TDetail: class
        {
            var data = _syncRecordStageRepository.GetSyncRecordStages <THeader,TDetail>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }

        protected void SyncOutConfirmation<THeader, TDetail>() where THeader : class where TDetail : class
        {
            var data = _syncRecordStageRepository.GetSyncRecordStages<THeader, TDetail>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        /// <summary>
        /// Recursively synchronize data from parent to detail
        /// </summary>
        /// <param name="list"></param>
        /// <param name="parentId"></param>
        protected void ProcessSyncOut(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                if (row.IsFile) SyncFile(row);
                else SyncData(row);

                SetSyncProcessed(row.RecordStageId);
                ProcessSyncOut(list, row.RecordStageId);
            }

        }

        /// <summary>
        /// Recursively confirm the synchronize, from parent to detail
        /// </summary>
        /// <param name="list"></param>
        /// <param name="parentId"></param>
        protected void ProcessSyncOutConfirmation(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                if (row.IsFile) ConfirmFile(row);
                else ConfirmData(row);

                ProcessSyncOutConfirmation(list, row.RecordStageId);
            }
        }

        /// <summary>
        /// Synchronize data, get record from table in database and send the record to server
        /// </summary>
        /// <param name="row"></param>
        protected void SyncData(DxSyncRecordStage row)
        {
            object data = GetReferenceData(row);
            SendData(row, data);
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="syncRecordStage"></param>
        /// <param name="data"></param>
        protected void SendData(DxSyncRecordStage syncRecordStage, object data)
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

        /// <summary>
        /// Confirm data
        /// </summary>
        /// <param name="syncRecordStage"></param>
        protected void ConfirmData(DxSyncRecordStage syncRecordStage)
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
                SetSyncComplete(syncRecordStage.RecordStageId);
            } else if( result.StatusCode == HttpResponseCode.NOT_FOUND)
            {
                SetUnSync(syncRecordStage.RecordStageId);
            }
        }

        /// <summary>
        /// Sync file
        /// The step is
        /// 1. Get file on local machine base on the record we want to send.
        /// 2. Check the file on local machine is exist.
        /// 3. Checkfile data on the server, -1 means failed to fetch. if fetch is not failed go forward.
        /// 4. GetFile extensions on local machine.
        /// 5. Read the file as bytes.
        /// 6. Get the byte size and assign as total byte size on the local machine.
        /// 7. Get remaining size we upload to server, remainsize is substracted from file size on. 
        ///    local machine and data already uploaded on server.
        /// 8. Send chunk file until end of file, start at current size, 
        ///    then Convert the chunk of binary to base64string.
        /// </summary>
        /// <param name="row"></param>
        private void SyncFile(DxSyncRecordStage row)
        {
            var fileUpload = EnvClass.Client.UploadPath + row.Filename;
            if (File.Exists(fileUpload))
            {
                int currentSize = CheckFileData(row);

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

        /// <summary>
        /// Fetch file size on the server, return -1 if fetch is failed
        /// </summary>
        /// <param name="syncRecordStage"></param>
        /// <returns></returns>
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

        /// <summary>
        /// SendFile data to server API, if failed send return -1 otherwise return filesize
        /// </summary>
        /// <param name="syncRecordStage"></param>
        /// <param name="data"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Confirm the file, if the file is completed sync (file is match) update the staging to sync complete
        /// </summary>
        /// <param name="syncRecordStage"></param>
        protected void ConfirmFile(DxSyncRecordStage syncRecordStage)
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

                    if(remainFileSize == 0) SetSyncComplete(syncRecordStage.RecordStageId);
                    else SetUnSync(syncRecordStage.RecordStageId);
                }
            }
        }

        /// <summary>
        /// Set Common Query Parameter
        /// </summary>
        /// <param name="requestAPI"></param>
        /// <param name="syncRecordStage"></param>
        protected void SetQueryParamsAndHeader(RequestAPI requestAPI, DxSyncRecordStage syncRecordStage)
        {
            requestAPI.AddHeader("X-Token", Token);
            requestAPI.AddQueryParam("DomainName", EnvClass.Client.ApplicationName);
            requestAPI.AddQueryParam("ClientId", EnvClass.Client.ClientId.ToString());
            requestAPI.AddQueryParam("EntityName", syncRecordStage.EntityName);
            requestAPI.AddQueryParam("ReferenceId", syncRecordStage.ReferenceId);
            requestAPI.AddQueryParam("RecordStageId", syncRecordStage.RecordStageId);
            requestAPI.AddQueryParam("RecordStageParentId", syncRecordStage.RecordStageParentId);
        }

        /// <summary>
        /// Write Log To Event Viewer
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="responseData"></param>
        /// <param name="data"></param>
        protected void WriteLog(string endpoint, ResponseData responseData, object data)
        {
            string applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            string body = JsonConvert.SerializeObject(data);
            string response = JsonConvert.SerializeObject(responseData);
            string logMessage = @"" + applicationName +" "
                                + DateTime.Now + "\n " + endpoint + "\n " + body  + "\n" + response ;
            _logger.Write(logMessage);
        }
    }
}
