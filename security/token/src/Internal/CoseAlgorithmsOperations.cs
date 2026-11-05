using System;
namespace token;

/// <summary>
/// Provides integer labels and names from the COSE Algorithms registry.
/// </summary>
internal static class CoseAlgorithmsOperations
{
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-1.</summary>
    internal const int RsaSha1 = CoseAlgorithms.RsaSha1;
    /// <summary>AES-CTR using a 128-bit key.</summary>
    internal const int Aes128Ctr = CoseAlgorithms.Aes128Ctr;
    /// <summary>AES-CTR using a 192-bit key.</summary>
    internal const int Aes192Ctr = CoseAlgorithms.Aes192Ctr;
    /// <summary>AES-CTR using a 256-bit key.</summary>
    internal const int Aes256Ctr = CoseAlgorithms.Aes256Ctr;
    /// <summary>AES-CBC using a 128-bit key.</summary>
    internal const int Aes128Cbc = CoseAlgorithms.Aes128Cbc;
    /// <summary>AES-CBC using a 192-bit key.</summary>
    internal const int Aes192Cbc = CoseAlgorithms.Aes192Cbc;
    /// <summary>AES-CBC using a 256-bit key.</summary>
    internal const int Aes256Cbc = CoseAlgorithms.Aes256Cbc;
    /// <summary>ECDSA using brainpoolP512r1 and SHA-512.</summary>
    internal const int EcdsaBrainpoolP512r1Sha512 = CoseAlgorithms.EcdsaBrainpoolP512r1Sha512;
    /// <summary>ECDSA using brainpoolP384r1 and SHA-384.</summary>
    internal const int EcdsaBrainpoolP384r1Sha384 = CoseAlgorithms.EcdsaBrainpoolP384r1Sha384;
    /// <summary>ECDSA using brainpoolP320r1 and SHA-384.</summary>
    internal const int EcdsaBrainpoolP320r1Sha384 = CoseAlgorithms.EcdsaBrainpoolP320r1Sha384;
    /// <summary>ECDSA using brainpoolP256r1 and SHA-256.</summary>
    internal const int EcdsaBrainpoolP256r1Sha256 = CoseAlgorithms.EcdsaBrainpoolP256r1Sha256;
    /// <summary>KangarooTwelve with 256-bit output.</summary>
    internal const int KangarooTwelve256 = CoseAlgorithms.KangarooTwelve256;
    /// <summary>KangarooTwelve with 128-bit output.</summary>
    internal const int KangarooTwelve128 = CoseAlgorithms.KangarooTwelve128;
    /// <summary>TurboSHAKE256.</summary>
    internal const int TurboShake256 = CoseAlgorithms.TurboShake256;
    /// <summary>TurboSHAKE128.</summary>
    internal const int TurboShake128 = CoseAlgorithms.TurboShake128;
    /// <summary>The WalnutDSA signature algorithm.</summary>
    internal const int WalnutDsa = CoseAlgorithms.WalnutDsa;
    /// <summary>Direct use of a shared symmetric key.</summary>
    internal const int Direct = CoseAlgorithms.Direct;
    /// <summary>ECDSA using P-256 and SHA-256.</summary>
    internal const int EcdsaSha256 = CoseAlgorithms.EcdsaSha256;
    /// <summary>The generic EdDSA algorithm.</summary>
    internal const int EdDsa = CoseAlgorithms.EdDsa;
    /// <summary>ECDSA using P-256 and SHA-256 under the current COSE assignment.</summary>
    internal const int EcdsaP256Sha256 = CoseAlgorithms.EcdsaP256Sha256;
    /// <summary>Direct key derivation using HKDF SHA-256.</summary>
    internal const int DirectHkdfSha256 = CoseAlgorithms.DirectHkdfSha256;
    /// <summary>Direct key derivation using HKDF SHA-512.</summary>
    internal const int DirectHkdfSha512 = CoseAlgorithms.DirectHkdfSha512;
    /// <summary>Direct key derivation using HKDF AES-128.</summary>
    internal const int DirectHkdfAes128 = CoseAlgorithms.DirectHkdfAes128;
    /// <summary>Direct key derivation using HKDF AES-256.</summary>
    internal const int DirectHkdfAes256 = CoseAlgorithms.DirectHkdfAes256;
    /// <summary>SHA-1.</summary>
    internal const int Sha1 = CoseAlgorithms.Sha1;
    /// <summary>SHA-256 truncated to 64 bits.</summary>
    internal const int Sha256Truncated64 = CoseAlgorithms.Sha256Truncated64;
    /// <summary>SHA-256.</summary>
    internal const int Sha256 = CoseAlgorithms.Sha256;
    /// <summary>SHA-512 truncated to 256 bits.</summary>
    internal const int Sha512Truncated256 = CoseAlgorithms.Sha512Truncated256;
    /// <summary>SHAKE128.</summary>
    internal const int Shake128 = CoseAlgorithms.Shake128;
    /// <summary>EdDSA using Ed25519.</summary>
    internal const int Ed25519 = CoseAlgorithms.Ed25519;
    /// <summary>ECDH-ES using HKDF SHA-256.</summary>
    internal const int EcdhEsHkdfSha256 = CoseAlgorithms.EcdhEsHkdfSha256;
    /// <summary>ECDH-ES using HKDF SHA-512.</summary>
    internal const int EcdhEsHkdfSha512 = CoseAlgorithms.EcdhEsHkdfSha512;
    /// <summary>ECDH-SS using HKDF SHA-256.</summary>
    internal const int EcdhSsHkdfSha256 = CoseAlgorithms.EcdhSsHkdfSha256;
    /// <summary>ECDH-SS using HKDF SHA-512.</summary>
    internal const int EcdhSsHkdfSha512 = CoseAlgorithms.EcdhSsHkdfSha512;
    /// <summary>ECDH-ES with 128-bit AES Key Wrap.</summary>
    internal const int EcdhEsAes128KeyWrap = CoseAlgorithms.EcdhEsAes128KeyWrap;
    /// <summary>ECDH-ES with 192-bit AES Key Wrap.</summary>
    internal const int EcdhEsAes192KeyWrap = CoseAlgorithms.EcdhEsAes192KeyWrap;
    /// <summary>ECDH-ES with 256-bit AES Key Wrap.</summary>
    internal const int EcdhEsAes256KeyWrap = CoseAlgorithms.EcdhEsAes256KeyWrap;
    /// <summary>ECDH-SS with 128-bit AES Key Wrap.</summary>
    internal const int EcdhSsAes128KeyWrap = CoseAlgorithms.EcdhSsAes128KeyWrap;
    /// <summary>ECDH-SS with 192-bit AES Key Wrap.</summary>
    internal const int EcdhSsAes192KeyWrap = CoseAlgorithms.EcdhSsAes192KeyWrap;
    /// <summary>ECDH-SS with 256-bit AES Key Wrap.</summary>
    internal const int EcdhSsAes256KeyWrap = CoseAlgorithms.EcdhSsAes256KeyWrap;
    /// <summary>ECDSA using P-384 and SHA-384.</summary>
    internal const int EcdsaSha384 = CoseAlgorithms.EcdsaSha384;
    /// <summary>ECDSA using P-521 and SHA-512.</summary>
    internal const int EcdsaSha512 = CoseAlgorithms.EcdsaSha512;
    /// <summary>RSASSA-PSS using SHA-256.</summary>
    internal const int RsaPssSha256 = CoseAlgorithms.RsaPssSha256;
    /// <summary>RSASSA-PSS using SHA-384.</summary>
    internal const int RsaPssSha384 = CoseAlgorithms.RsaPssSha384;
    /// <summary>RSASSA-PSS using SHA-512.</summary>
    internal const int RsaPssSha512 = CoseAlgorithms.RsaPssSha512;
    /// <summary>RSAES-OAEP with RFC 8017 default parameters.</summary>
    internal const int RsaOaep = CoseAlgorithms.RsaOaep;
    /// <summary>RSAES-OAEP using SHA-256.</summary>
    internal const int RsaOaepSha256 = CoseAlgorithms.RsaOaepSha256;
    /// <summary>RSAES-OAEP using SHA-512.</summary>
    internal const int RsaOaepSha512 = CoseAlgorithms.RsaOaepSha512;
    /// <summary>SHA-384.</summary>
    internal const int Sha384 = CoseAlgorithms.Sha384;
    /// <summary>SHA-512.</summary>
    internal const int Sha512 = CoseAlgorithms.Sha512;
    /// <summary>SHAKE256.</summary>
    internal const int Shake256 = CoseAlgorithms.Shake256;
    /// <summary>The HSS/LMS signature algorithm.</summary>
    internal const int HssLms = CoseAlgorithms.HssLms;
    /// <summary>ECDSA using secp256k1 and SHA-256.</summary>
    internal const int EcdsaSecp256k1Sha256 = CoseAlgorithms.EcdsaSecp256k1Sha256;
    /// <summary>ML-DSA-44.</summary>
    internal const int MlDsa44 = CoseAlgorithms.MlDsa44;
    /// <summary>ML-DSA-65.</summary>
    internal const int MlDsa65 = CoseAlgorithms.MlDsa65;
    /// <summary>ML-DSA-87.</summary>
    internal const int MlDsa87 = CoseAlgorithms.MlDsa87;
    /// <summary>ECDSA using P-384 and SHA-384 under the current COSE assignment.</summary>
    internal const int EcdsaP384Sha384 = CoseAlgorithms.EcdsaP384Sha384;
    /// <summary>ECDSA using P-521 and SHA-512 under the current COSE assignment.</summary>
    internal const int EcdsaP521Sha512 = CoseAlgorithms.EcdsaP521Sha512;
    /// <summary>EdDSA using Ed448.</summary>
    internal const int Ed448 = CoseAlgorithms.Ed448;
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-256.</summary>
    internal const int RsaSha256 = CoseAlgorithms.RsaSha256;
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-384.</summary>
    internal const int RsaSha384 = CoseAlgorithms.RsaSha384;
    /// <summary>RSASSA-PKCS1-v1_5 using SHA-512.</summary>
    internal const int RsaSha512 = CoseAlgorithms.RsaSha512;
    /// <summary>AES Key Wrap using a 256-bit key.</summary>
    internal const int Aes256KeyWrap = CoseAlgorithms.Aes256KeyWrap;
    /// <summary>AES Key Wrap using a 192-bit key.</summary>
    internal const int Aes192KeyWrap = CoseAlgorithms.Aes192KeyWrap;
    /// <summary>AES Key Wrap using a 128-bit key.</summary>
    internal const int Aes128KeyWrap = CoseAlgorithms.Aes128KeyWrap;
    /// <summary>AES-GCM using a 128-bit key.</summary>
    internal const int Aes128Gcm = CoseAlgorithms.Aes128Gcm;
    /// <summary>AES-GCM using a 192-bit key.</summary>
    internal const int Aes192Gcm = CoseAlgorithms.Aes192Gcm;
    /// <summary>AES-GCM using a 256-bit key.</summary>
    internal const int Aes256Gcm = CoseAlgorithms.Aes256Gcm;
    /// <summary>HMAC SHA-256 truncated to 64 bits.</summary>
    internal const int HmacSha256Truncated64 = CoseAlgorithms.HmacSha256Truncated64;
    /// <summary>HMAC SHA-256.</summary>
    internal const int HmacSha256 = CoseAlgorithms.HmacSha256;
    /// <summary>HMAC SHA-384.</summary>
    internal const int HmacSha384 = CoseAlgorithms.HmacSha384;
    /// <summary>HMAC SHA-512.</summary>
    internal const int HmacSha512 = CoseAlgorithms.HmacSha512;
    /// <summary>AES-CCM with a 64-bit tag, 13-byte nonce, and 128-bit key.</summary>
    internal const int AesCcm16_64_128 = CoseAlgorithms.AesCcm16_64_128;
    /// <summary>AES-CCM with a 64-bit tag, 13-byte nonce, and 256-bit key.</summary>
    internal const int AesCcm16_64_256 = CoseAlgorithms.AesCcm16_64_256;
    /// <summary>AES-CCM with a 64-bit tag, 7-byte nonce, and 128-bit key.</summary>
    internal const int AesCcm64_64_128 = CoseAlgorithms.AesCcm64_64_128;
    /// <summary>AES-CCM with a 64-bit tag, 7-byte nonce, and 256-bit key.</summary>
    internal const int AesCcm64_64_256 = CoseAlgorithms.AesCcm64_64_256;
    /// <summary>AES-MAC using a 128-bit key and 64-bit tag.</summary>
    internal const int AesMac128_64 = CoseAlgorithms.AesMac128_64;
    /// <summary>AES-MAC using a 256-bit key and 64-bit tag.</summary>
    internal const int AesMac256_64 = CoseAlgorithms.AesMac256_64;
    /// <summary>ChaCha20 with Poly1305.</summary>
    internal const int ChaCha20Poly1305 = CoseAlgorithms.ChaCha20Poly1305;
    /// <summary>AES-MAC using a 128-bit key and 128-bit tag.</summary>
    internal const int AesMac128_128 = CoseAlgorithms.AesMac128_128;
    /// <summary>AES-MAC using a 256-bit key and 128-bit tag.</summary>
    internal const int AesMac256_128 = CoseAlgorithms.AesMac256_128;
    /// <summary>AES-CCM with a 128-bit tag, 13-byte nonce, and 128-bit key.</summary>
    internal const int AesCcm16_128_128 = CoseAlgorithms.AesCcm16_128_128;
    /// <summary>AES-CCM with a 128-bit tag, 13-byte nonce, and 256-bit key.</summary>
    internal const int AesCcm16_128_256 = CoseAlgorithms.AesCcm16_128_256;
    /// <summary>AES-CCM with a 128-bit tag, 7-byte nonce, and 128-bit key.</summary>
    internal const int AesCcm64_128_128 = CoseAlgorithms.AesCcm64_128_128;
    /// <summary>AES-CCM with a 128-bit tag, 7-byte nonce, and 256-bit key.</summary>
    internal const int AesCcm64_128_256 = CoseAlgorithms.AesCcm64_128_256;
    /// <summary>The COSE initialization-vector generation algorithm.</summary>
    internal const int InitializationVectorGeneration = CoseAlgorithms.InitializationVectorGeneration;

