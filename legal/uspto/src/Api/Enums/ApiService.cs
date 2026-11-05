namespace uspto;

/// <summary>
/// Identifies a remote USPTO-related API service or a distinct service rate bucket.
/// </summary>
public enum ApiService
{
    /// <summary>No service has been selected.</summary>
    Unknown = 0,

    /// <summary>The USPTO Open Data Portal API.</summary>
    Odp = 1,

    /// <summary>The Open Data Portal Office Action data-set API.</summary>
    OfficeAction = 2,

    /// <summary>The general Trademark Status and Document Retrieval API.</summary>
    Tsdr = 3,

    /// <summary>The lower-rate TSDR PDF and ZIP download bucket.</summary>
    TsdrPdfAndZip = 4,

    /// <summary>The PatentsView PatentSearch API.</summary>
    PatentsView = 5
}
