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

        public void TransferFromStagingToMain()
        {
            var stagingSyncIn = GetStagingSyncIn<RequestFormItem>(DxSyncStatusStage.SYNC_COMPLETE);
            if (stagingSyncIn.Count() > 0)
            {
                using (var scope = new TransactionScope())
                {
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
                    {
                        connection.Open();
                        foreach (var staging in stagingSyncIn)
                        {
                            var refData = GetRequestFormItemIn(staging.ReferenceId, staging.Version);
                            string query = @"UPDATE [dbo].[RequestFormItem]
                                             SET [ApprovedQty] = @ApprovedQty
                                             ,[ItemStatus] = @ItemStatus
                                             ,[IsDocumentPending] = @IsDocumentPending
                                             WHERE [RequestFormItemId] = @RequestFormItemId";
                            connection.Execute(query, refData);
                        }
                    }
                    using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
                    {
                        connection.Open();
                        var syncIds = string.Join(",",(from staging in stagingSyncIn select $"'{staging.RecordStageId}'"));
                        string query = $@"UPDATE [dbo].[SyncInRecordStage]
                                         SET [StatusStage] = 'ON MAIN' 
                                         WHERE [RecordStageId] IN ({syncIds})";
                        connection.Execute(query);
                    }
                    scope.Complete();
                }
            }

        }

        public RequestForm GetRequestFormOut(string requestFormId, int version)
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
                    }).FirstOrDefault();
            }
        }

        public RequestFormItem GetRequestFormItemOut(string requestFormItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [RequestFormItemId] ,[RequestFormId] ,[ItemId] ,[ItemName]
                            ,[ItemGroupId] ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom] ,[Priority] ,[Reason] ,[Remarks]
                            ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty] ,[LastRequestDate]
                            ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate] ,[ApprovedQty] ,[LastModifiedBy] 
                            ,[IsHidden], [ClientId] ,[Version], [IsDocumentPending]
                      FROM [dbo].[Out_RequestFormItem] WHERE [RequestFormItemId] = @RequestFormItemId AND [Version] = @Version";
                return connection.Query<RequestFormItem>(query, 
                    new {
                        RequestFormItemId = requestFormItemId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        public RequestFormItem GetRequestFormItemIn(string requestFormItemId, int version)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                string query =
                    @"SELECT [RequestFormItemId] ,[RequestFormId] ,[ItemId] ,[ItemName]
                            ,[ItemGroupId] ,[ItemDimensionNumber] ,[BrandTypeId] ,[BrandTypeName]
                            ,[ColorSizeId] ,[ColorSizeName] ,[Qty] ,[Uom] ,[Priority] ,[Reason] ,[Remarks]
                            ,[AttachmentPath] ,[ItemStatus] ,[LastRequestQty] ,[LastRequestDate]
                            ,[LastSupplyQty] ,[LastSupplyDate] ,[Rob] ,[SyncStatus] ,[CreatedDate]
                            ,[CreatedBy] ,[LastModifiedDate] ,[ApprovedQty] ,[LastModifiedBy] 
                            ,[IsHidden], [ClientId] ,[Version], [IsDocumentPending]
                      FROM [dbo].[In_RequestFormItem] WHERE [RequestFormItemId] = @RequestFormItemId AND [Version] = @Version
                      ORDER BY [Version]";
                return connection.Query<RequestFormItem>(query, 
                    new {
                        RequestFormItemId = requestFormItemId,
                        Version = version
                    }).SingleOrDefault();
            }
        }

        private void CreateRequestFormItemIn(RequestFormItem requestFormItems)
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
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden], [ApprovedQty], [IsDocumentPending])
                     VALUES 
                       (@RequestFormItemId ,@ClientId ,@Version ,@RequestFormId ,@ItemId ,@ItemName ,@ItemGroupId ,@ItemDimensionNumber
                       ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Uom ,@Priority
                       ,@Reason ,@Remarks ,@AttachmentPath ,@ItemStatus ,@LastRequestQty ,@LastRequestDate
                       ,@LastSupplyQty ,@LastSupplyDate ,@Rob ,@SyncStatus ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                       ,@LastModifiedBy ,@IsHidden, @ApprovedQty, @IsDocumentPending)";
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
                    CreateRequestFormItemIn(requestFormItems);
                    scope.Complete();

                }
                catch (Exception) { }

            }
        }


        public void ResyncRequestFormItemToSyncOutStaging()
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                var query = $@"SELECT * FROM [dbo].[RequestFormItem] WHERE [SyncStatus] = 'RE SYNC' AND [IsHidden] = 0";
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();

                IList<DxSyncOutRecordStage> syncOutRecordStages = new List<DxSyncOutRecordStage>();
                IList<int> requestFormItemIds = new List<int>();
                IList<RequestFormItem> requestFormItems = new List<RequestFormItem>();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var requestFormItemId = reader["RequestFormItemId"].ToString();
                        var attachment = reader["AttachmentPath"].ToString();

                        var version = GetMaxVersionSyncOut(requestFormItemId, typeof(RequestFormItem).Name) + 1;

                        var parent = new DxSyncOutRecordStage
                        {
                            ClientId = SetupEnvironment.Client.ClientId,
                            DataCount = 1,
                            EntityName = typeof(RequestFormItem).Name,
                            IsFile = false,
                            LastSyncAt = DateTime.Now,
                            RecordStageParentId = SetupEnvironment.HelperValue.Root,
                            RecordStageId = Guid.NewGuid().ToString(),
                            StatusStage = DxSyncStatusStage.UN_SYNC,
                            ReferenceId = requestFormItemId,
                            Version = version
                        };
                        var child = new DxSyncOutRecordStage
                        {
                            ClientId = SetupEnvironment.Client.ClientId,
                            DataCount = 0,
                            EntityName = typeof(RequestFormItem).Name,
                            IsFile = true,
                            LastSyncAt = DateTime.Now,
                            Filename = attachment,
                            RecordStageParentId = parent.RecordStageId,
                            RecordStageId = Guid.NewGuid().ToString(),
                            StatusStage = DxSyncStatusStage.UN_SYNC,
                            ReferenceId = requestFormItemId,
                            Version = version
                        };
                        var approvedQty = string.IsNullOrWhiteSpace(reader["ApprovedQty"].ToString()) ? "0" : reader["ApprovedQty"].ToString();
                        var refData = new RequestFormItem
                        {
                            ClientId = SetupEnvironment.Client.ClientId,
                            RequestFormItemId = Convert.ToInt32(requestFormItemId),
                            AttachmentPath = attachment,
                            RequestFormId = Convert.ToInt32(reader["RequestFormId"]),
                            ApprovedQty = Convert.ToDecimal(approvedQty),
                            BrandTypeId = Convert.ToString(reader["BrandTypeId"]),
                            BrandTypeName = Convert.ToString(reader["BrandTypeName"]),
                            ColorSizeId = Convert.ToString(reader["ColorSizeId"]),
                            ColorSizeName = Convert.ToString(reader["ColorSizeName"]),
                            CreatedBy = Convert.ToString(reader["CreatedBy"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                            IsHidden = Convert.ToBoolean(reader["IsHidden"]),
                            ItemDimensionNumber = Convert.ToString(reader["ItemDimensionNumber"]),
                            ItemGroupId = Convert.ToInt32(reader["ItemGroupId"]),
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemName = Convert.ToString(reader["ItemName"]),
                            ItemStatus = Convert.ToString(reader["ItemStatus"]),
                            IsDocumentPending = Convert.ToBoolean(reader["IsDocumentPending"]),
                            LastModifiedBy = Convert.ToString(reader["LastModifiedBy"]),
                            LastRequestQty = Convert.ToDecimal(reader["LastRequestQty"]),
                            LastSupplyQty = Convert.ToDecimal(reader["LastSupplyQty"]),
                            Priority = Convert.ToString(reader["Priority"]),
                            Qty = Convert.ToDecimal(reader["Qty"]),
                            Reason = Convert.ToString(reader["Reason"]),
                            Remarks = Convert.ToString(reader["Remarks"]),
                            Rob = Convert.ToDecimal(reader["Rob"]),
                            SyncStatus = Convert.ToString(reader["SyncStatus"]),
                            Uom = Convert.ToString(reader["Uom"]),
                            Version = version
                        };
                        if (reader["LastRequestDate"].ToString() != "")
                        {
                            refData.LastRequestDate = Convert.ToDateTime(reader["LastRequestDate"].ToString());
                        }
                        if (reader["LastModifiedDate"].ToString() != "")
                        {
                            refData.LastModifiedDate = Convert.ToDateTime(reader["LastModifiedDate"].ToString());
                        }
                        if (reader["LastSupplyDate"].ToString() != "")
                        {
                            refData.LastSupplyDate = Convert.ToDateTime(reader["LastSupplyDate"].ToString());
                        }
                        requestFormItemIds.Add(Convert.ToInt32(requestFormItemId));
                        syncOutRecordStages.Add(parent);
                        syncOutRecordStages.Add(child);
                        requestFormItems.Add(refData);
                    }
                }
                if (requestFormItemIds.Count() > 0)
                {
                    using(TransactionScope scope = new TransactionScope())
                    {
                        CreateStagingSyncOut(syncOutRecordStages);
                        ResyncCopyRequestFormItemToStagingOut(requestFormItems);
                        using (IDbConnection connection2 = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
                        {
                            connection2.Open();
                            var requestFormItemIds_ = string.Join(",", requestFormItemIds);
                            string update = $@"UPDATE [dbo].[RequestFormItem] 
                                                SET [SyncStatus] = 'ON STAGING' 
                                                WHERE [RequestFormItemId] IN ({requestFormItemIds_})";
                            connection.Execute(update);
                        }
                        scope.Complete();
                    }
                }
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

        private void CopyRequestFormToStagingOut()
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

        private void ResyncCopyRequestFormItemToStagingOut(IList<RequestFormItem> requestFormItems)
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
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden] ,[ApprovedQty], [IsDocumentPending])
                     VALUES 
                       (@RequestFormItemId ,@ClientId ,@Version ,@RequestFormId ,@ItemId ,@ItemName ,@ItemGroupId ,@ItemDimensionNumber
                       ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Uom ,@Priority
                       ,@Reason ,@Remarks ,@AttachmentPath ,@ItemStatus ,@LastRequestQty ,@LastRequestDate
                       ,@LastSupplyQty ,@LastSupplyDate ,@Rob ,@SyncStatus ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                       ,@LastModifiedBy ,@IsHidden, @ApprovedQty, @IsDocumentPending)";
                connection.Execute(query, requestFormItems);
            }
        }
        private void CopyRequestFormItemToStagingOut(IEnumerable<int> requestFormIds)
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
                       ,[CreatedBy] ,[LastModifiedDate] ,[LastModifiedBy] ,[IsHidden] ,[ApprovedQty], [IsDocumentPending])
                     VALUES 
                       (@RequestFormItemId ,@ClientId ,@Version ,@RequestFormId ,@ItemId ,@ItemName ,@ItemGroupId ,@ItemDimensionNumber
                       ,@BrandTypeId ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName ,@Qty ,@Uom ,@Priority
                       ,@Reason ,@Remarks ,@AttachmentPath ,@ItemStatus ,@LastRequestQty ,@LastRequestDate
                       ,@LastSupplyQty ,@LastSupplyDate ,@Rob ,@SyncStatus ,@CreatedDate ,@CreatedBy ,@LastModifiedDate
                       ,@LastModifiedBy ,@IsHidden, @ApprovedQty, @IsDocumentPending)";
                connection.Execute(query, GetRequestFormItems(requestFormIds));
            }
        }

        private void MigratingDataTransactions(IList<DxSyncOutRecordStage> syncOutRecordStages, IEnumerable<int> requestFormIds)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                CreateStagingSyncOut(syncOutRecordStages);
                CopyRequestFormToStagingOut();
                CopyRequestFormItemToStagingOut(requestFormIds);
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
                string updateRF = $@"UPDATE [dbo].[RequestForm] 
                                   SET [SyncStatus] = 'ON STAGING' 
                                   WHERE [RequestFormId] IN ({requestFormIds_ })";
                string updateRFItem = $@"UPDATE [dbo].[RequestFormItem] 
                                        SET [SyncStatus] = 'ON STAGING' 
                                        WHERE [RequestFormId] IN ({requestFormIds_})";
                connection.Execute(updateRF);
                connection.Execute(updateRFItem);
            }
        }

        private IEnumerable<RequestForm> GetRequestForms()
        {
            using(IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBVesselInventory))
            {
                string query = $@"SELECT *, {SetupEnvironment.Client.ClientId} as ClientId, {1} as Version 
                                FROM [dbo].[RequestForm]
                                WHERE [SyncStatus] = 'NOT SYNC' 
                                AND [Status] = 'RELEASE' 
                                AND [IsHidden] = 0";
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
                    @"SELECT [RequestFormId] 
                      FROM [dbo].[RequestForm]
                      WHERE [SyncStatus] = 'NOT SYNC' 
                      AND [Status] = 'RELEASE' 
                      AND [IsHidden] = 0";
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
                Version = 1,
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
                Version = 1,
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
                    Version = 1,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
    }
}
