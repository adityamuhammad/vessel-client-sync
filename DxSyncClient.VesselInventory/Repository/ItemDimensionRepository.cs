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
    public class ItemDimensionRepository : SyncRecordStageRepository
    {
        public void TransferFromStagingToMain()
        {
            var stagingSyncIn = GetStagingSyncIn<ItemDimension>(DxSyncStatusStage.SYNC_COMPLETE);
            if (stagingSyncIn.Count() > 0)
                {
                using (var scope = new TransactionScope())
                {
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
                    {
                        connection.Open();
                        foreach (var staging in stagingSyncIn)
                        {
                            var refData = GetItemDimensionIn(staging.ReferenceId, staging.Version);
                            string query;
                            if(staging.Version > 1)
                            {
                                query = @" UPDATE [dbo].[ItemDimension]
                                           SET [ItemId] = @ItemId
                                              ,[BrandTypeId] = @BrandTypeId
                                              ,[BrandTypeName] = @BrandTypeName
                                              ,[ColorSizeId] = @ColorSizeId
                                              ,[ColorSizeName] = @ColorSizeName
                                              ,[SyncStatus] = @SyncStatus
                                            WHERE [ItemDimensionNumber] = @ItemDimensionNumber";
                            } else
                            {
                                query = @"INSERT INTO [dbo].[ItemDimension]
                                               ([ItemDimensionNumber] ,[ItemId]
                                               ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId]
                                               ,[ColorSizeName] ,[SyncStatus])
                                           VALUES
                                               (@ItemDimensionNumber ,@ItemId
                                               ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId
                                               ,@ColorSizeName ,@SyncStatus)";
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

        private ItemDimension GetItemDimensionIn(string itemDimensionNumber, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [ItemDimensionNumber] ,[ItemId] ,[BrandTypeId]
                              ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[SyncStatus]
                              ,[Version] ,[ClientId]
                      FROM [dbo].[In_ItemDimension] WHERE [ItemDimensionNumber] = @ItemDimensionNumber AND [Version] = @Version
                      ORDER BY [Version]";
                return connection.Query<ItemDimension>(query, new {
                        ItemDimensionNumber = itemDimensionNumber,
                        Version = version
                }).SingleOrDefault();
            }
        }

        private void CreateItemReferenceDataSyncIn(ItemDimension itemDimension)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[In_ItemDimension]
                       ([ItemDimensionNumber] ,[ItemId] ,[BrandTypeId]
                       ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName]
                       ,[SyncStatus] ,[Version] ,[ClientId])
                     VALUES
                       (@ItemDimensionNumber ,@ItemId ,@BrandTypeId
                       ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName
                       ,@SyncStatus ,@Version ,@ClientId)";

                connection.Execute(query, itemDimension);
            }
        }

        public void CreateItemSyncIn(DxSyncInRecordStage recordStages, ItemDimension itemDimension)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    CreateStagingSyncIn(recordStages);
                    CreateItemReferenceDataSyncIn(itemDimension);
                    scope.Complete();

                } catch (Exception) { }
                
            }
        }
    }
}
