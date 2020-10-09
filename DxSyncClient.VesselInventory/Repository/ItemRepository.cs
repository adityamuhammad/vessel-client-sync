using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Setup;
using System;
using System.Data;
using System.Linq;
using System.Transactions;

namespace DxSyncClient.VesselInventory.Repository
{
    public class ItemRepository : SyncRecordStageRepository
    {
        private Item GetItemIn(string itemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @" SELECT [ItemId] ,[ItemGroupId] ,[ItemName] ,[Uom] ,[SyncStatus] ,[ClientId] ,[Version]
                       FROM [dbo].[In_Item] WHERE [ItemId] = @ItemId AND [Version] = @Version
                      ORDER BY [Version]";
                return connection.Query<Item>(query, 
                    new {
                        ItemId = itemId,
                        Version = version
                    }).SingleOrDefault();
            }
            

        }
        public void TransferFromStagingToMain()
        {
            var stagingSyncIn = GetStagingSyncIn<Item>(DxSyncStatusStage.SYNC_COMPLETE);
            if (stagingSyncIn.Count() > 0)
            {
                using (var scope = new TransactionScope())
                {
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
                    {
                        connection.Open();
                        foreach (var staging in stagingSyncIn)
                        {
                            var refData = GetItemIn(staging.ReferenceId, staging.Version);
                            string query;
                            if(staging.Version > 1)
                            {
                                query = @"UPDATE [dbo].[Item]
                                         SET [ItemGroupId] = @ItemGroupId
                                             ,[ItemName] = @ItemName
                                             ,[Uom] = @Uom
                                         WHERE [ItemId] = @ItemId";
                            } else
                            {
                                query = @"INSERT INTO [dbo].[Item]
                                           ([ItemId] ,[ItemGroupId] ,[ItemName] ,[Uom] ,[SyncStatus])
                                         VALUES
                                           (@ItemId ,@ItemGroupId ,@ItemName ,@Uom ,@SyncStatus)";
                            }
                            refData.SyncStatus = DxSyncStatusStage.SYNC;
                            connection.Execute(query, refData);
                        }
                    }
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
                    {
                        connection.Open();
                        var syncIds = string.Join(",",(from staging in stagingSyncIn select $"'{staging.RecordStageId}'"));
                        string query = $@"UPDATE [dbo].[SyncInRecordStage]
                                         SET [StatusStage] = 'ON MAIN' 
                                         WHERE [RecordStageId] IN ({syncIds})";
                        connection.Execute(query);
                    }
                    scope.Complete();
                }
            }

        }
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
