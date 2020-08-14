using DxSync.Common;
using DxSync.FxLib;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using DxSyncClient.ServiceImpl.VesselInventory.Modules.RequestFormModule.Repository;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules.RequestFormModule
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

        public void APICall(IEnumerable<DxSyncRecordStage> list,string parentId = "0", int deep = -1)
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();
            if (collections.Count <= 0) return;
            deep++;
            foreach (var row in collections)
            {
                if (!row.IsFile)
                {
                    string endpointSyncOut = APISyncEndpoint.SyncOut;
                    string endpointSyncOutConfirmation = APISyncEndpoint.SyncOutConfirmation;

                    object data = null;
                    if (row.EntityName == EnvClass.EntityName.RequestForm)
                        data = _requestFormRepository.GetRequestFormData(row.ReferenceId);
                    else if (row.EntityName == EnvClass.EntityName.RequestFormItem)
                        data = _requestFormRepository.GetRequestFormItemData(row.ReferenceId);

                    SendData(endpointSyncOut, row, data);
                    ConfirmationData();
                } else
                {
                    string endpointSyncOutFileCheck = APISyncEndpoint.SyncOutFileCheck;
                    string endpointSyncOutFile = APISyncEndpoint.SyncOutFile;
                    string endpointSyncOutFileConfirmation = APISyncEndpoint.SyncOutFileConfirmation;

                    var fileUpload = EnvClass.Client.UploadPath + row.Filename;
                    if (File.Exists(fileUpload))
                    {
                        var extensions = Path.GetExtension(fileUpload);
                        byte[] file = File.ReadAllBytes(fileUpload);
                        int currentSize = 0;
                        int remainSize = file.Length - currentSize;
                        const int chunkSize = 2 * 1024;
                        while (remainSize > 0)
                        {
                            int countSize = Math.Min(remainSize, chunkSize);
                            using(MemoryStream ms = new MemoryStream())
                            {
                                ms.Write(file, currentSize, countSize);
                                byte[] fileChunk = ms.ToArray();
                                string binaryFileString = Convert.ToBase64String(fileChunk);
                                //send binaryfilestring

                            }
                            currentSize += countSize;
                            remainSize -= countSize;
                        }
                    }
                }
                Console.WriteLine(new String('\t', deep) + row.EntityName + row.ReferenceId + row.Filename);
                APICall(list, row.RecordStageId, deep);
            }

        }
        private void ConfirmationData()
        {

        }

        private bool SendData(string endpoint, DxSyncRecordStage syncRecordStage, object data)
        {

            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(endpoint);
                SetQueryParamsAndHeader(httpExtensions, syncRecordStage);
                httpExtensions.Body(data);
                return await httpExtensions.PostRaw();
            });
            var result = responseData.GetAwaiter().GetResult();
            WriteLog(endpoint, result, data);
            if (result is null) return false;

            if (result.StatusCode == HttpResponseCode.OK) return true;

            return false;
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

        public void SyncOut(string token)
        {
            _token = token;
            var dataSync = _requestFormRepository.GetSyncRecordStagesRequestForm();
            APICall(dataSync);
        }
    }
}
