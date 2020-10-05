using Dapper;
using DxSync.FxLib;
using DxSyncClient.Contract.Interfaces;
using DxSyncClient.VesselInventory.Setup;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DxSyncClient.VesselInventory.Repository
{
    public class SyncRecordStageRepository : ISyncRecordStageRepository
    {
        protected void CreateStagingSyncOut(IList<DxSyncOutRecordStage> syncRecordStages)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                string query = 
                    @"INSERT INTO [dbo].[SyncOutRecordStage]
                        ([RecordStageId] ,[RecordStageParentId] ,[ReferenceId] 
                        ,[ClientId] ,[Version] ,[StatusStage] ,[EntityName] ,[IsFile]
                        ,[DataCount], [Filename] ,[LastSyncAt]) 
                      VALUES 
                        (@RecordStageId ,@RecordStageParentId ,@ReferenceId
                        ,@ClientId ,@Version ,@StatusStage ,@Entityname ,@IsFile
                        ,@DataCount, @Filename,@LastSyncAt)";
                connection.Execute(query, syncRecordStages);
            }
        }

        protected void CreateStagingSyncIn(DxSyncInRecordStage syncRecordStages)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                string query = 
                    @"INSERT INTO [dbo].[SyncInRecordStage]
                        ([RecordStageId] ,[RecordStageParentId] ,[ReferenceId] 
                        ,[ClientId] ,[Version] ,[StatusStage] ,[EntityName] ,[IsFile]
                        ,[DataCount], [Filename] ,[LastSyncAt]) 
                      VALUES 
                        (@RecordStageId ,@RecordStageParentId ,@ReferenceId
                        ,@ClientId ,@Version ,@StatusStage ,@Entityname ,@IsFile
                        ,@DataCount, @Filename,@LastSyncAt)";
                connection.Execute(query, syncRecordStages);
            }
        }

        public IEnumerable<DxSyncOutRecordStage> GetStagingSyncOut<THeader, TDetail>(string statusStage) 
            where THeader: class 
            where TDetail: class
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    $@"SELECT [RecordStageId], [RecordStageParentId], [ReferenceId] ,[ClientId] 
                            ,[StatusStage] ,[EntityName] ,[IsFile] ,[Filename], [DataCount]
                      FROM [dbo].[SyncOutRecordStage]
                      WHERE [EntityName] IN
                            ('{typeof(THeader).Name}','{typeof(TDetail).Name}')
                      AND [StatusStage] = @StatusStage";
                return connection.Query<DxSyncOutRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }

        public IEnumerable<DxSyncOutRecordStage> GetStagingSyncOut<TData>(string statusStage)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    $@"SELECT [RecordStageId], [RecordStageParentId], [ReferenceId] ,[ClientId] 
                            ,[StatusStage] ,[EntityName] ,[IsFile] ,[Filename], [DataCount]
                      FROM [dbo].[SyncOutRecordStage]
                      WHERE [EntityName] IN ('{typeof(TData).Name}')
                      AND [StatusStage] = @StatusStage";
                return connection.Query<DxSyncOutRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }
        public IEnumerable<DxSyncInRecordStage> GetStagingSyncIn<TData>(string statusStage)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    $@"SELECT [RecordStageId], [RecordStageParentId], [ReferenceId] ,[ClientId] 
                            ,[StatusStage] ,[EntityName] ,[IsFile] ,[Filename], [DataCount]
                      FROM [dbo].[SyncInRecordStage]
                      WHERE [EntityName] IN ('{typeof(TData).Name}')
                      AND [StatusStage] = @StatusStage";
                return connection.Query<DxSyncInRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }

        public void UpdateStagingSyncOut(string recordStageId, string statusStage)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    @"UPDATE [dbo].[SyncOutRecordStage] SET [StatusStage] = @StatusStage 
                      WHERE [RecordStageId] = @RecordStageId";
                connection.Open();
                connection.Execute(query, new {
                    RecordStageId = recordStageId,
                    StatusStage = statusStage
                });
            }
        }

        public void UpdateStagingSyncIn(string recordStageId, string statusStage)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    @"UPDATE [dbo].[SyncInRecordStage] SET [StatusStage] = @StatusStage 
                      WHERE [RecordStageId] = @RecordStageId";
                connection.Open();
                connection.Execute(query, new {
                    RecordStageId = recordStageId,
                    StatusStage = statusStage
                });
            }
        }
    }
}
