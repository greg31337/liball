namespace gif;

internal class GifDuplicateFrameDetectionResult
{
    internal bool Success;
    internal bool[] DuplicateFrames;
    internal int[] DuplicateFrameIndices;
    internal int DuplicateCount;
    internal GifErrorDiagnostic Error;

    internal GifDuplicateFrameDetectionResult()
    {
        Success = false;
        DuplicateFrames = new bool[0];
        DuplicateFrameIndices = new int[0];
        DuplicateCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDuplicateFrameRemovalResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal int RemovedCount;
    internal int[] RemovedFrameIndices;
    internal GifErrorDiagnostic Error;

    internal GifDuplicateFrameRemovalResult()
    {
        Success = false;
        Animation = new GifAnimation();
        RemovedCount = 0;
        RemovedFrameIndices = new int[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifZeroDelayFrameRemovalResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal int RemovedCount;
    internal int[] RemovedFrameIndices;
    internal GifErrorDiagnostic Error;

    internal GifZeroDelayFrameRemovalResult()
    {
        Success = false;
        Animation = new GifAnimation();
        RemovedCount = 0;
        RemovedFrameIndices = new int[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifChangedBounds
{
    internal bool HasChanges;
    internal int Left;
    internal int Top;
    internal int Width;
    internal int Height;
    internal int FrameIndex;

    internal GifChangedBounds()
    {
        HasChanges = false;
        Left = 0;
        Top = 0;
        Width = 0;
        Height = 0;
        FrameIndex = -1;
    }
}

internal class GifChangedBoundsResult
{
    internal bool Success;
    internal GifChangedBounds[] Bounds;
    internal GifErrorDiagnostic Error;

    internal GifChangedBoundsResult()
    {
        Success = false;
        Bounds = new GifChangedBounds[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDeconstructResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal GifChangedBounds[] Bounds;
    internal GifErrorDiagnostic Error;

    internal GifDeconstructResult()
    {
        Success = false;
        Animation = new GifAnimation();
        Bounds = new GifChangedBounds[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifOptimizationResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal GifOptimizationMode OptimizationMode;
    internal GifErrorDiagnostic Error;

    internal GifOptimizationResult()
    {
        Success = false;
        Animation = new GifAnimation();
        OptimizationMode = GifOptimizationMode.None;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifFrameDelayNormalizationResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal int NormalizedCount;
    internal GifErrorDiagnostic Error;

    internal GifFrameDelayNormalizationResult()
    {
        Success = false;
        Animation = new GifAnimation();
        NormalizedCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifOptimizer
{
    internal GifOptimizationResult Optimize(GifAnimation animation)
    {
        return OptimizeWithOptions(animation, new GifEncodeOptions());
    }

    internal GifOptimizationResult OptimizeWithOptions(GifAnimation? animation, GifEncodeOptions? options)
    {
        GifOptimizationResult result = new GifOptimizationResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for optimization.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifEncodeOptions activeOptions = NormalizeEncodeOptions(options);
        result.OptimizationMode = activeOptions.OptimizationMode;
        if (activeOptions.OptimizationMode != GifOptimizationMode.None &&
            activeOptions.OptimizationMode != GifOptimizationMode.Deconstruct &&
            activeOptions.OptimizationMode != GifOptimizationMode.RemoveDuplicateFrames &&
            activeOptions.OptimizationMode != GifOptimizationMode.RemoveZeroDelayFrames)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF optimization mode is not supported.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        if (activeOptions.NormalizeFrameDelays && activeOptions.MinimumFrameDelayMilliseconds < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Minimum optimized frame delay cannot be negative.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifErrorDiagnostic validation = ValidateAnimationFrames(animation.Frames);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        GifAnimation optimized = CopyAnimation(animation);
        bool removeZeroDelayFrames = activeOptions.RemoveZeroDelayFrames || activeOptions.OptimizationMode == GifOptimizationMode.RemoveZeroDelayFrames;
        bool removeDuplicateFrames = activeOptions.RemoveDuplicateFrames || activeOptions.OptimizationMode == GifOptimizationMode.RemoveDuplicateFrames;
        bool deconstructFrames = activeOptions.DeconstructFrames || activeOptions.OptimizationMode == GifOptimizationMode.Deconstruct;

        if (removeZeroDelayFrames)
        {
            GifZeroDelayFrameRemovalResult removeZeroDelay = RemoveZeroDelayFrames(optimized);
            if (!removeZeroDelay.Success)
            {
                result.Animation = removeZeroDelay.Animation;
                result.Error = removeZeroDelay.Error;
                return result;
            }

            optimized = removeZeroDelay.Animation;
        }

        if (removeDuplicateFrames)
        {
            GifDuplicateFrameRemovalResult removeDuplicates = RemoveDuplicateFrames(optimized);
            if (!removeDuplicates.Success)
            {
                result.Animation = removeDuplicates.Animation;
                result.Error = removeDuplicates.Error;
                return result;
            }

            optimized = removeDuplicates.Animation;
        }

        if (activeOptions.NormalizeFrameDelays)
        {
            GifFrameDelayNormalizationResult normalization = NormalizeFrameDelays(optimized, activeOptions.MinimumFrameDelayMilliseconds);
            if (!normalization.Success)
            {
                result.Animation = normalization.Animation;
                result.Error = normalization.Error;
                return result;
            }

            optimized = normalization.Animation;
        }

        if (deconstructFrames)
        {
            GifDeconstructResult deconstruct = Deconstruct(optimized);
            if (!deconstruct.Success)
            {
                result.Animation = deconstruct.Animation;
                result.Error = deconstruct.Error;
                return result;
            }

            optimized = deconstruct.Animation;
        }

        if (activeOptions.RecalculateTimeline)
        {
            GifTimelineResult timeline = new GifTimeline().Calculate(optimized);
            if (!timeline.Success)
            {
                result.Animation = optimized;
                result.Error = timeline.Error;
                return result;
            }

            optimized.TotalDurationMilliseconds = timeline.TotalDurationMilliseconds;
        }
        else
        {
            optimized.TotalDurationMilliseconds = animation.TotalDurationMilliseconds;
        }

        result.Success = true;
        result.Animation = optimized;
        return result;
    }

    internal GifFrameDelayNormalizationResult NormalizeFrameDelays(GifAnimation? animation, int minimumDelayMilliseconds)
    {
        GifFrameDelayNormalizationResult result = new GifFrameDelayNormalizationResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for frame-delay normalization.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        if (minimumDelayMilliseconds < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Minimum optimized frame delay cannot be negative.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifAnimationFrame[] frames = animation.Frames;
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        GifAnimation optimized = CopyAnimationShell(animation);
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                result.Animation = optimized;
                result.Error = validation;
                return result;
            }

            if (frames[frameIndex].DelayMilliseconds < 0)
            {
                result.Animation = optimized;
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF animation frame delay cannot be negative.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
                return result;
            }

            GifAnimationFrame frame = CopyAnimationFrame(frames[frameIndex]);
            if (frame.DelayMilliseconds == 0 && minimumDelayMilliseconds > 0)
            {
                frame.DelayMilliseconds = minimumDelayMilliseconds;
                optimized.Warnings = AppendWarning(optimized.Warnings, new GifWarningDiagnostic(GifWarningCode.ZeroDelayNormalized, "Zero-delay frame was normalized to the configured minimum frame delay.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer")));
                result.NormalizedCount = result.NormalizedCount + 1;
            }

            optimized.Frames = AppendFrame(optimized.Frames, frame);
            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        result.Animation = optimized;
        return result;
    }

    internal GifErrorDiagnostic ValidateAnimationFrames(GifAnimationFrame[] frames)
    {
        if (frames == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                return validation;
            }

            frameIndex = frameIndex + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifDeconstructResult Deconstruct(GifAnimation? animation)
    {
        GifDeconstructResult result = new GifDeconstructResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for deconstruction.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifAnimationFrame[] frames = animation.Frames;
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        GifChangedBoundsResult changedBounds = DetectChangedBoundsForFrames(frames);
        if (!changedBounds.Success)
        {
            result.Error = changedBounds.Error;
            return result;
        }

        GifAnimation optimized = CopyAnimationShell(animation);
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifAnimationFrame frame = CopyAnimationFrame(frames[frameIndex]);
            GifChangedBounds bounds = changedBounds.Bounds[frameIndex];
            frame.Bitmap = CropBitmap(frames[frameIndex].Bitmap, bounds);
            frame.SourceLeft = bounds.Left;
            frame.SourceTop = bounds.Top;
            frame.SourceWidth = frame.Bitmap.Width;
            frame.SourceHeight = frame.Bitmap.Height;
            optimized.Frames = AppendFrame(optimized.Frames, frame);
            frameIndex = frameIndex + 1;
        }

        GifTimeline timeline = new GifTimeline();
        GifTimelineResult timelineResult = timeline.Calculate(optimized);
        if (!timelineResult.Success)
        {
            result.Animation = optimized;
            result.Bounds = changedBounds.Bounds;
            result.Error = timelineResult.Error;
            return result;
        }

        optimized.TotalDurationMilliseconds = timelineResult.TotalDurationMilliseconds;
        result.Success = true;
        result.Animation = optimized;
        result.Bounds = changedBounds.Bounds;
        return result;
    }

    internal GifChangedBoundsResult DetectChangedBounds(GifAnimation animation)
    {
        GifChangedBoundsResult result = new GifChangedBoundsResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for changed-bounds detection.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        return DetectChangedBoundsForFrames(animation.Frames);
    }

    internal GifChangedBoundsResult DetectChangedBoundsForFrames(GifAnimationFrame[] frames)
    {
        GifChangedBoundsResult result = new GifChangedBoundsResult();
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        result.Bounds = new GifChangedBounds[frames.Length];
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                result.Error = validation;
                return result;
            }

            if (frameIndex == 0)
            {
                result.Bounds[frameIndex] = CreateFullBounds(frames[frameIndex].Bitmap, frameIndex);
            }
            else
            {
                GifErrorDiagnostic dimensions = ValidateMatchingBitmapDimensions(frames[frameIndex - 1].Bitmap, frames[frameIndex].Bitmap, frameIndex);
                if (dimensions.Code != GifErrorCode.None)
                {
                    result.Error = dimensions;
                    return result;
                }

                result.Bounds[frameIndex] = FindChangedBounds(frames[frameIndex - 1].Bitmap, frames[frameIndex].Bitmap, frameIndex);
            }

            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        return result;
    }

    internal GifZeroDelayFrameRemovalResult RemoveZeroDelayFrames(GifAnimation animation)
    {
        GifZeroDelayFrameRemovalResult result = new GifZeroDelayFrameRemovalResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for zero-delay frame removal.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifAnimationFrame[] frames = animation.Frames;
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        GifAnimation optimized = CopyAnimationShell(animation);
        int keepZeroFrameIndex = FindZeroDelayFrameToKeep(frames);
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                result.Animation = optimized;
                result.Error = validation;
                return result;
            }

            if (frames[frameIndex].DelayMilliseconds < 0)
            {
                result.Animation = optimized;
                result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF animation frame delay cannot be negative.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
                return result;
            }

            if (frames[frameIndex].DelayMilliseconds == 0 && frameIndex != keepZeroFrameIndex)
            {
                if (optimized.Frames.Length > 0)
                {
                    GifErrorDiagnostic mergeError = MergeDelay(optimized.Frames[optimized.Frames.Length - 1], frames[frameIndex].DelayMilliseconds, frameIndex);
                    if (mergeError.Code != GifErrorCode.None)
                    {
                        result.Animation = optimized;
                        result.Error = mergeError;
                        return result;
                    }
                }

                result.RemovedFrameIndices = AppendIndex(result.RemovedFrameIndices, frameIndex);
                result.RemovedCount = result.RemovedCount + 1;
            }
            else
            {
                optimized.Frames = AppendFrame(optimized.Frames, CopyAnimationFrame(frames[frameIndex]));
            }

            frameIndex = frameIndex + 1;
        }

        GifTimeline timeline = new GifTimeline();
        GifTimelineResult timelineResult = timeline.Calculate(optimized);
        if (!timelineResult.Success)
        {
            result.Animation = optimized;
            result.Error = timelineResult.Error;
            return result;
        }

        optimized.TotalDurationMilliseconds = timelineResult.TotalDurationMilliseconds;
        result.Success = true;
        result.Animation = optimized;
        return result;
    }

    internal GifDuplicateFrameRemovalResult RemoveDuplicateFrames(GifAnimation animation)
    {
        GifDuplicateFrameRemovalResult result = new GifDuplicateFrameRemovalResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for duplicate-frame removal.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        GifAnimationFrame[] frames = animation.Frames;
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        GifDuplicateFrameDetectionResult detection = DetectDuplicateFramesInArray(frames);
        if (!detection.Success)
        {
            result.Error = detection.Error;
            return result;
        }

        GifAnimation optimized = CopyAnimationShell(animation);
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifAnimationFrame frame = CopyAnimationFrame(frames[frameIndex]);
            if (detection.DuplicateFrames[frameIndex] && optimized.Frames.Length > 0)
            {
                GifErrorDiagnostic mergeError = MergeDelay(optimized.Frames[optimized.Frames.Length - 1], frame.DelayMilliseconds, frameIndex);
                if (mergeError.Code != GifErrorCode.None)
                {
                    result.Animation = optimized;
                    result.Error = mergeError;
                    return result;
                }

                result.RemovedFrameIndices = AppendIndex(result.RemovedFrameIndices, frameIndex);
                result.RemovedCount = result.RemovedCount + 1;
            }
            else
            {
                optimized.Frames = AppendFrame(optimized.Frames, frame);
            }

            frameIndex = frameIndex + 1;
        }

        GifTimeline timeline = new GifTimeline();
        GifTimelineResult timelineResult = timeline.Calculate(optimized);
        if (!timelineResult.Success)
        {
            result.Animation = optimized;
            result.Error = timelineResult.Error;
            return result;
        }

        optimized.TotalDurationMilliseconds = timelineResult.TotalDurationMilliseconds;
        result.Success = true;
        result.Animation = optimized;
        return result;
    }

    internal GifDuplicateFrameDetectionResult DetectDuplicateFrames(GifAnimation animation)
    {
        GifDuplicateFrameDetectionResult result = new GifDuplicateFrameDetectionResult();
        if (animation == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for duplicate-frame detection.", new GifDiagnosticPosition(-1, -1, "Optimizer"));
            return result;
        }

        return DetectDuplicateFramesInArray(animation.Frames);
    }

    internal GifDuplicateFrameDetectionResult DetectDuplicateFramesInArray(GifAnimationFrame[] frames)
    {
        GifDuplicateFrameDetectionResult result = new GifDuplicateFrameDetectionResult();
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        result.DuplicateFrames = new bool[frames.Length];
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                result.Error = validation;
                return result;
            }

            if (frameIndex > 0 && AreDuplicateFrames(frames[frameIndex - 1], frames[frameIndex]))
            {
                result.DuplicateFrames[frameIndex] = true;
                result.DuplicateFrameIndices = AppendIndex(result.DuplicateFrameIndices, frameIndex);
                result.DuplicateCount = result.DuplicateCount + 1;
            }

            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        return result;
    }

    internal bool AreDuplicateFrames(GifAnimationFrame first, GifAnimationFrame second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        return AreDuplicateBitmaps(first.Bitmap, second.Bitmap);
    }

    internal bool AreDuplicateBitmaps(GifBitmap first, GifBitmap second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        if (first.Width != second.Width || first.Height != second.Height)
        {
            return false;
        }

        if (first.RgbaPixels == null || second.RgbaPixels == null)
        {
            return false;
        }

        if (first.RgbaPixels.Length != second.RgbaPixels.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.RgbaPixels.Length)
        {
            if (first.RgbaPixels[index] != second.RgbaPixels[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal GifErrorDiagnostic ValidateAnimationFrame(GifAnimationFrame frame, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for duplicate-frame detection.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        return ValidateBitmap(frame.Bitmap, frameIndex);
    }

    internal GifErrorDiagnostic ValidateBitmap(GifBitmap bitmap, int frameIndex)
    {
        if (bitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame bitmap is missing for duplicate-frame detection.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame bitmap dimensions must be positive for duplicate-frame detection.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        long pixelCount = (long)bitmap.Width * (long)bitmap.Height;
        if (pixelCount > 536870911)
        {
            return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "GIF animation frame bitmap is too large for duplicate-frame detection.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        long expectedLength = pixelCount * 4;

        if (bitmap.RgbaPixels == null || bitmap.RgbaPixels.Length != (int)expectedLength)
        {
            return new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "GIF animation frame RGBA byte count does not match its dimensions.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal int[] AppendIndex(int[] indices, int value)
    {
        if (indices == null)
        {
            indices = new int[0];
        }

        int[] result = new int[indices.Length + 1];
        int index = 0;
        while (index < indices.Length)
        {
            result[index] = indices[index];
            index = index + 1;
        }

        result[index] = value;
        return result;
    }

    internal GifBitmap CropBitmap(GifBitmap bitmap, GifChangedBounds bounds)
    {
        if (bounds == null || !bounds.HasChanges)
        {
            return CreateTransparentPlaceholderBitmap();
        }

        byte[] pixels = new byte[bounds.Width * bounds.Height * 4];
        int y = 0;
        while (y < bounds.Height)
        {
            int x = 0;
            while (x < bounds.Width)
            {
                int sourceX = bounds.Left + x;
                int sourceY = bounds.Top + y;
                int sourceOffset = ((sourceY * bitmap.Width) + sourceX) * 4;
                int destinationOffset = ((y * bounds.Width) + x) * 4;
                pixels[destinationOffset] = bitmap.RgbaPixels[sourceOffset];
                pixels[destinationOffset + 1] = bitmap.RgbaPixels[sourceOffset + 1];
                pixels[destinationOffset + 2] = bitmap.RgbaPixels[sourceOffset + 2];
                pixels[destinationOffset + 3] = bitmap.RgbaPixels[sourceOffset + 3];
                x = x + 1;
            }

            y = y + 1;
        }

        return new GifBitmap(bounds.Width, bounds.Height, pixels);
    }

    internal GifBitmap CreateTransparentPlaceholderBitmap()
    {
        return new GifBitmap(1, 1, new byte[] { 0, 0, 0, 0 });
    }

    internal GifChangedBounds CreateFullBounds(GifBitmap bitmap, int frameIndex)
    {
        GifChangedBounds bounds = new GifChangedBounds();
        bounds.HasChanges = true;
        bounds.Left = 0;
        bounds.Top = 0;
        bounds.Width = bitmap.Width;
        bounds.Height = bitmap.Height;
        bounds.FrameIndex = frameIndex;
        return bounds;
    }

    internal GifChangedBounds FindChangedBounds(GifBitmap previous, GifBitmap current, int frameIndex)
    {
        GifChangedBounds bounds = new GifChangedBounds();
        bounds.FrameIndex = frameIndex;
        int minX = current.Width;
        int minY = current.Height;
        int maxX = -1;
        int maxY = -1;
        int y = 0;
        while (y < current.Height)
        {
            int x = 0;
            while (x < current.Width)
            {
                int offset = ((y * current.Width) + x) * 4;
                if (PixelsDiffer(previous.RgbaPixels, current.RgbaPixels, offset))
                {
                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }

                x = x + 1;
            }

            y = y + 1;
        }

        if (maxX >= minX && maxY >= minY)
        {
            bounds.HasChanges = true;
            bounds.Left = minX;
            bounds.Top = minY;
            bounds.Width = (maxX - minX) + 1;
            bounds.Height = (maxY - minY) + 1;
        }

        return bounds;
    }

    internal bool PixelsDiffer(byte[] previous, byte[] current, int offset)
    {
        if (previous[offset] != current[offset])
        {
            return true;
        }

        if (previous[offset + 1] != current[offset + 1])
        {
            return true;
        }

        if (previous[offset + 2] != current[offset + 2])
        {
            return true;
        }

        if (previous[offset + 3] != current[offset + 3])
        {
            return true;
        }

        return false;
    }

    internal GifErrorDiagnostic ValidateMatchingBitmapDimensions(GifBitmap previous, GifBitmap current, int frameIndex)
    {
        if (previous.Width != current.Width || previous.Height != current.Height)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Coalesced frame dimensions must match for changed-bounds detection.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal int FindZeroDelayFrameToKeep(GifAnimationFrame[] frames)
    {
        if (frames == null)
        {
            return -1;
        }

        int nonZeroCount = 0;
        int lastZeroIndex = -1;
        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            if (frames[frameIndex] != null)
            {
                if (frames[frameIndex].DelayMilliseconds == 0)
                {
                    lastZeroIndex = frameIndex;
                }
                else
                {
                    nonZeroCount = nonZeroCount + 1;
                }
            }

            frameIndex = frameIndex + 1;
        }

        if (nonZeroCount == 0)
        {
            return lastZeroIndex;
        }

        return -1;
    }

    internal GifErrorDiagnostic MergeDelay(GifAnimationFrame target, int delayMilliseconds, int frameIndex)
    {
        if (target == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for duplicate-frame delay merge.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        if (target.DelayMilliseconds < 0 || delayMilliseconds < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "GIF animation frame delay cannot be negative.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        long mergedDelay = (long)target.DelayMilliseconds + delayMilliseconds;
        if (mergedDelay > 2147483647)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Merged GIF animation frame delay exceeds the supported range.", new GifDiagnosticPosition(-1, frameIndex, "Optimizer"));
        }

        target.DelayMilliseconds = (int)mergedDelay;
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifAnimation CopyAnimationShell(GifAnimation animation)
    {
        GifAnimation copy = new GifAnimation();
        copy.Width = animation.Width;
        copy.Height = animation.Height;
        copy.LoopCount = animation.LoopCount;
        copy.TotalDurationMilliseconds = animation.TotalDurationMilliseconds;
        copy.Warnings = CopyWarnings(animation.Warnings);
        return copy;
    }

    internal GifAnimation CopyAnimation(GifAnimation animation)
    {
        GifAnimation copy = CopyAnimationShell(animation);
        GifAnimationFrame[] frames = animation.Frames;
        if (frames == null)
        {
            frames = new GifAnimationFrame[0];
        }

        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifErrorDiagnostic validation = ValidateAnimationFrame(frames[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                return copy;
            }

            copy.Frames = AppendFrame(copy.Frames, CopyAnimationFrame(frames[frameIndex]));
            frameIndex = frameIndex + 1;
        }

        return copy;
    }

    internal GifEncodeOptions NormalizeEncodeOptions(GifEncodeOptions? options)
    {
        if (options == null)
        {
            return new GifEncodeOptions();
        }

        return options;
    }

    internal GifAnimationFrame CopyAnimationFrame(GifAnimationFrame frame)
    {
        GifAnimationFrame copy = new GifAnimationFrame();
        copy.Bitmap = CopyBitmap(frame.Bitmap);
        copy.DelayMilliseconds = frame.DelayMilliseconds;
        copy.DisposalMethod = frame.DisposalMethod;
        copy.SourceLeft = frame.SourceLeft;
        copy.SourceTop = frame.SourceTop;
        copy.SourceWidth = frame.SourceWidth;
        copy.SourceHeight = frame.SourceHeight;
        copy.FrameIndex = frame.FrameIndex;
        return copy;
    }

    internal GifBitmap CopyBitmap(GifBitmap bitmap)
    {
        byte[] rgbaPixels = new byte[bitmap.RgbaPixels.Length];
        int index = 0;
        while (index < bitmap.RgbaPixels.Length)
        {
            rgbaPixels[index] = bitmap.RgbaPixels[index];
            index = index + 1;
        }

        return new GifBitmap(bitmap.Width, bitmap.Height, rgbaPixels);
    }

    internal GifWarningDiagnostic[] CopyWarnings(GifWarningDiagnostic[] warnings)
    {
        if (warnings == null)
        {
            return new GifWarningDiagnostic[0];
        }

        GifWarningDiagnostic[] result = new GifWarningDiagnostic[warnings.Length];
        int index = 0;
        while (index < warnings.Length)
        {
            result[index] = warnings[index];
            index = index + 1;
        }

        return result;
    }

    internal GifWarningDiagnostic[] AppendWarning(GifWarningDiagnostic[] warnings, GifWarningDiagnostic warning)
    {
        if (warnings == null)
        {
            warnings = new GifWarningDiagnostic[0];
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
}
