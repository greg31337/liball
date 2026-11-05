namespace gif;

internal class GifCompositingCanvasResult
{
    internal bool Success;
    internal GifBitmap Canvas;
    internal byte[] RgbaPixels;
    internal GifErrorDiagnostic Error;

    internal GifCompositingCanvasResult()
    {
        Success = false;
        Canvas = new GifBitmap(0, 0, null);
        RgbaPixels = new byte[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifFrameOverlayResult
{
    internal bool Success;
    internal GifBitmap Canvas;
    internal byte[] RgbaPixels;
    internal bool Clipped;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifFrameOverlayResult()
    {
        Success = false;
        Canvas = new GifBitmap(0, 0, null);
        RgbaPixels = new byte[0];
        Clipped = false;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDisposalMethodResolutionResult
{
    internal bool Success;
    internal GifDisposalMethod OriginalDisposalMethod;
    internal GifDisposalMethod EffectiveDisposalMethod;
    internal bool UsedFallback;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifDisposalMethodResolutionResult()
    {
        Success = false;
        OriginalDisposalMethod = GifDisposalMethod.Unspecified;
        EffectiveDisposalMethod = GifDisposalMethod.Unspecified;
        UsedFallback = false;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDisposalApplyResult
{
    internal bool Success;
    internal GifBitmap Canvas;
    internal byte[] RgbaPixels;
    internal GifDisposalMethod EffectiveDisposalMethod;
    internal bool UsedFallback;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifDisposalApplyResult()
    {
        Success = false;
        Canvas = new GifBitmap(0, 0, null);
        RgbaPixels = new byte[0];
        EffectiveDisposalMethod = GifDisposalMethod.Unspecified;
        UsedFallback = false;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifCompositor
{
    internal GifCompositingCanvasResult InitializeCanvasFromDocument(GifDocument? document, GifDecodeOptions? options)
    {
        GifCompositingCanvasResult result = new GifCompositingCanvasResult();
        if (document == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for compositing canvas initialization.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        return InitializeCanvasFromScreen(document.LogicalScreen, document.GlobalColorTable, activeOptions);
    }

    internal GifCompositingCanvasResult InitializeCanvasFromScreen(GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifDecodeOptions options)
    {
        GifCompositingCanvasResult result = ValidateCanvasDimensions(logicalScreen);
        if (!result.Success)
        {
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        long rgbaByteCount = (long)logicalScreen.Width * (long)logicalScreen.Height * 4;
        if (rgbaByteCount > activeOptions.MaxMemoryBytes)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Compositing canvas exceeds the configured memory limit.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        byte[] rgbaPixels = new byte[logicalScreen.Width * logicalScreen.Height * 4];
        if (activeOptions.BackgroundMode == GifBackgroundMode.Transparent)
        {
            result.RgbaPixels = rgbaPixels;
            result.Canvas = new GifBitmap(logicalScreen.Width, logicalScreen.Height, rgbaPixels);
            result.Success = true;
            return result;
        }

        GifRgbaColor background = ResolveConfiguredBackgroundColor(logicalScreen, globalColorTable, activeOptions, result);
        if (!result.Success)
        {
            return result;
        }

        FillCanvas(rgbaPixels, background);
        result.RgbaPixels = rgbaPixels;
        result.Canvas = new GifBitmap(logicalScreen.Width, logicalScreen.Height, rgbaPixels);
        result.Success = true;
        return result;
    }

    internal GifCompositingCanvasResult ValidateCanvasDimensions(GifLogicalScreen logicalScreen)
    {
        GifCompositingCanvasResult result = new GifCompositingCanvasResult();
        if (logicalScreen == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "Logical screen is missing for compositing canvas initialization.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (logicalScreen.Width <= 0 || logicalScreen.Height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "Logical screen dimensions must be positive for compositing canvas initialization.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        long pixelCount = (long)logicalScreen.Width * (long)logicalScreen.Height;
        if (pixelCount > 536870911)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Compositing canvas is too large to allocate.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifColor ResolveBackgroundColor(GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifCompositingCanvasResult result)
    {
        if (globalColorTable == null || globalColorTable.IsEmpty())
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Global color table is required for logical-screen background compositing.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return new GifColor(0, 0, 0);
        }

        GifColorTableValidator validator = new GifColorTableValidator();
        if (!validator.IsValidPalette(globalColorTable))
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Global color table is invalid for compositing canvas initialization.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return new GifColor(0, 0, 0);
        }

        if (logicalScreen.BackgroundColorIndex < 0 || logicalScreen.BackgroundColorIndex >= globalColorTable.Count)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidBackgroundColorIndex, "Logical screen background color index is outside the global color table.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return new GifColor(0, 0, 0);
        }

        result.Success = true;
        return globalColorTable.Colors[logicalScreen.BackgroundColorIndex];
    }

    internal GifRgbaColor ResolveConfiguredBackgroundColor(GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifDecodeOptions options, GifCompositingCanvasResult result)
    {
        GifDecodeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.BackgroundMode == GifBackgroundMode.SolidColor)
        {
            GifRgbaColor solidColor = activeOptions.SolidBackgroundColor;
            if (solidColor == null)
            {
                solidColor = new GifRgbaColor(0, 0, 0, 255);
            }

            result.Success = true;
            return solidColor;
        }

        GifColor paletteColor = ResolveBackgroundColor(logicalScreen, globalColorTable, result);
        return new GifRgbaColor(paletteColor.Red, paletteColor.Green, paletteColor.Blue, 255);
    }

    internal void FillCanvas(byte[] rgbaPixels, GifRgbaColor color)
    {
        if (rgbaPixels == null || color == null)
        {
            return;
        }

        int offset = 0;
        while (offset < rgbaPixels.Length)
        {
            rgbaPixels[offset] = color.Red;
            rgbaPixels[offset + 1] = color.Green;
            rgbaPixels[offset + 2] = color.Blue;
            rgbaPixels[offset + 3] = color.Alpha;
            offset = offset + 4;
        }
    }

    internal GifDecodeOptions NormalizeOptions(GifDecodeOptions? options)
    {
        if (options == null)
        {
            return new GifDecodeOptions();
        }

        return options;
    }

    internal GifFrameOverlayResult OverlayFrameAtFramePosition(GifBitmap canvas, GifBitmap frameBitmap, GifFrame frame)
    {
        GifFrameOverlayResult result = new GifFrameOverlayResult();
        if (frame == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame is missing for compositing overlay.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        return OverlayFrameAtPosition(canvas, frameBitmap, frame.Left, frame.Top);
    }

    internal GifFrameOverlayResult OverlayFrameAtPosition(GifBitmap canvas, GifBitmap frameBitmap, int left, int top)
    {
        GifFrameOverlayResult result = ValidateOverlayInputs(canvas, frameBitmap);
        if (!result.Success)
        {
            return result;
        }

        result.Canvas = canvas;
        result.RgbaPixels = canvas.RgbaPixels;

        bool clipped = IsClipped(canvas.Width, canvas.Height, frameBitmap.Width, frameBitmap.Height, left, top);
        long frameRight = (long)left + frameBitmap.Width;
        long frameBottom = (long)top + frameBitmap.Height;
        int destinationLeft = left;
        if (destinationLeft < 0)
        {
            destinationLeft = 0;
        }

        int destinationTop = top;
        if (destinationTop < 0)
        {
            destinationTop = 0;
        }

        int destinationRight = canvas.Width;
        if (frameRight < destinationRight)
        {
            destinationRight = (int)frameRight;
        }

        int destinationBottom = canvas.Height;
        if (frameBottom < destinationBottom)
        {
            destinationBottom = (int)frameBottom;
        }

        int destinationY = destinationTop;
        while (destinationY < destinationBottom)
        {
            int sourceY = (int)((long)destinationY - top);

            int destinationX = destinationLeft;
            while (destinationX < destinationRight)
            {
                int sourceX = (int)((long)destinationX - left);
                int sourceOffset = ((sourceY * frameBitmap.Width) + sourceX) * 4;
                if (frameBitmap.RgbaPixels[sourceOffset + 3] != 0)
                {
                    int destinationOffset = ((destinationY * canvas.Width) + destinationX) * 4;
                    CopyRgbaPixel(frameBitmap.RgbaPixels, sourceOffset, canvas.RgbaPixels, destinationOffset);
                }

                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }

        if (clipped)
        {
            result.Clipped = true;
            result.Warning = new GifWarningDiagnostic(GifWarningCode.FrameClippedToLogicalScreen, "Frame overlay was clipped to the logical screen.", new GifDiagnosticPosition(-1, -1, "Compositor"));
        }

        result.Success = true;
        return result;
    }

    internal GifFrameOverlayResult ValidateOverlayInputs(GifBitmap canvas, GifBitmap frameBitmap)
    {
        GifFrameOverlayResult result = new GifFrameOverlayResult();
        if (canvas == null || frameBitmap == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Canvas and frame bitmap are required for compositing overlay.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (canvas.Width <= 0 || canvas.Height <= 0 || frameBitmap.Width <= 0 || frameBitmap.Height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Canvas and frame bitmap dimensions must be positive for compositing overlay.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (!HasExpectedRgbaLength(canvas.RgbaPixels, canvas.Width, canvas.Height))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Canvas RGBA byte count does not match its dimensions.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (!HasExpectedRgbaLength(frameBitmap.RgbaPixels, frameBitmap.Width, frameBitmap.Height))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Frame RGBA byte count does not match its dimensions.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal bool HasExpectedRgbaLength(byte[] rgbaPixels, int width, int height)
    {
        if (rgbaPixels == null)
        {
            return false;
        }

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        long pixelCount = (long)width * (long)height;
        if (pixelCount > 536870911)
        {
            return false;
        }

        long expectedLength = pixelCount * 4;

        return rgbaPixels.Length == (int)expectedLength;
    }

    internal bool IsClipped(int canvasWidth, int canvasHeight, int frameWidth, int frameHeight, int left, int top)
    {
        if (left < 0 || top < 0)
        {
            return true;
        }

        long right = (long)left + (long)frameWidth;
        long bottom = (long)top + (long)frameHeight;
        if (right > canvasWidth)
        {
            return true;
        }

        return bottom > canvasHeight;
    }

    internal void CopyRgbaPixel(byte[] source, int sourceOffset, byte[] destination, int destinationOffset)
    {
        destination[destinationOffset] = source[sourceOffset];
        destination[destinationOffset + 1] = source[sourceOffset + 1];
        destination[destinationOffset + 2] = source[sourceOffset + 2];
        destination[destinationOffset + 3] = source[sourceOffset + 3];
    }

    internal GifDisposalMethodResolutionResult ResolveDisposalMethod(GifFrame frame, GifDecodeOptions options)
    {
        GifDisposalMethodResolutionResult result = new GifDisposalMethodResolutionResult();
        if (frame == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame is missing for disposal method resolution.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        result.OriginalDisposalMethod = frame.DisposalMethod;
        if (IsReservedDisposalMethod(frame.DisposalMethod))
        {
            result.EffectiveDisposalMethod = NormalizeDisposalFallback(activeOptions.DisposalFallback);
            result.UsedFallback = true;
            result.Warning = new GifWarningDiagnostic(GifWarningCode.ReservedDisposalMethod, "Reserved disposal method was replaced with the configured fallback.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            result.Success = true;
            return result;
        }

        if (frame.DisposalMethod != GifDisposalMethod.Unspecified)
        {
            result.EffectiveDisposalMethod = frame.DisposalMethod;
            result.Success = true;
            return result;
        }

        result.EffectiveDisposalMethod = NormalizeDisposalFallback(activeOptions.DisposalFallback);
        result.UsedFallback = true;
        result.Success = true;
        return result;
    }

    internal GifDisposalMethod NormalizeDisposalFallback(GifDisposalMethod fallback)
    {
        if (fallback == GifDisposalMethod.Unspecified || IsReservedDisposalMethod(fallback))
        {
            return GifDisposalMethod.DoNotDispose;
        }

        return fallback;
    }

    internal bool IsReservedDisposalMethod(GifDisposalMethod disposalMethod)
    {
        return disposalMethod == GifDisposalMethod.Reserved4 || disposalMethod == GifDisposalMethod.Reserved5 || disposalMethod == GifDisposalMethod.Reserved6 || disposalMethod == GifDisposalMethod.Reserved7;
    }

    internal GifDisposalApplyResult ApplyDisposalWithDefaults(GifBitmap canvas, GifFrame frame, GifDecodeOptions options)
    {
        return ApplyDisposalWithScreen(canvas, frame, new GifLogicalScreen(0, 0), new GifPalette(null), options);
    }

    internal GifDisposalApplyResult ApplyDisposalWithScreen(GifBitmap canvas, GifFrame frame, GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifDecodeOptions options)
    {
        return ApplyDisposalWithPreviousCanvas(canvas, frame, logicalScreen, globalColorTable, new GifBitmap(0, 0, null), options);
    }

    internal GifDisposalApplyResult ApplyDisposalWithPreviousCanvas(GifBitmap canvas, GifFrame frame, GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifBitmap previousCanvas, GifDecodeOptions options)
    {
        GifDisposalApplyResult result = ValidateDisposalCanvas(canvas);
        if (!result.Success)
        {
            return result;
        }

        GifDisposalMethodResolutionResult disposal = ResolveDisposalMethod(frame, options);
        if (!disposal.Success)
        {
            result.Error = disposal.Error;
            return result;
        }

        result.EffectiveDisposalMethod = disposal.EffectiveDisposalMethod;
        result.UsedFallback = disposal.UsedFallback;
        result.Warning = disposal.Warning;
        if (disposal.EffectiveDisposalMethod == GifDisposalMethod.DoNotDispose)
        {
            GifDisposalApplyResult applied = ApplyDoNotDispose(canvas);
            applied.EffectiveDisposalMethod = disposal.EffectiveDisposalMethod;
            applied.UsedFallback = disposal.UsedFallback;
            applied.Warning = disposal.Warning;
            return applied;
        }

        if (disposal.EffectiveDisposalMethod == GifDisposalMethod.RestoreToBackground)
        {
            GifDisposalApplyResult applied = ApplyRestoreToBackground(canvas, frame, logicalScreen, globalColorTable, options);
            applied.EffectiveDisposalMethod = disposal.EffectiveDisposalMethod;
            applied.UsedFallback = disposal.UsedFallback;
            if (disposal.Warning.Code != GifWarningCode.None)
            {
                applied.Warning = disposal.Warning;
            }

            return applied;
        }

        if (disposal.EffectiveDisposalMethod == GifDisposalMethod.RestoreToPrevious)
        {
            GifDisposalApplyResult applied = ApplyRestoreToPrevious(canvas, previousCanvas);
            applied.EffectiveDisposalMethod = disposal.EffectiveDisposalMethod;
            applied.UsedFallback = disposal.UsedFallback;
            applied.Warning = disposal.Warning;
            return applied;
        }

        result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Disposal method is not implemented for compositing.", new GifDiagnosticPosition(-1, -1, "Compositor"));
        return result;
    }

    internal GifDisposalApplyResult ApplyDoNotDispose(GifBitmap canvas)
    {
        GifDisposalApplyResult result = ValidateDisposalCanvas(canvas);
        if (!result.Success)
        {
            return result;
        }

        result.Success = true;
        result.Canvas = canvas;
        result.RgbaPixels = canvas.RgbaPixels;
        result.EffectiveDisposalMethod = GifDisposalMethod.DoNotDispose;
        return result;
    }

    internal GifDisposalApplyResult ApplyRestoreToBackground(GifBitmap canvas, GifFrame frame, GifLogicalScreen logicalScreen, GifPalette globalColorTable, GifDecodeOptions options)
    {
        GifDisposalApplyResult result = ValidateDisposalCanvas(canvas);
        if (!result.Success)
        {
            return result;
        }

        if (frame == null || frame.Width <= 0 || frame.Height <= 0)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame rectangle is invalid for restore-to-background disposal.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        bool clipped = IsClipped(canvas.Width, canvas.Height, frame.Width, frame.Height, frame.Left, frame.Top);
        byte red = 0;
        byte green = 0;
        byte blue = 0;
        byte alpha = 0;

        if (activeOptions.BackgroundMode != GifBackgroundMode.Transparent)
        {
            GifCompositingCanvasResult backgroundResult = new GifCompositingCanvasResult();
            GifRgbaColor background = ResolveConfiguredBackgroundColor(logicalScreen, globalColorTable, activeOptions, backgroundResult);
            if (!backgroundResult.Success)
            {
                result.Success = false;
                result.Error = backgroundResult.Error;
                return result;
            }

            red = background.Red;
            green = background.Green;
            blue = background.Blue;
            alpha = background.Alpha;
        }

        FillRectangle(canvas, frame.Left, frame.Top, frame.Width, frame.Height, red, green, blue, alpha);
        result.Success = true;
        result.Canvas = canvas;
        result.RgbaPixels = canvas.RgbaPixels;
        result.EffectiveDisposalMethod = GifDisposalMethod.RestoreToBackground;
        if (clipped)
        {
            result.Warning = new GifWarningDiagnostic(GifWarningCode.FrameClippedToLogicalScreen, "Restore-to-background disposal was clipped to the logical screen.", new GifDiagnosticPosition(-1, -1, "Compositor"));
        }

        return result;
    }

    internal GifDisposalApplyResult CapturePreviousCanvas(GifBitmap canvas)
    {
        GifDisposalApplyResult result = ValidateDisposalCanvas(canvas);
        if (!result.Success)
        {
            return result;
        }

        byte[] pixels = new byte[canvas.RgbaPixels.Length];
        int index = 0;
        while (index < canvas.RgbaPixels.Length)
        {
            pixels[index] = canvas.RgbaPixels[index];
            index = index + 1;
        }

        result.Success = true;
        result.Canvas = new GifBitmap(canvas.Width, canvas.Height, pixels);
        result.RgbaPixels = pixels;
        return result;
    }

    internal GifDisposalApplyResult ApplyRestoreToPrevious(GifBitmap canvas, GifBitmap previousCanvas)
    {
        GifDisposalApplyResult result = ValidateDisposalCanvas(canvas);
        if (!result.Success)
        {
            return result;
        }

        GifDisposalApplyResult previousResult = ValidateDisposalCanvas(previousCanvas);
        if (!previousResult.Success)
        {
            result.Success = false;
            result.Error = previousResult.Error;
            return result;
        }

        if (canvas.Width != previousCanvas.Width || canvas.Height != previousCanvas.Height)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Previous canvas dimensions do not match the active canvas.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        int index = 0;
        while (index < canvas.RgbaPixels.Length)
        {
            canvas.RgbaPixels[index] = previousCanvas.RgbaPixels[index];
            index = index + 1;
        }

        result.Success = true;
        result.Canvas = canvas;
        result.RgbaPixels = canvas.RgbaPixels;
        result.EffectiveDisposalMethod = GifDisposalMethod.RestoreToPrevious;
        return result;
    }

    internal void FillRectangle(GifBitmap canvas, int left, int top, int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        if (canvas == null)
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        long rectangleRight = (long)left + width;
        long rectangleBottom = (long)top + height;
        int destinationLeft = left;
        if (destinationLeft < 0)
        {
            destinationLeft = 0;
        }

        int destinationTop = top;
        if (destinationTop < 0)
        {
            destinationTop = 0;
        }

        int destinationRight = canvas.Width;
        if (rectangleRight < destinationRight)
        {
            destinationRight = (int)rectangleRight;
        }

        int destinationBottom = canvas.Height;
        if (rectangleBottom < destinationBottom)
        {
            destinationBottom = (int)rectangleBottom;
        }

        int destinationY = destinationTop;
        while (destinationY < destinationBottom)
        {
            int destinationX = destinationLeft;
            while (destinationX < destinationRight)
            {
                int destinationOffset = ((destinationY * canvas.Width) + destinationX) * 4;
                canvas.RgbaPixels[destinationOffset] = red;
                canvas.RgbaPixels[destinationOffset + 1] = green;
                canvas.RgbaPixels[destinationOffset + 2] = blue;
                canvas.RgbaPixels[destinationOffset + 3] = alpha;
                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }
    }

    internal GifDisposalApplyResult ValidateDisposalCanvas(GifBitmap canvas)
    {
        GifDisposalApplyResult result = new GifDisposalApplyResult();
        if (canvas == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Canvas is missing for disposal application.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (canvas.Width <= 0 || canvas.Height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Canvas dimensions must be positive for disposal application.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        if (!HasExpectedRgbaLength(canvas.RgbaPixels, canvas.Width, canvas.Height))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Canvas RGBA byte count does not match its dimensions for disposal application.", new GifDiagnosticPosition(-1, -1, "Compositor"));
            return result;
        }

        result.Success = true;
        result.Canvas = canvas;
        result.RgbaPixels = canvas.RgbaPixels;
        return result;
    }
}
