using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory;
using DxSyncClient.VesselInventory.Abstractions;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class ItemSync : AbstractBaseSynchronization
    {
        private readonly ItemRepository _itemRepository;
        public ItemSync() : base(new SyncRecordStageRepository())
        {
            _itemRepository = RepositoryFactory.ItemRepository;
        }
        public void TransferFromStagingToMain()
        {
            _itemRepository.TransferFromStagingToMain();
        }

        public void SyncIn()
        {
            SyncIn<Item>();
        }
        public void SyncInConfirmation()
        {
            SyncInConfirmation<Item>();
        }

        public void SyncInComplete()
        {
            SyncInComplete<Item>();
        }
        protected override void CreateRowTransaction(DxSyncInRecordStage syncInRecordStage, object referenceData)
        {
            _itemRepository.CreateItemSyncIn(syncInRecordStage, (Item)referenceData);
        }

        protected override object GetReferenceDataSyncOut(DxSyncOutRecordStage syncRecordStage)
        {
            throw new System.NotImplementedException();
        }
    }
}
