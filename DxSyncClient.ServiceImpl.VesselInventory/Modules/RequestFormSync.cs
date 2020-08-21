using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class RequestFormSync : AbstractModuleClientSync
    {
        private readonly RequestFormRepository _requestFormRepository;
        public RequestFormSync()
        {
            _requestFormRepository = RepositoryFactory.RequestFormRepository;
        }

        public void InitializeData()
        {
             _requestFormRepository.InitializeData();
        }
        public void SetToken(string token)
        {
            Token = token;
        }

        public void SyncOut()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }
        public void SyncOutConfirmation()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        protected override object GetData(DxSyncRecordStage row)
        {
            object data = null;
            if (row.EntityName == typeof(RequestForm).Name)
                data = _requestFormRepository.GetRequestForm(row.ReferenceId);
            else if (row.EntityName == typeof(RequestFormItem).Name)
                data = _requestFormRepository.GetRequestFormItem(row.ReferenceId);
            return data;
        }

        protected override void SetSyncProcessed(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_PROCESSED);
        }

        protected override void SetSyncComplete(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_COMPLETE);
        }

        protected override void SetUnSync(string recordStageId)
        {
            _requestFormRepository.UpdateSync(recordStageId, DxSyncStatusStage.UN_SYNC);
        }

    }
}
