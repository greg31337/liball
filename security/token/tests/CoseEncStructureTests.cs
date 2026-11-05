using token;

namespace token.Tests;

public class CoseEncStructureTests
{
    [TestCase(CoseEncStructureContext.Encrypt, "Encrypt")]
    [TestCase(CoseEncStructureContext.Encrypt0, "Encrypt0")]
    [TestCase(CoseEncStructureContext.EncryptRecipient, "Enc_Recipient")]
    [TestCase(CoseEncStructureContext.MacRecipient, "Mac_Recipient")]
    [TestCase(CoseEncStructureContext.Recipient, "Rec_Recipient")]
    public void EveryRfcContextUsesTheRequiredFieldOrder(
        CoseEncStructureContext context,
        string expectedContext)
    {
        byte[] protectedParameters = Convert.FromHexString("A10101");
        byte[] externalData = new byte[] { 0, 128, 255 };

        TokenBytesResult result = CoseEncStructure.Create(
            context,
            protectedParameters,
            externalData);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.Kind, Is.EqualTo(TokenCborValueKind.Array));
            Assert.That(decoded.ArrayValue, Has.Length.EqualTo(3));
            Assert.That(decoded.ArrayValue[0].Kind,
                Is.EqualTo(TokenCborValueKind.TextString));
            Assert.That(decoded.ArrayValue[0].Text, Is.EqualTo(expectedContext));
            Assert.That(decoded.ArrayValue[1].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(decoded.ArrayValue[1].Bytes,
                Is.EqualTo(protectedParameters));
            Assert.That(decoded.ArrayValue[2].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(decoded.ArrayValue[2].Bytes, Is.EqualTo(externalData));
        });
    }

    [Test]
    public void Encrypt0UsesPreferredDeterministicEncoding()
    {
        TokenBytesResult result = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            Convert.FromHexString("A10101"),
            Convert.FromHexString("AABB"));

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "8368456E63727970743043A1010142AABB")));
        });
    }

    [Test]
    public void ConvenienceOverloadDefaultsExternalDataToEmptyBytes()
    {
        TokenBytesResult result = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt,
            Array.Empty<byte>());
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue[0].Text, Is.EqualTo("Encrypt"));
            Assert.That(decoded.ArrayValue[1].Bytes, Is.Empty);
            Assert.That(decoded.ArrayValue[2].Bytes, Is.Empty);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString("8367456E63727970744040")));
        });
    }

    [Test]
    public void ExactProtectedAndExternalBytesArePreserved()
    {
        byte[] protectedParameters = new byte[]
        {
            0xBF, 0x01, 0x18, 0x01, 0xFF
        };
        byte[] externalData = Enumerable.Range(0, 300)
            .Select(value => (byte)value)
            .ToArray();
        byte[] protectedCopy = protectedParameters.ToArray();
        byte[] externalCopy = externalData.ToArray();

        TokenBytesResult result = CoseEncStructure.Create(
            CoseEncStructureContext.EncryptRecipient,
            protectedParameters,
            externalData);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue[1].Bytes,
                Is.EqualTo(protectedParameters));
            Assert.That(decoded.ArrayValue[2].Bytes, Is.EqualTo(externalData));
            Assert.That(protectedParameters, Is.EqualTo(protectedCopy));
            Assert.That(externalData, Is.EqualTo(externalCopy));
        });
    }

    [Test]
    public void OutputSizeAndPolicyLimitsAreEnforced()
    {
        TokenBytesResult unrestricted = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 });
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactResult = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            exact);
        TokenBytesResult smallResult = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            small);
        TokenBytesResult invalidResult = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            invalid);

        Assert.Multiple(() =>
        {
            AssertSuccess(exactResult);
            AssertFailure(smallResult, "buffer_limit_exceeded");
            AssertFailure(invalidResult, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectInvalidArguments()
    {
        byte[] empty = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseEncStructure.Create(
                    CoseEncStructureContext.Encrypt,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEncStructure.Create(
                    CoseEncStructureContext.Encrypt,
                    empty,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEncStructure.Create(
                    CoseEncStructureContext.Encrypt,
                    empty,
                    empty,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEncStructure.Create(
                    (CoseEncStructureContext)999,
                    empty),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static CborItem Decode(byte[] encoded)
    {
        CborReader reader = CborReader.Create(encoded);
        CborReadResult result = reader.Read();
        Assert.That(result.Success, Is.True);
        Assert.That(reader.GetRemainingLength(), Is.Zero);
        return result.Item;
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.Not.Null);
    }

    static void AssertFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }
}
