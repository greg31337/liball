using System;
using System.Collections.Generic;

namespace token;

/// <summary>Contains the outcome of parsing or writing COSE header buckets.</summary>
public class CoseHeaderResult
{
    /// <summary>Indicates whether processing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when processing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed header, or <c>null</c> when processing failed.</summary>
    public CoseHeader? Header;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public CoseHeaderResult()
    {
        Success = false;
        Error = null;
        Header = null;
    }
}
