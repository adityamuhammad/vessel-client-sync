﻿
using DxSyncClient.ServiceImpl.VesselInventory.Modules;

namespace DxSyncClient.ServiceImpl.VesselInventory
{
    public class ModuleFactory
    {
        public static RequestFormSync RequestFormSync => new RequestFormSync();
    }
}