using System.Configuration;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class EnvClass
    {
        public static class Client
        {
            public static int ClientId => int.Parse(ConfigurationManager.AppSettings["ClientId"]);
            public static string ApplicationName => ConfigurationManager.AppSettings["ApplicationName"];
            public static string UploadPath => ConfigurationManager.AppSettings["UploadPath"];
        }

        public static class HelperValue
        {
            public const string Root = "0";
            public const int ChunkSize = 10 * 1024;
        }
    }
}
