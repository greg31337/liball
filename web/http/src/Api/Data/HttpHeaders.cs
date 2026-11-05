using System.Collections.Generic;

namespace http;

/// <summary>
/// Contains HTTP field lines in insertion order without combining duplicates.
/// </summary>
public class HttpHeaders
{
    /// <summary>
    /// The ordered field lines.
    /// </summary>
    public List<HttpHeaderField> Fields = new List<HttpHeaderField>();
}
