using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Transactions;
using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Setup;

namespace DxSyncClient.VesselInventory.Repository
{
    public class RequestFormRepository : SyncRecordStageRepository
    {
        public RequestFormRepository() { }

        public void TransferFromMainToStaging()
        {
            var requestFormIds = GetRequestFormIds();

            if (requestFormIds.Count() == 0) return;

            IList<DxSyncOutRecordStage> syncOutRecordStages = new List<DxSyncOutRecordStage>();

            AddRequestFormIdToSyncOutStaging(syncOutRecordStages,requestFormIds);

            AddRequestFormItemIdToSyncOutStaging(syncOutRecordStages, requestFormIds);

            MigratingDataTransactions(syncOutRecordStages, requestFormIds);
        }

        public RequestForm GetRequestForm(string requestFormId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query = 
                    @"SELECT [RequestFormId] ,[RequestFormNumber] ,[ProjectNumber] ,[DepartmentName]
                            ,[TargetDeliveryDate] ,[Status] ,[ShipId] ,[ShipName] ,[Notes] 
                            ,[CreatedDate] ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] 
                            ,[SyncStatus] ,[IsHidden] ,[ClientId] ,[Version]
                      FROM [dbo].[Out_RequestForm] WHERE [RequestFormId] = @RequestFormId AND [Version] = @Version";
                return connection.Query<RequestForm>(query, 
                    new {
                        RequestFormId = requestFormId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        public RequestFormItem GetRequestFormItem(string requestFormItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [RequestFormItemId] ,[RequestFormId] ,[ItemId] ,[ItemName]
                            ,[ItemGroupId] ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom] ,[Priority] ,[Reason] ,[Remarks]
                            ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty] ,[LastRequestDate]
                            ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden], [ClientId] ,[Version]
                      FROM [dbo].[Out_RequestFormItem] WHERE [RequestFormItemId] = @RequestFormItemId AND [Version] = @Version";
                return connection.Query<RequestFormItem>(query, 
                    new {
                        RequestFormItemId = requestFormItemId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        private void CreateItemReferenceDataSyncIn(RequestFormItem requestFormItems)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[In_RequestFormItem]
                       ([RequestFormItemId] ,[ClientId] ,[Version] ,[RequestFormId] ,[ItemId] ,[ItemName] ,[ItemGroupId] ,[ItemDimensionNumber]
                       ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom]
                       ,[Priority] ,[Reason] ,[Remarks] ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty]
                       ,[LastRequestDate] ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden])
                     VALUES 
                       (@RequestFormItemId ,@ClientId ,@Version ,@RequestFormId ,@ItemId ,@ItemName ,@ItemGroupId ,@ItemDimensionNumber
                       ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Uom ,@Priority
                       ,@Reason ,@Remarks ,@AttachmentPath ,@ItemStatus ,@LastRequestQty ,@LastRequestDate
                       ,@LastSupplyQty ,@LastSupplyDate ,@Rob ,@SyncStatus ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                       ,@LastModifiedBy ,@IsHidden)";

