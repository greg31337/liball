using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents selectable representation metadata and owned content.
/// </summary>
public class HttpRepresentation
{
    /// <summary>
    /// The representation media type, or <c>null</c> when unspecified.
    /// </summary>
    public HttpMediaType? MediaType;

    /// <summary>
    /// The character set token, or <c>null</c> when unspecified.
    /// </summary>
    public string? Charset;

    /// <summary>
    /// The content codings in the order in which they were applied.
    /// </summary>
    public List<string> ContentCodings = new List<string>();

    /// <summary>
    /// The representation language tags.
    /// </summary>
    public List<string> Languages = new List<string>();

    /// <summary>
    /// Indicates that a content length is known.
    /// </summary>
    public bool HasContentLength;

    /// <summary>
    /// The known content length.
    /// </summary>
    public long ContentLength;

    /// <summary>
    /// The Content-Location URI reference, or <c>null</c> when absent.
    /// </summary>
    public string? ContentLocation;

    /// <summary>
    /// The entity tag, or <c>null</c> when absent.
    /// </summary>
    public HttpEntityTag? EntityTag;

    /// <summary>
    /// Indicates that a Last-Modified value is available.
    /// </summary>
    public bool HasLastModified;

    /// <summary>
    /// The Last-Modified timestamp.
    /// </summary>
    public DateTimeOffset LastModified;

    /// <summary>
    /// The advertised range units.
    /// </summary>
    public List<string> AcceptedRangeUnits = new List<string>();

    /// <summary>
    /// The Content-Range metadata, or <c>null</c> for a complete representation.
    /// </summary>
    public HttpContentRange? ContentRange;

    /// <summary>
    /// The owned representation data.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();
}
