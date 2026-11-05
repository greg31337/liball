namespace gif;

internal class GifCoalesceResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal GifWarningDiagnostic[] Warnings;
    internal GifErrorDiagnostic Error;

    internal GifCoalesceResult()
    {
        Success = false;
        Animation = new GifAnimation();
        Warnings = new GifWarningDiagnostic[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifCoalescer
{
    internal GifCoalesceResult Coalesce(GifDocument? document)
    {
        return CoalesceWithOptions(document, new GifDecodeOptions());
    }

    internal GifCoalesceResult CoalesceWithOptions(GifDocument? document, GifDecodeOptions? options)
    {
        return CoalesceForFrameSelection(document, options, -1);
    }

    internal GifCoalesceResult CoalesceSelectedFrameWithOptions(GifDocument? document, GifDecodeOptions? options, int frameIndex)
    {
        return CoalesceForFrameSelection(document, options, frameIndex);
    }

    internal GifCoalesceResult CoalesceForFrameSelection(GifDocument? document, GifDecodeOptions? options, int selectedFrameIndex)
    {
        GifCoalesceResult result = new GifCoalesceResult();
        if (document == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for animation coalescing.", new GifDiagnosticPosition(-1, -1, "Coalescer"));
            return result;
        }

        GifCompositor compositor = new GifCompositor();
        GifDecodeOptions activeOptions = compositor.NormalizeOptions(options);
        GifCompositingCanvasResult canvasResult = compositor.InitializeCanvasFromDocument(document, activeOptions);
        if (!canvasResult.Success)
        {
            result.Error = canvasResult.Error;
            return result;
        }

        if (document.Frames == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document has no frames for animation coalescing.", new GifDiagnosticPosition(-1, -1, "Coalescer"));
            return result;
        }

        if (selectedFrameIndex < -1 || selectedFrameIndex >= document.Frames.Length)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Selected GIF frame index is outside the document frame list.", new GifDiagnosticPosition(-1, selectedFrameIndex, "FrameSelection"));
            return result;
        }

        GifAnimation animation = new GifAnimation();
        animation.Width = document.LogicalScreen.Width;
        animation.Height = document.LogicalScreen.Height;
        animation.LoopCount = document.LoopCount;
        animation.Warnings = AppendWarnings(animation.Warnings, document.Warnings);
        animation.PlainTextGraphics = document.PlainTextGraphics;

        GifBitmap canvas = canvasResult.Canvas;
        GifTimeline timeline = new GifTimeline();
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length && (selectedFrameIndex < 0 || frameIndex <= selectedFrameIndex))
        {
            GifFrame frame = document.Frames[frameIndex];
            if (frame == null)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for animation coalescing.", new GifDiagnosticPosition(-1, frameIndex, "Coalescer"));
                return result;
            }

            GifDisposalApplyResult previousCanvas = compositor.CapturePreviousCanvas(canvas);
            if (!previousCanvas.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = AddFrameContextToError(previousCanvas.Error, frameIndex);
                return result;
            }

            GifRgbaConversionResult frameBitmap = CreateFrameBitmap(document, frameIndex, activeOptions);
            if (!frameBitmap.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = AddFrameContextToError(frameBitmap.Error, frameIndex);
                return result;
            }

            animation.Warnings = AppendWarningsWithFrameContext(animation.Warnings, frame.Warnings, frameIndex);

            long rawDelayMilliseconds = (long)frame.DelayHundredths * 10;
            if (rawDelayMilliseconds < 0 || rawDelayMilliseconds > 2147483647)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF frame delay exceeds the supported millisecond range.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            GifFrameOverlayResult overlay = compositor.OverlayFrameAtFramePosition(canvas, frameBitmap.Bitmap, frame);
            if (!overlay.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = AddFrameContextToError(overlay.Error, frameIndex);
                return result;
            }

            animation.Warnings = AppendWarningsWithFrameContext(animation.Warnings, new GifWarningDiagnostic[] { overlay.Warning }, frameIndex);

            GifDisposalApplyResult coalescedCanvas = compositor.CapturePreviousCanvas(canvas);
            if (!coalescedCanvas.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = AddFrameContextToError(coalescedCanvas.Error, frameIndex);
                return result;
            }

            GifAnimationFrame animationFrame = CreateAnimationFrame(frame, coalescedCanvas.Canvas, frameIndex);
            GifDelayNormalizationResult delay = timeline.NormalizeDelayMilliseconds(animationFrame.DelayMilliseconds, activeOptions, frameIndex);
            if (!delay.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = delay.Error;
                return result;
            }

            animationFrame.DelayMilliseconds = delay.DelayMilliseconds;
            animation.Warnings = AppendWarning(animation.Warnings, delay.Warning);

            GifDisposalApplyResult disposal = compositor.ApplyDisposalWithPreviousCanvas(canvas, frame, document.LogicalScreen, document.GlobalColorTable, previousCanvas.Canvas, activeOptions);
            if (!disposal.Success)
            {
                result.Animation = animation;
                result.Warnings = animation.Warnings;
                result.Error = AddFrameContextToError(disposal.Error, frameIndex);
                return result;
            }

            animation.Warnings = AppendWarningsWithFrameContext(animation.Warnings, new GifWarningDiagnostic[] { disposal.Warning }, frameIndex);
            animationFrame.DisposalMethod = disposal.EffectiveDisposalMethod;
            if (selectedFrameIndex < 0 || frameIndex == selectedFrameIndex)
            {
                animation.Frames = AppendFrame(animation.Frames, animationFrame);
            }

            canvas = disposal.Canvas;
            if (selectedFrameIndex >= 0 && frameIndex < selectedFrameIndex)
            {
                frame.CompressedImageData = new byte[0];
                frame.IndexedPixels = new byte[0];
            }

            frameIndex = frameIndex + 1;
        }

        if (selectedFrameIndex >= 0 && animation.Frames.Length != 1)
        {
            result.Animation = animation;
            result.Warnings = animation.Warnings;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Selected GIF frame was not composited.", new GifDiagnosticPosition(-1, selectedFrameIndex, "FrameSelection"));
            return result;
        }

        GifTimelineResult timelineResult = timeline.Calculate(animation);
        if (!timelineResult.Success)
        {
            result.Animation = animation;
            result.Warnings = animation.Warnings;
            result.Error = timelineResult.Error;
            return result;
        }

        animation.TotalDurationMilliseconds = timelineResult.TotalDurationMilliseconds;
        result.Success = true;
        result.Animation = animation;
        result.Warnings = animation.Warnings;
        return result;
    }

    internal GifRgbaConversionResult CreateFrameBitmap(GifDocument document, int frameIndex, GifDecodeOptions options)
    {
        GifRgbaConversionResult result = new GifRgbaConversionResult();
        if (document == null || document.Frames == null || frameIndex < 0 || frameIndex >= document.Frames.Length)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame index is outside the document frame list.", new GifDiagnosticPosition(-1, frameIndex, "Coalescer"));
            return result;
        }

        GifFrame frame = document.Frames[frameIndex];
        if (NeedsRawDecode(frame))
        {
            GifRawFrameDecoder decoder = new GifRawFrameDecoder();
            GifRawFrameDecodeResult decode = decoder.DecodeFrameWithOptions(frame, options);
            if (!decode.Success)
            {
                result.Error = decode.Error;
                return result;
            }
        }

        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();
        return converter.ConvertDocumentFrame(document, frameIndex, options);
    }

    internal bool NeedsRawDecode(GifFrame frame)
    {
        if (frame == null)
        {
            return false;
        }

        if (frame.IndexedPixels == null)
        {
            return true;
        }

        long expectedPixelCount = (long)frame.Width * (long)frame.Height;
        if (expectedPixelCount < 0 || expectedPixelCount > 2147483647)
        {
            return false;
        }

        if (frame.IndexedPixels.Length == (int)expectedPixelCount)
        {
            return false;
        }

        if (frame.CompressedImageData != null && frame.CompressedImageData.Length > 0)
        {
            return true;
        }

        return false;
    }

    internal GifAnimationFrame CreateAnimationFrame(GifFrame frame, GifBitmap bitmap, int frameIndex)
    {
        GifAnimationFrame animationFrame = new GifAnimationFrame();
        animationFrame.Bitmap = bitmap;
        long delayMilliseconds = (long)frame.DelayHundredths * 10;
        if (delayMilliseconds < -2147483648)
        {
            animationFrame.DelayMilliseconds = -2147483648;
        }
        else if (delayMilliseconds > 2147483647)
        {
            animationFrame.DelayMilliseconds = 2147483647;
        }
        else
        {
            animationFrame.DelayMilliseconds = (int)delayMilliseconds;
        }
        animationFrame.DisposalMethod = frame.DisposalMethod;
        animationFrame.SourceLeft = frame.Left;
        animationFrame.SourceTop = frame.Top;
        animationFrame.SourceWidth = frame.Width;
        animationFrame.SourceHeight = frame.Height;
        animationFrame.FrameIndex = frameIndex;
        return animationFrame;
    }

    internal GifAnimationFrame[] AppendFrame(GifAnimationFrame[] frames, GifAnimationFrame frame)
    {
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        GifAnimationFrame[] result = new GifAnimationFrame[frames.Length + 1];
        int index = 0;
        while (index < frames.Length)
        {
            result[index] = frames[index];
            index = index + 1;
        }

        result[index] = frame;
        return result;
    }

    internal GifWarningDiagnostic[] AppendWarnings(GifWarningDiagnostic[] warnings, GifWarningDiagnostic[] items)
    {
        if (warnings == null)
        {
            warnings = new GifWarningDiagnostic[0];
        }

        if (items == null)
        {
            return warnings;
        }

        GifWarningDiagnostic[] result = warnings;
        int index = 0;
        while (index < items.Length)
        {
            result = AppendWarning(result, items[index]);
            index = index + 1;
        }

        return result;
    }

    internal GifWarningDiagnostic[] AppendWarningsWithFrameContext(GifWarningDiagnostic[] warnings, GifWarningDiagnostic[] items, int frameIndex)
    {
        if (warnings == null)
        {
            warnings = new GifWarningDiagnostic[0];
        }

        if (items == null)
        {
            return warnings;
        }

        GifWarningDiagnostic[] result = warnings;
        int index = 0;
        while (index < items.Length)
        {
            GifWarningDiagnostic warning = items[index];
            if (warning != null)
            {
                int resolvedFrameIndex = warning.Position.FrameIndex;
                if (resolvedFrameIndex < 0)
                {
                    resolvedFrameIndex = frameIndex;
                }

                GifWarningDiagnostic contextualWarning = new GifWarningDiagnostic(warning.Code, warning.Message, new GifDiagnosticPosition(warning.Position.ByteOffset, resolvedFrameIndex, warning.Position.BlockName));
                result = AppendWarning(result, contextualWarning);
            }

            index = index + 1;
        }

        return result;
    }

    internal GifErrorDiagnostic AddFrameContextToError(GifErrorDiagnostic error, int frameIndex)
    {
        if (error == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, new GifDiagnosticPosition(-1, frameIndex, "Coalescer"));
        }

        int resolvedFrameIndex = error.Position.FrameIndex;
        if (resolvedFrameIndex < 0)
        {
            resolvedFrameIndex = frameIndex;
        }

        return new GifErrorDiagnostic(error.Code, error.Message, new GifDiagnosticPosition(error.Position.ByteOffset, resolvedFrameIndex, error.Position.BlockName));
    }

    internal GifWarningDiagnostic[] AppendWarning(GifWarningDiagnostic[] warnings, GifWarningDiagnostic warning)
    {
        if (warnings == null)
        {
            warnings = new GifWarningDiagnostic[0];
        }

        if (warning == null || warning.Code == GifWarningCode.None)
        {
            return warnings;
        }

        GifWarningDiagnostic[] result = new GifWarningDiagnostic[warnings.Length + 1];
        int index = 0;
        while (index < warnings.Length)
        {
            result[index] = warnings[index];
            index = index + 1;
        }

        result[index] = warning;
        return result;
    }
}
