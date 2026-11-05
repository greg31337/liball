using System.Security.Cryptography;
using token;

namespace token.Tests;

public class EcdsaSignatureCodecTests
{
    [Test]
    public void DerToRawConvertsKnownSignatureWithPaddingAndFixedWidth()
    {
        byte[] der = new byte[]
        {
            0x30, 0x09,
            0x02, 0x02, 0x00, 0x80,
            0x02, 0x03, 0x01, 0x02, 0x03
        };

        TokenBytesResult result = EcdsaSignatureCodec.DerToRaw(der, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0, 0, 0, 0x80,
                0, 1, 2, 3
            }));
        });
    }

    [Test]
    public void DerToRawConvertsRealP256SignatureThatVerifiesInRawFormat()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("strict DER conversion");
        byte[] der = key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        TokenBytesResult result = EcdsaSignatureCodec.DerToRaw(der, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Has.Length.EqualTo(64));
            Assert.That(
                key.VerifyData(data, result.Bytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation),
                Is.True);
        });
    }

    [Test]
    public void DerToRawSupportsLongFormSequenceLengthForP521()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("long DER sequence");
        byte[] der = key.SignData(data, HashAlgorithmName.SHA512, DSASignatureFormat.Rfc3279DerSequence);

        TokenBytesResult result = EcdsaSignatureCodec.DerToRaw(der, 66);

        Assert.Multiple(() =>
        {
            Assert.That(der[1], Is.GreaterThanOrEqualTo(0x81));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Has.Length.EqualTo(132));
            Assert.That(
                key.VerifyData(data, result.Bytes, HashAlgorithmName.SHA512, DSASignatureFormat.IeeeP1363FixedFieldConcatenation),
                Is.True);
        });
    }

    [Test]
    public void DerToRawSupportsInputSpanSlices()
    {
        byte[] wrapped = new byte[] { 0xFF, 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02, 0xFF };

        TokenBytesResult result = EcdsaSignatureCodec.DerToRaw(wrapped.AsSpan(1, 8), 2);

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 1, 0, 2 }));
    }

    [TestCase(new byte[] { }, 0)]
    [TestCase(new byte[] { 0x31, 0x00 }, 0)]
    [TestCase(new byte[] { 0x30 }, 1)]
    [TestCase(new byte[] { 0x30, 0x80, 0x00, 0x00 }, 1)]
    [TestCase(new byte[] { 0x30, 0x81, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2 }, 1)]
    [TestCase(new byte[] { 0x30, 0x82, 0x00, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2 }, 2)]
    [TestCase(new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x02, 0x01 }, 7)]
    [TestCase(new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2, 0 }, 8)]
    public void DerToRawRejectsMalformedSequenceEncoding(byte[] der, int position)
    {
        AssertInvalidDer(EcdsaSignatureCodec.DerToRaw(der, 2), position);
    }

    [TestCase(new byte[] { 0x30, 0x06, 0x03, 0x01, 1, 0x02, 0x01, 2 }, 2)]
    [TestCase(new byte[] { 0x30, 0x06, 0x02, 0x00, 0x02, 0x02, 1, 2 }, 4)]
    [TestCase(new byte[] { 0x30, 0x06, 0x02, 0x01, 0x80, 0x02, 0x01, 2 }, 4)]
    [TestCase(new byte[] { 0x30, 0x07, 0x02, 0x02, 0, 1, 0x02, 0x01, 2 }, 4)]
    [TestCase(new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x03, 0x01, 2 }, 5)]
    [TestCase(new byte[] { 0x30, 0x07, 0x02, 0x02, 1, 2, 0x02, 0x01, 3 }, 4)]
    public void DerToRawRejectsInvalidIntegerEncoding(byte[] der, int position)
    {
        AssertInvalidDer(EcdsaSignatureCodec.DerToRaw(der, 1), position);
    }

    [Test]
    public void DerToRawAcceptsCanonicalZeroComponents()
    {
        byte[] der = new byte[] { 0x30, 0x06, 0x02, 0x01, 0, 0x02, 0x01, 0 };

        TokenBytesResult result = EcdsaSignatureCodec.DerToRaw(der, 2);

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void DerToRawRejectsNullAndInvalidComponentWidths()
    {
        byte[] valid = new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2 };

        Assert.Multiple(() =>
        {
            Assert.That(() => EcdsaSignatureCodec.DerToRaw((byte[])null, 32), Throws.ArgumentNullException);
            Assert.That(() => EcdsaSignatureCodec.DerToRaw(valid, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EcdsaSignatureCodec.DerToRaw(valid, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EcdsaSignatureCodec.DerToRaw(valid, int.MaxValue), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void RawToDerProducesCanonicalIntegersWithRequiredSignPadding()
    {
        byte[] raw = new byte[]
        {
            0, 0, 0, 0x80,
            0, 1, 2, 3
        };

        TokenBytesResult result = EcdsaSignatureCodec.RawToDer(raw, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x30, 0x09,
                0x02, 0x02, 0x00, 0x80,
                0x02, 0x03, 0x01, 0x02, 0x03
            }));
        });
    }

    [Test]
    public void RawToDerUsesMinimalEncodingForZeroAndLeadingZeroes()
    {
        TokenBytesResult zero = EcdsaSignatureCodec.RawToDer(new byte[] { 0, 0, 0, 0 }, 2);
        TokenBytesResult values = EcdsaSignatureCodec.RawToDer(new byte[] { 0, 1, 0, 2 }, 2);

        Assert.Multiple(() =>
        {
            Assert.That(zero.Bytes, Is.EqualTo(new byte[] { 0x30, 0x06, 0x02, 0x01, 0, 0x02, 0x01, 0 }));
            Assert.That(values.Bytes, Is.EqualTo(new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2 }));
        });
    }

    [Test]
    public void RawToDerConvertsRealP256SignatureThatVerifiesInDerFormat()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("strict raw conversion");
        byte[] raw = key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        TokenBytesResult result = EcdsaSignatureCodec.RawToDer(raw, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(
                key.VerifyData(data, result.Bytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence),
                Is.True);
            Assert.That(EcdsaSignatureCodec.DerToRaw(result.Bytes, 32).Bytes, Is.EqualTo(raw));
        });
    }

    [Test]
    public void RawToDerUsesCanonicalLongFormSequenceLengthForP521()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("long raw conversion");
        byte[] raw = key.SignData(data, HashAlgorithmName.SHA512, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        TokenBytesResult result = EcdsaSignatureCodec.RawToDer(raw, 66);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes[0], Is.EqualTo(0x30));
            Assert.That(result.Bytes[1], Is.EqualTo(0x81));
            Assert.That(result.Bytes[2], Is.EqualTo(result.Bytes.Length - 3));
            Assert.That(
                key.VerifyData(data, result.Bytes, HashAlgorithmName.SHA512, DSASignatureFormat.Rfc3279DerSequence),
                Is.True);
            Assert.That(EcdsaSignatureCodec.DerToRaw(result.Bytes, 66).Bytes, Is.EqualTo(raw));
        });
    }

    [Test]
    public void RawToDerSupportsInputSpanSlices()
    {
        byte[] wrapped = new byte[] { 9, 0, 1, 0, 2, 9 };

        TokenBytesResult result = EcdsaSignatureCodec.RawToDer(wrapped.AsSpan(1, 4), 2);

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x30, 0x06, 0x02, 0x01, 1, 0x02, 0x01, 2 }));
    }

    [TestCase(new byte[] { }, 1, 0)]
    [TestCase(new byte[] { 1, 2, 3 }, 2, 3)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5 }, 2, 4)]
    public void RawToDerRejectsIncorrectFixedWidthLengths(byte[] raw, int componentWidth, int position)
    {
        TokenBytesResult result = EcdsaSignatureCodec.RawToDer(raw, componentWidth);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_ecdsa_raw"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    [Test]
    public void RawToDerRejectsNullAndInvalidComponentWidths()
    {
        byte[] valid = new byte[] { 1, 2 };

        Assert.Multiple(() =>
        {
            Assert.That(() => EcdsaSignatureCodec.RawToDer((byte[])null, 1), Throws.ArgumentNullException);
            Assert.That(() => EcdsaSignatureCodec.RawToDer(valid, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EcdsaSignatureCodec.RawToDer(valid, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => EcdsaSignatureCodec.RawToDer(valid, int.MaxValue), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static void AssertInvalidDer(TokenBytesResult result, int position)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_ecdsa_der"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }
}
