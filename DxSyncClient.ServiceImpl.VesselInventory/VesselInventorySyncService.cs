using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory.Modules;

namespace DxSyncClient.ServiceImpl.VesselInventory
{

    public class VesselInventorySyncService : AbstractClientSyncService, IClientSyncService
    {
        private readonly RequestFormSync _requestFormSync;
        private readonly VesselGoodReceiveSync _vesselGoodReceiveSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = ModuleFactory.RequestFormSync;
            _vesselGoodReceiveSync = ModuleFactory.VesselGoodReceiveSync;
        }

        public void InitializeData()
        {
            _requestFormSync.InitializeData();
            _vesselGoodReceiveSync.InitializeData();
        }

        public void SetToken()
        {
            _requestFormSync.SetToken(Token);
            _vesselGoodReceiveSync.SetToken(Token);
        }

        public void SyncOut()
        {
            _requestFormSync.SyncOut();
            _vesselGoodReceiveSync.SyncOut();
        }
        public void SyncOutConfirmation()
        {
            _requestFormSync.SyncOutConfirmation();
            _vesselGoodReceiveSync.SyncOutConfirmation();
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
