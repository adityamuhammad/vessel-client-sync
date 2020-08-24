using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;

namespace DxSyncClient.ServiceImpl.VesselInventory.Repository
{
    public class VesselGoodIssuedRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {
            var vesselGoodIssuedIds = GetVesselGoodIssuedIds();
            if (vesselGoodIssuedIds.Count() <= 0) return;

            string vesselGoodIssuedIds_ = string.Join(",", vesselGoodIssuedIds);
            var tableGuidVesselGoodIssuedId = GuidPair(vesselGoodIssuedIds);
            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();
            AddVesselGoodReceiveIdToSyncRecordStage(tableGuidVesselGoodIssuedId, dxSyncRecordStages);
            string query = @"SELECT [VesselGoodIssuedItemId], [VesselGoodIssuedId] 
                             FROM [dbo].[VesselGoodIssuedItem]
                             WHERE [VesselGoodIssuedId] IN (" + vesselGoodIssuedIds_ + ") " +
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
                        AddToRecordStage(tableGuidVesselGoodIssuedId, dxSyncRecordStages, reader);
                    }
                }
            }
            StageTransactions(dxSyncRecordStages, vesselGoodIssuedIds_);
        }
        public VesselGoodIssued GetVesselGoodIssued(string vesselGoodIssuedId)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                string query =
                    @"SELECT [VesselGoodIssuedId] ,[VesselGoodIssuedNumber]
                            ,[VesselGoodIssuedDate] ,[ShipId]
                            ,[ShipName] ,[Notes]
                            ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate]
                            ,[LastModifiedBy] ,[IsHidden]
                      FROM [dbo].[VesselGoodIssued] WHERE [VesselGoodIssuedId] = @VesselGoodIssuedId";
                return connection.Query<VesselGoodIssued>(query, new { vesselGoodIssuedId }).SingleOrDefault();
            }
        }
        public VesselGoodIssuedItem GetVesselGoodIssuedItem(string vesselGoodIssuedItemId)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
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
                      FROM [dbo].[VesselGoodIssuedItem] WHERE [VesselGoodIssuedItemId] = @VesselGoodIssuedItemId";
                return connection.Query<VesselGoodIssuedItem>(query, new { vesselGoodIssuedItemId }).SingleOrDefault();
            }
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

        private void UpdateSyncStatusToOnStaging(string vesselGoodIssuedIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                connection.Open();
                string updateGIQuery = @"UPDATE [dbo].[VesselGoodIssued]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodIssuedId] IN (" + vesselGoodIssuedIds_ + ")";
                string updateGIItemQuery = @"UPDATE [dbo].[VesselGoodIssuedItem] 
                                            SET [SyncStatus] = 'ON STAGING' 
                                            WHERE [VesselGoodIssuedId] IN ("+vesselGoodIssuedIds_+")";
                connection.Execute(updateGIQuery);
                connection.Execute(updateGIItemQuery);
            }
        }
        private static void AddToRecordStage(Hashtable tableGuidVesselGoodReceiveId, IList<DxSyncRecordStage> dxSyncRecordStages, IDataReader reader)
        {
            var vesselGoodIssuedItemId = reader["VesselGoodIssuedItemId"].ToString();
            var vesselGoodIssuedId = int.Parse(reader["VesselGoodIssuedId"].ToString());
            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = tableGuidVesselGoodReceiveId[vesselGoodIssuedId].ToString();

            dxSyncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodIssuedItemId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = typeof(VesselGoodIssuedItem).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }
        private void AddVesselGoodReceiveIdToSyncRecordStage(Hashtable guidVesselGoodReceiveIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(DictionaryEntry guidVesselGoodReceiveId in guidVesselGoodReceiveIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = guidVesselGoodReceiveId.Value.ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = guidVesselGoodReceiveId.Key.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = typeof(VesselGoodIssued).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
        private IEnumerable<int> GetVesselGoodIssuedIds()
        {
            using(IDbConnection connection = DbConnectionFactory.DBVesselInventory())
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
