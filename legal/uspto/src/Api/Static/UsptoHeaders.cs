namespace uspto;

/// <summary>
/// Provides HTTP header names used by USPTO-related services and transfer workflows.
/// </summary>
public static class UsptoHeaders
{
    /// <summary>The API-key header used by Open Data Portal services.</summary>
    public const string OdpApiKey = "X-API-Key";

    /// <summary>The API-key header used by TSDR.</summary>
    public const string TsdrApiKey = "USPTO-API-KEY";

    /// <summary>The API-key header used by PatentsView PatentSearch.</summary>
    public const string PatentsViewApiKey = "X-Api-Key";

    /// <summary>The standard accepted-representation header.</summary>
    public const string Accept = "Accept";

    /// <summary>The standard request or response representation header.</summary>
    public const string ContentType = "Content-Type";

    /// <summary>The standard client product header.</summary>
    public const string UserAgent = "User-Agent";

    /// <summary>The standard server retry-delay header.</summary>
    public const string RetryAfter = "Retry-After";

    /// <summary>The standard entity-tag validator header.</summary>
    public const string ETag = "ETag";

    /// <summary>The standard last-modified validator header.</summary>
    public const string LastModified = "Last-Modified";

    /// <summary>The standard conditional entity-tag request header.</summary>
    public const string IfNoneMatch = "If-None-Match";

    /// <summary>The standard conditional modification-date request header.</summary>
    public const string IfModifiedSince = "If-Modified-Since";

    /// <summary>The standard redirect target header.</summary>
    public const string Location = "Location";

    /// <summary>The standard attachment filename header.</summary>
    public const string ContentDisposition = "Content-Disposition";

    /// <summary>The standard representation length header.</summary>
    public const string ContentLength = "Content-Length";

    /// <summary>The standard requested byte-range header.</summary>
    public const string Range = "Range";

    /// <summary>The standard partial-representation range header.</summary>
    public const string ContentRange = "Content-Range";

    /// <summary>The standard conditional range request header.</summary>
    public const string IfRange = "If-Range";

    /// <summary>The Open Data Portal response request-identifier header.</summary>
    public const string RequestIdentifier = "X-Request-ID";
}
