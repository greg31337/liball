using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8LDecodeContext
{
    internal int MaxDepth;
    internal bool LimitExceeded;
    internal bool NestedDecodeFailed;

    internal WebpVp8LDecodeContext(int maxDepth)
    {
        MaxDepth = maxDepth;
        LimitExceeded = false;
        NestedDecodeFailed = false;
    }
}

internal static class WebpVp8LDecoder
{
    internal const int PredictorTransformType = 0;
    internal const int ColorTransformType = 1;
    internal const int SubtractGreenTransformType = 2;
    internal const int ColorIndexingTransformType = 3;

    internal static WebpVp8LDecodeResult Decode(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Decode(payload.AsSpan(), strictness);
    }

    internal static WebpVp8LDecodeResult Decode(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        return DecodeWithLimit(payload, strictness, WebpConstants.DefaultMaxRecursionDepth);
    }

    internal static WebpVp8LDecodeResult DecodeWithLimit(ReadOnlySpan<byte> payload, WebpStrictness strictness, int maxRecursionDepth)
    {
        if (maxRecursionDepth <= 0)
        {
            throw new ArgumentOutOfRangeException("maxRecursionDepth");
        }

        WebpVp8LDecodeContext context = new WebpVp8LDecodeContext(maxRecursionDepth);
        return DecodeCore(payload, strictness, context);
    }

    internal static WebpVp8LDecodeResult DecodeCore(ReadOnlySpan<byte> payload, WebpStrictness strictness, WebpVp8LDecodeContext context)
    {
        WebpVp8LDecodeResult result = new WebpVp8LDecodeResult();
        WebpVp8LHeaderParseResult headerResult = WebpVp8LBitReader.ParseHeader(payload, strictness);
        CopyDiagnostics(result.Diagnostics, headerResult.Diagnostics);
        result.Header = headerResult.Header;
        if (!headerResult.IsValid || headerResult.Reader == null)
        {
            Finish(result);
            return result;
        }

        WebpVp8LBitReader reader = headerResult.Reader;
        List<WebpVp8LTransform> transforms = ParseTransformsWithContext(result, reader, result.Header.Width, result.Header.Height, context, 1);
        if (result.HasErrors())
        {
            AddContextDiagnostics(result, context);
            Finish(result);
            return result;
        }

        int transformedWidth = FindTransformedWidth(transforms, result.Header.Width);
        int transformedHeight = result.Header.Height;
        WebpVp8LImageDataDecodeResult imageData = DecodeImageDataWithContext(reader, transformedWidth, transformedHeight, context, 1);
        CopyDiagnostics(result.Diagnostics, imageData.Diagnostics);
        if (!imageData.IsValid)
        {
            AddContextDiagnostics(result, context);
            Finish(result);
            return result;
        }

        WebpVp8LColor[] pixels = ApplyInverseTransforms(imageData.Pixels, transforms, transformedWidth, transformedHeight);
        result.PixelBuffer = ToPixelBuffer(pixels, result.Header.Width, result.Header.Height);
        AddContextDiagnostics(result, context);
        Finish(result);
        return result;
    }

    internal static WebpVp8LImageDataDecodeResult DecodeImageData(WebpVp8LBitReader reader, int width, int height)
    {
        WebpVp8LDecodeContext context = new WebpVp8LDecodeContext(WebpConstants.DefaultMaxRecursionDepth);
        return DecodeImageDataWithContext(reader, width, height, context, 1);
    }

    internal static WebpVp8LImageDataDecodeResult DecodeImageDataWithContext(WebpVp8LBitReader reader, int width, int height, WebpVp8LDecodeContext context, int depth)
    {
        return DecodeImageDataRoleWithContext(reader, width, height, context, depth, true);
    }

    internal static WebpVp8LImageDataDecodeResult DecodeImageDataRoleWithContext(WebpVp8LBitReader reader, int width, int height, WebpVp8LDecodeContext context, int depth, bool allowMetaPrefixCodes)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(context);
        ValidateDimensions(width, height);

        WebpVp8LImageDataDecodeResult result = new WebpVp8LImageDataDecodeResult();
        result.Width = width;
        result.Height = height;

        WebpVp8LColorCacheParseResult colorCacheInfo = WebpVp8LColorCache.ParseColorCacheInfo(reader);
        CopyDiagnostics(result.Diagnostics, colorCacheInfo.Diagnostics);
        if (!colorCacheInfo.IsValid)
        {
            FinishImageData(result);
            return result;
        }

