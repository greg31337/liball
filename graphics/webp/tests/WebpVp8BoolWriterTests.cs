using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8BoolWriterTests
{
    [Test]
    public void WriteBool_RoundTripsThroughBoolDecoderWithMixedProbabilities()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        int[] probabilities = new int[] { 128, 200, 17, 240, 64, 129, 253, 2, 128 };
        int[] bits = new int[] { 1, 0, 1, 0, 1, 1, 0, 1, 0 };
        int index = 0;
        while (index < bits.Length)
        {
            writer.WriteBool(probabilities[index], bits[index]);
            index = index + 1;
        }

        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(writer.ToBytes());

        index = 0;
        while (index < bits.Length)
        {
            Assert.That(decoder.DecodeBool(probabilities[index]), Is.EqualTo(bits[index]));
            index = index + 1;
        }
    }

    [Test]
    public void WriteLiteral_RoundTripsThroughBoolDecoder()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        writer.WriteLiteral(0x5A, 8);
        writer.WriteLiteral(0x15, 5);

        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(writer.ToBytes());

        Assert.That(decoder.DecodeLiteral(8), Is.EqualTo(0x5A));
        Assert.That(decoder.DecodeLiteral(5), Is.EqualTo(0x15));
    }

    [Test]
    public void WriteBool_RejectsInvalidInputs()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBool(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBool(256, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteLiteral(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteLiteral(0, 32));
    }
}
