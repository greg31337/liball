namespace uspto;

/// <summary>
/// Reports a remote API failure that does not have a more specific exception type.
/// </summary>
public class ApiException : global::System.Exception
{
    /// <summary>The service that reported the failure.</summary>
    public ApiService Service;

    /// <summary>The HTTP status code when one was received.</summary>
    public int StatusCode;

    /// <summary>Whether <see cref="StatusCode"/> contains a received HTTP status code.</summary>
    public bool HasStatusCode;

    /// <summary>A service-provided error code that contains no credential data.</summary>
    public string? ErrorCode;

    /// <summary>Bounded service-provided error details with credential data removed.</summary>
    public string? ErrorDetails;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier;

    /// <summary>Whether retry policy may treat the failure as transient.</summary>
    public bool IsRetryable;

    /// <summary>Initializes an API exception and its safe remote failure context.</summary>
    /// <param name="message">The credential-free exception message.</param>
    /// <param name="service">The service that reported the failure.</param>
    /// <param name="statusCode">The HTTP status code, if present.</param>
    /// <param name="hasStatusCode">Whether <c>statusCode</c> is present.</param>
    /// <param name="errorCode">The service-provided error code.</param>
    /// <param name="errorDetails">Bounded, credential-free service error details.</param>
    /// <param name="requestIdentifier">The service request identifier.</param>
    /// <param name="isRetryable">Whether retry policy may treat the failure as transient.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public ApiException(string message, ApiService service, int statusCode, bool hasStatusCode, string? errorCode, string? errorDetails, string? requestIdentifier, bool isRetryable, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Service = service;
        StatusCode = statusCode;
        HasStatusCode = hasStatusCode;
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
        RequestIdentifier = requestIdentifier;
        IsRetryable = isRetryable;
    }
}
