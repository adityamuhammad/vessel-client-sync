using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class RepositoryFactory
    {
        public static SyncRecordStageRepository SyncRecordStageRepository => new SyncRecordStageRepository();
        public static RequestFormRepository RequestFormRepository => new RequestFormRepository();
        public static VesselGoodReceiveRepository VesselGoodReceiveRepository => new VesselGoodReceiveRepository();
        public static VesselGoodIssuedRepository VesselGoodIssuedRepository => new VesselGoodIssuedRepository();
    }
}
