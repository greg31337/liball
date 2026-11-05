using System;
namespace token;

/// <summary>
/// Provides security and implementation metadata for registered JOSE and COSE algorithms.
/// </summary>
internal static class AlgorithmMetadataRegistry
{
    static readonly string[] supportedJose = new string[]
    {
        JoseAlgorithms.HmacSha256, JoseAlgorithms.HmacSha384,
        JoseAlgorithms.HmacSha512, JoseAlgorithms.RsaSha256,
        JoseAlgorithms.RsaSha384, JoseAlgorithms.RsaSha512,
        JoseAlgorithms.EcdsaSha256, JoseAlgorithms.EcdsaSha384,
        JoseAlgorithms.EcdsaSha512, JoseAlgorithms.RsaPssSha256,
        JoseAlgorithms.RsaPssSha384, JoseAlgorithms.RsaPssSha512,
        JoseAlgorithms.RsaPkcs1, JoseAlgorithms.RsaOaep,
        JoseAlgorithms.RsaOaepSha256, JoseAlgorithms.RsaOaepSha384,
        JoseAlgorithms.RsaOaepSha512, JoseAlgorithms.Aes128KeyWrap,
        JoseAlgorithms.Aes192KeyWrap, JoseAlgorithms.Aes256KeyWrap,
        JoseAlgorithms.Direct, JoseAlgorithms.EcdhEs,
        JoseAlgorithms.EcdhEsAes128KeyWrap,
        JoseAlgorithms.EcdhEsAes192KeyWrap,
        JoseAlgorithms.EcdhEsAes256KeyWrap,
        JoseAlgorithms.Aes128GcmKeyWrap, JoseAlgorithms.Aes192GcmKeyWrap,
        JoseAlgorithms.Aes256GcmKeyWrap,
        JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
        JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
        JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
        JoseAlgorithms.Aes128CbcHmacSha256,
        JoseAlgorithms.Aes192CbcHmacSha384,
        JoseAlgorithms.Aes256CbcHmacSha512, JoseAlgorithms.Aes128Gcm,
        JoseAlgorithms.Aes192Gcm, JoseAlgorithms.Aes256Gcm
    };

    static readonly string[] recommendedJose = new string[]
    {
        JoseAlgorithms.HmacSha256, JoseAlgorithms.RsaSha256,
        JoseAlgorithms.EcdsaSha256, JoseAlgorithms.RsaOaep,
        JoseAlgorithms.Aes128KeyWrap, JoseAlgorithms.Aes256KeyWrap,
        JoseAlgorithms.Direct, JoseAlgorithms.EcdhEs,
        JoseAlgorithms.EcdhEsAes128KeyWrap,
        JoseAlgorithms.EcdhEsAes256KeyWrap,
        JoseAlgorithms.Aes128CbcHmacSha256,
        JoseAlgorithms.Aes256CbcHmacSha512, JoseAlgorithms.Aes128Gcm,
        JoseAlgorithms.Aes256Gcm
    };

    static readonly string[] deprecatedJose = new string[]
    {
        JoseAlgorithms.EdDsa
    };

    static readonly string[] prohibitedJose = new string[]
    {
        JoseAlgorithms.RsaSha1, JoseAlgorithms.Aes128Cbc,
        JoseAlgorithms.Aes192Cbc, JoseAlgorithms.Aes256Cbc,
        JoseAlgorithms.Aes128Ctr, JoseAlgorithms.Aes192Ctr,
        JoseAlgorithms.Aes256Ctr, JoseAlgorithms.HmacSha1
    };

    static readonly string[] compatibilityOnlyJose = new string[]
    {
        JoseAlgorithms.None, JoseAlgorithms.RsaPkcs1, JoseAlgorithms.EdDsa
    };

    static readonly string[] fullySpecifiedJose = new string[]
    {
        JoseAlgorithms.EcdsaSha256, JoseAlgorithms.EcdsaSha384,
        JoseAlgorithms.EcdsaSha512, JoseAlgorithms.EcdsaSecp256k1Sha256,
        JoseAlgorithms.Ed25519, JoseAlgorithms.Ed448, JoseAlgorithms.MlDsa44,
        JoseAlgorithms.MlDsa65, JoseAlgorithms.MlDsa87,
        JoseAlgorithms.Aes128KeyWrap, JoseAlgorithms.Aes192KeyWrap,
        JoseAlgorithms.Aes256KeyWrap, JoseAlgorithms.Direct,
        JoseAlgorithms.Aes128GcmKeyWrap, JoseAlgorithms.Aes192GcmKeyWrap,
        JoseAlgorithms.Aes256GcmKeyWrap,
        JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
        JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
        JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
        JoseAlgorithms.Aes128CbcHmacSha256,
        JoseAlgorithms.Aes192CbcHmacSha384,
        JoseAlgorithms.Aes256CbcHmacSha512, JoseAlgorithms.Aes128Gcm,
        JoseAlgorithms.Aes192Gcm, JoseAlgorithms.Aes256Gcm
    };

