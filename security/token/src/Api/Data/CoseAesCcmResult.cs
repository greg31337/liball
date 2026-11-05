using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Contains the outcome of COSE AES-CCM encryption.</summary>
public class CoseAesCcmResult
{
    /// <summary>Indicates whether encryption succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when encryption succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the ciphertext.</summary>
    public byte[]? Ciphertext;

    /// <summary>Contains the nonce.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains the authentication tag.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Initializes an unsuccessful result with no output or failure details.</summary>
    public CoseAesCcmResult()
    {
        Success = false;
        Error = null;
        Ciphertext = null;
        InitializationVector = null;
        AuthenticationTag = null;
    }
}
