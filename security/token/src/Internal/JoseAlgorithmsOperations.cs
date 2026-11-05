using System;
namespace token;

/// <summary>
/// Provides registered JOSE algorithm names and usage-category lookups.
/// </summary>
internal static class JoseAlgorithmsOperations
{
    /// <summary>HMAC using SHA-256.</summary>
    internal const string HmacSha256 = JoseAlgorithms.HmacSha256;

    /// <summary>HMAC using SHA-384.</summary>
    internal const string HmacSha384 = JoseAlgorithms.HmacSha384;

    /// <summary>HMAC using SHA-512.</summary>
    internal const string HmacSha512 = JoseAlgorithms.HmacSha512;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-256.</summary>
    internal const string RsaSha256 = JoseAlgorithms.RsaSha256;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-384.</summary>
    internal const string RsaSha384 = JoseAlgorithms.RsaSha384;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-512.</summary>
    internal const string RsaSha512 = JoseAlgorithms.RsaSha512;

    /// <summary>ECDSA using P-256 and SHA-256.</summary>
    internal const string EcdsaSha256 = JoseAlgorithms.EcdsaSha256;

    /// <summary>ECDSA using P-384 and SHA-384.</summary>
    internal const string EcdsaSha384 = JoseAlgorithms.EcdsaSha384;

    /// <summary>ECDSA using P-521 and SHA-512.</summary>
    internal const string EcdsaSha512 = JoseAlgorithms.EcdsaSha512;

    /// <summary>RSASSA-PSS using SHA-256.</summary>
    internal const string RsaPssSha256 = JoseAlgorithms.RsaPssSha256;

    /// <summary>RSASSA-PSS using SHA-384.</summary>
    internal const string RsaPssSha384 = JoseAlgorithms.RsaPssSha384;

    /// <summary>RSASSA-PSS using SHA-512.</summary>
    internal const string RsaPssSha512 = JoseAlgorithms.RsaPssSha512;

    /// <summary>No digital signature or MAC.</summary>
    internal const string None = JoseAlgorithms.None;

    /// <summary>RSAES-PKCS1-v1_5 key management.</summary>
    internal const string RsaPkcs1 = JoseAlgorithms.RsaPkcs1;

    /// <summary>RSAES OAEP using the default SHA-1 parameters.</summary>
    internal const string RsaOaep = JoseAlgorithms.RsaOaep;

    /// <summary>RSAES OAEP using SHA-256.</summary>
    internal const string RsaOaepSha256 = JoseAlgorithms.RsaOaepSha256;

    /// <summary>RSAES OAEP using SHA-384.</summary>
    internal const string RsaOaepSha384 = JoseAlgorithms.RsaOaepSha384;

    /// <summary>RSAES OAEP using SHA-512.</summary>
    internal const string RsaOaepSha512 = JoseAlgorithms.RsaOaepSha512;

    /// <summary>AES Key Wrap using a 128-bit key.</summary>
    internal const string Aes128KeyWrap = JoseAlgorithms.Aes128KeyWrap;

    /// <summary>AES Key Wrap using a 192-bit key.</summary>
    internal const string Aes192KeyWrap = JoseAlgorithms.Aes192KeyWrap;

    /// <summary>AES Key Wrap using a 256-bit key.</summary>
    internal const string Aes256KeyWrap = JoseAlgorithms.Aes256KeyWrap;

    /// <summary>Direct use of a shared symmetric key.</summary>
    internal const string Direct = JoseAlgorithms.Direct;

    /// <summary>ECDH-ES direct key agreement.</summary>
    internal const string EcdhEs = JoseAlgorithms.EcdhEs;

    /// <summary>ECDH-ES with 128-bit AES Key Wrap.</summary>
    internal const string EcdhEsAes128KeyWrap = JoseAlgorithms.EcdhEsAes128KeyWrap;

    /// <summary>ECDH-ES with 192-bit AES Key Wrap.</summary>
    internal const string EcdhEsAes192KeyWrap = JoseAlgorithms.EcdhEsAes192KeyWrap;

    /// <summary>ECDH-ES with 256-bit AES Key Wrap.</summary>
    internal const string EcdhEsAes256KeyWrap = JoseAlgorithms.EcdhEsAes256KeyWrap;

    /// <summary>AES-GCM key wrapping using a 128-bit key.</summary>
    internal const string Aes128GcmKeyWrap = JoseAlgorithms.Aes128GcmKeyWrap;