    static readonly int[] supportedCose = new int[]
    {
        CoseAlgorithms.RsaSha512, CoseAlgorithms.RsaSha384,
        CoseAlgorithms.RsaSha256, CoseAlgorithms.EcdsaP521Sha512,
        CoseAlgorithms.EcdsaP384Sha384, CoseAlgorithms.Sha512,
        CoseAlgorithms.Sha384, CoseAlgorithms.RsaOaepSha512,
        CoseAlgorithms.RsaOaepSha256, CoseAlgorithms.RsaOaep,
        CoseAlgorithms.RsaPssSha512, CoseAlgorithms.RsaPssSha384,
        CoseAlgorithms.RsaPssSha256, CoseAlgorithms.EcdsaSha512,
        CoseAlgorithms.EcdsaSha384, CoseAlgorithms.EcdhSsAes256KeyWrap,
        CoseAlgorithms.EcdhSsAes192KeyWrap,
        CoseAlgorithms.EcdhSsAes128KeyWrap,
        CoseAlgorithms.EcdhEsAes256KeyWrap,
        CoseAlgorithms.EcdhEsAes192KeyWrap,
        CoseAlgorithms.EcdhEsAes128KeyWrap,
        CoseAlgorithms.EcdhSsHkdfSha512, CoseAlgorithms.EcdhSsHkdfSha256,
        CoseAlgorithms.EcdhEsHkdfSha512, CoseAlgorithms.EcdhEsHkdfSha256,
        CoseAlgorithms.Sha256Truncated64, CoseAlgorithms.Sha256,
        CoseAlgorithms.Sha1, CoseAlgorithms.DirectHkdfAes256,
        CoseAlgorithms.DirectHkdfAes128, CoseAlgorithms.DirectHkdfSha512,
        CoseAlgorithms.DirectHkdfSha256,
        CoseAlgorithms.EcdsaP256Sha256,
        CoseAlgorithms.EcdsaSha256, CoseAlgorithms.Direct,
        CoseAlgorithms.Aes256KeyWrap, CoseAlgorithms.Aes192KeyWrap,
        CoseAlgorithms.Aes128KeyWrap, CoseAlgorithms.Aes128Gcm,
        CoseAlgorithms.Aes192Gcm, CoseAlgorithms.Aes256Gcm,
        CoseAlgorithms.HmacSha256Truncated64, CoseAlgorithms.HmacSha256,
        CoseAlgorithms.HmacSha384, CoseAlgorithms.HmacSha512,
        CoseAlgorithms.AesCcm16_64_128, CoseAlgorithms.AesCcm16_64_256,
        CoseAlgorithms.AesCcm64_64_128, CoseAlgorithms.AesCcm64_64_256,
        CoseAlgorithms.ChaCha20Poly1305, CoseAlgorithms.AesCcm16_128_128,
        CoseAlgorithms.AesCcm16_128_256, CoseAlgorithms.AesCcm64_128_128,
        CoseAlgorithms.AesCcm64_128_256
    };

