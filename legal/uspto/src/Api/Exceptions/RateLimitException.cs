namespace uspto;

/// <summary>
/// Reports that a service rate bucket refused a request.
/// </summary>
public class RateLimitException : global::System.Exception
{
    /// <summary>The service or rate bucket that refused the request.</summary>
    public ApiService Service;

    /// <summary>The HTTP status code when one was received.</summary>
    public int StatusCode;

    /// <summary>Whether <see cref="StatusCode"/> contains a received HTTP status code.</summary>
    public bool HasStatusCode;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier;

    /// <summary>The server-requested retry delay in milliseconds.</summary>
    public long RetryAfterMilliseconds;

    /// <summary>Whether <see cref="RetryAfterMilliseconds"/> contains a server-provided delay.</summary>
    public bool HasRetryAfterMilliseconds;

    /// <summary>Initializes a rate-limit exception and its bounded retry context.</summary>
    /// <param name="message">The credential-free exception message.</param>
    /// <param name="service">The service or rate bucket that refused the request.</param>
    /// <param name="statusCode">The HTTP status code, if present.</param>
    /// <param name="hasStatusCode">Whether <c>statusCode</c> is present.</param>
    /// <param name="requestIdentifier">The service request identifier.</param>
    /// <param name="retryAfterMilliseconds">The server-requested retry delay, if present.</param>
    /// <param name="hasRetryAfterMilliseconds">Whether <c>retryAfterMilliseconds</c> is present.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public RateLimitException(string message, ApiService service, int statusCode, bool hasStatusCode, string? requestIdentifier, long retryAfterMilliseconds, bool hasRetryAfterMilliseconds, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Service = service;
        StatusCode = statusCode;
        HasStatusCode = hasStatusCode;
        RequestIdentifier = requestIdentifier;
        RetryAfterMilliseconds = retryAfterMilliseconds;
        HasRetryAfterMilliseconds = hasRetryAfterMilliseconds;
    }
}
