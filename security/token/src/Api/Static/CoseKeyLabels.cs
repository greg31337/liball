using System;
namespace token;

/// <summary>
/// Provides COSE key types, common and type-specific parameters, operations, and curves.
/// </summary>
public static class CoseKeyLabels
{
    /// <summary>The OKP key type.</summary>
    public const int KeyTypeOkp = 1;

    /// <summary>The elliptic-curve key type with affine coordinates.</summary>
    public const int KeyTypeEc2 = 2;

    /// <summary>The RSA key type.</summary>
    public const int KeyTypeRsa = 3;

    /// <summary>The symmetric key type.</summary>
    public const int KeyTypeSymmetric = 4;

    /// <summary>The HSS/LMS key type.</summary>
    public const int KeyTypeHssLms = 5;

    /// <summary>The WalnutDSA key type.</summary>
    public const int KeyTypeWalnutDsa = 6;

    /// <summary>The algorithm key pair key type.</summary>
    public const int KeyTypeAlgorithmKeyPair = 7;

    /// <summary>The key type common parameter.</summary>
    public const int KeyType = 1;

    /// <summary>The key identifier common parameter.</summary>
    public const int KeyId = 2;

    /// <summary>The algorithm common parameter.</summary>
    public const int Algorithm = 3;

    /// <summary>The key operations common parameter.</summary>
    public const int KeyOperations = 4;

    /// <summary>The base initialization vector common parameter.</summary>
    public const int BaseInitializationVector = 5;

    /// <summary>The curve parameter for OKP and EC2 keys.</summary>
    public const int Curve = -1;

    /// <summary>The public x-coordinate or public-key parameter for OKP and EC2 keys.</summary>
    public const int X = -2;

    /// <summary>The y-coordinate parameter for EC2 keys.</summary>
    public const int Y = -3;

    /// <summary>The private key parameter for OKP and EC2 keys.</summary>
    public const int PrivateKey = -4;

    /// <summary>The RSA modulus parameter.</summary>
    public const int RsaModulus = -1;

    /// <summary>The RSA public exponent parameter.</summary>
    public const int RsaPublicExponent = -2;

    /// <summary>The RSA private exponent parameter.</summary>
    public const int RsaPrivateExponent = -3;

    /// <summary>The first RSA prime factor parameter.</summary>
    public const int RsaFirstPrimeFactor = -4;

    /// <summary>The second RSA prime factor parameter.</summary>
    public const int RsaSecondPrimeFactor = -5;

    /// <summary>The first RSA CRT exponent parameter.</summary>
    public const int RsaFirstFactorCrtExponent = -6;

    /// <summary>The second RSA CRT exponent parameter.</summary>
    public const int RsaSecondFactorCrtExponent = -7;

    /// <summary>The RSA CRT coefficient parameter.</summary>
    public const int RsaCrtCoefficient = -8;

    /// <summary>The RSA other-primes information parameter.</summary>
    public const int RsaOtherPrimes = -9;

    /// <summary>An RSA additional prime factor parameter.</summary>
    public const int RsaAdditionalPrimeFactor = -10;

    /// <summary>An RSA additional factor CRT exponent parameter.</summary>
    public const int RsaAdditionalFactorCrtExponent = -11;

    /// <summary>An RSA additional factor CRT coefficient parameter.</summary>
    public const int RsaAdditionalFactorCrtCoefficient = -12;

    /// <summary>The symmetric key material parameter.</summary>
    public const int SymmetricKey = -1;

    /// <summary>The HSS/LMS public key parameter.</summary>
    public const int HssLmsPublicKey = -1;

    /// <summary>The algorithm key pair public key parameter.</summary>
    public const int AlgorithmKeyPairPublicKey = -1;

    /// <summary>The algorithm key pair private key parameter.</summary>
    public const int AlgorithmKeyPairPrivateKey = -2;

    /// <summary>The sign key operation.</summary>
    public const int OperationSign = 1;

    /// <summary>The verify key operation.</summary>
    public const int OperationVerify = 2;

    /// <summary>The encrypt key operation.</summary>
    public const int OperationEncrypt = 3;

    /// <summary>The decrypt key operation.</summary>
    public const int OperationDecrypt = 4;

    /// <summary>The wrap-key operation.</summary>
    public const int OperationWrapKey = 5;

    /// <summary>The unwrap-key operation.</summary>
    public const int OperationUnwrapKey = 6;

    /// <summary>The derive-key operation.</summary>
    public const int OperationDeriveKey = 7;

    /// <summary>The derive-bits operation.</summary>
    public const int OperationDeriveBits = 8;

    /// <summary>The MAC-create operation.</summary>
    public const int OperationMacCreate = 9;

    /// <summary>The MAC-verify operation.</summary>
    public const int OperationMacVerify = 10;

    /// <summary>The NIST P-256 curve.</summary>
    public const int CurveP256 = 1;

    /// <summary>The NIST P-384 curve.</summary>
    public const int CurveP384 = 2;

    /// <summary>The NIST P-521 curve.</summary>
    public const int CurveP521 = 3;

    /// <summary>The X25519 curve.</summary>
    public const int CurveX25519 = 4;

    /// <summary>The X448 curve.</summary>
    public const int CurveX448 = 5;

    /// <summary>The Ed25519 curve.</summary>
    public const int CurveEd25519 = 6;

    /// <summary>The Ed448 curve.</summary>
    public const int CurveEd448 = 7;

    /// <summary>The secp256k1 curve.</summary>
    public const int CurveSecp256k1 = 8;

    /// <summary>The brainpoolP256r1 curve.</summary>
    public const int CurveBrainpoolP256r1 = 256;

    /// <summary>The brainpoolP320r1 curve.</summary>
    public const int CurveBrainpoolP320r1 = 257;

    /// <summary>The brainpoolP384r1 curve.</summary>
    public const int CurveBrainpoolP384r1 = 258;

    /// <summary>The brainpoolP512r1 curve.</summary>
    public const int CurveBrainpoolP512r1 = 259;
}
