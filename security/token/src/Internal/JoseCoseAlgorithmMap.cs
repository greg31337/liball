using System;
namespace token;

/// <summary>
/// Maps semantically equivalent JOSE algorithm names and COSE algorithm labels.
/// </summary>
internal static class JoseCoseAlgorithmMap
{
    static readonly string[] joseNames = new string[]
    {
        JoseAlgorithms.HmacSha256, JoseAlgorithms.HmacSha384,
        JoseAlgorithms.HmacSha512, JoseAlgorithms.RsaSha256,
        JoseAlgorithms.RsaSha384, JoseAlgorithms.RsaSha512,
        JoseAlgorithms.EcdsaSha256, JoseAlgorithms.EcdsaSha384,
        JoseAlgorithms.EcdsaSha512, JoseAlgorithms.RsaPssSha256,
        JoseAlgorithms.RsaPssSha384, JoseAlgorithms.RsaPssSha512,
        JoseAlgorithms.RsaOaep, JoseAlgorithms.RsaOaepSha256,
        JoseAlgorithms.RsaOaepSha512, JoseAlgorithms.Aes128KeyWrap,
        JoseAlgorithms.Aes192KeyWrap, JoseAlgorithms.Aes256KeyWrap,
        JoseAlgorithms.Direct, JoseAlgorithms.EcdhEsAes128KeyWrap,
        JoseAlgorithms.EcdhEsAes192KeyWrap,
        JoseAlgorithms.EcdhEsAes256KeyWrap, JoseAlgorithms.Aes128Gcm,
        JoseAlgorithms.Aes192Gcm, JoseAlgorithms.Aes256Gcm,
        JoseAlgorithms.EdDsa, JoseAlgorithms.RsaSha1, JoseAlgorithms.Aes128Cbc,
        JoseAlgorithms.Aes192Cbc, JoseAlgorithms.Aes256Cbc,
        JoseAlgorithms.Aes128Ctr, JoseAlgorithms.Aes192Ctr,
        JoseAlgorithms.Aes256Ctr, JoseAlgorithms.EcdsaSecp256k1Sha256,
        JoseAlgorithms.MlDsa44, JoseAlgorithms.MlDsa65, JoseAlgorithms.MlDsa87,
        JoseAlgorithms.Ed25519, JoseAlgorithms.Ed448
    };

    static readonly int[] coseLabels = new int[]
    {
        CoseAlgorithms.HmacSha256, CoseAlgorithms.HmacSha384,
        CoseAlgorithms.HmacSha512, CoseAlgorithms.RsaSha256,
        CoseAlgorithms.RsaSha384, CoseAlgorithms.RsaSha512,
        CoseAlgorithms.EcdsaP256Sha256, CoseAlgorithms.EcdsaP384Sha384,
        CoseAlgorithms.EcdsaP521Sha512, CoseAlgorithms.RsaPssSha256,
        CoseAlgorithms.RsaPssSha384, CoseAlgorithms.RsaPssSha512,
        CoseAlgorithms.RsaOaep, CoseAlgorithms.RsaOaepSha256,
        CoseAlgorithms.RsaOaepSha512, CoseAlgorithms.Aes128KeyWrap,
        CoseAlgorithms.Aes192KeyWrap, CoseAlgorithms.Aes256KeyWrap,
        CoseAlgorithms.Direct, CoseAlgorithms.EcdhEsAes128KeyWrap,
        CoseAlgorithms.EcdhEsAes192KeyWrap,
        CoseAlgorithms.EcdhEsAes256KeyWrap, CoseAlgorithms.Aes128Gcm,
        CoseAlgorithms.Aes192Gcm, CoseAlgorithms.Aes256Gcm,
        CoseAlgorithms.EdDsa, CoseAlgorithms.RsaSha1, CoseAlgorithms.Aes128Cbc,
        CoseAlgorithms.Aes192Cbc, CoseAlgorithms.Aes256Cbc,
        CoseAlgorithms.Aes128Ctr, CoseAlgorithms.Aes192Ctr,
        CoseAlgorithms.Aes256Ctr, CoseAlgorithms.EcdsaSecp256k1Sha256,
        CoseAlgorithms.MlDsa44, CoseAlgorithms.MlDsa65, CoseAlgorithms.MlDsa87,
        CoseAlgorithms.Ed25519, CoseAlgorithms.Ed448
    };

