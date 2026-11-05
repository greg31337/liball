using System;
using System.Text;

namespace token;

/// <summary>
/// Contains the outcome of parsing a compact JOSE serialization.
/// </summary>
public class JoseCompactParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed serialization, or <c>null</c> when parsing failed.</summary>
    public JoseCompactSerialization? Serialization;

    /// <summary>Initializes an unsuccessful parse result with no details.</summary>
    public JoseCompactParseResult()
    {
        Success = false;
        Error = null;
        Serialization = null;
    }
}
