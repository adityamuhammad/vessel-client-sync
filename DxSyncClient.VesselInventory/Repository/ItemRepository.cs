using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Setup;
using System;
using System.Data;
using System.Transactions;

namespace DxSyncClient.VesselInventory.Repository
{
    public class ItemRepository : SyncRecordStageRepository
    {
        private void CreateItemReferenceDataSyncIn(Item item)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[In_Item]
                       ([ItemId] ,[ItemGroupId] ,[ItemName]
                       ,[Uom] ,[SyncStatus] ,[ClientId] ,[Version])
                     VALUES
                       (@ItemId ,@ItemGroupId ,@ItemName
                       ,@Uom ,@SyncStatus ,@ClientId ,@Version)";
                connection.Execute(query, item);
            }
        }
        public void CreateItemSyncIn(DxSyncInRecordStage recordStages, Item item)
        {
            
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    CreateStagingSyncIn(recordStages);
                    CreateItemReferenceDataSyncIn(item);
                    scope.Complete();

                } catch (Exception) { }
                
            }
        }

    }
}
