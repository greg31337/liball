using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Contains an encoded COSE message and separately transported ciphertext.</summary>
public class CoseDetachedEncryptionResult
{
    /// <summary>Indicates whether encryption succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the encoded COSE message with a null ciphertext field.</summary>
    public byte[]? Bytes;

    /// <summary>Contains the detached ciphertext and authentication tag.</summary>
    public byte[]? Ciphertext;

    /// <summary>Initializes an unsuccessful result with no output.</summary>
    public CoseDetachedEncryptionResult()
    {
        Success = false;
        Error = null;
        Bytes = null;
        Ciphertext = null;
    }
}
