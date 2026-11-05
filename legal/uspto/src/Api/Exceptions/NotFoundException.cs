namespace uspto;

/// <summary>
/// Reports that a requested service resource was not found.
/// </summary>
public class NotFoundException : global::System.Exception
{
    /// <summary>The service queried for the resource.</summary>
    public ApiService Service;

    /// <summary>The normalized resource identifier, when safe to retain.</summary>
    public string? ResourceIdentifier;

    /// <summary>The HTTP status code when one was received.</summary>
    public int StatusCode;

    /// <summary>Whether <see cref="StatusCode"/> contains a received HTTP status code.</summary>
    public bool HasStatusCode;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier;

    /// <summary>Initializes a not-found exception and its resource context.</summary>
    /// <param name="message">The credential-free exception message.</param>
    /// <param name="service">The service queried for the resource.</param>
    /// <param name="resourceIdentifier">The normalized resource identifier.</param>
    /// <param name="statusCode">The HTTP status code, if present.</param>
    /// <param name="hasStatusCode">Whether <c>statusCode</c> is present.</param>
    /// <param name="requestIdentifier">The service request identifier.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public NotFoundException(string message, ApiService service, string? resourceIdentifier, int statusCode, bool hasStatusCode, string? requestIdentifier, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Service = service;
        ResourceIdentifier = resourceIdentifier;
        StatusCode = statusCode;
        HasStatusCode = hasStatusCode;
        RequestIdentifier = requestIdentifier;
    }
}
