using token;

namespace token.Tests;

public class CoseMacStructureTests
{
    [Test]
    public void MacUsesTheRfcFieldOrderAndPreferredEncoding()
    {
        byte[] protectedParameters = Convert.FromHexString("A10105");
        byte[] externalData = Convert.FromHexString("AABB");
        byte[] payload = new byte[] { 1, 2, 3 };

        TokenBytesResult result = CoseMacStructure.CreateMac(
            protectedParameters,
            externalData,
            payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "84634D414343A1010542AABB43010203")));
        });
    }

    [Test]
    public void Mac0UsesTheRfcFieldOrderAndPreferredEncoding()
    {
        byte[] protectedParameters = Convert.FromHexString("A10105");
        byte[] externalData = Convert.FromHexString("AABB");
        byte[] payload = new byte[] { 1, 2, 3 };

        TokenBytesResult result = CoseMacStructure.CreateMac0(
            protectedParameters,
            externalData,
            payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "84644D41433043A1010542AABB43010203")));
        });
    }

    [Test]
    public void ConvenienceOverloadsDefaultExternalDataToEmptyBytes()
    {
        TokenBytesResult mac = CoseMacStructure.CreateMac(
            Array.Empty<byte>(),
            new byte[] { 9 });
        TokenBytesResult mac0 = CoseMacStructure.CreateMac0(
            Array.Empty<byte>(),
            new byte[] { 9 });

        CborItem first = Decode(mac.Bytes);
        CborItem second = Decode(mac0.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(mac);
            Assert.That(first.ArrayValue, Has.Length.EqualTo(4));
            Assert.That(first.ArrayValue[0].Text, Is.EqualTo("MAC"));
            Assert.That(first.ArrayValue[1].Bytes, Is.Empty);
            Assert.That(first.ArrayValue[2].Bytes, Is.Empty);
            Assert.That(first.ArrayValue[3].Bytes, Is.EqualTo(new byte[] { 9 }));
            AssertSuccess(mac0);
            Assert.That(second.ArrayValue, Has.Length.EqualTo(4));
            Assert.That(second.ArrayValue[0].Text, Is.EqualTo("MAC0"));
            Assert.That(second.ArrayValue[1].Bytes, Is.Empty);
            Assert.That(second.ArrayValue[2].Bytes, Is.Empty);
            Assert.That(second.ArrayValue[3].Bytes, Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void ExactInputsAndFullPayloadArePreserved()
    {
        byte[] protectedParameters = new byte[]
        {
            0xBF, 0x01, 0x18, 0x01, 0xFF
        };
        byte[] externalData = new byte[] { 0, 128, 255 };
        byte[] payload = Enumerable.Range(0, 300)
            .Select(value => (byte)value)
            .ToArray();
        byte[] protectedCopy = protectedParameters.ToArray();
        byte[] externalCopy = externalData.ToArray();
        byte[] payloadCopy = payload.ToArray();

        TokenBytesResult result = CoseMacStructure.CreateMac(
            protectedParameters,
            externalData,
            payload);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue[1].Bytes,
                Is.EqualTo(protectedParameters));
            Assert.That(decoded.ArrayValue[2].Bytes, Is.EqualTo(externalData));
            Assert.That(decoded.ArrayValue[3].Bytes, Is.EqualTo(payload));
            Assert.That(protectedParameters, Is.EqualTo(protectedCopy));
            Assert.That(externalData, Is.EqualTo(externalCopy));
            Assert.That(payload, Is.EqualTo(payloadCopy));
        });
    }

    [Test]
    public void OutputSizeAndPolicyLimitsAreEnforced()
    {
        TokenBytesResult unrestricted = CoseMacStructure.CreateMac0(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 });
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactResult = CoseMacStructure.CreateMac0(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            exact);
        TokenBytesResult smallResult = CoseMacStructure.CreateMac0(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            small);
        TokenBytesResult invalidResult = CoseMacStructure.CreateMac(
            Array.Empty<byte>(),
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
    public void PublicOperationsRejectNullArguments()
    {
        byte[] empty = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseMacStructure.CreateMac(null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac(empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac(empty, null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac(empty, empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac(
                    empty,
                    empty,
                    empty,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac0(null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac0(empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac0(empty, null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac0(empty, empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseMacStructure.CreateMac0(
                    empty,
                    empty,
                    empty,
                    null),
                Throws.ArgumentNullException);
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