    static readonly int[] labels = new int[]
    {
        RsaSha1, Aes128Ctr, Aes192Ctr, Aes256Ctr, Aes128Cbc, Aes192Cbc,
        Aes256Cbc, EcdsaBrainpoolP512r1Sha512,
        EcdsaBrainpoolP384r1Sha384, EcdsaBrainpoolP320r1Sha384,
        EcdsaBrainpoolP256r1Sha256, KangarooTwelve256,
        KangarooTwelve128, TurboShake256, TurboShake128, WalnutDsa, -259, -258,
        -257, -53, -52, -51, -50, -49, -48, -47, -46, -45, -44, -43, -42,
        -41, -40, -39, -38, -37, -36, -35, -34, -33, -32, -31, -30, -29,
        -28, -27, -26, -25, -19, -18, -17, -16, -15, -14, -13, -12, -11,
        -10, -9, -8, -7, -6, Aes256KeyWrap, Aes192KeyWrap, Aes128KeyWrap,
        1, 2, 3, 4, 5, 6, 7, 10, 11,
        12, 13, 14, 15, 24, 25, 26, 30, 31, 32, 33, 34
    };

    static readonly string[] names = new string[]
    {
        "RS1", "A128CTR", "A192CTR", "A256CTR", "A128CBC", "A192CBC",
        "A256CBC", "ESB512", "ESB384", "ESB320", "ESB256", "KT256",
        "KT128", "TurboSHAKE256", "TurboSHAKE128", "WalnutDSA", "RS512",
        "RS384", "RS256", "Ed448", "ESP512", "ESP384", "ML-DSA-87",
        "ML-DSA-65", "ML-DSA-44", "ES256K", "HSS-LMS", "SHAKE256",
        "SHA-512", "SHA-384", "RSAES-OAEP w/ SHA-512",
        "RSAES-OAEP w/ SHA-256", "RSAES-OAEP w/ RFC 8017 default parameters",
        "PS512", "PS384", "PS256", "ES512", "ES384", "ECDH-SS + A256KW",
        "ECDH-SS + A192KW", "ECDH-SS + A128KW", "ECDH-ES + A256KW",
        "ECDH-ES + A192KW", "ECDH-ES + A128KW", "ECDH-SS + HKDF-512",
        "ECDH-SS + HKDF-256", "ECDH-ES + HKDF-512", "ECDH-ES + HKDF-256",
        "Ed25519", "SHAKE128", "SHA-512/256", "SHA-256", "SHA-256/64",
        "SHA-1", "direct+HKDF-AES-256", "direct+HKDF-AES-128",
        "direct+HKDF-SHA-512", "direct+HKDF-SHA-256", "ESP256", "EdDSA",
        "ES256", "direct", "A256KW", "A192KW", "A128KW", "A128GCM",
        "A192GCM", "A256GCM", "HMAC 256/64", "HMAC 256/256",
        "HMAC 384/384", "HMAC 512/512", "AES-CCM-16-64-128",
        "AES-CCM-16-64-256", "AES-CCM-64-64-128", "AES-CCM-64-64-256",
        "AES-MAC 128/64", "AES-MAC 256/64", "ChaCha20/Poly1305",
        "AES-MAC 128/128", "AES-MAC 256/128", "AES-CCM-16-128-128",
        "AES-CCM-16-128-256", "AES-CCM-64-128-128", "AES-CCM-64-128-256",
        "IV-GENERATION"
    };

    /// <summary>Determines whether an integer algorithm label is registered.</summary>
    /// <param name="label">The COSE algorithm label.</param>
    /// <returns><c>true</c> when the label is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(int label)
    {
        return RegistryLookup.Contains(labels, label);
    }

    /// <summary>Gets the registered algorithm name for a label.</summary>
    /// <param name="label">The COSE algorithm label.</param>
    /// <returns>The registered name, or <c>null</c> when the label is unknown.</returns>
    internal static string? GetName(int label)
    {
        int index = RegistryLookup.IndexOf(labels, label);
        if (index < 0)
        {
            return null;
        }

        return names[index];
    }

    /// <summary>Returns all registered COSE algorithm labels.</summary>
    /// <returns>A new array containing the labels.</returns>
    internal static int[] GetAll()
    {
        return RegistryLookup.Copy(labels);
    }
}
