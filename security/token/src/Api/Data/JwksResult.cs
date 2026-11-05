using System;
using System.Text;

namespace token;

/// <summary>
/// Contains the outcome of parsing a JSON Web Key Set.
/// </summary>
public class JwksResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed key set, or <c>null</c> when parsing failed.</summary>
    public JsonWebKeySet? Set;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public JwksResult()
    {
        Success = false;
        Error = null;
        Set = null;
    }
}
