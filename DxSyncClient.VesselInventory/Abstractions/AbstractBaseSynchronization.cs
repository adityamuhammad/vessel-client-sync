using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using DxSync.Common;
using DxSync.FxLib;
using DxSync.Log;
using DxSyncClient.Contract.Interfaces;
using DxSyncClient.RequestAPIModule;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DxSyncClient.VesselInventory.Abstractions
{
    public abstract class AbstractBaseSynchronization
    {
        private readonly ILogger _logger;
        private readonly ISyncRecordStageRepository _syncRecordStageRepository;

        public AbstractBaseSynchronization(ISyncRecordStageRepository syncRecordStageRepository)
        {
            _logger = LoggerFactory.GetLogger(LoggerFactory.LoggerType.WindowsEventViewer);
            _syncRecordStageRepository = syncRecordStageRepository;
        }

        protected abstract object GetReferenceData(DxSyncOutRecordStage syncRecordStage);

        protected void SetCompleteSyncOut(string recordStageId)
        {
            _syncRecordStageRepository.UpdateStagingSyncOut(recordStageId, DxSyncStatusStage.SYNC_COMPLETE);
        }

        protected void SetUnSyncSyncOut(string recordStageId)
        {
            _syncRecordStageRepository.UpdateStagingSyncOut(recordStageId, DxSyncStatusStage.UN_SYNC);
        }

        protected void SetProcessedSyncOut(string recordStageId)
        {
            _syncRecordStageRepository.UpdateStagingSyncOut(recordStageId, DxSyncStatusStage.SYNC_PROCESSED);
        }

        protected void SyncOut<THeader,TDetail>() 
            where THeader : class 
            where TDetail : class
        {
            var data = _syncRecordStageRepository.GetStagingSyncOut <THeader,TDetail>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }

        protected void SyncOutConfirmation<THeader, TDetail>() 
            where THeader : class 
            where TDetail : class
        {
            var data = _syncRecordStageRepository.GetStagingSyncOut<THeader, TDetail>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        protected void SyncOut<TData>()
        {
            var data = _syncRecordStageRepository.GetStagingSyncOut<TData>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }

        protected void SyncOutConfirmation<TData>()
        {
            var data = _syncRecordStageRepository.GetStagingSyncOut<TData>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        /// <summary>
        /// Recursively synchronize data from parent to detail
        /// </summary>
        /// <param name="list"></param>
        protected void ProcessSyncOut(IEnumerable<DxSyncOutRecordStage> list)
        {

            foreach (var row in list)
            {
                if (row.IsFile)
                {
                    FileSyncOut(row);
                } else
                {
                    DataSyncOut(row);
                }
                SetProcessedSyncOut(row.RecordStageId);
            }
        }

        /// <summary>
        /// Recursively confirm the synchronize, from parent to detail
        /// </summary>
        /// <param name="list"></param>
        protected void ProcessSyncOutConfirmation(IEnumerable<DxSyncOutRecordStage> list)
        {

            foreach (var row in list)
            {
                if (row.IsFile)
                {
                    FileSyncOutConfirmation(row);
                } else
                {
                    DataSyncOutConfirmation(row);
                }
            }
        }

        /// <summary>
        /// Synchronize data, get record from table in database and send the record to server
        /// </summary>
        /// <param name="row"></param>
        protected void DataSyncOut(DxSyncOutRecordStage row)
        {
            object data = GetReferenceData(row);
            SendData(row, data);
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="syncRecordStage"></param>
        /// <param name="data"></param>
        protected void SendData(DxSyncOutRecordStage syncRecordStage, object data)
        {
            string endpoint = APISyncEndpoint.SyncOut;

            var result = PostData(endpoint, syncRecordStage, data);

            WriteLog(endpoint, result, data);
        }

        /// <summary>
        /// Confirm data
        /// </summary>
        /// <param name="syncRecordStage"></param>
        protected void DataSyncOutConfirmation(DxSyncOutRecordStage syncRecordStage)
        {
            string endpoint = APISyncEndpoint.SyncOutConfirmation;
            ResponseData result = PostData(endpoint, syncRecordStage);

            WriteLog(endpoint, result, syncRecordStage);

            if (result == null) return;

            if (result.StatusCode == HttpResponseCode.OK)
            {
                SetCompleteSyncOut(syncRecordStage.RecordStageId);
            }
            else if (result.StatusCode == HttpResponseCode.NOT_FOUND)
            {
                SetUnSyncSyncOut(syncRecordStage.RecordStageId);
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
        /// <param name="syncRecordStage"></param>
        protected void FileSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            var fileUpload = SetupEnvironment.Client.UploadPath + syncRecordStage.Filename;
            if (File.Exists(fileUpload))
            {
                int currentSize = CheckFileData(syncRecordStage);

                if (currentSize != -1)
                {
                    var extensions = Path.GetExtension(fileUpload);

                    byte[] file = File.ReadAllBytes(fileUpload);
                    int fileSizeClient = file.Length;
                    int remainSize = fileSizeClient - currentSize;

                    const int chunkSize = SetupEnvironment.HelperValue.ChunkSize;

                    while (remainSize > 0)
                    {
                        int endSize = Math.Min(remainSize, chunkSize);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(file, currentSize, endSize);
                            byte[] fileChunk = ms.ToArray();
                            string binaryFileString = Convert.ToBase64String(fileChunk);

                            DxSyncFile syncFile = new DxSyncFile
                            {
                                FilePart = binaryFileString,
                                FileFormat = extensions,
                                IsNewFile = (currentSize == 0) ? true : false
                            };

                            int fileSizeServer = SendFile(syncRecordStage, syncFile);

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
        protected int CheckFileData(DxSyncOutRecordStage syncRecordStage)
        {
            string endpoint = APISyncEndpoint.SyncOutFileCheck;

            var result = PostData(endpoint, syncRecordStage);

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
        protected int SendFile(DxSyncOutRecordStage syncRecordStage, object data)
        {
            string endpoint = APISyncEndpoint.SyncOutFile;
            var result = PostData(endpoint, syncRecordStage, data);

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
        protected void FileSyncOutConfirmation(DxSyncOutRecordStage syncRecordStage)
        {
            var fileUpload = SetupEnvironment.Client.UploadPath + syncRecordStage.Filename;
            if (File.Exists(fileUpload))
            {
                string endpoint = APISyncEndpoint.SyncOutFileConfirmation;

                byte[] file = File.ReadAllBytes(fileUpload);

                int totalFileSize = file.Length;

                DxSyncFile syncFile = new DxSyncFile() { TotalFileSize = totalFileSize };

                var result = PostData(endpoint, syncRecordStage, syncFile);

                WriteLog(endpoint, result, syncRecordStage);

                if (result == null) return;
                if (result.StatusCode == HttpResponseCode.OK)
                {
                    int remainFileSize = (int)((JObject)result.Data).SelectToken("RemainFileSize");

                    if(remainFileSize == 0) SetCompleteSyncOut(syncRecordStage.RecordStageId);
                    else SetUnSyncSyncOut(syncRecordStage.RecordStageId);
                } else if(result.StatusCode == HttpResponseCode.NOT_FOUND)
                {
                    SetUnSyncSyncOut(syncRecordStage.RecordStageId);
                }
            }
        }

        /// <summary>
        /// Post data to server endpoint is the url, syncrecordstage is query parameter, data is body
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="syncRecordStage"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private ResponseData PostData(string endpoint, DxSyncOutRecordStage syncRecordStage, object data = null)
        {
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var requestAPI = new RequestAPI(endpoint);
                requestAPI.Body(data);
                SetQueryParamsAndHeader(requestAPI, syncRecordStage);
                return await requestAPI.PostAsync();
            });
            var result = responseData.GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// Set Common Query Parameter
        /// </summary>
        /// <param name="requestAPI"></param>
        /// <param name="syncRecordStage"></param>
        protected void SetQueryParamsAndHeader(RequestAPI requestAPI, DxSyncOutRecordStage syncRecordStage)
        {
            requestAPI.AddHeader("X-Token", SetupEnvironment.Client.Token);
            requestAPI.AddQueryParam("DomainName", SetupEnvironment.Client.ApplicationName);
            requestAPI.AddQueryParam("ClientId", SetupEnvironment.Client.ClientId.ToString());
            requestAPI.AddQueryParam("EntityName", syncRecordStage.EntityName);
            requestAPI.AddQueryParam("ReferenceId", syncRecordStage.ReferenceId);
            requestAPI.AddQueryParam("RecordStageId", syncRecordStage.RecordStageId);
            requestAPI.AddQueryParam("RecordStageParentId", syncRecordStage.RecordStageParentId);
            requestAPI.AddQueryParam("Version", syncRecordStage.Version.ToString());
            requestAPI.AddQueryParam("DataCount", syncRecordStage.DataCount.ToString());
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
