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

        public void TransferFromStagingToMain()
        {
            _requestFormRepository.TransferFromStagingToMain();
        }

        public void SyncOut()
        {
            SyncOut<RequestForm,RequestFormItem>();
        }
        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<RequestForm,RequestFormItem>();
        }

        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(RequestForm).Name)
                data = _requestFormRepository.GetRequestFormOut(syncRecordStage.ReferenceId, syncRecordStage.Version);
            else if (syncRecordStage.EntityName == typeof(RequestFormItem).Name)
                data = _requestFormRepository.GetRequestFormItemOut(syncRecordStage.ReferenceId, syncRecordStage.Version);
            return data;
        }

        public  void SyncIn()
        {
            SyncIn<RequestFormItem>();
        }

        public void SyncInConfirmation()
        {
            SyncInConfirmation<RequestFormItem>();
        }

        public void SyncInComplete()
        {
            SyncInComplete<RequestFormItem>();
        }

        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            _requestFormRepository.CreateItemSyncIn(syncInRecordStage, (RequestFormItem)referenceData);
        }
    }
}
