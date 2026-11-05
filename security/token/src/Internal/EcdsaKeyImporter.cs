using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Imports and validates native ECDSA keys on registered prime curves.
/// </summary>
internal static class EcdsaKeyImporter
{
    /// <summary>Imports a native ECDSA key for public operations.</summary>
    /// <param name="ecdsa">The caller-owned ECDSA key.</param>
    /// <returns>A result containing a key container that references <c>ecdsa</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>ecdsa</c> is null.</exception>
    internal static TokenKeyResult Import(ECDsa ecdsa)
    {
        return Import(ecdsa, false);
    }

    /// <summary>Imports a native ECDSA key with an explicit private-material declaration.</summary>
    /// <remarks>
    /// The returned container does not own or dispose <c>ecdsa</c>. The declaration is required
    /// because native ECDSA implementations do not provide a portable exception-free private-key probe.
    /// </remarks>
    /// <param name="ecdsa">The caller-owned ECDSA key.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing a key container that references <c>ecdsa</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>ecdsa</c> is null.</exception>
    internal static TokenKeyResult Import(ECDsa ecdsa, bool privateMaterial)
    {
        if (ecdsa == null)
        {
            throw new ArgumentNullException("ecdsa");
        }

        EcCurveInfo curve = GetCurve(ecdsa);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        return Success(ecdsa, privateMaterial, curve, null, 0, false);
    }

    /// <summary>Imports an ECDSA key for a JOSE algorithm.</summary>
    /// <param name="ecdsa">The caller-owned ECDSA key.</param>
    /// <param name="algorithm">The case-sensitive JOSE algorithm name.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either reference argument is null.</exception>
    internal static TokenKeyResult ImportJose(
        ECDsa ecdsa,
        string algorithm,
        bool privateMaterial)
    {
        if (ecdsa == null)
        {
            throw new ArgumentNullException("ecdsa");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        EcCurveInfo curve = GetCurve(ecdsa);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        EcAlgorithmRequirement requirement = GetJoseRequirement(algorithm);
        if (requirement.Error != null)
        {
            return Failure(requirement.Error);
        }

        if (requirement.CurveLabel != 0
            && curve.CoseLabel != requirement.CurveLabel)
        {
            return Failure(new TokenError(
                "algorithm_curve_mismatch",
                "The ECDSA curve does not match the JOSE algorithm.",
                -1));
        }

        return Success(ecdsa, privateMaterial, curve, algorithm, 0, false);
    }

    /// <summary>Imports an ECDSA key for a COSE algorithm using conservative policy.</summary>
    /// <param name="ecdsa">The caller-owned ECDSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>ecdsa</c> is null.</exception>
    internal static TokenKeyResult ImportCose(ECDsa ecdsa, int algorithm, bool privateMaterial)
    {
        return ImportCose(ecdsa, algorithm, privateMaterial, false);
    }

    /// <summary>Imports an ECDSA key for a COSE algorithm with explicit compatibility control.</summary>
    /// <param name="ecdsa">The caller-owned ECDSA key.</param>
    /// <param name="algorithm">The COSE algorithm label.</param>
    /// <param name="privateMaterial">Whether the native key supports private operations.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be imported.</param>
    /// <returns>A result containing the imported key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>ecdsa</c> is null.</exception>
    internal static TokenKeyResult ImportCose(
        ECDsa ecdsa,
        int algorithm,
        bool privateMaterial,
        bool allowCompatibility)
    {
        if (ecdsa == null)
        {
            throw new ArgumentNullException("ecdsa");
        }

        EcCurveInfo curve = GetCurve(ecdsa);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        EcAlgorithmRequirement requirement = GetCoseRequirement(
            algorithm,
            allowCompatibility);
        if (requirement.Error != null)
        {
            return Failure(requirement.Error);
        }

        if (requirement.CurveLabel != 0
            && curve.CoseLabel != requirement.CurveLabel)
        {
            return Failure(new TokenError(
                "algorithm_curve_mismatch",
                "The ECDSA curve does not match the COSE algorithm.",
                -1));
        }

        return Success(ecdsa, privateMaterial, curve, null, algorithm, true);
    }

    /// <summary>Validates an ECDSA key container for public operations using conservative policy.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(TokenKey key)
    {
        return Validate(key, false, false);
    }

    /// <summary>Validates an ECDSA key container with material and compatibility requirements.</summary>
    /// <param name="key">The key container to validate.</param>
    /// <param name="requirePrivateMaterial">Whether private signing capability is required.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic COSE labels may be accepted.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Validate(
        TokenKey key,
        bool requirePrivateMaterial,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key.KeyType != null
            && !string.Equals(
                key.KeyType,
                TokenKeyTypes.EllipticCurve,
                StringComparison.Ordinal))
        {
            return FailureResult(
                "invalid_key_type",
                "The key type is not an EC key.");
        }

