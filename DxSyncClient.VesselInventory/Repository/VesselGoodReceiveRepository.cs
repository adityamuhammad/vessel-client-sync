using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using Dapper;

namespace DxSyncClient.VesselInventory.Repository
{
    public class VesselGoodReceiveRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {
            var vesselGoodReceiveIds = GetVesselGoodReceiveIds();

            if (vesselGoodReceiveIds.Count() == 0) return;

            string vesselGoodReceiveIds_ = string.Join(",", vesselGoodReceiveIds);

            IList<DxSyncRecordStage> syncRecordStages = new List<DxSyncRecordStage>();

            AddVesselGoodReceiveIdToSyncRecordStage(vesselGoodReceiveIds, syncRecordStages);

            string query = @"SELECT [VesselGoodReceiveItemRejectId], [VesselGoodReceiveId] 
                             FROM [dbo].[VesselGoodReceiveItemReject]
                             WHERE [VesselGoodReceiveId] IN (" + vesselGoodReceiveIds_ + ") " +
                            "AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";
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
            StageTransactions(syncRecordStages, vesselGoodReceiveIds_);

        }

        public VesselGoodReceive GetVesselGoodReceive(string vesselGoodReceiveId)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = 
                    @"SELECT [VesselGoodReceiveId] ,[OfficeGoodIssuedNumber]
                              ,[VesselGoodReceiveNumber] ,[VesselGoodReceiveDate]
                              ,[ShipId] ,[ShipName] ,[BargeId] ,[BargeName]
                              ,[SyncStatus] ,[CreatedDate] ,[CreatedBy]
                              ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden]
                      FROM [dbo].[VesselGoodReceive]
                      WHERE [VesselGoodReceiveId] = @VesselGoodReceiveId";
                return connection.Query<VesselGoodReceive>(query, new { vesselGoodReceiveId }).SingleOrDefault();
            }
        }

        public VesselGoodReceiveItemReject GetVesselGoodReceiveItemReject(string vesselGoodReceiveItemRejectId)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodReceiveItemRejectId] ,[VesselGoodReceiveId] ,[RequestFormNumber]
                          ,[ItemId] ,[ItemGroupId] ,[ItemName] ,[ItemDimensionNumber] ,[BrandTypeId]
                          ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[Uom] ,[Qty] ,[CreatedDate]
                          ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]
                      FROM [dbo].[VesselGoodReceiveItemReject]
                      WHERE [VesselGoodReceiveItemRejectId] = @VesselGoodReceiveItemRejectId";
                return connection.Query<VesselGoodReceiveItemReject>(query, new { vesselGoodReceiveItemRejectId }).SingleOrDefault();
            }

        }

        private static void AddToRecordStage(IList<DxSyncRecordStage> syncRecordStages, IDataReader reader)
        {
            var vesselGoodReceiveItemRejectId = reader["VesselGoodReceiveItemRejectId"].ToString();
            var vesselGoodReceiveId = reader["VesselGoodReceiveId"].ToString();

            var parent = syncRecordStages.Where(x => x.ReferenceId == vesselGoodReceiveId).SingleOrDefault();

            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = parent.RecordStageId;

            parent.DataCount += 1;

            syncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodReceiveItemRejectId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = typeof(VesselGoodReceiveItemReject).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string vesselGoodReceiveIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                InsertToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(vesselGoodReceiveIds_);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(string vesselGoodReceiveIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateGRQuery = @"UPDATE [dbo].[VesselGoodReceive]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodReceiveId] IN (" + vesselGoodReceiveIds_ + ")";
                string updateGRItemQuery = @"UPDATE [dbo].[VesselGoodReceiveItemReject] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodReceiveId] IN ("+vesselGoodReceiveIds_+")";
                connection.Execute(updateGRQuery);
                connection.Execute(updateGRItemQuery);
            }
        }

        private void AddVesselGoodReceiveIdToSyncRecordStage(IEnumerable<int> vesselGoodReceiveIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(var vesselGoodReceiveId in vesselGoodReceiveIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = vesselGoodReceiveId.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = typeof(VesselGoodReceive).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
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