                connection.Execute(query, requestFormItems);
            }
        }
        
        public void CreateItemSyncIn(DxSyncInRecordStage recordStages, RequestFormItem requestFormItems)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    CreateStagingSyncIn(recordStages);
                    CreateItemReferenceDataSyncIn(requestFormItems);
                    scope.Complete();

                }
                catch (Exception) { }

            }
        }

        private static void AddRequestFormItemIdToSyncOutStaging(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> requestFormIds)
        {
            string requestFormIds_ = string.Join(",", requestFormIds);

            string query = $@"SELECT [RequestFormItemId] ,[RequestFormId] ,[AttachmentPath]
                            FROM [dbo].[RequestFormItem]
                            WHERE [RequestFormId] IN ({requestFormIds_})
                            AND [IsHidden] = 0
                            AND [SyncStatus] = 'NOT SYNC'";

            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
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

                        var parent = syncOutRecordStages.Where(x => x.ReferenceId == requestFormId).SingleOrDefault();

                        var recordStageId = Guid.NewGuid().ToString();
                        var recordStageParentId = parent.RecordStageId;

                        parent.DataCount += 1;
                        AddItemToStaging(syncOutRecordStages, requestFormItemId, recordStageId, recordStageParentId);

                        if (!string.IsNullOrEmpty(attachment))
                        {
                            parent.DataCount += 1;
                            AddFileToStaging(syncOutRecordStages, requestFormItemId, attachment, recordStageId);
                        }
                    }
                }
            }
        }

        private void CopyRequestFormToStaging()
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_RequestForm]
                        ([RequestFormId], [ClientId], [Version] ,[RequestFormNumber] ,[ProjectNumber] ,[DepartmentName] 
                        ,[TargetDeliveryDate] ,[Status] ,[ShipId] ,[ShipName] ,[Notes] ,[CreatedDate] ,[CreatedBy] 
                        ,[LastModifiedDate] ,[LastModifiedBy] ,[SyncStatus] ,[IsHidden]) 
                     VALUES 
                        (@RequestFormId ,@ClientId, @Version ,@RequestFormNumber ,@ProjectNumber ,@DepartmentName
                        ,@TargetDeliveryDate ,@Status ,@ShipId ,@ShipName ,@Notes ,@CreatedDate ,@CreatedBy
                        ,@LastModifiedDate ,@LastModifiedBy ,@SyncStatus ,@IsHidden)";
                connection.Execute(query, GetRequestForms());
            }
        }

        private void CopyRequestFormItemToStaging(IEnumerable<int> requestFormIds)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[Out_RequestFormItem]
                       ([RequestFormItemId] ,[ClientId] ,[Version] ,[RequestFormId] ,[ItemId] ,[ItemName] ,[ItemGroupId] ,[ItemDimensionNumber]
                       ,[BrandTypeId] ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom]
                       ,[Priority] ,[Reason] ,[Remarks] ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty]
                       ,[LastRequestDate] ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden])
                     VALUES 
                       (@RequestFormItemId ,@ClientId ,@Version ,@RequestFormId ,@ItemId ,@ItemName ,@ItemGroupId ,@ItemDimensionNumber
                       ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Uom ,@Priority
                       ,@Reason ,@Remarks ,@AttachmentPath ,@ItemStatus ,@LastRequestQty ,@LastRequestDate
                       ,@LastSupplyQty ,@LastSupplyDate ,@Rob ,@SyncStatus ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                       ,@LastModifiedBy ,@IsHidden)";

                connection.Execute(query, GetRequestFormItems(requestFormIds));
            }
        }

        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> requestFormIds)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                CreateStagingSyncOut(syncOutRecordStages);
                CopyRequestFormToStaging();
                CopyRequestFormItemToStaging(requestFormIds);
                UpdateSyncStatusToOnStaging(requestFormIds);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(IEnumerable<int> requestFormIds)
        {
            string requestFormIds_ = string.Join(",", requestFormIds);
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                connection.Open();
                string updateRF =
                    $@"UPDATE [dbo].[RequestForm] SET [SyncStatus] = 'ON STAGING' 
                      WHERE [RequestFormId] IN ({requestFormIds_ })";
                string updateRFItem =
                    $@"UPDATE [dbo].[RequestFormItem] SET [SyncStatus] = 'ON STAGING' 
                      WHERE [RequestFormId] IN ({requestFormIds_})";
                connection.Execute(updateRF);
                connection.Execute(updateRFItem);
            }
        }

        private IEnumerable<RequestForm> GetRequestForms()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = 
                    $@"SELECT *, {SetupEnvironment.Client.ClientId} as ClientId, {1} as Version FROM [dbo].[RequestForm]
                      WHERE [SyncStatus] = 'NOT SYNC' AND [Status] = 'RELEASE' and [IsHidden] = 0";
                return connection.Query<RequestForm>(query).ToList();
            }
        }

        private IEnumerable<RequestFormItem> GetRequestFormItems(IEnumerable<int> requestFormIds)
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string requestFormIds_ = string.Join(",", requestFormIds);

                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} as ClientId, {1} as Version
                                FROM [dbo].[RequestFormItem]
                                WHERE [RequestFormId] IN ({requestFormIds_})
                                AND [IsHidden] = 0
                                AND [SyncStatus] = 'NOT SYNC'";
                return connection.Query<RequestFormItem>(query).ToList();
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

        private static void AddItemToStaging(IList<DxSyncOutRecordStage> syncRecordStages, string requestFormItemId, string recordStageId, string recordStageParentId)
        {
            syncRecordStages.Add(new DxSyncOutRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = requestFormItemId,
                ClientId = SetupEnvironment.Client.ClientId,
                EntityName = typeof(RequestFormItem).Name,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private static void AddFileToStaging(IList<DxSyncOutRecordStage> syncRecordStages, string requestFormItemId, string attachment, string recordStageId)
        {
            syncRecordStages.Add(new DxSyncOutRecordStage
            {
                RecordStageId = Guid.NewGuid().ToString(),
                RecordStageParentId = recordStageId,
                ReferenceId = requestFormItemId,
                EntityName = typeof(RequestFormItem).Name,
                ClientId = SetupEnvironment.Client.ClientId,
                IsFile = true,
                Filename = attachment,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private void AddRequestFormIdToSyncOutStaging(IList<DxSyncOutRecordStage> dxSyncRecordStages, IEnumerable<int> requestFormIds)
        {
            foreach(var requestFormId in requestFormIds)
            {
                dxSyncRecordStages.Add(new DxSyncOutRecordStage
                {
                    RecordStageId = Guid.NewGuid().ToString(),
                    RecordStageParentId = SetupEnvironment.HelperValue.Root,
                    ReferenceId = requestFormId.ToString(),
                    ClientId = SetupEnvironment.Client.ClientId,
                    EntityName = typeof(RequestForm).Name,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
    }
}
