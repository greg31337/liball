using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8IntraPredictionSearchTests
{
    [Test]
    public void SearchLuma16X16_SelectsVerticalPredictionForTopMatchingBlock()
    {
        byte[] top = NewRamp(16, 20);
        byte[] left = NewFilled(16, 200);
        byte[] source = NewBlockFromTop(top, 16);

        WebpVp8IntraPredictionChoice choice = WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 16, top, left, 127);

        Assert.That(choice.Mode, Is.EqualTo(WebpVp8IntraModeParser.VPred));
        Assert.That(choice.Score, Is.EqualTo(0));
    }

    [Test]
    public void SearchLuma16X16_SelectsHorizontalPredictionForLeftMatchingBlock()
    {
        byte[] top = NewFilled(16, 10);
        byte[] left = NewRamp(16, 30);
        byte[] source = NewBlockFromLeft(left, 16);

        WebpVp8IntraPredictionChoice choice = WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 16, top, left, 127);

        Assert.That(choice.Mode, Is.EqualTo(WebpVp8IntraModeParser.HPred));
        Assert.That(choice.Score, Is.EqualTo(0));
    }

    [Test]
    public void SearchLuma16X16_SelectsTrueMotionPrediction()
    {
        byte[] top = NewRamp(16, 90);
        byte[] left = NewRamp(16, 100);
        byte[] source = WebpVp8IntraPredictionSearch.PredictBlock(WebpVp8IntraModeParser.TmPred, top, left, 80, 16);

        WebpVp8IntraPredictionChoice choice = WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 16, top, left, 80);

        Assert.That(choice.Mode, Is.EqualTo(WebpVp8IntraModeParser.TmPred));
        Assert.That(choice.Score, Is.EqualTo(0));
    }

    [Test]
    public void SearchChroma8X8_SelectsBestModeAndScoresPrediction()
    {
        byte[] top = NewFilled(8, 70);
        byte[] left = NewRamp(8, 40);
        byte[] source = NewBlockFromLeft(left, 8);

        WebpVp8IntraPredictionChoice choice = WebpVp8IntraPredictionSearch.SearchChroma8X8(source, 0, 8, top, left, 64);
        byte[] prediction = WebpVp8IntraPredictionSearch.PredictBlock(choice.Mode, top, left, 64, 8);

        Assert.That(choice.Mode, Is.EqualTo(WebpVp8IntraModeParser.HPred));
        Assert.That(WebpVp8IntraPredictionSearch.ScoreBlock(source, 0, 8, prediction, 8), Is.EqualTo(0));
    }

    [Test]
    public void PredictBlock_DcUsesRoundedAverageAndTrueMotionClamps()
    {
        byte[] top = NewFilled(8, 10);
        byte[] left = NewFilled(8, 30);

        byte[] dc = WebpVp8IntraPredictionSearch.PredictBlock(WebpVp8IntraModeParser.DcPred, top, left, 0, 8);
        byte[] tm = WebpVp8IntraPredictionSearch.PredictBlock(WebpVp8IntraModeParser.TmPred, NewFilled(8, 250), NewFilled(8, 250), 0, 8);

        Assert.That(dc[0], Is.EqualTo(20));
        Assert.That(tm[0], Is.EqualTo(255));
    }

    [Test]
    public void IntraPredictionSearch_RejectsInvalidInputs()
    {
        byte[] source = new byte[16 * 16];
        byte[] top = NewFilled(16, 127);
        byte[] left = NewFilled(16, 127);

        Assert.Throws<ArgumentNullException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(null!, 0, 16, top, left, 127));
        Assert.Throws<ArgumentNullException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 16, null!, left, 127));
        Assert.Throws<ArgumentException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 16, new byte[15], left, 127));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(source, -1, 16, top, left, 127));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(source, 0, 15, top, left, 127));
        Assert.Throws<ArgumentException>(() => WebpVp8IntraPredictionSearch.SearchLuma16X16(new byte[10], 0, 16, top, left, 127));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraPredictionSearch.PredictBlock(9, top, left, 127, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8IntraPredictionSearch.PredictBlock(WebpVp8IntraModeParser.DcPred, top, left, 127, 4));
        Assert.Throws<ArgumentException>(() => WebpVp8IntraPredictionSearch.ScoreBlock(source, 0, 16, new byte[15], 16));
    }

    static byte[] NewBlockFromTop(byte[] top, int size)
    {
        byte[] block = new byte[size * size];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                block[(row * size) + column] = top[column];
                column = column + 1;
            }

            row = row + 1;
        }

        return block;
    }

    static byte[] NewBlockFromLeft(byte[] left, int size)
    {
        byte[] block = new byte[size * size];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                block[(row * size) + column] = left[row];
                column = column + 1;
            }

            row = row + 1;
        }

        return block;
    }

    static byte[] NewRamp(int length, int start)
    {
        byte[] values = new byte[length];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = (byte)(start + index);
            index = index + 1;
        }

        return values;
    }

    static byte[] NewFilled(int length, byte value)
    {
        byte[] values = new byte[length];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
    }
}
