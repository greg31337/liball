using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborFloatTests
{
    [Test]
    public void HalfPrecisionPreservesZerosSubnormalsFiniteValuesAndInfinities()
    {
        CborFloatValue positiveZero = CborHalfPrecision.CreateHalfFloat(0x0000);
        CborFloatValue negativeZero = CborHalfPrecision.CreateHalfFloat(0x8000);
        CborFloatValue minimumSubnormal = CborHalfPrecision.CreateHalfFloat(0x0001);
        CborFloatValue maximumSubnormal = CborHalfPrecision.CreateHalfFloat(0x03ff);
        CborFloatValue minimumNormal = CborHalfPrecision.CreateHalfFloat(0x0400);
        CborFloatValue maximumFinite = CborHalfPrecision.CreateHalfFloat(0x7bff);
        CborFloatValue positiveInfinity = CborHalfPrecision.CreateHalfFloat(0x7c00);
        CborFloatValue negativeInfinity = CborHalfPrecision.CreateHalfFloat(0xfc00);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToUInt64Bits(positiveZero.Value), Is.EqualTo(0UL));
            Assert.That(BitConverter.DoubleToUInt64Bits(negativeZero.Value), Is.EqualTo(0x8000000000000000UL));
            Assert.That(minimumSubnormal.Value, Is.EqualTo(1.0 / 16777216.0));
            Assert.That(maximumSubnormal.Value, Is.EqualTo(1023.0 / 16777216.0));
            Assert.That(minimumNormal.Value, Is.EqualTo(1.0 / 16384.0));
            Assert.That(maximumFinite.Value, Is.EqualTo(65504.0));
            Assert.That(double.IsPositiveInfinity(positiveInfinity.Value), Is.True);
            Assert.That(double.IsNegativeInfinity(negativeInfinity.Value), Is.True);
            Assert.That(maximumFinite.Width, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(maximumFinite.RawBits, Is.EqualTo(0x7bff));
        });
    }

    [Test]
    public void EveryWidthClassifiesQuietAndSignalingNanPayloads()
    {
        const ushort halfQuietBits = 0x7e55;
        const ushort halfSignalingBits = 0x7d55;
        const uint singleQuietBits = 0x7fc12345;
        const uint singleSignalingBits = 0x7fa12345;
        const ulong doubleQuietBits = 0x7ff8123456789abc;
        const ulong doubleSignalingBits = 0x7ff0123456789abc;
        CborFloatValue halfQuiet = CborHalfPrecision.CreateHalfFloat(halfQuietBits);
        CborFloatValue halfSignaling = CborHalfPrecision.CreateHalfFloat(halfSignalingBits);
        CborFloatValue singleQuiet = CborHalfPrecision.CreateSingleFloat(singleQuietBits);
        CborFloatValue singleSignaling = CborHalfPrecision.CreateSingleFloat(singleSignalingBits);
        CborFloatValue doubleQuiet = CborHalfPrecision.CreateDoubleFloat(doubleQuietBits);
        CborFloatValue doubleSignaling = CborHalfPrecision.CreateDoubleFloat(doubleSignalingBits);

        Assert.Multiple(() =>
        {
            Assert.That(halfQuiet.IsNaN, Is.True);
            Assert.That(halfQuiet.IsSignalingNaN, Is.False);
            Assert.That(halfQuiet.RawBits, Is.EqualTo(halfQuietBits));
            Assert.That(halfSignaling.IsNaN, Is.True);
            Assert.That(halfSignaling.IsSignalingNaN, Is.True);
            Assert.That(halfSignaling.RawBits, Is.EqualTo(halfSignalingBits));
            Assert.That(singleQuiet.IsNaN, Is.True);
            Assert.That(singleQuiet.IsSignalingNaN, Is.False);
            Assert.That(singleQuiet.RawBits, Is.EqualTo(singleQuietBits));
            Assert.That(singleSignaling.IsNaN, Is.True);
            Assert.That(singleSignaling.IsSignalingNaN, Is.True);
            Assert.That(singleSignaling.RawBits, Is.EqualTo(singleSignalingBits));
            Assert.That(doubleQuiet.IsNaN, Is.True);
            Assert.That(doubleQuiet.IsSignalingNaN, Is.False);
            Assert.That(doubleQuiet.RawBits, Is.EqualTo(doubleQuietBits));
            Assert.That(doubleSignaling.IsNaN, Is.True);
            Assert.That(doubleSignaling.IsSignalingNaN, Is.True);
            Assert.That(doubleSignaling.RawBits, Is.EqualTo(doubleSignalingBits));
        });
    }

    [Test]
    public void SingleAndDoubleConversionsPreserveSignedZeroSubnormalsAndInfinity()
    {
        CborFloatValue singleNegativeZero = CborHalfPrecision.CreateSingleFloat(0x80000000);
        CborFloatValue singleSubnormal = CborHalfPrecision.CreateSingleFloat(0x00000001);
        CborFloatValue singleInfinity = CborHalfPrecision.CreateSingleFloat(0x7f800000);
        CborFloatValue doubleNegativeZero = CborHalfPrecision.CreateDoubleFloat(0x8000000000000000);
        CborFloatValue doubleSubnormal = CborHalfPrecision.CreateDoubleFloat(0x0000000000000001);
        CborFloatValue doubleInfinity = CborHalfPrecision.CreateDoubleFloat(0x7ff0000000000000);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToUInt64Bits(singleNegativeZero.Value), Is.EqualTo(0x8000000000000000UL));
            Assert.That(singleSubnormal.Value, Is.EqualTo((double)float.Epsilon));
            Assert.That(double.IsPositiveInfinity(singleInfinity.Value), Is.True);
            Assert.That(BitConverter.DoubleToUInt64Bits(doubleNegativeZero.Value), Is.EqualTo(0x8000000000000000UL));
            Assert.That(doubleSubnormal.Value, Is.EqualTo(double.Epsilon));
            Assert.That(double.IsPositiveInfinity(doubleInfinity.Value), Is.True);
        });
    }

    [Test]
    public void RawRepresentationsReadAndWriteWithoutChangingPayloadBits()
    {
        CborFloatValue half = CborHalfPrecision.ReadHalfPrecision([0x7e, 0x55]);
        CborFloatValue single = CborHalfPrecision.ReadSinglePrecision([0x7f, 0xa1, 0x23, 0x45]);
        CborFloatValue doubleValue = CborHalfPrecision.ReadDoublePrecision(
            [0x7f, 0xf0, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc]);
        byte[] halfDestination = new byte[2];
        byte[] singleDestination = new byte[4];
        byte[] doubleDestination = new byte[8];

        int halfWritten = CborHalfPrecision.WriteFloatRepresentation(halfDestination, half);
        int singleWritten = CborHalfPrecision.WriteFloatRepresentation(singleDestination, single);
        int doubleWritten = CborHalfPrecision.WriteFloatRepresentation(doubleDestination, doubleValue);

        Assert.Multiple(() =>
        {
            Assert.That(halfWritten, Is.EqualTo(2));
            Assert.That(halfDestination, Is.EqualTo(new byte[] { 0x7e, 0x55 }));
            Assert.That(singleWritten, Is.EqualTo(4));
            Assert.That(singleDestination, Is.EqualTo(new byte[] { 0x7f, 0xa1, 0x23, 0x45 }));
            Assert.That(doubleWritten, Is.EqualTo(8));
            Assert.That(doubleDestination, Is.EqualTo(new byte[] { 0x7f, 0xf0, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc }));
        });
    }

    [Test]
    public void NumericConversionsAndExactnessChecksPreserveSignedZero()
    {
        double negativeZero = BitConverter.UInt64BitsToDouble(0x8000000000000000);

        Assert.Multiple(() =>
        {
            Assert.That(CborHalfPrecision.ConvertDoubleToHalfBits(1.5), Is.EqualTo(0x3e00));
            Assert.That(CborHalfPrecision.ConvertDoubleToSingleBits(1.5), Is.EqualTo(0x3fc00000));
            Assert.That(CborHalfPrecision.ConvertDoubleToDoubleBits(1.5), Is.EqualTo(0x3ff8000000000000));
            Assert.That(CborHalfPrecision.ConvertDoubleToHalfBits(negativeZero), Is.EqualTo(0x8000));
            Assert.That(CborHalfPrecision.ConvertDoubleToSingleBits(negativeZero), Is.EqualTo(0x80000000));
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsHalf(1.5), Is.True);
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsHalf(1.0001), Is.False);
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsSingle((double)float.Epsilon), Is.True);
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsSingle(1.0 + 1.0 / 1099511627776.0), Is.False);
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsHalf(double.NaN), Is.False);
            Assert.That(CborHalfPrecision.IsExactlyRepresentableAsSingle(double.NaN), Is.False);
        });
    }

    [Test]
    public void InvalidRetainedWidthOrBitsAreRejectedBeforeWriting()
    {
        CborFloatValue invalidWidth = new CborFloatValue
        {
            Width = CborFloatWidth.None,
            RawBits = 0
        };
        CborFloatValue oversizedHalf = new CborFloatValue
        {
            Width = CborFloatWidth.Half,
            RawBits = 65536
        };
        byte[] tooSmallDestination = [0xa5];
        CborFloatValue validHalf = CborHalfPrecision.CreateHalfFloat(0x3c00);

        CborEncodeException? widthFailure = Assert.Throws<CborEncodeException>(
            () => CborHalfPrecision.WriteFloatRepresentation(new byte[8], invalidWidth));
        CborEncodeException? bitsFailure = Assert.Throws<CborEncodeException>(
            () => CborHalfPrecision.WriteFloatRepresentation(new byte[8], oversizedHalf));
        CborEncodeException? sizeFailure = Assert.Throws<CborEncodeException>(
            () => CborHalfPrecision.WriteFloatRepresentation(tooSmallDestination, validHalf));

        Assert.Multiple(() =>
        {
            Assert.That(widthFailure?.ErrorCode, Is.EqualTo("CBOR_FLOAT_WIDTH_INVALID"));
            Assert.That(bitsFailure?.ErrorCode, Is.EqualTo("CBOR_FLOAT_BITS_OUT_OF_RANGE"));
            Assert.That(sizeFailure?.ErrorCode, Is.EqualTo("CBOR_DESTINATION_TOO_SMALL"));
            Assert.That(tooSmallDestination, Is.EqualTo(new byte[] { 0xa5 }));
        });
    }
}
