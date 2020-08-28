using DxSyncClient.ServiceImpl.VesselInventory;
using System;

namespace DxSyncClient.App.VesselInventory
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientSync = new VesselInventorySyncService();

            clientSync.InitializeData();

            if (clientSync.Connect())
            {

                clientSync.Authenticate();

                Console.WriteLine("Sync Out Process...");
                clientSync.SyncOut();

                Console.WriteLine("Sync Out Confirmation Process...");
                clientSync.SyncOutConfirmation();

                clientSync.SyncIn();
                clientSync.SyncInConfirmation();

                Console.WriteLine("Done");
            }

            Console.ReadKey();

        }
    }
}
