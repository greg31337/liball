namespace http;

/// <summary>
/// Describes classification metadata for an HTTP status code.
/// </summary>
public class HttpStatusMetadata
{
    /// <summary>
    /// The numeric HTTP status code.
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// The semantic class of the status code.
    /// </summary>
    public HttpStatusClass StatusClass;

    /// <summary>
    /// Indicates that the status code appears in the registry snapshot.
    /// </summary>
    public bool IsRegistered;

    /// <summary>
    /// The conventional reason phrase, or <c>null</c> when none is known.
    /// </summary>
    public string? ReasonPhrase;

    /// <summary>
    /// Indicates that a response with this status can carry content when request context permits it.
    /// </summary>
    public bool AllowsContent;

    /// <summary>
    /// Indicates that a response with this status is cacheable by default.
    /// </summary>
    public bool IsCacheableByDefault;

    /// <summary>
    /// Indicates that the status represents a redirection response.
    /// </summary>
    public bool IsRedirect;

    /// <summary>
    /// Indicates that conventional redirect handling can change a POST request to GET.
    /// </summary>
    public bool RedirectsPostToGet;

    /// <summary>
    /// Indicates that the status commonly permits retrying the request subject to method semantics.
    /// </summary>
    public bool IsRetryable;
}