    static readonly int[] recommendedCose = new int[]
    {
        CoseAlgorithms.Ed448, CoseAlgorithms.EcdsaP521Sha512,
        CoseAlgorithms.EcdsaP384Sha384, CoseAlgorithms.MlDsa87,
        CoseAlgorithms.MlDsa65, CoseAlgorithms.MlDsa44, CoseAlgorithms.HssLms,
        CoseAlgorithms.Shake256, CoseAlgorithms.Sha512, CoseAlgorithms.Sha384,
        CoseAlgorithms.RsaOaepSha512, CoseAlgorithms.RsaOaepSha256,
        CoseAlgorithms.RsaOaep, CoseAlgorithms.RsaPssSha512,
        CoseAlgorithms.RsaPssSha384, CoseAlgorithms.RsaPssSha256,
        CoseAlgorithms.EcdhSsAes256KeyWrap,
        CoseAlgorithms.EcdhSsAes192KeyWrap,
        CoseAlgorithms.EcdhSsAes128KeyWrap,
        CoseAlgorithms.EcdhEsAes256KeyWrap,
        CoseAlgorithms.EcdhEsAes192KeyWrap,
        CoseAlgorithms.EcdhEsAes128KeyWrap,
        CoseAlgorithms.EcdhSsHkdfSha512, CoseAlgorithms.EcdhSsHkdfSha256,
        CoseAlgorithms.EcdhEsHkdfSha512, CoseAlgorithms.EcdhEsHkdfSha256,
        CoseAlgorithms.Ed25519, CoseAlgorithms.Shake128,
        CoseAlgorithms.Sha512Truncated256, CoseAlgorithms.Sha256,
        CoseAlgorithms.DirectHkdfAes256, CoseAlgorithms.DirectHkdfAes128,
        CoseAlgorithms.DirectHkdfSha512, CoseAlgorithms.DirectHkdfSha256,
        CoseAlgorithms.EcdsaP256Sha256, CoseAlgorithms.Direct,
        CoseAlgorithms.Aes256KeyWrap, CoseAlgorithms.Aes192KeyWrap,
        CoseAlgorithms.Aes128KeyWrap, CoseAlgorithms.Aes128Gcm,
        CoseAlgorithms.Aes192Gcm, CoseAlgorithms.Aes256Gcm,
        CoseAlgorithms.HmacSha256Truncated64, CoseAlgorithms.HmacSha256,
        CoseAlgorithms.HmacSha384, CoseAlgorithms.HmacSha512,
        CoseAlgorithms.AesCcm16_64_128, CoseAlgorithms.AesCcm16_64_256,
        CoseAlgorithms.AesCcm64_64_128, CoseAlgorithms.AesCcm64_64_256,
        CoseAlgorithms.AesMac128_64, CoseAlgorithms.AesMac256_64,
        CoseAlgorithms.ChaCha20Poly1305, CoseAlgorithms.AesMac128_128,
        CoseAlgorithms.AesMac256_128, CoseAlgorithms.AesCcm16_128_128,
        CoseAlgorithms.AesCcm16_128_256, CoseAlgorithms.AesCcm64_128_128,
        CoseAlgorithms.AesCcm64_128_256
    };

    static readonly int[] deprecatedCose = new int[]
    {
        CoseAlgorithms.RsaSha1, CoseAlgorithms.Aes128Ctr,
        CoseAlgorithms.Aes192Ctr, CoseAlgorithms.Aes256Ctr,
        CoseAlgorithms.Aes128Cbc, CoseAlgorithms.Aes192Cbc,
        CoseAlgorithms.Aes256Cbc, CoseAlgorithms.EcdsaSha512,
        CoseAlgorithms.EcdsaSha384, CoseAlgorithms.EdDsa,
        CoseAlgorithms.EcdsaSha256
    };

    static readonly int[] compatibilityOnlyCose = new int[]
    {
        CoseAlgorithms.RsaSha1, CoseAlgorithms.Aes128Ctr,
        CoseAlgorithms.Aes192Ctr, CoseAlgorithms.Aes256Ctr,
        CoseAlgorithms.Aes128Cbc, CoseAlgorithms.Aes192Cbc,
        CoseAlgorithms.Aes256Cbc, CoseAlgorithms.EcdsaSha512,
        CoseAlgorithms.EcdsaSha384, CoseAlgorithms.Sha256Truncated64,
        CoseAlgorithms.Sha1, CoseAlgorithms.EdDsa,
        CoseAlgorithms.EcdsaSha256
    };