        int colorCacheSize = 0;
        WebpVp8LColorCache? colorCache = null;
        if (colorCacheInfo.IsPresent)
        {
            colorCacheSize = colorCacheInfo.CacheSize;
            colorCache = new WebpVp8LColorCache(colorCacheInfo.CacheBits);
        }

        WebpVp8LMetaPrefixParseResult metaPrefix;
        if (allowMetaPrefixCodes)
        {
            metaPrefix = WebpVp8LMetaPrefixCodeParser.Parse(
                reader,
                width,
                height,
                colorCacheSize,
                (nestedReader, nestedWidth, nestedHeight) => DecodeSubresolutionImageWithContext(nestedReader, nestedWidth, nestedHeight, context, depth + 1));
        }
        else
        {
            metaPrefix = WebpVp8LMetaPrefixCodeParser.ParseSingleGroup(reader, width, height, colorCacheSize);
        }
        CopyDiagnostics(result.Diagnostics, metaPrefix.Diagnostics);
        if (!metaPrefix.IsValid)
        {
            FinishImageData(result);
            return result;
        }

        result.Pixels = DecodePixels(reader, width, height, metaPrefix, colorCache, result.Diagnostics);
        if (!result.HasErrors())
        {
            result.PixelBuffer = ToPixelBuffer(result.Pixels, width, height);
        }

