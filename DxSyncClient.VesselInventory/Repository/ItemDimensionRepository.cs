using Dapper;
using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Setup;
using System;
using System.Data;
using System.Transactions;

namespace DxSyncClient.VesselInventory.Repository
{
    public class ItemDimensionRepository : SyncRecordStageRepository
    {
        private void CreateItemReferenceDataSyncIn(ItemDimension itemDimension)
        {
            using (IDbConnection connection = DbConnectionFactory.GetConnection(DbConnectionFactory.DBConnectionString.DBSyncClientVesselInventory))
            {
                connection.Open();
                var query =
                    @"INSERT INTO [dbo].[In_ItemDimension]
                       ([ItemDimensionId] ,[ItemDimensionNumber] ,[ItemId] ,[BrandTypeId]
                       ,[BrandTypeName] ,[ColorSizeId] ,[ColorSizeName]
                       ,[SyncStatus] ,[Version] ,[ClientId])
                     VALUES
                       (@ItemDimensionId ,@ItemDimensionNumber ,@ItemId ,@BrandTypeId
                       ,@BrandTypeName ,@ColorSizeId ,@ColorSizeName
                       ,@SyncStatus ,@Version ,@ClientId)";

                connection.Execute(query, itemDimension);
            }
        }

        public void CreateItemSyncIn(DxSyncInRecordStage recordStages, ItemDimension itemDimension)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    CreateStagingSyncIn(recordStages);
                    CreateItemReferenceDataSyncIn(itemDimension);
                    scope.Complete();

                } catch (Exception) { }
                
            }
        }
    }
}
