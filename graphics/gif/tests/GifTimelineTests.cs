using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifTimelineTests
{
    [Test]
    public void Calculate_FromAnimationFramesBuildsStartEndAndTotalDuration()
    {
        GifAnimationFrame first = CreateAnimationFrame(50);
        GifAnimationFrame second = CreateAnimationFrame(70);
        GifAnimationFrame third = CreateAnimationFrame(0);

        GifTimelineResult result = new GifTimeline().CalculateFrames(new GifAnimationFrame[] { first, second, third });

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameStartMilliseconds, Is.EqualTo(new int[] { 0, 50, 120 }));
        Assert.That(result.FrameEndMilliseconds, Is.EqualTo(new int[] { 50, 120, 120 }));
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(120));
    }

    [Test]
    public void Calculate_FromDocumentConvertsHundredthsToMilliseconds()
    {
        GifDocument document = new GifDocument();
        GifFrame first = new GifFrame(0, 0, 1, 1);
        first.DelayHundredths = 5;
        GifFrame second = new GifFrame(0, 0, 1, 1);
        second.DelayHundredths = 7;
        document.Frames = new GifFrame[] { first, second };

        GifTimelineResult result = new GifTimeline().CalculateDocument(document);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameStartMilliseconds, Is.EqualTo(new int[] { 0, 50 }));
        Assert.That(result.FrameEndMilliseconds, Is.EqualTo(new int[] { 50, 120 }));
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(120));
    }

    [Test]
    public void Calculate_WithOptionsNormalizesZeroDelayForDurationAndWarning()
    {
        GifAnimationFrame first = CreateAnimationFrame(0);
        GifAnimationFrame second = CreateAnimationFrame(30);
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 20;

        GifTimelineResult result = new GifTimeline().CalculateFramesWithOptions(new GifAnimationFrame[] { first, second }, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameStartMilliseconds, Is.EqualTo(new int[] { 0, 20 }));
        Assert.That(result.FrameEndMilliseconds, Is.EqualTo(new int[] { 20, 50 }));
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(50));
        Assert.That(result.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
        Assert.That(result.Warnings[0].Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_WithOptionsLeavesNonZeroDelayBelowMinimumUnchanged()
    {
        GifAnimationFrame frame = CreateAnimationFrame(10);
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 20;

        GifTimelineResult result = new GifTimeline().CalculateFramesWithOptions(new GifAnimationFrame[] { frame }, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(10));
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Calculate_FromDocumentCanNormalizeZeroHundredths()
    {
        GifDocument document = new GifDocument();
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1) };
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 25;

        GifTimelineResult result = new GifTimeline().CalculateDocumentWithOptions(document, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameStartMilliseconds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.FrameEndMilliseconds, Is.EqualTo(new int[] { 25 }));
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(25));
        Assert.That(result.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
    }

    [Test]
    public void Calculate_AllowsEmptyAnimationTimeline()
    {
        GifTimelineResult result = new GifTimeline().Calculate(new GifAnimation());

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameStartMilliseconds, Is.Empty);
        Assert.That(result.FrameEndMilliseconds, Is.Empty);
        Assert.That(result.TotalDurationMilliseconds, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_RejectsNegativeAnimationDelay()
    {
        GifAnimationFrame frame = CreateAnimationFrame(-1);

        GifTimelineResult result = new GifTimeline().CalculateFrames(new GifAnimationFrame[] { frame });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_RejectsNegativeMinimumDelayOption()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = -1;

        GifTimelineResult result = new GifTimeline().CalculateFramesWithOptions(new GifAnimationFrame[] { CreateAnimationFrame(0) }, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Calculate_RejectsOverflowingTimeline()
    {
        GifAnimationFrame first = CreateAnimationFrame(2147483640);
        GifAnimationFrame second = CreateAnimationFrame(10);

        GifTimelineResult result = new GifTimeline().CalculateFrames(new GifAnimationFrame[] { first, second });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    static GifAnimationFrame CreateAnimationFrame(int delayMilliseconds)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.DelayMilliseconds = delayMilliseconds;
        return frame;
    }
}
