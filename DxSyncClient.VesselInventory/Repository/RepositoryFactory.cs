using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory
{
    public class RepositoryFactory
    {
        public static SyncRecordStageRepository SyncRecordStageRepository => new SyncRecordStageRepository();
        public static ItemRepository ItemRepository => new ItemRepository();
        public static ItemDimensionRepository ItemDimensionRepository => new ItemDimensionRepository();
        public static RequestFormRepository RequestFormRepository => new RequestFormRepository();
        public static VesselGoodReceiveRepository VesselGoodReceiveRepository => new VesselGoodReceiveRepository();
        public static VesselGoodIssuedRepository VesselGoodIssuedRepository => new VesselGoodIssuedRepository();
        public static VesselGoodReturnRepository VesselGoodReturnRepository => new VesselGoodReturnRepository();
        public static VesselGoodJournalRepository VesselGoodJournalRepository => new VesselGoodJournalRepository();

    }
}
