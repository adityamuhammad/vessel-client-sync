using System.Configuration;
using System.Data.SqlClient;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    class DbConnectionFactory
    {
        private static string GetConnectionString(string connectionStringName)
        {
            return ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
        }
        public static SqlConnection VesselInventoryDB()
        {
            return new SqlConnection(GetConnectionString("VesselInventoryDB"));
        }
        public static SqlConnection SyncVesselInventoryDB()
        {
            return new SqlConnection(GetConnectionString("SyncVesselDB"));
        }
    }
}
