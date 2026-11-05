using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ByteDecoderTests
{
    [Test]
    public void RoundTripsEveryByteValueAndEmbeddedZeroesLosslessly()
    {
        byte[] expected = new byte[258];
        for (int value = 0; value < 256; value++)
        {
            expected[value] = (byte)value;
        }

        expected[256] = 0;
        expected[257] = 255;
        BitBuffer buffer = new BitBuffer(0);
        ByteEncoder.EncodeBytes(expected, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        byte[] actual = ByteDecoder.DecodeBytes(reader, expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesFromAnUnalignedReaderPosition()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);
        ByteEncoder.EncodeBytes(new byte[] { 0, 128, 255 }, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());
        reader.SkipBits(3);

        byte[] decoded = ByteDecoder.DecodeBytes(reader, 3);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(new byte[] { 0, 128, 255 }));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void EmptySegmentReturnsAnIndependentlyOwnedArray()
    {
        BitReader reader = new BitReader(new byte[0], 0);

        byte[] first = ByteDecoder.DecodeBytes(reader, 0);
        byte[] second = ByteDecoder.DecodeBytes(reader, 0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Empty);
            Assert.That(second, Is.Empty);
            Assert.That(second, Is.Not.SameAs(first));
        });
    }

    [Test]
    public void ReturnedBytesDoNotAliasReaderStorageOrLaterResults()
    {
        byte[] encoded = new byte[] { 1, 0, 2 };
        BitReader reader = new BitReader(encoded, 24);
        encoded[0] = 99;

        byte[] first = ByteDecoder.DecodeBytes(reader, 3);
        first[0] = 77;
        reader.SeekBitPosition(0);
        byte[] second = ByteDecoder.DecodeBytes(reader, 3);

        Assert.That(second, Is.EqualTo(new byte[] { 1, 0, 2 }));
    }

    [Test]
    public void TruncatedSegmentIsRejectedBeforeReaderPositionChanges()
    {
        BitReader reader = new BitReader(new byte[] { 1, 2 }, 15);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            ByteDecoder.DecodeBytes(reader, 2))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsNullReaderAndInvalidOrOverLimitCounts()
    {
        BitReader reader = new BitReader(new byte[0], 0);
        QrInputException nullReader = Assert.Throws<QrInputException>(() =>
            ByteDecoder.DecodeBytes(null!, 0))!;
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            ByteDecoder.DecodeBytes(reader, -1))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            ByteDecoder.DecodeBytes(reader, QrLimits.DefaultMaximumInputBytes + 1))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullReader.InputName, Is.EqualTo("reader"));
            Assert.That(negative.InputName, Is.EqualTo("byteCount"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }
}
