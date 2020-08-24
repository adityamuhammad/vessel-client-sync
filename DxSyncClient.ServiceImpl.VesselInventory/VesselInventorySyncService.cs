using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory.Modules;

namespace DxSyncClient.ServiceImpl.VesselInventory
{

    public class VesselInventorySyncService : SyncPermission, IClientSyncService
    {
        private readonly RequestFormSync _requestFormSync;
        private readonly VesselGoodReceiveSync _vesselGoodReceiveSync;
        private readonly VesselGoodIssuedSync _vesselGoodIssuedSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = ModuleFactory.RequestFormSync;
            _vesselGoodReceiveSync = ModuleFactory.VesselGoodReceiveSync;
            _vesselGoodIssuedSync = ModuleFactory.VesselGoodIssuedSync;
        }

        public void InitializeData()
        {
            _requestFormSync.InitializeData();
            _vesselGoodReceiveSync.InitializeData();
            _vesselGoodIssuedSync.InitializeData();
        }

        public void SetToken()
        {
            _requestFormSync.SetToken(Token);
            _vesselGoodReceiveSync.SetToken(Token);
            _vesselGoodIssuedSync.SetToken(Token);
        }

        public void SyncOut()
        {
            _requestFormSync.SyncOut();
            _vesselGoodReceiveSync.SyncOut();
            _vesselGoodIssuedSync.SyncOut();
        }
        public void SyncOutConfirmation()
        {
            _requestFormSync.SyncOutConfirmation();
            _vesselGoodReceiveSync.SyncOutConfirmation();
            _vesselGoodIssuedSync.SyncOutConfirmation();
        }
        public void SyncIn()
        {
            throw new System.NotImplementedException();
        }

        public void SyncInConfirmation()
        {
            throw new System.NotImplementedException();
        }
    }
}
