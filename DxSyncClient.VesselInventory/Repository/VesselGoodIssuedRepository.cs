using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Setup;

namespace DxSyncClient.VesselInventory.Repository
{
    public class VesselGoodIssuedRepository : SyncRecordStageRepository
    {
        public void TransferFromMainToStaging()
        {
            var vesselGoodIssuedIds = GetVesselGoodIssuedIds();
            if (vesselGoodIssuedIds.Count() == 0)
            {
                return;
            }

            IList<DxSyncOutRecordStage> syncOutRecordStages = new List<DxSyncOutRecordStage>();

            AddVesselGoodIssuedIdToSyncOutStaging(syncOutRecordStages, vesselGoodIssuedIds);

            AddVesselGoodIssuedItemIdToSyncOutStaging(syncOutRecordStages, vesselGoodIssuedIds);

            MigratingDataTransactions(syncOutRecordStages, vesselGoodIssuedIds);
        }

        private static void AddVesselGoodIssuedItemIdToSyncOutStaging(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> vesselGoodIssuedIds)
        {
            string vesselGoodIssuedIds_ = string.Join(",", vesselGoodIssuedIds);
            string query = $@"SELECT [VesselGoodIssuedItemId], [VesselGoodIssuedId] 
                             FROM [dbo].[VesselGoodIssuedItem]
                             WHERE [VesselGoodIssuedId] IN ({vesselGoodIssuedIds_ })
                             AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AddItemToStaging(syncOutRecordStages, reader);
                    }
                }
            }
        }

        public VesselGoodIssued GetVesselGoodIssued(string vesselGoodIssuedId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodIssuedId] ,[VesselGoodIssuedNumber]
                            ,[VesselGoodIssuedDate] ,[ShipId]
                            ,[ShipName] ,[Notes]
                            ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate]
                            ,[LastModifiedBy] ,[IsHidden], [ClientId], [Version]
                      FROM [dbo].[Out_VesselGoodIssued] WHERE [VesselGoodIssuedId] = @VesselGoodIssuedId AND [Version] = @Version";
                return connection.Query<VesselGoodIssued>(query, 
                    new {
                        VesselGoodIssuedId = vesselGoodIssuedId,
                        Version = version
                    }).FirstOrDefault();
            }
        }
        public VesselGoodIssuedItem GetVesselGoodIssuedItem(string vesselGoodIssuedItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodIssuedItemId]
                            ,[VesselGoodIssuedId] ,[ItemId]
                            ,[ItemGroupId] ,[ItemName]
                            ,[ItemDimensionNumber] ,[BrandTypeId]
                            ,[BrandTypeName] ,[ColorSizeId]
                            ,[ColorSizeName] ,[Qty]
                            ,[Uom] ,[SyncStatus]
                            ,[CreatedDate] ,[CreatedBy]
                            ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden]
                            ,[ClientId] ,[Version]
                      FROM [dbo].[Out_VesselGoodIssuedItem] WHERE [VesselGoodIssuedItemId] = @VesselGoodIssuedItemId AND [Version] = @Version";
                return connection.Query<VesselGoodIssuedItem>(query, 
                    new {
                        VesselGoodIssuedItemId = vesselGoodIssuedItemId,
                        Version = version
                    }).FirstOrDefault();
            }
        }

        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> vesselGoodIssuedIds)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                CreateStagingSyncOut(syncOutRecordStages);
                CopyVesselGoodIssuedToStaging();
                CopyVesselGoodIssuedItemToStaging(vesselGoodIssuedIds);
                UpdateSyncStatusToOnStaging(vesselGoodIssuedIds);
                scope.Complete();
            }
        }
        private void CopyVesselGoodIssuedToStaging()
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodIssued]
                           ([VesselGoodIssuedId], [ClientId], [Version] ,[VesselGoodIssuedNumber] ,[VesselGoodIssuedDate] 
                            ,[ShipId] ,[ShipName] ,[Notes] ,[SyncStatus] ,[CreatedDate] ,[CreatedBy] 
                            ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden])
                     VALUES
                           (@VesselGoodIssuedId ,@ClientId ,@Version ,@VesselGoodIssuedNumber ,@VesselGoodIssuedDate 
                            ,@ShipId ,@ShipName ,@Notes ,@SyncStatus ,@CreatedDate ,@CreatedBy 
                            ,@LastModifiedDate ,@LastModifiedBy ,@IsHidden)";

                connection.Execute(query, GetVesselGoodIssueds());
            }
        }

        private void CopyVesselGoodIssuedItemToStaging(IEnumerable<int> vesselGoodIssuedIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodIssuedItem]
                           ([VesselGoodIssuedItemId] ,[ClientId] ,[Version] ,[VesselGoodIssuedId] ,[ItemId] ,[ItemGroupId]
                           ,[ItemName] ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId]
                           ,[ColorSizeName] ,[Qty] ,[Uom] ,[SyncStatus] ,[CreatedDate] ,[CreatedBy]
                           ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden])
                     VALUES
                           (@VesselGoodIssuedItemId ,@ClientId ,@Version ,@VesselGoodIssuedId ,@ItemId ,@ItemGroupId
                           ,@ItemName ,@ItemDimensionNumber ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId
                           ,@ColorSizeName ,@Qty ,@Uom ,@SyncStatus ,@CreatedDate ,@CreatedBy
                           ,@LastModifiedDate ,@LastModifiedBy ,@IsHidden)";

                connection.Execute(query, GetVesselGoodIssuedItems(vesselGoodIssuedIds));
            }
        }

        private void UpdateSyncStatusToOnStaging(IEnumerable<int> vesselGoodIssuedIds)
        {
            string vesselGoodIssuedIds_ = string.Join(",", vesselGoodIssuedIds);
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateGIQuery = $@"UPDATE [dbo].[VesselGoodIssued]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodIssuedId] IN ({vesselGoodIssuedIds_})";
                string updateGIItemQuery = $@"UPDATE [dbo].[VesselGoodIssuedItem] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodIssuedId] IN ({vesselGoodIssuedIds_})";
                connection.Execute(updateGIQuery);
                connection.Execute(updateGIItemQuery);
            }
        }

        private static void AddItemToStaging(IList<DxSyncOutRecordStage> syncRecordStages, IDataReader reader)
        {
            var vesselGoodIssuedItemId = reader["VesselGoodIssuedItemId"].ToString();
            var vesselGoodIssuedId = reader["VesselGoodIssuedId"].ToString();

            var parent = syncRecordStages.Where(x => x.ReferenceId == vesselGoodIssuedId).FirstOrDefault();

            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = parent.RecordStageId;

            parent.DataCount += 1;

            syncRecordStages.Add(new DxSyncOutRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodIssuedItemId,
                ClientId = SetupEnvironment.Client.ClientId,
                EntityName = typeof(VesselGoodIssuedItem).Name,
                IsFile = false,
                Version = 1,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private void AddVesselGoodIssuedIdToSyncOutStaging(IList<DxSyncOutRecordStage> dxSyncRecordStages, IEnumerable<int> vesselGoodIssuedIds)
        {
            foreach(var vesselGoodIssuedId in vesselGoodIssuedIds)
            {
                dxSyncRecordStages.Add(new DxSyncOutRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = SetupEnvironment.HelperValue.Root,
                    ReferenceId = vesselGoodIssuedId.ToString(),
                    ClientId = SetupEnvironment.Client.ClientId,
                    EntityName = typeof(VesselGoodIssued).Name,
                    IsFile = false,
                    Version = 1,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }

        private IEnumerable<VesselGoodIssued> GetVesselGoodIssueds()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} AS ClientId, {1} AS Version
                                FROM [dbo].[VesselGoodIssued]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<VesselGoodIssued>(query).ToList();
            }
        }

        private IEnumerable<VesselGoodIssuedItem> GetVesselGoodIssuedItems(IEnumerable<int> vesselGoodIssuedIds)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string vesselGoodIssuedIds_ = string.Join(",", vesselGoodIssuedIds);

                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} AS ClientId, {1} AS Version
                                 FROM [dbo].[VesselGoodIssuedItem]
                                 WHERE [VesselGoodIssuedId] IN ({vesselGoodIssuedIds_ })
                                 AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";
                return connection.Query<VesselGoodIssuedItem>(query).ToList();
            }
        }
        private IEnumerable<int> GetVesselGoodIssuedIds()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodIssuedId]
                                FROM [dbo].[VesselGoodIssued]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<int>(query).ToList();
            }
        }
    }
}
