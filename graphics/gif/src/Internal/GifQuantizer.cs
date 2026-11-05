namespace gif;

internal interface IGifQuantizationStrategy
{
    GifPaletteBuildResult QuantizeBitmapWithOptions(GifBitmap bitmap, GifQuantizeOptions options);
    GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options);
}

internal class GifExactQuantizer : IGifQuantizationStrategy
{
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

    public GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteBuildResult exact = builder.BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);
        if (!exact.Success)
        {
            return exact;
        }

        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(exact.Palette);
        if (!compacted.Success)
        {
            exact.Success = false;
            exact.Error = compacted.Error;
            return exact;
        }

        exact.Palette = compacted.Palette;
        return exact;
    }
}

internal class GifCallerPaletteQuantizer : IGifQuantizationStrategy
{
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

    public GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for caller-palette quantization.", new GifDiagnosticPosition(-1, -1, "CallerPalette"));
            return result;
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic validation = builder.ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(options.CallerPalette);
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        int transparentPixelCount = CountTransparentPixels(rgbaPixels, options);
        int transparentColorIndex = -1;
        if (transparentPixelCount > 0)
        {
            transparentColorIndex = ResolveTransparentColorIndex(compacted.Palette, options);
            if (transparentColorIndex < 0)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Caller palette does not provide a compatible transparent color index.", new GifDiagnosticPosition(-1, -1, "CallerPalette"));
                return result;
            }
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = options.CallerPalette.Count;
        result.TransparentPixelCount = transparentPixelCount;
        result.TransparentColorIndex = transparentColorIndex;
        result.HasTransparency = transparentPixelCount > 0;
        result.Exact = false;
        result.Success = true;
        return result;
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

    internal int ResolveTransparentColorIndex(GifPalette palette, GifQuantizeOptions options)
    {
        int colorIndex = options.TransparentColorIndex;
        if (options.TransparentColorMode == GifTransparentColorMode.AlphaThreshold)
        {
            return colorIndex;
        }

        GifRgbaColor exactColor = options.ExactTransparentColor;
        if (exactColor == null)
        {
            return -1;
        }

        if (colorIndex >= 0)
        {
            GifColor indexedColor = palette.Colors[colorIndex];
            if (indexedColor.Red == exactColor.Red && indexedColor.Green == exactColor.Green && indexedColor.Blue == exactColor.Blue)
            {
                return colorIndex;
            }

            return -1;
        }

        int index = 0;
        while (index < palette.Count)
        {
            GifColor candidate = palette.Colors[index];
            if (candidate.Red == exactColor.Red && candidate.Green == exactColor.Green && candidate.Blue == exactColor.Blue)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}

internal class GifQuantizer
{
    internal GifPaletteBuildResult QuantizeBitmap(GifBitmap bitmap)
    {
        return QuantizeBitmapWithOptions(bitmap, new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult QuantizeBitmapWithOptions(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        IGifQuantizationStrategy? strategy = CreateStrategy(activeOptions.QuantizerMode);
        if (strategy == null)
        {
            return CreateUnsupportedResult();
        }

        GifPaletteBuildResult result = strategy.QuantizeBitmapWithOptions(bitmap, activeOptions);
        return ApplyPaletteControls(result, activeOptions);
    }

    internal GifPaletteBuildResult QuantizeRgbaPixels(byte[] rgbaPixels)
    {
        return QuantizeRgbaPixelsWithOptions(rgbaPixels, new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        IGifQuantizationStrategy? strategy = CreateStrategy(activeOptions.QuantizerMode);
        if (strategy == null)
        {
            return CreateUnsupportedResult();
        }

        GifPaletteBuildResult result = strategy.QuantizeRgbaPixelsWithOptions(rgbaPixels, activeOptions);
        return ApplyPaletteControls(result, activeOptions);
    }

    internal IGifQuantizationStrategy? CreateStrategy(GifQuantizerMode mode)
    {
        if (mode == GifQuantizerMode.MedianCut)
        {
            return new GifMedianCutQuantizer();
        }

        if (mode == GifQuantizerMode.Octree)
        {
            return new GifOctreeQuantizer();
        }

        if (mode == GifQuantizerMode.Grayscale)
        {
            return new GifGrayscalePaletteGenerator();
        }

        if (mode == GifQuantizerMode.WebSafe)
        {
            return new GifWebSafePaletteGenerator();
        }

        if (mode == GifQuantizerMode.Exact)
        {
            return new GifExactQuantizer();
        }

        if (mode == GifQuantizerMode.CallerPalette)
        {
            return new GifCallerPaletteQuantizer();
        }

        return null;
    }

    internal GifPaletteBuildResult CreateUnsupportedResult()
    {
        GifPaletteBuildResult unsupported = new GifPaletteBuildResult();
        unsupported.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Requested quantizer mode is not implemented.", new GifDiagnosticPosition(-1, -1, "Quantizer"));
        return unsupported;
    }

    internal GifPaletteBuildResult ApplyPaletteControls(GifPaletteBuildResult result, GifQuantizeOptions options)
    {
        if (result == null)
        {
            GifPaletteBuildResult missing = new GifPaletteBuildResult();
            missing.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Quantization result is missing.", new GifDiagnosticPosition(-1, -1, "Quantizer"));
            return missing;
        }

        if (!result.Success)
        {
            return result;
        }

        if (result.HasTransparency && options.TransparentColorMode == GifTransparentColorMode.ExactColor)
        {
            GifRgbaColor exactColor = options.ExactTransparentColor;
            if (exactColor == null || result.TransparentColorIndex < 0 || result.TransparentColorIndex >= result.Palette.Count)
            {
                result.Success = false;
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Exact transparent color has no palette entry.", new GifDiagnosticPosition(-1, -1, "Quantizer"));
                return result;
            }

            result.Palette.Colors[result.TransparentColorIndex] = new GifColor(exactColor.Red, exactColor.Green, exactColor.Blue);
        }

        if (!options.PreservePaletteOrder)
        {
            SortPaletteResult(result);
        }

        return result;
    }

    internal void SortPaletteResult(GifPaletteBuildResult result)
    {
        int index = 0;
        while (index < result.Palette.Count)
        {
            int bestIndex = index;
            int candidateIndex = index + 1;
            while (candidateIndex < result.Palette.Count)
            {
                if (CompareColors(result.Palette.Colors[candidateIndex], result.Palette.Colors[bestIndex]) < 0)
                {
                    bestIndex = candidateIndex;
                }

                candidateIndex = candidateIndex + 1;
            }

            if (bestIndex != index)
            {
                GifColor swap = result.Palette.Colors[index];
                result.Palette.Colors[index] = result.Palette.Colors[bestIndex];
                result.Palette.Colors[bestIndex] = swap;
                if (result.TransparentColorIndex == index)
                {
                    result.TransparentColorIndex = bestIndex;
                }
                else if (result.TransparentColorIndex == bestIndex)
                {
                    result.TransparentColorIndex = index;
                }
            }

            index = index + 1;
        }
    }

    internal int CompareColors(GifColor left, GifColor right)
    {
        if (left.Red != right.Red)
        {
            return (int)left.Red - right.Red;
        }

        if (left.Green != right.Green)
        {
            return (int)left.Green - right.Green;
        }

        return (int)left.Blue - right.Blue;
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions? options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }
}
