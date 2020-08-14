namespace DxSyncClient.RequestAPIModule
{
    public class APISyncEndpoint
    {
        public const string CheckConnection = "http://localhost:50907/api/sync/connection_status";
        public const string GetToken = "http://localhost:50907/api/sync/token";
        public const string SyncOut = "http://localhost:50907/api/sync/out";
        public const string SyncOutConfirmation = "http://localhost:50907/api/sync/out_confirmation";
        public const string SyncOutFileCheck = "http://localhost:50907/api/sync/file_out_check";
        public const string SyncOutFile = "http://localhost:50907/api/sync/file_out";
        public const string SyncOutFileConfirmation = "http://localhost:50907/api/sync/file_out_confirmation";
    }
}
