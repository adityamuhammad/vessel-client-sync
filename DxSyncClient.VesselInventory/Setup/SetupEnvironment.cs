using System.Configuration;

namespace DxSyncClient.VesselInventory
{
    public class SetupEnvironment
    {
        public class Client
        {
            //static
            public static int ClientId => int.Parse(ConfigurationManager.AppSettings["ClientId"]);
            public static int ShipId => int.Parse(ConfigurationManager.AppSettings["ShipId"]);
            public static string ShipName => ConfigurationManager.AppSettings["ShipName"];
            public static string ApplicationName => ConfigurationManager.AppSettings["ApplicationName"];
            public static string UploadPath => ConfigurationManager.AppSettings["UploadPath"];
            public static string Token = string.Empty;

        }

        public static class HelperValue
        {
            public const string Root = "0";
            public const int ChunkSize = 10 * 1024;
        }

    }
}
