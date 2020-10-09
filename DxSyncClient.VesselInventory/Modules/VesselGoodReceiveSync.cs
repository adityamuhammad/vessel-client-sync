using DxSync.FxLib;
using DxSync.Entity.VesselInventory;
using DxSyncClient.VesselInventory.Repository;
using DxSyncClient.VesselInventory.Abstractions;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodReceiveSync : AbstractBaseSynchronization
    {
        private readonly VesselGoodReceiveRepository _vesselGoodReceiveRepository;
        public VesselGoodReceiveSync() : base(new SyncRecordStageRepository())
        {
            _vesselGoodReceiveRepository = RepositoryFactory.VesselGoodReceiveRepository;
        }
        public void TransferFromMainToStaging()
        {
             _vesselGoodReceiveRepository.TransferFromMainToStaging();
        }

        public void TransferFromStagingToMain()
        {
            _vesselGoodReceiveRepository.TransferFromStagingToMain();
        }
        public void SyncOut()
        {
            SyncOut<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage recordStage)
        {
            object data = null;
            if (recordStage.EntityName == typeof(VesselGoodReceive).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceiveOut(recordStage.ReferenceId, recordStage.Version);
            else if (recordStage.EntityName == typeof(VesselGoodReceiveItemReject).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceiveItemReject(recordStage.ReferenceId, recordStage.Version);
            return data;
        }

        public  void SyncIn()
        {
            SyncIn<VesselGoodReceiveItem>();
        }

        public void SyncInConfirmation()
        {
            SyncInConfirmation<VesselGoodReceiveItem>();
        }

        public void SyncInComplete()
        {
            SyncInComplete<VesselGoodReceiveItem>();
        }

        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            _vesselGoodReceiveRepository.CreateItemSyncIn(syncInRecordStage, (VesselGoodReceiveItem)referenceData);
        }
    }
}