        FinishImageData(result);
        return result;
    }

    internal static WebpVp8LColor[] DecodePixels(WebpVp8LBitReader reader, int width, int height, WebpVp8LMetaPrefixParseResult metaPrefix, WebpVp8LColorCache? colorCache, List<WebpDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(metaPrefix);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ValidateDimensions(width, height);

        WebpVp8LColor[] pixels = new WebpVp8LColor[width * height];
        int outputIndex = 0;
        while (outputIndex < pixels.Length)
        {
            int x = outputIndex % width;
            int y = outputIndex / width;
            WebpVp8LPrefixCodeGroup group = GetPrefixCodeGroup(metaPrefix, x, y, diagnostics);
            if (HasErrors(diagnostics))
            {
                return pixels;
            }

            int greenSymbol = group.Green.DecodeSymbol(reader);
            if (greenSymbol < 256)
            {
                WebpVp8LColor color = DecodeLiteralColor(reader, group, greenSymbol, diagnostics);
                if (HasErrors(diagnostics))
                {
                    return pixels;
                }

                pixels[outputIndex] = color;
                InsertColor(colorCache, color);
                outputIndex = outputIndex + 1;
            }
            else if (greenSymbol < 280)
            {
                outputIndex = DecodeBackwardReference(reader, width, pixels, outputIndex, group, greenSymbol, colorCache, diagnostics);
                if (HasErrors(diagnostics))
                {
                    return pixels;
                }
            }
            else
            {
                WebpVp8LColor color = DecodeCacheColor(greenSymbol, colorCache, diagnostics);
                if (HasErrors(diagnostics))
                {
                    return pixels;
                }

                pixels[outputIndex] = color;
                InsertColor(colorCache, color);
                outputIndex = outputIndex + 1;
            }
        }

        return pixels;
    }

    internal static List<WebpVp8LTransform> ParseTransforms(WebpVp8LDecodeResult result, WebpVp8LBitReader reader, int width, int height)
    {
        WebpVp8LDecodeContext context = new WebpVp8LDecodeContext(WebpConstants.DefaultMaxRecursionDepth);
        return ParseTransformsWithContext(result, reader, width, height, context, 1);
    }

    internal static List<WebpVp8LTransform> ParseTransformsWithContext(WebpVp8LDecodeResult result, WebpVp8LBitReader reader, int width, int height, WebpVp8LDecodeContext context, int depth)
    {
        List<WebpVp8LTransform> transforms = new List<WebpVp8LTransform>();
        int currentWidth = width;
        int currentHeight = height;
        bool hasPredictor = false;
        bool hasColor = false;
        bool hasSubtractGreen = false;
        bool hasColorIndexing = false;
        if (!RequireBits(result.Diagnostics, reader, 1, "VP8L bitstream is truncated before the transform flag"))
        {
            return transforms;
        }

        while (reader.ReadBits(1) != 0)
        {
            if (!RequireBits(result.Diagnostics, reader, 2, "VP8L bitstream is truncated before the transform type"))
            {
                return transforms;
            }

            WebpVp8LTransform transform = new WebpVp8LTransform();
            transform.Type = (int)reader.ReadBits(2);
            if (IsDuplicateTransform(transform.Type, hasPredictor, hasColor, hasSubtractGreen, hasColorIndexing))
            {
                AddDiagnostic(result.Diagnostics, "VP8L duplicate transform is not allowed");
                return transforms;
            }

            transform.WidthBefore = currentWidth;
            transform.HeightBefore = currentHeight;
            transform.WidthAfter = currentWidth;
            transform.HeightAfter = currentHeight;
            hasPredictor = hasPredictor || transform.Type == PredictorTransformType;
            hasColor = hasColor || transform.Type == ColorTransformType;
            hasSubtractGreen = hasSubtractGreen || transform.Type == SubtractGreenTransformType;
            hasColorIndexing = hasColorIndexing || transform.Type == ColorIndexingTransformType;
            ParseOneTransformWithContext(result, reader, transform, context, depth);
            transforms.Add(transform);
            currentWidth = transform.WidthAfter;
            currentHeight = transform.HeightAfter;
            if (result.HasErrors())
            {
                return transforms;
            }

            if (!RequireBits(result.Diagnostics, reader, 1, "VP8L bitstream is truncated before the next transform flag"))
            {
                return transforms;
            }
        }

        return transforms;
    }

    internal static void ParseOneTransform(WebpVp8LDecodeResult result, WebpVp8LBitReader reader, WebpVp8LTransform transform)
    {
        WebpVp8LDecodeContext context = new WebpVp8LDecodeContext(WebpConstants.DefaultMaxRecursionDepth);
        ParseOneTransformWithContext(result, reader, transform, context, 1);
    }

    internal static void ParseOneTransformWithContext(WebpVp8LDecodeResult result, WebpVp8LBitReader reader, WebpVp8LTransform transform, WebpVp8LDecodeContext context, int depth)
    {
        if (transform.Type == PredictorTransformType)
        {
            WebpVp8LPredictorTransformParseResult predictor = WebpVp8LPredictorTransformDecoder.Parse(
                reader,
                transform.WidthBefore,
                transform.HeightBefore,
                (nestedReader, nestedWidth, nestedHeight) => DecodeSubresolutionImageWithContext(nestedReader, nestedWidth, nestedHeight, context, depth + 1));
            CopyDiagnostics(result.Diagnostics, predictor.Diagnostics);
            transform.PredictorTransform = predictor.Transform;
        }
        else if (transform.Type == ColorTransformType)
        {
            WebpVp8LColorTransformParseResult color = WebpVp8LColorTransformDecoder.Parse(
                reader,
                transform.WidthBefore,
                transform.HeightBefore,
                (nestedReader, nestedWidth, nestedHeight) => DecodeSubresolutionImageWithContext(nestedReader, nestedWidth, nestedHeight, context, depth + 1));
            CopyDiagnostics(result.Diagnostics, color.Diagnostics);
            transform.ColorTransform = color.Transform;
        }
        else if (transform.Type == ColorIndexingTransformType)
        {
            WebpVp8LColorIndexingTransformParseResult colorIndexing = WebpVp8LColorIndexingTransformDecoder.Parse(
                reader,
                transform.WidthBefore,
                transform.HeightBefore,
                (nestedReader, nestedWidth, nestedHeight) => DecodeSubresolutionImageWithContext(nestedReader, nestedWidth, nestedHeight, context, depth + 1));
            CopyDiagnostics(result.Diagnostics, colorIndexing.Diagnostics);
            transform.ColorIndexingTransform = colorIndexing.Transform;
            transform.WidthAfter = colorIndexing.Transform.PackedWidth;
        }
    }

    internal static WebpVp8LColor[] ApplyInverseTransforms(WebpVp8LColor[] pixels, List<WebpVp8LTransform> transforms, int width, int height)
    {
        WebpVp8LColor[] output = pixels;
        int currentWidth = width;
        int currentHeight = height;
        int index = transforms.Count - 1;
        while (index >= 0)
        {
            WebpVp8LTransform transform = transforms[index];
            if (transform.Type == PredictorTransformType)
            {
                output = WebpVp8LPredictorTransformDecoder.ApplyInverse(output, currentWidth, currentHeight, transform.PredictorTransform);
            }
            else if (transform.Type == ColorTransformType)
            {
                output = WebpVp8LColorTransformDecoder.ApplyInverse(output, currentWidth, currentHeight, transform.ColorTransform);
            }
            else if (transform.Type == SubtractGreenTransformType)
            {
                output = WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(output, currentWidth, currentHeight);
            }
            else if (transform.Type == ColorIndexingTransformType)
            {
                output = WebpVp8LColorIndexingTransformDecoder.ApplyInverse(output, transform.WidthBefore, transform.HeightBefore, transform.ColorIndexingTransform);
            }

            currentWidth = transform.WidthBefore;
            currentHeight = transform.HeightBefore;
            index = index - 1;
        }

        return output;
    }

    internal static int FindTransformedWidth(List<WebpVp8LTransform> transforms, int width)
    {
        if (transforms.Count == 0)
        {
            return width;
        }

        return transforms[transforms.Count - 1].WidthAfter;
    }

    internal static WebpPixelBuffer DecodeSubresolutionImage(WebpVp8LBitReader reader, int width, int height)
    {
        WebpVp8LDecodeContext context = new WebpVp8LDecodeContext(WebpConstants.DefaultMaxRecursionDepth);
        return DecodeSubresolutionImageWithContext(reader, width, height, context, 1);
    }

    internal static WebpPixelBuffer DecodeSubresolutionImageWithContext(WebpVp8LBitReader reader, int width, int height, WebpVp8LDecodeContext context, int depth)
    {
        if (depth > context.MaxDepth)
        {
            context.LimitExceeded = true;
            return new WebpPixelBuffer();
        }

        WebpVp8LImageDataDecodeResult result = DecodeImageDataRoleWithContext(reader, width, height, context, depth, false);
        if (!result.IsValid)
        {
            context.NestedDecodeFailed = true;
            return NewOpaquePixelBuffer(width, height);
        }

        return result.PixelBuffer;
    }

    internal static WebpPixelBuffer NewOpaquePixelBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 3;
        while (index < buffer.Rgba.Length)
        {
            buffer.Rgba[index] = 255;
            index = index + 4;
        }

        return buffer;
    }

    internal static void AddContextDiagnostics(WebpVp8LDecodeResult result, WebpVp8LDecodeContext context)
    {
        if (context.LimitExceeded)
        {
            AddDiagnostic(result.Diagnostics, "VP8L recursion depth exceeds the configured security limit");
        }

        if (context.NestedDecodeFailed)
        {
            AddDiagnostic(result.Diagnostics, "VP8L nested image stream is invalid");
        }
    }

    internal static int DecodeBackwardReference(WebpVp8LBitReader reader, int width, WebpVp8LColor[] pixels, int outputIndex, WebpVp8LPrefixCodeGroup group, int greenSymbol, WebpVp8LColorCache? colorCache, List<WebpDiagnostic> diagnostics)
    {
        WebpVp8LLz77Reference reference = WebpVp8LLz77Decoder.DecodeReference(reader, group, greenSymbol, width);
        if (reference.Distance > outputIndex)
        {
            AddDiagnostic(diagnostics, "VP8L bad backward reference distance extends before the decoded pixel buffer");
            return outputIndex;
        }

        if (reference.Length > pixels.Length - outputIndex)
        {
            AddDiagnostic(diagnostics, "VP8L bad backward reference length extends beyond the decoded pixel buffer");
            return outputIndex;
        }

        WebpVp8LLz77Decoder.CopyPixels(pixels, outputIndex, reference.Length, reference.Distance);
        int index = 0;
        while (index < reference.Length)
        {
            InsertColor(colorCache, pixels[outputIndex + index]);
            index = index + 1;
        }

        return outputIndex + reference.Length;
    }

    internal static bool IsDuplicateTransform(int type, bool hasPredictor, bool hasColor, bool hasSubtractGreen, bool hasColorIndexing)
    {
        if (type == PredictorTransformType)
        {
            return hasPredictor;
        }

        if (type == ColorTransformType)
        {
            return hasColor;
        }

        if (type == SubtractGreenTransformType)
        {
            return hasSubtractGreen;
        }

        if (type == ColorIndexingTransformType)
        {
            return hasColorIndexing;
        }

        return false;
    }

    internal static WebpVp8LColor DecodeLiteralColor(WebpVp8LBitReader reader, WebpVp8LPrefixCodeGroup group, int greenSymbol, List<WebpDiagnostic> diagnostics)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        int red = group.Red.DecodeSymbol(reader);
        int blue = group.Blue.DecodeSymbol(reader);
        int alpha = group.Alpha.DecodeSymbol(reader);
        ValidateChannel(diagnostics, red, "red");
        ValidateChannel(diagnostics, blue, "blue");
        ValidateChannel(diagnostics, alpha, "alpha");
        if (HasErrors(diagnostics))
        {
            return color;
        }

        color.Red = (byte)red;
        color.Green = (byte)greenSymbol;
        color.Blue = (byte)blue;
        color.Alpha = (byte)alpha;
        return color;
    }

    internal static WebpVp8LColor DecodeCacheColor(int greenSymbol, WebpVp8LColorCache? colorCache, List<WebpDiagnostic> diagnostics)
    {
        if (colorCache == null)
        {
            AddDiagnostic(diagnostics, "VP8L color cache symbol was used without a color cache");
            return new WebpVp8LColor();
        }

        int cacheIndex = greenSymbol - 280;
        if (cacheIndex < 0 || cacheIndex >= colorCache.Size)
        {
            AddDiagnostic(diagnostics, "VP8L color cache symbol exceeds the color cache size");
            return new WebpVp8LColor();
        }

        if (!colorCache.ContainsIndex(cacheIndex))
        {
            return new WebpVp8LColor();
        }

        return colorCache.Lookup(cacheIndex);
    }

    internal static WebpVp8LPrefixCodeGroup GetPrefixCodeGroup(WebpVp8LMetaPrefixParseResult metaPrefix, int x, int y, List<WebpDiagnostic> diagnostics)
    {
        int groupIndex = 0;
        if (metaPrefix.HasEntropyImage)
        {
            int blockX = x >> metaPrefix.EntropyImage.PrefixBits;
            int blockY = y >> metaPrefix.EntropyImage.PrefixBits;
            groupIndex = metaPrefix.EntropyImage.MetaPrefixCodes[blockY * metaPrefix.EntropyImage.Width + blockX];
        }

        if (groupIndex < 0 || groupIndex >= metaPrefix.PrefixCodeGroups.Count)
        {
            AddDiagnostic(diagnostics, "VP8L meta prefix code references a missing prefix code group");
            return new WebpVp8LPrefixCodeGroup();
        }

        return metaPrefix.PrefixCodeGroups[groupIndex];
    }

    internal static WebpPixelBuffer ToPixelBuffer(WebpVp8LColor[] pixels, int width, int height)
    {
        ValidateDimensions(width, height);
        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal width * height", "pixels");
        }

        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < pixels.Length)
        {
            WebpVp8LColor color = pixels[index];
            ArgumentNullException.ThrowIfNull(color);
            int pixel = (index / width) * buffer.Stride + (index % width) * 4;
            buffer.Rgba[pixel] = color.Red;
            buffer.Rgba[pixel + 1] = color.Green;
            buffer.Rgba[pixel + 2] = color.Blue;
            buffer.Rgba[pixel + 3] = color.Alpha;
            index = index + 1;
        }

        return buffer;
    }

    internal static void InsertColor(WebpVp8LColorCache? colorCache, WebpVp8LColor color)
    {
        if (colorCache != null)
        {
            colorCache.Insert(color);
        }
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

    internal static void ValidateChannel(List<WebpDiagnostic> diagnostics, int value, string channel)
    {
        if (value < 0 || value > 255)
        {
            AddDiagnostic(diagnostics, "VP8L " + channel + " literal symbol exceeds byte range");
        }
    }

    internal static bool RequireBits(List<WebpDiagnostic> diagnostics, WebpVp8LBitReader reader, int bitCount, string message)
    {
        if (!reader.HasBits(bitCount))
        {
            AddDiagnostic(diagnostics, message);
            return false;
        }

        return true;
    }

    internal static void CopyDiagnostics(List<WebpDiagnostic> destination, List<WebpDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static bool HasErrors(List<WebpDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == WebpDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddDiagnostic(List<WebpDiagnostic> diagnostics, string message)
    {
        diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LDecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }

    internal static void FinishImageData(WebpVp8LImageDataDecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
