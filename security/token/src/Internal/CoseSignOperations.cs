using System;
namespace token;

/// <summary>Creates and verifies RFC 9052 COSE_Sign messages.</summary>
internal static class CoseSignOperations
{
    /// <summary>Creates a tagged COSE_Sign message using conservative algorithm policy.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="bodyHeader">The shared message headers.</param>
    /// <param name="signerHeaders">The headers corresponding to each signer.</param>
    /// <param name="keys">The private signing keys corresponding to each signer.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The signed message bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Create(
            payload,
            bodyHeader,
            signerHeaders,
            keys,
            true,
            false,
            policy);
    }

    /// <summary>Creates a COSE_Sign message with explicit tag and compatibility control.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="bodyHeader">The shared message headers.</param>
    /// <param name="signerHeaders">The headers corresponding to each signer.</param>
    /// <param name="keys">The private signing keys corresponding to each signer.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 98.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The signed message bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] payload,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            Array.Empty<byte>(),
            bodyHeader,
            signerHeaders,
            keys,
            tagged,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates an attached COSE_Sign message with external authenticated data.</summary>
    internal static TokenBytesResult CreateWithExternalAuthenticatedData(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            bodyHeader,
            signerHeaders,
            keys,
            tagged,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Creates a COSE_Sign message whose payload is transported separately.</summary>
    internal static TokenBytesResult CreateDetached(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        bool tagged,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return CreateCore(
            payload,
            externalAuthenticatedData,
            bodyHeader,
            signerHeaders,
            keys,
            tagged,
            true,
            allowCompatibility,
            policy);
    }

    static TokenBytesResult CreateCore(
        byte[] payload,
        byte[] externalAuthenticatedData,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        bool tagged,
        bool detached,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        ValidateCreateReferences(
            payload,
            bodyHeader,
            signerHeaders,
            keys,
            policy);
        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        TokenResult counts = ValidateCounts(
            signerHeaders.Length,
            keys.Length);
        if (!counts.Success)
        {
            return BytesFailure(counts.Error);
        }

        TokenResult bodyCritical =
            CoseCriticalHeaderValidator.Validate(bodyHeader);
        if (!bodyCritical.Success)
        {
            return BytesFailure(bodyCritical.Error);
        }

        TokenBytesResult bodyProtected = CoseSign.GetProtectedBytes(
            bodyHeader,
            policy);
        if (!bodyProtected.Success)
        {
            return bodyProtected;
        }

        CoseSign message = new CoseSign();
        message.Header = bodyHeader;
        message.Payload = detached ? null : payload;
        message.DetachedPayload = detached;
        message.Tagged = tagged;
        message.Signatures = new CoseSignature[signerHeaders.Length];
        int index = 0;
        while (index < signerHeaders.Length)
        {
            if (signerHeaders[index] == null)
            {
                return BytesFailure(
                    "invalid_cose_signer_header",
                    "A COSE_Sign signer header is missing.");
            }

            if (keys[index] == null)
            {
                return BytesFailure(
                    "invalid_key",
                    "A COSE_Sign signing key is missing.");
            }

            TokenResult headerResult = ValidateSignerHeader(
                signerHeaders[index],
                allowCompatibility,
                policy);
            if (!headerResult.Success)
            {
                return BytesFailure(headerResult.Error);
            }

            TokenBytesResult signerProtected =
                CoseSign.GetProtectedBytes(
                    signerHeaders[index],
                    policy);
            if (!signerProtected.Success)
            {
                return signerProtected;
            }

            TokenBytesResult toBeSigned =
                CoseSigStructure.CreateSignature(
                    (bodyProtected.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                    (signerProtected.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                    externalAuthenticatedData,
                    payload,
                    policy);
            if (!toBeSigned.Success)
            {
                return toBeSigned;
            }

            TokenBytesResult signature = Sign(
                toBeSigned.Bytes,
                keys[index],
                signerHeaders[index].Algorithm,
                allowCompatibility);
            if (!signature.Success)
            {
                return signature;
            }

            CoseSignature entry = new CoseSignature();
            entry.Header = signerHeaders[index];
            entry.Signature = signature.Bytes;
            message.Signatures[index] = entry;
            index += 1;
        }

        message.Header.ProtectedBytes = Copy(bodyProtected.Bytes);
        return message.WriteWithTagAndPolicy(tagged, policy);
    }

    /// <summary>Verifies every signature in an attached COSE_Sign message using conservative algorithm policy.</summary>
    /// <param name="cbor">The tagged or contextually untagged COSE_Sign bytes.</param>
    /// <param name="keys">The public verification keys corresponding to signature order.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSignResult Verify(
        byte[] cbor,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return Verify(cbor, keys, false, policy);
    }

    /// <summary>Verifies every signature with explicit compatibility control.</summary>
    /// <param name="cbor">The tagged or contextually untagged COSE_Sign bytes.</param>
    /// <param name="keys">The public verification keys corresponding to signature order.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <param name="policy">The size, algorithm, and critical-header policy.</param>
    /// <returns>The verified message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSignResult Verify(
        byte[] cbor,
        TokenKey[] keys,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            Array.Empty<byte>(),
            keys,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies an attached COSE_Sign message with external authenticated data.</summary>
    internal static CoseSignResult VerifyWithExternalAuthenticatedData(
        byte[] cbor,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            null,
            externalAuthenticatedData,
            keys,
            false,
            allowCompatibility,
            policy);
    }

    /// <summary>Verifies a COSE_Sign message using a separately transported payload.</summary>
    internal static CoseSignResult VerifyDetached(
        byte[] cbor,
        byte[] payload,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        return VerifyCore(
            cbor,
            payload,
            externalAuthenticatedData,
            keys,
            true,
            allowCompatibility,
            policy);
    }

    static CoseSignResult VerifyCore(
        byte[] cbor,
        byte[]? detachedPayload,
        byte[] externalAuthenticatedData,
        TokenKey[] keys,
        bool detached,
        bool allowCompatibility,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (detached && detachedPayload == null)
        {
            throw new ArgumentNullException("detachedPayload");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CoseSignResult parsed = CoseSign.Read(cbor, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Message ?? throw new InvalidOperationException("A required value is null.")).DetachedPayload && !detached)
        {
            return Failure(
                "missing_payload",
                "Attached COSE_Sign verification requires payload bytes.");
        }

        if (!parsed.Message.DetachedPayload && detached)
        {
            return Failure(
                "unexpected_attached_payload",
                "Detached COSE_Sign verification requires a null payload field.");
        }

        if (keys.Length != parsed.Message.Signatures.Length)
        {
            return Failure(
                "signature_key_count_mismatch",
                "A verification key is required for every COSE signature.");
        }

        TokenResult bodyCritical =
            CoseCriticalHeaderValidator.Validate((parsed.Message.Header ?? throw new InvalidOperationException("A required value is null.")));
        if (!bodyCritical.Success)
        {
            return Failure(bodyCritical.Error);
        }

        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                return Failure(
                    "invalid_key",
                    "A COSE_Sign verification key is missing.");
            }

            CoseSignature signature = parsed.Message.Signatures[index];
            TokenResult headerResult = ValidateSignerHeader(
                signature.Header,
                allowCompatibility,
                policy);
            if (!headerResult.Success)
            {
                return Failure(headerResult.Error);
            }

            TokenBytesResult toBeSigned =
                CoseSigStructure.CreateSignature(
                    parsed.Message.Header.ProtectedBytes,
                    (signature.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
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
                signature.Signature,
                keys[index],
                signature.Header.Algorithm,
                allowCompatibility);
            if (!verified.Success)
            {
                return Failure(verified.Error);
            }

            signature.SignatureVerified = true;
            index += 1;
        }

        if (detached)
        {
            parsed.Message.Payload = Copy(detachedPayload);
        }

        parsed.Message.SignaturesVerified = true;
        return parsed;
    }

    static TokenResult ValidateSignerHeader(
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
                "A COSE_Signature requires an algorithm header.");
        }

        if (protectedAlgorithm == null)
        {
            return ValidationFailure(
                "cose_algorithm_not_protected",
                "The COSE_Signature algorithm must be integrity protected.");
        }

        if (!header.HasAlgorithm)
        {
            return ValidationFailure(
                "invalid_cose_algorithm",
                "The COSE_Signature algorithm must use an integer label.");
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

    static TokenResult ValidateCounts(int headers, int keys)
    {
        if (headers == 0)
        {
            return ValidationFailure(
                "missing_cose_signers",
                "COSE_Sign creation requires at least one signer.");
        }

        if (headers != keys)
        {
            return ValidationFailure(
                "signature_key_count_mismatch",
                "A signing key is required for every COSE signer header.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0
            || policy.AllowedAlgorithms == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Sign cryptographic policy is invalid.");
        }

        return ValidationSuccess();
    }

    static void ValidateCreateReferences(
        byte[] payload,
        CoseHeader bodyHeader,
        CoseHeader[] signerHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (bodyHeader == null)
        {
            throw new ArgumentNullException("bodyHeader");
        }

        if (signerHeaders == null)
        {
            throw new ArgumentNullException("signerHeaders");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
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

    static CoseSignResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseSignResult Failure(TokenError? error)
    {
        CoseSignResult result = new CoseSignResult();
        result.Error = error;
        return result;
    }
}
