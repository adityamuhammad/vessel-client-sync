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
    public class VesselGoodJournalRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {

            IList<DxSyncOutRecordStage> syncRecordStages = new List<DxSyncOutRecordStage>();

            IList<string> documentReferences = new List<string>();
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodIssuedNumber]
                                 FROM [dbo].[VesselGoodIssued]
                                 WHERE [SyncStatus] = 'ON STAGING'
                                 AND [IsHidden] = 0";
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        documentReferences.Add($"'{reader["VesselGoodIssuedNumber"].ToString()}'");
                    }
                }
            }

            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodReturnNumber]
                                 FROM [dbo].[VesselGoodReturn]
                                 WHERE [SyncStatus] = 'ON STAGING'
                                 AND [IsHidden] = 0";
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        documentReferences.Add($"'{reader["VesselGoodReturnNumber"].ToString()}'");
                    }
                }
            }

            IList<int> journalIds = new List<int>();
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = @"SELECT [VesselGoodJournalId]
                                 FROM [dbo].[VesselGoodJournal]
                                 WHERE [SyncStatus] = 'NOT SYNC'
                                 AND [IsHidden] = 0";
                if (documentReferences.Count() > 0)
                {
                    string documentReferences_ = string.Join(",", documentReferences);
                    query += $@"AND [DocumentReference] IN ({documentReferences_})";
                }

                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AddToRecordStage(syncRecordStages, reader);
                        journalIds.Add(int.Parse(reader["VesselGoodJournalId"].ToString()));
                    }
                }
            }
            if(syncRecordStages.Count > 0)
            {
                string journalIds_ = string.Join(",", journalIds);
                MigratingDataTransactions(syncRecordStages, journalIds);
            }
        }

        public VesselGoodJournal GetVesselGoodJournal(string vesselGoodJournalId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [VesselGoodJournalId] ,[DocumentReference] ,[DocumentType] ,[ItemId] ,[ItemGroupId]
                            ,[ItemDimensionNumber] ,[ItemName] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Uom] ,[Qty] ,[InOut] 
                            ,[ShipId] ,[ShipName] ,[VesselGoodJournalDate] ,[SyncStatus] ,[IsHidden]
                            ,[Version] ,[ClientId]
                     FROM [dbo].[Out_VesselGoodJournal] WHERE [VesselGoodJournalId] = @VesselGoodJournalId AND [Version] = @Version";
                return connection.Query<VesselGoodJournal>(query, 
                    new {
                        VesselGoodJournalId = vesselGoodJournalId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> dxSyncRecordStages, IEnumerable<int> journalIds)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                CreateSyncOutStaging(dxSyncRecordStages);
                CopyVesselGoodJournalItemToStaging(journalIds);
                UpdateSyncStatusToOnStaging(journalIds);
                scope.Complete();
            }
        }
        private void CopyVesselGoodJournalItemToStaging(IEnumerable<int> vesselGoodJournalIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_VesselGoodJournal]
                           ([VesselGoodJournalId] ,[ClientId] ,[Version] ,[DocumentReference]
                           ,[DocumentType] ,[ItemId] ,[ItemGroupId]
                           ,[ItemDimensionNumber] ,[ItemName] ,[BrandTypeId]
                           ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName]
                           ,[Uom] ,[Qty] ,[InOut] ,[ShipId]
                           ,[ShipName] ,[VesselGoodJournalDate] ,[SyncStatus] ,[IsHidden])
                     VALUES
                           (@VesselGoodJournalId ,@ClientId ,@Version ,@DocumentReference
                           ,@DocumentType ,@ItemId ,@ItemGroupId
                           ,@ItemDimensionNumber ,@ItemName ,@BrandTypeId
                           ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName 
                           ,@Uom ,@Qty ,@InOut ,@ShipId
                           ,@ShipName ,@VesselGoodJournalDate ,@SyncStatus ,@IsHidden)";


                connection.Execute(query, GetVesselGoodJournals(vesselGoodJournalIds));
            }
        }

        public IEnumerable<VesselGoodJournal> GetVesselGoodJournals(IEnumerable<int> vesselGoodJournalIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string journalIds_ = string.Join(",", vesselGoodJournalIds);
                string query = $@"SELECT *, {EnvClass.Client.ClientId} AS ClientId, {1} AS Version
                                 FROM [dbo].[VesselGoodJournal] 
                                 WHERE [VesselGoodJournalId] IN ({journalIds_})";
                return connection.Query<VesselGoodJournal>(query).ToList();
            }

        }

        private void UpdateSyncStatusToOnStaging(IEnumerable<int> journalIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {

                connection.Open();
                string journalIds_ = string.Join(",", journalIds);
                string updateGJQuery = $@"UPDATE [dbo].[VesselGoodJournal]
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [VesselGoodJournalId] IN ({journalIds_ })";
                connection.Execute(updateGJQuery);
            }
        }

        private static void AddToRecordStage(IList<DxSyncOutRecordStage> dxSyncRecordStages, IDataReader reader)
        {
            var vesselGoodJournalId = reader["VesselGoodJournalId"].ToString();
            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = EnvClass.HelperValue.Root;

            dxSyncRecordStages.Add(new DxSyncOutRecordStage
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
