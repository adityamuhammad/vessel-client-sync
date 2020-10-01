using DxSync.FxLib;
using DxSync.Entity.VesselInventory;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodReceiveSync : AbstractModuleClientSync
    {
        private readonly VesselGoodReceiveRepository _vesselGoodReceiveRepository;
        public VesselGoodReceiveSync()
        {
            _vesselGoodReceiveRepository = RepositoryFactory.VesselGoodReceiveRepository;
        }
        public void InitializeData()
        {
             _vesselGoodReceiveRepository.InitializeData();
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
