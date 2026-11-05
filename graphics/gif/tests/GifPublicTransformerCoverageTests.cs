using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicTransformerCoverageTests
{
    [Test]
    public void PublicTransformerSetsUniformAndPerFrameDelaysAndDisposals()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation animation = CreateAnimation();

        GifAnimation uniformDelay = transformer.SetDelay(animation, 25);
        Assert.That(uniformDelay, Is.SameAs(animation));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Frames[2].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(75));

        GifAnimation individualDelays = transformer.SetDelays(animation, new int[] { 10, 20, 30 });
        Assert.That(individualDelays, Is.SameAs(animation));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(animation.Frames[2].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(60));

        GifAnimation uniformDisposal = transformer.SetDisposal(animation, GifDisposalMethod.RestoreToBackground);
        Assert.That(uniformDisposal, Is.SameAs(animation));
        Assert.That(animation.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(animation.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));

        GifAnimation individualDisposals = transformer.SetDisposals(animation, new GifDisposalMethod[]
        {
            GifDisposalMethod.Unspecified,
            GifDisposalMethod.DoNotDispose,
            GifDisposalMethod.RestoreToPrevious
        });
        Assert.That(individualDisposals, Is.SameAs(animation));
        Assert.That(animation.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(animation.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
    }

    [Test]
    public void PublicTransformerSetsAnimationAndDocumentLoopCountsAndComments()
    {
        GifCodec codec = new GifCodec();
        IGifTransformer transformer = codec;
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifAnimation animation = CreateAnimation();
        GifDocument document = CreateDocument(codec);

        Assert.That(transformer.SetAnimationLoopCount(animation, 0), Is.SameAs(animation));
        Assert.That(animation.LoopCount, Is.EqualTo(0));
        Assert.That(transformer.SetDocumentLoopCount(document, 5), Is.SameAs(document));
        Assert.That(document.LoopCount, Is.EqualTo(5));
        Assert.That(document.Metadata.LoopCount, Is.EqualTo(5));

        transformer.SetCommentsFromText(document, new string[] { "first", "second" });
        Assert.That(document.Comments, Has.Length.EqualTo(2));
        Assert.That(document.Comments[0].Text, Is.EqualTo("first"));
        Assert.That(document.Comments[1].Text, Is.EqualTo("second"));
        GifCommentExtension replacement = new GifCommentExtension("replacement");
        Assert.That(transformer.SetComments(document, new GifCommentExtension[] { replacement }), Is.SameAs(document));
        Assert.That(document.Comments, Is.SameAs(document.Metadata.Comments));
        Assert.That(document.Comments[0], Is.SameAs(replacement));

        GifDocument roundTrip = decoder.DecodeBytes(encoder.Encode(document, null), null);
        Assert.That(roundTrip.LoopCount, Is.EqualTo(5));
        Assert.That(roundTrip.Comments, Has.Length.EqualTo(1));
        Assert.That(roundTrip.Comments[0].Text, Is.EqualTo("replacement"));

        transformer.SetCommentsFromText(document, null);
        transformer.SetDocumentLoopCount(document, -1);
        GifDocument cleared = decoder.DecodeBytes(encoder.Encode(document, null), null);
        Assert.That(cleared.LoopCount, Is.EqualTo(-1));
        Assert.That(cleared.Comments, Is.Empty);
    }

    [Test]
    public void PublicTransformerSwitchesBetweenGlobalAndLocalPalettes()
    {
        GifCodec codec = new GifCodec();
        IGifTransformer transformer = codec;
        GifDocument document = CreateDocument(codec);
        GifPalette global = CreateAlternatePalette();
        GifPalette first = CreatePalette();
        GifPalette second = CreateAlternatePalette();

        Assert.That(transformer.SetPalettes(document, new GifPalette[] { first, second }), Is.SameAs(document));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[0].LocalColorTable, Is.SameAs(first));
        Assert.That(document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[1].LocalColorTable, Is.SameAs(second));

        Assert.That(transformer.SetPalette(document, global), Is.SameAs(document));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.GlobalColorTable, Is.SameAs(global));
        Assert.That(document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(document.Frames[1].HasLocalColorTable, Is.False);
    }

    [Test]
    public void PublicTransformerLocalPaletteFailureIsAtomic()
    {
        GifCodec codec = new GifCodec();
        IGifTransformer transformer = codec;
        GifDocument document = CreateDocument(codec);
        GifPalette originalGlobal = document.GlobalColorTable;
        GifPalette invalid = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });

        GifException exception = Assert.Throws<GifException>(() => transformer.SetPalettes(document, new GifPalette[] { CreateAlternatePalette(), invalid }));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(exception.FrameIndex, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.LogicalScreen.GlobalColorTableSize, Is.EqualTo(originalGlobal.Count));
        Assert.That(document.GlobalColorTable, Is.SameAs(originalGlobal));
        Assert.That(document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(document.Frames[0].LocalColorTable.IsEmpty(), Is.True);
        Assert.That(document.Frames[1].HasLocalColorTable, Is.False);
        Assert.That(document.Frames[1].LocalColorTable.IsEmpty(), Is.True);
    }

    [Test]
    public void PublicCodecCoalescesAndDeconstructsWithoutMutatingSources()
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifTransformer transformer = codec;
        GifDocument document = decoder.DecodeBytes(GifFixtures.AnimatedDisposalMethod1Gif89a(), null);
        byte[] firstCompressed = CopyBytes(document.Frames[0].CompressedImageData);

        GifAnimation coalesced = decoder.Coalesce(document, null);
        GifAnimation deconstructed = transformer.Deconstruct(coalesced);

        Assert.That(coalesced.Frames, Has.Length.EqualTo(3));
        Assert.That(coalesced.Frames[0].Bitmap.Width, Is.EqualTo(coalesced.Width));
        Assert.That(coalesced.Frames[0].Bitmap.Height, Is.EqualTo(coalesced.Height));
        Assert.That(deconstructed.Frames, Has.Length.EqualTo(3));
        Assert.That(deconstructed.Frames[0].Bitmap.Width, Is.EqualTo(coalesced.Width));
        Assert.That(deconstructed.Frames[2].SourceLeft, Is.EqualTo(1));
        Assert.That(deconstructed.Frames[2].SourceWidth, Is.EqualTo(1));
        Assert.That(deconstructed.Frames[2].Bitmap.Width, Is.EqualTo(1));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(firstCompressed));
        Assert.That(coalesced.Frames[2].Bitmap.Width, Is.EqualTo(coalesced.Width));
    }

    [Test]
    public void PublicTransformerSupportsEveryOptimizationBehavior()
    {
        IGifTransformer transformer = new GifCodec();
        GifEncodeOptions noneOptions = new GifEncodeOptions();
        GifEncodeOptions deconstructOptions = new GifEncodeOptions();
        deconstructOptions.OptimizationMode = GifOptimizationMode.Deconstruct;
        GifEncodeOptions duplicateOptions = new GifEncodeOptions();
        duplicateOptions.OptimizationMode = GifOptimizationMode.RemoveDuplicateFrames;
        GifEncodeOptions zeroOptions = new GifEncodeOptions();
        zeroOptions.OptimizationMode = GifOptimizationMode.RemoveZeroDelayFrames;

        GifAnimation noneSource = CreateOptimizationAnimation();
        GifAnimation none = transformer.Optimize(noneSource, noneOptions);
        GifAnimation deconstructed = transformer.Optimize(CreateOptimizationAnimation(), deconstructOptions);
        GifAnimation duplicatesRemoved = transformer.Optimize(CreateOptimizationAnimation(), duplicateOptions);
        GifAnimation zerosRemoved = transformer.Optimize(CreateOptimizationAnimation(), zeroOptions);

        Assert.That(none.Frames, Has.Length.EqualTo(4));
        Assert.That(none, Is.Not.SameAs(noneSource));
        Assert.That(deconstructed.Frames, Has.Length.EqualTo(4));
        Assert.That(deconstructed.Frames[3].SourceLeft, Is.EqualTo(1));
        Assert.That(deconstructed.Frames[3].SourceWidth, Is.EqualTo(1));
        Assert.That(duplicatesRemoved.Frames, Has.Length.EqualTo(3));
        Assert.That(duplicatesRemoved.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(zerosRemoved.Frames, Has.Length.EqualTo(3));
        Assert.That(zerosRemoved.Frames[2].FrameIndex, Is.EqualTo(3));
    }

    [Test]
    public void PublicTransformerCombinesOptimizationsAndControlsTimelineRecalculation()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation source = CreateOptimizationAnimation();
        source.TotalDurationMilliseconds = 999;
        GifEncodeOptions options = new GifEncodeOptions();
        options.RemoveZeroDelayFrames = true;
        options.RemoveDuplicateFrames = true;
        options.NormalizeFrameDelays = true;
        options.MinimumFrameDelayMilliseconds = 25;
        options.DeconstructFrames = true;
        options.RecalculateTimeline = true;

        GifAnimation optimized = transformer.Optimize(source, options);

        Assert.That(optimized.Frames, Has.Length.EqualTo(2));
        Assert.That(optimized.Frames[0].FrameIndex, Is.EqualTo(0));
        Assert.That(optimized.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(optimized.Frames[1].FrameIndex, Is.EqualTo(3));
        Assert.That(optimized.Frames[1].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(optimized.Frames[1].SourceLeft, Is.EqualTo(1));
        Assert.That(optimized.Frames[1].SourceWidth, Is.EqualTo(1));
        Assert.That(optimized.TotalDurationMilliseconds, Is.EqualTo(60));
        Assert.That(source.Frames, Has.Length.EqualTo(4));
        Assert.That(source.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(source.TotalDurationMilliseconds, Is.EqualTo(999));

        GifEncodeOptions preserveTimelineOptions = new GifEncodeOptions();
        preserveTimelineOptions.RemoveDuplicateFrames = true;
        preserveTimelineOptions.RecalculateTimeline = false;
        GifAnimation preservedTimeline = transformer.Optimize(source, preserveTimelineOptions);
        Assert.That(preservedTimeline.Frames, Has.Length.EqualTo(3));
        Assert.That(preservedTimeline.TotalDurationMilliseconds, Is.EqualTo(999));
    }

    [Test]
    public void PublicTransformerRejectsNullInputs()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation missingDelays = CreateAnimation();
        GifAnimation missingDisposals = CreateAnimation();

        Assert.That(Assert.Throws<GifException>(() => transformer.Deconstruct(null)).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetDelay(null, 10)).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetDelays(null, new int[0])).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetDisposal(null, GifDisposalMethod.Unspecified)).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetDisposals(null, new GifDisposalMethod[0])).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetAnimationLoopCount(null, 0)).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetDocumentLoopCount(null, 0)).ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetCommentsFromText(null, null)).ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetComments(null, null)).ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetPalette(null, CreatePalette())).ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(Assert.Throws<GifException>(() => transformer.SetPalettes(null, new GifPalette[0])).ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(Assert.Throws<GifException>(() => transformer.Optimize(null, null)).ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));

        GifException delayError = Assert.Throws<GifException>(() => transformer.SetDelays(missingDelays, null));
        GifException disposalError = Assert.Throws<GifException>(() => transformer.SetDisposals(missingDisposals, null));

        Assert.That(delayError.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(delayError.BlockName, Is.EqualTo("Timeline"));
        Assert.That(missingDelays.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(missingDelays.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(disposalError.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(disposalError.BlockName, Is.EqualTo("GraphicControlExtension"));
        Assert.That(missingDisposals.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(missingDisposals.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
    }

    [Test]
    public void PublicTransformerInvalidValuesFailWithoutPartialMutation()
    {
        GifCodec codec = new GifCodec();
        IGifTransformer transformer = codec;
        GifAnimation delays = CreateAnimation();
        GifAnimation disposals = CreateAnimation();
        GifAnimation loop = CreateAnimation();
        GifDocument comments = CreateDocument(codec);
        GifDocument globalPalette = CreateDocument(codec);
        GifPalette originalGlobal = globalPalette.GlobalColorTable;

        GifException delayError = Assert.Throws<GifException>(() => transformer.SetDelays(delays, new int[] { 99, -1, 88 }));
        GifException disposalError = Assert.Throws<GifException>(() => transformer.SetDisposals(disposals, new GifDisposalMethod[]
        {
            GifDisposalMethod.RestoreToBackground,
            (GifDisposalMethod)99,
            GifDisposalMethod.RestoreToPrevious
        }));
        GifException loopError = Assert.Throws<GifException>(() => transformer.SetAnimationLoopCount(loop, -2));
        GifException commentError = Assert.Throws<GifException>(() => transformer.SetComments(comments, new GifCommentExtension[] { new GifCommentExtension("valid"), null! }));
        GifException paletteError = Assert.Throws<GifException>(() => transformer.SetPalette(globalPalette, new GifPalette(new GifColor[] { new GifColor(1, 2, 3) })));

        Assert.That(delayError.FrameIndex, Is.EqualTo(1));
        Assert.That(delays.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(delays.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(delays.Frames[2].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(delays.TotalDurationMilliseconds, Is.EqualTo(30));
        Assert.That(disposalError.FrameIndex, Is.EqualTo(1));
        Assert.That(disposals.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(disposals.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(loopError.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(loop.LoopCount, Is.EqualTo(-1));
        Assert.That(commentError.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(comments.Comments, Is.Empty);
        Assert.That(paletteError.ErrorCode, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(globalPalette.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(globalPalette.GlobalColorTable, Is.SameAs(originalGlobal));

        GifEncodeOptions invalidOptimization = new GifEncodeOptions();
        invalidOptimization.OptimizationMode = (GifOptimizationMode)99;
        GifAnimation optimizeSource = CreateAnimation();
        GifException optimizeError = Assert.Throws<GifException>(() => transformer.Optimize(optimizeSource, invalidOptimization));
        Assert.That(optimizeError.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(optimizeSource.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(optimizeSource.TotalDurationMilliseconds, Is.EqualTo(30));
    }

    static GifDocument CreateDocument(GifCodec codec)
    {
        IGifFactory factory = codec;
        GifBitmap[] frames = new GifBitmap[]
        {
            factory.CreateIndexedBitmap(1, 1, new byte[] { 0 }, CreatePalette()),
            factory.CreateIndexedBitmap(1, 1, new byte[] { 1 }, CreatePalette())
        };
        GifAnimationOptions options = new GifAnimationOptions();
        options.DelaysMilliseconds = new int[] { 10, 20 };
        return factory.CreateAnimation(frames, options, null);
    }

    static GifAnimation CreateAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.TotalDurationMilliseconds = 30;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }, 10, 0),
            CreateFrame(new byte[] { 0, 0, 0, 255, 1, 1, 1, 255 }, 20, 1),
            CreateFrame(new byte[] { 0, 0, 0, 255, 2, 2, 2, 255 }, 0, 2)
        };
        return animation;
    }

    static GifAnimation CreateOptimizationAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.TotalDurationMilliseconds = 60;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }, 10, 0),
            CreateFrame(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }, 20, 1),
            CreateFrame(new byte[] { 0, 0, 0, 255, 5, 5, 5, 255 }, 0, 2),
            CreateFrame(new byte[] { 0, 0, 0, 255, 9, 9, 9, 255 }, 30, 3)
        };
        return animation;
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(2, 1, rgbaPixels);
        frame.DelayMilliseconds = delayMilliseconds;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        frame.SourceWidth = 2;
        frame.SourceHeight = 1;
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifPalette CreateAlternatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 255)
        });
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
