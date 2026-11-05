using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class JsonWebSignatureTests
{
    [Test]
    public void SignAndVerifyAttachedUnencodedPayloadUsesRawUtf8Bytes()
    {
        string payloadText = "raw payload \u2603 with spaces";
        byte[] payload = Encoding.UTF8.GetBytes(payloadText);
        byte[] keyBytes = Sequence(32, 181);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignature.Sign(
            payload, header, key, policy);
        string headerSegment = Base64Url.Encode(header.WriteUtf8().Bytes).Text;
        string signingText = headerSegment + "." + payloadText;
        byte[] expectedSignature = HMACSHA256.HashData(
            keyBytes,
            Encoding.UTF8.GetBytes(signingText));
        string expected = signingText + "."
            + Base64Url.Encode(expectedSignature).Text;
        JwsCompactResult verified = JsonWebSignature.Verify(
            signed.Text, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Text, Is.EqualTo(expected));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.SignatureVerified, Is.True);
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParseAndDiagnosticReadUnderstandAttachedUnencodedPayload()
    {
        byte[] keyBytes = Sequence(32, 191);
        string payloadText = "not_base64 + / = \u03c0";
        byte[] payload = Encoding.UTF8.GetBytes(payloadText);
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy signingPolicy = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignature.Sign(
            payload,
            header,
            SymmetricKeyImporterOperations.Import(keyBytes).Key,
            signingPolicy);
        TokenPolicy diagnosticPolicy = DiagnosticPolicy();

        JwsCompactResult parsed = JsonWebSignature.Parse(signed.Text);
        JwsCompactResult diagnostic = JsonWebSignature.ReadDiagnostic(
            signed.Text, diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.Payload, Is.EqualTo(payload));
            Assert.That(diagnostic.Token.SignatureVerified, Is.False);
        });
    }

    [Test]
    public void DetachedUnencodedPayloadSupportsArbitraryBinaryBytes()
    {
        byte[] payload = new byte[] { 0, 255, 46, 128, 1, 2, 3 };
        byte[] keyBytes = Sequence(32, 201);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult signed = JsonWebSignature.SignDetached(
            payload, header, key, policy);
        string headerSegment = Base64Url.Encode(header.WriteUtf8().Bytes).Text;
        byte[] prefix = Encoding.ASCII.GetBytes(headerSegment + ".");
        byte[] signingInput = new byte[prefix.Length + payload.Length];
        prefix.CopyTo(signingInput, 0);
        payload.CopyTo(signingInput, prefix.Length);
        byte[] expectedSignature = HMACSHA256.HashData(keyBytes, signingInput);
        string expected = headerSegment + ".."
            + Base64Url.Encode(expectedSignature).Text;
        JwsCompactResult verified = JsonWebSignature.VerifyDetached(
            signed.Text, payload, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Text, Is.EqualTo(expected));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(verified.Token.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ExplicitB64TrueRemainsEncodedAndMustBeCritical()
    {
        byte[] payload = Encoding.ASCII.GetBytes("encoded payload");
        byte[] keyBytes = Sequence(32, 211);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);
        JoseHeader validHeader = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":true}");
        JoseHeader missingCritical = Header(
            "{\"alg\":\"HS256\",\"b64\":false}");

        TokenTextResult signed = JsonWebSignature.Sign(
            payload, validHeader, key, policy);
        JwsCompactResult verified = JsonWebSignature.Verify(
            signed.Text, key, policy);
        TokenTextResult rejected = JsonWebSignature.Sign(
            payload, missingCritical, key, policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Text.Split('.')[1],
                Is.EqualTo(Base64Url.Encode(payload).Text));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Token.PayloadBase64UrlEncoded, Is.True);
            AssertTextFailure(rejected, "missing_critical_header");
        });
    }

    [Test]
    public void AttachedUnencodedPayloadRejectsPeriodAndInvalidUtf8()
    {
        byte[] keyBytes = Sequence(32, 221);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult period = JsonWebSignature.Sign(
            Encoding.ASCII.GetBytes("contains.period"),
            header,
            key,
            policy);
        TokenTextResult invalidUtf8 = JsonWebSignature.Sign(
            new byte[] { 255 },
            header,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(period, "invalid_unencoded_payload");
            AssertTextFailure(invalidUtf8, "invalid_utf8");
        });
    }

    [Test]
    public void UnencodedPayloadTamperingFailsVerification()
    {
        byte[] keyBytes = Sequence(32, 231);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignature.Sign(
            Encoding.ASCII.GetBytes("original raw payload"),
            header,
            key,
            policy);
        string tampered = signed.Text.Replace(
            "original raw payload",
            "tampered raw payload");

        JwsCompactResult result = JsonWebSignature.Verify(
            tampered, key, policy);

        AssertFailure(result, "signature_mismatch");
    }

    [Test]
    public void SignDetachedHmacOmitsPayloadAndUsesEncodedExternalBytes()
    {
        byte[] source = Sequence(29, 91);
        ReadOnlySpan<byte> payload = source.AsSpan(3, 19);
        byte[] expectedPayload = payload.ToArray();
        byte[] keyBytes = Sequence(32, 101);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header("{\"typ\":\"JWT\",\"alg\":\"HS256\"}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult result = JsonWebSignature.SignDetached(
            payload,
            header,
            key,
            policy);

        string headerSegment = Base64Url.Encode(header.WriteUtf8().Bytes).Text;
        string payloadSegment = Base64Url.Encode(expectedPayload).Text;
        string signingText = headerSegment + "." + payloadSegment;
        byte[] expectedSignature = HMACSHA256.HashData(
            keyBytes,
            Encoding.ASCII.GetBytes(signingText));
        string expected = headerSegment + ".."
            + Base64Url.Encode(expectedSignature).Text;
        JoseCompactParseResult parsed = JoseCompact.Parse(result.Text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(parsed.Serialization.GetEncodedSegment(1).Length,
                Is.EqualTo(0));
            Assert.That(parsed.Serialization.GetDecodedSegmentBytes(2),
                Is.EqualTo(expectedSignature));
            Assert.That(source, Is.EqualTo(Sequence(29, 91)));
        });
    }

    [Test]
    public void VerifyDetachedAuthenticatesAndReturnsAnIndependentPayloadCopy()
    {
        byte[] payload = Encoding.UTF8.GetBytes("detached payload \u2603");
        byte[] original = payload.ToArray();
        byte[] keyBytes = Sequence(32, 111);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignature.SignDetached(
            payload,
            Header("{\"alg\":\"HS256\",\"kid\":\"detached-1\"}"),
            key,
            policy);

        JwsCompactResult result = JsonWebSignature.VerifyDetached(
            signed.Text,
            payload,
            key,
            policy);
        payload[0] ^= 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Token.SignatureVerified, Is.True);
            Assert.That(result.Token.PayloadAvailable, Is.True);
            Assert.That(result.Token.Payload, Is.EqualTo(original));
            Assert.That(result.Token.Payload, Is.Not.SameAs(payload));
            Assert.That(result.Token.KeyId, Is.EqualTo("detached-1"));
        });
    }

    [Test]
    public void DetachedRsaPkcsAndPssRoundTripWithPublicOnlyKey()
    {
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey signingKey = RsaKeyImporterOperations.Import(privateRsa, true).Key;
        TokenKey verificationKey = RsaKeyImporterOperations.Import(publicRsa, false).Key;
        byte[] payload = Sequence(43, 121);
        string[] algorithms = new string[]
        {
            JoseAlgorithms.RsaSha256,
            JoseAlgorithms.RsaPssSha256
        };
        TokenPolicy policy = SigningPolicy(algorithms);

        int index = 0;
        while (index < algorithms.Length)
        {
            TokenTextResult signed = JsonWebSignature.SignDetached(
                payload,
                Header("{\"alg\":\"" + algorithms[index] + "\"}"),
                signingKey,
                policy);
            JwsCompactResult verified = JsonWebSignature.VerifyDetached(
                signed.Text,
                payload,
                verificationKey,
                policy);

            Assert.Multiple(() =>
            {
                Assert.That(signed.Success, Is.True);
                Assert.That(signed.Text.Split('.')[1], Is.Empty);
                Assert.That(verified.Success, Is.True);
                Assert.That(verified.Token.SignatureVerified, Is.True);
                Assert.That(verified.Token.Payload, Is.EqualTo(payload));
                Assert.That(verified.Token.Algorithm, Is.EqualTo(algorithms[index]));
            });

            index += 1;
        }
    }

    [Test]
    public void VerifyDetachedAcceptsExternalEcdsaRawSignature()
    {
        using ECDsa privateEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEcdsa = ECDsa.Create(
            privateEcdsa.ExportParameters(false));
        byte[] payload = Sequence(35, 131);
        string headerSegment = Base64Url.Encode(
            Encoding.UTF8.GetBytes("{\"alg\":\"ES256\"}")).Text;
        string signingText = headerSegment + "." + Base64Url.Encode(payload).Text;
        byte[] signature = privateEcdsa.SignData(
            Encoding.ASCII.GetBytes(signingText),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string compact = headerSegment + ".." + Base64Url.Encode(signature).Text;
        TokenKey key = EcdsaKeyImporter.Import(publicEcdsa, false).Key;

        JwsCompactResult result = JsonWebSignature.VerifyDetached(
            compact,
            payload,
            key,
            SigningPolicy(JoseAlgorithms.EcdsaSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Payload, Is.EqualTo(payload));
            Assert.That(result.Token.Signature, Has.Length.EqualTo(64));
            Assert.That(result.Token.SignatureVerified, Is.True);
        });
    }

    [Test]
    public void VerifyDetachedRejectsAttachedSerializationAndPayloadTampering()
    {
        byte[] payload = Encoding.ASCII.GetBytes("original detached payload");
        byte[] wrongPayload = Encoding.ASCII.GetBytes("altered detached payload!");
        byte[] keyBytes = Sequence(32, 141);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenTextResult detached = JsonWebSignature.SignDetached(
            payload,
            Header("{\"alg\":\"HS256\"}"),
            key,
            policy);
        TokenTextResult attached = JsonWebSignature.Sign(
            payload,
            Header("{\"alg\":\"HS256\"}"),
            key,
            policy);

        JwsCompactResult substituted = JsonWebSignature.VerifyDetached(
            detached.Text, wrongPayload, key, policy);
        JwsCompactResult notDetached = JsonWebSignature.VerifyDetached(
            attached.Text, payload, key, policy);
        JwsCompactResult missingExternalPayload = JsonWebSignature.Verify(
            detached.Text, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(substituted, "signature_mismatch");
            AssertFailure(notDetached, "invalid_detached_jws");
            AssertFailure(missingExternalPayload, "signature_mismatch");
        });
    }

    [Test]
    public void DetachedOperationsEnforcePayloadSizeAndAlgorithmPolicy()
    {
        byte[] payload = Sequence(101, 151);
        byte[] keyBytes = Sequence(32, 161);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy generous = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenTextResult signed = JsonWebSignature.SignDetached(
            payload, header, key, generous);
        TokenPolicy limited = SigningPolicy(JoseAlgorithms.HmacSha256);
        limited.MaximumTokenSizeInBytes = 100;
        TokenPolicy denied = new TokenPolicy();

        TokenTextResult signTooLarge = JsonWebSignature.SignDetached(
            payload, header, key, limited);
        JwsCompactResult verifyTooLarge = JsonWebSignature.VerifyDetached(
            signed.Text, payload, key, limited);
        TokenTextResult signDenied = JsonWebSignature.SignDetached(
            new byte[] { 1 }, header, key, denied);
        JwsCompactResult verifyDenied = JsonWebSignature.VerifyDetached(
            signed.Text, payload, key, denied);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            AssertTextFailure(signTooLarge, "token_too_large");
            AssertFailure(verifyTooLarge, "token_too_large");
            AssertTextFailure(signDenied, "algorithm_not_allowed");
            AssertFailure(verifyDenied, "algorithm_not_allowed");
        });
    }

    [Test]
    public void DetachedOperationsRejectNullArguments()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 171)).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebSignature.SignDetached(
                null, header, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.SignDetached(
                new byte[0], null, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.SignDetached(
                new byte[0], header, null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.SignDetached(
                new byte[0], header, key, null), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.VerifyDetached(
                null, new byte[0], key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.VerifyDetached(
                "text", null, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.VerifyDetached(
                "text", new byte[0], null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.VerifyDetached(
                "text", new byte[0], key, null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void VerifyHmacAuthenticatesExactCompactInputAndExposesPayload()
    {
        string headerText = "{\"typ\":\"JWT\",\r\n \"alg\":\"HS256\"}";
        byte[] payload = Encoding.UTF8.GetBytes("externally signed \u2603");
        byte[] keyBytes = Sequence(32, 41);
        string compact = HmacCompact(headerText, payload, keyBytes);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;

        JwsCompactResult result = JsonWebSignature.Verify(
            compact,
            key,
            SigningPolicy(JoseAlgorithms.HmacSha256));

        JoseCompactParseResult parsed = JoseCompact.Parse(compact);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Token.SignatureVerified, Is.True);
            Assert.That(result.Token.PayloadAvailable, Is.True);
            Assert.That(result.Token.Payload, Is.EqualTo(payload));
            Assert.That(result.Token.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Token.ProtectedHeader.Get(JoseHeaderNames.Type)
                .StringValue, Is.EqualTo("JWT"));
            Assert.That(result.Token.Signature,
                Is.EqualTo(parsed.Serialization.GetDecodedSegmentBytes(2)));
        });
    }

    [Test]
    public void VerifyRsaPkcsAndPssWithPublicOnlyKey()
    {
        using RSA privateRsa = RSA.Create(2048);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(privateRsa.ExportParameters(false));
        TokenKey signingKey = RsaKeyImporterOperations.Import(privateRsa, true).Key;
        TokenKey verificationKey = RsaKeyImporterOperations.Import(publicRsa, false).Key;
        byte[] payload = Sequence(29, 13);
        string[] algorithms = new string[]
        {
            JoseAlgorithms.RsaSha256,
            JoseAlgorithms.RsaPssSha256
        };
        TokenPolicy policy = SigningPolicy(algorithms);

        int index = 0;
        while (index < algorithms.Length)
        {
            TokenTextResult signed = JsonWebSignature.Sign(
                payload,
                Header("{\"alg\":\"" + algorithms[index] + "\"}"),
                signingKey,
                policy);
            JwsCompactResult verified = JsonWebSignature.Verify(
                signed.Text,
                verificationKey,
                policy);

            Assert.Multiple(() =>
            {
                Assert.That(signed.Success, Is.True);
                Assert.That(verified.Success, Is.True);
                Assert.That(verified.Token.SignatureVerified, Is.True);
                Assert.That(verified.Token.Payload, Is.EqualTo(payload));
                Assert.That(verified.Token.Algorithm, Is.EqualTo(algorithms[index]));
            });

            index += 1;
        }
    }

    [Test]
    public void VerifyEcdsaAcceptsExternalJoseRawSignatureWithPublicOnlyKey()
    {
        using ECDsa privateEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa publicEcdsa = ECDsa.Create(
            privateEcdsa.ExportParameters(false));
        byte[] payload = Sequence(27, 21);
        byte[] headerBytes = Encoding.UTF8.GetBytes("{\"alg\":\"ES256\"}");
        string headerSegment = Base64Url.Encode(headerBytes).Text;
        string payloadSegment = Base64Url.Encode(payload).Text;
        string signingText = headerSegment + "." + payloadSegment;
        byte[] signature = privateEcdsa.SignData(
            Encoding.ASCII.GetBytes(signingText),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string compact = signingText + "." + Base64Url.Encode(signature).Text;
        TokenKey key = EcdsaKeyImporter.Import(publicEcdsa, false).Key;

        JwsCompactResult result = JsonWebSignature.Verify(
            compact,
            key,
            SigningPolicy(JoseAlgorithms.EcdsaSha256));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.SignatureVerified, Is.True);
            Assert.That(result.Token.Payload, Is.EqualTo(payload));
            Assert.That(result.Token.Signature, Has.Length.EqualTo(64));
        });
    }

    [Test]
    public void VerifyRejectsHeaderPayloadAndSignatureTamperingWithoutExposure()
    {
        byte[] keyBytes = Sequence(32, 31);
        byte[] payload = Encoding.ASCII.GetBytes("original");
        string compact = HmacCompact(
            "{\"alg\":\"HS256\",\"kid\":\"one\"}",
            payload,
            keyBytes);
        JoseCompactParseResult parsed = JoseCompact.Parse(compact);
        string headerTampered = Base64Url.Encode(Encoding.UTF8.GetBytes(
            "{\"alg\":\"HS256\",\"kid\":\"two\"}")).Text
            + "." + parsed.Serialization.GetEncodedSegment(1).ToString()
            + "." + parsed.Serialization.GetEncodedSegment(2).ToString();
        string payloadTampered = parsed.Serialization.GetEncodedSegment(0).ToString()
            + "." + Base64Url.Encode(Encoding.ASCII.GetBytes("changed")).Text
            + "." + parsed.Serialization.GetEncodedSegment(2).ToString();
        byte[] alteredSignature = parsed.Serialization.GetDecodedSegmentBytes(2);
        alteredSignature[0] ^= 1;
        string signatureTampered = parsed.Serialization.GetEncodedSegment(0).ToString()
            + "." + parsed.Serialization.GetEncodedSegment(1).ToString()
            + "." + Base64Url.Encode(alteredSignature).Text;
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        JwsCompactResult wrongHeader = JsonWebSignature.Verify(
            headerTampered, key, policy);
        JwsCompactResult wrongPayload = JsonWebSignature.Verify(
            payloadTampered, key, policy);
        JwsCompactResult wrongSignature = JsonWebSignature.Verify(
            signatureTampered, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongHeader, "signature_mismatch");
            AssertFailure(wrongPayload, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
        });
    }

    [Test]
    public void VerifyRequiresExactAllowlistAndUnderstoodCriticalExtensions()
    {
        byte[] keyBytes = Sequence(32, 51);
        string compact = HmacCompact(
            "{\"alg\":\"HS256\",\"crit\":[\"urn:example:flag\"],"
            + "\"urn:example:flag\":true}",
            Array.Empty<byte>(),
            keyBytes);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy defaultPolicy = new TokenPolicy();
        TokenPolicy wrongCase = SigningPolicy("hs256");
        wrongCase.UnderstoodCriticalHeaders = new string[] { "urn:example:flag" };
        TokenPolicy understood = SigningPolicy(JoseAlgorithms.HmacSha256);
        understood.UnderstoodCriticalHeaders = new string[] { "urn:example:flag" };

        JwsCompactResult unknownExtension = JsonWebSignature.Verify(
            compact, key, defaultPolicy);
        JwsCompactResult wrongAlgorithm = JsonWebSignature.Verify(
            compact, key, wrongCase);
        JwsCompactResult valid = JsonWebSignature.Verify(
            compact, key, understood);

        Assert.Multiple(() =>
        {
            AssertFailure(unknownExtension, "unknown_critical_header");
            AssertFailure(wrongAlgorithm, "algorithm_not_allowed");
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Token.PayloadAvailable, Is.True);
            Assert.That(valid.Token.Payload, Is.Empty);
        });
    }

    [TestCase("urn:example:signature", "unknown_algorithm")]
    [TestCase("ML-DSA-65", "unsupported_algorithm")]
    [TestCase("none", "invalid_unsecured_signature")]
    public void VerifyRejectsUnknownUnavailableAndUnsecuredAlgorithms(
        string algorithm,
        string code)
    {
        string compact = JoseCompact.WriteJws(
            Encoding.UTF8.GetBytes("{\"alg\":\"" + algorithm + "\"}"),
            new byte[] { 1 },
            new byte[] { 2 }).Text;
        TokenPolicy policy = SigningPolicy(algorithm);
        policy.AllowUnsecuredTokens = true;

        JwsCompactResult result = JsonWebSignature.Verify(
            compact,
            SymmetricKeyImporterOperations.Import(Sequence(32, 61)).Key,
            policy);

        AssertFailure(result, code);
    }

    [Test]
    public void VerifyPropagatesStructuralPolicyAndKeyFailures()
    {
        byte[] keyBytes = Sequence(32, 71);
        string compact = HmacCompact(
            "{\"alg\":\"HS256\"}",
            new byte[] { 1 },
            keyBytes);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        TokenPolicy invalid = SigningPolicy(JoseAlgorithms.HmacSha256);
        invalid.AllowedAlgorithms = null;
        TokenPolicy small = SigningPolicy(JoseAlgorithms.HmacSha256);
        small.MaximumTokenSizeInBytes = compact.Length - 1;
        TokenKey signOnly = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        signOnly.KeyOperations = new string[] { TokenKeyOperations.Sign };
        string missingSignature = JoseCompact.WriteJws(
            Encoding.UTF8.GetBytes("{\"alg\":\"HS256\"}"),
            new byte[] { 1 },
            Array.Empty<byte>()).Text;

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebSignature.Verify(
                compact, key, invalid), "invalid_policy");
            AssertFailure(JsonWebSignature.Verify(
                compact, key, small), "token_too_large");
            AssertFailure(JsonWebSignature.Verify(
                compact, signOnly,
                SigningPolicy(JoseAlgorithms.HmacSha256)),
                "key_operation_not_permitted");
            AssertFailure(JsonWebSignature.Verify(
                missingSignature, key,
                SigningPolicy(JoseAlgorithms.HmacSha256)),
                "missing_signature");
        });
    }

    [Test]
    public void VerifyRejectsNullArguments()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 81)).Key;
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebSignature.Verify(null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Verify("text", null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Verify("text", key, null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void SignHmacProducesInteroperableCompactSerialization()
    {
        byte[] payload = Encoding.UTF8.GetBytes("signed payload \u2603");
        byte[] originalPayload = payload.ToArray();
        byte[] keyBytes = Sequence(32, 17);
        TokenKey key = SymmetricKeyImporterOperations.Import(keyBytes).Key;
        JoseHeader header = Header("{\"typ\":\"JWT\",\"alg\":\"HS256\"}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult result = JsonWebSignature.Sign(
            payload,
            header,
            key,
            policy);

        string headerText = header.Write().Text;
        string headerSegment = Base64Url.Encode(Encoding.UTF8.GetBytes(headerText)).Text;
        string payloadSegment = Base64Url.Encode(payload).Text;
        string signingText = headerSegment + "." + payloadSegment;
        byte[] expectedMac = HMACSHA256.HashData(
            keyBytes,
            Encoding.ASCII.GetBytes(signingText));
        string expected = signingText + "." + Base64Url.Encode(expectedMac).Text;
        TokenPolicy diagnosticPolicy = DiagnosticPolicy();
        JwsCompactResult diagnostic = JsonWebSignature.ReadDiagnostic(
            result.Text,
            diagnosticPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(diagnostic.Success, Is.True);
            Assert.That(diagnostic.Token.Payload, Is.EqualTo(originalPayload));
            Assert.That(diagnostic.Token.Signature, Is.EqualTo(expectedMac));
            Assert.That(payload, Is.EqualTo(originalPayload));
            Assert.That(header.Write().Text, Is.EqualTo(headerText));
        });
    }

    [Test]
    public void SignRsaPkcsAndPssInteroperateWithBuiltInVerification()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        byte[] payload = Sequence(37, 4);
        string[] algorithms = new string[]
        {
            JoseAlgorithms.RsaSha256,
            JoseAlgorithms.RsaPssSha256
        };
        RSASignaturePadding[] paddings = new RSASignaturePadding[]
        {
            RSASignaturePadding.Pkcs1,
            RSASignaturePadding.Pss
        };
        TokenPolicy policy = SigningPolicy(algorithms);

        int index = 0;
        while (index < algorithms.Length)
        {
            JoseHeader header = Header(
                "{\"alg\":\"" + algorithms[index] + "\"}");
            TokenTextResult signed = JsonWebSignature.Sign(
                payload,
                header,
                key,
                policy);
            JoseCompactParseResult parsed = JoseCompact.Parse(signed.Text);
            byte[] signingInput = SigningInput(parsed.Serialization);

            Assert.Multiple(() =>
            {
                Assert.That(signed.Success, Is.True);
                Assert.That(parsed.Success, Is.True);
                Assert.That(rsa.VerifyData(
                    signingInput,
                    parsed.Serialization.GetDecodedSegmentBytes(2),
                    HashAlgorithmName.SHA256,
                    paddings[index]), Is.True);
            });

            index += 1;
        }
    }

    [Test]
    public void SignEcdsaProducesJoseRawSignatureAcceptedByBuiltInProvider()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, true).Key;
        byte[] payload = Sequence(31, 9);
        TokenTextResult signed = JsonWebSignature.Sign(
            payload.AsSpan(2, 23),
            Header("{\"alg\":\"ES256\",\"kid\":\"ec-1\"}"),
            key,
            SigningPolicy(JoseAlgorithms.EcdsaSha256));
        JoseCompactParseResult parsed = JoseCompact.Parse(signed.Text);
        byte[] signingInput = SigningInput(parsed.Serialization);
        byte[] signature = parsed.Serialization.GetDecodedSegmentBytes(2);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(parsed.Serialization.GetDecodedSegmentBytes(1),
                Is.EqualTo(payload.AsSpan(2, 23).ToArray()));
            Assert.That(signature, Has.Length.EqualTo(64));
            Assert.That(ecdsa.VerifyData(
                signingInput,
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation), Is.True);
        });
    }

    [Test]
    public void SignRequiresAnExactExplicitAlgorithmAllowlist()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 3)).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy defaultPolicy = new TokenPolicy();
        TokenPolicy wrongCase = SigningPolicy("hs256");
        TokenPolicy allowed = SigningPolicy(JoseAlgorithms.HmacSha256);

        TokenTextResult deniedByDefault = JsonWebSignature.Sign(
            Array.Empty<byte>(), header, key, defaultPolicy);
        TokenTextResult deniedByCase = JsonWebSignature.Sign(
            Array.Empty<byte>(), header, key, wrongCase);
        TokenTextResult accepted = JsonWebSignature.Sign(
            Array.Empty<byte>(), header, key, allowed);

        Assert.Multiple(() =>
        {
            AssertTextFailure(deniedByDefault, "algorithm_not_allowed");
            AssertTextFailure(deniedByCase, "algorithm_not_allowed");
            Assert.That(accepted.Success, Is.True);
        });
    }

    [TestCase("{}", "missing_algorithm")]
    [TestCase("{\"alg\":\"\"}", "invalid_algorithm")]
    [TestCase("{\"alg\":\"A128KW\"}", "invalid_jws_algorithm")]
    [TestCase("{\"alg\":\"urn:example:signature\"}", "unknown_algorithm")]
    [TestCase("{\"alg\":\"ML-DSA-65\"}", "unsupported_algorithm")]
    [TestCase("{\"alg\":\"none\"}", "unsecured_jws_not_supported")]
    [TestCase("{\"alg\":\"HS256\",\"enc\":\"A128GCM\"}", "invalid_jws_header_parameter")]
    public void SignRejectsInvalidOrUnsupportedHeaderAlgorithms(
        string headerText,
        string code)
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 5)).Key;
        TokenPolicy policy = SigningPolicy(
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.None,
            "urn:example:signature",
            "ML-DSA-65");
        policy.AllowUnsecuredTokens = true;
        policy.UnderstoodCriticalHeaders = new string[] { "b64" };

        TokenTextResult result = JsonWebSignature.Sign(
            new byte[] { 1 },
            Header(headerText),
            key,
            policy);

        AssertTextFailure(result, code);
    }

    [Test]
    public void SignValidatesCriticalExtensionsBeforeSigning()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 6)).Key;
        JoseHeader header = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"urn:example:flag\"],"
            + "\"urn:example:flag\":true}");
        TokenPolicy deniedPolicy = SigningPolicy(JoseAlgorithms.HmacSha256);
        TokenPolicy understoodPolicy = SigningPolicy(JoseAlgorithms.HmacSha256);
        understoodPolicy.UnderstoodCriticalHeaders = new string[]
        {
            "urn:example:flag"
        };

        TokenTextResult denied = JsonWebSignature.Sign(
            new byte[] { 1 }, header, key, deniedPolicy);
        TokenTextResult understood = JsonWebSignature.Sign(
            new byte[] { 1 }, header, key, understoodPolicy);

        Assert.Multiple(() =>
        {
            AssertTextFailure(denied, "unknown_critical_header");
            Assert.That(understood.Success, Is.True);
        });
    }

    [Test]
    public void SignEnforcesHeaderDepthAndFinalCompactSizeLimits()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 7)).Key;
        TokenPolicy depthPolicy = SigningPolicy(JoseAlgorithms.HmacSha256);
        depthPolicy.MaximumJsonDepth = 1;
        TokenTextResult tooDeep = JsonWebSignature.Sign(
            new byte[] { 1 },
            Header("{\"alg\":\"HS256\",\"custom\":{}}"),
            key,
            depthPolicy);

        TokenPolicy generous = SigningPolicy(JoseAlgorithms.HmacSha256);
        JoseHeader ordinaryHeader = Header("{\"alg\":\"HS256\"}");
        byte[] ordinaryPayload = Sequence(12, 8);
        TokenTextResult ordinary = JsonWebSignature.Sign(
            ordinaryPayload, ordinaryHeader, key, generous);
        TokenPolicy capped = SigningPolicy(JoseAlgorithms.HmacSha256);
        capped.MaximumTokenSizeInBytes = ordinary.Text.Length - 1;
        TokenTextResult tooLarge = JsonWebSignature.Sign(
            ordinaryPayload, ordinaryHeader, key, capped);

        Assert.Multiple(() =>
        {
            AssertTextFailure(tooDeep, "json_depth_exceeded");
            Assert.That(ordinary.Success, Is.True);
            AssertTextFailure(tooLarge, "token_too_large");
        });
    }

    [Test]
    public void SignReturnsPolicyAndKeyValidationFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 10)).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy invalidSize = SigningPolicy(JoseAlgorithms.HmacSha256);
        invalidSize.MaximumTokenSizeInBytes = 0;
        TokenPolicy invalidDepth = SigningPolicy(JoseAlgorithms.HmacSha256);
        invalidDepth.MaximumJsonDepth = 0;
        TokenPolicy invalidAlgorithms = new TokenPolicy();
        invalidAlgorithms.AllowedAlgorithms = null;
        TokenPolicy invalidCritical = SigningPolicy(JoseAlgorithms.HmacSha256);
        invalidCritical.UnderstoodCriticalHeaders = null;
        using RSA wrongRsa = RSA.Create(2048);
        TokenKey wrongKey = RsaKeyImporterOperations.Import(wrongRsa, true).Key;

        Assert.Multiple(() =>
        {
            AssertTextFailure(JsonWebSignature.Sign(
                new byte[] { 1 }, header, key, invalidSize), "invalid_policy");
            AssertTextFailure(JsonWebSignature.Sign(
                new byte[] { 1 }, header, key, invalidDepth), "invalid_policy");
            AssertTextFailure(JsonWebSignature.Sign(
                new byte[] { 1 }, header, key, invalidAlgorithms), "invalid_policy");
            AssertTextFailure(JsonWebSignature.Sign(
                new byte[] { 1 }, header, key, invalidCritical), "invalid_policy");
            AssertTextFailure(JsonWebSignature.Sign(
                new byte[] { 1 }, header, wrongKey,
                SigningPolicy(JoseAlgorithms.HmacSha256)), "invalid_key_type");
        });

    }

    [Test]
    public void SignRejectsNullArguments()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 11)).Key;
        JoseHeader header = Header("{\"alg\":\"HS256\"}");
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebSignature.Sign(
                null, header, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Sign(
                new byte[0], null, key, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Sign(
                new byte[0], header, null, policy), Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Sign(
                new byte[0], header, key, null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ParseReadsProtectedMetadataAndWithholdsUnverifiedPayload()
    {
        string text = Compact(
            "{\"alg\":\"HS256\",\"kid\":\"key-1\",\"typ\":\"JWT\"}",
            Encoding.UTF8.GetBytes("unverified payload"),
            new byte[] { 1, 2, 3, 4 });

        JwsCompactResult result = JsonWebSignature.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Token.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(result.Token.Algorithm, Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Token.KeyId, Is.EqualTo("key-1"));
            Assert.That(result.Token.ProtectedHeader.Get(JoseHeaderNames.Type)
                .StringValue, Is.EqualTo("JWT"));
            Assert.That(result.Token.AlgorithmMetadata.Known, Is.True);
            Assert.That(result.Token.AlgorithmMetadata.Supported, Is.True);
            Assert.That(result.Token.AlgorithmAvailable, Is.True);
            Assert.That(result.Token.PayloadAvailable, Is.False);
            Assert.That(result.Token.Payload, Is.Null);
            Assert.That(result.Token.Signature, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.Token.SignatureVerified, Is.False);
        });
    }

    [Test]
    public void DiagnosticReadRequiresExplicitPolicyAndExposesUnverifiedPayload()
    {
        string text = Compact(
            "{\"alg\":\"ES256\"}",
            new byte[] { 0, 1, 2, 255 },
            new byte[] { 7 });
        TokenPolicy deniedPolicy = new TokenPolicy();
        TokenPolicy allowedPolicy = new TokenPolicy();
        allowedPolicy.AllowUnauthenticatedDecode = true;

        JwsCompactResult denied = JsonWebSignature.ReadDiagnostic(
            text,
            deniedPolicy);
        JwsCompactResult allowed = JsonWebSignature.ReadDiagnostic(
            text,
            allowedPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(denied, "unauthenticated_decode_not_allowed");
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Token.PayloadAvailable, Is.True);
            Assert.That(allowed.Token.Payload, Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
            Assert.That(allowed.Token.SignatureVerified, Is.False);
        });
    }

    [Test]
    public void DiagnosticReadDistinguishesAnEmptyPayloadFromWithheldPayload()
    {
        string text = Compact(
            "{\"alg\":\"RS256\"}",
            Array.Empty<byte>(),
            new byte[] { 1 });
        TokenPolicy policy = DiagnosticPolicy();

        JwsCompactResult parsed = JsonWebSignature.Parse(text);
        JwsCompactResult diagnostic = JsonWebSignature.ReadDiagnostic(text, policy);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Token.PayloadAvailable, Is.False);
            Assert.That(parsed.Token.Payload, Is.Null);
            Assert.That(diagnostic.Token.PayloadAvailable, Is.True);
            Assert.That(diagnostic.Token.Payload, Is.Empty);
        });
    }

    [Test]
    public void ParseKeepsKnownUnavailableAlgorithmsAsMetadata()
    {
        string text = Compact(
            "{\"alg\":\"ML-DSA-65\"}",
            new byte[] { 1 },
            new byte[] { 2 });

        JwsCompactResult result = JsonWebSignature.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.AlgorithmMetadata.Known, Is.True);
            Assert.That(result.Token.AlgorithmMetadata.Supported, Is.False);
            Assert.That(result.Token.AlgorithmAvailable, Is.False);
        });
    }

    [Test]
    public void ParseKeepsUnknownAlgorithmsForDiagnosticMetadata()
    {
        string text = Compact(
            "{\"alg\":\"urn:example:signature\"}",
            new byte[] { 1 },
            new byte[] { 2 });

        JwsCompactResult result = JsonWebSignature.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Algorithm, Is.EqualTo("urn:example:signature"));
            Assert.That(result.Token.AlgorithmMetadata.Known, Is.False);
            Assert.That(result.Token.AlgorithmAvailable, Is.False);
        });
    }

    [Test]
    public void ParseAcceptsCallerUnderstoodCriticalExtensions()
    {
        string text = Compact(
            "{\"alg\":\"HS256\",\"crit\":[\"urn:example:flag\"],"
            + "\"urn:example:flag\":true}",
            new byte[] { 1 },
            new byte[] { 2 });
        TokenPolicy defaultPolicy = new TokenPolicy();
        TokenPolicy extensionPolicy = new TokenPolicy();
        extensionPolicy.UnderstoodCriticalHeaders = new string[]
        {
            "urn:example:flag"
        };

        JwsCompactResult unknown = JsonWebSignature.Parse(text, defaultPolicy);
        JwsCompactResult understood = JsonWebSignature.Parse(text, extensionPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(unknown, "unknown_critical_header");
            Assert.That(understood.Success, Is.True);
        });
    }

    [Test]
    public void ParseTreatsCompactPayloadAsRawWhenB64IsFalse()
    {
        string text = Compact(
            "{\"alg\":\"HS256\",\"crit\":[\"b64\"],\"b64\":false}",
            Encoding.ASCII.GetBytes("NDA1"),
            new byte[] { 1 });
        TokenPolicy policy = new TokenPolicy();

        JwsCompactResult result = JsonWebSignature.Parse(text, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.PayloadBase64UrlEncoded, Is.False);
        });
    }

    [TestCase("{}", "missing_algorithm")]
    [TestCase("{\"alg\":\"\"}", "invalid_algorithm")]
    [TestCase("{\"alg\":\"A128KW\"}", "invalid_jws_algorithm")]
    [TestCase("{\"alg\":\"HS256\",\"enc\":\"A128GCM\"}", "invalid_jws_header_parameter")]
    public void ParseRejectsInvalidJwsHeaderSemantics(
        string header,
        string code)
    {
        string text = Compact(header, new byte[] { 1 }, new byte[] { 2 });

        JwsCompactResult result = JsonWebSignature.Parse(text);

        AssertFailure(result, code);
    }

    [Test]
    public void ParseRecognizesUnsecuredJwsWithoutExposingItsPayload()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}");
        string text = Compact(
            "{\"alg\":\"none\"}",
            payload,
            Array.Empty<byte>());

        JwsCompactResult result = JsonWebSignature.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Token.Algorithm, Is.EqualTo(JoseAlgorithms.None));
            Assert.That(result.Token.Unsecured, Is.True);
            Assert.That(result.Token.AlgorithmAvailable, Is.False);
            Assert.That(result.Token.Signature, Is.Empty);
            Assert.That(result.Token.PayloadAvailable, Is.False);
            Assert.That(result.Token.Payload, Is.Null);
            Assert.That(result.Token.SignatureVerified, Is.False);
        });
    }

    [Test]
    public void CreateAndValidateUnsecuredJwtRequiresExplicitPolicy()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"sub\":\"alice\"}");
        JoseHeader header = Header("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        TokenPolicy allowed = SigningPolicy(JoseAlgorithms.None);
        allowed.AllowUnsecuredTokens = true;
        TokenPolicy disabled = SigningPolicy(JoseAlgorithms.None);
        TokenPolicy missingAllowlist = new TokenPolicy();
        missingAllowlist.AllowUnsecuredTokens = true;
        TokenPolicy wrongCase = SigningPolicy("NONE");
        wrongCase.AllowUnsecuredTokens = true;

        TokenTextResult created = JsonWebSignature.CreateUnsecured(
            payload, header, allowed);
        TokenTextResult deniedCreation = JsonWebSignature.CreateUnsecured(
            payload, header, disabled);
        TokenTextResult deniedAlgorithm = JsonWebSignature.CreateUnsecured(
            payload, header, missingAllowlist);
        TokenTextResult deniedCase = JsonWebSignature.CreateUnsecured(
            payload, header, wrongCase);
        JwsCompactResult validated = JsonWebSignature.ValidateUnsecured(
            created.Text, allowed);
        JwsCompactResult deniedValidation = JsonWebSignature.ValidateUnsecured(
            created.Text, disabled);
        TokenKey key = SymmetricKeyImporterOperations.Import(Sequence(32, 92)).Key;
        JwsCompactResult ordinaryVerification = JsonWebSignature.Verify(
            created.Text, key, allowed);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Text.EndsWith('.'), Is.True);
            AssertTextFailure(deniedCreation, "unsecured_tokens_not_allowed");
            AssertTextFailure(deniedAlgorithm, "algorithm_not_allowed");
            AssertTextFailure(deniedCase, "algorithm_not_allowed");
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Token.Unsecured, Is.True);
            Assert.That(validated.Token.Payload, Is.EqualTo(payload));
            Assert.That(validated.Token.PayloadAvailable, Is.True);
            Assert.That(validated.Token.SignatureVerified, Is.False);
            AssertFailure(deniedValidation, "unsecured_tokens_not_allowed");
            AssertFailure(ordinaryVerification, "unsecured_jws_not_supported");
        });
    }

    [Test]
    public void UnsecuredOperationsEnforceAlgorithmSignatureAndB64Rules()
    {
        TokenPolicy policy = SigningPolicy(JoseAlgorithms.None);
        policy.AllowUnsecuredTokens = true;
        JoseHeader unencoded = Header(
            "{\"alg\":\"none\",\"crit\":[\"b64\"],\"b64\":false}");
        byte[] payload = Encoding.UTF8.GetBytes("raw payload \u2603");
        TokenTextResult created = JsonWebSignature.CreateUnsecured(
            payload, unencoded, policy);
        JwsCompactResult validated = JsonWebSignature.ValidateUnsecured(
            created.Text, policy);
        TokenTextResult wrongAlgorithm = JsonWebSignature.CreateUnsecured(
            payload, Header("{\"alg\":\"HS256\"}"), policy);
        JwsCompactResult signedAlgorithm = JsonWebSignature.ValidateUnsecured(
            Compact("{\"alg\":\"HS256\"}", payload, new byte[] { 1 }),
            policy);
        JwsCompactResult nonemptySignature = JsonWebSignature.Parse(
            Compact("{\"alg\":\"none\"}", payload, new byte[] { 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Text.Contains(".raw payload \u2603."), Is.True);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Token.PayloadBase64UrlEncoded, Is.False);
            Assert.That(validated.Token.Payload, Is.EqualTo(payload));
            AssertTextFailure(wrongAlgorithm, "invalid_unsecured_algorithm");
            AssertFailure(signedAlgorithm, "invalid_unsecured_algorithm");
            AssertFailure(nonemptySignature, "invalid_unsecured_signature");
        });
    }

    [Test]
    public void ParseRejectsMissingSignatureForSignedAlgorithms()
    {
        string text = Compact(
            "{\"alg\":\"HS256\"}",
            new byte[] { 1 },
            Array.Empty<byte>());

        JwsCompactResult result = JsonWebSignature.Parse(text);

        AssertFailure(result, "missing_signature");
    }

    [Test]
    public void ParseRejectsFiveSegmentJweAndMissingProtectedHeader()
    {
        string jwe = JoseCompact.WriteJwe(
            Encoding.UTF8.GetBytes("{\"alg\":\"dir\",\"enc\":\"A128GCM\"}"),
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 }).Text;
        string missingHeader = JoseCompact.WriteJws(
            Array.Empty<byte>(),
            new byte[] { 1 },
            new byte[] { 2 }).Text;

        JwsCompactResult wrongKind = JsonWebSignature.Parse(jwe);
        JwsCompactResult missing = JsonWebSignature.Parse(missingHeader);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongKind, "invalid_jws_compact");
            AssertFailure(missing, "missing_protected_header");
        });
    }

    [Test]
    public void ParsePropagatesCompactAndStrictHeaderFailures()
    {
        JwsCompactResult compact = JsonWebSignature.Parse("AA.A+.AA");
        JwsCompactResult json = JsonWebSignature.Parse(
            CompactBytes(Encoding.UTF8.GetBytes("{\"alg\":}")));
        JwsCompactResult duplicate = JsonWebSignature.Parse(
            CompactBytes(Encoding.UTF8.GetBytes(
                "{\"alg\":\"HS256\",\"alg\":\"RS256\"}")));
        JwsCompactResult root = JsonWebSignature.Parse(
            CompactBytes(Encoding.UTF8.GetBytes("[]")));

        Assert.Multiple(() =>
        {
            AssertFailure(compact, "invalid_compact_token");
            AssertFailure(json, "invalid_json");
            AssertFailure(duplicate, "duplicate_json_member");
            AssertFailure(root, "json_root_not_object");
        });
    }

    [Test]
    public void ParseEnforcesTokenSizeAndHeaderDepthLimits()
    {
        string ordinary = Compact(
            "{\"alg\":\"HS256\"}",
            new byte[] { 1 },
            new byte[] { 2 });
        TokenPolicy sizePolicy = new TokenPolicy();
        sizePolicy.MaximumTokenSizeInBytes = ordinary.Length - 1;
        TokenPolicy depthPolicy = new TokenPolicy();
        depthPolicy.MaximumJsonDepth = 1;
        string nested = Compact(
            "{\"alg\":\"HS256\",\"custom\":{}}",
            new byte[] { 1 },
            new byte[] { 2 });

        JwsCompactResult tooLarge = JsonWebSignature.Parse(ordinary, sizePolicy);
        JwsCompactResult tooDeep = JsonWebSignature.Parse(nested, depthPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(tooLarge, "token_too_large");
            AssertFailure(tooDeep, "json_depth_exceeded");
        });
    }

    [Test]
    public void InvalidPoliciesReturnStableFailures()
    {
        string text = Compact(
            "{\"alg\":\"HS256\"}",
            new byte[] { 1 },
            new byte[] { 2 });
        TokenPolicy size = new TokenPolicy();
        size.MaximumTokenSizeInBytes = 0;
        TokenPolicy depth = new TokenPolicy();
        depth.MaximumJsonDepth = 0;
        TokenPolicy critical = new TokenPolicy();
        critical.UnderstoodCriticalHeaders = null;

        Assert.Multiple(() =>
        {
            AssertFailure(JsonWebSignature.Parse(text, size), "invalid_policy");
            AssertFailure(JsonWebSignature.Parse(text, depth), "invalid_policy");
            AssertFailure(JsonWebSignature.Parse(text, critical), "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JsonWebSignature.Parse(null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.Parse("text", null),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.ReadDiagnostic(null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => JsonWebSignature.ReadDiagnostic("text", null),
                Throws.ArgumentNullException);
        });
    }

    static TokenPolicy DiagnosticPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowUnauthenticatedDecode = true;
        return policy;
    }

    static TokenPolicy SigningPolicy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static string HmacCompact(
        string headerText,
        byte[] payload,
        byte[] key)
    {
        string headerSegment = Base64Url.Encode(
            Encoding.UTF8.GetBytes(headerText)).Text;
        string payloadSegment = Base64Url.Encode(payload).Text;
        string signingText = headerSegment + "." + payloadSegment;
        byte[] signature = HMACSHA256.HashData(
            key,
            Encoding.ASCII.GetBytes(signingText));
        return signingText + "." + Base64Url.Encode(signature).Text;
    }

    static JoseHeader Header(string text)
    {
        JoseHeaderResult result = JoseHeader.Read(text);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static byte[] SigningInput(JoseCompactSerialization serialization)
    {
        string text = serialization.GetEncodedSegment(0).ToString()
            + "." + serialization.GetEncodedSegment(1).ToString();
        return Encoding.ASCII.GetBytes(text);
    }

    static byte[] Sequence(int length, int seed)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(seed + index);
            index += 1;
        }

        return result;
    }

    static string Compact(string header, byte[] payload, byte[] signature)
    {
        return JoseCompact.WriteJws(
            Encoding.UTF8.GetBytes(header),
            payload,
            signature).Text;
    }

    static string CompactBytes(byte[] header)
    {
        return JoseCompact.WriteJws(
            header,
            new byte[] { 1 },
            new byte[] { 2 }).Text;
    }

    static void AssertFailure(JwsCompactResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertTextFailure(TokenTextResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
