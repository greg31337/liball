using System;
namespace token;

/// <summary>
/// Provides integer labels from the COSE Algorithms registry.
/// </summary>
public static class CoseAlgorithms
{
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-1.</summary>
    public const int RsaSha1 = -65535;

    /// <summary>AES-CTR using a 128-bit key.</summary>
    public const int Aes128Ctr = -65534;

    /// <summary>AES-CTR using a 192-bit key.</summary>
    public const int Aes192Ctr = -65533;

    /// <summary>AES-CTR using a 256-bit key.</summary>
    public const int Aes256Ctr = -65532;

    /// <summary>AES-CBC using a 128-bit key.</summary>
    public const int Aes128Cbc = -65531;

    /// <summary>AES-CBC using a 192-bit key.</summary>
    public const int Aes192Cbc = -65530;

    /// <summary>AES-CBC using a 256-bit key.</summary>
    public const int Aes256Cbc = -65529;

    /// <summary>ECDSA using brainpoolP512r1 and SHA-512.</summary>
    public const int EcdsaBrainpoolP512r1Sha512 = -268;

    /// <summary>ECDSA using brainpoolP384r1 and SHA-384.</summary>
    public const int EcdsaBrainpoolP384r1Sha384 = -267;

    /// <summary>ECDSA using brainpoolP320r1 and SHA-384.</summary>
    public const int EcdsaBrainpoolP320r1Sha384 = -266;

    /// <summary>ECDSA using brainpoolP256r1 and SHA-256.</summary>
    public const int EcdsaBrainpoolP256r1Sha256 = -265;

    /// <summary>KangarooTwelve with 256-bit output.</summary>
    public const int KangarooTwelve256 = -264;

    /// <summary>KangarooTwelve with 128-bit output.</summary>
    public const int KangarooTwelve128 = -263;

    /// <summary>TurboSHAKE256.</summary>
    public const int TurboShake256 = -262;

    /// <summary>TurboSHAKE128.</summary>
    public const int TurboShake128 = -261;

    /// <summary>The WalnutDSA signature algorithm.</summary>
    public const int WalnutDsa = -260;

    /// <summary>Direct use of a shared symmetric key.</summary>
    public const int Direct = -6;

    /// <summary>ECDSA using P-256 and SHA-256.</summary>
    public const int EcdsaSha256 = -7;

    /// <summary>The generic EdDSA algorithm.</summary>
    public const int EdDsa = -8;

    /// <summary>ECDSA using P-256 and SHA-256 under the current COSE assignment.</summary>
    public const int EcdsaP256Sha256 = -9;

    /// <summary>Direct key derivation using HKDF SHA-256.</summary>
    public const int DirectHkdfSha256 = -10;

    /// <summary>Direct key derivation using HKDF SHA-512.</summary>
    public const int DirectHkdfSha512 = -11;

    /// <summary>Direct key derivation using HKDF AES-128.</summary>
    public const int DirectHkdfAes128 = -12;

    /// <summary>Direct key derivation using HKDF AES-256.</summary>
    public const int DirectHkdfAes256 = -13;

    /// <summary>SHA-1.</summary>
    public const int Sha1 = -14;

    /// <summary>SHA-256 truncated to 64 bits.</summary>
    public const int Sha256Truncated64 = -15;

    /// <summary>SHA-256.</summary>
    public const int Sha256 = -16;

    /// <summary>SHA-512 truncated to 256 bits.</summary>
    public const int Sha512Truncated256 = -17;

    /// <summary>SHAKE128.</summary>
    public const int Shake128 = -18;

    /// <summary>EdDSA using Ed25519.</summary>
    public const int Ed25519 = -19;

    /// <summary>ECDH-ES using HKDF SHA-256.</summary>
    public const int EcdhEsHkdfSha256 = -25;

    /// <summary>ECDH-ES using HKDF SHA-512.</summary>
    public const int EcdhEsHkdfSha512 = -26;

    /// <summary>ECDH-SS using HKDF SHA-256.</summary>
    public const int EcdhSsHkdfSha256 = -27;

    /// <summary>ECDH-SS using HKDF SHA-512.</summary>
    public const int EcdhSsHkdfSha512 = -28;

    /// <summary>ECDH-ES with 128-bit AES Key Wrap.</summary>
    public const int EcdhEsAes128KeyWrap = -29;

    /// <summary>ECDH-ES with 192-bit AES Key Wrap.</summary>
    public const int EcdhEsAes192KeyWrap = -30;

    /// <summary>ECDH-ES with 256-bit AES Key Wrap.</summary>
    public const int EcdhEsAes256KeyWrap = -31;

    /// <summary>ECDH-SS with 128-bit AES Key Wrap.</summary>
    public const int EcdhSsAes128KeyWrap = -32;

    /// <summary>ECDH-SS with 192-bit AES Key Wrap.</summary>
    public const int EcdhSsAes192KeyWrap = -33;

    /// <summary>ECDH-SS with 256-bit AES Key Wrap.</summary>
    public const int EcdhSsAes256KeyWrap = -34;

    /// <summary>ECDSA using P-384 and SHA-384.</summary>
    public const int EcdsaSha384 = -35;

    /// <summary>ECDSA using P-521 and SHA-512.</summary>
    public const int EcdsaSha512 = -36;

