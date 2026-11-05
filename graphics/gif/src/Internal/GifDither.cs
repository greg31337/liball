namespace gif;

internal class GifIndexedColorMappingResult
{
    internal bool Success;
    internal byte[] IndexedPixels;
    internal GifBitmap Bitmap;
    internal int TransparentPixelCount;
    internal GifErrorDiagnostic Error;

    internal GifIndexedColorMappingResult()
    {
        Success = false;
        IndexedPixels = new byte[0];
        Bitmap = new GifBitmap(0, 0, null);
        TransparentPixelCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDither
{
    internal GifIndexedColorMappingResult Map(GifBitmap bitmap, GifPaletteBuildResult paletteResult)
    {
        return MapWithOptions(bitmap, paletteResult, new GifQuantizeOptions());
    }

    internal GifIndexedColorMappingResult MapWithOptions(GifBitmap bitmap, GifPaletteBuildResult paletteResult, GifQuantizeOptions options)
    {
        GifIndexedColorMappingResult result = new GifIndexedColorMappingResult();
        if (paletteResult == null || !paletteResult.Success)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Successful palette result is required for indexed color mapping.", new GifDiagnosticPosition(-1, -1, "Dither"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.DitherMode == GifDitherMode.None)
        {
            return MapNoDitherWithOptions(bitmap, paletteResult.Palette, activeOptions, paletteResult.TransparentColorIndex);
        }

        if (activeOptions.DitherMode == GifDitherMode.FloydSteinberg)
        {
            return MapFloydSteinbergWithOptions(bitmap, paletteResult.Palette, activeOptions, paletteResult.TransparentColorIndex);
        }

        if (activeOptions.DitherMode == GifDitherMode.OrderedBayer)
        {
            return MapOrderedBayerWithOptions(bitmap, paletteResult.Palette, activeOptions, paletteResult.TransparentColorIndex);
        }

        result.Error = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Requested dither mode is not implemented.", new GifDiagnosticPosition(-1, -1, "Dither"));
        return result;
    }

    internal GifIndexedColorMappingResult MapNoDither(GifBitmap bitmap, GifPalette palette)
    {
        return MapNoDitherWithOptions(bitmap, palette, new GifQuantizeOptions(), -1);
    }

    internal GifIndexedColorMappingResult MapNoDitherWithOptions(GifBitmap bitmap, GifPalette palette, GifQuantizeOptions options, int transparentColorIndex)
    {
        GifIndexedColorMappingResult result = new GifIndexedColorMappingResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic bitmapValidation = builder.ValidateBitmap(bitmap);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            result.Error = bitmapValidation;
            return result;
        }

        GifErrorDiagnostic paletteValidation = builder.ValidateLookupPalette(palette);
        if (paletteValidation.Code != GifErrorCode.None)
        {
            result.Error = paletteValidation;
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = builder.ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        if (transparentColorIndex < -1 || transparentColorIndex >= palette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the palette for indexed color mapping.", new GifDiagnosticPosition(-1, -1, "Dither"));
            return result;
        }

        byte[] indexedPixels = new byte[bitmap.RgbaPixels.Length / 4];
        int pixelIndex = 0;
        int offset = 0;
        while (offset < bitmap.RgbaPixels.Length)
        {
            if (transparentColorIndex >= 0 && builder.IsTransparentPixel(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], bitmap.RgbaPixels[offset + 3], activeOptions))
            {
                indexedPixels[pixelIndex] = (byte)transparentColorIndex;
                result.TransparentPixelCount = result.TransparentPixelCount + 1;
            }
            else
            {
                GifPaletteLookupResult nearest = builder.FindNearestRgbColorExcludingIndex(palette, bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], transparentColorIndex);
                if (!nearest.Success)
                {
                    result.Error = nearest.Error;
                    return result;
                }

                indexedPixels[pixelIndex] = (byte)nearest.ColorIndex;
            }

            pixelIndex = pixelIndex + 1;
            offset = offset + 4;
        }

        result.IndexedPixels = indexedPixels;
        result.Bitmap = GifBitmap.CreateIndexed(bitmap.Width, bitmap.Height, indexedPixels, palette);
        result.Success = true;
        return result;
    }

