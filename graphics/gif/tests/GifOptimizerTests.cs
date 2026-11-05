using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifOptimizerTests
{
    [Test]
    public void Optimize_DefaultModeCopiesAnimationAndCalculatesDuration()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0)
        };

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, null!);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OptimizationMode, Is.EqualTo(GifOptimizationMode.None));
        Assert.That(result.Animation, Is.Not.SameAs(animation));
        Assert.That(result.Animation.Frames[0], Is.Not.SameAs(animation.Frames[0]));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(10));
    }

    [Test]
    public void Optimize_DeconstructModeUsesSubRectangleOptimizer()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(2, 1, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }, 10, 0),
            CreateFrame(2, 1, new byte[] { 0, 0, 0, 255, 9, 9, 9, 255 }, 20, 1)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = GifOptimizationMode.Deconstruct;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OptimizationMode, Is.EqualTo(GifOptimizationMode.Deconstruct));
        AssertFrameRectangle(result.Animation.Frames[1], 1, 0, 1, 1);
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 9, 9, 9, 255 }));
    }

    [Test]
    public void Optimize_RemoveDuplicateFramesModeUsesDuplicateRemoval()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = GifOptimizationMode.RemoveDuplicateFrames;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OptimizationMode, Is.EqualTo(GifOptimizationMode.RemoveDuplicateFrames));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void Optimize_RemoveZeroDelayFramesModeUsesZeroDelayRemoval()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 0, 0),
            CreateFrame(new byte[] { 9, 9, 9, 255 }, 20, 1)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = GifOptimizationMode.RemoveZeroDelayFrames;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OptimizationMode, Is.EqualTo(GifOptimizationMode.RemoveZeroDelayFrames));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void Optimize_RejectsUnsupportedOptimizationMode()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = (GifOptimizationMode)99;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.OptimizationMode, Is.EqualTo((GifOptimizationMode)99));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Optimize_CombinesZeroDelayRemovalNormalizationAndTimelineRecalculation()
    {
        GifAnimation animation = new GifAnimation();
        animation.TotalDurationMilliseconds = 999;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 0, 0),
            CreateFrame(new byte[] { 9, 8, 7, 255 }, 0, 1)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.RemoveZeroDelayFrames = true;
        options.NormalizeFrameDelays = true;
        options.MinimumFrameDelayMilliseconds = 25;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(25));
        Assert.That(result.Animation.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(999));
    }

    [Test]
    public void Optimize_CanLeaveStoredTimelineUnchanged()
    {
        GifAnimation animation = new GifAnimation();
        animation.TotalDurationMilliseconds = 777;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.RemoveDuplicateFrames = true;
        options.RecalculateTimeline = false;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(777));
    }

    [Test]
    public void Optimize_RejectsNegativeNormalizationDelayWithoutMutatingSource()
    {
        GifAnimation animation = new GifAnimation();
        animation.TotalDurationMilliseconds = 12;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 0, 0)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.NormalizeFrameDelays = true;
        options.MinimumFrameDelayMilliseconds = -1;

        GifOptimizationResult result = new GifOptimizer().OptimizeWithOptions(animation, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(12));
    }

    [Test]
    public void Deconstruct_CropsFramesToChangedBoundsAndSetsSourceRectangle()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 3;
        animation.Height = 2;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255
            }, 10, 0),
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                9, 9, 9, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                8, 8, 8, 255
            }, 20, 1)
        };

        GifDeconstructResult result = new GifOptimizer().Deconstruct(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Width, Is.EqualTo(3));
        Assert.That(result.Animation.Height, Is.EqualTo(2));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(30));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(2));
        AssertFrameRectangle(result.Animation.Frames[0], 0, 0, 3, 2);
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(animation.Frames[0].Bitmap.RgbaPixels));
        AssertFrameRectangle(result.Animation.Frames[1], 1, 0, 2, 2);
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            0, 0, 0, 255,
            0, 0, 0, 255,
            8, 8, 8, 255
        }));
        AssertBounds(result.Bounds[1], true, 1, 0, 2, 2, 1);
    }

    [Test]
    public void Deconstruct_UsesTransparentPlaceholderForUnchangedFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1)
        };

        GifDeconstructResult result = new GifOptimizer().Deconstruct(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(2));
        AssertFrameRectangle(result.Animation.Frames[1], 0, 0, 1, 1);
        Assert.That(result.Animation.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(30));
        Assert.That(result.Bounds[1].HasChanges, Is.False);
    }

    [Test]
    public void Deconstruct_DoesNotMutateSourceFramesOrBitmaps()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0)
        };

        GifDeconstructResult result = new GifOptimizer().Deconstruct(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0], Is.Not.SameAs(animation.Frames[0]));
        Assert.That(result.Animation.Frames[0].Bitmap, Is.Not.SameAs(animation.Frames[0].Bitmap));
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.Not.SameAs(animation.Frames[0].Bitmap.RgbaPixels));

        result.Animation.Frames[0].Bitmap.RgbaPixels[0] = 99;

        Assert.That(animation.Frames[0].Bitmap.RgbaPixels[0], Is.EqualTo(1));
    }

    [Test]
    public void Deconstruct_RejectsMismatchedCoalescedFrameDimensions()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }),
            CreateFrame(2, 1, new byte[] { 1, 2, 3, 255, 1, 2, 3, 255 }, 0, 1)
        };

        GifDeconstructResult result = new GifOptimizer().Deconstruct(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void DetectChangedBounds_FirstFrameIsFullCanvasAndNextFrameIsTightBounds()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255
            }, 0, 0),
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                9, 9, 9, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                8, 8, 8, 255
            }, 0, 1)
        };

        GifChangedBoundsResult result = new GifOptimizer().DetectChangedBounds(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bounds.Length, Is.EqualTo(2));
        AssertBounds(result.Bounds[0], true, 0, 0, 3, 2, 0);
        AssertBounds(result.Bounds[1], true, 1, 0, 2, 2, 1);
    }

    [Test]
    public void DetectChangedBounds_ReportsEmptyBoundsForUnchangedFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }),
            CreateFrame(new byte[] { 1, 2, 3, 255 })
        };

        GifChangedBoundsResult result = new GifOptimizer().DetectChangedBounds(animation);

        Assert.That(result.Success, Is.True);
        AssertBounds(result.Bounds[1], false, 0, 0, 0, 0, 1);
    }

    [Test]
    public void DetectChangedBounds_TreatsAlphaChangesAsPixelChanges()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }),
            CreateFrame(new byte[] { 1, 2, 3, 0 })
        };

        GifChangedBoundsResult result = new GifOptimizer().DetectChangedBounds(animation);

        Assert.That(result.Success, Is.True);
        AssertBounds(result.Bounds[1], true, 0, 0, 1, 1, 1);
    }

    [Test]
    public void DetectChangedBounds_RejectsMismatchedCoalescedFrameDimensions()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }),
            CreateFrame(2, 1, new byte[] { 1, 2, 3, 255, 1, 2, 3, 255 }, 0, 1)
        };

        GifChangedBoundsResult result = new GifOptimizer().DetectChangedBounds(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void RemoveZeroDelayFrames_RemovesZeroDelayFramesAndPreservesDuration()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.LoopCount = 4;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 50, 0),
            CreateFrame(new byte[] { 4, 5, 6, 255 }, 0, 1),
            CreateFrame(new byte[] { 7, 8, 9, 255 }, 20, 2)
        };

        GifZeroDelayFrameRemovalResult result = new GifOptimizer().RemoveZeroDelayFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.RemovedFrameIndices, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Animation.Width, Is.EqualTo(1));
        Assert.That(result.Animation.Height, Is.EqualTo(1));
        Assert.That(result.Animation.LoopCount, Is.EqualTo(4));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(2));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(0));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(50));
        Assert.That(result.Animation.Frames[1].FrameIndex, Is.EqualTo(2));
        Assert.That(result.Animation.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(70));
    }

    [Test]
    public void RemoveZeroDelayFrames_RemovesLeadingZeroDelayFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 0, 0),
            CreateFrame(new byte[] { 9, 9, 9, 255 }, 30, 1)
        };

        GifZeroDelayFrameRemovalResult result = new GifOptimizer().RemoveZeroDelayFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedFrameIndices, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void RemoveZeroDelayFrames_KeepsLastFrameWhenAllDelaysAreZero()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 0, 0),
            CreateFrame(new byte[] { 9, 9, 9, 255 }, 0, 1)
        };

        GifZeroDelayFrameRemovalResult result = new GifOptimizer().RemoveZeroDelayFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.RemovedFrameIndices, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(0));
    }

    [Test]
    public void RemoveZeroDelayFrames_DoesNotMutateSourceFramesOrBitmaps()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0)
        };

        GifZeroDelayFrameRemovalResult result = new GifOptimizer().RemoveZeroDelayFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0], Is.Not.SameAs(animation.Frames[0]));
        Assert.That(result.Animation.Frames[0].Bitmap, Is.Not.SameAs(animation.Frames[0].Bitmap));
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.Not.SameAs(animation.Frames[0].Bitmap.RgbaPixels));

        result.Animation.Frames[0].Bitmap.RgbaPixels[0] = 99;

        Assert.That(animation.Frames[0].Bitmap.RgbaPixels[0], Is.EqualTo(1));
    }

    [Test]
    public void RemoveZeroDelayFrames_RejectsNegativeDelay()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, -1, 0)
        };

        GifZeroDelayFrameRemovalResult result = new GifOptimizer().RemoveZeroDelayFrames(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void RemoveDuplicateFrames_RemovesConsecutiveDuplicatesAndMergesDelay()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.LoopCount = 3;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 50, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 70, 1),
            CreateFrame(new byte[] { 9, 9, 9, 255 }, 30, 2)
        };

        GifDuplicateFrameRemovalResult result = new GifOptimizer().RemoveDuplicateFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.RemovedFrameIndices, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Animation.Width, Is.EqualTo(1));
        Assert.That(result.Animation.Height, Is.EqualTo(1));
        Assert.That(result.Animation.LoopCount, Is.EqualTo(3));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(2));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(120));
        Assert.That(result.Animation.Frames[0].FrameIndex, Is.EqualTo(0));
        Assert.That(result.Animation.Frames[1].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(result.Animation.Frames[1].FrameIndex, Is.EqualTo(2));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(150));
    }

    [Test]
    public void RemoveDuplicateFrames_MergesDuplicateRunsIntoFirstFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 30, 2)
        };

        GifDuplicateFrameRemovalResult result = new GifOptimizer().RemoveDuplicateFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(2));
        Assert.That(result.RemovedFrameIndices, Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(60));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(60));
    }

    [Test]
    public void RemoveDuplicateFrames_DoesNotMutateSourceFramesOrBitmaps()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1)
        };

        GifDuplicateFrameRemovalResult result = new GifOptimizer().RemoveDuplicateFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(result.Animation.Frames[0], Is.Not.SameAs(animation.Frames[0]));
        Assert.That(result.Animation.Frames[0].Bitmap, Is.Not.SameAs(animation.Frames[0].Bitmap));
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.Not.SameAs(animation.Frames[0].Bitmap.RgbaPixels));

        result.Animation.Frames[0].Bitmap.RgbaPixels[0] = 99;

        Assert.That(animation.Frames[0].Bitmap.RgbaPixels[0], Is.EqualTo(1));
    }

    [Test]
    public void RemoveDuplicateFrames_RejectsMergedDelayOverflow()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 2147483640, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 1)
        };

        GifDuplicateFrameRemovalResult result = new GifOptimizer().RemoveDuplicateFrames(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void DetectDuplicateFrames_MarksConsecutiveDuplicateFrames()
    {
        GifAnimationFrame first = CreateFrame(new byte[] { 1, 2, 3, 255 });
        GifAnimationFrame second = CreateFrame(new byte[] { 1, 2, 3, 255 });
        GifAnimationFrame third = CreateFrame(new byte[] { 9, 9, 9, 255 });
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[] { first, second, third };

        GifDuplicateFrameDetectionResult result = new GifOptimizer().DetectDuplicateFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(result.DuplicateFrames, Is.EqualTo(new bool[] { false, true, false }));
        Assert.That(result.DuplicateFrameIndices, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void DetectDuplicateFrames_DoesNotMarkNonConsecutiveDuplicates()
    {
        GifAnimationFrame first = CreateFrame(new byte[] { 1, 2, 3, 255 });
        GifAnimationFrame second = CreateFrame(new byte[] { 9, 9, 9, 255 });
        GifAnimationFrame third = CreateFrame(new byte[] { 1, 2, 3, 255 });
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[] { first, second, third };

        GifDuplicateFrameDetectionResult result = new GifOptimizer().DetectDuplicateFrames(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DuplicateCount, Is.EqualTo(0));
        Assert.That(result.DuplicateFrames, Is.EqualTo(new bool[] { false, false, false }));
        Assert.That(result.DuplicateFrameIndices, Is.Empty);
    }

    [Test]
    public void AreDuplicateBitmaps_RequiresSameDimensionsAndPixelBytes()
    {
        GifOptimizer optimizer = new GifOptimizer();
        GifBitmap first = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifBitmap same = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifBitmap differentPixel = new GifBitmap(1, 1, new byte[] { 1, 2, 4, 255 });
        GifBitmap differentSize = new GifBitmap(2, 1, new byte[] { 1, 2, 3, 255, 1, 2, 3, 255 });

        Assert.That(optimizer.AreDuplicateBitmaps(first, same), Is.True);
        Assert.That(optimizer.AreDuplicateBitmaps(first, differentPixel), Is.False);
        Assert.That(optimizer.AreDuplicateBitmaps(first, differentSize), Is.False);
    }

    [Test]
    public void DetectDuplicateFrames_RejectsInvalidFrameBitmap()
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(1, 1, new byte[] { 1, 2, 3 });
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[] { frame };

        GifDuplicateFrameDetectionResult result = new GifOptimizer().DetectDuplicateFrames(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void DetectDuplicateFrames_AllowsEmptyAnimation()
    {
        GifDuplicateFrameDetectionResult result = new GifOptimizer().DetectDuplicateFrames(new GifAnimation());

        Assert.That(result.Success, Is.True);
        Assert.That(result.DuplicateCount, Is.EqualTo(0));
        Assert.That(result.DuplicateFrames, Is.Empty);
        Assert.That(result.DuplicateFrameIndices, Is.Empty);
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels)
    {
        return CreateFrame(rgbaPixels, 0, -1);
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels, int delayMilliseconds, int frameIndex)
    {
        return CreateFrame(1, 1, rgbaPixels, delayMilliseconds, frameIndex);
    }

    static GifAnimationFrame CreateFrame(int width, int height, byte[] rgbaPixels, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(width, height, rgbaPixels);
        frame.DelayMilliseconds = delayMilliseconds;
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static void AssertBounds(GifChangedBounds bounds, bool hasChanges, int left, int top, int width, int height, int frameIndex)
    {
        Assert.That(bounds.HasChanges, Is.EqualTo(hasChanges));
        Assert.That(bounds.Left, Is.EqualTo(left));
        Assert.That(bounds.Top, Is.EqualTo(top));
        Assert.That(bounds.Width, Is.EqualTo(width));
        Assert.That(bounds.Height, Is.EqualTo(height));
        Assert.That(bounds.FrameIndex, Is.EqualTo(frameIndex));
    }

    static void AssertFrameRectangle(GifAnimationFrame frame, int left, int top, int width, int height)
    {
        Assert.That(frame.SourceLeft, Is.EqualTo(left));
        Assert.That(frame.SourceTop, Is.EqualTo(top));
        Assert.That(frame.SourceWidth, Is.EqualTo(width));
        Assert.That(frame.SourceHeight, Is.EqualTo(height));
        Assert.That(frame.Bitmap.Width, Is.EqualTo(width));
        Assert.That(frame.Bitmap.Height, Is.EqualTo(height));
    }
}
