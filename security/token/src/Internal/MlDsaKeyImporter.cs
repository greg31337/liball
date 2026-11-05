using System;
using System.Security.Cryptography;

namespace token;

internal static class MlDsaKeyImporter
{
    internal static TokenKeyResult ImportPublic(byte[] publicKey, string algorithm)
    {
        MLDsaAlgorithm? selected = GetAlgorithm(algorithm);
        if (selected == null)
        {
            return Failure("unsupported_algorithm", "The ML-DSA algorithm is not supported.");
        }

        if (!MLDsa.IsSupported)
        {
            return Failure("unsupported_algorithm", "ML-DSA has no usable built-in platform implementation.");
        }

        if (publicKey.Length != selected.PublicKeySizeInBytes)
        {
            return Failure("invalid_mldsa_key", "The ML-DSA public key has an invalid length.");
        }

        using (MLDsa key = MLDsa.ImportMLDsaPublicKey(selected, publicKey))
        {
            return Create(key.ExportMLDsaPublicKey(), null, algorithm);
        }
    }

    internal static TokenKeyResult ImportPrivateSeed(byte[] privateSeed, string algorithm)
    {
        MLDsaAlgorithm? selected = GetAlgorithm(algorithm);
        if (selected == null)
        {
            return Failure("unsupported_algorithm", "The ML-DSA algorithm is not supported.");
        }

        if (!MLDsa.IsSupported)
        {
            return Failure("unsupported_algorithm", "ML-DSA has no usable built-in platform implementation.");
        }

        if (privateSeed.Length != selected.PrivateSeedSizeInBytes)
        {
            return Failure("invalid_mldsa_key", "The ML-DSA private seed has an invalid length.");
        }

        using (MLDsa key = MLDsa.ImportMLDsaPrivateSeed(selected, privateSeed))
        {
            return Create(
                key.ExportMLDsaPublicKey(),
                key.ExportMLDsaPrivateSeed(),
                algorithm);
        }
    }

    internal static TokenKeyResult Read(
        byte[] publicKey,
        byte[]? privateSeed,
        string algorithm)
    {
        MLDsaAlgorithm? selected = GetAlgorithm(algorithm);
        if (selected == null)
        {
            return Failure("unsupported_algorithm", "The ML-DSA algorithm is not supported.");
        }

        if (publicKey.Length != selected.PublicKeySizeInBytes)
        {
            return Failure("invalid_mldsa_key", "The ML-DSA public key has an invalid length.");
        }

        if (privateSeed != null && privateSeed.Length != selected.PrivateSeedSizeInBytes)
        {
            return Failure("invalid_mldsa_key", "The ML-DSA private seed has an invalid length.");
        }

        return Create(
            publicKey.AsSpan().ToArray(),
            privateSeed == null ? null : privateSeed.AsSpan().ToArray(),
            algorithm);
    }

    internal static MLDsaAlgorithm? GetAlgorithm(string algorithm)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (string.Equals(algorithm, JoseAlgorithms.MlDsa44, StringComparison.Ordinal))
        {
            return MLDsaAlgorithm.MLDsa44;
        }

        if (string.Equals(algorithm, JoseAlgorithms.MlDsa65, StringComparison.Ordinal))
        {
            return MLDsaAlgorithm.MLDsa65;
        }

        if (string.Equals(algorithm, JoseAlgorithms.MlDsa87, StringComparison.Ordinal))
        {
            return MLDsaAlgorithm.MLDsa87;
        }

        return null;
    }

    internal static TokenKeyResult Create(
        byte[] publicKey,
        byte[]? privateSeed,
        string algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.AlgorithmKeyPair;
        key.CoseKeyType = CoseKeyLabels.KeyTypeAlgorithmKeyPair;
        key.Algorithm = algorithm;
        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromJose(algorithm);
        key.CoseAlgorithm = mapping.CoseLabel;
        key.HasCoseAlgorithm = mapping.Mapped;
        key.AlgorithmPublicKey = publicKey;
        key.AlgorithmPrivateKey = privateSeed;
        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = privateSeed != null;
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    internal static TokenKeyResult Failure(string code, string message)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
