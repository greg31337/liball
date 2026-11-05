using System;
namespace token;

/// <summary>
/// Provides registered JOSE algorithm names.
/// </summary>
public static class JoseAlgorithms
{
    /// <summary>HMAC using SHA-256.</summary>
    public const string HmacSha256 = "HS256";

    /// <summary>HMAC using SHA-384.</summary>
    public const string HmacSha384 = "HS384";

    /// <summary>HMAC using SHA-512.</summary>
    public const string HmacSha512 = "HS512";

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-256.</summary>
    public const string RsaSha256 = "RS256";

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-384.</summary>
    public const string RsaSha384 = "RS384";

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-512.</summary>
    public const string RsaSha512 = "RS512";

    /// <summary>ECDSA using P-256 and SHA-256.</summary>
    public const string EcdsaSha256 = "ES256";

    /// <summary>ECDSA using P-384 and SHA-384.</summary>
    public const string EcdsaSha384 = "ES384";

    /// <summary>ECDSA using P-521 and SHA-512.</summary>
    public const string EcdsaSha512 = "ES512";

    /// <summary>RSASSA-PSS using SHA-256.</summary>
    public const string RsaPssSha256 = "PS256";

    /// <summary>RSASSA-PSS using SHA-384.</summary>
    public const string RsaPssSha384 = "PS384";

    /// <summary>RSASSA-PSS using SHA-512.</summary>
    public const string RsaPssSha512 = "PS512";

    /// <summary>No digital signature or MAC.</summary>
    public const string None = "none";

    /// <summary>RSAES-PKCS1-v1_5 key management.</summary>
    public const string RsaPkcs1 = "RSA1_5";

    /// <summary>RSAES OAEP using the default SHA-1 parameters.</summary>
    public const string RsaOaep = "RSA-OAEP";

    /// <summary>RSAES OAEP using SHA-256.</summary>
    public const string RsaOaepSha256 = "RSA-OAEP-256";

    /// <summary>RSAES OAEP using SHA-384.</summary>
    public const string RsaOaepSha384 = "RSA-OAEP-384";

    /// <summary>RSAES OAEP using SHA-512.</summary>
    public const string RsaOaepSha512 = "RSA-OAEP-512";

    /// <summary>AES Key Wrap using a 128-bit key.</summary>
    public const string Aes128KeyWrap = "A128KW";

    /// <summary>AES Key Wrap using a 192-bit key.</summary>
    public const string Aes192KeyWrap = "A192KW";

    /// <summary>AES Key Wrap using a 256-bit key.</summary>
    public const string Aes256KeyWrap = "A256KW";

    /// <summary>Direct use of a shared symmetric key.</summary>
    public const string Direct = "dir";

    /// <summary>ECDH-ES direct key agreement.</summary>
    public const string EcdhEs = "ECDH-ES";

    /// <summary>ECDH-ES with 128-bit AES Key Wrap.</summary>
    public const string EcdhEsAes128KeyWrap = "ECDH-ES+A128KW";

    /// <summary>ECDH-ES with 192-bit AES Key Wrap.</summary>
    public const string EcdhEsAes192KeyWrap = "ECDH-ES+A192KW";

    /// <summary>ECDH-ES with 256-bit AES Key Wrap.</summary>
    public const string EcdhEsAes256KeyWrap = "ECDH-ES+A256KW";

    /// <summary>AES-GCM key wrapping using a 128-bit key.</summary>
    public const string Aes128GcmKeyWrap = "A128GCMKW";

    /// <summary>AES-GCM key wrapping using a 192-bit key.</summary>
    public const string Aes192GcmKeyWrap = "A192GCMKW";

    /// <summary>AES-GCM key wrapping using a 256-bit key.</summary>
    public const string Aes256GcmKeyWrap = "A256GCMKW";

    /// <summary>PBES2-HS256 with 128-bit AES Key Wrap.</summary>
    public const string Pbes2HmacSha256Aes128KeyWrap = "PBES2-HS256+A128KW";

    /// <summary>PBES2-HS384 with 192-bit AES Key Wrap.</summary>
    public const string Pbes2HmacSha384Aes192KeyWrap = "PBES2-HS384+A192KW";

    /// <summary>PBES2-HS512 with 256-bit AES Key Wrap.</summary>
    public const string Pbes2HmacSha512Aes256KeyWrap = "PBES2-HS512+A256KW";

    /// <summary>AES-128-CBC with HMAC-SHA-256 authenticated encryption.</summary>
    public const string Aes128CbcHmacSha256 = "A128CBC-HS256";

    /// <summary>AES-192-CBC with HMAC-SHA-384 authenticated encryption.</summary>
    public const string Aes192CbcHmacSha384 = "A192CBC-HS384";

    /// <summary>AES-256-CBC with HMAC-SHA-512 authenticated encryption.</summary>
    public const string Aes256CbcHmacSha512 = "A256CBC-HS512";

    /// <summary>AES-GCM content encryption using a 128-bit key.</summary>
    public const string Aes128Gcm = "A128GCM";

    /// <summary>AES-GCM content encryption using a 192-bit key.</summary>
    public const string Aes192Gcm = "A192GCM";

    /// <summary>AES-GCM content encryption using a 256-bit key.</summary>
    public const string Aes256Gcm = "A256GCM";

    /// <summary>The deprecated generic EdDSA algorithm name.</summary>
    public const string EdDsa = "EdDSA";

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-1 for JWK metadata.</summary>
    public const string RsaSha1 = "RS1";

    /// <summary>AES-CBC using a 128-bit key for JWK metadata.</summary>
    public const string Aes128Cbc = "A128CBC";

    /// <summary>AES-CBC using a 192-bit key for JWK metadata.</summary>
    public const string Aes192Cbc = "A192CBC";

    /// <summary>AES-CBC using a 256-bit key for JWK metadata.</summary>
    public const string Aes256Cbc = "A256CBC";

    /// <summary>AES-CTR using a 128-bit key for JWK metadata.</summary>
    public const string Aes128Ctr = "A128CTR";

    /// <summary>AES-CTR using a 192-bit key for JWK metadata.</summary>
    public const string Aes192Ctr = "A192CTR";

    /// <summary>AES-CTR using a 256-bit key for JWK metadata.</summary>
    public const string Aes256Ctr = "A256CTR";

    /// <summary>HMAC using SHA-1 for JWK metadata.</summary>
    public const string HmacSha1 = "HS1";

    /// <summary>ECDSA using secp256k1 and SHA-256.</summary>
    public const string EcdsaSecp256k1Sha256 = "ES256K";

    /// <summary>ML-DSA-44.</summary>
    public const string MlDsa44 = "ML-DSA-44";

    /// <summary>ML-DSA-65.</summary>
    public const string MlDsa65 = "ML-DSA-65";

    /// <summary>ML-DSA-87.</summary>
    public const string MlDsa87 = "ML-DSA-87";

    /// <summary>EdDSA using Ed25519.</summary>
    public const string Ed25519 = "Ed25519";

    /// <summary>EdDSA using Ed448.</summary>
    public const string Ed448 = "Ed448";
}
