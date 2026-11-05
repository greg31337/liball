using System;
namespace token;

/// <summary>Creates and verifies RFC 9338 version 2 countersignatures.</summary>
internal static class CoseCounterSignatureOperations
{
    /// <summary>Creates a tagged full countersignature with empty external authenticated data.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="header">The countersigner headers.</param>
    /// <param name="key">The private countersigning key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureResult Create(
        CoseCounterSignatureTarget target,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        return Create(
            target,
            Array.Empty<byte>(),
            header,
            key,
            true,
            false,
            policy);
    }

    /// <summary>Creates a full version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The countersigner headers, including a protected algorithm.</param>
    /// <param name="key">The private countersigning key.</param>
    /// <param name="tagged">Whether standalone serialization uses CBOR tag 19.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureResult Create(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        ValidateReferences(
            target,
            externalAuthenticatedData,
            header,
            key,
            policy);
        TokenResult targetResult = ValidateTarget(target);
        if (!targetResult.Success)
        {
            return Failure(targetResult.Error);
        }

        TokenResult headerResult = ValidateHeader(
            header,
            allowCompatibility,
            policy);
        if (!headerResult.Success)
        {
            return Failure(headerResult.Error);
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!protectedBytes.Success)
        {
            return Failure(protectedBytes.Error);
        }

        TokenBytesResult toBeSigned = CoseCounterSignatureStructure.Create(
            target.BodyProtected,
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            target.Payload,
            target.OtherFields,
            policy);
        if (!toBeSigned.Success)
        {
            return Failure(toBeSigned.Error);
        }

        TokenBytesResult signature = Sign(
            toBeSigned.Bytes,
            key,
            header.Algorithm,
            allowCompatibility);
        if (!signature.Success)
        {
            return Failure(signature.Error);
        }

        CoseCounterSignature counterSignature =
            new CoseCounterSignature();
        counterSignature.Header = header;
        counterSignature.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        counterSignature.Signature = signature.Bytes;
        counterSignature.Tagged = tagged;
        CoseCounterSignatureResult result =
            new CoseCounterSignatureResult();
        result.Success = true;
        result.CounterSignature = counterSignature;
        return result;
    }

    /// <summary>Verifies a full countersignature with empty external authenticated data.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="counterSignature">The countersignature to verify.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A success result only when the countersignature is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Verify(
        CoseCounterSignatureTarget target,
        CoseCounterSignature counterSignature,
        TokenKey key,
        TokenPolicy policy)
    {
        return Verify(
            target,
            Array.Empty<byte>(),
            counterSignature,
            key,
            false,
            policy);
    }

    /// <summary>Verifies a full countersignature with explicit external data and compatibility control.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="counterSignature">The countersignature to verify.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A success result only when the countersignature is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Verify(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        CoseCounterSignature counterSignature,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        if (counterSignature == null)
        {
            throw new ArgumentNullException("counterSignature");
        }

        ValidateReferences(
            target,
            externalAuthenticatedData,
            counterSignature.Header,
            key,
            policy);
        if (counterSignature.Signature == null)
        {
            return ValidationFailure(
                "invalid_countersignature",
                "The countersignature bytes are missing.");
        }

        TokenResult targetResult = ValidateTarget(target);
        if (!targetResult.Success)
        {
            return targetResult;
        }

        TokenResult headerResult = ValidateHeader(
            counterSignature.Header,
            allowCompatibility,
            policy);
        if (!headerResult.Success)
        {
            return headerResult;
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            counterSignature.Header,
            policy);
        if (!protectedBytes.Success)
        {
            return ValidationFailure(protectedBytes.Error);
        }

        TokenBytesResult toBeSigned = CoseCounterSignatureStructure.Create(
            target.BodyProtected,
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            target.Payload,
            target.OtherFields,
            policy);
        if (!toBeSigned.Success)
        {
            return ValidationFailure(toBeSigned.Error);
        }

        TokenResult verified = VerifySignature(
            toBeSigned.Bytes,
            counterSignature.Signature,
            key,
            (counterSignature.Header ?? throw new InvalidOperationException("A required value is null.")).Algorithm,
            allowCompatibility);
        if (verified.Success)
        {
            counterSignature.SignatureVerified = true;
        }

        return verified;
    }

    /// <summary>Creates an abbreviated version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="algorithm">The contextually supplied signature algorithm.</param>
    /// <param name="key">The private countersigning key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>The abbreviated signature bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateAbbreviated(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        int algorithm,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        ValidateAbbreviatedReferences(
            target,
            externalAuthenticatedData,
            key,
            policy);
        TokenResult targetResult = ValidateTarget(target);
        if (!targetResult.Success)
        {
            return BytesFailure(targetResult.Error);
        }

        TokenResult algorithmResult = ValidateAlgorithm(
            algorithm,
            allowCompatibility,
            policy);
        if (!algorithmResult.Success)
        {
            return BytesFailure(algorithmResult.Error);
        }

        TokenBytesResult toBeSigned =
            CoseCounterSignatureStructure.CreateAbbreviated(
                target.BodyProtected,
                externalAuthenticatedData,
                target.Payload,
                target.OtherFields,
                policy);
        if (!toBeSigned.Success)
        {
            return toBeSigned;
        }

        return Sign(
            toBeSigned.Bytes,
            key,
            algorithm,
            allowCompatibility);
    }

    /// <summary>Verifies an abbreviated version 2 countersignature.</summary>
    /// <param name="target">The finalized COSE target byte-string fields.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="signature">The abbreviated signature bytes.</param>
    /// <param name="algorithm">The contextually supplied signature algorithm.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A success result only when the countersignature is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult VerifyAbbreviated(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        byte[] signature,
        int algorithm,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        ValidateAbbreviatedReferences(
            target,
            externalAuthenticatedData,
            key,
            policy);
        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        TokenResult targetResult = ValidateTarget(target);
        if (!targetResult.Success)
        {
            return targetResult;
        }

        TokenResult algorithmResult = ValidateAlgorithm(
            algorithm,
            allowCompatibility,
            policy);
        if (!algorithmResult.Success)
        {
            return algorithmResult;
        }

        TokenBytesResult toBeSigned =
            CoseCounterSignatureStructure.CreateAbbreviated(
                target.BodyProtected,
                externalAuthenticatedData,
                target.Payload,
                target.OtherFields,
                policy);
        if (!toBeSigned.Success)
        {
            return ValidationFailure(toBeSigned.Error);
        }

        return VerifySignature(
            toBeSigned.Bytes,
            signature,
            key,
            algorithm,
            allowCompatibility);
    }

    /// <summary>Stores an abbreviated signature in unprotected header label 12.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="signature">The abbreviated signature bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static void SetAbbreviatedHeader(
        CoseHeader header,
        byte[] signature)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        header.SetUnprotected(
            CoseHeaderLabels.CounterSignature0V2,
            BytesItem(Copy(signature)));
    }

    /// <summary>Reads an abbreviated signature from unprotected header label 12.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <returns>The signature bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static TokenBytesResult ReadAbbreviatedHeader(CoseHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (header.GetProtected(CoseHeaderLabels.CounterSignature0V2) != null)
        {
            return BytesFailure(
                "countersignature_must_be_unprotected",
                "An abbreviated version 2 countersignature must be unprotected.");
        }

        CborItem? value = header.GetUnprotected(
            CoseHeaderLabels.CounterSignature0V2);
        if (value == null)
        {
            return BytesFailure(
                "missing_countersignature",
                "The COSE header does not contain an abbreviated version 2 countersignature.");
        }

        if (value.Kind != TokenCborValueKind.ByteString
            || value.Bytes == null)
        {
            return BytesFailure(
                "invalid_countersignature_header",
                "Header label 12 must contain a byte string.");
        }

        return BytesSuccess(Copy(value.Bytes));
    }

    static TokenResult ValidateHeader(
        CoseHeader? header,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        TokenResult critical = CoseCriticalHeaderValidator.Validate((header ?? throw new InvalidOperationException("A required value is null.")));
        if (!critical.Success)
        {
            return critical;
        }

        if (header.Get(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "missing_cose_algorithm",
                "A full countersignature requires an algorithm header.");
        }

        if (header.GetProtected(CoseHeaderLabels.Algorithm) == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The countersignature algorithm must be protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The countersignature algorithm must use an integer label.");
        }

        return ValidateAlgorithm(
            header.Algorithm,
            allowCompatibility,
            policy);
    }

    static TokenResult ValidateAlgorithm(
        int algorithm,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The countersignature policy is invalid.");
        }

        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsEcdsa(algorithm) && !IsRsa(algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an implemented signature algorithm.");
        }

        if (IsCompatibilityEcdsa(algorithm) && !allowCompatibility)
        {
            return ValidationFailure(
                "compatibility_required",
                "The polymorphic COSE ECDSA algorithm requires compatibility opt-in.");
        }

        string? coseName = CoseAlgorithmsOperations.GetName(algorithm);
        AlgorithmMappingResult mapping =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        bool allowed = RegistryLookup.Contains(
            policy.AllowedAlgorithms,
            coseName,
            false);
        if (!allowed && mapping.Mapped)
        {
            allowed = RegistryLookup.Contains(
                policy.AllowedAlgorithms,
                mapping.JoseName,
                false);
        }

        if (!allowed)
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The countersignature algorithm is not allowed by policy.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateTarget(CoseCounterSignatureTarget target)
    {
        if (target.BodyProtected == null
            || target.Payload == null
            || target.OtherFields == null)
        {
            return ValidationFailure(
                "invalid_countersignature_target",
                "A countersignature target requires protected, payload, and other-field arrays.");
        }

        int index = 0;
        while (index < target.OtherFields.Length)
        {
            if (target.OtherFields[index] == null)
            {
                return ValidationFailure(
                    "invalid_countersignature_target",
                    "Every countersignature target other field must contain bytes.");
            }

            index += 1;
        }

        return ValidationSuccess();
    }

    static TokenBytesResult Sign(
        byte[]? data,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (IsEcdsa(algorithm))
        {
            return CoseEcdsaAlgorithms.Sign(
                (data ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm,
                allowCompatibility);
        }

        AlgorithmMappingResult mapping =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapping.Mapped)
        {
            return BytesFailure(
                "invalid_algorithm",
                "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Sign((data ?? throw new InvalidOperationException("A required value is null.")), key, (mapping.JoseName ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenResult VerifySignature(
        byte[]? data,
        byte[] signature,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (IsEcdsa(algorithm))
        {
            return CoseEcdsaAlgorithms.Verify(
                (data ?? throw new InvalidOperationException("A required value is null.")),
                signature,
                key,
                algorithm,
                allowCompatibility);
        }

        AlgorithmMappingResult mapping =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapping.Mapped)
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Verify(
            (data ?? throw new InvalidOperationException("A required value is null.")),
            signature,
            key,
            (mapping.JoseName ?? throw new InvalidOperationException("A required value is null.")));
    }

    static bool IsEcdsa(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaP256Sha256
            || algorithm == CoseAlgorithms.EcdsaP384Sha384
            || algorithm == CoseAlgorithms.EcdsaP521Sha512
            || algorithm == CoseAlgorithms.EcdsaSecp256k1Sha256
            || IsCompatibilityEcdsa(algorithm);
    }

    static bool IsCompatibilityEcdsa(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaSha256
            || algorithm == CoseAlgorithms.EcdsaSha384
            || algorithm == CoseAlgorithms.EcdsaSha512;
    }

    static bool IsRsa(int algorithm)
    {
        return algorithm == CoseAlgorithms.RsaSha256
            || algorithm == CoseAlgorithms.RsaSha384
            || algorithm == CoseAlgorithms.RsaSha512
            || algorithm == CoseAlgorithms.RsaPssSha256
            || algorithm == CoseAlgorithms.RsaPssSha384
            || algorithm == CoseAlgorithms.RsaPssSha512;
    }

    static void ValidateReferences(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        CoseHeader? header,
        TokenKey key,
        TokenPolicy policy)
    {
        ValidateAbbreviatedReferences(
            target,
            externalAuthenticatedData,
            key,
            policy);
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }
    }

    static void ValidateAbbreviatedReferences(
        CoseCounterSignatureTarget target,
        byte[] externalAuthenticatedData,
        TokenKey key,
        TokenPolicy policy)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    static byte[] Copy(byte[] value)
    {
        return value.AsSpan().ToArray();
    }

    static CborItem BytesItem(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }

    static TokenResult ValidationFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static CoseCounterSignatureResult Failure(TokenError? error)
    {
        CoseCounterSignatureResult result =
            new CoseCounterSignatureResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesSuccess(byte[] value)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = value;
        return result;
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        return BytesFailure(new TokenError(code, message, -1));
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
