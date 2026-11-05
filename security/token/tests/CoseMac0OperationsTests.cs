using System.Text;
using token;

namespace token.Tests;

public class CoseMac0OperationsTests
{
    [TestCase(CoseAlgorithms.HmacSha256Truncated64, 32, 8)]
    [TestCase(CoseAlgorithms.HmacSha256, 32, 32)]
    [TestCase(CoseAlgorithms.HmacSha384, 48, 48)]
    [TestCase(CoseAlgorithms.HmacSha512, 64, 64)]
    public void EveryHmacAlgorithmRoundTrips(
        int algorithm,
        int keySize,
        int tagSize)
    {
        byte[] payload = Sequence(257, 9);
        TokenKey key = Symmetric(Sequence(keySize, 1), algorithm);
        TokenPolicy policy = Policy(CoseAlgorithmsOperations.GetName(algorithm));

        TokenBytesResult created = CoseMac0Operations.Create(
            payload,
            Header(algorithm),
            key,
            policy);
        CoseMac0Result parsed = CoseMac0.Read(created.Bytes);
        CoseMac0Result verified = CoseMac0Operations.Verify(
            created.Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Tagged, Is.True);
            Assert.That(parsed.Message.Tag, Has.Length.EqualTo(tagSize));
            Assert.That(parsed.Message.TagVerified, Is.False);
            AssertVerified(verified, payload);
        });
    }

    [Test]
    public void CreationAuthenticatesTheExactMac0Structure()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        byte[] payload = Encoding.UTF8.GetBytes(
            "the MAC context string is significant");
        TokenKey key = Symmetric(Sequence(32, 5), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        CoseHeader header = Header(algorithm);
        header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("text/plain"));

        TokenBytesResult created = CoseMac0Operations.Create(
            payload,
            header,
            key,
            policy);
        CoseMac0Result parsed = CoseMac0.Read(created.Bytes);
        TokenBytesResult structure = CoseMacStructure.CreateMac0(
            parsed.Message.Header.ProtectedBytes,
            payload);
        TokenBytesResult wrongStructure = CoseMacStructure.CreateMac(
            parsed.Message.Header.ProtectedBytes,
            payload);
        TokenResult independent = CoseHmacAlgorithms.Verify(
            structure.Bytes,
            parsed.Message.Tag,
            key,
            algorithm);
        TokenResult wrongContext = CoseHmacAlgorithms.Verify(
            wrongStructure.Bytes,
            parsed.Message.Tag,
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(independent.Success, Is.True);
            Assert.That(wrongContext.Success, Is.False);
            Assert.That(wrongContext.Error.Code, Is.EqualTo("mac_mismatch"));
            Assert.That(parsed.Message.Header.ContentTypeText,
                Is.EqualTo("text/plain"));
        });
    }

    [Test]
    public void IndependentlyCreatedMac0MessageVerifies()
    {
        int algorithm = CoseAlgorithms.HmacSha384;
        byte[] payload = Sequence(70, 11);
        TokenKey key = Symmetric(Sequence(48, 20), algorithm);
        TokenPolicy policy = Policy("HMAC 384/384");
        CoseHeader header = Header(algorithm);
        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            header,
            policy);
        TokenBytesResult structure = CoseMacStructure.CreateMac0(
            protectedBytes.Bytes,
            payload);
        TokenBytesResult tag = CoseHmacAlgorithms.Create(
            structure.Bytes,
            key,
            algorithm);
        CoseMac0 message = new CoseMac0();
        message.Header = header;
        message.Payload = payload;
        message.Tag = tag.Bytes;
        byte[] encoded = message.WriteWithTag(false).Bytes;

        CoseMac0Result verified = CoseMac0Operations.Verify(
            encoded,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertVerified(verified, payload);
            Assert.That(verified.Message.Tagged, Is.False);
        });
    }

    [Test]
    public void PayloadTagAndProtectedHeaderTamperingFailClosed()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 30), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult created = CoseMac0Operations.Create(
            Sequence(32, 1),
            Header(algorithm),
            key,
            policy);

        CoseMac0 payload = CoseMac0.Read(created.Bytes).Message;
        payload.Payload[0] ^= 1;
        CoseMac0 tag = CoseMac0.Read(created.Bytes).Message;
        tag.Tag[^1] ^= 1;
        CoseMac0 header = CoseMac0.Read(created.Bytes).Message;
        header.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cwt"));

        CoseMac0Result payloadResult = CoseMac0Operations.Verify(
            payload.Write().Bytes, key, policy);
        CoseMac0Result tagResult = CoseMac0Operations.Verify(
            tag.Write().Bytes, key, policy);
        CoseMac0Result headerResult = CoseMac0Operations.Verify(
            header.Write().Bytes, key, policy);

        Assert.Multiple(() =>
        {
            AssertFailure(payloadResult, "mac_mismatch");
            AssertFailure(tagResult, "mac_mismatch");
            AssertFailure(headerResult, "mac_mismatch");
        });
    }

    [Test]
    public void WrongKeyAndIncorrectTagLengthFailClosed()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 40), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult created = CoseMac0Operations.Create(
            new byte[] { 1, 2, 3 },
            Header(algorithm),
            key,
            policy);
        TokenKey wrong = Symmetric(Sequence(32, 90), algorithm);
        CoseMac0 shortTag = CoseMac0.Read(created.Bytes).Message;
        shortTag.Tag = shortTag.Tag[..^1];

        CoseMac0Result wrongResult = CoseMac0Operations.Verify(
            created.Bytes,
            wrong,
            policy);
        CoseMac0Result shortResult = CoseMac0Operations.Verify(
            shortTag.Write().Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongResult, "mac_mismatch");
            AssertFailure(shortResult, "mac_mismatch");
        });
    }

    [Test]
    public void AlgorithmMustBeIntegerProtectedImplementedAndAllowed()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 50), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        CoseHeader missing = new CoseHeader();
        CoseHeader unprotected = new CoseHeader();
        unprotected.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        CoseHeader text = new CoseHeader();
        text.SetProtected(
            CoseHeaderLabels.Algorithm,
            Text("HMAC 256/256"));
        CoseHeader invalid = Header(CoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertCreateFailure(missing, key, policy,
                "missing_cose_algorithm");
            AssertCreateFailure(unprotected, key, policy,
                "cose_algorithm_not_protected");
            AssertCreateFailure(text, key, policy,
                "invalid_cose_algorithm");
            AssertCreateFailure(invalid, key, Policy("A128GCM"),
                "invalid_algorithm");
            AssertCreateFailure(Header(algorithm), key,
                Policy("HMAC 384/384"),
                "algorithm_not_allowed");
        });
    }

    [Test]
    public void CriticalHeadersAreValidatedForCreateAndVerify()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 60), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        CoseHeader invalid = Header(algorithm);
        SetUnknownCritical(invalid);
        TokenBytesResult createResult = CoseMac0Operations.Create(
            new byte[] { 1 }, invalid, key, policy);
        TokenBytesResult valid = CoseMac0Operations.Create(
            new byte[] { 1 }, Header(algorithm), key, policy);
        CoseMac0 model = CoseMac0.Read(valid.Bytes).Message;
        SetUnknownCritical(model.Header);
        CoseMac0Result verifyResult = CoseMac0Operations.Verify(
            model.Write().Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(createResult,
                "unknown_cose_critical_header");
            AssertFailure(verifyResult,
                "unknown_cose_critical_header");
        });
    }

    [Test]
    public void KeyMetadataRestrictionsAreEnforcedForBothOperations()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenKey verifyOnly = Symmetric(Sequence(32, 70), algorithm);
        verifyOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationMacVerify };
        TokenBytesResult deniedCreate = CoseMac0Operations.Create(
            new byte[] { 1 }, Header(algorithm), verifyOnly, policy);
        TokenKey unrestricted = Symmetric(Sequence(32, 70), algorithm);
        TokenBytesResult created = CoseMac0Operations.Create(
            new byte[] { 1 }, Header(algorithm), unrestricted, policy);
        TokenKey createOnly = Symmetric(Sequence(32, 70), algorithm);
        createOnly.KeyOperations =
            new string[] { TokenKeyOperations.MacCreate };
        CoseMac0Result deniedVerify = CoseMac0Operations.Verify(
            created.Bytes,
            createOnly,
            policy);
        TokenKey encryptionUse = Symmetric(Sequence(32, 70), algorithm);
        encryptionUse.Use = TokenKeyUses.Encryption;
        TokenBytesResult deniedUse = CoseMac0Operations.Create(
            new byte[] { 1 }, Header(algorithm), encryptionUse, policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(deniedCreate,
                "key_operation_not_permitted");
            AssertFailure(deniedVerify,
                "key_operation_not_permitted");
            AssertBytesFailure(deniedUse,
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void ExplicitTagEmptyPayloadAndDetachedRequirementAreHandled()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 80), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult untagged = CoseMac0Operations.Create(
            System.Array.Empty<byte>(),
            Header(algorithm),
            key,
            false,
            policy);
        CoseMac0Result verified = CoseMac0Operations.Verify(
            untagged.Bytes,
            key,
            policy);
        CoseMac0 detached = CoseMac0.Read(untagged.Bytes).Message;
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseMac0Result detachedResult = CoseMac0Operations.Verify(
            detached.Write().Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            AssertVerified(verified, System.Array.Empty<byte>());
            Assert.That(verified.Message.Tagged, Is.False);
            AssertFailure(detachedResult, "missing_payload");
        });
    }

    [Test]
    public void StructuralSizeAndPolicyFailuresAreReturned()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        TokenKey key = Symmetric(Sequence(32, 90), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");
        TokenBytesResult created = CoseMac0Operations.Create(
            Sequence(20, 1), Header(algorithm), key, policy);
        TokenPolicy small = Policy("HMAC 256/256");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;
        TokenPolicy invalid = Policy("HMAC 256/256");
        invalid.AllowedAlgorithms = null;

        CoseMac0Result malformed = CoseMac0Operations.Verify(
            new byte[] { 0x9F }, key, policy);
        CoseMac0Result trailing = CoseMac0Operations.Verify(
            created.Bytes.Concat(new byte[] { 0 }).ToArray(),
            key,
            policy);
        TokenBytesResult smallCreate = CoseMac0Operations.Create(
            Sequence(20, 1), Header(algorithm), key, small);
        CoseMac0Result smallVerify = CoseMac0Operations.Verify(
            created.Bytes, key, small);
        TokenBytesResult invalidCreate = CoseMac0Operations.Create(
            Sequence(20, 1), Header(algorithm), key, invalid);
        CoseMac0Result invalidVerify = CoseMac0Operations.Verify(
            created.Bytes, key, invalid);

        Assert.Multiple(() =>
        {
            AssertFailure(malformed, "unexpected_end_of_cbor");
            AssertFailure(trailing, "trailing_cose_message_data");
            AssertBytesFailure(smallCreate, "buffer_limit_exceeded");
            AssertFailure(smallVerify, "cbor_size_exceeded");
            AssertBytesFailure(invalidCreate, "invalid_policy");
            AssertFailure(invalidVerify, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        CoseHeader header = Header(algorithm);
        TokenKey key = Symmetric(Sequence(32, 100), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseMac0Operations.Create(
                null, header, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Create(
                System.Array.Empty<byte>(), null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Create(
                System.Array.Empty<byte>(), header, null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Create(
                System.Array.Empty<byte>(), header, key, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Verify(
                null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Verify(
                System.Array.Empty<byte>(), null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseMac0Operations.Verify(
                System.Array.Empty<byte>(), key, null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void DetachedPayloadAndExternalAuthenticatedDataAreBound()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        byte[] payload = Encoding.ASCII.GetBytes("detached payload");
        byte[] external = Encoding.ASCII.GetBytes("channel binding");
        TokenKey key = Symmetric(Sequence(32, 110), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");

        TokenBytesResult created = CoseMac0Operations.CreateDetached(
            payload,
            external,
            Header(algorithm),
            key,
            false,
            policy);
        CoseMac0Result parsed = CoseMac0.Read(created.Bytes);
        CoseMac0Result verified = CoseMac0Operations.VerifyDetached(
            created.Bytes,
            payload,
            external,
            key,
            policy);
        CoseMac0Result wrongPayload = CoseMac0Operations.VerifyDetached(
            created.Bytes,
            Encoding.ASCII.GetBytes("wrong payload"),
            external,
            key,
            policy);
        CoseMac0Result wrongExternal = CoseMac0Operations.VerifyDetached(
            created.Bytes,
            payload,
            Encoding.ASCII.GetBytes("wrong binding"),
            key,
            policy);
        CoseMac0Result attachedOperation = CoseMac0Operations.Verify(
            created.Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.DetachedPayload, Is.True);
            Assert.That(parsed.Message.Payload, Is.Null);
            AssertVerified(verified, payload);
            Assert.That(verified.Message.DetachedPayload, Is.True);
            AssertFailure(wrongPayload, "mac_mismatch");
            AssertFailure(wrongExternal, "mac_mismatch");
            AssertFailure(attachedOperation, "missing_payload");
        });
    }

    [Test]
    public void AttachedExternalAuthenticatedDataRequiresExactMatch()
    {
        int algorithm = CoseAlgorithms.HmacSha256;
        byte[] payload = new byte[] { 1, 2, 3 };
        byte[] external = new byte[] { 4, 5, 6 };
        TokenKey key = Symmetric(Sequence(32, 120), algorithm);
        TokenPolicy policy = Policy("HMAC 256/256");

        TokenBytesResult created =
            CoseMac0Operations.CreateWithExternalAuthenticatedData(
                payload,
                external,
                Header(algorithm),
                key,
                true,
                policy);
        CoseMac0Result verified =
            CoseMac0Operations.VerifyWithExternalAuthenticatedData(
                created.Bytes,
                external,
                key,
                policy);
        CoseMac0Result omitted = CoseMac0Operations.Verify(
            created.Bytes,
            key,
            policy);
        CoseMac0Result detachedOperation =
            CoseMac0Operations.VerifyDetached(
                created.Bytes,
                payload,
                external,
                key,
                policy);

        Assert.Multiple(() =>
        {
            AssertVerified(verified, payload);
            AssertFailure(omitted, "mac_mismatch");
            AssertFailure(detachedOperation,
                "unexpected_attached_payload");
        });
    }

    static CoseHeader Header(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        return header;
    }

    static TokenKey Symmetric(byte[] material, int algorithm)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            material,
            algorithm).Key;
        key.Use = TokenKeyUses.Signature;
        return key;
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

    static byte[] Sequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }

    static void AssertCreateFailure(
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy,
        string code)
    {
        AssertBytesFailure(CoseMac0Operations.Create(
            new byte[] { 1 },
            header,
            key,
            policy), code);
    }

    static void AssertVerified(CoseMac0Result result, byte[] payload)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Message.TagVerified, Is.True);
        Assert.That(result.Message.Payload, Is.EqualTo(payload));
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
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

    static void AssertFailure(CoseMac0Result result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
