using System.Collections.Generic;
using DxSync.FxLib;

namespace DxSyncClient.Contract.Interfaces
{
    public interface ISyncRecordStageRepository
    {
        IEnumerable<DxSyncOutRecordStage> GetStagingSyncOut<TData>(string statusStage);
        IEnumerable<DxSyncInRecordStage> GetStagingSyncIn<TData>(string statusStage);
        IEnumerable<DxSyncOutRecordStage> GetStagingSyncOut<THeader, TDetail>(string statusStage)
            where THeader : class
            where TDetail : class;
        void UpdateStagingSyncOut(string recordStageId, string statusStage);
        void UpdateStagingSyncIn(string recordStageId, string statusStage);
    }
}