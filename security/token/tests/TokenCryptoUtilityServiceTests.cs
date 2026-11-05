using System.Security.Cryptography;
using token;

namespace token.Tests;

public class TokenCryptoUtilityServiceTests
{
    [Test]
    public void FixedTimeComparisonSupportsArraysAndSpanSlices()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        byte[] left = new byte[] { 9, 1, 2, 3, 9 };
        byte[] right = new byte[] { 8, 1, 2, 3, 8 };

        Assert.Multiple(() =>
        {
            Assert.That(service.FixedTimeEquals(new byte[] { 1, 2 }, new byte[] { 1, 2 }), Is.True);
            Assert.That(service.FixedTimeEquals(new byte[] { 1, 2 }, new byte[] { 1, 3 }), Is.False);
            Assert.That(service.FixedTimeEquals(new byte[] { 1 }, new byte[] { 1, 0 }), Is.False);
            Assert.That(service.FixedTimeEquals(null!, null!), Is.False);
            Assert.That(service.FixedTimeEqualsSpan(left.AsSpan(1, 3), right.AsSpan(1, 3)), Is.True);
        });
    }

    [Test]
    public void SecureRandomOperationsReturnAndFillRequestedBytes()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        TokenBytesResult first = service.GenerateRandomBytes(32);
        TokenBytesResult second = service.GenerateRandomBytes(32);
        byte[] destination = new byte[34];
        destination[0] = 91;
        destination[33] = 92;

        service.FillRandomBytes(destination.AsSpan(1, 32));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Bytes, Has.Length.EqualTo(32));
            Assert.That(second.Bytes, Is.Not.EqualTo(first.Bytes));
            Assert.That(destination[0], Is.EqualTo(91));
            Assert.That(destination[33], Is.EqualTo(92));
            Assert.That(destination.AsSpan(1, 32).ToArray(), Is.Not.EqualTo(new byte[32]));
            Assert.That(service.GenerateRandomBytes(0).Bytes, Is.Empty);
        });
    }

    [Test]
    public void BigEndianOperationsNormalizeMinimizeEncodeAndWrite()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        byte[] wrapped = new byte[] { 9, 0, 0, 0x12, 0x34, 9 };
        byte[] destination = new byte[] { 9, 9, 9, 9, 9 };

        TokenBytesResult normalized = service.NormalizeBigEndianInteger(new byte[] { 0, 0x12, 0x34 }, 4);
        TokenBytesResult spanNormalized = service.NormalizeBigEndianIntegerSpan(wrapped.AsSpan(1, 4), 3);
        TokenBytesResult minimal = service.MinimizeBigEndianInteger(new byte[] { 0, 0, 0x80 });
        TokenBytesResult spanMinimal = service.MinimizeBigEndianIntegerSpan(wrapped.AsSpan(1, 4));
        TokenBytesResult encoded = service.EncodeUInt64BigEndian(0x1234, 4);
        TokenResult written = service.WriteBigEndianIntegerSpan(
            new byte[] { 0, 0xAB, 0xCD },
            destination.AsSpan(1, 3));

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Bytes, Is.EqualTo(new byte[] { 0, 0, 0x12, 0x34 }));
            Assert.That(spanNormalized.Bytes, Is.EqualTo(new byte[] { 0, 0x12, 0x34 }));
            Assert.That(minimal.Bytes, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(spanMinimal.Bytes, Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 0, 0, 0x12, 0x34 }));
            Assert.That(written.Success, Is.True);
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 0, 0xAB, 0xCD, 9 }));
        });
    }

    [Test]
    public void BigEndianOverflowReturnsStableFailureWithoutChangingDestination()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        byte[] destination = new byte[] { 7, 7 };

        TokenBytesResult normalized = service.NormalizeBigEndianInteger(new byte[] { 1, 2, 3 }, 2);
        TokenBytesResult encoded = service.EncodeUInt64BigEndian(256, 1);
        TokenResult written = service.WriteBigEndianIntegerSpan(new byte[] { 1, 2, 3 }, destination);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Error!.Code, Is.EqualTo("integer_too_large"));
            Assert.That(encoded.Error!.Code, Is.EqualTo("integer_too_large"));
            Assert.That(written.Error!.Code, Is.EqualTo("integer_too_large"));
            Assert.That(destination, Is.EqualTo(new byte[] { 7, 7 }));
        });
    }

    [Test]
    public void EcdsaConversionRoundTripsRealSignaturesThroughArrayAndSpanApis()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("public crypto utility");
        byte[] der = key.SignData(
            data,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        byte[] wrappedDer = new byte[der.Length + 2];
        der.CopyTo(wrappedDer, 1);

        TokenBytesResult raw = service.ConvertEcdsaDerToRaw(der, 32);
        TokenBytesResult spanRaw = service.ConvertEcdsaDerToRawSpan(
            wrappedDer.AsSpan(1, der.Length),
            32);
        TokenBytesResult convertedDer = service.ConvertEcdsaRawToDer(raw.Bytes!, 32);
        TokenBytesResult spanDer = service.ConvertEcdsaRawToDerSpan(raw.Bytes.AsSpan(), 32);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Success, Is.True);
            Assert.That(raw.Bytes, Has.Length.EqualTo(64));
            Assert.That(spanRaw.Bytes, Is.EqualTo(raw.Bytes));
            Assert.That(convertedDer.Bytes, Is.EqualTo(der));
            Assert.That(spanDer.Bytes, Is.EqualTo(der));
            Assert.That(
                key.VerifyData(
                    data,
                    raw.Bytes,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation),
                Is.True);
        });
    }

    [Test]
    public void EcdsaConversionRejectsMalformedDerAndWrongRawLength()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();

        TokenBytesResult der = service.ConvertEcdsaDerToRaw(new byte[] { 0x31, 0 }, 32);
        TokenBytesResult raw = service.ConvertEcdsaRawToDer(new byte[] { 1, 2, 3 }, 2);

        Assert.Multiple(() =>
        {
            Assert.That(der.Error!.Code, Is.EqualTo("invalid_ecdsa_der"));
            Assert.That(raw.Error!.Code, Is.EqualTo("invalid_ecdsa_raw"));
        });
    }

    [Test]
    public void EcdsaConversionMatchesCanonicalKnownVector()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();
        byte[] der = new byte[]
        {
            0x30, 0x09,
            0x02, 0x02, 0x00, 0x80,
            0x02, 0x03, 0x01, 0x02, 0x03
        };
        byte[] raw = new byte[]
        {
            0, 0, 0, 0x80,
            0, 1, 2, 3
        };

        TokenBytesResult decoded = service.ConvertEcdsaDerToRaw(der, 4);
        TokenBytesResult encoded = service.ConvertEcdsaRawToDer(raw, 4);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Bytes, Is.EqualTo(raw));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes, Is.EqualTo(der));
        });
    }

    [Test]
    public void CryptoUtilityRejectsInvalidArguments()
    {
        ITokenCryptoUtilityService service = new TokenCryptoUtilityService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.GenerateRandomBytes(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => service.NormalizeBigEndianInteger(null!, 1), Throws.ArgumentNullException);
            Assert.That(() => service.MinimizeBigEndianInteger(null!), Throws.ArgumentNullException);
            Assert.That(() => service.NormalizeBigEndianInteger(Array.Empty<byte>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => service.EncodeUInt64BigEndian(0, 9), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => service.ConvertEcdsaDerToRaw(null!, 32), Throws.ArgumentNullException);
            Assert.That(() => service.ConvertEcdsaRawToDer(null!, 32), Throws.ArgumentNullException);
            Assert.That(() => service.ConvertEcdsaRawToDer(new byte[] { 1, 2 }, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
