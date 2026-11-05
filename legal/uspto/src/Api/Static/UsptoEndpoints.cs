namespace uspto;

/// <summary>
/// Provides the default HTTPS endpoint roots used by USPTO data services.
/// </summary>
public static class UsptoEndpoints
{
    /// <summary>The USPTO Open Data Portal origin.</summary>
    public const string OdpBaseUri = "https://api.uspto.gov/";

    /// <summary>The version-one USPTO Open Data Portal API root.</summary>
    public const string OdpApiV1BaseUri = "https://api.uspto.gov/api/v1/";

    /// <summary>The Trademark Status and Document Retrieval API origin.</summary>
    public const string TsdrBaseUri = "https://tsdrapi.uspto.gov/";

    /// <summary>The configurable default origin for the PatentsView PatentSearch API.</summary>
    public const string PatentsViewBaseUri = "https://search.patentsview.org/";

    /// <summary>The USPTO bulk-data download origin used by catalog redirects.</summary>
    public const string BulkDataBaseUri = "https://bulkdata.uspto.gov/";
}
