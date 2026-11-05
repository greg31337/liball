using System;
namespace token;

/// <summary>
/// Provides defensive snapshots of the JWK registries used by this library.
/// </summary>
internal static class JwkRegistryOperations
{
    static readonly string[] parameters = new string[]
    {
        JwkNames.KeyType, JwkNames.Use, JwkNames.KeyOperations,
        JwkNames.Algorithm, JwkNames.KeyId, JwkNames.X509Url,
        JwkNames.X509CertificateChain, JwkNames.X509Sha1Thumbprint,
        JwkNames.X509Sha256Thumbprint, JwkNames.Curve, JwkNames.X, JwkNames.Y,
        JwkNames.PrivateKey, JwkNames.RsaModulus, JwkNames.RsaExponent,
        JwkNames.RsaFirstPrime, JwkNames.RsaSecondPrime,
        JwkNames.RsaFirstExponent, JwkNames.RsaSecondExponent,
        JwkNames.RsaCoefficient, JwkNames.RsaOtherPrimes,
        JwkNames.SymmetricKey, JwkNames.AlgorithmPublicKey,
        JwkNames.AlgorithmPrivateKey
    };

    static readonly string[] keyTypes = new string[]
    {
        TokenKeyTypes.Symmetric, TokenKeyTypes.Rsa,
        TokenKeyTypes.EllipticCurve, TokenKeyTypes.OctetKeyPair,
        TokenKeyTypes.AlgorithmKeyPair
    };

    static readonly string[] keyUses = new string[]
    {
        TokenKeyUses.Signature, TokenKeyUses.Encryption
    };

    static readonly string[] keyOperations = new string[]
    {
        TokenKeyOperations.Sign, TokenKeyOperations.Verify,
        TokenKeyOperations.Encrypt, TokenKeyOperations.Decrypt,
        TokenKeyOperations.WrapKey, TokenKeyOperations.UnwrapKey,
        TokenKeyOperations.DeriveKey, TokenKeyOperations.DeriveBits,
        TokenKeyOperations.MacCreate, TokenKeyOperations.MacVerify
    };

    /// <summary>Returns registered JWK member names.</summary>
    /// <returns>A new array containing the member names.</returns>
    internal static string[] GetParameters()
    {
        return RegistryLookup.Copy(parameters);
    }

    /// <summary>Returns registered JWK key types supported by the normalized key model.</summary>
    /// <returns>A new array containing the key type names.</returns>
    internal static string[] GetKeyTypes()
    {
        return RegistryLookup.Copy(keyTypes);
    }

    /// <summary>Returns registered JWK public-key uses.</summary>
    /// <returns>A new array containing the use names.</returns>
    internal static string[] GetKeyUses()
    {
        return RegistryLookup.Copy(keyUses);
    }

    /// <summary>Returns registered JWK key operations.</summary>
    /// <returns>A new array containing the operation names.</returns>
    internal static string[] GetKeyOperations()
    {
        return RegistryLookup.Copy(keyOperations);
    }

    /// <summary>Returns registered JWK curve names shared with the COSE curve registry.</summary>
    /// <returns>A new array containing the curve names.</returns>
    internal static string[] GetCurves()
    {
        int[] labels = CoseKeyLabelsOperations.GetCurves();
        string[] names = new string[labels.Length];
        int index = 0;
        while (index < labels.Length)
        {
            names[index] = CoseKeyLabelsOperations.GetCurveName(labels[index])
                ?? throw new InvalidOperationException("A required value is null.");
            index += 1;
        }

        return names;
    }
}
