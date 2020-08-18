using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory.Modules;

namespace DxSyncClient.ServiceImpl.VesselInventory
{

    public class VesselInventorySyncService : AbstractClientSyncService, IClientSyncService
    {
        private readonly RequestFormSync _requestFormSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = ModuleFactory.RequestFormSync;
        }

        public void InitializeData()
        {
            _requestFormSync.InitializeData();
        }

        public void SyncOut()
        {
            _requestFormSync.SetToken(Token);
            _requestFormSync.SyncOut();
            _requestFormSync.SyncOutConfirmation();
        }

        public void SyncIn()
        {
            throw new System.NotImplementedException();
        }

        public void SyncOutConfirmation()
        {
            throw new System.NotImplementedException();
        }

        public void SyncInConfirmation()
        {
            throw new System.NotImplementedException();
        }
    }
}
