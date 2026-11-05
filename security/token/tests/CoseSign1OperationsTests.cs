using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseSign1OperationsTests
{
    [Test]
    public void DetachedPayloadAndExternalAuthenticatedDataAreBound()
    {
        using ECDsa signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa verifier = PublicPart(signer);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        byte[] payload = Encoding.UTF8.GetBytes("detached signature payload");
        byte[] externalData = Encoding.UTF8.GetBytes("sign1 aad");
        TokenPolicy policy = Policy("ES256");
        TokenKey publicKey = EcKey(verifier, algorithm, false);

        TokenBytesResult created = CoseSign1Operations.CreateDetached(
            payload,
            externalData,
            Header(algorithm),
            EcKey(signer, algorithm, true),
            true,
            false,
            policy);
        CoseSign1Result parsed = CoseSign1.Read(created.Bytes);
        CoseSign1Result verified = CoseSign1Operations.VerifyDetached(
            created.Bytes,
            payload,
            externalData,
            publicKey,
            false,
            policy);
        CoseSign1Result wrongPayload = CoseSign1Operations.VerifyDetached(
            created.Bytes,
            new byte[] { 9 },
            externalData,
            publicKey,
            false,
            policy);
        CoseSign1Result wrongData = CoseSign1Operations.VerifyDetached(
            created.Bytes,
            payload,
            new byte[] { 9 },
            publicKey,
            false,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.DetachedPayload, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            AssertFailure(wrongPayload, "signature_mismatch");
            AssertFailure(wrongData, "signature_mismatch");
        });
    }

    [Test]
    public void CreateAndVerifyEcdsaAndRsaMessages()
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
        byte[] payload = Encoding.UTF8.GetBytes("signed once");

        CoseSign1Result p256Result = CreateAndVerify(
            payload,
            Header(CoseAlgorithms.EcdsaP256Sha256),
            EcKey(p256, CoseAlgorithms.EcdsaP256Sha256, true),
            EcKey(p256Public, CoseAlgorithms.EcdsaP256Sha256, false),
            Policy("ES256"));
        CoseSign1Result p384Result = CreateAndVerify(
            payload,
            Header(CoseAlgorithms.EcdsaP384Sha384),
            EcKey(p384, CoseAlgorithms.EcdsaP384Sha384, true),
            EcKey(p384Public, CoseAlgorithms.EcdsaP384Sha384, false),
            Policy("ES384"));
        CoseSign1Result p521Result = CreateAndVerify(
            payload,
            Header(CoseAlgorithms.EcdsaP521Sha512),
            EcKey(p521, CoseAlgorithms.EcdsaP521Sha512, true),
            EcKey(p521Public, CoseAlgorithms.EcdsaP521Sha512, false),
            Policy("ES512"));
        CoseSign1Result rsaResult = CreateAndVerify(
            payload,
            Header(CoseAlgorithms.RsaSha256),
            RsaKey(rsa, CoseAlgorithms.RsaSha256, true),
            RsaKey(rsaPublic, CoseAlgorithms.RsaSha256, false),
            Policy("RS256"));
        CoseSign1Result pssResult = CreateAndVerify(
            payload,
            Header(CoseAlgorithms.RsaPssSha512),
            RsaKey(rsa, CoseAlgorithms.RsaPssSha512, true),
            RsaKey(rsaPublic, CoseAlgorithms.RsaPssSha512, false),
            Policy("PS512"));

        Assert.Multiple(() =>
        {
            AssertVerified(p256Result, payload, 64);
            AssertVerified(p384Result, payload, 96);
            AssertVerified(p521Result, payload, 132);
            AssertVerified(rsaResult, payload, 256);
            AssertVerified(pssResult, payload, 256);
        });
    }

    [Test]
    public void CreationSignsTheExactSignature1Structure()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        byte[] payload = Encoding.ASCII.GetBytes("bound content");
        CoseHeader header = Header(algorithm);
        header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("text/plain"));
        TokenKey publicKey = EcKey(publicNative, algorithm, false);

        TokenBytesResult created = CoseSign1Operations.Create(
            payload,
            header,
            EcKey(native, algorithm, true),
            Policy("ES256"));
        CoseSign1Result parsed = CoseSign1.Read(created.Bytes);
        TokenBytesResult structure = CoseSigStructure.CreateSignature1(
            parsed.Message.Header.ProtectedBytes,
            payload);
        TokenResult independentlyVerified = CoseEcdsaAlgorithms.Verify(
            structure.Bytes,
            parsed.Message.Signature,
            publicKey,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.SignatureVerified, Is.False);
            Assert.That(independentlyVerified.Success, Is.True);
            Assert.That(parsed.Message.Header.ContentTypeText,
                Is.EqualTo("text/plain"));
        });
    }

    [Test]
    public void ChangedProtectedHeaderPayloadOrSignatureFailsClosed()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenKey privateKey = EcKey(native, algorithm, true);
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        TokenBytesResult created = CoseSign1Operations.Create(
            new byte[] { 1, 2, 3 },
            Header(algorithm),
            privateKey,
            policy);

        CoseSign1 signatureModel = CoseSign1.Read(created.Bytes).Message;
        signatureModel.Signature[0] ^= 1;
        CoseSign1 payloadModel = CoseSign1.Read(created.Bytes).Message;
        payloadModel.Payload[0] ^= 1;
        CoseSign1 headerModel = CoseSign1.Read(created.Bytes).Message;
        headerModel.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("changed"));

        CoseSign1Result signatureResult = CoseSign1Operations.Verify(
            signatureModel.Write().Bytes,
            publicKey,
            policy);
        CoseSign1Result payloadResult = CoseSign1Operations.Verify(
            payloadModel.Write().Bytes,
            publicKey,
            policy);
        CoseSign1Result headerResult = CoseSign1Operations.Verify(
            headerModel.Write().Bytes,
            publicKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(signatureResult, "signature_mismatch");
            AssertFailure(payloadResult, "signature_mismatch");
            AssertFailure(headerResult, "signature_mismatch");
        });
    }

    [Test]
    public void WrongVerificationKeyFailsClosed()
    {
        using ECDsa signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa wrong = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa wrongPublic = PublicPart(wrong);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult created = CoseSign1Operations.Create(
            new byte[] { 1 },
            Header(algorithm),
            EcKey(signer, algorithm, true),
            policy);

        CoseSign1Result result = CoseSign1Operations.Verify(
            created.Bytes,
            EcKey(wrongPublic, algorithm, false),
            policy);

        AssertFailure(result, "signature_mismatch");
    }

    [Test]
    public void AlgorithmMustBeIntegerAndProtected()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey key = EcKey(native, algorithm, true);
        TokenPolicy policy = Policy("ES256");
        CoseHeader missing = new CoseHeader();
        CoseHeader unprotected = new CoseHeader();
        unprotected.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        CoseHeader text = new CoseHeader();
        text.SetProtected(CoseHeaderLabels.Algorithm, Text("ES256"));

        TokenBytesResult missingResult = Create(missing, key, policy);
        TokenBytesResult unprotectedResult = Create(
            unprotected,
            key,
            policy);
        TokenBytesResult textResult = Create(text, key, policy);

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
    public void AlgorithmRegistrationCompatibilityAndPolicyAreEnforced()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        TokenKey currentKey = EcKey(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            true);
        TokenKey compatibleKey = EcKey(
            native,
            CoseAlgorithms.EcdsaSha256,
            true);
        TokenKey compatiblePublic = EcKey(
            publicNative,
            CoseAlgorithms.EcdsaSha256,
            false);
        TokenPolicy allowed = Policy("ES256");

        TokenBytesResult unknown = Create(
            Header(123456),
            currentKey,
            allowed);
        TokenBytesResult notSignature = Create(
            Header(CoseAlgorithms.HmacSha256),
            currentKey,
            Policy("HMAC 256/256"));
        TokenBytesResult denied = Create(
            Header(CoseAlgorithms.EcdsaP256Sha256),
            currentKey,
            Policy("PS256"));
        TokenBytesResult conservative = Create(
            Header(CoseAlgorithms.EcdsaSha256),
            compatibleKey,
            allowed);
        TokenBytesResult compatible = CoseSign1Operations.Create(
            new byte[] { 1 },
            Header(CoseAlgorithms.EcdsaSha256),
            compatibleKey,
            true,
            true,
            allowed);
        CoseSign1Result conservativeVerify =
            CoseSign1Operations.Verify(
                compatible.Bytes,
                compatiblePublic,
                allowed);
        CoseSign1Result compatibleVerify = CoseSign1Operations.Verify(
            compatible.Bytes,
            compatiblePublic,
            true,
            allowed);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(unknown, "unknown_algorithm");
            AssertBytesFailure(notSignature, "invalid_algorithm");
            AssertBytesFailure(denied, "algorithm_not_allowed");
            AssertBytesFailure(conservative, "compatibility_required");
            AssertSuccess(compatible);
            AssertFailure(conservativeVerify, "compatibility_required");
            Assert.That(compatibleVerify.Success, Is.True);
            Assert.That(compatibleVerify.Message.SignatureVerified,
                Is.True);
        });
    }

    [Test]
    public void CriticalHeadersAreValidatedForCreateAndVerify()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenKey privateKey = EcKey(native, algorithm, true);
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        CoseHeader invalid = Header(algorithm);
        SetUnknownCritical(invalid);
        TokenBytesResult createResult = Create(
            invalid,
            privateKey,
            policy);

        TokenBytesResult valid = Create(
            Header(algorithm),
            privateKey,
            policy);
        CoseSign1 model = CoseSign1.Read(valid.Bytes).Message;
        SetUnknownCritical(model.Header);
        CoseSign1Result verifyResult = CoseSign1Operations.Verify(
            model.Write().Bytes,
            publicKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                createResult,
                "unknown_cose_critical_header");
            AssertFailure(
                verifyResult,
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
        TokenBytesResult signing = Create(
            Header(algorithm),
            signingKey,
            policy);

        TokenBytesResult created = Create(
            Header(algorithm),
            EcKey(privateNative, algorithm, true),
            policy);
        TokenKey verificationKey = EcKey(
            publicNative,
            algorithm,
            false);
        verificationKey.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationSign };
        CoseSign1Result verification = CoseSign1Operations.Verify(
            created.Bytes,
            verificationKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(signing, "key_operation_not_permitted");
            AssertFailure(verification, "key_operation_not_permitted");
        });
    }

    [Test]
    public void ExplicitTagEmptyPayloadAndDetachedRequirementAreHandled()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        TokenKey privateKey = EcKey(native, algorithm, true);
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        TokenBytesResult untagged = CoseSign1Operations.Create(
            System.Array.Empty<byte>(),
            Header(algorithm),
            privateKey,
            false,
            false,
            policy);
        CoseSign1Result verified = CoseSign1Operations.Verify(
            untagged.Bytes,
            publicKey,
            policy);
        CoseSign1 detached = CoseSign1.Read(untagged.Bytes).Message;
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseSign1Result detachedResult = CoseSign1Operations.Verify(
            detached.Write().Bytes,
            publicKey,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Tagged, Is.False);
            Assert.That(verified.Message.Payload, Is.Empty);
            AssertFailure(detachedResult, "missing_payload");
        });
    }

    [Test]
    public void StructuralFailuresAreReturnedBeforeVerification()
    {
        using ECDsa native = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = PublicPart(native);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenKey publicKey = EcKey(publicNative, algorithm, false);
        TokenPolicy policy = Policy("ES256");
        TokenBytesResult signed = Create(
            Header(algorithm),
            EcKey(native, algorithm, true),
            policy);

        CoseSign1Result invalid = CoseSign1Operations.Verify(
            new byte[] { 0x9F },
            publicKey,
            policy);
        CoseSign1Result trailing = CoseSign1Operations.Verify(
            signed.Bytes.Concat(new byte[] { 0 }).ToArray(),
            publicKey,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(invalid, "unexpected_end_of_cbor");
            AssertFailure(trailing, "trailing_cose_message_data");
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
        TokenBytesResult created = Create(
            Header(algorithm),
            privateKey,
            policy);
        TokenPolicy small = Policy("ES256");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        TokenPolicy invalid = Policy("ES256");
        invalid.AllowedAlgorithms = null;

        TokenBytesResult smallCreate = Create(
            Header(algorithm),
            privateKey,
            small);
        CoseSign1Result smallVerify = CoseSign1Operations.Verify(
            created.Bytes,
            publicKey,
            small);
        TokenBytesResult invalidCreate = Create(
            Header(algorithm),
            privateKey,
            invalid);
        CoseSign1Result invalidVerify = CoseSign1Operations.Verify(
            created.Bytes,
            publicKey,
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
        TokenKey key = new TokenKey();
        TokenPolicy policy = Policy("ES256");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseSign1Operations.Create(
                null, header, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Create(
                new byte[0], null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Create(
                new byte[0], header, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Create(
                new byte[0], header, key, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Verify(
                null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Verify(
                new byte[0], null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign1Operations.Verify(
                new byte[0], key, null),
                Throws.ArgumentNullException);
        });
    }

    static CoseSign1Result CreateAndVerify(
        byte[] payload,
        CoseHeader header,
        TokenKey privateKey,
        TokenKey publicKey,
        TokenPolicy policy)
    {
        TokenBytesResult created = CoseSign1Operations.Create(
            payload,
            header,
            privateKey,
            policy);
        AssertSuccess(created);
        return CoseSign1Operations.Verify(
            created.Bytes,
            publicKey,
            policy);
    }

    static TokenBytesResult Create(
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy)
    {
        return CoseSign1Operations.Create(
            new byte[] { 1, 2, 3 },
            header,
            key,
            policy);
    }

    static CoseHeader Header(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(new byte[] { 1 }));
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

    static void AssertVerified(
        CoseSign1Result result,
        byte[] payload,
        int signatureLength)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Message.SignatureVerified, Is.True);
        Assert.That(result.Message.Payload, Is.EqualTo(payload));
        Assert.That(result.Message.Signature,
            Has.Length.EqualTo(signatureLength));
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

    static void AssertFailure(CoseSign1Result result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
