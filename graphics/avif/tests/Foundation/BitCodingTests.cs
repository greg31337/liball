namespace avif.Tests;

public class BitCodingTests
{
    [Test]
    public void UvlcKnownValuesUseExpectedBits()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new BitWriter(buffer.Length, 12);

        writer.WriteUvlc(buffer, 0);
        writer.WriteUvlc(buffer, 1);
        writer.WriteUvlc(buffer, 2);
        writer.WriteUvlc(buffer, 3);

        Assert.That(buffer, Is.EqualTo(new byte[] { 166, 64 }));

        BitReader reader = new BitReader(buffer.Length, writer.BitPosition);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUvlc(buffer), Is.EqualTo(0));
            Assert.That(reader.ReadUvlc(buffer), Is.EqualTo(1));
            Assert.That(reader.ReadUvlc(buffer), Is.EqualTo(2));
            Assert.That(reader.ReadUvlc(buffer), Is.EqualTo(3));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.RemainingBits(), Is.Zero);
        });
    }

    [Test]
    public void UvlcMaximumValueUsesThirtyTwoLeadingZeros()
    {
        byte[] buffer = new byte[5];
        BitWriter writer = new BitWriter(buffer.Length, 33);
        writer.WriteUvlc(buffer, uint.MaxValue);

        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 0, 0, 0, 128 }));

        BitReader reader = new BitReader(buffer.Length, 33);
        uint value = reader.ReadUvlc(buffer);
        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(uint.MaxValue));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.BitPosition, Is.EqualTo(33));
        });
    }

    [Test]
    public void UvlcRejectsOverlongAndTruncatedCodesAtomically()
    {
        byte[] overlong = [0, 0, 0, 0, 64];
        byte[] truncated = [0];
        BitReader overlongReader = new BitReader(overlong.Length, 34);
        BitReader truncatedReader = BitReader.Create(truncated.Length);

        uint overlongValue = overlongReader.ReadUvlc(overlong);
        uint truncatedValue = truncatedReader.ReadUvlc(truncated);

        Assert.Multiple(() =>
        {
            Assert.That(overlongValue, Is.Zero);
            Assert.That(overlongReader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(overlongReader.BitPosition, Is.Zero);
            Assert.That(truncatedValue, Is.Zero);
            Assert.That(truncatedReader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncatedReader.BitPosition, Is.Zero);
        });
    }

    [Test]
    public void NsKnownValuesUseExpectedVariableWidths()
    {
        byte[] expectedBytes = [0, 64, 128, 192, 224];
        long[] expectedBits = [2, 2, 2, 3, 3];
        uint value = 0;
        while (value < 5)
        {
            byte[] buffer = new byte[1];
            BitWriter writer = BitWriter.Create(buffer.Length);
            writer.WriteNs(buffer, value, 5);

            Assert.That(buffer[0], Is.EqualTo(expectedBytes[value]));
            Assert.That(writer.BitPosition, Is.EqualTo(expectedBits[value]));

            BitReader reader = new BitReader(buffer.Length, writer.BitPosition);
            Assert.That(reader.ReadNs(buffer, 5), Is.EqualTo(value));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            value++;
        }
    }

    [Test]
    public void NsRoundTripsEveryValueAcrossSmallAlphabetSizes()
    {
        uint valueCount = 1;
        while (valueCount <= 33)
        {
            uint value = 0;
            while (value < valueCount)
            {
                byte[] buffer = new byte[8];
                BitWriter writer = BitWriter.Create(buffer.Length);
                writer.WriteNs(buffer, value, valueCount);
                BitReader reader = new BitReader(buffer.Length, writer.BitPosition);

                Assert.That(reader.ReadNs(buffer, valueCount), Is.EqualTo(value));
                Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
                value++;
            }

            valueCount++;
        }
    }

    [Test]
    public void NsRejectsInvalidValueAndTruncatedExtraBitAtomically()
    {
        byte[] destination = [204];
        BitWriter writer = BitWriter.Create(destination.Length);
        AvifResultCode writeCode = writer.WriteNs(destination, 5, 5);

        byte[] truncated = [192];
        BitReader reader = new BitReader(truncated.Length, 2);
        uint value = reader.ReadNs(truncated, 5);

        Assert.Multiple(() =>
        {
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.BitPosition, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204 }));
            Assert.That(value, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(reader.BitPosition, Is.Zero);
        });
    }
}
