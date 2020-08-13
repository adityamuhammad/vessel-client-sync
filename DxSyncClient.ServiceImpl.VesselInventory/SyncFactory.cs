
using DxSyncClient.ServiceImpl.VesselInventory.Modules.RequestFormModule;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class SyncFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
    }
}
