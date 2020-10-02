using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class VesselGoodJournalSync : AbstractBaseSynchronization
    {
        private readonly VesselGoodJournalRepository _vesselgoodJournalRepository;
        public VesselGoodJournalSync() : base(new SyncRecordStageRepository())
        {
            _vesselgoodJournalRepository = RepositoryFactory.VesselGoodJournalRepository;
        }

        public void TransferFromMainToStaging()
        {
            _vesselgoodJournalRepository.TransferFromMainToStaging();
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
