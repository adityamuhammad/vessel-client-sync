using Dapper;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DxSyncClient.VesselInventory.Repository
{
    public class SyncRecordStageRepository
    {
        protected void InsertToStaging(IList<DxSyncRecordStage> syncRecordStages)
        {
            using(IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                connection.Open();
                string query = 
                    @"INSERT INTO [dbo].[SyncOutRecordStage]
                        ([RecordStageId] ,[RecordStageParentId] ,[ReferenceId] 
                        ,[ClientId] ,[StatusStage] ,[EntityName] ,[IsFile]
                        ,[Filename] ,[LastSyncAt]) 
                      VALUES 
                        (@RecordStageId ,@RecordStageParentId ,@ReferenceId
                        ,@ClientId ,@StatusStage ,@Entityname ,@IsFile
                        ,@Filename,@LastSyncAt)";
                connection.Execute(query, syncRecordStages);
            }
        }

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStages<THeader, TDetail>(string statusStage) 
            where THeader: class 
            where TDetail: class
        {
            using (IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = 
                    @"SELECT [RecordStageId], [RecordStageParentId], [ReferenceId] ,[ClientId] 
                            ,[StatusStage] ,[EntityName] ,[IsFile] ,[Filename]
                      FROM [dbo].[SyncOutRecordStage]
                      WHERE [EntityName] IN
                            ('" + typeof(THeader).Name + "','" + 
                                  typeof(TDetail).Name +  "')" +
                      "AND [StatusStage] = @StatusStage";
                return connection.Query<DxSyncRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStages<TData>(string statusStage)
        {
            using (IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = 
                    @"SELECT [RecordStageId], [RecordStageParentId], [ReferenceId] ,[ClientId] 
                            ,[StatusStage] ,[EntityName] ,[IsFile] ,[Filename]
                      FROM [dbo].[SyncOutRecordStage]
                      WHERE [EntityName] IN ('" + typeof(TData).Name + "')" +
                      "AND [StatusStage] = @StatusStage";
                return connection.Query<DxSyncRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }

        public void UpdateSync(string recordStageId, string statusStage)
        {
            using(IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
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
        protected Hashtable GuidPair(IEnumerable<int> ids)
        {
            Hashtable hashtable = new Hashtable();
            foreach(var id in ids)
                hashtable.Add(id, Guid.NewGuid());
            return hashtable;
        }

    }
}
