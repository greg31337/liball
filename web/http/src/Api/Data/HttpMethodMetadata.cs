namespace http;

/// <summary>
/// Describes registered and semantic characteristics of an HTTP method.
/// </summary>
public class HttpMethodMetadata
{
    /// <summary>
    /// The method token with its canonical or supplied spelling.
    /// </summary>
    public string Method = "";

    /// <summary>
    /// Indicates that the method appears in the registry snapshot.
    /// </summary>
    public bool IsRegistered;

    /// <summary>
    /// Indicates that RFC 9110 defines core semantics for the method.
    /// </summary>
    public bool IsCore;

    /// <summary>
    /// Indicates that the method is safe.
    /// </summary>
    public bool IsSafe;

    /// <summary>
    /// Indicates that the method is idempotent.
    /// </summary>
    public bool IsIdempotent;

    /// <summary>
    /// Indicates that responses to the method are cacheable when other requirements are met.
    /// </summary>
    public bool IsCacheable;

    /// <summary>
    /// Indicates that request content has defined semantics for the method.
    /// </summary>
    public bool HasDefinedRequestContentSemantics;
}
