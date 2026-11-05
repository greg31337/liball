using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Contains the outcome of COSE ECDH recipient processing.</summary>
public class CoseEcdhResult
{
    /// <summary>Indicates whether ECDH recipient processing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the derived or unwrapped content key.</summary>
    public byte[]? ContentKey;

    /// <summary>Contains the empty or wrapped recipient ciphertext.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the generated ephemeral public key, when applicable.</summary>
    public TokenKey? SenderPublicKey;

    /// <summary>Initializes an unsuccessful result with no key output.</summary>
    public CoseEcdhResult()
    {
        Success = false;
        Error = null;
        ContentKey = null;
        EncryptedKey = null;
        SenderPublicKey = null;
    }
}
