using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DxSyncClient.VesselInventory
{
    class DbConnectionFactory
    {
        public enum DBConnectionString
        {
            DBVesselInventory,
            DBSyncClientVesselInventory
        }
        private static string GetConnectionString(string connectionStringName)
        {
            return ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
        }
        public static IDbConnection GetConnection(DBConnectionString dBConnectionString)
        {
            switch (dBConnectionString)
            {
                case DBConnectionString.DBVesselInventory:
                    return new SqlConnection(GetConnectionString("DBVesselInventory"));
                case DBConnectionString.DBSyncClientVesselInventory:
                    return new SqlConnection(GetConnectionString("DBSyncClientVesselInventory"));
                default:
                    throw new ArgumentException();
            }
        }
    }
}
