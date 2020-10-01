using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodReturnSync : AbstractModuleClientSync
    {
        private readonly VesselGoodReturnRepository _vesselGoodReturnRepository;
        public VesselGoodReturnSync()
        {
            _vesselGoodReturnRepository = RepositoryFactory.VesselGoodReturnRepository;
        }

        public void InitializeData()
        {
            _vesselGoodReturnRepository.InitializeData();
        }

        public void SyncOut()
        {
            SyncOut<VesselGoodReturn, VesselGoodReturnItem>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodReturn, VesselGoodReturnItem>();
        }

        protected override object GetReferenceData(DxSyncOutRecordStage syncRecordStage)
        {
            object data = null;
            if (syncRecordStage.EntityName == typeof(VesselGoodReturn).Name)
                data = _vesselGoodReturnRepository.GetVesselGoodReturn(syncRecordStage.ReferenceId, syncRecordStage.Version);
            else if (syncRecordStage.EntityName == typeof(VesselGoodReturnItem).Name)
                data = _vesselGoodReturnRepository.GetVesselGoodReturnItem(syncRecordStage.ReferenceId, syncRecordStage.Version);
            return data;
        }
    }
}