    static readonly int[] fullySpecifiedCose = new int[]
    {
        CoseAlgorithms.EcdsaBrainpoolP512r1Sha512,
        CoseAlgorithms.EcdsaBrainpoolP384r1Sha384,
        CoseAlgorithms.EcdsaBrainpoolP320r1Sha384,
        CoseAlgorithms.EcdsaBrainpoolP256r1Sha256,
        CoseAlgorithms.EcdsaP256Sha256, CoseAlgorithms.EcdsaP384Sha384,
        CoseAlgorithms.EcdsaP521Sha512,
        CoseAlgorithms.EcdsaSecp256k1Sha256, CoseAlgorithms.Ed25519,
        CoseAlgorithms.Ed448, CoseAlgorithms.MlDsa44, CoseAlgorithms.MlDsa65,
        CoseAlgorithms.MlDsa87, CoseAlgorithms.Aes128KeyWrap,
        CoseAlgorithms.Aes192KeyWrap, CoseAlgorithms.Aes256KeyWrap,
        CoseAlgorithms.Direct, CoseAlgorithms.DirectHkdfSha256,
        CoseAlgorithms.DirectHkdfSha512, CoseAlgorithms.DirectHkdfAes128,
        CoseAlgorithms.DirectHkdfAes256, CoseAlgorithms.Aes128Gcm,
        CoseAlgorithms.Aes192Gcm, CoseAlgorithms.Aes256Gcm,
        CoseAlgorithms.HmacSha256Truncated64, CoseAlgorithms.HmacSha256,
        CoseAlgorithms.HmacSha384, CoseAlgorithms.HmacSha512,
        CoseAlgorithms.AesCcm16_64_128, CoseAlgorithms.AesCcm16_64_256,
        CoseAlgorithms.AesCcm64_64_128, CoseAlgorithms.AesCcm64_64_256,
        CoseAlgorithms.AesMac128_64, CoseAlgorithms.AesMac256_64,
        CoseAlgorithms.ChaCha20Poly1305, CoseAlgorithms.AesMac128_128,
        CoseAlgorithms.AesMac256_128, CoseAlgorithms.AesCcm16_128_128,
        CoseAlgorithms.AesCcm16_128_256, CoseAlgorithms.AesCcm64_128_128,
        CoseAlgorithms.AesCcm64_128_256, CoseAlgorithms.Sha1,
        CoseAlgorithms.Sha256Truncated64, CoseAlgorithms.Sha256,
        CoseAlgorithms.Sha512Truncated256, CoseAlgorithms.Sha384,
        CoseAlgorithms.Sha512, CoseAlgorithms.Shake128,
        CoseAlgorithms.Shake256
    };

    /// <summary>Gets metadata for a case-sensitive JOSE algorithm name.</summary>
    /// <param name="name">The algorithm name to inspect.</param>
    /// <returns>A new metadata object; unknown names have <see cref="AlgorithmMetadata.Known"/> set to <c>false</c>.</returns>
    internal static AlgorithmMetadata GetJose(string name)
    {
        bool known = JoseAlgorithmsOperations.IsRegistered(name);
        return new AlgorithmMetadata(
            name,
            0,
            false,
            known,
            ContainsKnown(supportedJose, name, known),
            ContainsKnown(recommendedJose, name, known),
            ContainsKnown(deprecatedJose, name, known),
            ContainsKnown(prohibitedJose, name, known),
            ContainsKnown(compatibilityOnlyJose, name, known),
            ContainsKnown(fullySpecifiedJose, name, known));
    }

    /// <summary>Gets metadata for a COSE integer algorithm label.</summary>
    /// <param name="label">The algorithm label to inspect.</param>
    /// <returns>A new metadata object; unknown labels have <see cref="AlgorithmMetadata.Known"/> set to <c>false</c>.</returns>
    internal static AlgorithmMetadata GetCose(int label)
    {
        bool known = CoseAlgorithmsOperations.IsRegistered(label);
        return new AlgorithmMetadata(
            CoseAlgorithmsOperations.GetName(label),
            label,
            true,
            known,
            ContainsKnown(supportedCose, label, known),
            ContainsKnown(recommendedCose, label, known),
            ContainsKnown(deprecatedCose, label, known),
            false,
            ContainsKnown(compatibilityOnlyCose, label, known),
            ContainsKnown(fullySpecifiedCose, label, known));
    }

    /// <summary>Returns JOSE identifiers classified as fully specified.</summary>
    /// <returns>A new array containing the case-sensitive algorithm names.</returns>
    internal static string[] GetFullySpecifiedJose()
    {
        return RegistryLookup.Copy(fullySpecifiedJose);
    }

    /// <summary>Returns COSE identifiers classified as fully specified.</summary>
    /// <returns>A new array containing the algorithm labels.</returns>
    internal static int[] GetFullySpecifiedCose()
    {
        return RegistryLookup.Copy(fullySpecifiedCose);
    }

    static bool ContainsKnown(string[] values, string value, bool known)
    {
        if (!known)
        {
            return false;
        }

        return RegistryLookup.Contains(values, value, false);
    }

    static bool ContainsKnown(int[] values, int value, bool known)
    {
        if (!known)
        {
            return false;
        }

        return RegistryLookup.Contains(values, value);
    }
}
