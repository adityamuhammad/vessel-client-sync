namespace DxSyncClient.Service
{
    public interface IClientSyncService
    {
        void InitializeData();
        bool TestConnectToAPIEndPoint();
        string GetAuthenticationToken();
        void SyncOut(string token);
        void SyncIn();
    }
}
