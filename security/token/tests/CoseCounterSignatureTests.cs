using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseCounterSignatureTests
{
    [Test]
    public void FullCounterSignatureRoundTripsThroughSign1HeaderAndBindsTarget()
    {
        using ECDsa primaryNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa counterNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa counterPublic = PublicPart(counterNative);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        byte[] payload = Encoding.UTF8.GetBytes("countersigned content");
        byte[] externalData = Encoding.UTF8.GetBytes("notary context");
        TokenBytesResult primary = CoseSign1Operations.Create(
            payload,
            Header(algorithm, 1),
            EcKey(primaryNative, algorithm, true),
            policy);
        CoseSign1 message = CoseSign1.Read(primary.Bytes, policy).Message;
        CoseCounterSignatureTarget target = Target(message);

        CoseCounterSignatureResult created =
            CoseCounterSignatureOperations.Create(
                target,
                externalData,
                Header(algorithm, 2),
                EcKey(counterNative, algorithm, true),
                true,
                false,
                policy);
        TokenBytesResult standalone = created.CounterSignature.WriteWithPolicy(policy);
        CoseCounterSignatureResult standaloneParsed =
            CoseCounterSignature.Read(standalone.Bytes, policy);
        TokenResult attached = CoseCounterSignature.SetHeader(
            message.Header,
            new CoseCounterSignature[] { created.CounterSignature },
            policy);
        TokenBytesResult encodedMessage = message.WriteWithPolicy(policy);
        CoseSign1 parsedMessage = CoseSign1.Read(
            encodedMessage.Bytes,
            policy).Message;
        CoseCounterSignatureCollectionResult parsed =
            CoseCounterSignature.ReadHeader(parsedMessage.Header, policy);
        TokenResult verified = CoseCounterSignatureOperations.Verify(
            Target(parsedMessage),
            externalData,
            parsed.CounterSignatures[0],
            EcKey(counterPublic, algorithm, false),
            false,
            policy);
        CoseCounterSignatureTarget changed = Target(parsedMessage);
        changed.OtherFields[0] = changed.OtherFields[0].ToArray();
        changed.OtherFields[0][0] ^= 1;
        TokenResult changedSignature = CoseCounterSignatureOperations.Verify(
            changed,
            externalData,
            parsed.CounterSignatures[0],
            EcKey(counterPublic, algorithm, false),
            false,
            policy);
        TokenResult changedData = CoseCounterSignatureOperations.Verify(
            Target(parsedMessage),
            new byte[] { 8 },
            parsed.CounterSignatures[0],
            EcKey(counterPublic, algorithm, false),
            false,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(primary.Success, Is.True);
            Assert.That(created.Success, Is.True);
            Assert.That(created.CounterSignature.Signature,
                Has.Length.EqualTo(64));
            Assert.That(standalone.Success, Is.True);
            Assert.That(standalone.Bytes[0], Is.EqualTo(0xD3));
            Assert.That(standaloneParsed.Success, Is.True);
            Assert.That(standaloneParsed.CounterSignature.Tagged, Is.True);
            Assert.That(attached.Success, Is.True);
            Assert.That(encodedMessage.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.CounterSignatures, Has.Length.EqualTo(1));
            Assert.That(parsed.CounterSignatures[0].Header.KeyId,
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(verified.Success, Is.True);
            Assert.That(parsed.CounterSignatures[0].SignatureVerified, Is.True);
            AssertFailure(changedSignature, "signature_mismatch");
            AssertFailure(changedData, "signature_mismatch");
        });
    }

    [Test]
    public void MultipleFullCounterSignaturesUseHeaderArrayForm()
    {
        TokenPolicy policy = Policy("ES256");
        CoseHeader header = new CoseHeader();
        CoseCounterSignature first = Model(1);
        CoseCounterSignature second = Model(2);

        TokenResult stored = CoseCounterSignature.SetHeader(
            header,
            new CoseCounterSignature[] { first, second },
            policy);
        CoseCounterSignatureCollectionResult parsed =
            CoseCounterSignature.ReadHeader(header, policy);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Success, Is.True);
            Assert.That(header.GetUnprotected(
                CoseHeaderLabels.CounterSignatureV2).ArrayValue,
                Has.Length.EqualTo(2));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.CounterSignatures, Has.Length.EqualTo(2));
            Assert.That(parsed.CounterSignatures[0].Signature,
                Is.EqualTo(first.Signature));
            Assert.That(parsed.CounterSignatures[1].Signature,
                Is.EqualTo(second.Signature));
        });
    }

    [Test]
    public void AbbreviatedCounterSignatureRoundTripsThroughHeader()
    {
        using ECDsa counterNative =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa counterPublic = PublicPart(counterNative);
        int algorithm = CoseAlgorithms.EcdsaP256Sha256;
        TokenPolicy policy = Policy("ES256");
        byte[] externalData = Encoding.UTF8.GetBytes("group context");
        CoseCounterSignatureTarget target = new CoseCounterSignatureTarget();
        target.BodyProtected = new byte[] { 0xA1, 0x01, 0x26 };
        target.Payload = Encoding.UTF8.GetBytes("ciphertext");
        target.OtherFields = new byte[][] { new byte[] { 1, 2, 3 } };

        TokenBytesResult created =
            CoseCounterSignatureOperations.CreateAbbreviated(
                target,
                externalData,
                algorithm,
                EcKey(counterNative, algorithm, true),
                false,
                policy);
        CoseHeader header = new CoseHeader();
        CoseCounterSignatureOperations.SetAbbreviatedHeader(
            header,
            created.Bytes);
        TokenBytesResult parsed =
            CoseCounterSignatureOperations.ReadAbbreviatedHeader(header);
        TokenResult verified =
            CoseCounterSignatureOperations.VerifyAbbreviated(
                target,
                externalData,
                parsed.Bytes,
                algorithm,
                EcKey(counterPublic, algorithm, false),
                false,
                policy);
        CoseCounterSignatureTarget changed = new CoseCounterSignatureTarget();
        changed.BodyProtected = target.BodyProtected;
        changed.Payload = new byte[] { 9 };
        changed.OtherFields = target.OtherFields;
        TokenResult changedResult =
            CoseCounterSignatureOperations.VerifyAbbreviated(
                changed,
                externalData,
                parsed.Bytes,
                algorithm,
                EcKey(counterPublic, algorithm, false),
                false,
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Bytes, Has.Length.EqualTo(64));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Bytes, Is.EqualTo(created.Bytes));
            Assert.That(verified.Success, Is.True);
            AssertFailure(changedResult, "signature_mismatch");
        });
    }

    [Test]
    public void CounterSignatureStructureUsesVersionTwoContextWhenNeeded()
    {
        TokenPolicy policy = new TokenPolicy();
        TokenBytesResult withoutOther = CoseCounterSignatureStructure.Create(
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 },
            Array.Empty<byte[]>(),
            policy);
        TokenBytesResult withOther = CoseCounterSignatureStructure.Create(
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 },
            new byte[][] { new byte[] { 5 } },
            policy);
        CborItem legacy = CborReader.Create(withoutOther.Bytes).Read().Item;
        CborItem versionTwo = CborReader.Create(withOther.Bytes).Read().Item;

        Assert.Multiple(() =>
        {
            Assert.That(withoutOther.Success, Is.True);
            Assert.That(withOther.Success, Is.True);
            Assert.That(legacy.ArrayValue[0].Text,
                Is.EqualTo("CounterSignature"));
            Assert.That(legacy.ArrayValue, Has.Length.EqualTo(5));
            Assert.That(versionTwo.ArrayValue[0].Text,
                Is.EqualTo("CounterSignatureV2"));
            Assert.That(versionTwo.ArrayValue, Has.Length.EqualTo(6));
            Assert.That(versionTwo.ArrayValue[5].ArrayValue[0].Bytes,
                Is.EqualTo(new byte[] { 5 }));
        });
    }

    [Test]
    public void CounterSignatureHeadersMustBeUnprotectedAndWellTyped()
    {
        TokenPolicy policy = Policy("ES256");
        CoseHeader protectedHeader = new CoseHeader();
        protectedHeader.SetProtected(
            CoseHeaderLabels.CounterSignatureV2,
            Bytes(new byte[] { 1 }));
        CoseHeader malformed = new CoseHeader();
        malformed.SetUnprotected(
            CoseHeaderLabels.CounterSignatureV2,
            Bytes(new byte[] { 1 }));
        CoseHeader abbreviatedProtected = new CoseHeader();
        abbreviatedProtected.SetProtected(
            CoseHeaderLabels.CounterSignature0V2,
            Bytes(new byte[] { 1 }));

        CoseCounterSignatureCollectionResult protectedResult =
            CoseCounterSignature.ReadHeader(protectedHeader, policy);
        CoseCounterSignatureCollectionResult malformedResult =
            CoseCounterSignature.ReadHeader(malformed, policy);
        TokenBytesResult abbreviatedResult =
            CoseCounterSignatureOperations.ReadAbbreviatedHeader(
                abbreviatedProtected);

        Assert.Multiple(() =>
        {
            AssertFailure(protectedResult,
                "countersignature_must_be_unprotected");
            AssertFailure(malformedResult,
                "invalid_countersignature_header");
            AssertBytesFailure(abbreviatedResult,
                "countersignature_must_be_unprotected");
        });
    }

    static CoseCounterSignatureTarget Target(CoseSign1 message)
    {
        return CoseCounterSignatureTarget.FromSign1(
            message,
            message.Payload);
    }

    static CoseCounterSignature Model(byte value)
    {
        CoseCounterSignature model = new CoseCounterSignature();
        model.Header = Header(CoseAlgorithms.EcdsaP256Sha256, value);
        model.Signature = new byte[] { value, (byte)(value + 1) };
        return model;
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
            false).Key;
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

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertFailure(
        CoseCounterSignatureCollectionResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }
}