    /// <summary>RSASSA-PSS using SHA-256.</summary>
    public const int RsaPssSha256 = -37;

    /// <summary>RSASSA-PSS using SHA-384.</summary>
    public const int RsaPssSha384 = -38;

    /// <summary>RSASSA-PSS using SHA-512.</summary>
    public const int RsaPssSha512 = -39;

    /// <summary>RSAES-OAEP with RFC 8017 default parameters.</summary>
    public const int RsaOaep = -40;

    /// <summary>RSAES-OAEP using SHA-256.</summary>
    public const int RsaOaepSha256 = -41;

    /// <summary>RSAES-OAEP using SHA-512.</summary>
    public const int RsaOaepSha512 = -42;

    /// <summary>SHA-384.</summary>
    public const int Sha384 = -43;

    /// <summary>SHA-512.</summary>
    public const int Sha512 = -44;

    /// <summary>SHAKE256.</summary>
    public const int Shake256 = -45;

    /// <summary>The HSS/LMS signature algorithm.</summary>
    public const int HssLms = -46;

    /// <summary>ECDSA using secp256k1 and SHA-256.</summary>
    public const int EcdsaSecp256k1Sha256 = -47;

    /// <summary>ML-DSA-44.</summary>
    public const int MlDsa44 = -48;

    /// <summary>ML-DSA-65.</summary>
    public const int MlDsa65 = -49;

    /// <summary>ML-DSA-87.</summary>
    public const int MlDsa87 = -50;

    /// <summary>ECDSA using P-384 and SHA-384 under the current COSE assignment.</summary>
    public const int EcdsaP384Sha384 = -51;

    /// <summary>ECDSA using P-521 and SHA-512 under the current COSE assignment.</summary>
    public const int EcdsaP521Sha512 = -52;

    /// <summary>EdDSA using Ed448.</summary>
    public const int Ed448 = -53;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-256.</summary>
    public const int RsaSha256 = -257;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-384.</summary>
    public const int RsaSha384 = -258;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-512.</summary>
    public const int RsaSha512 = -259;

    /// <summary>AES Key Wrap using a 256-bit key.</summary>
    public const int Aes256KeyWrap = -5;

    /// <summary>AES Key Wrap using a 192-bit key.</summary>
    public const int Aes192KeyWrap = -4;

    /// <summary>AES Key Wrap using a 128-bit key.</summary>
    public const int Aes128KeyWrap = -3;

    /// <summary>AES-GCM using a 128-bit key.</summary>
    public const int Aes128Gcm = 1;

    /// <summary>AES-GCM using a 192-bit key.</summary>
    public const int Aes192Gcm = 2;

    /// <summary>AES-GCM using a 256-bit key.</summary>
    public const int Aes256Gcm = 3;

    /// <summary>HMAC SHA-256 truncated to 64 bits.</summary>
    public const int HmacSha256Truncated64 = 4;

    /// <summary>HMAC SHA-256.</summary>
    public const int HmacSha256 = 5;

    /// <summary>HMAC SHA-384.</summary>
    public const int HmacSha384 = 6;

    /// <summary>HMAC SHA-512.</summary>
    public const int HmacSha512 = 7;

    /// <summary>AES-CCM with a 64-bit tag, 13-byte nonce, and 128-bit key.</summary>
    public const int AesCcm16_64_128 = 10;

    /// <summary>AES-CCM with a 64-bit tag, 13-byte nonce, and 256-bit key.</summary>
    public const int AesCcm16_64_256 = 11;

    /// <summary>AES-CCM with a 64-bit tag, 7-byte nonce, and 128-bit key.</summary>
    public const int AesCcm64_64_128 = 12;

    /// <summary>AES-CCM with a 64-bit tag, 7-byte nonce, and 256-bit key.</summary>
    public const int AesCcm64_64_256 = 13;

    /// <summary>AES-MAC using a 128-bit key and 64-bit tag.</summary>
    public const int AesMac128_64 = 14;

    /// <summary>AES-MAC using a 256-bit key and 64-bit tag.</summary>
    public const int AesMac256_64 = 15;

    /// <summary>ChaCha20 with Poly1305.</summary>
    public const int ChaCha20Poly1305 = 24;

    /// <summary>AES-MAC using a 128-bit key and 128-bit tag.</summary>
    public const int AesMac128_128 = 25;

    /// <summary>AES-MAC using a 256-bit key and 128-bit tag.</summary>
    public const int AesMac256_128 = 26;

    /// <summary>AES-CCM with a 128-bit tag, 13-byte nonce, and 128-bit key.</summary>
    public const int AesCcm16_128_128 = 30;

    /// <summary>AES-CCM with a 128-bit tag, 13-byte nonce, and 256-bit key.</summary>
    public const int AesCcm16_128_256 = 31;

    /// <summary>AES-CCM with a 128-bit tag, 7-byte nonce, and 128-bit key.</summary>
    public const int AesCcm64_128_128 = 32;

    /// <summary>AES-CCM with a 128-bit tag, 7-byte nonce, and 256-bit key.</summary>
    public const int AesCcm64_128_256 = 33;

    /// <summary>The COSE initialization-vector generation algorithm.</summary>
    public const int InitializationVectorGeneration = 34;
}
