using DxSyncClient.Service;
using DxSyncClient.ServiceImpl.VesselInventory;
using System;

namespace DxSyncClient.App.VesselInventory
{
    class Program
    {
        static void Main(string[] args)
        {
            IClientSyncService clientSync = new VesselInventorySyncService();

            clientSync.InitializeData();

            if (clientSync.Connect())
            {
                clientSync.Authenticate();
                clientSync.SyncOut();
                //clientSync.SyncOutConfirmation();
                //clientSync.SyncIn();
                //clientSync.SyncInConfirmation();
            }

            Console.ReadKey();

        }
    }
}