        if (key.CoseKeyType != 0 && key.CoseKeyType != CoseKeyLabels.KeyTypeEc2)
        {
            return FailureResult(
                "invalid_key_type",
                "The COSE key type is not EC2.");
        }

        if (key.Ecdsa == null)
        {
            return FailureResult(
                "missing_key_material",
                "A native ECDSA key is required.");
        }

        EcCurveInfo curve = GetCurve(key.Ecdsa);
        if (curve.Error != null)
        {
            return FailureResult(curve.Error);
        }

        if (key.Curve != null
            && !string.Equals(key.Curve, curve.Name, StringComparison.Ordinal))
        {
            return FailureResult(
                "conflicting_key_curve",
                "The curve name does not match the native ECDSA key.");
        }

        if (key.CoseCurve != 0 && key.CoseCurve != curve.CoseLabel)
        {
            return FailureResult(
                "conflicting_key_curve",
                "The COSE curve label does not match the native ECDSA key.");
        }

        if (requirePrivateMaterial && !key.NativePrivateMaterial)
        {
            return FailureResult(
                "missing_private_key",
                "The ECDSA key is not declared to contain private material.");
        }

        if (key.Algorithm != null)
        {
            EcAlgorithmRequirement jose = GetJoseRequirement(key.Algorithm);
            if (jose.Error != null)
            {
                return FailureResult(jose.Error);
            }

            if (jose.CurveLabel != 0
                && jose.CurveLabel != curve.CoseLabel)
            {
                return FailureResult(
                    "algorithm_curve_mismatch",
                    "The ECDSA curve does not match the JOSE algorithm.");
            }
        }

        if (key.HasCoseAlgorithm)
        {
            EcAlgorithmRequirement cose = GetCoseRequirement(
                key.CoseAlgorithm,
                allowCompatibility);
            if (cose.Error != null)
            {
                return FailureResult(cose.Error);
            }

            if (cose.CurveLabel != 0
                && cose.CurveLabel != curve.CoseLabel)
            {
                return FailureResult(
                    "algorithm_curve_mismatch",
                    "The ECDSA curve does not match the COSE algorithm.");
            }
        }

        if (key.Algorithm != null && key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(key.Algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return FailureResult(
                    "conflicting_key_algorithm",
                    "The JOSE and COSE algorithm identifiers are not equivalent.");
            }
        }

