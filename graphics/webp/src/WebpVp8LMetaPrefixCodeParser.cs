using System;

namespace webp;

internal static class WebpVp8LMetaPrefixCodeParser
{
    internal static WebpVp8LMetaPrefixParseResult Parse(WebpVp8LBitReader reader, int width, int height, int colorCacheSize)
    {
        return ParseCore(reader, width, height, colorCacheSize, null);
    }

    internal static WebpVp8LMetaPrefixParseResult Parse(WebpVp8LBitReader reader, int width, int height, int colorCacheSize, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer> entropyImageDecoder)
    {
        ArgumentNullException.ThrowIfNull(entropyImageDecoder);
        return ParseCore(reader, width, height, colorCacheSize, entropyImageDecoder);
    }

    internal static WebpVp8LMetaPrefixParseResult ParseCore(WebpVp8LBitReader reader, int width, int height, int colorCacheSize, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer>? entropyImageDecoder)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidateDimensions(width, height);
        ValidateColorCacheSize(colorCacheSize);

        WebpVp8LMetaPrefixParseResult result = new WebpVp8LMetaPrefixParseResult();
        if (reader.ReadBits(1) == 0)
        {
            result.EntropyImage.NumPrefixGroups = 1;
        }
        else
        {
            ParseEntropyImage(result, reader, width, height, entropyImageDecoder);
        }

        if (!result.HasErrors())
        {
            ParsePrefixCodeGroups(result, reader, colorCacheSize);
        }

        Finish(result);
        return result;
    }

    internal static WebpVp8LMetaPrefixParseResult ParseSingleGroup(WebpVp8LBitReader reader, int width, int height, int colorCacheSize)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidateDimensions(width, height);
        ValidateColorCacheSize(colorCacheSize);

        WebpVp8LMetaPrefixParseResult result = new WebpVp8LMetaPrefixParseResult();
        result.EntropyImage.NumPrefixGroups = 1;
        ParsePrefixCodeGroups(result, reader, colorCacheSize);
        Finish(result);
        return result;
    }

    internal static void ParseEntropyImage(WebpVp8LMetaPrefixParseResult result, WebpVp8LBitReader reader, int width, int height, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer>? entropyImageDecoder)
    {
        result.HasEntropyImage = true;
        WebpVp8LEntropyImage entropyImage = result.EntropyImage;
        entropyImage.PrefixBits = (int)reader.ReadBits(3) + 2;
        entropyImage.Width = DivRoundUp(width, 1 << entropyImage.PrefixBits);
        entropyImage.Height = DivRoundUp(height, 1 << entropyImage.PrefixBits);

        if (entropyImageDecoder == null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, "VP8L entropy image decode requires an entropy image decoder");
            return;
        }

        WebpPixelBuffer pixels = entropyImageDecoder(reader, entropyImage.Width, entropyImage.Height);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.Width == 0 && pixels.Height == 0 && pixels.Rgba.Length == 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, "VP8L entropy image decode exceeded the recursion depth limit");
            return;
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(pixels);
        ValidateEntropyImageDimensions(pixels, entropyImage.Width, entropyImage.Height);
        ExtractMetaPrefixCodes(entropyImage, pixels);
    }

    internal static void ParsePrefixCodeGroups(WebpVp8LMetaPrefixParseResult result, WebpVp8LBitReader reader, int colorCacheSize)
    {
        int group = 0;
        while (group < result.EntropyImage.NumPrefixGroups)
        {
            WebpVp8LPrefixCodeGroup prefixGroup = ParsePrefixCodeGroup(result, reader, colorCacheSize);
            if (result.HasErrors())
            {
                return;
            }

            result.PrefixCodeGroups.Add(prefixGroup);
            group = group + 1;
        }
    }

    internal static WebpVp8LPrefixCodeGroup ParsePrefixCodeGroup(WebpVp8LMetaPrefixParseResult result, WebpVp8LBitReader reader, int colorCacheSize)
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = ParseOnePrefixCode(result, reader, 256 + 24 + colorCacheSize);
        if (result.HasErrors())
        {
            return group;
        }

        group.Red = ParseOnePrefixCode(result, reader, 256);
        if (result.HasErrors())
        {
            return group;
        }

        group.Blue = ParseOnePrefixCode(result, reader, 256);
        if (result.HasErrors())
        {
            return group;
        }

        group.Alpha = ParseOnePrefixCode(result, reader, 256);
        if (result.HasErrors())
        {
            return group;
        }

        group.Distance = ParseOnePrefixCode(result, reader, 40);
        return group;
    }

    internal static WebpVp8LPrefixCodeTable ParseOnePrefixCode(WebpVp8LMetaPrefixParseResult result, WebpVp8LBitReader reader, int alphabetSize)
    {
        WebpVp8LCodeLengthsParseResult codeLengths = WebpVp8LPrefixCodeParser.ParseCodeLengths(reader, alphabetSize);
        CopyDiagnostics(result, codeLengths);
        if (codeLengths.Table == null)
        {
            return new WebpVp8LPrefixCodeTable();
        }

        return codeLengths.Table;
    }

    internal static void ExtractMetaPrefixCodes(WebpVp8LEntropyImage entropyImage, WebpPixelBuffer pixels)
    {
        entropyImage.MetaPrefixCodes = new int[pixels.Width * pixels.Height];
        int maxCode = 0;
        int row = 0;
        while (row < pixels.Height)
        {
            int rowOffset = row * pixels.Stride;
            int column = 0;
            while (column < pixels.Width)
            {
                int pixel = rowOffset + column * 4;
                int index = row * pixels.Width + column;
                int metaPrefixCode = (pixels.Rgba[pixel] << 8) | pixels.Rgba[pixel + 1];
                entropyImage.MetaPrefixCodes[index] = metaPrefixCode;
                if (metaPrefixCode > maxCode)
                {
                    maxCode = metaPrefixCode;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        entropyImage.NumPrefixGroups = maxCode + 1;
    }

    internal static void CopyDiagnostics(WebpVp8LMetaPrefixParseResult result, WebpVp8LCodeLengthsParseResult codeLengths)
    {
        int index = 0;
        while (index < codeLengths.Diagnostics.Count)
        {
            result.Diagnostics.Add(codeLengths.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static int DivRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateColorCacheSize(int colorCacheSize)
    {
        if (colorCacheSize < 0)
        {
            throw new ArgumentOutOfRangeException("colorCacheSize");
        }

        if (colorCacheSize > (1 << WebpConstants.Vp8LMaxColorCacheBits))
        {
            throw new ArgumentOutOfRangeException("colorCacheSize");
        }
    }

    internal static void ValidateEntropyImageDimensions(WebpPixelBuffer pixels, int width, int height)
    {
        if (pixels.Width != width || pixels.Height != height)
        {
            throw new ArgumentException("entropy image dimensions must match the derived prefix image dimensions", "pixels");
        }
    }

    internal static void AddDiagnostic(WebpVp8LMetaPrefixParseResult result, WebpDiagnosticSeverity severity, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LMetaPrefixParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
