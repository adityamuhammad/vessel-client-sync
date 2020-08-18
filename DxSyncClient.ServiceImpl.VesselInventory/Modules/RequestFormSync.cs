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

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class RequestFormSync
    {
        private readonly RequestFormRepository _requestFormRepository;
        private readonly ILogger _logger;
        private string _token;
        public RequestFormSync()
        {
            _requestFormRepository = new RequestFormRepository();
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
        }

        public void InitializeData()
        {
             _requestFormRepository.GenerateData();
        }
        public void SyncOut(string token)
        {
            SetToken(token);
            var dataSync = _requestFormRepository.GetSyncRecordStagesRequestForm();
            RequestAPISyncOut(dataSync);
        }
        public void SyncOutConfirmation(string token)
        {
            SetToken(token);
        }

        public void RequestAPISyncOut(IEnumerable<DxSyncRecordStage> list,string parentId = "0", int deep = -1)
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();
            if (collections.Count <= 0) return;
            deep++;
            foreach (var row in collections)
            {
                if (!row.IsFile)
                {

                    object data = null;
                    if (row.EntityName == EnvClass.EntityName.RequestForm)
                        data = _requestFormRepository.GetRequestFormData(row.ReferenceId);
                    else if (row.EntityName == EnvClass.EntityName.RequestFormItem)
                        data = _requestFormRepository.GetRequestFormItemData(row.ReferenceId);

                    SendData(row, data);
                } else
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
                            int chunkSize = EnvClass.HelperValue.ChunkSize;

                            while (remainSize > 0)
                            {
                                int countSize = Math.Min(remainSize, chunkSize);
                                using(MemoryStream ms = new MemoryStream())
                                {
                                    ms.Write(file, currentSize, countSize);
                                    byte[] fileChunk = ms.ToArray();
                                    string binaryFileString = Convert.ToBase64String(fileChunk);

                                    DxSyncFile syncFile = new DxSyncFile();
                                    syncFile.FilePart = binaryFileString;
                                    syncFile.FileFormat = extensions;
                                    syncFile.IsNewFile = (currentSize == 0) ? true : false;

                                    int fileSizeServer = SendFileData(row, syncFile);

                                    if(fileSizeServer == -1) break;

                                    currentSize = fileSizeServer;
                                    remainSize = fileSizeClient - fileSizeServer;
                                }
                            }
                        }
                        
                    }
                }
                Console.WriteLine(new String('\t', deep) + row.EntityName + row.ReferenceId + row.Filename);
                RequestAPISyncOut(list, row.RecordStageId, deep);
            }

        }
        private void SendData(DxSyncRecordStage syncRecordStage, object data)
        {

            string endpoint = APISyncEndpoint.SyncOut;
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(endpoint);
                SetQueryParamsAndHeader(httpExtensions, syncRecordStage);
                httpExtensions.Body(data);
                return await httpExtensions.PostRaw();
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
                var httpExtensions = new HttpExtensions(endpoint);
                SetQueryParamsAndHeader(httpExtensions, syncRecordStage);
                return await httpExtensions.PostRaw();
            });
            var result = responseData.GetAwaiter().GetResult();

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
                var httpExtensions = new HttpExtensions(endpoint);
                SetQueryParamsAndHeader(httpExtensions, syncRecordStage);
                httpExtensions.Body(data);
                return await httpExtensions.PostRaw();
            });
            var result = responseData.GetAwaiter().GetResult();
            if (result == null) return -1;

            if (result.StatusCode == HttpResponseCode.OK)
            {
                int currentSize = (int)((JObject)result.Data).SelectToken("CurrentFileSize");
                return currentSize;
            }
            return -1;
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

        private void SetQueryParamsAndHeader(HttpExtensions httpExtensions, DxSyncRecordStage syncRecordStage)
        {
            httpExtensions.AddHeader("X-Token", _token);
            httpExtensions.AddQueryParam("DomainName", EnvClass.Client.ApplicationName);
            httpExtensions.AddQueryParam("ClientId", EnvClass.Client.ClientId.ToString());
            httpExtensions.AddQueryParam("EntityName", syncRecordStage.EntityName);
            httpExtensions.AddQueryParam("ReferenceId", syncRecordStage.ReferenceId);
            httpExtensions.AddQueryParam("RecordStageId", syncRecordStage.RecordStageId);
            httpExtensions.AddQueryParam("RecordStageParentId", syncRecordStage.RecordStageParentId);
        }
        private void SetToken(string token)
        {
            _token = token;
        }
    }
}
