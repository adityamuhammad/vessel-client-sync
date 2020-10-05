using DxSyncClient.VesselInventory.Modules;

namespace DxSyncClient.VesselInventory
{
    public class ModuleFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
        public static VesselGoodReceiveSync VesselGoodReceiveSync => new VesselGoodReceiveSync();
        public static VesselGoodIssuedSync VesselGoodIssuedSync => new VesselGoodIssuedSync();
        public static VesselGoodReturnSync VesselGoodReturnSync => new VesselGoodReturnSync();
        public static VesselGoodJournalSync VesselGoodJournalSync => new VesselGoodJournalSync();
        public static ItemDimensionSync ItemDimensionSync => new ItemDimensionSync();
        public static ItemSync ItemSync => new ItemSync();
    }
}
