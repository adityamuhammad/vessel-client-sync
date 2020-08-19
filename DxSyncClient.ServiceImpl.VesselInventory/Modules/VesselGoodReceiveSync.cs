using DxSync.Log;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class VesselGoodReceiveSync
    {
        private readonly ILogger _logger;
        private readonly VesselGoodReceiveRepository _vesselGoodReceiveRepository;
        private string _token;
        public VesselGoodReceiveSync()
        {
            _vesselGoodReceiveRepository = RepositoryFactory.VesselGoodReceiveRepository;
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
        }
        public void InitializeData()
        {
             _vesselGoodReceiveRepository.InitializeData();
        }
        public void SetToken(string token)
        {
            _token = token;
        }

    }
}
