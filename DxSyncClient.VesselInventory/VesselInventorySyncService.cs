﻿using DxSyncClient.VesselInventory.Modules;

namespace DxSyncClient.VesselInventory
{

    public class VesselInventorySyncService : SyncPermission
    {
        private readonly RequestFormSync _requestFormSync;
        private readonly VesselGoodReceiveSync _vesselGoodReceiveSync;
        private readonly VesselGoodIssuedSync _vesselGoodIssuedSync;
        private readonly VesselGoodReturnSync _vesselGoodReturnSync;
        private readonly VesselGoodJournalSync _vesselGoodJournalSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = ModuleFactory.RequestFormSync;
            _vesselGoodReceiveSync = ModuleFactory.VesselGoodReceiveSync;
            _vesselGoodIssuedSync = ModuleFactory.VesselGoodIssuedSync;
            _vesselGoodReturnSync = ModuleFactory.VesselGoodReturnSync;
            _vesselGoodJournalSync = ModuleFactory.VesselGoodJournalSync;
        }

        public void InitializeData()
        {
            _requestFormSync.InitializeData();
            _vesselGoodReceiveSync.InitializeData();
            _vesselGoodIssuedSync.InitializeData();
            _vesselGoodReturnSync.InitializeData();
            _vesselGoodJournalSync.InitializeData();
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
            
        }

        public void SyncInConfirmation()
        {
        }
    }
}