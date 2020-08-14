using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules.RequestFormModule.Repository
{
    public class RequestFormRepository
    {
        public RequestFormRepository() { }

        private IEnumerable<int> GetRequestFormIds()
        {
            using(IDbConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                string query = @"select RequestFormId 
                                from RequestForm 
                                where SyncStatus = 'NOT SYNC' 
                                and Status ='RELEASE' 
                                and IsHidden = 0";
                return connection.Query<int>(query).ToList();
            }
        }

        private Hashtable GuidRequestFormId(IEnumerable<int> requestFormIds)
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

            if (requestFormIds.Count() <= 0) return;

            string requestFormIds_ = string.Join(",", requestFormIds);
            var now = DateTime.Now;

            var tableGuidAndRequestFormId = GuidRequestFormId(requestFormIds);

            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();

            AddRequestFormIdToSyncRecordStage(tableGuidAndRequestFormId, dxSyncRecordStages);

            string query = @"select RequestFormItemId, RequestFormId, AttachmentPath
                            from RequestFormItem
                            where RequestFormId in (" + requestFormIds_ + ") and IsHidden = 0 " +
                            "and SyncStatus = 'NOT SYNC'";

            using(IDbConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                using (IDataReader reader = command.ExecuteReader())
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
            using (IDbConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                connection.Open();
                string updateRequestFormQuery = @"update RequestForm 
                                            set SyncStatus = 'ON STAGING' 
                                            where RequestFormId in (" + requestFormIds_ + ")";
                string updateRequestFormItemQuery = @"update RequestFormItem 
                                                    set SyncStatus = 'ON STAGING' 
                                                    where RequestFormId in ("+requestFormIds_+")";
                connection.Execute(updateRequestFormQuery);
                connection.Execute(updateRequestFormItemQuery);
            }
        }

        private void AddToStaging(IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            using(IDbConnection connection = DbConnectionFactory.SyncVesselInventoryDB())
            {
                connection.Open();
                string query = @"insert into SyncOutRecordStage 
                                (RecordStageId, RecordStageParentId, ReferenceId, 
                                ClientId, StatusStage, EntityName, IsFile, 
                                Filename, LastSyncAt) values 
                                (@RecordStageId,@RecordStageParentId,@ReferenceId,
                                @ClientId,@StatusStage,@Entityname,@IsFile,
                                @Filename,@LastSyncAt)";
                connection.Execute(query, dxSyncRecordStages);
            }
        }

        public IEnumerable<DxSyncRecordStage> GetSyncRecordStagesRequestForm()
        {
            using (IDbConnection connection = DbConnectionFactory.SyncVesselInventoryDB())
            {
                string query = @"select RecordStageId, RecordStageParentId,
                                ReferenceId, ClientId,StatusStage, 
                                EntityName,IsFile,Filename
                                from SyncOutRecordStage where EntityName in('RequestForm','RequestFormItem')";
                return connection.Query<DxSyncRecordStage>(query).ToList();
            }
        }

        public RequestForm GetRequestFormData(string requestFormId)
        {
            using (IDbConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                string query = @"select RequestFormId, RequestFormNumber, ProjectNumber,
                                DepartmentName, TargetDeliveryDate, Status, ShipId,
                                ShipName, Notes, CreatedDate, CreatedBy, LastModifiedDate,
                                LastModifiedBy, SyncStatus, IsHidden
                                from RequestForm where RequestFormId = @RequestFormId";
                return connection.Query<RequestForm>(query, new { requestFormId }).SingleOrDefault();
            }
        }

        public RequestFormItem GetRequestFormItemData(string requestFormItemId)
        {
            using (IDbConnection connection = DbConnectionFactory.VesselInventoryDB())
            {
                string query = @"select RequestFormItemId, RequestFormId, ItemId, ItemName,
                                ItemGroupId, ItemDimensionNumber, BrandTypeId, BrandTypeName,
                                ColorSizeId, ColorSizeName, Qty, Uom, Priority, Reason, Remarks,
                                AttachmentPath, ItemStatus, LastRequestQty, LastRequestDate, 
                                LastSupplyQty, LastSupplyDate,Rob, SyncStatus, CreatedDate,
                                CreatedBy,LastModifiedDate, LastModifiedBy, IsHidden
                                from RequestFormItem 
                                where RequestFormItemId = @RequestFormItemId";
                return connection.Query<RequestFormItem>(query, new { requestFormItemId }).SingleOrDefault();
            }
        }

    }
}
