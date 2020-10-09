using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class ItemDimensionSync : AbstractBaseSynchronization
    {
        private readonly ItemDimensionRepository _ItemDimensionRepository;
        public ItemDimensionSync() : base(new SyncRecordStageRepository())
        {
            _ItemDimensionRepository = RepositoryFactory.ItemDimensionRepository;
        }
        public void SyncIn()
        {
            SyncIn<ItemDimension>();
        }
        public void SyncInConfirmation()
        {
            SyncInConfirmation<ItemDimension>();
        }

        public void TransferFromStagingToMain()
        {
            _ItemDimensionRepository.TransferFromStagingToMain();
        }

        public void SyncInComplete()
        {
            SyncInComplete<ItemDimension>();
        }
        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            _ItemDimensionRepository.CreateItemSyncIn(syncInRecordStage, (ItemDimension)referenceData);
        }

        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            throw new System.NotImplementedException();
        }
    }
}
