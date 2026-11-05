using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8IntraModeParserTests
{
    [Test]
    public void ParseKeyFrameModes_ReadsBPredSubblockAndUvModesForEachMacroblock()
    {
        byte[] data = new byte[16];
        WebpVp8SegmentationHeader segmentation = new WebpVp8SegmentationHeader();

        WebpVp8IntraModeParseResult result = WebpVp8IntraModeParser.ParseKeyFrameModes(new WebpVp8BoolDecoder(data), 2, 1, segmentation, false, 128);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Macroblocks.Count, Is.EqualTo(2));
        Assert.That(result.Macroblocks[0].MacroblockX, Is.EqualTo(0));
        Assert.That(result.Macroblocks[1].MacroblockX, Is.EqualTo(1));
        Assert.That(result.Macroblocks[0].YMode, Is.EqualTo(WebpVp8IntraModeParser.BPred));
        Assert.That(result.Macroblocks[1].YMode, Is.EqualTo(WebpVp8IntraModeParser.BPred));
        Assert.That(result.Macroblocks[0].UvMode, Is.EqualTo(WebpVp8IntraModeParser.DcPred));
        Assert.That(result.Macroblocks[1].UvMode, Is.EqualTo(WebpVp8IntraModeParser.DcPred));
        Assert.That(result.Macroblocks[0].YSubblockModes, Is.EqualTo(NewRepeatedModes(WebpVp8IntraModeParser.BDcPred)));
        Assert.That(result.Macroblocks[1].YSubblockModes, Is.EqualTo(NewRepeatedModes(WebpVp8IntraModeParser.BDcPred)));
    }

    [Test]
    public void ParseKeyFrameModes_ReadsSegmentIdAndSkipCoeffBeforeIntraModes()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteBit(1);
        writer.WriteBit(1);
        writer.WriteBit(0);
        writer.WriteBit(0);
        int index = 0;
        while (index < 16)
        {
            writer.WriteBit(0);
            index = index + 1;
        }

        writer.WriteBit(0);
        WebpVp8SegmentationHeader segmentation = new WebpVp8SegmentationHeader();
        segmentation.UpdateMap = true;
        segmentation.SegmentTreeProbabilities = new byte[] { 128, 128, 128 };

        WebpVp8IntraModeParseResult result = WebpVp8IntraModeParser.ParseKeyFrameModes(new WebpVp8BoolDecoder(writer.ToBytes()), 1, 1, segmentation, true, 128);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Macroblocks[0].SegmentId, Is.EqualTo(3));
        Assert.That(result.Macroblocks[0].SkipCoefficients, Is.False);
        Assert.That(result.Macroblocks[0].YMode, Is.EqualTo(WebpVp8IntraModeParser.BPred));
        Assert.That(result.Macroblocks[0].UvMode, Is.EqualTo(WebpVp8IntraModeParser.DcPred));
    }

    [Test]
    public void ParseKeyFrameModesForDimensions_RoundsDimensionsToMacroblockGrid()
    {
        byte[] data = new byte[32];
        WebpVp8SegmentationHeader segmentation = new WebpVp8SegmentationHeader();

        WebpVp8IntraModeParseResult result = WebpVp8IntraModeParser.ParseKeyFrameModesForDimensions(new WebpVp8BoolDecoder(data), 17, 16, segmentation, false, 128);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Macroblocks.Count, Is.EqualTo(2));
        Assert.That(result.Macroblocks[1].MacroblockX, Is.EqualTo(1));
    }

    [Test]
    public void ParseKeyFrameModes_ReportsTruncatedModeData()
    {
        WebpVp8SegmentationHeader segmentation = new WebpVp8SegmentationHeader();

        WebpVp8IntraModeParseResult result = WebpVp8IntraModeParser.ParseKeyFrameModes(new WebpVp8BoolDecoder(new byte[] { 0, 0 }), 2, 2, segmentation, false, 128);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void ParseKeyFrameModes_RejectsInvalidInputs()
    {
        WebpVp8SegmentationHeader segmentation = new WebpVp8SegmentationHeader();
        WebpVp8BoolDecoder decoder = new WebpVp8BoolDecoder(new byte[] { 0, 0 });

        Assert.Throws<ArgumentNullException>(() => WebpVp8IntraModeParser.ParseKeyFrameModes(null!, 1, 1, segmentation, false, 128));
        Assert.Throws<ArgumentNullException>(() => WebpVp8IntraModeParser.ParseKeyFrameModes(decoder, 1, 1, null!, false, 128));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraModeParser.ParseKeyFrameModes(decoder, 0, 1, segmentation, false, 128));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraModeParser.ParseKeyFrameModes(decoder, 1, 0, segmentation, false, 128));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraModeParser.ParseKeyFrameModes(decoder, 1, 1, segmentation, true, 0));
    }

    static int[] NewRepeatedModes(int mode)
    {
        int[] modes = new int[16];
        int index = 0;
        while (index < modes.Length)
        {
            modes[index] = mode;
            index = index + 1;
        }

        return modes;
    }
}
