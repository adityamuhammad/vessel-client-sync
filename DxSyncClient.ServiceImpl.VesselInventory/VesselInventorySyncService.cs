using DxSync.Common;
using DxSyncClient.RequestAPIModule;
using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory.Module.RequestForm;
using DxSyncClient.ServiceImpl.VesselInventory.Module.VesselGoodIssued;
using System.Threading.Tasks;

namespace DxSyncClient.ServiceImpl.VesselInventory
{

    public class VesselInventorySyncService : AbstractClientSyncService, IClientSyncService
    {
        private readonly RequestFormSync _requestFormSync;
        private readonly VesselGoodIssuedSync _vesselGoodIssuedSync;
        public VesselInventorySyncService()
        {
            _requestFormSync = SyncFactory.RequestFormSync;
            _vesselGoodIssuedSync = SyncFactory.VesselGoodIssuedSync;
        }
        public bool TestConnectToAPIEndPoint()
        {
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(APISyncEndpoint.CheckConnection);
                return await httpExtensions.GetRaw();
            });
            var result = responseData.GetAwaiter().GetResult();
            if(result != null)
            {
                if (result.StatusCode == HttpResponseCode.OK)
                    return true;
                else
                    return false;
            }
            return false;
        }

        public void InitializeData()
        {
            _requestFormSync.InitializeData();
            _vesselGoodIssuedSync.InitializeData();
        }

        public void SyncOut(string token)
        {
            _requestFormSync.SyncOut(token);
        }

        public void SyncIn()
        {
            throw new System.NotImplementedException();
        }

    }
}
