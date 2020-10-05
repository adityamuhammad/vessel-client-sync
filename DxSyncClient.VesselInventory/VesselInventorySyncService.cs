using DxSyncClient.VesselInventory.Modules;

namespace DxSyncClient.VesselInventory
{

    public class VesselInventorySyncService : SyncPermission
    {
        private readonly RequestFormSync _requestFormSync;
        private readonly VesselGoodReceiveSync _vesselGoodReceiveSync;
        private readonly VesselGoodIssuedSync _vesselGoodIssuedSync;
        private readonly VesselGoodReturnSync _vesselGoodReturnSync;
        private readonly VesselGoodJournalSync _vesselGoodJournalSync;
        private readonly ItemDimensionSync _itemDimensionSync;
        private readonly ItemSync _itemSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = ModuleFactory.RequestFormSync;
            _vesselGoodReceiveSync = ModuleFactory.VesselGoodReceiveSync;
            _vesselGoodIssuedSync = ModuleFactory.VesselGoodIssuedSync;
            _vesselGoodReturnSync = ModuleFactory.VesselGoodReturnSync;
            _vesselGoodJournalSync = ModuleFactory.VesselGoodJournalSync;
            _itemDimensionSync = ModuleFactory.ItemDimensionSync;
            _itemSync = ModuleFactory.ItemSync;
        }

        public void TransferFromMainToStaging()
        {
            _requestFormSync.TransferFromMainToStaging();
            _vesselGoodReceiveSync.TransferFromMainToStaging();
            _vesselGoodIssuedSync.TransferFromMainToStaging();
            _vesselGoodReturnSync.TransferFromMainToStaging();
            _vesselGoodJournalSync.TransferFromMainToStaging();
        }

        public void SyncOut()
        {
            _requestFormSync.SyncOut();
            _vesselGoodReceiveSync.SyncOut();
            _vesselGoodIssuedSync.SyncOut();
            _vesselGoodReturnSync.SyncOut();
            _vesselGoodJournalSync.SyncOut();
        }
        public void SyncOutConfirmation()
        {
            _requestFormSync.SyncOutConfirmation();
            _vesselGoodReceiveSync.SyncOutConfirmation();
            _vesselGoodIssuedSync.SyncOutConfirmation();
            _vesselGoodReturnSync.SyncOutConfirmation();
            _vesselGoodJournalSync.SyncOutConfirmation();
        }
        public void SyncIn()
        {
            _requestFormSync.SyncIn();
            _vesselGoodReceiveSync.SyncIn();
            _itemSync.SyncIn();
            _itemDimensionSync.SyncIn();
        }

        public void SyncInConfirmation()
        {
            _requestFormSync.SyncInConfirmation();
            _vesselGoodReceiveSync.SyncInConfirmation();
            _itemSync.SyncInConfirmation();
            _itemDimensionSync.SyncInConfirmation();
        }

        public void SyncInComplete()
        {
            _requestFormSync.SyncInComplete();
            _vesselGoodReceiveSync.SyncInComplete();
            _itemSync.SyncInComplete();
            _itemDimensionSync.SyncInComplete();
        }
    }
}
