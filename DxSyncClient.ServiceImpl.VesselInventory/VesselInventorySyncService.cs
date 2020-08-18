using DxSync.Common;
using DxSyncClient.RequestAPIModule;
using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory.Modules;
using System.Threading.Tasks;

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
            _requestFormSync.SyncOut(Token);
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
