namespace uspto;

/// <summary>
/// Reports a missing, rejected, or unauthorized service credential.
/// </summary>
public class AuthenticationException : global::System.Exception
{
    /// <summary>The service for which authentication failed.</summary>
    public ApiService Service;

    /// <summary>The HTTP status code when one was received.</summary>
    public int StatusCode;

    /// <summary>Whether <see cref="StatusCode"/> contains a received HTTP status code.</summary>
    public bool HasStatusCode;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier;

    /// <summary>Initializes an authentication exception without retaining credential values.</summary>
    /// <param name="message">The credential-free exception message.</param>
    /// <param name="service">The service for which authentication failed.</param>
    /// <param name="statusCode">The HTTP status code, if present.</param>
    /// <param name="hasStatusCode">Whether <c>statusCode</c> is present.</param>
    /// <param name="requestIdentifier">The service request identifier.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public AuthenticationException(string message, ApiService service, int statusCode, bool hasStatusCode, string? requestIdentifier, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Service = service;
        StatusCode = statusCode;
        HasStatusCode = hasStatusCode;
        RequestIdentifier = requestIdentifier;
    }
}
