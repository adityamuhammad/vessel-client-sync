
using DxSyncClient.ServiceImpl.VesselInventory.Modules;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class ModuleFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
        public static VesselGoodReceiveSync VesselGoodReceiveSync => new VesselGoodReceiveSync();
        public static VesselGoodIssuedSync VesselGoodIssuedSync => new VesselGoodIssuedSync();
    }
}
