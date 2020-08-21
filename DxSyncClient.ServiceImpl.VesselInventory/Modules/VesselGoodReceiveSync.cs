using DxSync.FxLib;
using DxSync.Entity.VesselInventory;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
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
        public void SetToken(string token)
        {
            Token = token;
        }
        public void SyncOut()
        {
            SyncOut<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodReceive, VesselGoodReceiveItemReject>();
        }

        protected override object GetReferenceData(DxSyncRecordStage recordStage)
        {
            object data = null;
            if (recordStage.EntityName == typeof(VesselGoodReceive).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceive(recordStage.ReferenceId);
            else if (recordStage.EntityName == typeof(VesselGoodReceiveItemReject).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceiveItemReject(recordStage.ReferenceId);
            return data;
        }

    }
}
