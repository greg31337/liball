using System;
namespace token;

/// <summary>Creates and verifies RFC 9052 COSE_Sign1 messages.</summary>
internal static class CoseSign1Operations
{
    /// <summary>Creates a tagged COSE_Sign1 message using conservative algorithm policy.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The message headers, including a protected signature algorithm.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The signed message bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            header,
            key,
            true,
            false,
            false,
            policy);
    }

    /// <summary>Creates a COSE_Sign1 message with explicit tag and compatibility control.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="header">The message headers, including a protected signature algorithm.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The signed message bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            header,
            key,
            tagged,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates an attached COSE_Sign1 message with external authenticated data.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The message headers.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="allowCompatibility">Whether deprecated algorithm labels may be used.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The signed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates a COSE_Sign1 message whose signed payload is detached.</summary>
    /// <param name="payload">The detached payload bytes.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="header">The message headers.</param>
    /// <param name="key">The private signing key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 18.</param>
    /// <param name="allowCompatibility">Whether deprecated algorithm labels may be used.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The signed message with a null payload field or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateDetached(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            true,
            allowCompatibility,
            policy);
    }

    static TokenBytesResult CreateCore(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader header,
        TokenKey key,
        bool tagged,
        bool detached,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        ValidateCreateReferences(payload, header, key, policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        TokenResult headerResult = ValidateHeader(
            header,
            allowCompatibility,
            policy);
        if (!headerResult.Success)
        {
            return BytesFailure(headerResult.Error);
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            header,
            policy);
        if (!protectedBytes.Success)
        {
            return protectedBytes;
        }

        TokenBytesResult toBeSigned = CoseSigStructure.CreateSignature1(
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            externalAuthenticatedData,
            payload,
            policy);
        if (!toBeSigned.Success)
        {
            return toBeSigned;
        }

        TokenBytesResult signature = Sign(
            toBeSigned.Bytes,
            key,
            header.Algorithm,
            allowCompatibility);
        if (!signature.Success)
        {
            return signature;
        }

        CoseSign1 message = new CoseSign1();
        message.Header = header;
        message.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        message.Payload = detached ? null : payload;
        message.DetachedPayload = detached;
        message.Signature = signature.Bytes;
        message.Tagged = tagged;
        return message.WriteWithTagAndPolicy(tagged, policy);
    }

    /// <summary>Verifies an attached COSE_Sign1 message using conservative algorithm policy.</summary>
    /// <param name="cbor">The tagged or contextually untagged COSE_Sign1 bytes.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSign1Result Verify(
        byte[] cbor,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            Array.Empty<byte>(),
            key,
            false,
            false,
            policy);
    }

    /// <summary>Verifies an attached COSE_Sign1 message with explicit compatibility control.</summary>
    /// <param name="cbor">The tagged or contextually untagged COSE_Sign1 bytes.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSign1Result Verify(
        byte[] cbor,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            Array.Empty<byte>(),
            key,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies an attached COSE_Sign1 message with external authenticated data.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated algorithm labels may be used.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSign1Result VerifyWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            externalAuthenticatedData,
            key,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies a COSE_Sign1 message against detached payload and external authenticated data.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The externally supplied detached payload.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="key">The public verification key.</param>
    /// <param name="allowCompatibility">Whether deprecated algorithm labels may be used.</param>
    /// <param name="policy">The safety policy.</param>
    /// <returns>The verified message containing the supplied payload or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSign1Result VerifyDetached(
        byte[] cbor,
        byte[] payload,
        byte[] externalAuthenticatedData,
        TokenKey key,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            payload,
            externalAuthenticatedData,
            key,
            true,
            allowCompatibility,
            policy);
    }

    static CoseSign1Result VerifyCore(
        byte[] cbor,
        byte[]? detachedPayload,
        byte[] externalAuthenticatedData,
        TokenKey key,
        bool detached,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedPayload == null)
        {
            throw new ArgumentNullException("detachedPayload");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseSign1Result parsed = CoseSign1.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload && !detached)
        {
            return Failure(
                "missing_payload",
                "Attached COSE_Sign1 verification requires payload bytes.");
        }

        if (!parsed.Message.DetachedPayload && detached)
        {
            return Failure(
                "unexpected_attached_payload",
                "Detached COSE_Sign1 verification requires a null payload field.");
        }

        TokenResult headerResult = ValidateHeader(
            parsed.Message.Header,
            allowCompatibility,
            policy);
        if (!headerResult.Success)
        {
            return Failure(headerResult.Error);
        }

        TokenBytesResult toBeSigned = CoseSigStructure.CreateSignature1(
            (parsed.Message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            externalAuthenticatedData,
            ((detached ? detachedPayload : parsed.Message.Payload)
                ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!toBeSigned.Success)
        {
            return Failure(toBeSigned.Error);
        }

        TokenResult verified = VerifySignature(
            toBeSigned.Bytes,
            parsed.Message.Signature,
            key,
            parsed.Message.Header.Algorithm,
            allowCompatibility);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        if (detached)
        {
            parsed.Message.Payload = Copy(detachedPayload);
        }

        parsed.Message.SignatureVerified = true;
        return parsed;
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

        CborItem? protectedAlgorithm = header.GetProtected(
            CoseHeaderLabels.Algorithm);
        CborItem? anyAlgorithm = header.Get(CoseHeaderLabels.Algorithm);
        if (anyAlgorithm == null)
        {
            return ValidationFailure(
                "missing_cose_algorithm",
                "A COSE_Sign1 message requires an algorithm header.");
        }

        if (protectedAlgorithm == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE_Sign1 algorithm must be integrity protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE_Sign1 algorithm must use an integer label.");
        }

        TokenResult algorithm = ValidateSignatureAlgorithm(
            header.Algorithm,
            allowCompatibility);
        if (!algorithm.Success)
        {
            return algorithm;
        }

        if (!IsAllowed(header.Algorithm, policy.AllowedAlgorithms))
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The COSE signing algorithm is not allowed by policy.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateSignatureAlgorithm(
        int algorithm,
        bool allowCompatibility)
    {
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

        AlgorithmMappingResult mapped =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapped.Mapped)
        {
            return BytesFailure(
                "invalid_algorithm",
                "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Sign((data ?? throw new InvalidOperationException("A required value is null.")), key, (mapped.JoseName ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenResult VerifySignature(
        byte[]? data,
        byte[]? signature,
        TokenKey key,
        int algorithm,
        bool allowCompatibility)
    {
        if (IsEcdsa(algorithm))
        {
            return CoseEcdsaAlgorithms.Verify(
                (data ?? throw new InvalidOperationException("A required value is null.")),
                (signature ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm,
                allowCompatibility);
        }

        AlgorithmMappingResult mapped =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapped.Mapped)
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Verify(
            (data ?? throw new InvalidOperationException("A required value is null.")),
            (signature ?? throw new InvalidOperationException("A required value is null.")),
            key,
            (mapped.JoseName ?? throw new InvalidOperationException("A required value is null.")));
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

    static bool IsAllowed(int algorithm, string[]? allowed)
    {
        string? coseName = CoseAlgorithmsOperations.GetName(algorithm);
        if (RegistryLookup.Contains(allowed, coseName, false))
        {
            return true;
        }

        AlgorithmMappingResult mapping =
            JoseCoseAlgorithmMap.FromCose(algorithm);
        return mapping.Mapped
            && RegistryLookup.Contains(
                allowed,
                mapping.JoseName,
                false);
    }

    static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Sign1 cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] payload,
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (header == null)
        {
            throw new ArgumentNullException("header");
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

    static byte[] Copy(byte[]? value)
    {
        return value.AsSpan().ToArray();
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
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

    static CoseSign1Result Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseSign1Result Failure(TokenError? error)
    {
        CoseSign1Result result = new CoseSign1Result();
        result.Error = error;
        return result;
    }
}
