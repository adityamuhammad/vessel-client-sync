using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Transactions;
using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;

namespace DxSyncClient.VesselInventory.Repository
{
    public class RequestFormRepository : SyncRecordStageRepository
    {
        public RequestFormRepository()
        {
            
        }

        public void InitializeData()
        {
            var requestFormIds = GetRequestFormIds();

            if (requestFormIds.Count() == 0) return;

            string requestFormIds_ = string.Join(",", requestFormIds);

            IList<DxSyncRecordStage> syncRecordStages = new List<DxSyncRecordStage>();

            AddRequestFormIdToSyncRecordStage(requestFormIds, syncRecordStages);

            string query = @"SELECT [RequestFormItemId] ,[RequestFormId] ,[AttachmentPath]
                            FROM [dbo].[RequestFormItem]
                            WHERE [RequestFormId] IN (" + requestFormIds_ + ") " +
                           "AND [IsHidden] = 0 " +
                           "AND [SyncStatus] = 'NOT SYNC'";

            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var requestFormItemId = reader["RequestFormItemId"].ToString();
                        var requestFormId = reader["RequestFormId"].ToString();
                        string attachment = reader["AttachmentPath"].ToString();

                        var parent = syncRecordStages.Where(x => x.ReferenceId == requestFormId).SingleOrDefault();

                        var recordStageId = Guid.NewGuid().ToString();
                        var recordStageParentId = parent.RecordStageId;

                        parent.DataCount += 1;
                        AddItemToStage(syncRecordStages, requestFormItemId, recordStageId, recordStageParentId);

                        if (!string.IsNullOrEmpty(attachment))
                        {
                            parent.DataCount += 1;
                            AddFileToStage(syncRecordStages, requestFormItemId, attachment, recordStageId);
                        }
                    }
                }
            }
            StageTransactions(syncRecordStages, requestFormIds_);
        }


        private void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string requestFormIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                InsertToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(requestFormIds_);
                scope.Complete();
            }
        }


        public RequestForm GetRequestForm(string requestFormId)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = 
                    @"SELECT [RequestFormId] ,[RequestFormNumber] ,[ProjectNumber] ,[DepartmentName]
                            ,[TargetDeliveryDate] ,[Status] ,[ShipId] ,[ShipName] ,[Notes] 
                            ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] 
                            ,[SyncStatus] ,[IsHidden]
                      FROM [dbo].[RequestForm] WHERE [RequestFormId] = @RequestFormId";
                return connection.Query<RequestForm>(query, new { requestFormId }).SingleOrDefault();
            }
        }

        public RequestFormItem GetRequestFormItem(string requestFormItemId)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query =
                    @"SELECT [RequestFormItemId] ,[RequestFormId] ,[ItemId] ,[ItemName]
                            ,[ItemGroupId] ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom] ,[Priority] ,[Reason] ,[Remarks]
                            ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty] ,[LastRequestDate]
                            ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden]
                      FROM [dbo].[RequestFormItem] WHERE [RequestFormItemId] = @RequestFormItemId";
                return connection.Query<RequestFormItem>(query, new { requestFormItemId }).SingleOrDefault();
            }
        }
        private void UpdateSyncStatusToOnStaging(string requestFormIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateRF = 
                    @"UPDATE [dbo].[RequestForm] SET [SyncStatus] = 'ON STAGING' 
                      WHERE [RequestFormId] IN (" + requestFormIds_ + ")";
                string updateRFItem = 
                    @"UPDATE [dbo].[RequestFormItem] SET [SyncStatus] = 'ON STAGING' 
                      WHERE [RequestFormId] IN ("+requestFormIds_+")";
                connection.Execute(updateRF);
                connection.Execute(updateRFItem);
            }
        }

        private IEnumerable<int> GetRequestFormIds()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = 
                    @"SELECT [RequestFormId] FROM [dbo].[RequestForm]
                      WHERE [SyncStatus] = 'NOT SYNC' AND [Status] = 'RELEASE' and [IsHidden] = 0";
                return connection.Query<int>(query).ToList();
            }
        }

        private static void AddItemToStage(IList<DxSyncRecordStage> syncRecordStages, string requestFormItemId, string recordStageId, string recordStageParentId)
        {
            syncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = requestFormItemId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = typeof(RequestFormItem).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private static void AddFileToStage(IList<DxSyncRecordStage> syncRecordStages, string requestFormItemId, string attachment, string recordStageId)
        {
            syncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = Guid.NewGuid().ToString(),
                RecordStageParentId = recordStageId,
                ReferenceId = requestFormItemId,
                EntityName = typeof(RequestFormItem).Name,
                ClientId = EnvClass.Client.ClientId,
                IsFile = true,
                Filename = attachment,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }
        private void AddRequestFormIdToSyncRecordStage(IEnumerable<int> requestFormIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(var requestFormId in requestFormIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = requestFormId.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = typeof(RequestForm).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }

    }
}
