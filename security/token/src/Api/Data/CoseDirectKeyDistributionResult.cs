using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Contains the outcome of COSE direct key distribution.</summary>
public class CoseDirectKeyDistributionResult
{
    /// <summary>Indicates whether key distribution succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when distribution succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains an independent copy of the content key.</summary>
    public byte[]? ContentKey;

    /// <summary>Contains the empty COSE recipient ciphertext value.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Initializes an unsuccessful result with no key output.</summary>
    public CoseDirectKeyDistributionResult()
    {
        Success = false;
        Error = null;
        ContentKey = null;
        EncryptedKey = null;
    }
}
