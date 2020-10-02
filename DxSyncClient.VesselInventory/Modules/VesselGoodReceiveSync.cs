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
        public void SyncOut()
        {
            SyncOut<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        protected override object GetReferenceData(DxSyncOutRecordStage recordStage)
        {
            object data = null;
            if (recordStage.EntityName == typeof(VesselGoodReceive).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceive(recordStage.ReferenceId, recordStage.Version);
            else if (recordStage.EntityName == typeof(VesselGoodReceiveItemReject).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceiveItemReject(recordStage.ReferenceId, recordStage.Version);
            return data;
        }

    }
}
