namespace BackgroundWorker.Utils
{
    public class BackgroundWorkerSettings(IConfiguration configuration)
    {
        public string AllowedProductionOrigins => configuration.GetValue<string>(nameof(AllowedProductionOrigins)) ?? throw new ArgumentNullException(nameof(AllowedProductionOrigins));
        public int NumberOfHandlers => configuration.GetValue<int?>(nameof(NumberOfHandlers)) ?? throw new ArgumentNullException(nameof(NumberOfHandlers));
        /// <summary>
        /// TODO: use more secure. See for details: https://learn.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data?tabs=using-primary-key
        /// </summary>
        public string HistoryConnectionString => configuration.GetValue<string>(nameof(HistoryConnectionString)) ?? throw new ArgumentNullException(nameof(HistoryConnectionString));
        public string BlobConnectionString => configuration.GetValue<string>(nameof(BlobConnectionString)) ?? throw new ArgumentNullException(nameof(BlobConnectionString));
        public string BlobContainerName => configuration.GetValue<string>(nameof(BlobContainerName)) ?? throw new ArgumentNullException(nameof(BlobContainerName));
        public string BlobCacheMetadataKey => configuration.GetValue<string>(nameof(BlobCacheMetadataKey)) ?? throw new ArgumentNullException(nameof(BlobCacheMetadataKey));
        public int BlobCacheValidDays => configuration.GetValue<int?>(nameof(BlobCacheValidDays)) ?? throw new ArgumentNullException(nameof(BlobCacheValidDays));
    }
}
