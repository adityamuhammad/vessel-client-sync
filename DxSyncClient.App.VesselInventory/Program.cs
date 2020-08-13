using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory;
using System;

namespace DxSyncClient.App.VesselInventory
{
    class Program
    {
        public static object IClientSyncService { get; private set; }

        static void Main(string[] args)
        {
            IClientSyncService clientSync = new VesselInventorySyncService();

            clientSync.InitializeData();

            bool isConnected = clientSync.TestConnectToAPIEndPoint();
            if (!isConnected) return;

            var token = clientSync.GetAuthenticationToken();
            clientSync.SyncOut(token);
            Console.ReadKey();

        }
    }
}
