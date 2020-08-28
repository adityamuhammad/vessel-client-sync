using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;

namespace DxSyncClient.VesselInventory.Repository
{
    public class VesselGoodReturnRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {
            var vesselGoodReturnIds = GetVesselGoodReturnIds();
            if (vesselGoodReturnIds.Count() == 0) return;

            string vesselGoodReturnIds_ = string.Join(",", vesselGoodReturnIds);

            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();

            AddVesselGoodReturnIdToSyncRecordStage(vesselGoodReturnIds, dxSyncRecordStages);

            string query = @"SELECT [VesselGoodReturnItemId], [VesselGoodReturnId] 
                             FROM [dbo].[VesselGoodReturnItem]
                             WHERE [VesselGoodReturnId] IN (" + vesselGoodReturnIds_ + ") " +
                            "AND SyncStatus = 'NOT SYNC' AND ISHidden = 0 ";

            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AddToRecordStage(dxSyncRecordStages, reader);
                    }
                }
            }
            StageTransactions(dxSyncRecordStages, vesselGoodReturnIds_);
        }

        public VesselGoodReturn GetVesselGoodReturn(string vesselGoodReturnId)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                string query =
                    @"SELECT [VesselGoodReturnId] ,[VesselGoodReturnNumber]
                            ,[VesselGoodReturnDate] ,[ShipId] ,[ShipName]
                            ,[Notes] ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate]
                            ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]
                      FROM [dbo].[VesselGoodReturn] WHERE [VesselGoodReturnId] = @VesselGoodReturnId";
                return connection.Query<VesselGoodReturn>(query, new { vesselGoodReturnId }).SingleOrDefault();
            }
        }

        public VesselGoodReturnItem GetVesselGoodReturnItem(string vesselGoodReturnItemId)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
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
                      FROM [dbo].[VesselGoodReturnItem] WHERE [VesselGoodReturnItemId] = @VesselGoodReturnItemId";
                return connection.Query<VesselGoodReturnItem>(query, new { vesselGoodReturnItemId }).SingleOrDefault();
            }

        }

        private static void AddToRecordStage(IList<DxSyncRecordStage> syncRecordStages, IDataReader reader)
        {
            var vesselGoodReturnItemId = reader["VesselGoodReturnItemId"].ToString();
            var vesselGoodReturnId = reader["VesselGoodReturnId"].ToString();

            var parent = syncRecordStages.Where(x => x.ReferenceId == vesselGoodReturnId).SingleOrDefault();

            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = parent.RecordStageId;

            parent.DataCount += 1;

            syncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodReturnItemId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = typeof(VesselGoodReturnItem).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }
        private void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string vesselGoodReturnIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                InsertToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(vesselGoodReturnIds_);
                scope.Complete();
            }
        }
        private void UpdateSyncStatusToOnStaging(string vesselGoodReturnIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                connection.Open();
                string updateGIQuery = @"UPDATE [dbo].[VesselGoodReturn]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodReturnId] IN (" + vesselGoodReturnIds_ + ")";
                string updateGIItemQuery = @"UPDATE [dbo].[VesselGoodReturnItem] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodReturnId] IN ("+ vesselGoodReturnIds_+")";
                connection.Execute(updateGIQuery);
                connection.Execute(updateGIItemQuery);
            }
        }
        private void AddVesselGoodReturnIdToSyncRecordStage(IEnumerable<int> vesselGoodReturnIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(var vesselGoodReturnId in vesselGoodReturnIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = vesselGoodReturnId.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = typeof(VesselGoodReturn).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
        private IList<int> GetVesselGoodReturnIds()
        {
            using(IDbConnection connection = DbConnectionFactory.DBVesselInventory())
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
