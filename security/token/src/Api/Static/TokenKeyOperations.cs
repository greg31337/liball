using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>
/// Provides normalized key operation names used by <see cref="TokenKey"/>.
/// </summary>
public static class TokenKeyOperations
{
    /// <summary>Create a signature.</summary>
    public const string Sign = "sign";
    /// <summary>Verify a signature.</summary>
    public const string Verify = "verify";
    /// <summary>Encrypt content.</summary>
    public const string Encrypt = "encrypt";
    /// <summary>Decrypt content.</summary>
    public const string Decrypt = "decrypt";
    /// <summary>Wrap a key.</summary>
    public const string WrapKey = "wrapKey";
    /// <summary>Unwrap a key.</summary>
    public const string UnwrapKey = "unwrapKey";
    /// <summary>Derive a key.</summary>
    public const string DeriveKey = "deriveKey";
    /// <summary>Derive raw key bits.</summary>
    public const string DeriveBits = "deriveBits";
    /// <summary>Create a message authentication code.</summary>
    public const string MacCreate = "macCreate";
    /// <summary>Verify a message authentication code.</summary>
    public const string MacVerify = "macVerify";
}
