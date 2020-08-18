﻿using Dapper;
using DxSync.FxLib;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DxSyncClient.ServiceImpl.VesselInventory.Repository
{
    public class SyncRecordStageRepository
    {
        protected void AddToStaging(IList<DxSyncRecordStage> syncRecordStages)
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

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStages<T1, T2>() where T1: class where T2: class
        {
            using (IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = @"select RecordStageId, RecordStageParentId,
                                ReferenceId, ClientId,StatusStage, 
                                EntityName,IsFile,Filename
                                from SyncOutRecordStage 
                                where EntityName 
                                in('" +typeof(T1).Name + "','" + typeof(T2).Name +  "')";
                return connection.Query<DxSyncRecordStage>(query).ToList();
            }
        }

        public void SetSyncProcessed(string recordStageId)
        {
            using(IDbConnection connection = DbConnectionFactory.DBSyncVesselInventory())
            {
                string query = "update SyncOutRecordStage set StatusStage = 'SYNC PROCESSED' where RecordStageId = @RecordStageId";
                connection.Open();
                connection.Execute(query, new { RecordStageId = recordStageId});
            }
        }

    }
}