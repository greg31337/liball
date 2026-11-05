namespace gif;

internal class GifGrayscalePaletteGenerator : IGifQuantizationStrategy
{
    internal GifPaletteBuildResult Generate()
    {
        return GenerateColorCount(GifConstants.MaximumColorTableSize);
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

        return CreatePalette(activeOptions.MaxColors, false, 0);
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
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for grayscale palette generation.", new GifDiagnosticPosition(-1, -1, "GrayscalePalette"));
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
        int grayscaleColorCount = activeOptions.MaxColors;
        if (hasTransparency)
        {
            grayscaleColorCount = grayscaleColorCount - 1;
        }

        if (grayscaleColorCount <= 0 && opaquePixelCount > 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Grayscale palette has no slots for opaque colors.", new GifDiagnosticPosition(-1, -1, "GrayscalePalette"));
            return result;
        }

        return CreatePalette(grayscaleColorCount, hasTransparency, transparentPixelCount);
    }

    internal GifPaletteBuildResult CreatePalette(int grayscaleColorCount, bool hasTransparency, int transparentPixelCount)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        int outputColorCount = grayscaleColorCount;
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
        while (index < grayscaleColorCount)
        {
            byte shade = CalculateShade(index, grayscaleColorCount);
            colors[outputIndex] = new GifColor(shade, shade, shade);
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

    internal byte CalculateShade(int index, int colorCount)
    {
        if (colorCount <= 1)
        {
            return 0;
        }

        if (index <= 0)
        {
            return 0;
        }

        if (index >= colorCount - 1)
        {
            return 255;
        }

        long numerator = ((long)index * 255) + ((colorCount - 1) / 2);
        return (byte)(numerator / (colorCount - 1));
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
