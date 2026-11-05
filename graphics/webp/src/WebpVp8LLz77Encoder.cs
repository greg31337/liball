using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpVp8LLz77Encoder
{
    internal const int DefaultMinLength = 3;
    internal const int DefaultMaxDistance = 4096;
    internal const int MaxLength = 4096;

    internal static List<WebpVp8LLz77Token> FindMatches(WebpVp8LColor[] pixels, int width, int height)
    {
        return FindMatches(pixels, width, height, DefaultMinLength, DefaultMaxDistance);
    }

    internal static List<WebpVp8LLz77Token> FindMatches(WebpVp8LColor[] pixels, int width, int height, int minLength, int maxDistance)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ValidatePixels(pixels, width, height);
        ValidateMatchLimits(minLength, maxDistance);

        List<WebpVp8LLz77Token> tokens = new List<WebpVp8LLz77Token>();
        int index = 0;
        while (index < pixels.Length)
        {
            WebpVp8LLz77Token match = FindBestMatch(pixels, width, index, minLength, maxDistance);
            if (match.IsLiteral)
            {
                match.Color = pixels[index].Copy();
                tokens.Add(match);
                index = index + 1;
            }
            else
            {
                tokens.Add(match);
                index = index + match.Length;
            }
        }

        return tokens;
    }

    internal static int EncodeDistanceCode(int distance, int imageWidth)
    {
        if (distance <= 0)
        {
            throw new ArgumentOutOfRangeException("distance");
        }

        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("imageWidth");
        }

        int code = 1;
        while (code <= 120)
        {
            if (WebpVp8LLz77Decoder.MapDistanceCode(code, imageWidth) == distance)
            {
                return code;
            }

            code = code + 1;
        }

        return distance + 120;
    }

    internal static WebpVp8LPrefixValue EncodePrefixValue(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (value <= 4)
        {
            WebpVp8LPrefixValue small = new WebpVp8LPrefixValue();
            small.PrefixCode = value - 1;
            return small;
        }

        int prefixCode = 4;
        while (prefixCode <= 39)
        {
            int extraBitCount = (prefixCode - 2) >> 1;
            int baseValue = PrefixBaseValue(prefixCode);
            int limit = baseValue + (1 << extraBitCount) - 1;
            if (value >= baseValue && value <= limit)
            {
                WebpVp8LPrefixValue encoded = new WebpVp8LPrefixValue();
                encoded.PrefixCode = prefixCode;
                encoded.ExtraBitCount = extraBitCount;
                encoded.ExtraValue = value - baseValue;
                return encoded;
            }

            prefixCode = prefixCode + 1;
        }

        throw new ArgumentOutOfRangeException("value");
    }

    internal static int EncodeLengthSymbol(int length)
    {
        WebpVp8LPrefixValue prefixValue = EncodePrefixValue(length);
        if (prefixValue.PrefixCode > 23)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        return 256 + prefixValue.PrefixCode;
    }

    internal static WebpVp8LLz77Token FindBestMatch(WebpVp8LColor[] pixels, int width, int index, int minLength, int maxDistance)
    {
        WebpVp8LLz77Token best = new WebpVp8LLz77Token();
        int start = index - maxDistance;
        if (start < 0)
        {
            start = 0;
        }

        int candidate = index - 1;
        while (candidate >= start)
        {
            int length = FindMatchLength(pixels, candidate, index, MaxLength);
            if (length > best.Length)
            {
                best.IsLiteral = false;
                best.Length = length;
                best.Distance = index - candidate;
                best.DistanceCode = EncodeDistanceCode(best.Distance, width);
            }

            candidate = candidate - 1;
        }

        if (best.Length < minLength)
        {
            best.IsLiteral = true;
            best.Length = 1;
            best.Distance = 0;
            best.DistanceCode = 0;
        }

        return best;
    }

    internal static int FindMatchLength(WebpVp8LColor[] pixels, int candidate, int index, int maxLength)
    {
        int length = 0;
        while (index + length < pixels.Length && length < maxLength)
        {
            if (!ColorsEqual(pixels[candidate + length], pixels[index + length]))
            {
                return length;
            }

            length = length + 1;
        }

        return length;
    }

    internal static bool ColorsEqual(WebpVp8LColor a, WebpVp8LColor b)
    {
        return a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue && a.Alpha == b.Alpha;
    }

    internal static int PrefixBaseValue(int prefixCode)
    {
        int extraBitCount = (prefixCode - 2) >> 1;
        int offset = (2 + (prefixCode & 1)) << extraBitCount;
        return offset + 1;
    }

    internal static void ValidatePixels(WebpVp8LColor[] pixels, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal width * height", "pixels");
        }
    }

    internal static void ValidateMatchLimits(int minLength, int maxDistance)
    {
        if (minLength < 2)
        {
            throw new ArgumentOutOfRangeException("minLength");
        }

        if (maxDistance <= 0)
        {
            throw new ArgumentOutOfRangeException("maxDistance");
        }
    }
}
