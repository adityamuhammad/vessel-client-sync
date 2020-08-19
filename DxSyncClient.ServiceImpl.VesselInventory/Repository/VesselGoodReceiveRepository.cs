using Dapper;
using DxSync.FxLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;

namespace DxSyncClient.ServiceImpl.VesselInventory.Repository
{
    public class VesselGoodReceiveRepository : SyncRecordStageRepository
    {
        public void InitializeData()
        {
            var vesselGoodReceiveIds = GetVesselGoodReceiveIds();
            if (vesselGoodReceiveIds.Count() <= 0) return;

            string vesselGoodReceiveIds_ = string.Join(",", vesselGoodReceiveIds);
            var now = DateTime.Now;

            var tableGuidVesselGoodReceiveId = GuidPair(vesselGoodReceiveIds);

            IList<DxSyncRecordStage> dxSyncRecordStages = new List<DxSyncRecordStage>();
            AddVesselGoodReceiveIdToSyncRecordStage(tableGuidVesselGoodReceiveId, dxSyncRecordStages);

            string query = @"select VesselGoodReceiveItemRejectId, VesselGoodReceiveId 
                             from VesselGoodReceiveItemReject
                            where VesselGoodReceiveId in (" + vesselGoodReceiveIds_ + ") " +
                            "and SyncStatus = 'NOT SYNC' and IsHidden = 0 ";
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                IDbCommand command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AddToRecordStage(tableGuidVesselGoodReceiveId, dxSyncRecordStages, reader);
                    }
                }
            }
            StageTransactions(dxSyncRecordStages, vesselGoodReceiveIds_);

        }

        private static void AddToRecordStage(Hashtable tableGuidVesselGoodReceiveId, IList<DxSyncRecordStage> dxSyncRecordStages, IDataReader reader)
        {
            var vesselGoodReceiveItemRejectId = reader["VesselGoodReceiveItemRejectId"].ToString();
            var vesselGoodReceiveId = int.Parse(reader["VesselGoodReceiveId"].ToString());
            var recordStageId = Guid.NewGuid().ToString();
            var recordStageParentId = tableGuidVesselGoodReceiveId[vesselGoodReceiveId].ToString();

            dxSyncRecordStages.Add(new DxSyncRecordStage
            {
                RecordStageId = recordStageId,
                RecordStageParentId = recordStageParentId,
                ReferenceId = vesselGoodReceiveItemRejectId,
                ClientId = EnvClass.Client.ClientId,
                EntityName = EnvClass.EntityName.VesselGoodReceiveItem,
                IsFile = false,
                StatusStage = DxSyncStatusStage.UN_SYNC,
                LastSyncAt = DateTime.Now
            });
        }

        private void StageTransactions(IList<DxSyncRecordStage> dxSyncRecordStages, string vesselGoodReceiveIds_)
        {
            using(TransactionScope scope = new TransactionScope())
            {
                InsertToStaging(dxSyncRecordStages);
                UpdateSyncStatusToOnStaging(vesselGoodReceiveIds_);
                scope.Complete();
            }
        }

        private void UpdateSyncStatusToOnStaging(string vesselGoodReceiveIds_)
        {
            using (IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                connection.Open();
                string updateGRQuery = @"update VesselGoodReceive 
                                            set SyncStatus = 'ON STAGING' 
                                            where VesselGoodReceiveId in (" + vesselGoodReceiveIds_ + ")";
                string updateGRItemQuery = @"update VesselGoodReceiveItemReject 
                                                    set SyncStatus = 'ON STAGING' 
                                                    where VesselGoodReceiveId in ("+vesselGoodReceiveIds_+")";
                connection.Execute(updateGRQuery);
                connection.Execute(updateGRItemQuery);
            }
        }

        private void AddVesselGoodReceiveIdToSyncRecordStage(Hashtable guidVesselGoodReceiveIds, IList<DxSyncRecordStage> dxSyncRecordStages)
        {
            foreach(DictionaryEntry guidVesselGoodReceiveId in guidVesselGoodReceiveIds)
            {
                dxSyncRecordStages.Add(new DxSyncRecordStage
                {
                    RecordStageId = guidVesselGoodReceiveId.Value.ToString(),
                    RecordStageParentId = EnvClass.HelperValue.Root,
                    ReferenceId = guidVesselGoodReceiveId.Key.ToString(),
                    ClientId = EnvClass.Client.ClientId,
                    EntityName = EnvClass.EntityName.VesselGoodReceive,
                    IsFile = false,
                    LastSyncAt = DateTime.Now,
                    StatusStage = DxSyncStatusStage.UN_SYNC
                });
            }
        }
        private IEnumerable<int> GetVesselGoodReceiveIds()
        {
            using(IDbConnection connection = DbConnectionFactory.DBVesselInventory())
            {
                string query = @"select VesselGoodReceiveId 
                                from VesselGoodReceive 
                                where  CreatedDate < dateadd(hour, -1, getdate())
                                and IsHidden = 0";
                return connection.Query<int>(query).ToList();
            }

        }
    }
}