        return SuccessResult();
    }

    static EcCurveInfo GetCurve(ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(false);
        string? value = parameters.Curve.Oid.Value;
        if (string.Equals(value, "1.2.840.10045.3.1.7", StringComparison.Ordinal))
        {
            return Curve("P-256", CoseKeyLabels.CurveP256);
        }

        if (string.Equals(value, "1.3.132.0.34", StringComparison.Ordinal))
        {
            return Curve("P-384", CoseKeyLabels.CurveP384);
        }

        if (string.Equals(value, "1.3.132.0.35", StringComparison.Ordinal))
        {
            return Curve("P-521", CoseKeyLabels.CurveP521);
        }

        if (string.Equals(value, "1.3.132.0.10", StringComparison.Ordinal))
        {
            return Curve("secp256k1", CoseKeyLabels.CurveSecp256k1);
        }

        if (string.Equals(value, "1.3.36.3.3.2.8.1.1.7", StringComparison.Ordinal))
        {
            return Curve("brainpoolP256r1", CoseKeyLabels.CurveBrainpoolP256r1);
        }

        if (string.Equals(value, "1.3.36.3.3.2.8.1.1.9", StringComparison.Ordinal))
        {
            return Curve("brainpoolP320r1", CoseKeyLabels.CurveBrainpoolP320r1);
        }

        if (string.Equals(value, "1.3.36.3.3.2.8.1.1.11", StringComparison.Ordinal))
        {
            return Curve("brainpoolP384r1", CoseKeyLabels.CurveBrainpoolP384r1);
        }

        if (string.Equals(value, "1.3.36.3.3.2.8.1.1.13", StringComparison.Ordinal))
        {
            return Curve("brainpoolP512r1", CoseKeyLabels.CurveBrainpoolP512r1);
        }

        EcCurveInfo result = new EcCurveInfo();
        result.Error = new TokenError(
            "unsupported_curve",
            "The ECDSA key does not use a supported registered named curve.",
            -1);
        return result;
    }

    static EcAlgorithmRequirement GetJoseRequirement(string algorithm)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return RequirementFailure(
                "unknown_algorithm",
                "The JOSE algorithm is not registered.");
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal))
        {
            return Requirement(CoseKeyLabels.CurveP256);
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal))
        {
            return Requirement(CoseKeyLabels.CurveP384);
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha512, StringComparison.Ordinal))
        {
            return Requirement(CoseKeyLabels.CurveP521);
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdsaSecp256k1Sha256,
            StringComparison.Ordinal))
        {
            return Requirement(CoseKeyLabels.CurveSecp256k1);
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdhEs, StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes256KeyWrap,
                StringComparison.Ordinal))
        {
            return Requirement(0);
        }

        return RequirementFailure(
            "invalid_key_algorithm",
            "The JOSE algorithm does not use an ECDSA key.");
    }

    static EcAlgorithmRequirement GetCoseRequirement(
        int algorithm,
        bool allowCompatibility)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(algorithm);
        if (!metadata.Known)
        {
            return RequirementFailure(
                "unknown_algorithm",
                "The COSE algorithm is not registered.");
        }

        bool compatibility = algorithm == CoseAlgorithms.EcdsaSha256
            || algorithm == CoseAlgorithms.EcdsaSha384
            || algorithm == CoseAlgorithms.EcdsaSha512;
        if (compatibility && !allowCompatibility)
        {
            return RequirementFailure(
                "compatibility_required",
                "The polymorphic COSE ECDSA algorithm requires compatibility opt-in.");
        }

        if (algorithm == CoseAlgorithms.EcdsaP256Sha256
            || algorithm == CoseAlgorithms.EcdsaSha256)
        {
            return Requirement(CoseKeyLabels.CurveP256);
        }

        if (algorithm == CoseAlgorithms.EcdsaP384Sha384
            || algorithm == CoseAlgorithms.EcdsaSha384)
        {
            return Requirement(CoseKeyLabels.CurveP384);
        }

        if (algorithm == CoseAlgorithms.EcdsaP521Sha512
            || algorithm == CoseAlgorithms.EcdsaSha512)
        {
            return Requirement(CoseKeyLabels.CurveP521);
        }

        if (algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256)
        {
            return Requirement(CoseKeyLabels.CurveSecp256k1);
        }

        if (algorithm == CoseAlgorithms.EcdsaBrainpoolP256r1Sha256)
        {
            return Requirement(CoseKeyLabels.CurveBrainpoolP256r1);
        }

        if (algorithm == CoseAlgorithms.EcdsaBrainpoolP320r1Sha384)
        {
            return Requirement(CoseKeyLabels.CurveBrainpoolP320r1);
        }

        if (algorithm == CoseAlgorithms.EcdsaBrainpoolP384r1Sha384)
        {
            return Requirement(CoseKeyLabels.CurveBrainpoolP384r1);
        }

        if (algorithm == CoseAlgorithms.EcdsaBrainpoolP512r1Sha512)
        {
            return Requirement(CoseKeyLabels.CurveBrainpoolP512r1);
        }

        if (algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap)
        {
            return Requirement(0);
        }

        return RequirementFailure(
            "invalid_key_algorithm",
            "The COSE algorithm does not use an ECDSA key.");
    }

    static TokenKeyResult Success(
        ECDsa ecdsa,
        bool privateMaterial,
        EcCurveInfo curve,
        string? joseAlgorithm,
        int coseAlgorithm,
        bool hasCoseAlgorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.EllipticCurve;
        key.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        key.Algorithm = joseAlgorithm;
        key.CoseAlgorithm = coseAlgorithm;
        key.HasCoseAlgorithm = hasCoseAlgorithm;
        key.Curve = curve.Name;
        key.CoseCurve = curve.CoseLabel;
        key.Ecdsa = ecdsa;
        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = privateMaterial;

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static TokenKeyResult Failure(TokenError error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    static TokenResult SuccessResult()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult FailureResult(string code, string message)
    {
        return FailureResult(new TokenError(code, message, -1));
    }

    static TokenResult FailureResult(TokenError error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static EcCurveInfo Curve(string name, int coseLabel)
    {
        EcCurveInfo curve = new EcCurveInfo();
        curve.Name = name;
        curve.CoseLabel = coseLabel;
        return curve;
    }

    static EcAlgorithmRequirement Requirement(int curveLabel)
    {
        EcAlgorithmRequirement requirement = new EcAlgorithmRequirement();
        requirement.CurveLabel = curveLabel;
        return requirement;
    }

    static EcAlgorithmRequirement RequirementFailure(string code, string message)
    {
        EcAlgorithmRequirement requirement = new EcAlgorithmRequirement();
        requirement.Error = new TokenError(code, message, -1);
        return requirement;
    }
}
