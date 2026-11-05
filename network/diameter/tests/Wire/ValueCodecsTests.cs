using System;

namespace diameter.Tests;

public class ValueCodecsTests
{
    [Test]
    public void IntegerCodecsUseExactNetworkByteOrder()
    {
        byte[] integer32 = new byte[4];
        byte[] integer64 = new byte[8];
        byte[] unsigned32 = new byte[4];
        byte[] unsigned64 = new byte[8];

        DiameterValueResult encodedInteger32 = ValueCodecs.EncodeInteger32(integer32, -2);
        DiameterValueResult encodedInteger64 = ValueCodecs.EncodeInteger64(integer64, long.MinValue);
        DiameterValueResult encodedUnsigned32 = ValueCodecs.EncodeUnsigned32(unsigned32, 2309737967U);
        DiameterValueResult encodedUnsigned64 = ValueCodecs.EncodeUnsigned64(unsigned64, 81985529216486895UL);

        DiameterValueResult decodedInteger32 = ValueCodecs.DecodeInteger32(integer32);
        DiameterValueResult decodedInteger64 = ValueCodecs.DecodeInteger64(integer64);
        DiameterValueResult decodedUnsigned32 = ValueCodecs.DecodeUnsigned32(unsigned32);
        DiameterValueResult decodedUnsigned64 = ValueCodecs.DecodeUnsigned64(unsigned64);

        Assert.Multiple(() =>
        {
            AssertSuccess(encodedInteger32, 4, 4, 0);
            AssertSuccess(encodedInteger64, 8, 8, 0);
            AssertSuccess(encodedUnsigned32, 4, 4, 0);
            AssertSuccess(encodedUnsigned64, 8, 8, 0);
            Assert.That(integer32, Is.EqualTo(new byte[] { 255, 255, 255, 254 }));
            Assert.That(integer64, Is.EqualTo(new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(unsigned32, Is.EqualTo(new byte[] { 137, 171, 205, 239 }));
            Assert.That(unsigned64, Is.EqualTo(new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 }));
            AssertSuccess(decodedInteger32, 4, 0, 4);
            AssertSuccess(decodedInteger64, 8, 0, 8);
            AssertSuccess(decodedUnsigned32, 4, 0, 4);
            AssertSuccess(decodedUnsigned64, 8, 0, 8);
            Assert.That(decodedInteger32.Integer32, Is.EqualTo(-2));
            Assert.That(decodedInteger64.Integer64, Is.EqualTo(long.MinValue));
            Assert.That(decodedUnsigned32.Unsigned32, Is.EqualTo(2309737967U));
            Assert.That(decodedUnsigned64.Unsigned64, Is.EqualTo(81985529216486895UL));
        });
    }

    [Test]
    public void IntegerCodecsRoundTripMinimumAndMaximumValues()
    {
        byte[] value32 = new byte[4];
        byte[] value64 = new byte[8];

        ValueCodecs.EncodeInteger32(value32, int.MinValue);
        Assert.That(ValueCodecs.DecodeInteger32(value32).Integer32, Is.EqualTo(int.MinValue));
        ValueCodecs.EncodeInteger32(value32, int.MaxValue);
        Assert.That(ValueCodecs.DecodeInteger32(value32).Integer32, Is.EqualTo(int.MaxValue));
        ValueCodecs.EncodeUnsigned32(value32, uint.MaxValue);
        Assert.That(ValueCodecs.DecodeUnsigned32(value32).Unsigned32, Is.EqualTo(uint.MaxValue));

        ValueCodecs.EncodeInteger64(value64, long.MinValue);
        Assert.That(ValueCodecs.DecodeInteger64(value64).Integer64, Is.EqualTo(long.MinValue));
        ValueCodecs.EncodeInteger64(value64, long.MaxValue);
        Assert.That(ValueCodecs.DecodeInteger64(value64).Integer64, Is.EqualTo(long.MaxValue));
        ValueCodecs.EncodeUnsigned64(value64, ulong.MaxValue);
        Assert.That(ValueCodecs.DecodeUnsigned64(value64).Unsigned64, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void FloatCodecsPreserveIeeeWireBits()
    {
        byte[] float32 = new byte[4];
        byte[] float64 = new byte[8];

        DiameterValueResult encodedFloat32 = ValueCodecs.EncodeFloat32(float32, 1.0F);
        DiameterValueResult encodedFloat64 = ValueCodecs.EncodeFloat64(float64, -0.0);
        DiameterValueResult decodedFloat32 = ValueCodecs.DecodeFloat32(float32);
        DiameterValueResult decodedFloat64 = ValueCodecs.DecodeFloat64(float64);

        Assert.Multiple(() =>
        {
            AssertSuccess(encodedFloat32, 4, 4, 0);
            AssertSuccess(encodedFloat64, 8, 8, 0);
            Assert.That(float32, Is.EqualTo(new byte[] { 63, 128, 0, 0 }));
            Assert.That(float64, Is.EqualTo(new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(decodedFloat32.Float32, Is.EqualTo(1.0F));
            Assert.That(BitConverter.DoubleToInt64Bits(decodedFloat64.Float64), Is.EqualTo(long.MinValue));
            Assert.That(decodedFloat32.ConsumedCount, Is.EqualTo(4));
            Assert.That(decodedFloat64.ConsumedCount, Is.EqualTo(8));
        });
    }

    [Test]
    public void FloatCodecsPreserveInfinityAndNanPayloadBits()
    {
        byte[] positiveInfinity = new byte[] { 127, 128, 0, 0 };
        byte[] nan = new byte[] { 127, 248, 0, 0, 0, 0, 0, 1 };

        DiameterValueResult float32 = ValueCodecs.DecodeFloat32(positiveInfinity);
        DiameterValueResult float64 = ValueCodecs.DecodeFloat64(nan);
        byte[] encodedInfinity = new byte[4];
        byte[] encodedNan = new byte[8];
        ValueCodecs.EncodeFloat32(encodedInfinity, float32.Float32);
        ValueCodecs.EncodeFloat64(encodedNan, float64.Float64);

        Assert.Multiple(() =>
        {
            Assert.That(float.IsPositiveInfinity(float32.Float32), Is.True);
            Assert.That(double.IsNaN(float64.Float64), Is.True);
            Assert.That(encodedInfinity, Is.EqualTo(positiveInfinity));
            Assert.That(encodedNan, Is.EqualTo(nan));
        });
    }

    [Test]
    public void FixedWidthDecodersIgnoreTrailingInputAndRejectShortInput()
    {
        DiameterValueResult complete32 = ValueCodecs.DecodeUnsigned32(
            new byte[] { 0, 0, 0, 1, 99 });
        DiameterValueResult complete64 = ValueCodecs.DecodeUnsigned64(
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 99 });
        DiameterValueResult shortInteger32 = ValueCodecs.DecodeInteger32(new byte[3]);
        DiameterValueResult shortInteger64 = ValueCodecs.DecodeInteger64(new byte[7]);
        DiameterValueResult shortUnsigned32 = ValueCodecs.DecodeUnsigned32(new byte[0]);
        DiameterValueResult shortUnsigned64 = ValueCodecs.DecodeUnsigned64(new byte[7]);
        DiameterValueResult shortFloat32 = ValueCodecs.DecodeFloat32(new byte[3]);
        DiameterValueResult shortFloat64 = ValueCodecs.DecodeFloat64(new byte[7]);

        Assert.Multiple(() =>
        {
            Assert.That(complete32.Unsigned32, Is.EqualTo(1U));
            Assert.That(complete32.ConsumedCount, Is.EqualTo(4));
            Assert.That(complete64.Unsigned64, Is.EqualTo(1UL));
            Assert.That(complete64.ConsumedCount, Is.EqualTo(8));
            AssertIncomplete(shortInteger32, 4, 3);
            AssertIncomplete(shortInteger64, 8, 7);
            AssertIncomplete(shortUnsigned32, 4, 0);
            AssertIncomplete(shortUnsigned64, 8, 7);
            AssertIncomplete(shortFloat32, 4, 3);
            AssertIncomplete(shortFloat64, 8, 7);
        });
    }

    [Test]
    public void FixedWidthEncodersRejectShortDestinationsWithoutWriting()
    {
        byte[] destination32 = new byte[] { 170, 170, 170 };
        byte[] destination64 = new byte[] { 170, 170, 170, 170, 170, 170, 170 };

        DiameterValueResult integer32 = ValueCodecs.EncodeInteger32(destination32, 1);
        DiameterValueResult integer64 = ValueCodecs.EncodeInteger64(destination64, 1L);
        DiameterValueResult unsigned32 = ValueCodecs.EncodeUnsigned32(destination32, 1U);
        DiameterValueResult unsigned64 = ValueCodecs.EncodeUnsigned64(destination64, 1UL);
        DiameterValueResult float32 = ValueCodecs.EncodeFloat32(destination32, 1.0F);
        DiameterValueResult float64 = ValueCodecs.EncodeFloat64(destination64, 1.0);

        Assert.Multiple(() =>
        {
            AssertDestinationTooSmall(integer32, 4);
            AssertDestinationTooSmall(integer64, 8);
            AssertDestinationTooSmall(unsigned32, 4);
            AssertDestinationTooSmall(unsigned64, 8);
            AssertDestinationTooSmall(float32, 4);
            AssertDestinationTooSmall(float64, 8);
            Assert.That(destination32, Is.All.EqualTo(170));
            Assert.That(destination64, Is.All.EqualTo(170));
        });
    }

    [Test]
    public void OctetStringCodecCopiesValuesAndSupportsEmptyInput()
    {
        byte[] value = new byte[] { 1, 2, 3 };
        byte[] destination = new byte[3];
        DiameterValueResult encoding = ValueCodecs.EncodeOctetString(destination, value);
        DiameterValueResult decoding = ValueCodecs.DecodeOctetString(destination);
        destination[0] = 9;

        DiameterValueResult emptyEncoding = ValueCodecs.EncodeOctetString(
            new byte[0],
            new byte[0]);
        DiameterValueResult emptyDecoding = ValueCodecs.DecodeOctetString(new byte[0]);

        Assert.Multiple(() =>
        {
            AssertSuccess(encoding, 3, 3, 0);
            Assert.That(decoding.RequiredCount, Is.EqualTo(3));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(3));
            Assert.That(decoding.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(decoding.Data, Is.Not.SameAs(destination));
            AssertSuccess(emptyEncoding, 0, 0, 0);
            Assert.That(emptyDecoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(emptyDecoding.Data, Is.Empty);
        });
    }

    [Test]
    public void OctetStringEncodingRejectsShortDestinationWithoutWriting()
    {
        byte[] destination = new byte[] { 170, 170 };

        DiameterValueResult result = ValueCodecs.EncodeOctetString(
            destination,
            new byte[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            AssertDestinationTooSmall(result, 3);
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    private static void AssertSuccess(
        DiameterValueResult result,
        int requiredCount,
        int writtenCount,
        int consumedCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.EqualTo(writtenCount));
        Assert.That(result.ConsumedCount, Is.EqualTo(consumedCount));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertIncomplete(DiameterValueResult result, int requiredCount, int offset)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Information));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo("incomplete-value"));
    }

    private static void AssertDestinationTooSmall(DiameterValueResult result, int requiredCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error));
        Assert.That(result.Issues[0].Offset, Is.Zero);
        Assert.That(result.Issues[0].Code, Is.EqualTo("destination-too-small"));
    }
}
