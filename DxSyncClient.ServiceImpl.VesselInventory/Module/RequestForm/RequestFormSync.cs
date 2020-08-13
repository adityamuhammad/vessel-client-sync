using DxSync.FxLib;
using DxSyncClient.ServiceImpl.VesselInventory.Module.RequestForm.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DxSyncClient.ServiceImpl.VesselInventory.Module.RequestForm
{
    public class RequestFormSync
    {
        RequestFormRepository _requestFormRepository;
        public RequestFormSync()
        {
            _requestFormRepository = new RequestFormRepository();
        }

        public void InitializeData()
        {
             _requestFormRepository.GenerateData();
            SyncOut();
        }
        private void PrintTree(IList<DxSyncRecordStage> list,string parentId = "0", int deep = -1)
        {
            var data = list.Where(s => s.RecordStageParentId == parentId).ToList();
            if (data.Count <= 0) return;
            deep++;
            foreach (var d in data)
            {
                Console.WriteLine(new String('\t', deep) + d.EntityName + d.ReferenceId + d.Filename);
                PrintTree(list, d.RecordStageId, deep);
            }

        }

        public void APICall(IEnumerable<DxSyncRecordStage> list,string parentId = "0", int deep = -1)
        {
            var collections = list.Where(row => row.RecordStageParentId == parentId).ToList();
            if (collections.Count <= 0) return;
            deep++;
            foreach (var row in collections)
            {
                if (!row.IsFile)
                {
                    switch (row.EntityName)
                    {
                        case EnvClass.EntityName.RequestForm:
                            //call api
                            break;
                        case EnvClass.EntityName.RequestFormItem:
                            //call api
                            break;
                        default:
                            break;
                    }
                }
                Console.WriteLine(new String('\t', deep) + row.EntityName + row.ReferenceId + row.Filename);
                APICall(list, row.RecordStageId, deep);
            }

        }

        public void SyncOut(string token)
        {
            var dataSync = _requestFormRepository.GetSyncRecordStagesRequestForm();
            APICall(dataSync);
        }
    }
}
