using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Contains the outcome of a key import operation.
/// </summary>
public class TokenKeyResult
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the import succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the imported key, or <c>null</c> when the import failed.</summary>
    public TokenKey? Key;

    /// <summary>Initializes an unsuccessful result with no key or error details.</summary>
    public TokenKeyResult()
    {
        Success = false;
        Error = null;
        Key = null;
    }
}
