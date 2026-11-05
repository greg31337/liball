using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents an HTTP media type or media range.
/// </summary>
public class HttpMediaType
{
    /// <summary>
    /// The media type token, excluding a wildcard marker.
    /// </summary>
    public string Type = "";

    /// <summary>
    /// The media subtype token, excluding a wildcard marker.
    /// </summary>
    public string Subtype = "";

    /// <summary>
    /// Indicates that the type component is a wildcard.
    /// </summary>
    public bool IsTypeWildcard;

    /// <summary>
    /// Indicates that the subtype component is a wildcard.
    /// </summary>
    public bool IsSubtypeWildcard;

    /// <summary>
    /// The ordered media parameters.
    /// </summary>
    public List<HttpParameter> Parameters = new List<HttpParameter>();
}
