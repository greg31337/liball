using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8BoolDecoderTests
{
    [Test]
    public void Constructor_InitializesRangeAndFirstSixteenBits()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0x12, 0x34, 0x56 });

        Assert.That(decoder.Range, Is.EqualTo(255));
        Assert.That(decoder.Value, Is.EqualTo(0x1234U));
        Assert.That(decoder.BitCount, Is.EqualTo(0));
        Assert.That(decoder.BytePosition(), Is.EqualTo(2));
    }

    [Test]
    public void DecodeBool_ReturnsZeroOrOneFromSplitComparison()
    {
        WebpVp8BoolDecoder zero = new WebpVp8BoolDecoder(new byte[] { 0x7F, 0xFF });
        WebpVp8BoolDecoder one = new WebpVp8BoolDecoder(new byte[] { 0x80, 0x00 });

        Assert.That(zero.DecodeBool(128), Is.EqualTo(0));
        Assert.That(zero.Range, Is.EqualTo(128));
        Assert.That(zero.Value, Is.EqualTo(0x7FFFU));
        Assert.That(one.DecodeBool(128), Is.EqualTo(1));
        Assert.That(one.Range, Is.EqualTo(254));
        Assert.That(one.Value, Is.EqualTo(0U));
        Assert.That(one.BitCount, Is.EqualTo(1));
    }

    [Test]
    public void DecodeBool_RenormalizesAndLoadsNextByteWhenBitCountWraps()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0x00, 0x00, 0x5A });

        Assert.That(decoder.DecodeBool(1), Is.EqualTo(0));
        Assert.That(decoder.DecodeBool(1), Is.EqualTo(0));

        Assert.That(decoder.Value, Is.EqualTo(0x1680U));
        Assert.That(decoder.BitCount, Is.EqualTo(6));
        Assert.That(decoder.BytePosition(), Is.EqualTo(3));
        Assert.That(decoder.IsTruncated, Is.False);
    }

    [Test]
    public void DecodeBool_TracksTruncatedRenormalizationAsZeroPaddedInput()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0x00, 0x00 });

        decoder.DecodeBool(1);
        decoder.DecodeBool(1);

        Assert.That(decoder.IsTruncated, Is.True);
        Assert.That(decoder.Value, Is.EqualTo(0U));
    }

    [Test]
    public void DecodeBitAndLiteral_ReadBitsHighToLow()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0x80, 0x00, 0x00 });

        Assert.That(decoder.DecodeLiteral(4), Is.EqualTo(8U));
    }

    [Test]
    public void DecodeSignedLiteral_ExtendsSignFromFirstBit()
    {
        WebpVp8BoolDecoder negative = new WebpVp8BoolDecoder(new byte[] { 0x80, 0x00, 0x00 });
        WebpVp8BoolDecoder positive = new WebpVp8BoolDecoder(new byte[] { 0x00, 0x00, 0x00 });

        Assert.That(negative.DecodeSignedLiteral(3), Is.EqualTo(-4));
        Assert.That(positive.DecodeSignedLiteral(3), Is.EqualTo(0));
        Assert.That(positive.DecodeSignedLiteral(0), Is.EqualTo(0));
    }

    [Test]
    public void DecodeTree_WalksVp8StyleBranchPairs()
    {
        int[] tree = new int[] { 2, -2, 0, -1 };
        byte[] probabilities = new byte[] { 128, 128 };
        WebpVp8BoolDecoder zero = new WebpVp8BoolDecoder(new byte[] { 0x00, 0x00, 0x00 });
        WebpVp8BoolDecoder one = new WebpVp8BoolDecoder(new byte[] { 0x40, 0x00, 0x00 });
        WebpVp8BoolDecoder two = new WebpVp8BoolDecoder(new byte[] { 0x80, 0x00, 0x00 });

        Assert.That(zero.DecodeTree(tree, probabilities), Is.EqualTo(0));
        Assert.That(one.DecodeTree(tree, probabilities), Is.EqualTo(1));
        Assert.That(two.DecodeTree(tree, probabilities), Is.EqualTo(2));
    }

    [Test]
    public void Decoder_RejectsInvalidInputs()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0, 0 });

        Assert.Throws<ArgumentException>(() => new WebpVp8BoolDecoder(new byte[] { 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeBool(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeBool(256));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeLiteral(33));
        Assert.Throws<ArgumentException>(() => decoder.DecodeTree(new int[] { 0 }, new byte[] { 128 }));
        Assert.Throws<ArgumentException>(() => decoder.DecodeTree(new int[] { 2, -1, 0, -2 }, new byte[] { 128 }));
    }
}
