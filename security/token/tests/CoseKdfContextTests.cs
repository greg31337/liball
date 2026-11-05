using token;

namespace token.Tests;

public class CoseKdfContextTests
{
    [Test]
    public void IntegerAlgorithmEncodesEveryRfcFieldInOrder()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(-25, 256);
        parameters.PartyUInfo.Identity = new byte[] { 1 };
        parameters.PartyUInfo.Nonce = Unsigned(5);
        parameters.PartyVInfo.Nonce = Bytes(new byte[] { 0xAA, 0xBB });
        parameters.PartyVInfo.Other = new byte[] { 3 };
        parameters.ProtectedParameters = Convert.FromHexString("A10101");
        parameters.SupplementalPublicOther = new byte[] { 0x99 };
        parameters.SupplementalPrivateInfo = new byte[] { 4, 5 };

        TokenBytesResult result = CoseKdfContext.Create(parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "85381883410105F683F642AABB41038319010043A101014199420405")));
        });
    }

    [Test]
    public void TextAlgorithmOmitsOptionalSupplementalFields()
    {
        CoseKdfContextParameters parameters =
            CoseKdfContextParameters.Create("private", 128);

        TokenBytesResult result = CoseKdfContext.Create(parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(
                result.Bytes,
                Is.EqualTo(Convert.FromHexString(
                    "84677072697661746583F6F6F683F6F6F682188040")));
        });
    }

    [Test]
    public void EmptyOptionalValuesArePresentRatherThanOmitted()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(1, 0);
        parameters.PartyUInfo.Identity = Array.Empty<byte>();
        parameters.PartyUInfo.Nonce = Null();
        parameters.PartyUInfo.Other = Array.Empty<byte>();
        parameters.SupplementalPublicOther = Array.Empty<byte>();
        parameters.SupplementalPrivateInfo = Array.Empty<byte>();

        TokenBytesResult result = CoseKdfContext.Create(parameters);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue, Has.Length.EqualTo(5));
            Assert.That(decoded.ArrayValue[1].ArrayValue[0].Kind,
                Is.EqualTo(TokenCborValueKind.ByteString));
            Assert.That(decoded.ArrayValue[1].ArrayValue[0].Bytes, Is.Empty);
            Assert.That(decoded.ArrayValue[1].ArrayValue[1].Kind,
                Is.EqualTo(TokenCborValueKind.Null));
            Assert.That(decoded.ArrayValue[1].ArrayValue[2].Bytes, Is.Empty);
            Assert.That(decoded.ArrayValue[3].ArrayValue,
                Has.Length.EqualTo(3));
            Assert.That(decoded.ArrayValue[3].ArrayValue[2].Bytes, Is.Empty);
            Assert.That(decoded.ArrayValue[4].Bytes, Is.Empty);
        });
    }

    [Test]
    public void ByteAndSignedIntegerNonceFormsAreAccepted()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(1, 128);
        parameters.PartyUInfo.Nonce = Bytes(new byte[] { 7, 8 });
        parameters.PartyVInfo.Nonce = Negative(-10);

        TokenBytesResult result = CoseKdfContext.Create(parameters);
        CborItem decoded = Decode(result.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(decoded.ArrayValue[1].ArrayValue[1].Bytes,
                Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(decoded.ArrayValue[2].ArrayValue[1].Kind,
                Is.EqualTo(TokenCborValueKind.NegativeInteger));
            Assert.That(decoded.ArrayValue[2].ArrayValue[1].IntegerArgument,
                Is.EqualTo(9));
        });
    }

    [Test]
    public void InvalidMutableInputsReturnStableFailures()
    {
        CoseKdfContextParameters algorithm =
            new CoseKdfContextParameters(1, 128);
        algorithm.AlgorithmIdentifier = Boolean(true);
        CoseKdfContextParameters party =
            new CoseKdfContextParameters(1, 128);
        party.PartyUInfo = null;
        CoseKdfContextParameters protectedParameters =
            new CoseKdfContextParameters(1, 128);
        protectedParameters.ProtectedParameters = null;
        CoseKdfContextParameters nonce =
            new CoseKdfContextParameters(1, 128);
        nonce.PartyVInfo.Nonce = Text("wrong");
        CoseKdfContextParameters incompleteNonce =
            new CoseKdfContextParameters(1, 128);
        incompleteNonce.PartyUInfo.Nonce = new CborItem();
        incompleteNonce.PartyUInfo.Nonce.Kind =
            TokenCborValueKind.ByteString;

        TokenBytesResult algorithmResult = CoseKdfContext.Create(algorithm);
        TokenBytesResult partyResult = CoseKdfContext.Create(party);
        TokenBytesResult protectedResult =
            CoseKdfContext.Create(protectedParameters);
        TokenBytesResult nonceResult = CoseKdfContext.Create(nonce);
        TokenBytesResult incompleteResult =
            CoseKdfContext.Create(incompleteNonce);

        Assert.Multiple(() =>
        {
            AssertFailure(algorithmResult, "invalid_cose_kdf_algorithm");
            AssertFailure(partyResult, "invalid_cose_kdf_context");
            AssertFailure(protectedResult, "invalid_cose_kdf_context");
            AssertFailure(nonceResult, "invalid_cose_kdf_nonce");
            AssertFailure(incompleteResult, "invalid_cose_kdf_nonce");
        });
    }

    [Test]
    public void ConstructionPreservesCallerOwnedBytes()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(1, 256);
        parameters.PartyUInfo.Identity = new byte[] { 1 };
        parameters.PartyUInfo.Nonce = Bytes(new byte[] { 2 });
        parameters.PartyUInfo.Other = new byte[] { 3 };
        parameters.PartyVInfo.Identity = new byte[] { 4 };
        parameters.ProtectedParameters = new byte[] { 5 };
        parameters.SupplementalPublicOther = new byte[] { 6 };
        parameters.SupplementalPrivateInfo = new byte[] { 7 };
        byte[][] copies = new byte[][]
        {
            parameters.PartyUInfo.Identity.ToArray(),
            parameters.PartyUInfo.Nonce.Bytes.ToArray(),
            parameters.PartyUInfo.Other.ToArray(),
            parameters.PartyVInfo.Identity.ToArray(),
            parameters.ProtectedParameters.ToArray(),
            parameters.SupplementalPublicOther.ToArray(),
            parameters.SupplementalPrivateInfo.ToArray()
        };

        TokenBytesResult result = CoseKdfContext.Create(parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(parameters.PartyUInfo.Identity, Is.EqualTo(copies[0]));
            Assert.That(parameters.PartyUInfo.Nonce.Bytes, Is.EqualTo(copies[1]));
            Assert.That(parameters.PartyUInfo.Other, Is.EqualTo(copies[2]));
            Assert.That(parameters.PartyVInfo.Identity, Is.EqualTo(copies[3]));
            Assert.That(parameters.ProtectedParameters, Is.EqualTo(copies[4]));
            Assert.That(parameters.SupplementalPublicOther, Is.EqualTo(copies[5]));
            Assert.That(parameters.SupplementalPrivateInfo, Is.EqualTo(copies[6]));
        });
    }

    [Test]
    public void OutputSizeDepthAndPolicyLimitsAreEnforced()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(1, 128);
        TokenBytesResult unrestricted = CoseKdfContext.Create(parameters);
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = unrestricted.Bytes.Length;
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = unrestricted.Bytes.Length - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumCborDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        TokenBytesResult exactResult = CoseKdfContext.Create(
            parameters,
            exact);
        TokenBytesResult smallResult = CoseKdfContext.Create(
            parameters,
            small);
        TokenBytesResult shallowResult = CoseKdfContext.Create(
            parameters,
            shallow);
        TokenBytesResult invalidResult = CoseKdfContext.Create(
            parameters,
            invalid);

        Assert.Multiple(() =>
        {
            AssertSuccess(exactResult);
            AssertFailure(smallResult, "buffer_limit_exceeded");
            AssertFailure(shallowResult, "cbor_depth_exceeded");
            AssertFailure(invalidResult, "invalid_policy");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(1, 128);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseKdfContextParameters.Create((string)null, 128),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseKdfContext.Create(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseKdfContext.Create(parameters, null),
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

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Negative(int value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = (ulong)(-(long)value - 1L);
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

    static CborItem Null()
    {
        return new CborItem();
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
