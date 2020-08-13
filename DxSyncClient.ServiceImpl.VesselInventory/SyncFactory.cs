using DxSyncClient.ServiceImpl.VesselInventory.Module.RequestForm;
using DxSyncClient.ServiceImpl.VesselInventory.Module.VesselGoodIssued;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class SyncFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
        public static VesselGoodIssuedSync VesselGoodIssuedSync => new VesselGoodIssuedSync();
    }
}
