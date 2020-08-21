using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using DxSync.Common;
using DxSync.FxLib;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using Newtonsoft.Json;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public abstract class AbstractSync
    {
        private readonly ILogger _logger;
        protected string Token;
        public AbstractSync()
        {
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");

        }
        protected abstract void SetSyncComplete(string recordStageId);
        protected abstract void SetUnSync(string recordStageId);
        protected abstract void SetSyncProcessed(string recordStageId);
        protected abstract object GetData(DxSyncRecordStage syncRecordStage);

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
        protected void ProcessSyncOut(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                object data = GetData(row);
                SendData(row, data);
                SetSyncProcessed(row.RecordStageId);
                ProcessSyncOut(list, row.RecordStageId);
            }

        }
        protected void ProcessSyncOutConfirmation(IEnumerable<DxSyncRecordStage> list,string parentId = "0")
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();

            if (collections.Count <= 0) return;

            foreach (var row in collections)
            {
                ConfirmData(row);
                ProcessSyncOutConfirmation(list, row.RecordStageId);
            }
        }
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
