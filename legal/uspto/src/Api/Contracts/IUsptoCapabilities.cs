namespace uspto;

/// <summary>
/// Provides the value domains understood by a configured USPTO client suite.
/// </summary>
/// <remarks>
/// Implementations can omit values that are unavailable because of configuration or remote-service capability.
/// </remarks>
public interface IUsptoCapabilities
{
    /// <summary>Gets conservative options suitable as a starting point for a client suite.</summary>
    /// <returns>A new options object whose mutable policy objects are not shared with other calls.</returns>
    UsptoClientOptions GetRecommendedClientOptions();

    /// <summary>Gets a conservative service-independent search request.</summary>
    /// <returns>A new search request whose mutable child objects are not shared with other calls.</returns>
    SearchRequest GetRecommendedSearchRequest();

    /// <summary>Gets empty metadata suitable for populating from a search response.</summary>
    /// <returns>A new empty response metadata object.</returns>
    SearchResponseMetadata GetEmptySearchResponseMetadata();

    /// <summary>Gets an empty result suitable for populating during a download.</summary>
    /// <returns>A new empty download result.</returns>
    DownloadResult GetEmptyDownloadResult();

    /// <summary>Gets an empty result suitable for populating while listing an archive.</summary>
    /// <returns>A new empty archive listing result.</returns>
    ArchiveListingResult GetEmptyArchiveListingResult();

    /// <summary>Gets an empty result suitable for populating during archive extraction.</summary>
    /// <returns>A new empty extraction result.</returns>
    ExtractionResult GetEmptyExtractionResult();

    /// <summary>Gets the configured service and rate-bucket kinds.</summary>
    /// <returns>The supported service kinds.</returns>
    ApiService[] GetSupportedApiServices();

    /// <summary>Gets the supported patent identifier kinds.</summary>
    /// <returns>The supported patent identifier kinds.</returns>
    IdentifierKind[] GetSupportedPatentIdentifierKinds();

    /// <summary>Gets the supported trademark identifier kinds.</summary>
    /// <returns>The supported trademark identifier kinds.</returns>
    TrademarkIdentifierKind[] GetSupportedTrademarkIdentifierKinds();

    /// <summary>Gets the supported structured-search operators.</summary>
    /// <returns>The supported search operators.</returns>
    SearchOperator[] GetSupportedSearchOperators();

    /// <summary>Gets the supported result ordering directions.</summary>
    /// <returns>The supported ordering directions.</returns>
    SortDirection[] GetSupportedSortDirections();

    /// <summary>Gets the supported search export formats.</summary>
    /// <returns>The supported export formats.</returns>
    ExportFormat[] GetSupportedExportFormats();

    /// <summary>Gets the supported document representations.</summary>
    /// <returns>The supported document formats.</returns>
    DocumentFormat[] GetSupportedDocumentFormats();

    /// <summary>Gets the supported compression and archive formats.</summary>
    /// <returns>The supported compression formats.</returns>
    CompressionFormat[] GetSupportedCompressionFormats();

    /// <summary>Gets the supported checksum algorithms.</summary>
    /// <returns>The supported checksum algorithms.</returns>
    ChecksumAlgorithm[] GetSupportedChecksumAlgorithms();

    /// <summary>Gets the supported response cache modes.</summary>
    /// <returns>The supported cache modes.</returns>
    CacheMode[] GetSupportedCacheModes();

    /// <summary>Gets the download states that can be reported.</summary>
    /// <returns>The reportable download states.</returns>
    DownloadState[] GetReportableDownloadStates();

    /// <summary>Gets the bulk dataset families understood by the suite.</summary>
    /// <returns>The supported dataset families.</returns>
    DatasetFamily[] GetSupportedDatasetFamilies();

    /// <summary>Gets the provenance kinds that can be reported.</summary>
    /// <returns>The reportable provenance kinds.</returns>
    DataProvenance[] GetReportableDataProvenance();

    /// <summary>Gets the fallback access paths understood by the suite.</summary>
    /// <returns>The supported fallback access paths.</returns>
    FallbackAccess[] GetSupportedFallbackAccessPaths();

    /// <summary>Gets the fallback authority requirements understood by the suite.</summary>
    /// <returns>The supported fallback authority requirements.</returns>
    FallbackAuthority[] GetSupportedFallbackAuthorities();

    /// <summary>Gets the mirror actions that can be planned.</summary>
    /// <returns>The supported mirror actions.</returns>
    MirrorAction[] GetSupportedMirrorActions();

    /// <summary>Gets the PTAB collections understood by the suite.</summary>
    /// <returns>The supported PTAB collections.</returns>
    PtabCollection[] GetSupportedPtabCollections();
}
