using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Creates and verifies fixed-width COSE ECDSA signatures.</summary>
internal static class CoseEcdsaAlgorithms
{
    /// <summary>Creates a COSE ECDSA signature using conservative algorithm policy.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The ECDSA signing key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <returns>A raw fixed-width <c>r || s</c> signature, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Sign(
        byte[] data,
        TokenKey key,
        int algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        return Sign(data.AsSpan(), key, algorithm, false);
    }

    /// <summary>Creates a COSE ECDSA signature with explicit compatibility control.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The ECDSA signing key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <returns>A raw fixed-width <c>r || s</c> signature, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Sign(
        byte[] data,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        return Sign(data.AsSpan(), key, algorithm, allowCompatibility);
    }

    /// <summary>Creates a COSE ECDSA signature over a byte span.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The ECDSA signing key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <returns>A raw fixed-width <c>r || s</c> signature, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult Sign(
        ReadOnlySpan<byte> data,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            true,
            allowCompatibility);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        byte[] der = (key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).SignData(
            data,
            GetHashAlgorithm(algorithm),
            DSASignatureFormat.Rfc3279DerSequence);
        TokenBytesResult raw = EcdsaSignatureCodec.DerToRaw(
            der,
            GetComponentWidth(algorithm));
        CryptographicOperations.ZeroMemory(der);
        return raw;
    }

    /// <summary>Verifies a COSE ECDSA signature using conservative algorithm policy.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The raw fixed-width <c>r || s</c> signature.</param>
    /// <param name="key">The ECDSA verification key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <returns>A successful result when the signature is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenResult Verify(
        byte[] data,
        byte[] signature,
        TokenKey key,
        int algorithm)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        return Verify(
            data.AsSpan(),
            signature.AsSpan(),
            key,
            algorithm,
            false);
    }

    /// <summary>Verifies a COSE ECDSA signature with explicit compatibility control.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The raw fixed-width <c>r || s</c> signature.</param>
    /// <param name="key">The ECDSA verification key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <returns>A successful result when the signature is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenResult Verify(
        byte[] data,
        byte[] signature,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        return Verify(
            data.AsSpan(),
            signature.AsSpan(),
            key,
            algorithm,
            allowCompatibility);
    }

    /// <summary>Verifies a COSE ECDSA signature over byte spans.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The raw fixed-width <c>r || s</c> signature.</param>
    /// <param name="key">The ECDSA verification key and its metadata.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <returns>A successful result when the signature is valid, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult Verify(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> signature,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TokenResult validation = Validate(
            key,
            algorithm,
            false,
            allowCompatibility);
        if (!validation.Success)
        {
            return validation;
        }

        TokenBytesResult der = EcdsaSignatureCodec.RawToDer(
            signature,
            GetComponentWidth(algorithm));
        if (!der.Success)
        {
            return Failure(
                "signature_mismatch",
                "The COSE ECDSA signature does not match the signed data.");
        }

        bool verified = (key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).VerifyData(
            data,
            der.Bytes,
            GetHashAlgorithm(algorithm),
            DSASignatureFormat.Rfc3279DerSequence);
        CryptographicOperations.ZeroMemory(der.Bytes);
        if (!verified)
        {
            return Failure(
                "signature_mismatch",
                "The COSE ECDSA signature does not match the signed data.");
        }

        return Success();
    }

    static TokenResult Validate(
        TokenKey key,
        int algorithm,
        bool signing,
        bool allowCompatibility)
    {
        TokenResult algorithmResult = ValidateAlgorithm(
            algorithm,
            allowCompatibility);
        if (!algorithmResult.Success)
        {
            return algorithmResult;
        }

        TokenResult keyResult = EcdsaKeyImporter.Validate(
            key,
            signing,
            allowCompatibility);
        if (!keyResult.Success)
        {
            return keyResult;
        }

        TokenKeyResult requested = EcdsaKeyImporter.ImportCose(
            (key.Ecdsa ?? throw new InvalidOperationException("A required value is null.")),
            algorithm,
            signing,
            allowCompatibility);
        if (!requested.Success)
        {
            return Failure(requested.Error);
        }

        if (key.Algorithm != null)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(key.Algorithm);
            if (!RegistryLookup.Contains(labels, algorithm))
            {
                return Failure(
                    "conflicting_key_algorithm",
                    "The JOSE key algorithm does not match the requested COSE ECDSA algorithm.");
            }
        }

        if (key.HasCoseAlgorithm
            && !EquivalentAlgorithms(key.CoseAlgorithm, algorithm))
        {
            return Failure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match the requested ECDSA algorithm.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Signature,
                StringComparison.Ordinal))
        {
            return Failure(
                "key_use_algorithm_mismatch",
                "The key use does not permit ECDSA signing or verification.");
        }

        string operation = signing
            ? TokenKeyOperations.Sign
            : TokenKeyOperations.Verify;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                operation,
                false))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested ECDSA operation is not permitted by the key metadata.");
        }

        int coseOperation = signing
            ? CoseKeyLabels.OperationSign
            : CoseKeyLabels.OperationVerify;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                coseOperation))
        {
            return Failure(
                "key_operation_not_permitted",
                "The requested ECDSA operation is not permitted by the COSE key metadata.");
        }

        return Success();
    }

    static TokenResult ValidateAlgorithm(
        int algorithm,
        bool allowCompatibility)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsEcdsaAlgorithm(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented ECDSA signing algorithm.");
        }

        if (IsCompatibilityAlgorithm(algorithm)
            && !allowCompatibility)
        {
            return Failure(
                "compatibility_required",
                "The polymorphic COSE ECDSA algorithm requires compatibility opt-in.");
        }

        return Success();
    }

    static bool IsEcdsaAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP256Sha256
            || algorithm == CoseAlgorithms.EcdsaP384Sha384
            || algorithm == CoseAlgorithms.EcdsaP521Sha512
            || algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256
            || IsCompatibilityAlgorithm(algorithm);
    }

    static bool IsCompatibilityAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaSha256
            || algorithm == CoseAlgorithms.EcdsaSha384
            || algorithm == CoseAlgorithms.EcdsaSha512;
    }

    static bool EquivalentAlgorithms(int left, int right)
    {
        if (left == right)
        {
            return true;
        }

        return IsP256(left) && IsP256(right)
            || IsP384(left) && IsP384(right)
            || IsP521(left) && IsP521(right);
    }

    static bool IsP256(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP256Sha256
            || algorithm == CoseAlgorithms.EcdsaSha256;
    }

    static bool IsP384(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP384Sha384
            || algorithm == CoseAlgorithms.EcdsaSha384;
    }

    static bool IsP521(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP521Sha512
            || algorithm == CoseAlgorithms.EcdsaSha512;
    }

    static HashAlgorithmName GetHashAlgorithm(int algorithm)
    {
        if (IsP256(algorithm)
            || algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256)
        {
            return HashAlgorithmName.SHA256;
        }

        if (IsP384(algorithm))
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static int GetComponentWidth(int algorithm)
    {
        if (IsP256(algorithm)
            || algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256)
        {
            return 32;
        }

        if (IsP384(algorithm))
        {
            return 48;
        }

        return 66;
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
