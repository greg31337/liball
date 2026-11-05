using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains the outcome of parsing a JOSE header.
/// </summary>
public class JoseHeaderResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed header, or <c>null</c> when parsing failed.</summary>
    public JoseHeader? Header;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public JoseHeaderResult()
    {
        Success = false;
        Error = null;
        Header = null;
    }
}