    /// <summary>AES-GCM key wrapping using a 192-bit key.</summary>
    internal const string Aes192GcmKeyWrap = JoseAlgorithms.Aes192GcmKeyWrap;

    /// <summary>AES-GCM key wrapping using a 256-bit key.</summary>
    internal const string Aes256GcmKeyWrap = JoseAlgorithms.Aes256GcmKeyWrap;

    /// <summary>PBES2-HS256 with 128-bit AES Key Wrap.</summary>
    internal const string Pbes2HmacSha256Aes128KeyWrap = JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap;

    /// <summary>PBES2-HS384 with 192-bit AES Key Wrap.</summary>
    internal const string Pbes2HmacSha384Aes192KeyWrap = JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap;

    /// <summary>PBES2-HS512 with 256-bit AES Key Wrap.</summary>
    internal const string Pbes2HmacSha512Aes256KeyWrap = JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap;

    /// <summary>AES-128-CBC with HMAC-SHA-256 authenticated encryption.</summary>
    internal const string Aes128CbcHmacSha256 = JoseAlgorithms.Aes128CbcHmacSha256;

    /// <summary>AES-192-CBC with HMAC-SHA-384 authenticated encryption.</summary>
    internal const string Aes192CbcHmacSha384 = JoseAlgorithms.Aes192CbcHmacSha384;

    /// <summary>AES-256-CBC with HMAC-SHA-512 authenticated encryption.</summary>
    internal const string Aes256CbcHmacSha512 = JoseAlgorithms.Aes256CbcHmacSha512;

    /// <summary>AES-GCM content encryption using a 128-bit key.</summary>
    internal const string Aes128Gcm = JoseAlgorithms.Aes128Gcm;

    /// <summary>AES-GCM content encryption using a 192-bit key.</summary>
    internal const string Aes192Gcm = JoseAlgorithms.Aes192Gcm;

    /// <summary>AES-GCM content encryption using a 256-bit key.</summary>
    internal const string Aes256Gcm = JoseAlgorithms.Aes256Gcm;

    /// <summary>The deprecated generic EdDSA algorithm name.</summary>
    internal const string EdDsa = JoseAlgorithms.EdDsa;

    /// <summary>RSASSA-PKCS1-v1_5 using SHA-1 for JWK metadata.</summary>
    internal const string RsaSha1 = JoseAlgorithms.RsaSha1;

    /// <summary>AES-CBC using a 128-bit key for JWK metadata.</summary>
    internal const string Aes128Cbc = JoseAlgorithms.Aes128Cbc;

    /// <summary>AES-CBC using a 192-bit key for JWK metadata.</summary>
    internal const string Aes192Cbc = JoseAlgorithms.Aes192Cbc;

    /// <summary>AES-CBC using a 256-bit key for JWK metadata.</summary>
    internal const string Aes256Cbc = JoseAlgorithms.Aes256Cbc;

    /// <summary>AES-CTR using a 128-bit key for JWK metadata.</summary>
    internal const string Aes128Ctr = JoseAlgorithms.Aes128Ctr;

    /// <summary>AES-CTR using a 192-bit key for JWK metadata.</summary>
    internal const string Aes192Ctr = JoseAlgorithms.Aes192Ctr;

    /// <summary>AES-CTR using a 256-bit key for JWK metadata.</summary>
    internal const string Aes256Ctr = JoseAlgorithms.Aes256Ctr;

    /// <summary>HMAC using SHA-1 for JWK metadata.</summary>
    internal const string HmacSha1 = JoseAlgorithms.HmacSha1;

    /// <summary>ECDSA using secp256k1 and SHA-256.</summary>
    internal const string EcdsaSecp256k1Sha256 = JoseAlgorithms.EcdsaSecp256k1Sha256;

    /// <summary>ML-DSA-44.</summary>
    internal const string MlDsa44 = JoseAlgorithms.MlDsa44;

    /// <summary>ML-DSA-65.</summary>
    internal const string MlDsa65 = JoseAlgorithms.MlDsa65;

    /// <summary>ML-DSA-87.</summary>
    internal const string MlDsa87 = JoseAlgorithms.MlDsa87;

    /// <summary>EdDSA using Ed25519.</summary>
    internal const string Ed25519 = JoseAlgorithms.Ed25519;

