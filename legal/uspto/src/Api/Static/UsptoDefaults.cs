namespace uspto;

/// <summary>
/// Provides conservative defaults and hard interoperability ceilings used by client policies.
/// </summary>
public static class UsptoDefaults
{
    /// <summary>The default number of records requested from offset-based APIs.</summary>
    public const int PageSize = 100;

    /// <summary>The documented maximum number of records in a PatentsView page.</summary>
    public const int PatentsViewMaximumPageSize = 1000;

    /// <summary>The default maximum number of pages processed by an aggregate request.</summary>
    public const int MaximumPages = 100;

    /// <summary>The default maximum number of records returned by an aggregate request.</summary>
    public const int MaximumResults = 10000;

    /// <summary>The default total number of HTTP attempts, including the initial request.</summary>
    public const int HttpAttemptCount = 4;

    /// <summary>The default initial retry delay in milliseconds.</summary>
    public const int InitialRetryDelayMilliseconds = 500;

    /// <summary>The default maximum retry delay in milliseconds.</summary>
    public const int MaximumRetryDelayMilliseconds = 30000;

    /// <summary>The default total retry budget in milliseconds.</summary>
    public const int RetryBudgetMilliseconds = 120000;

    /// <summary>The default request timeout in milliseconds.</summary>
    public const int RequestTimeoutMilliseconds = 100000;

    /// <summary>The default download timeout in milliseconds.</summary>
    public const int DownloadTimeoutMilliseconds = 1800000;

    /// <summary>The default streamed download buffer size in bytes.</summary>
    public const int DownloadBufferSize = 81920;

    /// <summary>The default largest buffered API response in bytes.</summary>
    public const int MaximumResponseBytes = 16777216;

    /// <summary>The default largest retained raw response in bytes.</summary>
    public const int MaximumRawResponseBytes = 1048576;

    /// <summary>The default successful-response cache lifetime in seconds.</summary>
    public const int CacheLifetimeSeconds = 300;

    /// <summary>The default maximum number of entries in the memory cache.</summary>
    public const int MemoryCacheEntryCount = 1024;

    /// <summary>The default maximum number of entries accepted from one archive.</summary>
    public const int ArchiveEntryCount = 100000;

    /// <summary>The default maximum uncompressed size of one archive entry in bytes.</summary>
    public const long ArchiveEntryBytes = 2147483648L;

    /// <summary>The default maximum total uncompressed archive size in bytes.</summary>
    public const long ArchiveTotalBytes = 4294967296L;

    /// <summary>The default maximum ratio of uncompressed to compressed archive bytes.</summary>
    public const int ArchiveCompressionRatio = 1000;

    /// <summary>The published TSDR general request allowance per minute.</summary>
    public const int TsdrGeneralRequestsPerMinute = 60;

    /// <summary>The published TSDR PDF and ZIP request allowance per minute.</summary>
    public const int TsdrPdfAndZipRequestsPerMinute = 4;

    /// <summary>The published PatentsView request allowance per minute.</summary>
    public const int PatentsViewRequestsPerMinute = 45;

    /// <summary>The default maximum number of redirect hops.</summary>
    public const int RedirectHopCount = 5;
}
