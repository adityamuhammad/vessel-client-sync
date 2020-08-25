using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;

namespace DxSyncClient.ServiceImpl.VesselInventory.Repository
{
    public class VesselGoodJournalRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {

            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();

            string query = @"SELECT [VesselGoodJournalId]
                             FROM [dbo].[VesselGoodJournal]
                             WHERE [SyncStatus] = 'NOT SYNC'
                             AND [IsHidden] = 0 AND [InOut] = 'OUT'";
            IList<int> journalIds = new List<int>();

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
                        journalIds.Add(int.Parse(reader["VesselGoodJournalId"].ToString()));
                    }
                }
            }
            if(dxSyncRecordStages.Count > 0)
            {
                string journalIds_ = string.Join(",", journalIds);
                StageTransactions(dxSyncRecordStages, journalIds_);
            }
        }

        public VesselGoodJournal GetVesselGoodJournal(string vesselGoodJournalId)
        {

            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                string query =
                    @"SELECT [VesselGoodJournalId] ,[DocumentReference] ,[DocumentType] ,[ItemId] ,[ItemGroupId]
                            ,[ItemDimensionNumber] ,[ItemName] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Uom] ,[Qty] ,[InOut] 
                            ,[ShipId] ,[ShipName] ,[VesselGoodJournalDate] ,[SyncStatus] ,[IsHidden]
                     FROM [dbo].[VesselGoodJournal] WHERE [VesselGoodJournalId] = @VesselGoodJournalId";
                return connection.Query<VesselGoodJournal>(query, new { vesselGoodJournalId }).SingleOrDefault();
            }
        }

        private void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string journalIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                InsertToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(journalIds_);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(string journalIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                connection.Open();
                string updateGJQuery = @"UPDATE [dbo].[VesselGoodJournal]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodJournalId] IN (" + journalIds_ + ")";
                connection.Execute(updateGJQuery);
            }
        }

        private static void AddToRecordStage(IList<DxSyncRecordStage> dxSyncRecordStages, IDataReader reader)
        {
            var vesselGoodJournalId = reader["VesselGoodJournalId"].ToString();
            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = EnvClass.HelperValue.Root;

            dxSyncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodJournalId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = typeof(VesselGoodJournal).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }
    }
}
