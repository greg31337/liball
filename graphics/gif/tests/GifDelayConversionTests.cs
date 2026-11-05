using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifDelayConversionTests
{
    [TestCase(0, 0)]
    [TestCase(1, 10)]
    [TestCase(100, 1000)]
    [TestCase(65535, 655350)]
    public void ConvertHundredthsToMilliseconds_ReturnsExactDelay(int delayHundredths, int expectedMilliseconds)
    {
        IGifTransformer transformer = new GifCodec();

        int result = transformer.ConvertDelayHundredthsToMilliseconds(delayHundredths);

        Assert.That(result, Is.EqualTo(expectedMilliseconds));
    }

    [TestCase(10, GifDelayRoundingMode.Floor, 1)]
    [TestCase(14, GifDelayRoundingMode.Floor, 1)]
    [TestCase(14, GifDelayRoundingMode.Nearest, 1)]
    [TestCase(15, GifDelayRoundingMode.Nearest, 2)]
    [TestCase(16, GifDelayRoundingMode.Nearest, 2)]
    [TestCase(11, GifDelayRoundingMode.Ceiling, 2)]
    [TestCase(1, GifDelayRoundingMode.Floor, 0)]
    [TestCase(1, GifDelayRoundingMode.Nearest, 0)]
    [TestCase(1, GifDelayRoundingMode.Ceiling, 1)]
    public void ConvertMillisecondsToHundredths_UsesSelectedRounding(int delayMilliseconds, GifDelayRoundingMode roundingMode, int expectedHundredths)
    {
        IGifTransformer transformer = new GifCodec();

        int result = transformer.ConvertDelayMillisecondsToHundredths(delayMilliseconds, roundingMode);

        Assert.That(result, Is.EqualTo(expectedHundredths));
    }

    [TestCase(655359, GifDelayRoundingMode.Floor)]
    [TestCase(655354, GifDelayRoundingMode.Nearest)]
    [TestCase(655350, GifDelayRoundingMode.Ceiling)]
    public void ConvertMillisecondsToHundredths_AcceptsModeSpecificMaximum(int delayMilliseconds, GifDelayRoundingMode roundingMode)
    {
        int result = new GifCodec().ConvertDelayMillisecondsToHundredths(delayMilliseconds, roundingMode);

        Assert.That(result, Is.EqualTo(65535));
    }

    [TestCase(655360, GifDelayRoundingMode.Floor)]
    [TestCase(655355, GifDelayRoundingMode.Nearest)]
    [TestCase(655351, GifDelayRoundingMode.Ceiling)]
    [TestCase(2147483647, GifDelayRoundingMode.Floor)]
    public void ConvertMillisecondsToHundredths_RejectsRoundedValuesOutsideGifRange(int delayMilliseconds, GifDelayRoundingMode roundingMode)
    {
        GifException exception = Assert.Throws<GifException>(() => new GifCodec().ConvertDelayMillisecondsToHundredths(delayMilliseconds, roundingMode));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(exception.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(exception.Message, Does.Contain("16-bit"));
    }

    [Test]
    public void Conversion_RejectsNegativeInputsAndUnsupportedRoundingMode()
    {
        GifCodec codec = new GifCodec();

        GifException negativeHundredths = Assert.Throws<GifException>(() => codec.ConvertDelayHundredthsToMilliseconds(-1));
        GifException negativeMilliseconds = Assert.Throws<GifException>(() => codec.ConvertDelayMillisecondsToHundredths(-1, GifDelayRoundingMode.Floor));
        GifException invalidMode = Assert.Throws<GifException>(() => codec.ConvertDelayMillisecondsToHundredths(10, (GifDelayRoundingMode)99));

        Assert.That(negativeHundredths.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(negativeHundredths.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(negativeMilliseconds.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(negativeMilliseconds.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(invalidMode.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(invalidMode.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(invalidMode.Message, Does.Contain("rounding mode"));
    }

    [Test]
    public void ConvertHundredthsToMilliseconds_DistinguishesRangeAndOverflowFailures()
    {
        GifCodec codec = new GifCodec();

        GifException range = Assert.Throws<GifException>(() => codec.ConvertDelayHundredthsToMilliseconds(65536));
        GifException overflow = Assert.Throws<GifException>(() => codec.ConvertDelayHundredthsToMilliseconds(214748365));

        Assert.That(range.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(range.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(range.Message, Does.Contain("16-bit"));
        Assert.That(overflow.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(overflow.BlockName, Is.EqualTo("DelayConversion"));
        Assert.That(overflow.Message, Does.Contain("overflow"));
    }
}
