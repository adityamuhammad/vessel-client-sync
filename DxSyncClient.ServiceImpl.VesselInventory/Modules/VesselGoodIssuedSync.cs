using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class VesselGoodIssuedSync : AbstractModuleClientSync
    {
        private readonly VesselGoodIssuedRepository _vesselGoodIssuedRepository;
        public VesselGoodIssuedSync()
        {
            _vesselGoodIssuedRepository = RepositoryFactory.VesselGoodIssuedRepository;
        }
        public void InitializeData()
        {
             _vesselGoodIssuedRepository.InitializeData();
        }
        public void SyncOut()
        {
            SyncOut<VesselGoodIssued, VesselGoodIssuedItem>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodIssued, VesselGoodIssuedItem>();
        }
        protected override object GetReferenceData(DxSyncRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(VesselGoodIssued).Name)
                data = _vesselGoodIssuedRepository.GetVesselGoodIssued(syncRecordStage.ReferenceId);
            else if (syncRecordStage.EntityName == typeof(VesselGoodIssuedItem).Name)
                data = _vesselGoodIssuedRepository.GetVesselGoodIssuedItem(syncRecordStage.ReferenceId);
            return data;
        }
    }
}
