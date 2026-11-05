namespace gif;

internal class GifWebSafePaletteGenerator : IGifQuantizationStrategy
{
    internal GifPaletteBuildResult Generate()
    {
        return GenerateWithOptions(new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult GenerateColorCount(int colorCount)
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = colorCount;
        return GenerateWithOptions(options);
    }

    internal GifPaletteBuildResult GenerateWithOptions(GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic validation = ValidateOptions(activeOptions);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return CreatePalette(DetermineWebSafeColorCount(activeOptions.MaxColors), false, 0);
    }

    internal GifPaletteBuildResult QuantizeBitmap(GifBitmap bitmap)
    {
        return QuantizeBitmapWithOptions(bitmap, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeBitmapWithOptions(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic validation = builder.ValidateBitmap(bitmap);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return QuantizeRgbaPixelsWithOptions(bitmap.RgbaPixels, options);
    }

    internal GifPaletteBuildResult QuantizeRgbaPixels(byte[] rgbaPixels)
    {
        return QuantizeRgbaPixelsWithOptions(rgbaPixels, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for web-safe palette generation.", new GifDiagnosticPosition(-1, -1, "WebSafePalette"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic validation = ValidateOptions(activeOptions);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        int transparentPixelCount = CountTransparentPixels(rgbaPixels, activeOptions);
        int opaquePixelCount = (rgbaPixels.Length / 4) - transparentPixelCount;
        bool hasTransparency = transparentPixelCount > 0;
        int paletteSlots = activeOptions.MaxColors;
        if (hasTransparency)
        {
            paletteSlots = paletteSlots - 1;
        }

        if (paletteSlots <= 0 && opaquePixelCount > 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Web-safe palette has no slots for opaque colors.", new GifDiagnosticPosition(-1, -1, "WebSafePalette"));
            return result;
        }

        return CreatePalette(DetermineWebSafeColorCount(paletteSlots), hasTransparency, transparentPixelCount);
    }

    internal GifPaletteBuildResult CreatePalette(int webSafeColorCount, bool hasTransparency, int transparentPixelCount)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        int outputColorCount = webSafeColorCount;
        if (hasTransparency)
        {
            outputColorCount = outputColorCount + 1;
        }

        GifColor[] colors = new GifColor[outputColorCount];
        int outputIndex = 0;
        if (hasTransparency)
        {
            colors[0] = new GifColor(0, 0, 0);
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            result.TransparentPixelCount = transparentPixelCount;
            outputIndex = 1;
        }

        int index = 0;
        while (index < webSafeColorCount)
        {
            colors[outputIndex] = CreateWebSafeColor(index);
            outputIndex = outputIndex + 1;
            index = index + 1;
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(new GifPalette(colors));
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = outputColorCount;
        result.Exact = false;
        result.Success = true;
        return result;
    }

    internal GifColor CreateWebSafeColor(int index)
    {
        int redIndex = index / 36;
        int remainder = index - (redIndex * 36);
        int greenIndex = remainder / 6;
        int blueIndex = remainder - (greenIndex * 6);
        return new GifColor((byte)(redIndex * 51), (byte)(greenIndex * 51), (byte)(blueIndex * 51));
    }

    internal int DetermineWebSafeColorCount(int maxColors)
    {
        if (maxColors > 216)
        {
            return 216;
        }

        return maxColors;
    }

    internal int CountTransparentPixels(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        int count = 0;
        int offset = 0;
        while (offset < rgbaPixels.Length)
        {
            if (builder.IsTransparentPixel(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2], rgbaPixels[offset + 3], options))
            {
                count = count + 1;
            }

            offset = offset + 4;
        }

        return count;
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }

    internal GifErrorDiagnostic ValidateOptions(GifQuantizeOptions options)
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        return builder.ValidateOptions(options);
    }
}
