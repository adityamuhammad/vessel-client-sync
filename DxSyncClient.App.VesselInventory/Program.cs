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
            clientSync.TransferFromMainToStaging();
            Console.WriteLine("Done");

            Console.Write("Connecting to server... ");
            if (clientSync.Connect())
            {

                Console.WriteLine("Connected");
                Console.Write("Authenticating... ");
                clientSync.Authenticate();
                Console.WriteLine("Authenticated");

                //Console.Write("Sync out process... ");
                //clientSync.SyncOut();
                //Console.WriteLine("Done");

                //Console.Write("Sync out confirmation process... ");
                //clientSync.SyncOutConfirmation();
                //Console.WriteLine("Done");

                Console.Write("Sync in process... ");
                clientSync.SyncIn();
                clientSync.SyncInConfirmation();
                clientSync.SyncInComplete();

                Console.WriteLine("Finish");
            } else
            {
                Console.WriteLine("Failed connect to server.");
            }
            

            Console.ReadKey();

        }
    }
}
