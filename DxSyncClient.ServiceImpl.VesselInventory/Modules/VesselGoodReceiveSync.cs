using DxSync.FxLib;
using DxSync.Entity.VesselInventory;
using DxSyncClient.ServiceImpl.VesselInventory.Repository;

namespace DxSyncClient.ServiceImpl.VesselInventory.Modules
{
    public class VesselGoodReceiveSync : AbstractModuleClientSync
    {
        private readonly VesselGoodReceiveRepository _vesselGoodReceiveRepository;
        public VesselGoodReceiveSync()
        {
            _vesselGoodReceiveRepository = RepositoryFactory.VesselGoodReceiveRepository;
        }
        public void InitializeData()
        {
             _vesselGoodReceiveRepository.InitializeData();
        }
        public void SetToken(string token)
        {
            Token = token;
        }
        public void SyncOut()
        {
            var data = _vesselGoodReceiveRepository.GetSyncRecordStages
                <VesselGoodReceive,VesselGoodReceiveItemReject>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }

        public void SyncOutConfirmation()
        {
            var data = _vesselGoodReceiveRepository.GetSyncRecordStages<VesselGoodReceive, VesselGoodReceiveItemReject>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        protected override void SetSyncProcessed(string recordStageId)
        {
            _vesselGoodReceiveRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_PROCESSED);
        }
        protected override void SetSyncComplete(string recordStageId)
        {
            _vesselGoodReceiveRepository.UpdateSync(recordStageId, DxSyncStatusStage.SYNC_COMPLETE);
        }
        protected override void SetUnSync(string recordStageId)
        {
            _vesselGoodReceiveRepository.UpdateSync(recordStageId, DxSyncStatusStage.UN_SYNC);
        }

        protected override object GetData(DxSyncRecordStage recordStage)
        {
            object data = null;
            if (recordStage.EntityName == typeof(VesselGoodReceive).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceive(recordStage.ReferenceId);
            else if (recordStage.EntityName == typeof(VesselGoodReceiveItemReject).Name)
                data = _vesselGoodReceiveRepository.GetVesselGoodReceiveItemReject(recordStage.ReferenceId);
            return data;
        }

    }
}
