﻿using DxSyncClient.VesselInventory.Repository;

namespace DxSyncClient.VesselInventory
{
    public class RepositoryFactory
    {
        public static RequestFormRepository RequestFormRepository => new RequestFormRepository();
        public static VesselGoodReceiveRepository VesselGoodReceiveRepository => new VesselGoodReceiveRepository();
        public static VesselGoodIssuedRepository VesselGoodIssuedRepository => new VesselGoodIssuedRepository();
        public static VesselGoodReturnRepository VesselGoodReturnRepository => new VesselGoodReturnRepository();
        public static VesselGoodJournalRepository VesselGoodJournalRepository => new VesselGoodJournalRepository();

    }
}
