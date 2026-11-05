using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseSignOperationsTests
{
    const string WorkingGroupVector =
        "D8628443A10300A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840D71C05DB52C9CE7F1BF5AAC01334BBEACAC1D86A2303E6EEAA89266F45C01ED602CA649EAF790D8BC99D2458457CA6A872061940E7AFBE48E289DFAC146AE258";

    [Test]
    public void DetachedPayloadAndExternalAuthenticatedDataBindEverySignature()
    {
        using ECDsa signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa verifier = PublicPart(signer);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        byte[] payload = Encoding.UTF8.GetBytes("detached sign payload");
        byte[] externalData = Encoding.UTF8.GetBytes("sign aad");
        TokenPolicy policy = Policy("ES256");
        TokenKey[] publicKeys = new TokenKey[]
        {
            EcKey(verifier, algorithm, false)
        };

        TokenBytesResult created = CoseSignOperations.CreateDetached(
            payload,
            externalData,
            new CoseHeader(),
            new CoseHeader[] { Header(algorithm, 1) },
            new TokenKey[] { EcKey(signer, algorithm, true) },
            true,
            false,
            policy);
        CoseSignResult parsed = CoseSign.Read(created.Bytes);
        CoseSignResult verified = CoseSignOperations.VerifyDetached(
            created.Bytes,
            payload,
            externalData,
            publicKeys,
            false,
            policy);
        CoseSignResult wrongData = CoseSignOperations.VerifyDetached(
            created.Bytes,
            payload,
            new byte[] { 7 },
            publicKeys,
            false,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.DetachedPayload, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.SignaturesVerified, Is.True);
            AssertFailure(wrongData, "signature_mismatch");
        });
    }

    [Test]
    public void CreateAndVerifyMixedEcdsaAndRsaSigners()
    {
        using ECDsa p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa p256Public = PublicPart(p256);
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using ECDsa p384Public = PublicPart(p384);
        using ECDsa p521 = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        using ECDsa p521Public = PublicPart(p521);
        using RSA rsa = RSA.Create(2048);
        using RSA rsaPublic = RSA.Create();
        rsaPublic.ImportParameters(rsa.ExportParameters(false));
        int[] algorithms = new int[]
        {
            CoseAlgorithms.EcdsaP256Sha256,
            CoseAlgorithms.EcdsaP384Sha384,
            CoseAlgorithms.EcdsaP521Sha512,
            CoseAlgorithms.RsaSha256,
            CoseAlgorithms.RsaPssSha512
        };
        CoseHeader[] headers = Headers(algorithms);
        TokenKey[] privateKeys = new TokenKey[]
        {
            EcKey(p256, algorithms[0], true),
            EcKey(p384, algorithms[1], true),
            EcKey(p521, algorithms[2], true),
            RsaKey(rsa, algorithms[3], true),
            RsaKey(rsa, algorithms[4], true)
        };
        TokenKey[] publicKeys = new TokenKey[]
        {
            EcKey(p256Public, algorithms[0], false),
            EcKey(p384Public, algorithms[1], false),
            EcKey(p521Public, algorithms[2], false),
            RsaKey(rsaPublic, algorithms[3], false),
            RsaKey(rsaPublic, algorithms[4], false)
        };
        byte[] payload = Encoding.UTF8.GetBytes(
            "one payload authenticated by five signers");
        CoseHeader body = new CoseHeader();
        body.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cbor"));
        TokenPolicy policy = Policy(
            "ES256", "ES384", "ES512", "RS256", "PS512");

        TokenBytesResult created = CoseSignOperations.Create(
            payload,
            body,
            headers,
            privateKeys,
            policy);
        CoseSignResult parsed = CoseSign.Read(created.Bytes);
        CoseSignResult verified = CoseSignOperations.Verify(
            created.Bytes,
            publicKeys,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.SignaturesVerified, Is.False);
            Assert.That(parsed.Message.Signatures.All(
                value => !value.SignatureVerified), Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.Header.ContentTypeText,
                Is.EqualTo("application/cbor"));
            Assert.That(verified.Message.Signatures, Has.Length.EqualTo(5));
            Assert.That(verified.Message.SignaturesVerified, Is.True);
            Assert.That(verified.Message.Signatures.All(
                value => value.SignatureVerified), Is.True);
            Assert.That(verified.Message.Signatures.Select(
                value => value.Header.Algorithm),
                Is.EqualTo(algorithms));
            Assert.That(verified.Message.Signatures[0].Signature,
                Has.Length.EqualTo(64));
            Assert.That(verified.Message.Signatures[1].Signature,
                Has.Length.EqualTo(96));
            Assert.That(verified.Message.Signatures[2].Signature,
                Has.Length.EqualTo(132));
            Assert.That(verified.Message.Signatures[3].Signature,
                Has.Length.EqualTo(256));
            Assert.That(verified.Message.Signatures[4].Signature,
                Has.Length.EqualTo(256));
        });
    }

    [Test]
    public void VerifyMatchesCoseWorkingGroupCompatibilityVector()
    {
        using ECDsa native = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64Url.Decode(
                    "usWxHK2PmfnHKwXPS54m0kTcGJ90UiglWiGahtagnv8").Bytes,
                Y = Base64Url.Decode(
                    "IBOL-C3BttVivg-lSreASjpkttcsz-1rb7btKLv8EX4").Bytes
            }
        });
        TokenKey key = EcKey(
            native,
            CoseAlgorithms.EcdsaSha256,
            false);
        TokenPolicy policy = Policy("ES256");
        CoseSignResult parsed = CoseSign.Read(
            Convert.FromHexString(WorkingGroupVector));
        TokenBytesResult structure = CoseSigStructure.CreateSignature(
            parsed.Message.Header.ProtectedBytes,
            parsed.Message.Signatures[0].Header.ProtectedBytes,
            parsed.Message.Payload);
        bool nativeVerified = native.VerifyData(
            structure.Bytes,
            parsed.Message.Signatures[0].Signature,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        CoseSignResult conservative = CoseSignOperations.Verify(
            Convert.FromHexString(WorkingGroupVector),
            new TokenKey[] { key },
            policy);
        CoseSignResult compatible = CoseSignOperations.Verify(
            Convert.FromHexString(WorkingGroupVector),
            new TokenKey[] { key },
            true,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(conservative, "compatibility_required");
            Assert.That(structure.Bytes, Is.EqualTo(Convert.FromHexString(
                "85695369676E617475726543A1030043A101264054546869732069732074686520636F6E74656E742E")));
            Assert.That(nativeVerified, Is.True);
            Assert.That(
                compatible.Success,
                Is.True,
                compatible.Error == null ? "no error" : compatible.Error.Code);
            Assert.That(compatible.Message.SignaturesVerified, Is.True);
            Assert.That(Encoding.UTF8.GetString(compatible.Message.Payload),
                Is.EqualTo("This is the content."));
        });
    }

    [Test]
    public void EverySignatureBindsBodySignerAndPayloadBytes()
    {
        using ECDsa firstNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa secondNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa firstPublicNative = PublicPart(firstNative);
        using ECDsa secondPublicNative = PublicPart(secondNative);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey[] privateKeys = new TokenKey[]
        {
            EcKey(firstNative, algorithm, true),
            EcKey(secondNative, algorithm, true)
        };
        TokenKey[] publicKeys = new TokenKey[]
        {
            EcKey(firstPublicNative, algorithm, false),
            EcKey(secondPublicNative, algorithm, false)
        };
        CoseHeader body = new CoseHeader();
        body.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("text/plain"));
        CoseHeader[] headers = Headers(new int[] { algorithm, algorithm });
        headers[0].SetProtectedByTextLabel("signer", Unsigned(1));
        headers[1].SetProtectedByTextLabel("signer", Unsigned(2));
        byte[] payload = Encoding.ASCII.GetBytes("bound content");
        TokenPolicy policy = Policy("ES256");

        TokenBytesResult created = CoseSignOperations.Create(
            payload,
            body,
            headers,
            privateKeys,
            policy);
        CoseSignResult parsed = CoseSign.Read(created.Bytes);
        TokenBytesResult firstStructure = CoseSigStructure.CreateSignature(
            parsed.Message.Header.ProtectedBytes,
            parsed.Message.Signatures[0].Header.ProtectedBytes,
            payload);
        TokenBytesResult secondStructure = CoseSigStructure.CreateSignature(
            parsed.Message.Header.ProtectedBytes,
            parsed.Message.Signatures[1].Header.ProtectedBytes,
            payload);
        TokenResult firstVerified = CoseEcdsaAlgorithms.Verify(
            firstStructure.Bytes,
            parsed.Message.Signatures[0].Signature,
            publicKeys[0],
            algorithm);
        TokenResult secondVerified = CoseEcdsaAlgorithms.Verify(
            secondStructure.Bytes,
            parsed.Message.Signatures[1].Signature,
            publicKeys[1],
            algorithm);
        TokenResult crossed = CoseEcdsaAlgorithms.Verify(
            secondStructure.Bytes,
            parsed.Message.Signatures[0].Signature,
            publicKeys[0],
            algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(firstVerified.Success, Is.True);
            Assert.That(secondVerified.Success, Is.True);
            Assert.That(firstStructure.Bytes,
                Is.Not.EqualTo(secondStructure.Bytes));
            Assert.That(crossed.Success, Is.False);
            Assert.That(crossed.Error.Code,
                Is.EqualTo("signature_mismatch"));
        });
    }

    [Test]
    public void AnyChangedSignatureOrPayloadFailsClosed()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey privateKey = EcKey(native, algorithm, true);
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult created = CoseSignOperations.Create(
            new byte[] { 1, 2, 3 },
            new CoseHeader(),
            Headers(new int[] { algorithm }),
            new TokenKey[] { privateKey },
            policy);
        CoseSignResult signatureModel = CoseSign.Read(created.Bytes);
        signatureModel.Message.Signatures[0].Signature[0] ^= 1;
        byte[] changedSignature = signatureModel.Message.Write().Bytes;
        CoseSignResult payloadModel = CoseSign.Read(created.Bytes);
        payloadModel.Message.Payload[0] ^= 1;
        byte[] changedPayload = payloadModel.Message.Write().Bytes;

        CoseSignResult signatureResult = CoseSignOperations.Verify(
            changedSignature,
            new TokenKey[] { publicKey },
            policy);
        CoseSignResult payloadResult = CoseSignOperations.Verify(
            changedPayload,
            new TokenKey[] { publicKey },
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(signatureResult, "signature_mismatch");
            AssertFailure(payloadResult, "signature_mismatch");
        });
    }

    [Test]
    public void VerificationRequiresEveryMatchingKey()
    {
        using ECDsa first = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa second = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa firstPublic = PublicPart(first);
        using ECDsa secondPublic = PublicPart(second);
        using ECDsa wrongPublic = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult created = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            Headers(new int[] { algorithm, algorithm }),
            new TokenKey[]
            {
                EcKey(first, algorithm, true),
                EcKey(second, algorithm, true)
            },
            policy);

        CoseSignResult tooFew = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[] { EcKey(firstPublic, algorithm, false) },
            policy);
        CoseSignResult missing = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[]
            {
                EcKey(firstPublic, algorithm, false),
                null
            },
            policy);
        CoseSignResult wrong = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[]
            {
                EcKey(firstPublic, algorithm, false),
                EcKey(wrongPublic, algorithm, false)
            },
            policy);
        CoseSignResult valid = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[]
            {
                EcKey(firstPublic, algorithm, false),
                EcKey(secondPublic, algorithm, false)
            },
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(tooFew, "signature_key_count_mismatch");
            AssertFailure(missing, "invalid_key");
            AssertFailure(wrong, "signature_mismatch");
            Assert.That(valid.Success, Is.True);
        });
    }

    [Test]
    public void SignerAlgorithmsMustBeIntegerAndProtected()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcKey(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            true);
        TokenPolicy policy = Policy("ES256");
        CoseHeader missing = new CoseHeader();
        CoseHeader unprotected = new CoseHeader();
        unprotected.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.EcdsaP256Sha256));
        CoseHeader text = new CoseHeader();
        text.SetProtected(CoseHeaderLabels.Algorithm, Text("ES256"));

        TokenBytesResult missingResult = CreateOne(missing, key, policy);
        TokenBytesResult unprotectedResult = CreateOne(
            unprotected,
            key,
            policy);
        TokenBytesResult textResult = CreateOne(text, key, policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missingResult, "missing_cose_algorithm");
            AssertBytesFailure(
                unprotectedResult,
                "cose_algorithm_not_protected");
            AssertBytesFailure(textResult, "invalid_cose_algorithm");
        });
    }

    [Test]
    public void AlgorithmRegistrationTypeCompatibilityAndPolicyAreEnforced()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey currentKey = EcKey(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            true);
        TokenKey compatibleKey = EcKey(
            native,
            CoseAlgorithms.EcdsaSha256,
            true);
        TokenPolicy allowed = Policy("ES256");
        TokenPolicy denied = Policy("PS256");

        TokenBytesResult unknown = CreateOne(
            Header(123456, 1),
            currentKey,
            allowed);
        TokenBytesResult notSignature = CreateOne(
            Header(CoseAlgorithms.HmacSha256, 1),
            currentKey,
            Policy("HMAC 256/256"));
        TokenBytesResult notAllowed = CreateOne(
            Header(CoseAlgorithms.EcdsaP256Sha256, 1),
            currentKey,
            denied);
        TokenBytesResult conservative = CreateOne(
            Header(CoseAlgorithms.EcdsaSha256, 1),
            compatibleKey,
            allowed);
        TokenBytesResult compatible = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            new CoseHeader[]
            {
                Header(CoseAlgorithms.EcdsaSha256, 1)
            },
            new TokenKey[] { compatibleKey },
            true,
            true,
            allowed);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(unknown, "unknown_algorithm");
            AssertBytesFailure(notSignature, "invalid_algorithm");
            AssertBytesFailure(notAllowed, "algorithm_not_allowed");
            AssertBytesFailure(conservative, "compatibility_required");
            AssertSuccess(compatible);
        });
    }

    [Test]
    public void BodyAndSignerCriticalHeadersAreValidated()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey key = EcKey(native, algorithm, true);
        TokenPolicy policy = Policy("ES256");
        CoseHeader body = new CoseHeader();
        SetUnknownCritical(body);
        CoseHeader signer = Header(algorithm, 1);
        SetUnknownCritical(signer);

        TokenBytesResult bodyResult = CoseSignOperations.Create(
            new byte[] { 1 },
            body,
            Headers(new int[] { algorithm }),
            new TokenKey[] { key },
            policy);
        TokenBytesResult signerResult = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            new CoseHeader[] { signer },
            new TokenKey[] { key },
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                bodyResult,
                "unknown_cose_critical_header");
            AssertBytesFailure(
                signerResult,
                "unknown_cose_critical_header");
        });
    }

    [Test]
    public void KeyMetadataRestrictionsAreEnforcedForBothOperations()
    {
        using ECDsa privateNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(privateNative);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenKey signingKey = EcKey(privateNative, algorithm, true);
        signingKey.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationVerify };
        TokenBytesResult signing = CreateOne(
            Header(algorithm, 1),
            signingKey,
            policy);
        TokenKey permittedSigningKey = EcKey(
            privateNative,
            algorithm,
            true);
        TokenBytesResult created = CreateOne(
            Header(algorithm, 1),
            permittedSigningKey,
            policy);
        TokenKey verificationKey = EcKey(
            publicNative,
            algorithm,
            false);
        verificationKey.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationSign };
        CoseSignResult verification = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[] { verificationKey },
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(signing, "key_operation_not_permitted");
            AssertFailure(verification, "key_operation_not_permitted");
        });
    }

    [Test]
    public void CreationValidatesSignerCountsAndEntries()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey key = EcKey(native, algorithm, true);
        TokenPolicy policy = Policy("ES256");

        TokenBytesResult empty = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            System.Array.Empty<CoseHeader>(),
            System.Array.Empty<TokenKey>(),
            policy);
        TokenBytesResult mismatch = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            Headers(new int[] { algorithm }),
            System.Array.Empty<TokenKey>(),
            policy);
        TokenBytesResult missingHeader = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            new CoseHeader[] { null },
            new TokenKey[] { key },
            policy);
        TokenBytesResult missingKey = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            Headers(new int[] { algorithm }),
            new TokenKey[] { null },
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(empty, "missing_cose_signers");
            AssertBytesFailure(mismatch, "signature_key_count_mismatch");
            AssertBytesFailure(
                missingHeader,
                "invalid_cose_signer_header");
            AssertBytesFailure(missingKey, "invalid_key");
        });
    }

    [Test]
    public void ExplicitTagControlAndAttachedPayloadRequirementAreEnforced()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult untagged = CoseSignOperations.Create(
            new byte[] { 1 },
            new CoseHeader(),
            Headers(new int[] { algorithm }),
            new TokenKey[] { EcKey(native, algorithm, true) },
            false,
            false,
            policy);
        CoseSign detached = CoseSign.Read(untagged.Bytes).Message;
        detached.DetachedPayload = true;
        detached.Payload = null;
        byte[] detachedBytes = detached.Write().Bytes;

        CoseSignResult untaggedVerified = CoseSignOperations.Verify(
            untagged.Bytes,
            new TokenKey[] { EcKey(publicNative, algorithm, false) },
            policy);
        CoseSignResult detachedResult = CoseSignOperations.Verify(
            detachedBytes,
            new TokenKey[] { EcKey(publicNative, algorithm, false) },
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(untaggedVerified.Success, Is.True);
            Assert.That(untaggedVerified.Message.Tagged, Is.False);
            AssertFailure(detachedResult, "missing_payload");
        });
    }

    [Test]
    public void SizeAndPolicyLimitsAreEnforced()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey privateKey = EcKey(native, algorithm, true);
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult created = CreateOne(
            Header(algorithm, 1),
            privateKey,
            policy);
        TokenPolicy small = Policy("ES256");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        TokenPolicy invalid = Policy("ES256");
        invalid.AllowedAlgorithms = null;

        TokenBytesResult smallCreate = CreateOne(
            Header(algorithm, 1),
            privateKey,
            small);
        CoseSignResult smallVerify = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[] { publicKey },
            small);
        TokenBytesResult invalidCreate = CreateOne(
            Header(algorithm, 1),
            privateKey,
            invalid);
        CoseSignResult invalidVerify = CoseSignOperations.Verify(
            created.Bytes,
            new TokenKey[] { publicKey },
            invalid);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(smallCreate, "buffer_limit_exceeded");
            AssertFailure(smallVerify, "cbor_size_exceeded");
            AssertBytesFailure(invalidCreate, "invalid_policy");
            AssertFailure(invalidVerify, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseHeader header = new CoseHeader();
        CoseHeader[] headers = new CoseHeader[] { header };
        TokenKey[] keys = new TokenKey[] { new TokenKey() };
        TokenPolicy policy = Policy("ES256");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseSignOperations.Create(
                null, header, headers, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Create(
                new byte[0], null, headers, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Create(
                new byte[0], header, null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Create(
                new byte[0], header, headers, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Create(
                new byte[0], header, headers, keys, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Verify(
                null, keys, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Verify(
                new byte[0], null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSignOperations.Verify(
                new byte[0], keys, null),
                Throws.ArgumentNullException);
        });
    }

    static TokenBytesResult CreateOne(
        CoseHeader signer,
        TokenKey key,
        TokenPolicy policy)
    {
        return CoseSignOperations.Create(
            new byte[] { 1, 2, 3 },
            new CoseHeader(),
            new CoseHeader[] { signer },
            new TokenKey[] { key },
            policy);
    }

    static CoseHeader[] Headers(int[] algorithms)
    {
        CoseHeader[] result = new CoseHeader[algorithms.Length];
        int index = 0;
        while (index < algorithms.Length)
        {
            result[index] = Header(algorithms[index], (byte)(index + 1));
            index += 1;
        }

        return result;
    }

    static CoseHeader Header(int algorithm, byte keyId)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(algorithm));
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { keyId }));
        return header;
    }

    static TokenKey EcKey(ECDsa native, int algorithm, bool privateMaterial)
    {
        TokenKey key = EcdsaKeyImporter.ImportCose(
            native,
            algorithm,
            privateMaterial,
            IsCompatibility(algorithm)).Key;
        key.Use = TokenKeyUses.Signature;
        return key;
    }

    static bool IsCompatibility(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdsaSha256
            || algorithm == CoseAlgorithms.EcdsaSha384
            || algorithm == CoseAlgorithms.EcdsaSha512;
    }

    static TokenKey RsaKey(RSA native, int algorithm, bool privateMaterial)
    {
        TokenKey key = RsaKeyImporterOperations.ImportCose(
            native,
            algorithm,
            privateMaterial).Key;
        key.Use = TokenKeyUses.Signature;
        return key;
    }

    static ECDsa PublicPart(ECDsa source)
    {
        return ECDsa.Create(source.ExportParameters(false));
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static void SetUnknownCritical(CoseHeader header)
    {
        header.SetProtectedByTextLabel("private", Boolean(true));
        header.SetProtected(
            CoseHeaderLabels.Critical,
            Array(Text("private")));
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.Not.Null);
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }

    static void AssertFailure(CoseSignResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
