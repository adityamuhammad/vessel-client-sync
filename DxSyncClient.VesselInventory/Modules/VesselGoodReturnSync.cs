using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodReturnSync : AbstractBaseSynchronization
    {
        private readonly VesselGoodReturnRepository _vesselGoodReturnRepository;
        public VesselGoodReturnSync() : base(new SyncRecordStageRepository())
        {
            _vesselGoodReturnRepository = RepositoryFactory.VesselGoodReturnRepository;
        }

        public void TransferFromMainToStaging()
        {
            _vesselGoodReturnRepository.TransferFromMainToStaging();
        }

        public void SyncOut()
        {
            SyncOut<VesselGoodReturn, VesselGoodReturnItem>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodReturn, VesselGoodReturnItem>();
        }

        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(VesselGoodReturn).Name)
                data = _vesselGoodReturnRepository.GetVesselGoodReturn(syncRecordStage.ReferenceId, syncRecordStage.Version);
            else if (syncRecordStage.EntityName == typeof(VesselGoodReturnItem).Name)
                data = _vesselGoodReturnRepository.GetVesselGoodReturnItem(syncRecordStage.ReferenceId, syncRecordStage.Version);
            return data;
        }

        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            throw new System.NotImplementedException();
        }
    }
}