    internal GifIndexedColorMappingResult MapFloydSteinberg(GifBitmap bitmap, GifPalette palette)
    {
        return MapFloydSteinbergWithOptions(bitmap, palette, new GifQuantizeOptions(), -1);
    }

    internal GifIndexedColorMappingResult MapFloydSteinbergWithOptions(GifBitmap bitmap, GifPalette palette, GifQuantizeOptions options, int transparentColorIndex)
    {
        GifIndexedColorMappingResult result = new GifIndexedColorMappingResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic bitmapValidation = builder.ValidateBitmap(bitmap);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            result.Error = bitmapValidation;
            return result;
        }

        GifErrorDiagnostic paletteValidation = builder.ValidateLookupPalette(palette);
        if (paletteValidation.Code != GifErrorCode.None)
        {
            result.Error = paletteValidation;
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = builder.ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        if (transparentColorIndex < -1 || transparentColorIndex >= palette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the palette for Floyd-Steinberg color mapping.", new GifDiagnosticPosition(-1, -1, "Dither"));
            return result;
        }

        byte[] indexedPixels = new byte[bitmap.RgbaPixels.Length / 4];
        int[] redErrors = new int[indexedPixels.Length];
        int[] greenErrors = new int[indexedPixels.Length];
        int[] blueErrors = new int[indexedPixels.Length];

        int y = 0;
        while (y < bitmap.Height)
        {
            int x = 0;
            while (x < bitmap.Width)
            {
                int pixelIndex = (y * bitmap.Width) + x;
                int offset = pixelIndex * 4;
                if (transparentColorIndex >= 0 && builder.IsTransparentPixel(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], bitmap.RgbaPixels[offset + 3], activeOptions))
                {
                    indexedPixels[pixelIndex] = (byte)transparentColorIndex;
                    result.TransparentPixelCount = result.TransparentPixelCount + 1;
                }
                else
                {
                    byte red = ClampToByte(bitmap.RgbaPixels[offset] + (redErrors[pixelIndex] / 16));
                    byte green = ClampToByte(bitmap.RgbaPixels[offset + 1] + (greenErrors[pixelIndex] / 16));
                    byte blue = ClampToByte(bitmap.RgbaPixels[offset + 2] + (blueErrors[pixelIndex] / 16));
                    GifPaletteLookupResult nearest = builder.FindNearestRgbColorExcludingIndex(palette, red, green, blue, transparentColorIndex);
                    if (!nearest.Success)
                    {
                        result.Error = nearest.Error;
                        return result;
                    }

                    indexedPixels[pixelIndex] = (byte)nearest.ColorIndex;
                    int redError = red - nearest.Color.Red;
                    int greenError = green - nearest.Color.Green;
                    int blueError = blue - nearest.Color.Blue;
                    DiffuseFloydSteinbergError(redErrors, greenErrors, blueErrors, bitmap.Width, bitmap.Height, x, y, redError, greenError, blueError);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        result.IndexedPixels = indexedPixels;
        result.Bitmap = GifBitmap.CreateIndexed(bitmap.Width, bitmap.Height, indexedPixels, palette);
        result.Success = true;
        return result;
    }

    internal GifIndexedColorMappingResult MapOrderedBayer(GifBitmap bitmap, GifPalette palette)
    {
        return MapOrderedBayerWithOptions(bitmap, palette, new GifQuantizeOptions(), -1);
    }

    internal GifIndexedColorMappingResult MapOrderedBayerWithOptions(GifBitmap bitmap, GifPalette palette, GifQuantizeOptions options, int transparentColorIndex)
    {
        GifIndexedColorMappingResult result = new GifIndexedColorMappingResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic bitmapValidation = builder.ValidateBitmap(bitmap);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            result.Error = bitmapValidation;
            return result;
        }

        GifErrorDiagnostic paletteValidation = builder.ValidateLookupPalette(palette);
        if (paletteValidation.Code != GifErrorCode.None)
        {
            result.Error = paletteValidation;
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = builder.ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        if (transparentColorIndex < -1 || transparentColorIndex >= palette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the palette for ordered Bayer color mapping.", new GifDiagnosticPosition(-1, -1, "Dither"));
            return result;
        }

        byte[] indexedPixels = new byte[bitmap.RgbaPixels.Length / 4];
        int y = 0;
        while (y < bitmap.Height)
        {
            int x = 0;
            while (x < bitmap.Width)
            {
                int pixelIndex = (y * bitmap.Width) + x;
                int offset = pixelIndex * 4;
                if (transparentColorIndex >= 0 && builder.IsTransparentPixel(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], bitmap.RgbaPixels[offset + 3], activeOptions))
                {
                    indexedPixels[pixelIndex] = (byte)transparentColorIndex;
                    result.TransparentPixelCount = result.TransparentPixelCount + 1;
                }
                else
                {
                    int adjustment = CalculateOrderedBayerAdjustment(x, y);
                    byte red = ClampToByte(bitmap.RgbaPixels[offset] + adjustment);
                    byte green = ClampToByte(bitmap.RgbaPixels[offset + 1] + adjustment);
                    byte blue = ClampToByte(bitmap.RgbaPixels[offset + 2] + adjustment);
                    GifPaletteLookupResult nearest = builder.FindNearestRgbColorExcludingIndex(palette, red, green, blue, transparentColorIndex);
                    if (!nearest.Success)
                    {
                        result.Error = nearest.Error;
                        return result;
                    }

                    indexedPixels[pixelIndex] = (byte)nearest.ColorIndex;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        result.IndexedPixels = indexedPixels;
        result.Bitmap = GifBitmap.CreateIndexed(bitmap.Width, bitmap.Height, indexedPixels, palette);
        result.Success = true;
        return result;
    }

    internal int CalculateOrderedBayerAdjustment(int x, int y)
    {
        return (GetBayer4Value(x, y) * 17) - 128;
    }

    internal int GetBayer4Value(int x, int y)
    {
        int matrixX = x & 3;
        int matrixY = y & 3;
        if (matrixY == 0)
        {
            if (matrixX == 0)
            {
                return 0;
            }

            if (matrixX == 1)
            {
                return 8;
            }

            if (matrixX == 2)
            {
                return 2;
            }

            return 10;
        }

        if (matrixY == 1)
        {
            if (matrixX == 0)
            {
                return 12;
            }

            if (matrixX == 1)
            {
                return 4;
            }

            if (matrixX == 2)
            {
                return 14;
            }

            return 6;
        }

        if (matrixY == 2)
        {
            if (matrixX == 0)
            {
                return 3;
            }

            if (matrixX == 1)
            {
                return 11;
            }

            if (matrixX == 2)
            {
                return 1;
            }

            return 9;
        }

        if (matrixX == 0)
        {
            return 15;
        }

        if (matrixX == 1)
        {
            return 7;
        }

        if (matrixX == 2)
        {
            return 13;
        }

        return 5;
    }

    internal void DiffuseFloydSteinbergError(int[] redErrors, int[] greenErrors, int[] blueErrors, int width, int height, int x, int y, int redError, int greenError, int blueError)
    {
        AddFloydSteinbergError(redErrors, greenErrors, blueErrors, width, height, x + 1, y, redError, greenError, blueError, 7);
        AddFloydSteinbergError(redErrors, greenErrors, blueErrors, width, height, x - 1, y + 1, redError, greenError, blueError, 3);
        AddFloydSteinbergError(redErrors, greenErrors, blueErrors, width, height, x, y + 1, redError, greenError, blueError, 5);
        AddFloydSteinbergError(redErrors, greenErrors, blueErrors, width, height, x + 1, y + 1, redError, greenError, blueError, 1);
    }

    internal void AddFloydSteinbergError(int[] redErrors, int[] greenErrors, int[] blueErrors, int width, int height, int x, int y, int redError, int greenError, int blueError, int weight)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int pixelIndex = (y * width) + x;
        redErrors[pixelIndex] = redErrors[pixelIndex] + (redError * weight);
        greenErrors[pixelIndex] = greenErrors[pixelIndex] + (greenError * weight);
        blueErrors[pixelIndex] = blueErrors[pixelIndex] + (blueError * weight);
    }

    internal byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }
}
