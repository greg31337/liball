using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents generic HTTP authentication scheme data.
/// </summary>
public class HttpAuthenticationValue
{
    /// <summary>
    /// The authentication scheme token.
    /// </summary>
    public string Scheme = "";

    /// <summary>
    /// The token68 octets, or <c>null</c> when parameter form is used.
    /// </summary>
    public byte[]? Token68;

    /// <summary>
    /// The ordered authentication parameters.
    /// </summary>
    public List<HttpParameter> Parameters = new List<HttpParameter>();
}
