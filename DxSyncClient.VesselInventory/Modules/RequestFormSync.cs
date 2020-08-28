﻿using DxSync.Entity.VesselInventory;
using DxSync.FxLib;
using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory.Modules
{
    public class RequestFormSync : AbstractModuleClientSync
    {
        private readonly RequestFormRepository _requestFormRepository;
        public RequestFormSync()
        {
            _requestFormRepository = RepositoryFactory.RequestFormRepository;
        }

        public void InitializeData()
        {
             _requestFormRepository.InitializeData();
        }

        public void SyncOut()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.UN_SYNC);
            ProcessSyncOut(data);
        }
        public void SyncOutConfirmation()
        {
            var data = _requestFormRepository.GetSyncRecordStages
                <RequestForm,RequestFormItem>(DxSyncStatusStage.SYNC_PROCESSED);
            ProcessSyncOutConfirmation(data);
        }

        protected override object GetReferenceData(DxSyncRecordStage row)
        {
            object data = null;
            if (row.EntityName == typeof(RequestForm).Name)
                data = _requestFormRepository.GetRequestForm(row.ReferenceId);
            else if (row.EntityName == typeof(RequestFormItem).Name)
                data = _requestFormRepository.GetRequestFormItem(row.ReferenceId);
            return data;
        }

    }
}