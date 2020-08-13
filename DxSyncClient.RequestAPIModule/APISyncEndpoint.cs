namespace DxSyncClient.RequestAPIModule
{
    public class APISyncEndpoint
    {
        public const string CheckConnection = "http://localhost:50907/api/sync/connection_status";
        public const string GetToken = "http://localhost:50907/api/sync/token";
        public const string SyncOutData = "http://localhost:50907/api/sync/out";
    }
}
