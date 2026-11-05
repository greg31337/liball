using System.Text;
using token;

namespace token.Tests;

public class CoseSignTests
{
    const string WorkingGroupVector =
        "D8628441A0A054546869732069732074686520636F6E74656E742E818343A10126A1044231315840E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A";

    [Test]
    public void ReadMatchesCoseWorkingGroupSignVector()
    {
        CoseSignResult result = CoseSign.Read(
            Convert.FromHexString(WorkingGroupVector));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message.Tagged, Is.True);
            Assert.That(result.Message.DetachedPayload, Is.False);
            Assert.That(Encoding.UTF8.GetString(result.Message.Payload),
                Is.EqualTo("This is the content."));
            Assert.That(result.Message.Header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA0 }));
            Assert.That(result.Message.Header.ProtectedParameters, Is.Empty);
            Assert.That(result.Message.Signatures, Has.Length.EqualTo(1));
            Assert.That(result.Message.Signatures[0].Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha256));
            Assert.That(result.Message.Signatures[0].Header.HasAlgorithm,
                Is.True);
            Assert.That(result.Message.Signatures[0].Header.KeyId,
                Is.EqualTo(Encoding.ASCII.GetBytes("11")));
            Assert.That(result.Message.Signatures[0].Signature,
                Has.Length.EqualTo(64));
            Assert.That(result.Message.Signatures[0].Signature,
                Is.EqualTo(Convert.FromHexString(
                    "E2AEAFD40D69D19DFE6E52077C5D7FF4E408282CBEFB5D06CBF414AF2E19D982AC45AC98B8544C908B4507DE1E90B717C3D34816FE926A2B98F53AFD2FA0F30A")));
        });
    }

    [Test]
    public void WriteUsesDeterministicRfcFieldOrder()
    {
        CoseSign message = new CoseSign();
        message.Payload = new byte[] { 1 };
        CoseSignature signature = Signature(
            CoseAlgorithms.EcdsaSha256,
            new byte[] { 9 },
            new byte[] { 2, 3 });
        message.Signatures = new CoseSignature[] { signature };

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(
                "D8628440A04101818343A10126A1044109420203")));
            Assert.That(message.Header.ProtectedBytes, Is.Empty);
            Assert.That(signature.Header.ProtectedBytes,
                Is.EqualTo(Convert.FromHexString("A10126")));
            Assert.That(message.Tagged, Is.True);
        });
    }

    [Test]
    public void MultipleSignaturesRoundTripWithIndependentHeaders()
    {
        CoseSign message = new CoseSign();
        message.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cbor"));
        message.Header.SetUnprotectedByTextLabel("private", Boolean(true));
        message.Payload = Sequence(20, 40);
        message.Signatures = new CoseSignature[]
        {
            Signature(
                CoseAlgorithms.EcdsaSha256,
                new byte[] { 1 },
                Sequence(64, 1)),
            Signature(
                CoseAlgorithms.EcdsaSha384,
                new byte[] { 2 },
                Sequence(96, 2)),
            Signature(
                CoseAlgorithms.EcdsaSha512,
                new byte[] { 3 },
                Sequence(132, 3))
        };

        TokenBytesResult written = CoseSign.Write(message, true);
        CoseSignResult parsed = CoseSign.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Payload, Is.EqualTo(message.Payload));
            Assert.That(parsed.Message.Header.ContentTypeText,
                Is.EqualTo("application/cbor"));
            Assert.That(parsed.Message.Header.GetUnprotectedByTextLabel("private")
                .BooleanValue, Is.True);
            Assert.That(parsed.Message.Signatures, Has.Length.EqualTo(3));
            Assert.That(parsed.Message.Signatures[0].Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha256));
            Assert.That(parsed.Message.Signatures[1].Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha384));
            Assert.That(parsed.Message.Signatures[2].Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaSha512));
            Assert.That(parsed.Message.Signatures[0].Header.KeyId,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(parsed.Message.Signatures[1].Signature,
                Is.EqualTo(message.Signatures[1].Signature));
            Assert.That(parsed.Message.Signatures[2].Signature,
                Is.EqualTo(message.Signatures[2].Signature));
        });
    }

    [Test]
    public void DetachedAndEmptyAttachedPayloadsRemainDistinct()
    {
        CoseSign detached = Message();
        detached.DetachedPayload = true;
        detached.Payload = null;
        CoseSign attached = Message();
        attached.Payload = System.Array.Empty<byte>();

        TokenBytesResult detachedBytes = detached.WriteWithTag(false);
        TokenBytesResult attachedBytes = attached.WriteWithTag(false);
        CoseSignResult detachedResult = CoseSign.Read(detachedBytes.Bytes);
        CoseSignResult attachedResult = CoseSign.Read(attachedBytes.Bytes);
        CborItem detachedItem = Decode(detachedBytes.Bytes);
        CborItem attachedItem = Decode(attachedBytes.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(detachedBytes.Bytes[3], Is.EqualTo(0xF6));
            Assert.That(attachedBytes.Bytes[3], Is.EqualTo(0x40));
            Assert.That(detachedItem.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(attachedItem.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(detachedResult.Success, Is.True);
            Assert.That(detachedResult.Message.DetachedPayload, Is.True);
            Assert.That(detachedResult.Message.Payload, Is.Null);
            Assert.That(detachedResult.Message.Tagged, Is.False);
            Assert.That(attachedResult.Message.DetachedPayload, Is.False);
            Assert.That(attachedResult.Message.Payload, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTagControlOverridesAndUpdatesTheModel()
    {
        CoseSign message = Message();

        TokenBytesResult untagged = message.WriteWithTag(false);
        bool afterUntagged = message.Tagged;
        TokenBytesResult tagged = message.WriteWithTag(true);

        Assert.Multiple(() =>
        {
            Assert.That(untagged.Bytes[0], Is.EqualTo(0x84));
            Assert.That(afterUntagged, Is.False);
            Assert.That(tagged.Bytes[0], Is.EqualTo(0xD8));
            Assert.That(tagged.Bytes[1], Is.EqualTo(0x62));
            Assert.That(message.Tagged, Is.True);
            Assert.That(CoseSign.Read(untagged.Bytes).Message.Tagged, Is.False);
            Assert.That(CoseSign.Read(tagged.Bytes).Message.Tagged, Is.True);
        });
    }

    [Test]
    public void ParsedProtectedBytesRemainExactUntilWriting()
    {
        byte[] encoded = Convert.FromHexString(WorkingGroupVector);

        CoseSignResult parsed = CoseSign.Read(encoded);
        byte[] bodyProtected = parsed.Message.Header.ProtectedBytes.ToArray();
        byte[] signerProtected =
            parsed.Message.Signatures[0].Header.ProtectedBytes.ToArray();
        encoded[3] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(bodyProtected, Is.EqualTo(new byte[] { 0xA0 }));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(bodyProtected));
            Assert.That(signerProtected,
                Is.EqualTo(Convert.FromHexString("A10126")));
            Assert.That(parsed.Message.Signatures[0].Header.ProtectedBytes,
                Is.EqualTo(signerProtected));
        });
    }

    [Test]
    public void ParsedMessageWritesWithoutInvalidatingProtectedBytes()
    {
        byte[] vector = Convert.FromHexString(WorkingGroupVector);
        CoseSignResult parsed = CoseSign.Read(vector);

        TokenBytesResult written = parsed.Message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(written);
            Assert.That(written.Bytes, Is.EqualTo(vector));
            Assert.That(parsed.Message.Header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void WritingDoesNotMutatePayloadOrSignatureInputs()
    {
        byte[] payload = Sequence(32, 10);
        byte[] signatureBytes = Sequence(64, 70);
        CoseSign message = Message();
        message.Payload = payload;
        message.Signatures[0].Signature = signatureBytes;
        byte[] payloadCopy = payload.ToArray();
        byte[] signatureCopy = signatureBytes.ToArray();

        TokenBytesResult result = message.Write();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(payload, Is.EqualTo(payloadCopy));
            Assert.That(signatureBytes, Is.EqualTo(signatureCopy));
        });
    }

    [Test]
    public void MalformedSignatureEntriesAreRejected()
    {
        CborItem validHeader = Bytes(System.Array.Empty<byte>());
        CborItem validMap = Map();
        CoseSignResult notArray = ReadMessage(Boolean(true));
        CoseSignResult wrongLength = ReadMessage(Array(
            validHeader,
            validMap));
        CoseSignResult wrongProtected = ReadMessage(Array(
            Map(),
            validMap,
            Bytes(new byte[] { 1 })));
        CoseSignResult wrongUnprotected = ReadMessage(Array(
            validHeader,
            Array(),
            Bytes(new byte[] { 1 })));
        CoseSignResult wrongSignature = ReadMessage(Array(
            validHeader,
            validMap,
            Boolean(false)));

        Assert.Multiple(() =>
        {
            AssertFailure(notArray, "invalid_cose_signature_structure", 0);
            AssertFailure(wrongLength, "invalid_cose_signature_structure", 0);
            AssertFailure(wrongProtected, "invalid_cose_protected_header", -1);
            AssertFailure(wrongUnprotected, "invalid_cose_unprotected_header", -1);
            AssertFailure(wrongSignature, "invalid_cose_signature_structure", 0);
        });
    }

    [Test]
    public void InvalidMessageAndHeaderStructuresAreRejected()
    {
        CoseSignResult emptySignatures = CoseSign.Read(Encode(Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Array())));
        CoseSignResult badBodyProtected = CoseSign.Read(Encode(Array(
            Bytes(new byte[] { 1 }),
            Map(),
            Bytes(new byte[] { 1 }),
            Array(ValidSignature()))));
        byte[] valid = Message().Write().Bytes;
        CoseSignResult trailing = CoseSign.Read(
            valid.Concat(new byte[] { 0 }).ToArray());
        CoseSignResult wrongTag = CoseSign.Read(Encode(
            Tag(18, SignStructure(ValidSignature()))));

        Assert.Multiple(() =>
        {
            AssertFailure(
                emptySignatures,
                "invalid_cose_message_structure",
                3);
            AssertFailure(
                badBodyProtected,
                "invalid_cose_protected_header",
                -1);
            AssertFailure(
                trailing,
                "trailing_cose_message_data",
                valid.Length);
            AssertFailure(
                wrongTag,
                "unexpected_cose_message_type",
                0);
        });
    }

    [Test]
    public void DuplicateHeaderLabelsAreRejectedWhileWriting()
    {
        CoseSign message = Message();
        message.Header.ProtectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(-7))
        };
        message.Header.UnprotectedParameters = new CborMapEntry[]
        {
            Entry(CoseHeaderLabels.Algorithm, Int(-7))
        };

        TokenBytesResult result = message.Write();

        AssertBytesFailure(result, "duplicate_cose_header_label");
    }

    [Test]
    public void InvalidModelsReturnStableFailures()
    {
        CoseSign missingHeader = Message();
        missingHeader.Header = null;
        CoseSign missingPayload = Message();
        missingPayload.Payload = null;
        CoseSign conflictingDetachedPayload = Message();
        conflictingDetachedPayload.DetachedPayload = true;
        CoseSign missingSignatures = Message();
        missingSignatures.Signatures = null;
        CoseSign emptySignatures = Message();
        emptySignatures.Signatures = System.Array.Empty<CoseSignature>();
        CoseSign nullSignature = Message();
        nullSignature.Signatures = new CoseSignature[] { null };
        CoseSign missingSignerHeader = Message();
        missingSignerHeader.Signatures[0].Header = null;
        CoseSign missingSignatureBytes = Message();
        missingSignatureBytes.Signatures[0].Signature = null;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                missingHeader.Write(),
                "invalid_cose_sign_header");
            AssertBytesFailure(
                missingPayload.Write(),
                "invalid_cose_sign_payload");
            AssertBytesFailure(
                conflictingDetachedPayload.Write(),
                "invalid_cose_sign_payload");
            AssertBytesFailure(
                missingSignatures.Write(),
                "invalid_cose_sign_signatures");
            AssertBytesFailure(
                emptySignatures.Write(),
                "invalid_cose_sign_signatures");
            AssertBytesFailure(
                nullSignature.Write(),
                "invalid_cose_sign_signature");
            AssertBytesFailure(
                missingSignerHeader.Write(),
                "invalid_cose_sign_signature");
            AssertBytesFailure(
                missingSignatureBytes.Write(),
                "invalid_cose_sign_signature");
        });
    }

    [Test]
    public void ReadAndWriteEnforceSizeDepthAndPolicyLimits()
    {
        CoseSign message = Message();
        TokenBytesResult unrestricted = message.Write();
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 2;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactWrite = message.WriteWithPolicy(exact);
        TokenBytesResult smallWrite = message.WriteWithPolicy(small);
        CoseSignResult exactRead = CoseSign.Read(unrestricted.Bytes, exact);
        CoseSignResult smallRead = CoseSign.Read(unrestricted.Bytes, small);
        CoseSignResult shallowRead = CoseSign.Read(
            unrestricted.Bytes,
            shallow);
        TokenBytesResult invalidWrite = message.WriteWithPolicy(invalid);
        CoseSignResult invalidRead = CoseSign.Read(
            unrestricted.Bytes,
            invalid);

        Assert.Multiple(() =>
        {
            AssertSuccess(exactWrite);
            Assert.That(exactRead.Success, Is.True);
            AssertBytesFailure(smallWrite, "buffer_limit_exceeded");
            AssertFailure(smallRead, "cbor_size_exceeded", 21);
            AssertFailure(shallowRead, "cbor_depth_exceeded", 4);
            AssertBytesFailure(invalidWrite, "invalid_policy");
            AssertFailure(invalidRead, "invalid_policy", -1);
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseSign message = Message();
        byte[] encoded = message.Write().Bytes;

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseSign.Read(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign.Read(encoded, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign.Write(null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign.Write(message, (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseSign.Write(message, true, null),
                Throws.ArgumentNullException);
            Assert.That(() => message.WriteWithPolicy((TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => message.WriteWithTagAndPolicy(true, null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ModelsHaveSafeDefaults()
    {
        CoseSignature signature = new CoseSignature();
        CoseSign message = new CoseSign();
        CoseSignResult result = new CoseSignResult();

        Assert.Multiple(() =>
        {
            Assert.That(signature.Header, Is.Not.Null);
            Assert.That(signature.Signature, Is.Empty);
            Assert.That(message.Header, Is.Not.Null);
            Assert.That(message.Payload, Is.Empty);
            Assert.That(message.DetachedPayload, Is.False);
            Assert.That(message.Signatures, Is.Empty);
            Assert.That(message.Tagged, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }

    static CoseSign Message()
    {
        CoseSign message = new CoseSign();
        message.Payload = new byte[] { 1, 2, 3 };
        message.Signatures = new CoseSignature[]
        {
            Signature(
                CoseAlgorithms.EcdsaSha256,
                new byte[] { 7 },
                new byte[] { 8, 9 })
        };
        return message;
    }

    static CoseSignature Signature(
        int algorithm,
        byte[] keyId,
        byte[] signatureBytes)
    {
        CoseSignature signature = new CoseSignature();
        signature.Header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        signature.Header.SetUnprotected(
            CoseHeaderLabels.KeyId,
            Bytes(keyId));
        signature.Signature = signatureBytes;
        return signature;
    }

    static CoseSignResult ReadMessage(CborItem signature)
    {
        return CoseSign.Read(Encode(SignStructure(signature)));
    }

    static CborItem SignStructure(CborItem signature)
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }),
            Array(signature));
    }

    static CborItem ValidSignature()
    {
        return Array(
            Bytes(System.Array.Empty<byte>()),
            Map(),
            Bytes(new byte[] { 1 }));
    }

    static CborItem Decode(byte[] encoded)
    {
        CborReader reader = CborReader.Create(encoded);
        CborReadResult result = reader.Read();
        Assert.That(result.Success, Is.True);
        Assert.That(reader.GetRemainingLength(), Is.Zero);
        return result.Item;
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult result = writer.WriteItem(item);
        Assert.That(result.Success, Is.True);
        return writer.ToArray();
    }

    static CborMapEntry Entry(int key, CborItem value)
    {
        return new CborMapEntry(Int(key), value);
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

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static CborItem Array(params CborItem[] items)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = items;
        return item;
    }

    static CborItem Tag(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
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

    static void AssertFailure(
        CoseSignResult result,
        string code,
        int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
        Assert.That(result.Message, Is.Null);
    }
}
