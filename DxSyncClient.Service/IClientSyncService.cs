namespace DxSyncClient.Service
{
    public interface IClientSyncService
    {
        bool Connect();
        void InitializeData();
        void Authenticate();
        void SetToken();
        void SyncOut();
        void SyncOutConfirmation();
        void SyncIn();
        void SyncInConfirmation();
    }
}
