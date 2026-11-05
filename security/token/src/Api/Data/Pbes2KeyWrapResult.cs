using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>
/// Contains the outcome of JWE PBES2 key wrapping.
/// </summary>
public class Pbes2KeyWrapResult
{
    /// <summary>Indicates whether PBES2 key wrapping succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the JWE encrypted-key value.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the randomly generated value for the JWE <c>p2s</c> header.</summary>
    public byte[]? SaltInput;

    /// <summary>Contains the PBKDF2 iteration count for the JWE <c>p2c</c> header.</summary>
    public int IterationCount;

    /// <summary>Initializes an unsuccessful result with no output or error details.</summary>
    public Pbes2KeyWrapResult()
    {
        Success = false;
        Error = null;
        EncryptedKey = null;
        SaltInput = null;
        IterationCount = 0;
    }
}
