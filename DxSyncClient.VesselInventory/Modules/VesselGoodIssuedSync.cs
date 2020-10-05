using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodIssuedSync : AbstractBaseSynchronization
    {
        private readonly VesselGoodIssuedRepository _vesselGoodIssuedRepository;
        public VesselGoodIssuedSync() : base(new SyncRecordStageRepository())
        {
            _vesselGoodIssuedRepository = RepositoryFactory.VesselGoodIssuedRepository;
        }
        public void TransferFromMainToStaging()
        {
             _vesselGoodIssuedRepository.TransferFromMainToStaging();
        }
        public void SyncOut()
        {
            SyncOut<VesselGoodIssued, VesselGoodIssuedItem>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodIssued, VesselGoodIssuedItem>();
        }
        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(VesselGoodIssued).Name)
                data = _vesselGoodIssuedRepository.GetVesselGoodIssued(syncRecordStage.ReferenceId, syncRecordStage.Version);
            else if (syncRecordStage.EntityName == typeof(VesselGoodIssuedItem).Name)
                data = _vesselGoodIssuedRepository.GetVesselGoodIssuedItem(syncRecordStage.ReferenceId, syncRecordStage.Version);
            return data;
        }

        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            throw new System.NotImplementedException();
        }
    }
}
