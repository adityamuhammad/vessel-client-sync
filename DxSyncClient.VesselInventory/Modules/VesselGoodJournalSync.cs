using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodJournalSync : AbstractModuleClientSync
    {
        private readonly VesselGoodJournalRepository _vesselgoodJournalRepository;
        public VesselGoodJournalSync()
        {
            _vesselgoodJournalRepository = RepositoryFactory.VesselGoodJournalRepository;
        }

        public void InitializeData()
        {
            _vesselgoodJournalRepository.InitializeData();
        }

        public void SyncOut()
        {
            SyncOut<VesselGoodJournal>();
        }

        public void SyncOutConfirmation()
        {
            SyncOutConfirmation<VesselGoodJournal>();
        }

        protected override object GetReferenceData(DxSyncOutRecordStage syncRecordStage)
        {
            return _vesselgoodJournalRepository.GetVesselGoodJournal(syncRecordStage.ReferenceId, syncRecordStage.Version);
        }
    }
}
