using Dapper;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Transactions;

namespace DxSyncClient.ServiceImpl.VesselInventory.Module.RequestForm.Repository
{
    public class RequestFormRepository
    {
        public RequestFormRepository() { }
        private string _QueryGetRequestFormItemIdIn(string requestFormIds)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select RequestFormItemId, RequestFormId, AttachmentPath ");
            stringBuilder.Append("from RequestFormItem ");
            stringBuilder.Append("where RequestFormId in (" + requestFormIds + ") ");
            stringBuilder.Append("and IsHidden = 0");
            stringBuilder.Append("and SyncStatus = 'NOT SYNC'");
            return stringBuilder.ToString();
        }

        private IList<int> GetRequestFormIds()
        {
            IList<int> requestFormIds = new List<int>();
            using(SqlConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select RequestFormId from RequestForm where SyncStatus = 'NOT SYNC' and Status ='RELEASE' and IsHidden = 0";
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        requestFormIds.Add(int.Parse(reader["RequestFormId"].ToString()));
            }
            return requestFormIds;
        }

        private Hashtable GuidRequestFormId(IList<int> requestFormIds)
        {
            Hashtable hashtable = new Hashtable();
            foreach(var requestFormId in requestFormIds)
                hashtable.Add(requestFormId, Guid.NewGuid());
            return hashtable;
        }
        private void AddRequestFormIdToSyncRecordStage(Hashtable guidRequestFormIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(DictionaryEntry guidRequestFormId in guidRequestFormIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = guidRequestFormId.Value.ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = guidRequestFormId.Key.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = EnvClass.EntityName.RequestForm,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }

        public void GenerateData()
        {
            var requestFormIds = GetRequestFormIds();
            if (requestFormIds.Count <= 0) return;

            string requestFormIds_ = string.Join(",", requestFormIds);
            var now = DateTime.Now;

            var tableGuidAndRequestFormId = GuidRequestFormId(requestFormIds);

            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();

            AddRequestFormIdToSyncRecordStage(tableGuidAndRequestFormId, dxSyncRecordStages);


            using(SqlConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = _QueryGetRequestFormItemIdIn(requestFormIds_);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var requestFormItemId = reader["RequestFormItemId"].ToString();
                        var recordStageId = Guid.NewGuid().ToString();
                        var recordStageParentId = tableGuidAndRequestFormId[int.Parse(reader["RequestFormId"].ToString())].ToString();
                        string attachment = reader["AttachmentPath"].ToString();

                        dxSyncRecordStages.Add(new DxSyncRecordStage
                        {
                            RecordStageId = recordStageId,
                            RecordStageParentId = recordStageParentId,
                            ReferenceId = requestFormItemId,
                            ClientId = EnvClass.Client.ClientId,
                            EntityName = EnvClass.EntityName.RequestFormItem,
                            IsFile = false,
                            StatusStage = DxSyncStatusStage.UN_SYNC,
                            LastSyncAt = now
                        });

                        if (!string.IsNullOrEmpty(attachment))
                        {
                            dxSyncRecordStages.Add(new DxSyncRecordStage
                            {
                                RecordStageId = Guid.NewGuid().ToString(),
                                RecordStageParentId = recordStageId,
                                ReferenceId = requestFormItemId,
                                EntityName = EnvClass.EntityName.RequestFormItem,
                                ClientId = EnvClass.Client.ClientId,
                                IsFile = true,
                                Filename = attachment,
                                StatusStage = DxSyncStatusStage.UN_SYNC,
                                LastSyncAt = now
                            });
                        }
                    }
                }
            }
            StageTransactions(dxSyncRecordStages, requestFormIds_);
        }

        public void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string requestFormIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                AddToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(requestFormIds_);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(string requestFormIds_)
        {
            using (SqlConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "update RequestForm set SyncStatus = 'ON STAGING' where RequestFormId in ("+requestFormIds_+")";
                command.ExecuteNonQuery();
                command.CommandText = "update RequestFormItem set SyncStatus = 'ON STAGING' where RequestFormId in ("+requestFormIds_+")";
                command.ExecuteNonQuery();
            }
        }

        private void AddToStaging(IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            using(SqlConnection connection = DbConnectionFactory.SyncVesselInventoryDB())
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.Connection = connection;

                command.CommandText = "insert into SyncOutRecordStage (RecordStageId, RecordStageParentId, ReferenceId, ClientId, StatusStage, EntityName, IsFile, Filename, LastSyncAt) values (@RecordStageId,@RecordStageParentId,@ReferenceId,@ClientId,@StatusStage,@Entityname,@IsFile,@Filename,@LastSyncAt)";

                command.Parameters.Add(new SqlParameter("@RecordStageId", SqlDbType.NChar));
                command.Parameters.Add(new SqlParameter("@RecordStageParentId", SqlDbType.NChar));
                command.Parameters.Add(new SqlParameter("@ReferenceId", SqlDbType.NChar));
                command.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Int));
                command.Parameters.Add(new SqlParameter("@StatusStage", SqlDbType.NChar));
                command.Parameters.Add(new SqlParameter("@EntityName", SqlDbType.NChar));
                command.Parameters.Add(new SqlParameter("@IsFile", SqlDbType.Bit));
                command.Parameters.Add(new SqlParameter("@Filename", SqlDbType.NVarChar));
                command.Parameters.Add(new SqlParameter("@LastSyncAt", SqlDbType.DateTime));
                foreach(var record in dxSyncRecordStages)
                {
                    command.Parameters["@RecordStageId"].Value = record.RecordStageId;
                    command.Parameters["@RecordStageParentId"].Value = record.RecordStageParentId;
                    command.Parameters["@ReferenceId"].Value = (object)record.ReferenceId ?? DBNull.Value;
                    command.Parameters["@ClientId"].Value = record.ClientId;
                    command.Parameters["@StatusStage"].Value = record.StatusStage;
                    command.Parameters["@EntityName"].Value = (object)record.EntityName ?? DBNull.Value;
                    command.Parameters["@IsFile"].Value = record.IsFile;
                    command.Parameters["@Filename"].Value = (object)record.Filename ?? DBNull.Value;
                    command.Parameters["@LastSyncAt"].Value = record.LastSyncAt;
                    command.ExecuteNonQuery();
                }

            }
        }

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStagesRequestForm()
        {
            using (SqlConnection connection = DbConnectionFactory.SyncVesselInventoryDB())
            {
                string query = @"select RecordStageId, RecordStageParentId,
                                ReferenceId, ClientId,StatusStage, 
                                EntityName,IsFile,Filename
                                from SyncOutRecordStage where EntityName in('RequestForm','RequestFormItem')";
                return connection.Query<DxSyncRecordStage>(query).ToList();
            }
        }
    }
}