    /// <summary>EdDSA using Ed448.</summary>
    internal const string Ed448 = JoseAlgorithms.Ed448;

    static readonly string[] signature = new string[]
    {
        HmacSha256, HmacSha384, HmacSha512, RsaSha256, RsaSha384, RsaSha512,
        EcdsaSha256, EcdsaSha384, EcdsaSha512, RsaPssSha256, RsaPssSha384,
        RsaPssSha512, None, EdDsa, EcdsaSecp256k1Sha256, MlDsa44, MlDsa65,
        MlDsa87, Ed25519, Ed448
    };

    static readonly string[] keyManagement = new string[]
    {
        RsaPkcs1, RsaOaep, RsaOaepSha256, RsaOaepSha384, RsaOaepSha512,
        Aes128KeyWrap, Aes192KeyWrap, Aes256KeyWrap, Direct, EcdhEs,
        EcdhEsAes128KeyWrap, EcdhEsAes192KeyWrap, EcdhEsAes256KeyWrap,
        Aes128GcmKeyWrap, Aes192GcmKeyWrap, Aes256GcmKeyWrap,
        Pbes2HmacSha256Aes128KeyWrap, Pbes2HmacSha384Aes192KeyWrap,
        Pbes2HmacSha512Aes256KeyWrap
    };

    static readonly string[] contentEncryption = new string[]
    {
        Aes128CbcHmacSha256, Aes192CbcHmacSha384, Aes256CbcHmacSha512,
        Aes128Gcm, Aes192Gcm, Aes256Gcm
    };

    static readonly string[] jwkOnly = new string[]
    {
        RsaSha1, Aes128Cbc, Aes192Cbc, Aes256Cbc, Aes128Ctr, Aes192Ctr,
        Aes256Ctr, HmacSha1
    };

    static readonly string[] all = Combine();

    /// <summary>
    /// Determines whether an exact, case-sensitive algorithm name is registered.
    /// </summary>
    /// <param name="name">The algorithm name.</param>
    /// <returns><c>true</c> when the name is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(string name)
    {
        return RegistryLookup.Contains(all, name, false);
    }

    /// <summary>
    /// Determines whether an algorithm is registered for JWS signatures or MACs.
    /// </summary>
    /// <param name="name">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWS algorithm; otherwise, <c>false</c>.</returns>
    internal static bool IsSignature(string name)
    {
        return RegistryLookup.Contains(signature, name, false);
    }

    /// <summary>
    /// Determines whether an algorithm is registered for JWE key management.
    /// </summary>
    /// <param name="name">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a key-management algorithm; otherwise, <c>false</c>.</returns>
    internal static bool IsKeyManagement(string name)
    {
        return RegistryLookup.Contains(keyManagement, name, false);
    }

    /// <summary>
    /// Determines whether an algorithm is registered for JWE content encryption.
    /// </summary>
    /// <param name="name">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a content-encryption algorithm; otherwise, <c>false</c>.</returns>
    internal static bool IsContentEncryption(string name)
    {
        return RegistryLookup.Contains(contentEncryption, name, false);
    }

    /// <summary>
    /// Determines whether an algorithm name is registered only for JWK metadata.
    /// </summary>
    /// <param name="name">The case-sensitive algorithm name.</param>
    /// <returns><c>true</c> for a JWK-only name; otherwise, <c>false</c>.</returns>
    internal static bool IsJwkOnly(string name)
    {
        return RegistryLookup.Contains(jwkOnly, name, false);
    }

    /// <summary>
    /// Returns all registered JOSE algorithm names known by this library.
    /// </summary>
    /// <returns>A new array containing the algorithm names.</returns>
    internal static string[] GetAll()
    {
        return RegistryLookup.Copy(all);
    }

    static string[] Combine()
    {
        string[] result = new string[
            signature.Length + keyManagement.Length + contentEncryption.Length + jwkOnly.Length];
        int resultIndex = 0;
        resultIndex = CopyTo(signature, result, resultIndex);
        resultIndex = CopyTo(keyManagement, result, resultIndex);
        resultIndex = CopyTo(contentEncryption, result, resultIndex);
        CopyTo(jwkOnly, result, resultIndex);
        return result;
    }

    static int CopyTo(string[] source, string[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationIndex] = source[index];
            destinationIndex += 1;
            index += 1;
        }

        return destinationIndex;
    }
}
