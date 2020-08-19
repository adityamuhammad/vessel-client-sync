using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class RepositoryFactory
    {
        public static RequestFormRepository RequestFormRepository => new RequestFormRepository();
        public static VesselGoodReceiveRepository VesselGoodReceiveRepository => new VesselGoodReceiveRepository();
    }
}
