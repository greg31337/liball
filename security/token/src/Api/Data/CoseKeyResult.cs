using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Contains the outcome of parsing a COSE_Key structure.</summary>
public class CoseKeyResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed key, or <c>null</c> when parsing failed.</summary>
    public TokenKey? Key;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public CoseKeyResult()
    {
        Success = false;
        Error = null;
        Key = null;
    }
}
