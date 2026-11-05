using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCompositorTests
{
    [Test]
    public void InitializeCanvas_TransparentModeCreatesTransparentRgbaCanvas()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromScreen(new GifLogicalScreen(2, 2), CreatePalette(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[16]));
        Assert.That(result.Canvas.Width, Is.EqualTo(2));
        Assert.That(result.Canvas.Height, Is.EqualTo(2));
        Assert.That(result.Canvas.Stride, Is.EqualTo(8));
        Assert.That(result.Canvas.RgbaPixels, Is.SameAs(result.RgbaPixels));
    }

    [Test]
    public void InitializeCanvas_LogicalScreenModeFillsBackgroundColorFromGlobalPalette()
    {
        GifLogicalScreen logicalScreen = new GifLogicalScreen(2, 1);
        logicalScreen.BackgroundColorIndex = 1;
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromScreen(logicalScreen, CreatePalette(), new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[]
        {
            10, 20, 30, 255,
            10, 20, 30, 255
        }));
    }

    [Test]
    public void InitializeCanvas_SolidColorModeUsesCallerRgbaColorWithoutPalette()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.SolidColor;
        options.SolidBackgroundColor = new GifRgbaColor(21, 31, 41, 51);
        GifLogicalScreen logicalScreen = new GifLogicalScreen(1, 2);
        logicalScreen.BackgroundColorIndex = 99;
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromScreen(logicalScreen, new GifPalette(null), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[]
        {
            21, 31, 41, 51,
            21, 31, 41, 51
        }));
    }

    [Test]
    public void InitializeCanvas_FromDocumentUsesDocumentScreenAndGlobalPalette()
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.LogicalScreen.BackgroundColorIndex = 1;
        document.GlobalColorTable = CreatePalette();
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromDocument(document, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
    }

    [Test]
    public void InitializeCanvas_RejectsMissingOrInvalidLogicalScreen()
    {
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult missing = compositor.InitializeCanvasFromScreen(null!, CreatePalette(), new GifDecodeOptions());
        GifCompositingCanvasResult invalidDimensions = compositor.InitializeCanvasFromScreen(new GifLogicalScreen(0, 1), CreatePalette(), new GifDecodeOptions());

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(invalidDimensions.Success, Is.False);
        Assert.That(invalidDimensions.Error.Code, Is.EqualTo(GifErrorCode.DimensionLimitExceeded));
    }

    [Test]
    public void InitializeCanvas_RejectsMissingInvalidOrOutOfRangeBackgroundPalette()
    {
        GifCompositor compositor = new GifCompositor();
        GifLogicalScreen logicalScreen = new GifLogicalScreen(1, 1);
        GifPalette invalidPalette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });
        GifLogicalScreen outOfRange = new GifLogicalScreen(1, 1);
        outOfRange.BackgroundColorIndex = 2;

        GifCompositingCanvasResult missing = compositor.InitializeCanvasFromScreen(logicalScreen, new GifPalette(null), new GifDecodeOptions());
        GifCompositingCanvasResult invalid = compositor.InitializeCanvasFromScreen(logicalScreen, invalidPalette, new GifDecodeOptions());
        GifCompositingCanvasResult invalidIndex = compositor.InitializeCanvasFromScreen(outOfRange, CreatePalette(), new GifDecodeOptions());

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(invalid.Success, Is.False);
        Assert.That(invalid.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(invalidIndex.Success, Is.False);
        Assert.That(invalidIndex.Error.Code, Is.EqualTo(GifErrorCode.InvalidBackgroundColorIndex));
    }

    [Test]
    public void InitializeCanvas_RejectsOversizedCanvasAllocation()
    {
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromScreen(new GifLogicalScreen(32768, 32768), CreatePalette(), new GifDecodeOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
    }

    [Test]
    public void InitializeCanvas_RejectsConfiguredMemoryLimitBeforeAllocation()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxMemoryBytes = 3;
        GifCompositor compositor = new GifCompositor();

        GifCompositingCanvasResult result = compositor.InitializeCanvasFromScreen(new GifLogicalScreen(1, 1), CreatePalette(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.RgbaPixels, Is.Empty);
    }

    [Test]
    public void OverlayFrame_CopiesOpaqueFramePixelsAtOffset()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(3, 2, CreateRepeatedRgba(3, 2, 1, 1, 1, 255));
        GifBitmap frame = new GifBitmap(2, 1, new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 255
        });

        GifFrameOverlayResult result = compositor.OverlayFrameAtPosition(canvas, frame, 1, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Clipped, Is.False);
        Assert.That(result.RgbaPixels, Is.SameAs(canvas.RgbaPixels));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            1, 1, 1, 255,
            1, 1, 1, 255,
            1, 1, 1, 255,
            1, 1, 1, 255,
            10, 20, 30, 255,
            40, 50, 60, 255
        }));
    }

    [Test]
    public void OverlayFrame_SkipsTransparentSourcePixels()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 1, CreateRepeatedRgba(2, 1, 1, 1, 1, 255));
        GifBitmap frame = new GifBitmap(2, 1, new byte[]
        {
            10, 20, 30, 0,
            40, 50, 60, 128
        });

        GifFrameOverlayResult result = compositor.OverlayFrameAtPosition(canvas, frame, 0, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            1, 1, 1, 255,
            40, 50, 60, 128
        }));
    }

    [Test]
    public void OverlayFrame_ClipsFrameOutsideCanvasBounds()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 2, CreateRepeatedRgba(2, 2, 0, 0, 0, 255));
        GifBitmap frame = new GifBitmap(3, 2, new byte[]
        {
            1, 1, 1, 255,
            2, 2, 2, 255,
            3, 3, 3, 255,
            4, 4, 4, 255,
            5, 5, 5, 255,
            6, 6, 6, 255
        });

        GifFrameOverlayResult result = compositor.OverlayFrameAtPosition(canvas, frame, 1, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Clipped, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.FrameClippedToLogicalScreen));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255,
            0, 0, 0, 255,
            0, 0, 0, 255,
            1, 1, 1, 255
        }));
    }

    [Test]
    public void OverlayFrame_SupportsNegativeOffsetsByClipping()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 1, CreateRepeatedRgba(2, 1, 0, 0, 0, 255));
        GifBitmap frame = new GifBitmap(2, 1, new byte[]
        {
            1, 1, 1, 255,
            2, 2, 2, 255
        });

        GifFrameOverlayResult result = compositor.OverlayFrameAtPosition(canvas, frame, -1, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Clipped, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            2, 2, 2, 255,
            0, 0, 0, 255
        }));
    }

    [Test]
    public void OverlayFrame_UsesGifFrameOffsets()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 2, CreateRepeatedRgba(2, 2, 0, 0, 0, 255));
        GifBitmap frameBitmap = new GifBitmap(1, 1, new byte[] { 9, 8, 7, 255 });
        GifFrame frame = new GifFrame(1, 0, 1, 1);

        GifFrameOverlayResult result = compositor.OverlayFrameAtFramePosition(canvas, frameBitmap, frame);

        Assert.That(result.Success, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255,
            9, 8, 7, 255,
            0, 0, 0, 255,
            0, 0, 0, 255
        }));
    }

    [Test]
    public void OverlayFrame_RejectsInvalidInputs()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 0, 0, 0, 255 });
        GifBitmap badFrame = new GifBitmap(1, 1, new byte[] { 1, 2, 3 });
        GifBitmap zeroCanvas = new GifBitmap(0, 1, new byte[0]);

        GifFrameOverlayResult missing = compositor.OverlayFrameAtPosition(null!, badFrame, 0, 0);
        GifFrameOverlayResult invalidDimensions = compositor.OverlayFrameAtPosition(zeroCanvas, badFrame, 0, 0);
        GifFrameOverlayResult badLength = compositor.OverlayFrameAtPosition(canvas, badFrame, 0, 0);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(invalidDimensions.Success, Is.False);
        Assert.That(invalidDimensions.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void ResolveDisposalMethod_UsesDoNotDisposeFallbackForUnspecifiedByDefault()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalDisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.UsedFallback, Is.True);
    }

    [Test]
    public void ResolveDisposalMethod_UsesConfiguredFallbackForUnspecifiedFrames()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.RestoreToBackground;

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(result.UsedFallback, Is.True);
    }

    [Test]
    public void ResolveDisposalMethod_PreservesExplicitFrameDisposal()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.RestoreToPrevious;
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.RestoreToBackground;

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.UsedFallback, Is.False);
    }

    [Test]
    public void ResolveDisposalMethod_UsesFallbackAndWarnsForReservedDisposal()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.Reserved5;
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.RestoreToPrevious;

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalDisposalMethod, Is.EqualTo(GifDisposalMethod.Reserved5));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ReservedDisposalMethod));
    }

    [Test]
    public void ResolveDisposalMethod_NormalizesUnspecifiedFallbackToDoNotDispose()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.Unspecified;

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.UsedFallback, Is.True);
    }

    [Test]
    public void ResolveDisposalMethod_NormalizesReservedFallbackToDoNotDispose()
    {
        GifCompositor compositor = new GifCompositor();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.Reserved4;
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.Reserved7;

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(frame, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalDisposalMethod, Is.EqualTo(GifDisposalMethod.Reserved4));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ReservedDisposalMethod));
    }

    [Test]
    public void ResolveDisposalMethod_RejectsMissingFrame()
    {
        GifCompositor compositor = new GifCompositor();

        GifDisposalMethodResolutionResult result = compositor.ResolveDisposalMethod(null!, new GifDecodeOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }

    [Test]
    public void ApplyDoNotDispose_LeavesCanvasPixelsUnchanged()
    {
        GifCompositor compositor = new GifCompositor();
        byte[] pixels = new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 };
        GifBitmap canvas = new GifBitmap(2, 1, pixels);

        GifDisposalApplyResult result = compositor.ApplyDoNotDispose(canvas);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Canvas, Is.SameAs(canvas));
        Assert.That(result.RgbaPixels, Is.SameAs(pixels));
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
    }

    [Test]
    public void ApplyDisposal_MethodOneLeavesCanvasPixelsUnchanged()
    {
        GifCompositor compositor = new GifCompositor();
        byte[] pixels = new byte[] { 1, 2, 3, 255 };
        GifBitmap canvas = new GifBitmap(1, 1, pixels);
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;

        GifDisposalApplyResult result = compositor.ApplyDisposalWithDefaults(canvas, frame, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    [Test]
    public void ApplyDisposal_UnspecifiedFrameUsesDoNotDisposeFallback()
    {
        GifCompositor compositor = new GifCompositor();
        byte[] pixels = new byte[] { 1, 2, 3, 255 };
        GifBitmap canvas = new GifBitmap(1, 1, pixels);
        GifFrame frame = new GifFrame(0, 0, 1, 1);

        GifDisposalApplyResult result = compositor.ApplyDisposalWithDefaults(canvas, frame, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.RgbaPixels, Is.SameAs(pixels));
    }

    [Test]
    public void ApplyDoNotDispose_RejectsInvalidCanvas()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap badLength = new GifBitmap(1, 1, new byte[] { 1, 2, 3 });

        GifDisposalApplyResult missing = compositor.ApplyDoNotDispose(null!);
        GifDisposalApplyResult invalidLength = compositor.ApplyDoNotDispose(badLength);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(invalidLength.Success, Is.False);
        Assert.That(invalidLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void ApplyRestoreToBackground_TransparentModeClearsFrameRectangle()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(3, 2, CreateRepeatedRgba(3, 2, 9, 9, 9, 255));
        GifFrame frame = new GifFrame(1, 0, 2, 2);
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifDisposalApplyResult result = compositor.ApplyRestoreToBackground(canvas, frame, new GifLogicalScreen(3, 2), CreatePalette(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            0, 0, 0, 0,
            0, 0, 0, 0,
            9, 9, 9, 255,
            0, 0, 0, 0,
            0, 0, 0, 0
        }));
    }

    [Test]
    public void ApplyRestoreToBackground_LogicalScreenModeFillsFrameRectangleFromBackgroundIndex()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 2, CreateRepeatedRgba(2, 2, 9, 9, 9, 255));
        GifFrame frame = new GifFrame(0, 1, 2, 1);
        GifLogicalScreen logicalScreen = new GifLogicalScreen(2, 2);
        logicalScreen.BackgroundColorIndex = 1;

        GifDisposalApplyResult result = compositor.ApplyRestoreToBackground(canvas, frame, logicalScreen, CreatePalette(), new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            9, 9, 9, 255,
            10, 20, 30, 255,
            10, 20, 30, 255
        }));
    }

    [Test]
    public void ApplyRestoreToBackground_SolidColorModeUsesCallerRgbaColorWithoutPalette()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 2, CreateRepeatedRgba(2, 2, 9, 9, 9, 255));
        GifFrame frame = new GifFrame(0, 1, 2, 1);
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.SolidColor;
        options.SolidBackgroundColor = new GifRgbaColor(22, 32, 42, 52);

        GifDisposalApplyResult result = compositor.ApplyRestoreToBackground(canvas, frame, new GifLogicalScreen(2, 2), new GifPalette(null), options);

        Assert.That(result.Success, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            9, 9, 9, 255,
            22, 32, 42, 52,
            22, 32, 42, 52
        }));
    }

    [Test]
    public void ApplyRestoreToBackground_ClipsFrameRectangle()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 2, CreateRepeatedRgba(2, 2, 9, 9, 9, 255));
        GifFrame frame = new GifFrame(1, 1, 2, 2);
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifDisposalApplyResult result = compositor.ApplyRestoreToBackground(canvas, frame, new GifLogicalScreen(2, 2), CreatePalette(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.FrameClippedToLogicalScreen));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            9, 9, 9, 255,
            9, 9, 9, 255,
            0, 0, 0, 0
        }));
    }

    [Test]
    public void ApplyDisposal_MethodTwoRestoresFrameRectangleToBackground()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 1, CreateRepeatedRgba(2, 1, 9, 9, 9, 255));
        GifFrame frame = new GifFrame(1, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifDisposalApplyResult result = compositor.ApplyDisposalWithScreen(canvas, frame, new GifLogicalScreen(2, 1), CreatePalette(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 9, 9, 9, 255, 0, 0, 0, 0 }));
    }

    [Test]
    public void ApplyRestoreToBackground_RejectsInvalidFrameOrBackgroundPalette()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 9, 9, 9, 255 });
        GifFrame invalidFrame = new GifFrame(0, 0, 0, 1);

        GifDisposalApplyResult badFrame = compositor.ApplyRestoreToBackground(canvas, invalidFrame, new GifLogicalScreen(1, 1), CreatePalette(), new GifDecodeOptions());
        GifDisposalApplyResult missingPalette = compositor.ApplyRestoreToBackground(canvas, new GifFrame(0, 0, 1, 1), new GifLogicalScreen(1, 1), new GifPalette(null), new GifDecodeOptions());

        Assert.That(badFrame.Success, Is.False);
        Assert.That(badFrame.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(missingPalette.Success, Is.False);
        Assert.That(missingPalette.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
    }

    [Test]
    public void CapturePreviousCanvas_CopiesCurrentCanvasPixels()
    {
        GifCompositor compositor = new GifCompositor();
        byte[] pixels = new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 };
        GifBitmap canvas = new GifBitmap(2, 1, pixels);

        GifDisposalApplyResult result = compositor.CapturePreviousCanvas(canvas);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Canvas, Is.Not.SameAs(canvas));
        Assert.That(result.RgbaPixels, Is.Not.SameAs(pixels));
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));

        pixels[0] = 99;

        Assert.That(result.RgbaPixels[0], Is.EqualTo(1));
    }

    [Test]
    public void ApplyRestoreToPrevious_ReplacesCanvasWithSnapshotPixels()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(2, 1, new byte[] { 9, 9, 9, 255, 8, 8, 8, 255 });
        GifBitmap previous = new GifBitmap(2, 1, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });

        GifDisposalApplyResult result = compositor.ApplyRestoreToPrevious(canvas, previous);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Canvas, Is.SameAs(canvas));
        Assert.That(result.RgbaPixels, Is.SameAs(canvas.RgbaPixels));
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));
    }

    [Test]
    public void ApplyDisposal_MethodThreeRestoresPreviousSnapshot()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 9, 9, 9, 255 });
        GifBitmap previous = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.RestoreToPrevious;

        GifDisposalApplyResult result = compositor.ApplyDisposalWithPreviousCanvas(canvas, frame, new GifLogicalScreen(1, 1), CreatePalette(), previous, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    [Test]
    public void ApplyDisposal_ReservedMethodAppliesConfiguredFallbackAndWarning()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 9, 9, 9, 255 });
        GifBitmap previous = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.DisposalMethod = GifDisposalMethod.Reserved6;
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = GifDisposalMethod.RestoreToPrevious;

        GifDisposalApplyResult result = compositor.ApplyDisposalWithPreviousCanvas(canvas, frame, new GifLogicalScreen(1, 1), CreatePalette(), previous, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EffectiveDisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ReservedDisposalMethod));
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    [Test]
    public void ApplyRestoreToPrevious_RejectsMissingOrMismatchedSnapshot()
    {
        GifCompositor compositor = new GifCompositor();
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 9, 9, 9, 255 });
        GifBitmap mismatched = new GifBitmap(2, 1, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });

        GifDisposalApplyResult missing = compositor.ApplyRestoreToPrevious(canvas, null!);
        GifDisposalApplyResult badSize = compositor.ApplyRestoreToPrevious(canvas, mismatched);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(badSize.Success, Is.False);
        Assert.That(badSize.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(10, 20, 30)
        });
    }

    static byte[] CreateRepeatedRgba(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        byte[] pixels = new byte[width * height * 4];
        int offset = 0;
        while (offset < pixels.Length)
        {
            pixels[offset] = red;
            pixels[offset + 1] = green;
            pixels[offset + 2] = blue;
            pixels[offset + 3] = alpha;
            offset = offset + 4;
        }

        return pixels;
    }
}
