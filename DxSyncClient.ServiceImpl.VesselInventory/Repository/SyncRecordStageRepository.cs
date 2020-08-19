using Dapper;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DxSyncClient.ServiceImpl.VesselInventory.Repository
{
    public class SyncRecordStageRepository
    {
        protected void InsertToStaging(IList<DxSyncRecordStage> syncRecordStages)
        {
            using(IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                connection.Open();
                string query = @"insert into SyncOutRecordStage 
                                (RecordStageId, RecordStageParentId, ReferenceId, 
                                ClientId, StatusStage, EntityName, IsFile, 
                                Filename, LastSyncAt) values 
                                (@RecordStageId,@RecordStageParentId,@ReferenceId,
                                @ClientId,@StatusStage,@Entityname,@IsFile,
                                @Filename,@LastSyncAt)";
                connection.Execute(query, syncRecordStages);
            }
        }

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStages<T1, T2>(string statusStage) where T1: class where T2: class
        {
            using (IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = @"select RecordStageId, RecordStageParentId,
                                ReferenceId, ClientId,StatusStage, 
                                EntityName,IsFile,Filename
                                from SyncOutRecordStage 
                                where EntityName 
                                in('" +typeof(T1).Name + "','" + typeof(T2).Name +  "')" +
                                "and StatusStage = @StatusStage";
                return connection.Query<DxSyncRecordStage>(query, new { StatusStage = statusStage}).ToList();
            }
        }

        public void UpdateSync(string recordStageId, string statusStage)
        {
            using(IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = @"update SyncOutRecordStage 
                                set StatusStage = @StatusStage 
                                where RecordStageId = @RecordStageId";
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
