
using DxSyncClient.ServiceImpl.VesselInventory.Modules.RequestFormModule;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class ModuleFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
    }
}
