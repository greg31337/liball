using System;
namespace token;

/// <summary>
/// Provides registered JSON Web Key member names and key-type names.
/// </summary>
public static class JwkNames
{
    /// <summary>The key type member.</summary>
    public const string KeyType = "kty";
    /// <summary>The public key use member.</summary>
    public const string Use = "use";
    /// <summary>The key operations member.</summary>
    public const string KeyOperations = "key_ops";
    /// <summary>The algorithm member.</summary>
    public const string Algorithm = "alg";
    /// <summary>The key identifier member.</summary>
    public const string KeyId = "kid";
    /// <summary>The X.509 certificate URL member.</summary>
    public const string X509Url = "x5u";
    /// <summary>The X.509 certificate chain member.</summary>
    public const string X509CertificateChain = "x5c";
    /// <summary>The SHA-1 X.509 certificate thumbprint member.</summary>
    public const string X509Sha1Thumbprint = "x5t";
    /// <summary>The SHA-256 X.509 certificate thumbprint member.</summary>
    public const string X509Sha256Thumbprint = "x5t#S256";
    /// <summary>The elliptic curve member.</summary>
    public const string Curve = "crv";
    /// <summary>The EC x-coordinate or OKP public-key member.</summary>
    public const string X = "x";
    /// <summary>The EC y-coordinate member.</summary>
    public const string Y = "y";
    /// <summary>The private key member used by EC and OKP keys.</summary>
    public const string PrivateKey = "d";
    /// <summary>The RSA modulus member.</summary>
    public const string RsaModulus = "n";
    /// <summary>The RSA public exponent member.</summary>
    public const string RsaExponent = "e";
    /// <summary>The first RSA prime factor member.</summary>
    public const string RsaFirstPrime = "p";
    /// <summary>The second RSA prime factor member.</summary>
    public const string RsaSecondPrime = "q";
    /// <summary>The first RSA CRT exponent member.</summary>
    public const string RsaFirstExponent = "dp";
    /// <summary>The second RSA CRT exponent member.</summary>
    public const string RsaSecondExponent = "dq";
    /// <summary>The RSA CRT coefficient member.</summary>
    public const string RsaCoefficient = "qi";
    /// <summary>The RSA other-primes member.</summary>
    public const string RsaOtherPrimes = "oth";
    /// <summary>The symmetric key member.</summary>
    public const string SymmetricKey = "k";
    /// <summary>The algorithm-specific public key member.</summary>
    public const string AlgorithmPublicKey = "pub";
    /// <summary>The algorithm-specific private key member.</summary>
    public const string AlgorithmPrivateKey = "priv";
}
