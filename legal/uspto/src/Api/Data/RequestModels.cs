namespace uspto;

/// <summary>
/// Carries settings that apply to one client request.
/// </summary>
public class RequestContext
{
    /// <summary>The service that will receive the request.</summary>
    public ApiService Service = ApiService.Unknown;

    /// <summary>Cancellation requested by the caller.</summary>
    public global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);

    /// <summary>Whether a bounded copy of the raw response should be retained.</summary>
    public bool RetainRawResponse = false;

    /// <summary>The maximum number of raw response bytes to retain.</summary>
    public long MaximumRawResponseBytes = UsptoDefaults.MaximumRawResponseBytes;

    /// <summary>An optional absolute deadline for the complete operation.</summary>
    public global::System.DateTimeOffset Deadline = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="Deadline"/> contains a value.</summary>
    public bool HasDeadline = false;

    /// <summary>An optional caller-supplied correlation identifier that contains no credentials.</summary>
    public string? CorrelationIdentifier = null;

    /// <summary>An optional per-request fallback policy that replaces the configured suite policy.</summary>
    public DataFallbackPolicy? Fallback = null;
}

/// <summary>
/// Describes a safely captured failure for one independently loaded composite section.
/// </summary>
public class SectionError
{
    /// <summary>The composite section that failed.</summary>
    public string? Section = null;

    /// <summary>The stable exception category.</summary>
    public string? ExceptionType = null;

    /// <summary>A credential-free failure message.</summary>
    public string? Message = null;

    /// <summary>The stable service or validation error code.</summary>
    public string? ErrorCode = null;

    /// <summary>The service associated with the failure.</summary>
    public ApiService Service = ApiService.Unknown;

    /// <summary>The received HTTP status code.</summary>
    public int StatusCode = 0;

    /// <summary>Whether <see cref="StatusCode"/> contains a received value.</summary>
    public bool HasStatusCode = false;

    /// <summary>The service request identifier associated with the failure.</summary>
    public string? RequestIdentifier = null;

    /// <summary>Whether retry policy may treat the failure as transient.</summary>
    public bool IsRetryable = false;
}