    static readonly string[] legacyJoseNames = new string[]
    {
        JoseAlgorithms.EcdsaSha256, JoseAlgorithms.EcdsaSha384,
        JoseAlgorithms.EcdsaSha512
    };

    static readonly int[] legacyCoseLabels = new int[]
    {
        CoseAlgorithms.EcdsaSha256, CoseAlgorithms.EcdsaSha384,
        CoseAlgorithms.EcdsaSha512
    };

    /// <summary>Maps a case-sensitive JOSE algorithm name to its canonical COSE label.</summary>
    /// <param name="joseName">The JOSE algorithm name.</param>
    /// <returns>A mapping result. Fully specified COSE labels are preferred over deprecated polymorphic labels.</returns>
    internal static AlgorithmMappingResult FromJose(string joseName)
    {
        int index = IndexOf(joseNames, joseName);
        if (index < 0)
        {
            return new AlgorithmMappingResult(false, joseName, 0);
        }

        return new AlgorithmMappingResult(true, joseNames[index], coseLabels[index]);
    }

    /// <summary>Maps a COSE algorithm label to its equivalent JOSE algorithm name.</summary>
    /// <param name="coseLabel">The COSE algorithm label.</param>
    /// <returns>A mapping result that also recognizes deprecated COSE ECDSA labels.</returns>
    internal static AlgorithmMappingResult FromCose(int coseLabel)
    {
        int index = RegistryLookup.IndexOf(coseLabels, coseLabel);
        if (index >= 0)
        {
            return new AlgorithmMappingResult(true, joseNames[index], coseLabels[index]);
        }

        index = RegistryLookup.IndexOf(legacyCoseLabels, coseLabel);
        if (index >= 0)
        {
            return new AlgorithmMappingResult(
                true,
                legacyJoseNames[index],
                legacyCoseLabels[index]);
        }

        return new AlgorithmMappingResult(false, null, coseLabel);
    }

    /// <summary>Returns every exact COSE mapping for a JOSE algorithm name.</summary>
    /// <param name="joseName">The case-sensitive JOSE algorithm name.</param>
    /// <returns>A new array with the canonical label first, followed by recognized legacy labels.</returns>
    internal static int[] GetCoseLabels(string joseName)
    {
        int count = 0;
        int index = 0;
        while (index < joseNames.Length)
        {
            if (string.Equals(joseNames[index], joseName, StringComparison.Ordinal))
            {
                count += 1;
            }

            index += 1;
        }

        index = 0;
        while (index < legacyJoseNames.Length)
        {
            if (string.Equals(legacyJoseNames[index], joseName, StringComparison.Ordinal))
            {
                count += 1;
            }

            index += 1;
        }

        int[] result = new int[count];
        int resultIndex = 0;
        index = 0;
        while (index < joseNames.Length)
        {
            if (string.Equals(joseNames[index], joseName, StringComparison.Ordinal))
            {
                result[resultIndex] = coseLabels[index];
                resultIndex += 1;
            }

            index += 1;
        }

        index = 0;
        while (index < legacyJoseNames.Length)
        {
            if (string.Equals(legacyJoseNames[index], joseName, StringComparison.Ordinal))
            {
                result[resultIndex] = legacyCoseLabels[index];
                resultIndex += 1;
            }

            index += 1;
        }

        return result;
    }

    /// <summary>Returns all JOSE algorithms with a canonical COSE mapping.</summary>
    /// <returns>A new array containing the case-sensitive JOSE names.</returns>
    internal static string[] GetJoseAlgorithms()
    {
        return RegistryLookup.Copy(joseNames);
    }

    /// <summary>Returns all canonical COSE algorithm labels mapped from JOSE.</summary>
    /// <returns>A new array aligned by index with <see cref="GetJoseAlgorithms"/>.</returns>
    internal static int[] GetCanonicalCoseAlgorithms()
    {
        return RegistryLookup.Copy(coseLabels);
    }

    static int IndexOf(string[] values, string value)
    {
        if (value == null)
        {
            return -1;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }
}
