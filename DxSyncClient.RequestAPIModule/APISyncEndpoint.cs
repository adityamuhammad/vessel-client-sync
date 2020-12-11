namespace DxSyncClient.RequestAPIModule
{
    public class APISyncEndpoint
    {
        //private const string Host = "http://localhost:50907/";
        //private const string Host = "https://192.168.181.22:44384/";
        private const string Host = "https://ptmbp.co.id:44384/";
        public const string CheckConnection = Host + "api/sync/connection_status";
        public const string GetToken = Host + "api/sync/token";
        public const string SyncOut = Host + "api/sync/out";
        public const string SyncOutConfirmation = Host + "api/sync/out_confirmation";
        public const string SyncOutFileCheck = Host + "api/sync/file_out_check";
        public const string SyncOutFile = Host + "api/sync/file_out";
        public const string SyncOutFileConfirmation = Host + "api/sync/file_out_confirmation";
        public const string SyncInCheck = Host + "api/sync/sync_in_check";
        public const string SyncIn = Host + "api/sync/sync_in";
        public const string SyncInConfirmation = Host + "api/sync/sync_in_confirmation";
        public const string SyncInComplete = Host + "api/sync/sync_in_complete";
    }
}
