using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using Dapper;
using DxSyncClient.VesselInventory.Setup;

namespace DxSyncClient.VesselInventory.Repository
{
    public class VesselGoodReceiveRepository : SyncRecordStageRepository
    {
        public void TransferFromMainToStaging()
        {
            var vesselGoodReceiveIds = GetVesselGoodReceiveIds();

            if (vesselGoodReceiveIds.Count() == 0) return;

            IList<DxSyncOutRecordStage> syncRecordStages = new List<DxSyncOutRecordStage>();

            AddVesselGoodReceiveIdToSyncRecordStage(syncRecordStages, vesselGoodReceiveIds);

            AddVesselGoodReceiveItemRejectIdToSyncRecordStage(syncRecordStages, vesselGoodReceiveIds);

            MigratingDataTransactions(syncRecordStages, vesselGoodReceiveIds);

        }
        public void TransferFromStagingToMain()
        {
            var stagingSyncIn = GetStagingSyncIn<VesselGoodReceiveItem>(DxSyncStatusStage.SYNC_COMPLETE);
            if (stagingSyncIn.Count() > 0)
            {
                using (var scope = new TransactionScope())
                {
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
                    {
                        connection.Open();
                        foreach (var staging in stagingSyncIn)
                        {
                            var refData = GetVesselGoodReceiveItemIn(staging.ReferenceId, staging.Version);
                            string insertItem = @"INSERT INTO [dbo].[VesselGoodReceiveItem] ([VesselGoodReceiveId]
                                               ,[RequestFormNumber] ,[ItemId] ,[ItemGroupId]
                                               ,[ItemName] ,[ItemDimensionNumber] ,[BrandTypeId]
                                               ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName]
                                               ,[Qty] ,[Uom] ,[SyncStatus]
                                               ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate]
                                               ,[LastModifiedBy] ,[IsHidden])
                                             VALUES
                                               (@VesselGoodReceiveId ,@RequestFormNumber ,@ItemId
                                               ,@ItemGroupId ,@ItemName ,@ItemDimensionNumber
                                               ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId
                                               ,@ColorSizeName ,@Qty ,@Uom
                                               ,@SyncStatus ,@CreatedDate ,@CreatedBy
                                               ,@LastModifiedDate ,@LastModifiedBy ,@IsHidden)";
                            var documentNumber = connection.Query<string>("SELECT [VesselGoodReceiveNumber] FROM [dbo].[VesselGoodReceive] WHERE [VesselGoodReceiveId] = @VesselGoodReceiveId", new { refData.VesselGoodReceiveId }).SingleOrDefault();
                            var goodJournal = new VesselGoodJournal
                            {
                                BrandTypeId = refData.BrandTypeId,
                                BrandTypeName = refData.BrandTypeName,
                                ColorSizeId = refData.ColorSizeId,
                                ColorSizeName = refData.ColorSizeName,
                                DocumentReference = documentNumber,
                                DocumentType = "Vessel Good Receive",
                                InOut = "IN",
                                IsHidden = false,
                                ItemDimensionNumber = refData.ItemDimensionNumber,
                                ItemGroupId = refData.ItemGroupId,
                                ItemId = refData.ItemId,
                                ItemName = refData.ItemName,
                                Qty = refData.Qty,
                                ShipId = SetupEnvironment.Client.ShipId,
                                ShipName = SetupEnvironment.Client.ShipName,
                                Uom = refData.Uom,
                                SyncStatus = DxSyncStatusStage.NOT_SYNC,
                                VesselGoodJournalDate = refData.CreatedDate
                            };
                            string insertJournal = $@"INSERT INTO [dbo].[VesselGoodJournal]
                                                       ([DocumentReference] ,[DocumentType] ,[ItemId] ,[ItemGroupId]
                                                       ,[ItemDimensionNumber] ,[ItemName] ,[BrandTypeId] ,[BrandTypeName]
                                                       ,[ColorSizeId] ,[ColorSizeName] ,[Uom] ,[Qty]
                                                       ,[InOut] ,[ShipId] ,[ShipName] ,[VesselGoodJournalDate]
                                                       ,[SyncStatus] ,[IsHidden])
                                                     VALUES
                                                       (@DocumentReference ,@DocumentType ,@ItemId ,@ItemGroupId
                                                       ,@ItemDimensionNumber ,@ItemName ,@BrandTypeId ,@BrandTypeName
                                                       ,@ColorSizeId ,@ColorSizeName ,@Uom ,@Qty
                                                       ,@InOut ,@ShipId ,@ShipName ,@VesselGoodJournalDate
                                                       ,@SyncStatus ,@IsHidden)";
                            connection.Execute(insertItem, refData);
                            connection.Execute(insertJournal, goodJournal);
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

        private static void AddVesselGoodReceiveItemRejectIdToSyncRecordStage(IList<DxSyncOutRecordStage> syncRecordStages, IEnumerable<int> vesselGoodReceiveIds)
        {
            string vesselGoodReceiveIds_ = string.Join(",", vesselGoodReceiveIds);

            string query = $@"SELECT [VesselGoodReceiveItemRejectId], [VesselGoodReceiveId] 
                             FROM [dbo].[VesselGoodReceiveItemReject]
                             WHERE [VesselGoodReceiveId] IN ({vesselGoodReceiveIds_})
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
                        AddToRecordStage(syncRecordStages, reader);
                    }
                }
            }
        }
        public void CreateItemSyncIn(DxSyncInRecordStage recordStages, VesselGoodReceiveItem vesselGoodReceiveItem)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    CreateStagingSyncIn(recordStages);
                    CreateItemReferenceDataSyncIn(vesselGoodReceiveItem);
                    scope.Complete();
                } catch (Exception) { }
            }
        }

        private void CreateItemReferenceDataSyncIn(VesselGoodReceiveItem vesselGoodReceiveItem)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[In_VesselGoodReceiveItem]
                           ([VesselGoodReceiveItemId] ,[VesselGoodReceiveId] ,[RequestFormNumber]
                           ,[ItemId] ,[ItemGroupId] ,[ItemName]
                           ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName]
                           ,[ColorSizeId] ,[ColorSizeName] ,[Qty]
                           ,[Uom] ,[SyncStatus] ,[CreatedDate]
                           ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy]
                           ,[IsHidden] ,[Version] ,[ClientId])
                     VALUES
                           (@VesselGoodReceiveItemId ,@VesselGoodReceiveId ,@RequestFormNumber
                           ,@ItemId ,@ItemGroupId ,@ItemName ,@ItemDimensionNumber
                           ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId
                           ,@ColorSizeName ,@Qty ,@Uom ,@SyncStatus
                           ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                           ,@LastModifiedBy ,@IsHidden ,@Version ,@ClientId)";

                connection.Execute(query, vesselGoodReceiveItem);
            }

        }

        public VesselGoodReceive GetVesselGoodReceiveOut(string vesselGoodReceiveId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    @"SELECT [VesselGoodReceiveId] ,[OfficeGoodIssuedNumber]
                              ,[VesselGoodReceiveNumber] ,[VesselGoodReceiveDate]
                              ,[ShipId] ,[ShipName] ,[BargeId] ,[BargeName]
                              ,[SyncStatus] ,[CreatedDate] ,[CreatedBy]
                              ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden]
                              ,[ClientId] ,[Version]
                      FROM [dbo].[Out_VesselGoodReceive]
                      WHERE [VesselGoodReceiveId] = @VesselGoodReceiveId AND [Version] = @Version";
                return connection.Query<VesselGoodReceive>(query, 
                    new {
                        VesselGoodReceiveId = vesselGoodReceiveId,
                        Version = version
                    }).SingleOrDefault();
            }
        }
        public VesselGoodReceiveItem GetVesselGoodReceiveItemIn(string vesselGoodReceiveItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @" SELECT [VesselGoodReceiveItemId] ,[VesselGoodReceiveId]
                              ,[RequestFormNumber] ,[ItemId] ,[ItemGroupId] ,[ItemName]
                              ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId]
                              ,[ColorSizeName] ,[Qty] ,[Uom] ,[SyncStatus]
                              ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy]
                              ,[IsHidden]
                      FROM [dbo].[In_VesselGoodReceiveItem]
                      WHERE [VesselGoodReceiveItemId] = @VesselGoodReceiveItemId AND [Version] = @Version";
                return connection.Query<VesselGoodReceiveItem>(query, 
                    new {
                        VesselGoodReceiveItemId = vesselGoodReceiveItemId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        public VesselGoodReceiveItemReject GetVesselGoodReceiveItemReject(string vesselGoodReceiveItemRejectId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodReceiveItemRejectId] ,[VesselGoodReceiveId] ,[RequestFormNumber]
                          ,[ItemId] ,[ItemGroupId] ,[ItemName] ,[ItemDimensionNumber] ,[BrandTypeId]
                          ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[Uom] ,[Qty] ,[CreatedDate]
                          ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]
                          ,[ClientId] ,[Version]
                      FROM [dbo].[Out_VesselGoodReceiveItemReject]
                      WHERE [VesselGoodReceiveItemRejectId] = @VesselGoodReceiveItemRejectId AND [Version] = @Version";
                return connection.Query<VesselGoodReceiveItemReject>(query, 
                    new {
                        VesselGoodReceiveItemRejectId = vesselGoodReceiveItemRejectId,
                        Version = version
                    }).SingleOrDefault();
            }

        }

        private static void AddToRecordStage(IList<DxSyncOutRecordStage> syncRecordStages, IDataReader reader)
        {
            var vesselGoodReceiveItemRejectId = reader["VesselGoodReceiveItemRejectId"].ToString();
            var vesselGoodReceiveId = reader["VesselGoodReceiveId"].ToString();

            var parent = syncRecordStages.Where(x => x.ReferenceId == vesselGoodReceiveId).SingleOrDefault();

            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = parent.RecordStageId;

            parent.DataCount += 1;

            syncRecordStages.Add(new DxSyncOutRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodReceiveItemRejectId,
                ClientId = SetupEnvironment.Client.ClientId,
                EntityName = typeof(VesselGoodReceiveItemReject).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> dxSyncRecordStages, IEnumerable<int> vesselGoodReceiveIds)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                CreateStagingSyncOut(dxSyncRecordStages);
                CopyVesselGoodReceiveToStaging();
                CopyVesselGoodReceiveItemRejectToStaging(vesselGoodReceiveIds);
                UpdateSyncStatusToOnStaging(vesselGoodReceiveIds);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(IEnumerable<int> vesselGoodReceiveIds)
        {
            string vesselGoodReceiveIds_ = string.Join(",", vesselGoodReceiveIds);
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateGRQuery = $@"UPDATE [dbo].[VesselGoodReceive]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodReceiveId] IN ({vesselGoodReceiveIds_ })";
                string updateGRItemQuery = $@"UPDATE [dbo].[VesselGoodReceiveItemReject] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodReceiveId] IN ({vesselGoodReceiveIds_})";
                connection.Execute(updateGRQuery);
                connection.Execute(updateGRItemQuery);
            }
        }

        private void AddVesselGoodReceiveIdToSyncRecordStage(IList<DxSyncOutRecordStage> dxSyncRecordStages, IEnumerable<int> vesselGoodReceiveIds )
        {
            foreach(var vesselGoodReceiveId in vesselGoodReceiveIds)
            {
                dxSyncRecordStages.Add(new DxSyncOutRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = SetupEnvironment.HelperValue.Root,
                    ReferenceId = vesselGoodReceiveId.ToString(),
                    ClientId = SetupEnvironment.Client.ClientId,
                    EntityName = typeof(VesselGoodReceive).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
        private void CopyVesselGoodReceiveToStaging()
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodReceive]
                       ([VesselGoodReceiveId] ,[ClientId] ,[Version] ,[OfficeGoodIssuedNumber] ,[VesselGoodReceiveNumber]
                       ,[VesselGoodReceiveDate] ,[ShipId] ,[ShipName]
                       ,[BargeId] ,[BargeName] ,[SyncStatus] ,[CreatedDate]
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden])
                     VALUES
                       (@VesselGoodReceiveId ,@ClientId ,@Version ,@OfficeGoodIssuedNumber, @VesselGoodReceiveNumber,
                        @VesselGoodReceiveDate, @ShipId, @ShipName, 
                        @BargeId, @BargeName, @SyncStatus, @CreatedDate, 
                        @CreatedBy, @LastModifiedDate ,@LastModifiedBy,@IsHidden)";

                connection.Execute(query, GetVesselGoodReceives());
            }
        }
        private void CopyVesselGoodReceiveItemRejectToStaging(IEnumerable<int> vesselGoodReceiveIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                        @"INSERT INTO [dbo].[Out_VesselGoodReceiveItemReject]
                           ([VesselGoodReceiveItemRejectId] ,[ClientId] ,[Version] ,[VesselGoodReceiveId] ,[RequestFormNumber] ,[ItemId] 
                           ,[ItemGroupId] ,[ItemName] ,[ItemDimensionNumber]
                           ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName]
                           ,[Uom] ,[Qty] ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate]
                           ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]) 
                        VALUES
                           (@VesselGoodReceiveItemRejectId ,@ClientId ,@Version ,@VesselGoodReceiveId ,@RequestFormNumber ,@ItemId
                           ,@ItemGroupId ,@ItemName ,@ItemDimensionNumber 
                           ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName 
                           ,@Uom ,@Qty ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                           ,@LastModifiedBy ,@SyncStatus ,@IsHidden)";
                connection.Execute(query, GetVesselGoodReceiveItemRejects(vesselGoodReceiveIds));
            }
        }

        private IEnumerable<VesselGoodReceiveItemReject> GetVesselGoodReceiveItemRejects(IEnumerable<int> vesselGoodReceiveIds)
        {
            string vesselGoodReceiveIds_ = string.Join(",", vesselGoodReceiveIds);
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} AS ClientId, {1} AS Version
                             FROM [dbo].[VesselGoodReceiveItemReject]
                             WHERE [VesselGoodReceiveId] IN ({vesselGoodReceiveIds_})
                             AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";
                return connection.Query<VesselGoodReceiveItemReject>(query).ToList();
            }

        }
        private IEnumerable<VesselGoodReceive> GetVesselGoodReceives()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT *
                                FROM [dbo].[VesselGoodReceive]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<VesselGoodReceive>(query).ToList();
            }
        }

        private IEnumerable<int> GetVesselGoodReceiveIds()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodReceiveId]
                                FROM [dbo].[VesselGoodReceive]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<int>(query).ToList();
            }
        }
    }
}
