namespace uspto;

/// <summary>
/// Configures the endpoints, credentials, transport, policies, and safety limits of a USPTO client suite.
/// </summary>
public class UsptoClientOptions
{
    /// <summary>The USPTO Open Data Portal base URI.</summary>
    public global::System.Uri OdpBaseUri = new global::System.Uri(UsptoEndpoints.OdpBaseUri);

    /// <summary>The Office Action data-set API base URI.</summary>
    public global::System.Uri OfficeActionBaseUri = new global::System.Uri(UsptoEndpoints.OdpBaseUri);

    /// <summary>The Trademark Status and Document Retrieval API base URI.</summary>
    public global::System.Uri TsdrBaseUri = new global::System.Uri(UsptoEndpoints.TsdrBaseUri);

    /// <summary>The PatentsView PatentSearch API base URI.</summary>
    public global::System.Uri PatentsViewBaseUri = new global::System.Uri(UsptoEndpoints.PatentsViewBaseUri);

    /// <summary>An optional provider consulted at request time so credentials can rotate.</summary>
    public IApiKeyProvider? ApiKeyProvider = null;

    /// <summary>An explicit Open Data Portal API key, or <c>null</c> to use the provider or environment.</summary>
    public string? OdpApiKey = null;

    /// <summary>An explicit TSDR API key, or <c>null</c> to use the provider or environment.</summary>
    public string? TsdrApiKey = null;

    /// <summary>An explicit PatentsView API key, or <c>null</c> to use the provider or environment.</summary>
    public string? PatentsViewApiKey = null;

    /// <summary>An optional caller-owned HTTP client used for connection reuse and testability.</summary>
    public global::System.Net.Http.HttpClient? HttpClient = null;

    /// <summary>Whether the suite disposes the injected HTTP client when the suite is disposed.</summary>
    public bool DisposeHttpClient = false;

    /// <summary>The product token sent in the HTTP <c>User-Agent</c> header.</summary>
    public string UserAgent = "uspto-dotnet/1.0";

    /// <summary>The maximum time allowed to establish an HTTP connection, in milliseconds.</summary>
    public int ConnectTimeoutMilliseconds = UsptoDefaults.RequestTimeoutMilliseconds;

    /// <summary>The maximum time allowed to receive HTTP response headers, in milliseconds.</summary>
    public int HeaderTimeoutMilliseconds = UsptoDefaults.RequestTimeoutMilliseconds;

    /// <summary>The maximum time allowed to read an API response body, in milliseconds.</summary>
    public int BodyTimeoutMilliseconds = UsptoDefaults.RequestTimeoutMilliseconds;

    /// <summary>The maximum total time allowed for one non-download HTTP operation, in milliseconds.</summary>
    public int OverallTimeoutMilliseconds = UsptoDefaults.RequestTimeoutMilliseconds;

    /// <summary>The default context copied into individual requests.</summary>
    public RequestContext Request = new RequestContext();

    /// <summary>The retry policy shared by remote clients.</summary>
    public RetryPolicy Retry = new RetryPolicy();

    /// <summary>The independently enforced service rate buckets.</summary>
    public RateLimitPolicy[] RateLimits = new RateLimitPolicy[]
    {
        new RateLimitPolicy
        {
            Service = ApiService.Tsdr,
            RequestsPerWindow = UsptoDefaults.TsdrGeneralRequestsPerMinute
        },
        new RateLimitPolicy
        {
            Service = ApiService.TsdrPdfAndZip,
            RequestsPerWindow = UsptoDefaults.TsdrPdfAndZipRequestsPerMinute
        },
        new RateLimitPolicy
        {
            Service = ApiService.PatentsView,
            RequestsPerWindow = UsptoDefaults.PatentsViewRequestsPerMinute
        }
    };

    /// <summary>The response cache policy.</summary>
    public CachePolicy Cache = new CachePolicy();

    /// <summary>The online, cache, and local-mirror fallback policy.</summary>
    public DataFallbackPolicy Fallback = new DataFallbackPolicy();

    /// <summary>The streamed download policy.</summary>
    public DownloadPolicy Download = new DownloadPolicy();

    /// <summary>The archive extraction safety limits.</summary>
    public ExtractionLimits Extraction = new ExtractionLimits();

    /// <summary>The local dataset mirror policy.</summary>
    public MirrorPolicy Mirror = new MirrorPolicy();

    /// <summary>The cross-cutting request, response, parser, and result limits.</summary>
    public UsptoLimits Limits = new UsptoLimits();

    /// <summary>Additional HTTPS hosts accepted for document and bulk download redirects.</summary>
    public string[] PermittedDownloadHosts = new string[0];

    /// <summary>Whether loopback HTTP endpoints are permitted for caller-controlled tests.</summary>
    public bool PermitInsecureTestEndpoints = false;
}
