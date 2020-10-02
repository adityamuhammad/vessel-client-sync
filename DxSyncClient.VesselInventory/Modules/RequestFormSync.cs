using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class RequestFormSync : AbstractBaseSynchronization
    {
        private readonly RequestFormRepository _requestFormRepository;
        public RequestFormSync() : base(new SyncRecordStageRepository())
        {
            _requestFormRepository = RepositoryFactory.RequestFormRepository;
        }

        public void TransferFromMainToStaging()
        {
             _requestFormRepository.TransferFromMainToStaging();
        }

        public void SyncOut()
        {
            var data = _requestFormRepository.GetStagingSyncOut
                <RequestForm,RequestFormItem>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }
        public void SyncOutConfirmation()
        {
            var data = _requestFormRepository.GetStagingSyncOut
                <RequestForm,RequestFormItem>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        protected override object GetReferenceData(DxSyncOutRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(RequestForm).Name)
                data = _requestFormRepository.GetRequestForm(syncRecordStage.ReferenceId, syncRecordStage.Version);
            else if (syncRecordStage.EntityName == typeof(RequestFormItem).Name)
                data = _requestFormRepository.GetRequestFormItem(syncRecordStage.ReferenceId, syncRecordStage.Version);
            return data;
        }

    }
}
