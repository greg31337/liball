using token;

namespace token.Tests;

public class CoseSigStructureTests
{
    [Test]
    public void Signature1UsesTheRfcFieldOrderAndPreferredEncoding()
    {
        byte[] bodyProtected = Convert.FromHexString("A10126");
        byte[] externalData = Convert.FromHexString("AABB");
        byte[] payload = new byte[] { 1, 2, 3 };

        TokenBytesResult result = CoseSigStructure.CreateSignature1(
            bodyProtected,
            externalData,
            payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "846A5369676E61747572653143A1012642AABB43010203")));
        });
    }

    [Test]
    public void SignatureUsesBodyAndSignerProtectedBuckets()
    {
        byte[] bodyProtected = Convert.FromHexString("A10443010203");
        byte[] signerProtected = Convert.FromHexString("A10127");
        byte[] externalData = new byte[] { 128, 255 };
        byte[] payload = Array.Empty<byte>();

        TokenBytesResult result = CoseSigStructure.CreateSignature(
            bodyProtected,
            signerProtected,
            externalData,
            payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "85695369676E617475726546A1044301020343A101274280FF40")));
        });
    }

    [Test]
    public void ConvenienceOverloadsDefaultExternalDataToEmptyBytes()
    {
        TokenBytesResult signature1 = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            new byte[] { 9 });
        TokenBytesResult signature = CoseSigStructure.CreateSignature(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 9 });

        CborItem first = Decode(signature1.Bytes);
        CborItem second = Decode(signature.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(signature1);
            Assert.That(first.ArrayValue, Has.Length.EqualTo(4));
            Assert.That(first.ArrayValue[0].Text, Is.EqualTo("Signature1"));
            Assert.That(first.ArrayValue[1].Bytes, Is.Empty);
            Assert.That(first.ArrayValue[2].Bytes, Is.Empty);
            Assert.That(first.ArrayValue[3].Bytes, Is.EqualTo(new byte[] { 9 }));
            AssertSuccess(signature);
            Assert.That(second.ArrayValue, Has.Length.EqualTo(5));
            Assert.That(second.ArrayValue[0].Text, Is.EqualTo("Signature"));
            Assert.That(second.ArrayValue[1].Bytes, Is.Empty);
            Assert.That(second.ArrayValue[2].Bytes, Is.Empty);
            Assert.That(second.ArrayValue[3].Bytes, Is.Empty);
            Assert.That(second.ArrayValue[4].Bytes, Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void PayloadAlwaysUsesTheSuppliedFullBytes()
    {
        byte[] payload = Enumerable.Range(0, 300)
            .Select(value => (byte)value)
            .ToArray();

        TokenBytesResult result = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            payload);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue[3].Bytes, Is.EqualTo(payload));
            Assert.That(result.Bytes[14], Is.EqualTo(0x59));
            Assert.That(result.Bytes[15], Is.EqualTo(0x01));
            Assert.That(result.Bytes[16], Is.EqualTo(0x2C));
        });
    }

    [Test]
    public void ConstructionDoesNotMutateCallerInputs()
    {
        byte[] bodyProtected = new byte[] { 1, 2 };
        byte[] signerProtected = new byte[] { 3, 4 };
        byte[] externalData = new byte[] { 5, 6 };
        byte[] payload = new byte[] { 7, 8 };
        byte[] bodyCopy = bodyProtected.ToArray();
        byte[] signerCopy = signerProtected.ToArray();
        byte[] externalCopy = externalData.ToArray();
        byte[] payloadCopy = payload.ToArray();

        TokenBytesResult result = CoseSigStructure.CreateSignature(
            bodyProtected,
            signerProtected,
            externalData,
            payload);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(bodyProtected, Is.EqualTo(bodyCopy));
            Assert.That(signerProtected, Is.EqualTo(signerCopy));
            Assert.That(externalData, Is.EqualTo(externalCopy));
            Assert.That(payload, Is.EqualTo(payloadCopy));
        });
    }

    [Test]
    public void OutputSizeAndPolicyLimitsAreEnforced()
    {
        TokenBytesResult unrestricted = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 });
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactResult = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            exact);
        TokenBytesResult smallResult = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
            small);
        TokenBytesResult invalidResult = CoseSigStructure.CreateSignature1(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3 },
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
        TokenPolicy policy = new TokenPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseSigStructure.CreateSignature1(null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature1(empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature1(empty, null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature1(empty, empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature1(
                    empty,
                    empty,
                    empty,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(null, empty, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(empty, null, empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(empty, empty, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(
                    empty,
                    empty,
                    null,
                    empty),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(
                    empty,
                    empty,
                    empty,
                    null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseSigStructure.CreateSignature(
                    empty,
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
