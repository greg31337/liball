using System;

namespace http;

/// <summary>
/// Contains current representation metadata used to evaluate request preconditions.
/// </summary>
public class HttpPreconditionContext
{
    /// <summary>
    /// The request method token.
    /// </summary>
    public string Method = "";

    /// <summary>
    /// Indicates that the evaluator is acting as a cache rather than an origin server.
    /// </summary>
    public bool IsCacheRecipient;

    /// <summary>
    /// Indicates that a current selected representation exists.
    /// </summary>
    public bool RepresentationExists;

    /// <summary>
    /// The current entity tag, or <c>null</c> when unavailable.
    /// </summary>
    public HttpEntityTag? EntityTag;

    /// <summary>
    /// Indicates that a current Last-Modified timestamp is available.
    /// </summary>
    public bool HasLastModified;

    /// <summary>
    /// The current Last-Modified timestamp.
    /// </summary>
    public DateTimeOffset LastModified;

    /// <summary>
    /// Indicates that the response Date timestamp is available.
    /// </summary>
    public bool HasDate;

    /// <summary>
    /// The response Date timestamp.
    /// </summary>
    public DateTimeOffset Date;
}
