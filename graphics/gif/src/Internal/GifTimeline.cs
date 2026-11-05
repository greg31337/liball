namespace gif;

internal class GifTimelineResult
{
    internal bool Success;
    internal int TotalDurationMilliseconds;
    internal int[] FrameStartMilliseconds;
    internal int[] FrameEndMilliseconds;
    internal GifWarningDiagnostic[] Warnings;
    internal GifErrorDiagnostic Error;

    internal GifTimelineResult()
    {
        Success = false;
        TotalDurationMilliseconds = 0;
        FrameStartMilliseconds = new int[0];
        FrameEndMilliseconds = new int[0];
        Warnings = new GifWarningDiagnostic[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDelayNormalizationResult
{
    internal bool Success;
    internal int DelayMilliseconds;
    internal bool Normalized;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifDelayNormalizationResult()
    {
        Success = false;
        DelayMilliseconds = 0;
        Normalized = false;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifTimeline
{
    internal GifTimelineResult Calculate(GifAnimation animation)
    {
        return CalculateAnimationWithOptions(animation, new GifDecodeOptions());
    }

    internal GifTimelineResult CalculateAnimationWithOptions(GifAnimation animation, GifDecodeOptions options)
    {
        GifTimelineResult result = new GifTimelineResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for timeline calculation.", new GifDiagnosticPosition(-1, -1, "Timeline"));
            return result;
        }

        return CalculateFramesWithOptions(animation.Frames, options);
    }

    internal GifTimelineResult CalculateFrames(GifAnimationFrame[] frames)
    {
        return CalculateFramesWithOptions(frames, new GifDecodeOptions());
    }

    internal GifTimelineResult CalculateFramesWithOptions(GifAnimationFrame[] frames, GifDecodeOptions options)
    {
        GifTimelineResult result = new GifTimelineResult();
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        result.FrameStartMilliseconds = new int[frames.Length];
        result.FrameEndMilliseconds = new int[frames.Length];

        long elapsedMilliseconds = 0;
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifAnimationFrame frame = frames[frameIndex];
            if (frame == null)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for timeline calculation.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            GifDelayNormalizationResult delay = NormalizeDelayMilliseconds(frame.DelayMilliseconds, options, frameIndex);
            if (!delay.Success)
            {
                result.Error = delay.Error;
                return result;
            }

            result.Warnings = AppendWarning(result.Warnings, delay.Warning);
            result.FrameStartMilliseconds[frameIndex] = (int)elapsedMilliseconds;
            elapsedMilliseconds = elapsedMilliseconds + delay.DelayMilliseconds;
            if (elapsedMilliseconds > 2147483647)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF animation timeline duration exceeds the supported range.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            result.FrameEndMilliseconds[frameIndex] = (int)elapsedMilliseconds;
            frameIndex = frameIndex + 1;
        }

        result.TotalDurationMilliseconds = (int)elapsedMilliseconds;
        result.Success = true;
        return result;
    }

    internal GifTimelineResult CalculateDocument(GifDocument document)
    {
        return CalculateDocumentWithOptions(document, new GifDecodeOptions());
    }

    internal GifTimelineResult CalculateDocumentWithOptions(GifDocument document, GifDecodeOptions options)
    {
        GifTimelineResult result = new GifTimelineResult();
        if (document == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for timeline calculation.", new GifDiagnosticPosition(-1, -1, "Timeline"));
            return result;
        }

        if (document.Frames == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document has no frames for timeline calculation.", new GifDiagnosticPosition(-1, -1, "Timeline"));
            return result;
        }

        result.FrameStartMilliseconds = new int[document.Frames.Length];
        result.FrameEndMilliseconds = new int[document.Frames.Length];

        long elapsedMilliseconds = 0;
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            GifFrame frame = document.Frames[frameIndex];
            if (frame == null)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for timeline calculation.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            long rawDelayMilliseconds = (long)frame.DelayHundredths * 10;
            if (rawDelayMilliseconds < 0 || rawDelayMilliseconds > 2147483647)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF frame delay exceeds the supported millisecond range.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            GifDelayNormalizationResult delay = NormalizeDelayMilliseconds((int)rawDelayMilliseconds, options, frameIndex);
            if (!delay.Success)
            {
                result.Error = delay.Error;
                return result;
            }

            result.Warnings = AppendWarning(result.Warnings, delay.Warning);
            result.FrameStartMilliseconds[frameIndex] = (int)elapsedMilliseconds;
            elapsedMilliseconds = elapsedMilliseconds + delay.DelayMilliseconds;
            if (elapsedMilliseconds > 2147483647)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF timeline duration exceeds the supported range.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
                return result;
            }

            result.FrameEndMilliseconds[frameIndex] = (int)elapsedMilliseconds;
            frameIndex = frameIndex + 1;
        }

        result.TotalDurationMilliseconds = (int)elapsedMilliseconds;
        result.Success = true;
        return result;
    }

    internal GifDelayNormalizationResult NormalizeDelayMilliseconds(int delayMilliseconds, GifDecodeOptions options, int frameIndex)
    {
        GifDelayNormalizationResult result = new GifDelayNormalizationResult();
        if (delayMilliseconds < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF frame delay cannot be negative.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.MinimumFrameDelayMilliseconds < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Minimum frame delay cannot be negative.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
            return result;
        }

        result.DelayMilliseconds = delayMilliseconds;
        if (delayMilliseconds == 0 && activeOptions.MinimumFrameDelayMilliseconds > 0)
        {
            result.DelayMilliseconds = activeOptions.MinimumFrameDelayMilliseconds;
            result.Normalized = true;
            result.Warning = new GifWarningDiagnostic(GifWarningCode.ZeroDelayNormalized, "Zero-delay frame was normalized to the configured minimum frame delay.", new GifDiagnosticPosition(-1, frameIndex, "Timeline"));
        }

        result.Success = true;
        return result;
    }

    internal GifDecodeOptions NormalizeOptions(GifDecodeOptions options)
    {
        if (options == null)
        {
            return new GifDecodeOptions();
        }

        return options;
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
