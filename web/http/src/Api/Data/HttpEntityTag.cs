using System;

namespace http;

/// <summary>
/// Represents an HTTP entity tag without its wire quoting.
/// </summary>
public class HttpEntityTag
{
    /// <summary>
    /// Indicates the wildcard alternative used by an entity-tag list.
    /// </summary>
    public bool IsWildcard;

    /// <summary>
    /// The opaque tag octets between the quotation marks.
    /// </summary>
    public byte[] OpaqueTag = Array.Empty<byte>();

    /// <summary>
    /// Indicates that this is a weak entity tag.
    /// </summary>
    public bool IsWeak;
}
