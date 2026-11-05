using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseSigningServicePublicTests
{
    [Test]
    public void Sign1AttachedPayloadRequiresVerificationOrDiagnosticOptIn()
    {
        ICoseSigningOperations service = new CoseSigningService();
        TokenPolicy policy = Policy();
        byte[] payload = Encoding.UTF8.GetBytes("public sign1 payload");
        byte[] externalData = Encoding.UTF8.GetBytes("sign1 aad");
        using ECDsa privateNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            privateNative.ExportParameters(false));
        TokenKey privateKey = Key(privateNative, true);
        TokenKey publicKey = Key(publicNative, false);

        TokenBytesResult created = service.CreateCoseSign1(
            payload,
            externalData,
            Header(policy, 1),
            privateKey,
            true,
            false,
            policy);
        CoseSign1Result parsed = service.ReadCoseSign1(
            created.Bytes,
            policy);
        CoseSign1Result denied = service.ReadCoseSign1Diagnostic(
            created.Bytes,
            policy);
        policy.AllowUnauthenticatedDecode = true;
        CoseSign1Result diagnostic =
            service.ReadCoseSign1Diagnostic(created.Bytes, policy);
        CoseSign1Result verified = service.VerifyCoseSign1(
            created.Bytes,
            externalData,
            publicKey,
            false,
            policy);
        CoseSign1Result wrongData = service.VerifyCoseSign1(
            created.Bytes,
            new byte[] { 9 },
            publicKey,
            false,
            policy);
        TokenBytesResult untagged = service.WriteCoseSign1(
            diagnostic.Message,
            false,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(parsed.Message.PayloadAvailable, Is.False);
            Assert.That(parsed.Message.SignatureVerified, Is.False);
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error.Code,
                Is.EqualTo("unauthenticated_decode_not_allowed"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Message.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Message.PayloadAvailable, Is.True);
            Assert.That(diagnostic.Message.SignatureVerified, Is.False);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.PayloadAvailable, Is.True);
            Assert.That(verified.Message.SignatureVerified, Is.True);
            Assert.That(wrongData.Success, Is.False);
            Assert.That(wrongData.Error.Code,
                Is.EqualTo("signature_mismatch"));
            Assert.That(untagged.Success, Is.True);
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
        });
    }

    [Test]
    public void Sign1DetachedPayloadAndExternalDataAreBound()
    {
        ICoseSigningOperations service = new CoseSigningService();
        TokenPolicy policy = Policy();
        byte[] payload = Encoding.UTF8.GetBytes("detached sign1");
        byte[] externalData = new byte[] { 1, 2, 3 };
        using ECDsa privateNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            privateNative.ExportParameters(false));

        TokenBytesResult created = service.CreateDetachedCoseSign1(
            payload,
            externalData,
            Header(policy, 2),
            Key(privateNative, true),
            false,
            false,
            policy);
        CoseSign1Result parsed = service.ReadCoseSign1(
            created.Bytes,
            policy);
        CoseSign1Result verified = service.VerifyDetachedCoseSign1(
            created.Bytes,
            payload,
            externalData,
            Key(publicNative, false),
            false,
            policy);
        CoseSign1Result wrongPayload =
            service.VerifyDetachedCoseSign1(
                created.Bytes,
                new byte[] { 9 },
                externalData,
                Key(publicNative, false),
                false,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Bytes[0], Is.EqualTo(0x84));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.DetachedPayload, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(parsed.Message.PayloadAvailable, Is.False);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.PayloadAvailable, Is.True);
            Assert.That(wrongPayload.Success, Is.False);
            Assert.That(wrongPayload.Error.Code,
                Is.EqualTo("signature_mismatch"));
        });
    }

    [Test]
    public void SignSupportsMultipleSignersAttachedAndDetached()
    {
        ICoseSigningOperations service = new CoseSigningService();
        TokenPolicy policy = Policy();
        byte[] payload = Encoding.UTF8.GetBytes("shared payload");
        byte[] externalData = Encoding.UTF8.GetBytes("shared aad");
        using ECDsa firstPrivate = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa secondPrivate = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa firstPublic = ECDsa.Create(
            firstPrivate.ExportParameters(false));
        using ECDsa secondPublic = ECDsa.Create(
            secondPrivate.ExportParameters(false));
        CoseHeader[] headers = new CoseHeader[]
        {
            Header(policy, 1),
            Header(policy, 2)
        };
        TokenKey[] privateKeys = new TokenKey[]
        {
            Key(firstPrivate, true),
            Key(secondPrivate, true)
        };
        TokenKey[] publicKeys = new TokenKey[]
        {
            Key(firstPublic, false),
            Key(secondPublic, false)
        };

        TokenBytesResult attached = service.CreateCoseSign(
            payload,
            externalData,
            new CoseHeader(),
            headers,
            privateKeys,
            true,
            false,
            policy);
        CoseSignResult parsed = service.ReadCoseSign(
            attached.Bytes,
            policy);
        CoseSignResult denied = service.ReadCoseSignDiagnostic(
            attached.Bytes,
            policy);
        TokenPolicy diagnosticPolicy = Policy();
        diagnosticPolicy.AllowUnauthenticatedDecode = true;
        CoseSignResult diagnostic = service.ReadCoseSignDiagnostic(
            attached.Bytes,
            diagnosticPolicy);
        CoseSignResult verified = service.VerifyCoseSign(
            attached.Bytes,
            externalData,
            publicKeys,
            false,
            policy);
        TokenBytesResult detached = service.CreateDetachedCoseSign(
            payload,
            externalData,
            new CoseHeader(),
            headers,
            privateKeys,
            false,
            false,
            policy);
        CoseSignResult detachedVerified =
            service.VerifyDetachedCoseSign(
                detached.Bytes,
                payload,
                externalData,
                publicKeys,
                false,
                policy);
        TokenBytesResult rewritten = service.WriteCoseSign(
            verified.Message,
            false,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            Assert.That(parsed.Message.PayloadAvailable, Is.False);
            Assert.That(parsed.Message.Signatures, Has.Length.EqualTo(2));
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error.Code,
                Is.EqualTo("unauthenticated_decode_not_allowed"));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Message.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Message.PayloadAvailable, Is.True);
            Assert.That(diagnostic.Message.SignaturesVerified, Is.False);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Message.Payload, Is.EqualTo(payload));
            Assert.That(verified.Message.PayloadAvailable, Is.True);
            Assert.That(verified.Message.SignaturesVerified, Is.True);
            Assert.That(verified.Message.Signatures[0].SignatureVerified,
                Is.True);
            Assert.That(verified.Message.Signatures[1].SignatureVerified,
                Is.True);
            Assert.That(detached.Success, Is.True);
            Assert.That(detachedVerified.Success, Is.True);
            Assert.That(detachedVerified.Message.Payload,
                Is.EqualTo(payload));
            Assert.That(detachedVerified.Message.PayloadAvailable, Is.True);
            Assert.That(rewritten.Success, Is.True);
            Assert.That(rewritten.Bytes[0], Is.EqualTo(0x84));
        });
    }

    [Test]
    public void Sign1RoundTripsEveryBuiltInSignatureFamily()
    {
        ICoseSigningOperations service = new CoseSigningService();
        byte[] payload = Encoding.UTF8.GetBytes("public signature families");
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using ECDsa p384Public = ECDsa.Create(
            p384.ExportParameters(false));
        using ECDsa p521 = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        using ECDsa p521Public = ECDsa.Create(
            p521.ExportParameters(false));
        using RSA rsa = RSA.Create(2048);
        using RSA rsaPublic = RSA.Create();
        rsaPublic.ImportParameters(rsa.ExportParameters(false));

        CoseSign1Result es384 = RoundTripSign1(
            service,
            payload,
            CoseAlgorithms.EcdsaP384Sha384,
            EcKey(p384, CoseAlgorithms.EcdsaP384Sha384, true),
            EcKey(p384Public, CoseAlgorithms.EcdsaP384Sha384, false),
            AlgorithmPolicy("ES384"));
        CoseSign1Result es512 = RoundTripSign1(
            service,
            payload,
            CoseAlgorithms.EcdsaP521Sha512,
            EcKey(p521, CoseAlgorithms.EcdsaP521Sha512, true),
            EcKey(p521Public, CoseAlgorithms.EcdsaP521Sha512, false),
            AlgorithmPolicy("ES512"));
        CoseSign1Result rs256 = RoundTripSign1(
            service,
            payload,
            CoseAlgorithms.RsaSha256,
            RsaKey(rsa, CoseAlgorithms.RsaSha256, true),
            RsaKey(rsaPublic, CoseAlgorithms.RsaSha256, false),
            AlgorithmPolicy("RS256"));
        CoseSign1Result ps512 = RoundTripSign1(
            service,
            payload,
            CoseAlgorithms.RsaPssSha512,
            RsaKey(rsa, CoseAlgorithms.RsaPssSha512, true),
            RsaKey(rsaPublic, CoseAlgorithms.RsaPssSha512, false),
            AlgorithmPolicy("PS512"));

        Assert.Multiple(() =>
        {
            Assert.That(es384.Message.Signature, Has.Length.EqualTo(96));
            Assert.That(es512.Message.Signature, Has.Length.EqualTo(132));
            Assert.That(rs256.Message.Signature, Has.Length.EqualTo(256));
            Assert.That(ps512.Message.Signature, Has.Length.EqualTo(256));
            Assert.That(es384.Message.Payload, Is.EqualTo(payload));
            Assert.That(es512.Message.SignatureVerified, Is.True);
            Assert.That(rs256.Message.SignatureVerified, Is.True);
            Assert.That(ps512.Message.SignatureVerified, Is.True);
        });
    }

    [Test]
    public void Sign1TamperingAlgorithmPolicyAndResourceLimitsFailClosed()
    {
        ICoseSigningOperations service = new CoseSigningService();
        TokenPolicy policy = Policy();
        using ECDsa privateNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            privateNative.ExportParameters(false));
        TokenKey privateKey = Key(privateNative, true);
        TokenKey publicKey = Key(publicNative, false);
        TokenBytesResult created = service.CreateCoseSign1(
            Encoding.UTF8.GetBytes("authenticated COSE message"),
            System.Array.Empty<byte>(),
            Header(policy, 1),
            privateKey,
            true,
            false,
            policy);
        byte[] tampered = created.Bytes!.ToArray();
        tampered[^1] ^= 1;
        CoseSign1Result tamperedResult = service.VerifyCoseSign1(
            tampered,
            System.Array.Empty<byte>(),
            publicKey,
            false,
            policy);
        CoseSign1Result denied = service.VerifyCoseSign1(
            created.Bytes,
            System.Array.Empty<byte>(),
            publicKey,
            false,
            new TokenPolicy());
        TokenPolicy small = Policy();
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        CoseSign1Result tooLarge = service.ReadCoseSign1(
            created.Bytes,
            small);
        TokenPolicy shallow = Policy();
        shallow.MaximumCborDepth = 1;
        CoseSign1Result tooDeep = service.ReadCoseSign1(
            created.Bytes,
            shallow);

        Assert.Multiple(() =>
        {
            Assert.That(tamperedResult.Success, Is.False);
            Assert.That(
                tamperedResult.Error!.Code,
                Is.EqualTo("signature_mismatch"));
            Assert.That(tamperedResult.Message, Is.Null);
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error!.Code, Is.EqualTo("algorithm_not_allowed"));
            Assert.That(tooLarge.Success, Is.False);
            Assert.That(tooLarge.Error!.Code, Is.EqualTo("cbor_size_exceeded"));
            Assert.That(tooDeep.Success, Is.False);
            Assert.That(tooDeep.Error!.Code, Is.EqualTo("cbor_depth_exceeded"));
        });
    }

    [Test]
    public void FullAndAbbreviatedCounterSignaturesRoundTripAndVerify()
    {
        ICoseSigningOperations service = new CoseSigningService();
        TokenPolicy policy = Policy();
        byte[] externalData = Encoding.UTF8.GetBytes("counter aad");
        CoseCounterSignatureTarget target =
            new CoseCounterSignatureTarget();
        target.BodyProtected = Convert.FromHexString("A10101");
        target.Payload = Encoding.UTF8.GetBytes("counter target");
        target.OtherFields = new byte[][] { new byte[] { 4, 5 } };
        using ECDsa privateNative = ECDsa.Create(
            ECCurve.NamedCurves.nistP256);
        using ECDsa publicNative = ECDsa.Create(
            privateNative.ExportParameters(false));
        TokenKey privateKey = Key(privateNative, true);
        TokenKey publicKey = Key(publicNative, false);

        CoseCounterSignatureResult created =
            service.CreateCoseCounterSignature(
                target,
                externalData,
                Header(policy, 7),
                privateKey,
                true,
                false,
                policy);
        TokenBytesResult written = service.WriteCoseCounterSignature(
            created.CounterSignature,
            true,
            policy);
        CoseCounterSignatureResult parsed =
            service.ReadCoseCounterSignature(written.Bytes, policy);
        TokenResult verified = service.VerifyCoseCounterSignature(
            target,
            externalData,
            parsed.CounterSignature,
            publicKey,
            false,
            policy);
        CoseHeader containingHeader = new CoseHeader();
        TokenResult stored = service.SetCoseCounterSignaturesOnHeader(
            containingHeader,
            new CoseCounterSignature[] { parsed.CounterSignature },
            policy);
        CoseCounterSignatureCollectionResult fromHeader =
            service.ReadCoseCounterSignaturesFromHeader(
                containingHeader,
                policy);
        TokenBytesResult abbreviated =
            service.CreateAbbreviatedCoseCounterSignature(
                target,
                externalData,
                CoseAlgorithms.EcdsaP256Sha256,
                privateKey,
                false,
                policy);
        TokenResult abbreviatedStored =
            service.SetAbbreviatedCoseCounterSignatureOnHeader(
                containingHeader,
                abbreviated.Bytes);
        TokenBytesResult abbreviatedRead =
            service.ReadAbbreviatedCoseCounterSignatureFromHeader(
                containingHeader);
        TokenResult abbreviatedVerified =
            service.VerifyAbbreviatedCoseCounterSignature(
                target,
                externalData,
                abbreviatedRead.Bytes,
                CoseAlgorithms.EcdsaP256Sha256,
                publicKey,
                false,
                policy);
        CoseCounterSignatureTarget changed =
            new CoseCounterSignatureTarget();
        changed.BodyProtected = target.BodyProtected;
        changed.Payload = new byte[] { 9 };
        changed.OtherFields = target.OtherFields;
        TokenResult changedResult = service.VerifyCoseCounterSignature(
            changed,
            externalData,
            parsed.CounterSignature,
            publicKey,
            false,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes[0], Is.EqualTo(0xD3));
            Assert.That(parsed.Success, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(parsed.CounterSignature.SignatureVerified, Is.True);
            Assert.That(stored.Success, Is.True);
            Assert.That(fromHeader.Success, Is.True);
            Assert.That(fromHeader.CounterSignatures,
                Has.Length.EqualTo(1));
            Assert.That(abbreviated.Success, Is.True);
            Assert.That(abbreviatedStored.Success, Is.True);
            Assert.That(abbreviatedRead.Success, Is.True);
            Assert.That(abbreviatedRead.Bytes,
                Is.EqualTo(abbreviated.Bytes));
            Assert.That(abbreviatedVerified.Success, Is.True);
            Assert.That(changedResult.Success, Is.False);
            Assert.That(changedResult.Error.Code,
                Is.EqualTo("signature_mismatch"));
        });
    }

    static CoseHeader Header(TokenPolicy policy, byte keyId)
    {
        return AlgorithmHeader(
            policy,
            CoseAlgorithms.EcdsaP256Sha256,
            keyId);
    }

    static CoseHeader AlgorithmHeader(
        TokenPolicy policy,
        int algorithm,
        byte keyId)
    {
        CoseHeader header = new CoseHeader();
        header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(
                CoseHeaderLabels.Algorithm,
                Int(algorithm))
        };
        header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { keyId }))
        };
        TokenBytesResult prepared = new CoseTokenService()
            .WriteCoseHeader(header, policy);
        Assert.That(prepared.Success, Is.True);
        return header;
    }

    static TokenKey Key(ECDsa native, bool privateMaterial)
    {
        return EcKey(
            native,
            CoseAlgorithms.EcdsaP256Sha256,
            privateMaterial);
    }

    static TokenKey EcKey(
        ECDsa native,
        int algorithm,
        bool privateMaterial)
    {
        TokenKeyResult imported = new TokenKeyService()
            .ImportCoseEc2Key(
                native,
                algorithm,
                privateMaterial);
        Assert.That(imported.Success, Is.True);
        imported.Key.Use = TokenKeyUses.Signature;
        return imported.Key;
    }

    static TokenKey RsaKey(
        RSA native,
        int algorithm,
        bool privateMaterial)
    {
        TokenKeyResult imported = new TokenKeyService().ImportCoseRsaKey(
            native,
            algorithm,
            privateMaterial);
        Assert.That(imported.Success, Is.True);
        imported.Key!.Use = TokenKeyUses.Signature;
        return imported.Key;
    }

    static CoseSign1Result RoundTripSign1(
        ICoseSigningOperations service,
        byte[] payload,
        int algorithm,
        TokenKey privateKey,
        TokenKey publicKey,
        TokenPolicy policy)
    {
        TokenBytesResult created = service.CreateCoseSign1(
            payload,
            System.Array.Empty<byte>(),
            AlgorithmHeader(policy, algorithm, 1),
            privateKey,
            true,
            false,
            policy);
        Assert.That(created.Success, Is.True, created.Error?.Code);
        CoseSign1Result verified = service.VerifyCoseSign1(
            created.Bytes!,
            System.Array.Empty<byte>(),
            publicKey,
            false,
            policy);
        Assert.That(verified.Success, Is.True, verified.Error?.Code);
        return verified;
    }

    static TokenPolicy Policy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = new string[] { "ES256" };
        return policy;
    }

    static TokenPolicy AlgorithmPolicy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
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
}
