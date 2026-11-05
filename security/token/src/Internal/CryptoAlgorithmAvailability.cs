using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Reports whether optional signature algorithms have usable built-in .NET implementations.
/// </summary>
internal static class CryptoAlgorithmAvailability
{
    static readonly bool es256KAvailable = ProbeEs256K();
    static readonly bool ed25519Available = ProbeEdwards("Ed25519");
    static readonly bool ed448Available = ProbeEdwards("Ed448");

    /// <summary>Reports whether the platform ECDSA provider can create and use secp256k1 keys.</summary>
    /// <returns><c>true</c> when ES256K can use the built-in ECDSA provider; otherwise, <c>false</c>.</returns>
    internal static bool IsEs256KAvailable()
    {
        return es256KAvailable;
    }

    /// <summary>Reports whether a public built-in Ed25519 signing primitive is available.</summary>
    /// <returns><c>true</c> when the target runtime exposes usable Ed25519 signing and verification methods; otherwise, <c>false</c>.</returns>
    internal static bool IsEd25519Available()
    {
        return ed25519Available;
    }

    /// <summary>Reports whether a public built-in Ed448 signing primitive is available.</summary>
    /// <returns><c>true</c> when the target runtime exposes usable Ed448 signing and verification methods; otherwise, <c>false</c>.</returns>
    internal static bool IsEd448Available()
    {
        return ed448Available;
    }

    /// <summary>Reports whether any public built-in EdDSA signing primitive is available.</summary>
    /// <returns><c>true</c> when Ed25519 or Ed448 is available; otherwise, <c>false</c>.</returns>
    internal static bool IsEdDsaAvailable()
    {
        return ed25519Available || ed448Available;
    }

    /// <summary>Reports runtime availability for a registered JOSE algorithm.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns><c>true</c> when the algorithm is in built-in implementation scope and its required platform primitive is available; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithm</c> is null.</exception>
    internal static bool IsJoseAlgorithmAvailable(string algorithm)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdsaSecp256k1Sha256,
            StringComparison.Ordinal))
        {
            return es256KAvailable;
        }

        if (string.Equals(algorithm, JoseAlgorithms.EdDsa, StringComparison.Ordinal))
        {
            return IsEdDsaAvailable();
        }

        if (string.Equals(algorithm, JoseAlgorithms.Ed25519, StringComparison.Ordinal))
        {
            return ed25519Available;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Ed448, StringComparison.Ordinal))
        {
            return ed448Available;
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        return metadata.Known && metadata.Supported && !metadata.Prohibited;
    }

    /// <summary>Reports runtime availability for a registered COSE algorithm.</summary>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns><c>true</c> when the algorithm is in built-in implementation scope and its required platform primitive is available; otherwise, <c>false</c>.</returns>
    internal static bool IsCoseAlgorithmAvailable(int algorithm)
    {
        if (algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256)
        {
            return es256KAvailable;
        }

        if (algorithm == CoseAlgorithms.EdDsa)
        {
            return IsEdDsaAvailable();
        }

        if (algorithm == CoseAlgorithms.Ed25519)
        {
            return ed25519Available;
        }

        if (algorithm == CoseAlgorithms.Ed448)
        {
            return ed448Available;
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(algorithm);
        return metadata.Known && metadata.Supported && !metadata.Prohibited;
    }

    /// <summary>Checks whether a JOSE algorithm can be used by a cryptographic operation.</summary>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <returns>A successful result when the algorithm is available; otherwise, a stable unknown, prohibited, or unsupported algorithm error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithm</c> is null.</exception>
    internal static TokenResult CheckJoseAlgorithm(string algorithm)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return Failure(
                "unknown_algorithm",
                "The JOSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return Failure(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited for cryptographic operations.");
        }

        if (!IsJoseAlgorithmAvailable(algorithm))
        {
            return Failure(
                "unsupported_algorithm",
                "The JOSE algorithm has no usable built-in .NET cryptographic implementation.");
        }

        return Success();
    }

    /// <summary>Checks whether a COSE algorithm can be used by a cryptographic operation.</summary>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <returns>A successful result when the algorithm is available; otherwise, a stable unknown, prohibited, or unsupported algorithm error.</returns>
    internal static TokenResult CheckCoseAlgorithm(int algorithm)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(algorithm);
        if (!metadata.Known)
        {
            return Failure(
                "unknown_algorithm",
                "The COSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return Failure(
                "prohibited_algorithm",
                "The COSE algorithm is prohibited for cryptographic operations.");
        }

        if (!IsCoseAlgorithmAvailable(algorithm))
        {
            return Failure(
                "unsupported_algorithm",
                "The COSE algorithm has no usable built-in .NET cryptographic implementation.");
        }

        return Success();
    }

    static bool ProbeEs256K()
    {
        Task<bool> probe = Task.Run(() => ProbeEs256KCore());
        while (!probe.IsCompleted)
        {
            Thread.Yield();
        }

        if (probe.IsCompletedSuccessfully)
        {
            return probe.Result;
        }

        AggregateException? failure = probe.Exception;
        (failure ?? throw new InvalidOperationException("A required value is null.")).Flatten();
        return false;
    }

    static bool ProbeEs256KCore()
    {
        ECCurve curve = ECCurve.CreateFromValue("1.3.132.0.10");
        using (ECDsa ecdsa = ECDsa.Create(curve))
        {
            byte[] data = new byte[] { 69, 83, 50, 53, 54, 75 };
            byte[] signature = ecdsa.SignData(
                data,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            bool verified = ecdsa.KeySize == 256 && ecdsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            CryptographicOperations.ZeroMemory(signature);
            return verified;
        }
    }

    static bool ProbeEdwards(string name)
    {
        Type? type = Type.GetType(
            "System.Security.Cryptography." + name
            + ", System.Security.Cryptography.Algorithms",
            false);
        if (type == null)
        {
            type = Type.GetType(
                "System.Security.Cryptography." + name
                + ", System.Security.Cryptography",
                false);
        }

        if (type == null || !type.IsPublic)
        {
            return false;
        }

        bool sign = false;
        bool verify = false;
        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Static);
        int index = 0;
        while (index < methods.Length)
        {
            if (string.Equals(methods[index].Name, "Sign", StringComparison.Ordinal))
            {
                sign = true;
            }

            if (string.Equals(methods[index].Name, "Verify", StringComparison.Ordinal))
            {
                verify = true;
            }

            index += 1;
        }

        return sign && verify;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
