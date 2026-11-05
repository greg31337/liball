namespace gif;

internal class GifRgbaConversionResult
{
    internal bool Success;
    internal byte[] RgbaPixels;
    internal GifBitmap Bitmap;
    internal GifErrorDiagnostic Error;

    internal GifRgbaConversionResult()
    {
        Success = false;
        RgbaPixels = new byte[0];
        Bitmap = new GifBitmap(0, 0, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifIndexedToRgbaConverter
{
    internal GifRgbaConversionResult ConvertIndexedPixels(byte[] indexedPixels, int width, int height, GifPalette palette)
    {
        return ConvertIndexedPixelsWithTransparency(indexedPixels, width, height, palette, false, -1);
    }

    internal GifRgbaConversionResult ConvertIndexedPixelsWithTransparency(byte[] indexedPixels, int width, int height, GifPalette palette, bool hasTransparency, int transparentColorIndex)
    {
        GifRgbaConversionResult result = ValidateInputs(indexedPixels, width, height, palette);
        if (!result.Success)
        {
            return result;
        }

        byte[] rgbaPixels = new byte[indexedPixels.Length * 4];
        int index = 0;
        while (index < indexedPixels.Length)
        {
            int paletteIndex = indexedPixels[index];
            GifColor color = palette.Colors[paletteIndex];
            int rgbaOffset = index * 4;
            rgbaPixels[rgbaOffset] = color.Red;
            rgbaPixels[rgbaOffset + 1] = color.Green;
            rgbaPixels[rgbaOffset + 2] = color.Blue;
            rgbaPixels[rgbaOffset + 3] = 255;
            index = index + 1;
        }

        if (hasTransparency)
        {
            GifRgbaConversionResult transparency = ApplyTransparency(rgbaPixels, indexedPixels, transparentColorIndex, palette.Count);
            if (!transparency.Success)
            {
                return transparency;
            }

            rgbaPixels = transparency.RgbaPixels;
        }

        result.RgbaPixels = rgbaPixels;
        result.Bitmap = new GifBitmap(width, height, rgbaPixels);
        result.Success = true;
        return result;
    }

    internal GifRgbaConversionResult ConvertFrame(GifFrame frame, GifPalette palette)
    {
        GifRgbaConversionResult result = new GifRgbaConversionResult();
        if (frame == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame is missing for indexed-to-RGBA conversion.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        return ConvertIndexedPixelsWithTransparency(frame.IndexedPixels, frame.Width, frame.Height, palette, frame.HasTransparency, frame.TransparentColorIndex);
    }

    internal GifRgbaConversionResult ConvertDocumentFrame(GifDocument document, int frameIndex, GifDecodeOptions options)
    {
        GifRgbaConversionResult result = new GifRgbaConversionResult();
        if (document == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for RGBA conversion.", new GifDiagnosticPosition(-1, frameIndex, "RgbaConversion"));
            return result;
        }

        GifDecodeOptions activeOptions = options;
        if (activeOptions == null)
        {
            activeOptions = new GifDecodeOptions();
        }

        if (document.Frames != null && frameIndex >= 0 && frameIndex < document.Frames.Length && document.Frames[frameIndex] != null)
        {
            GifFrame frame = document.Frames[frameIndex];
            long rgbaByteCount = (long)frame.Width * (long)frame.Height * 4;
            if (rgbaByteCount > activeOptions.MaxMemoryBytes)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "RGBA frame exceeds the configured memory limit.", new GifDiagnosticPosition(-1, frameIndex, "RgbaConversion"));
                return result;
            }
        }

        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();
        GifActiveColorTableResult active = resolver.ResolveDocumentWithOptions(document, frameIndex, activeOptions);
        if (!active.Success)
        {
            result.Error = active.Error;
            return result;
        }

        if (document.Frames == null || frameIndex < 0 || frameIndex >= document.Frames.Length || document.Frames[frameIndex] == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for RGBA conversion.", new GifDiagnosticPosition(-1, frameIndex, "RgbaConversion"));
            return result;
        }

        GifFrame resolvedFrame = document.Frames[frameIndex];
        return ConvertFrame(resolvedFrame, active.Palette);
    }

    internal GifRgbaConversionResult ApplyTransparency(byte[] rgbaPixels, byte[] indexedPixels, int transparentColorIndex, int paletteCount)
    {
        GifRgbaConversionResult result = new GifRgbaConversionResult();
        if (transparentColorIndex < 0 || transparentColorIndex >= paletteCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the active color table.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        if (rgbaPixels == null || indexedPixels == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA and indexed pixel buffers are required for transparency application.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        if (indexedPixels.Length > 536870911 || rgbaPixels.Length != indexedPixels.Length * 4)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count does not match indexed pixel count for transparency application.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        int index = 0;
        while (index < indexedPixels.Length)
        {
            if (indexedPixels[index] == transparentColorIndex)
            {
                rgbaPixels[(index * 4) + 3] = 0;
            }

            index = index + 1;
        }

        result.Success = true;
        result.RgbaPixels = rgbaPixels;
        return result;
    }

    internal GifRgbaConversionResult ValidateInputs(byte[] indexedPixels, int width, int height, GifPalette palette)
    {
        GifRgbaConversionResult result = new GifRgbaConversionResult();
        if (width <= 0 || height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed image dimensions must be positive for RGBA conversion.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > 536870911)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "RGBA output is too large to allocate.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        if (indexedPixels == null)
        {
            indexedPixels = new byte[0];
        }

        if (indexedPixels.Length != (int)expectedPixelCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Indexed pixel count does not match image dimensions for RGBA conversion.", new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            return result;
        }

        GifActiveColorTableResolver resolver = new GifActiveColorTableResolver();
        GifErrorDiagnostic paletteValidation = resolver.ValidatePalette(palette, "Active color table", -1);
        if (paletteValidation.Code != GifErrorCode.None)
        {
            result.Error = paletteValidation;
            return result;
        }

        GifErrorDiagnostic indexValidation = ValidatePaletteIndices(indexedPixels, palette);
        if (indexValidation.Code != GifErrorCode.None)
        {
            result.Error = indexValidation;
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifErrorDiagnostic ValidatePaletteIndices(byte[] indexedPixels, GifPalette palette)
    {
        if (indexedPixels == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        int index = 0;
        while (index < indexedPixels.Length)
        {
            if (indexedPixels[index] >= palette.Count)
            {
                string message = "Indexed pixel value " + ((int)indexedPixels[index]).ToString() + " exceeds the active color table.";
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, message, new GifDiagnosticPosition(-1, -1, "RgbaConversion"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}
