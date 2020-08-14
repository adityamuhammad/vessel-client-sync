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

        public static class EntityName
        {
            public const string RequestForm = "RequestForm";
            public const string RequestFormItem = "RequestFormItem";
            public const string VesselGoodIssued = "VesselGoodIssued";
            public const string VesselGoodIssuedItem = "VesselGoodIssuedItem";
            public const string VesselGoodReceive = "VesselGoodReceive";
            public const string VesselGoodReceiveItem = "VesselGoodReceiveItem";
            public const string VesselGoodReturn = "VesselGoodReturn";
            public const string VesselGoodReturnItemm = "VesselGoodReturnItem";
        }

        public static class HelperValue
        {
            public const string Root = "0";
        }
    }
}
