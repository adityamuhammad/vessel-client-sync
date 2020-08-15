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
        public static SqlConnection DBVesselInventory()
        {
            return new SqlConnection(GetConnectionString("DBVesselInventory"));
        }
        public static SqlConnection DBSyncVesselInventory()
        {
            return new SqlConnection(GetConnectionString("DBSyncClientVesselInventory"));
        }
    }
}
