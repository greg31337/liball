using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>
/// Provides normalized key type names used by <see cref="TokenKey"/>.
/// </summary>
public static class TokenKeyTypes
{
    /// <summary>A symmetric octet-sequence key.</summary>
    public const string Symmetric = "oct";

    /// <summary>An RSA key.</summary>
    public const string Rsa = "RSA";

    /// <summary>An elliptic-curve key with affine coordinates.</summary>
    public const string EllipticCurve = "EC";

    /// <summary>An octet key pair.</summary>
    public const string OctetKeyPair = "OKP";

    /// <summary>An algorithm-specific key pair, including ML-DSA keys.</summary>
    public const string AlgorithmKeyPair = "AKP";
}
