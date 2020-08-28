using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
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

        protected override object GetReferenceData(DxSyncRecordStage syncRecordStage)
        {
            return _vesselgoodJournalRepository.GetVesselGoodJournal(syncRecordStage.ReferenceId);
        }
    }
}
