using DxSyncClient.VesselInventory;
using System;

namespace DxSyncClient.App.VesselInventory
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientSync = new VesselInventorySyncService();

            Console.Write("Transfering from main to staging... ");
            clientSync.InitializeData();
            Console.WriteLine("Done");

            Console.Write("Connecting to server... ");
            if (clientSync.Connect())
            {

                Console.WriteLine("Connected");
                Console.Write("Authenticating... ");
                clientSync.Authenticate();
                Console.WriteLine("Authenticated");

                Console.Write("Sync Out Process... ");
                clientSync.SyncOut();
                Console.WriteLine("Done");

                Console.Write("Sync Out Confirmation Process... ");
                clientSync.SyncOutConfirmation();
                Console.WriteLine("Done");

                clientSync.SyncIn();
                clientSync.SyncInConfirmation();

                Console.WriteLine("Finish");
            } else
            {
                Console.WriteLine("Failed connect to server.");
            }

            Console.ReadKey();

        }
    }
}
