﻿using System;
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
    public class VesselGoodReturnRepository : SyncRecordStageRepository
    {
        public void TransferFromMainToStaging()
        {
            var vesselGoodReturnIds = GetVesselGoodReturnIds();
            if (vesselGoodReturnIds.Count() == 0)
            {
                return;
            }

            string vesselGoodReturnIds_ = string.Join(",", vesselGoodReturnIds);

            IList<DxSyncOutRecordStage> syncOutRecordStages = new List<DxSyncOutRecordStage>();

            AddVesselGoodReturnIdToSyncRecordStage(syncOutRecordStages, vesselGoodReturnIds);

            AddVesselGoodReturnItemIdToSyncRecordStage(syncOutRecordStages, vesselGoodReturnIds);

            MigratingDataTransactions(syncOutRecordStages, vesselGoodReturnIds);
        }

        private static void AddVesselGoodReturnItemIdToSyncRecordStage(IList<DxSyncOutRecordStage> syncOutRecordStage, IEnumerable<int> vesselGoodReturnIds)
        {
            string vesselGoodReturnIds_ = string.Join(",", vesselGoodReturnIds);
            string query = $@"SELECT [VesselGoodReturnItemId], [VesselGoodReturnId] 
                             FROM [dbo].[VesselGoodReturnItem]
                             WHERE [VesselGoodReturnId] IN ({vesselGoodReturnIds_ })
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
                        AddToRecordStage(syncOutRecordStage, reader);
                    }

                }
            }
        }

        public VesselGoodReturn GetVesselGoodReturn(string vesselGoodReturnId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodReturnId] ,[VesselGoodReturnNumber]
                            ,[VesselGoodReturnDate] ,[ShipId] ,[ShipName]
                            ,[Notes] ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate]
                            ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]
                            ,[ClientId] ,[Version]
                      FROM [dbo].[Out_VesselGoodReturn] WHERE [VesselGoodReturnId] = @VesselGoodReturnId AND [Version] = @Version";
                return connection.Query<VesselGoodReturn>(query, 
                    new {
                        VesselGoodReturnId = vesselGoodReturnId,
                        Version = version
                    }).FirstOrDefault();
            }
        }

        public VesselGoodReturnItem GetVesselGoodReturnItem(string vesselGoodReturnItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodReturnItemId] ,[VesselGoodReturnId]
                            ,[ItemId] ,[ItemGroupId]
                            ,[ItemDimensionNumber] ,[ItemName]
                            ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName]
                            ,[Qty] ,[Reason]
                            ,[Uom] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate]
                            ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]
                            ,[ClientId] ,[Version]
                      FROM [dbo].[Out_VesselGoodReturnItem] 
                      WHERE [VesselGoodReturnItemId] = @VesselGoodReturnItemId
                      AND [Version] = @Version";
                return connection.Query<VesselGoodReturnItem>(query, 
                    new {
                        VesselGoodReturnItemId = vesselGoodReturnItemId,
                        Version = version
                    }).FirstOrDefault();
            }

        }

        private static void AddToRecordStage(IList<DxSyncOutRecordStage> syncRecordStages, IDataReader reader)
        {
            var vesselGoodReturnItemId = reader["VesselGoodReturnItemId"].ToString();
            var vesselGoodReturnId = reader["VesselGoodReturnId"].ToString();

            var parent = syncRecordStages.Where(x => x.ReferenceId == vesselGoodReturnId).FirstOrDefault();

            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = parent.RecordStageId;

            parent.DataCount += 1;

            syncRecordStages.Add(new DxSyncOutRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodReturnItemId,
                ClientId = SetupEnvironment.Client.ClientId,
                EntityName = typeof(VesselGoodReturnItem).Name,
                IsFile = false,
                Version = 1,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }
        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> vesselGoodReturnIds)
        {
            string vesselGoodReturnIds_ = string.Join(",", vesselGoodReturnIds);

            using(TransactionScope scope = new TransactionScope())
            {
                CreateStagingSyncOut(syncOutRecordStages);
                CopyVesselGoodReturnToStaging();
                CopyVesselGoodReturnItemToStaging(vesselGoodReturnIds);
                UpdateSyncStatusToOnStaging(vesselGoodReturnIds_);
                scope.Complete();
            }
        }
        private void UpdateSyncStatusToOnStaging(string vesselGoodReturnIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateGIQuery = $@"UPDATE [dbo].[VesselGoodReturn]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodReturnId] IN ({vesselGoodReturnIds_ })";
                string updateGIItemQuery = $@"UPDATE [dbo].[VesselGoodReturnItem] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodReturnId] IN ({vesselGoodReturnIds_})";
                connection.Execute(updateGIQuery);
                connection.Execute(updateGIItemQuery);
            }
        }
        private void AddVesselGoodReturnIdToSyncRecordStage(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> vesselGoodReturnIds)
        {
            foreach(var vesselGoodReturnId in vesselGoodReturnIds)
            {
                syncOutRecordStages.Add(new DxSyncOutRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = SetupEnvironment.HelperValue.Root,
                    ReferenceId = vesselGoodReturnId.ToString(),
                    ClientId = SetupEnvironment.Client.ClientId,
                    EntityName = typeof(VesselGoodReturn).Name,
                    IsFile = false,
                    Version = 1,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
        private IEnumerable<VesselGoodReturn> GetVesselGoodReturns()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} AS ClientId, {1} AS Version
                                FROM [dbo].[VesselGoodReturn]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<VesselGoodReturn>(query).ToList();
            }
        }

        private void CopyVesselGoodReturnToStaging()
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodReturn]
                           ([VesselGoodReturnId] ,[ClientId] ,[Version] ,[VesselGoodReturnNumber] 
                           ,[VesselGoodReturnDate] ,[ShipId] ,[ShipName] ,[Notes]
                           ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy]
                           ,[SyncStatus] ,[IsHidden])
                     VALUES
                           (@VesselGoodReturnId ,@ClientId ,@Version ,@VesselGoodReturnNumber
                           ,@VesselGoodReturnDate ,@ShipId ,@ShipName ,@Notes
                           ,@CreatedDate ,@CreatedBy ,@LastModifiedDate ,@LastModifiedBy
                           ,@SyncStatus ,@IsHidden)";
                connection.Execute(query, GetVesselGoodReturns());
            }
        }
        private void CopyVesselGoodReturnItemToStaging(IEnumerable<int> vesselGoodReturnIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodReturnItem]
                           ([VesselGoodReturnItemId] ,[ClientId] ,[Version] ,[VesselGoodReturnId] ,[ItemId]
                           ,[ItemGroupId] ,[ItemDimensionNumber] ,[ItemName] ,[BrandTypeId]
                           ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Reason]
                           ,[Uom] ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy]
                           ,[SyncStatus] ,[IsHidden])
                     VALUES
                           (@VesselGoodReturnItemId  ,@ClientId ,@Version ,@VesselGoodReturnId ,@ItemId
                           ,@ItemGroupId ,@ItemDimensionNumber ,@ItemName ,@BrandTypeId
                           ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Reason
                           ,@Uom ,@CreatedDate ,@CreatedBy ,@LastModifiedDate ,@LastModifiedBy
                           ,@SyncStatus ,@IsHidden)";

                connection.Execute(query, GetVesselGoodReturnItems(vesselGoodReturnIds));
            }
        }

        private IEnumerable<VesselGoodReturnItem> GetVesselGoodReturnItems(IEnumerable<int> vesselGoodReturnIds)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string vesselGoodReturnIds_ = string.Join(",", vesselGoodReturnIds);

                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} AS ClientId, {1} AS Version
                                 FROM [dbo].[VesselGoodReturnItem]
                                 WHERE [VesselGoodReturnId] IN ({vesselGoodReturnIds_ })
                                 AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";
                return connection.Query<VesselGoodReturnItem>(query).ToList();
            }
        }

        private IList<int> GetVesselGoodReturnIds()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodReturnId]
                                FROM [dbo].[VesselGoodReturn]
                                WHERE  [CreatedDate] < DATEADD(HOUR, -1, GETDATE())
                                AND [IsHidden] = 0 AND SyncStatus = 'NOT SYNC'";
                return connection.Query<int>(query).ToList();
            }
        }
    }
}
