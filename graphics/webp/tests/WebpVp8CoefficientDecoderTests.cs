using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8CoefficientDecoderTests
{
    [Test]
    public void DecodeBlock_ReadsSingleCoefficientThenEndOfBlock()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0b110, 3);
        writer.WriteBit(0);
        writer.WriteBit(0);

        WebpVp8CoefficientBlock block = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), new WebpVp8CoefficientProbabilityTable(), WebpVp8CoefficientDecoder.PlaneYWithoutY2, 0, 0);

        Assert.That(block.HasCoefficients, Is.True);
        Assert.That(block.Coefficients[0], Is.EqualTo(1));
        Assert.That(block.Coefficients[1], Is.EqualTo(0));
        Assert.That(block.LastDecodedCoefficient, Is.EqualTo(0));
        Assert.That(block.EndingContext, Is.EqualTo(1));
    }

    [Test]
    public void DecodeBlock_SkipsEobBranchAfterZeroToken()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0b10, 2);
        writer.WriteLiteral(0b1100, 4);
        writer.WriteBit(0);
        writer.WriteBit(0);

        WebpVp8CoefficientBlock block = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), new WebpVp8CoefficientProbabilityTable(), WebpVp8CoefficientDecoder.PlaneYWithoutY2, 0, 0);

        Assert.That(block.HasCoefficients, Is.True);
        Assert.That(block.Coefficients[0], Is.EqualTo(0));
        Assert.That(block.Coefficients[1], Is.EqualTo(2));
        Assert.That(block.LastDecodedCoefficient, Is.EqualTo(1));
    }

    [Test]
    public void DecodeBlock_ReadsCategoryExtraBitsAndSign()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0b111100, 6);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteBit(0);

        WebpVp8CoefficientBlock block = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), new WebpVp8CoefficientProbabilityTable(), WebpVp8CoefficientDecoder.PlaneYWithoutY2, 0, 0);

        Assert.That(block.HasCoefficients, Is.True);
        Assert.That(block.Coefficients[0], Is.EqualTo(-5));
    }

    [Test]
    public void DecodeBlock_RespectsStartCoefficientForYBlocksWithY2()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0b110, 3);
        writer.WriteBit(0);
        writer.WriteBit(0);

        WebpVp8CoefficientBlock block = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), new WebpVp8CoefficientProbabilityTable(), WebpVp8CoefficientDecoder.PlaneYWithY2, 1, 0);

        Assert.That(block.Coefficients[0], Is.EqualTo(0));
        Assert.That(block.Coefficients[1], Is.EqualTo(1));
        Assert.That(block.LastDecodedCoefficient, Is.EqualTo(1));
    }

    [Test]
    public void ProbabilityTable_SetProbabilityUsesVp8FlattenedIndex()
    {
        WebpVp8CoefficientProbabilityTable table = new WebpVp8CoefficientProbabilityTable();
        int index = WebpVp8CoefficientDecoder.ProbabilityIndex(WebpVp8CoefficientDecoder.PlaneUv, 6, 2, 5);

        table.SetProbability(WebpVp8CoefficientDecoder.PlaneUv, 6, 2, 5, 77);

        Assert.That(table.Values[index], Is.EqualTo(77));
        Assert.That(table.GetProbability(WebpVp8CoefficientDecoder.PlaneUv, 6, 2, 5), Is.EqualTo(77));
    }

    [Test]
    public void Decoder_RejectsInvalidInputs()
    {
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0, 0 });
        WebpVp8CoefficientProbabilityTable table = new WebpVp8CoefficientProbabilityTable();

        Assert.Throws<ArgumentNullException>(() => WebpVp8CoefficientDecoder.DecodeBlock(null!, table, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => WebpVp8CoefficientDecoder.DecodeBlock(decoder, null!, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.DecodeBlock(decoder, table, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.DecodeBlock(decoder, table, 0, 16, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.DecodeBlock(decoder, table, 0, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetProbability(0, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.ProbabilityIndex(4, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.ProbabilityIndex(0, 8, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.ProbabilityIndex(0, 0, 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientDecoder.ProbabilityIndex(0, 0, 0, 11));
    }
}
