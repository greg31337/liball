using System;

namespace webp;

internal static class WebpVp8LLz77Decoder
{
    internal static int DecodePrefixValue(WebpVp8LBitReader reader, int prefixCode)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidatePrefixCode(prefixCode);

        if (prefixCode < 4)
        {
            return prefixCode + 1;
        }

        int extraBits = (prefixCode - 2) >> 1;
        int offset = (2 + (prefixCode & 1)) << extraBits;
        return offset + (int)reader.ReadBits(extraBits) + 1;
    }

    internal static int DecodeLength(WebpVp8LBitReader reader, int greenSymbol)
    {
        if (greenSymbol < 256 || greenSymbol > 279)
        {
            throw new ArgumentOutOfRangeException("greenSymbol");
        }

        return DecodePrefixValue(reader, greenSymbol - 256);
    }

    internal static int DecodeDistanceCode(WebpVp8LBitReader reader, WebpVp8LPrefixCodeTable distancePrefixCode)
    {
        ArgumentNullException.ThrowIfNull(distancePrefixCode);
        int prefixCode = distancePrefixCode.DecodeSymbol(reader);
        return DecodePrefixValue(reader, prefixCode);
    }

    internal static WebpVp8LLz77Reference DecodeReference(WebpVp8LBitReader reader, WebpVp8LPrefixCodeGroup group, int greenSymbol, int imageWidth)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(group.Distance);

        WebpVp8LLz77Reference reference = new WebpVp8LLz77Reference();
        reference.Length = DecodeLength(reader, greenSymbol);
        reference.DistanceCode = DecodeDistanceCode(reader, group.Distance);
        reference.Distance = MapDistanceCode(reference.DistanceCode, imageWidth);
        return reference;
    }

    internal static int MapDistanceCode(int distanceCode, int imageWidth)
    {
        if (distanceCode <= 0)
        {
            throw new ArgumentOutOfRangeException("distanceCode");
        }

        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("imageWidth");
        }

        if (distanceCode > 120)
        {
            return distanceCode - 120;
        }

        int[] xOffsets = CreateDistanceMapX();
        int[] yOffsets = CreateDistanceMapY();
        int index = distanceCode - 1;
        int distance = xOffsets[index] + yOffsets[index] * imageWidth;
        if (distance < 1)
        {
            return 1;
        }

        return distance;
    }

    internal static void CopyPixels(WebpVp8LColor[] pixels, int outputIndex, int length, int distance)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ValidateCopyRange(pixels, outputIndex, length, distance);

        int sourceIndex = outputIndex - distance;
        int index = 0;
        while (index < length)
        {
            WebpVp8LColor color = pixels[sourceIndex + index];
            ArgumentNullException.ThrowIfNull(color);
            pixels[outputIndex + index] = color.Copy();
            index = index + 1;
        }
    }

    internal static void ValidatePrefixCode(int prefixCode)
    {
        if (prefixCode < 0 || prefixCode > 39)
        {
            throw new ArgumentOutOfRangeException("prefixCode");
        }
    }

    internal static void ValidateCopyRange(WebpVp8LColor[] pixels, int outputIndex, int length, int distance)
    {
        if (outputIndex < 0 || outputIndex > pixels.Length)
        {
            throw new ArgumentOutOfRangeException("outputIndex");
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (distance <= 0)
        {
            throw new ArgumentOutOfRangeException("distance");
        }

        if (distance > outputIndex)
        {
            throw new ArgumentException("LZ77 distance extends before the decoded pixel buffer", "distance");
        }

        if (length > pixels.Length - outputIndex)
        {
            throw new ArgumentException("LZ77 length extends beyond the decoded pixel buffer", "length");
        }
    }

    internal static int[] CreateDistanceMapX()
    {
        return new int[]
        {
            0, 1, 1, -1, 0, 2, 1, -1, 2, -2, 2, -2, 0, 3, 1, -1, 3, -3, 2, -2, 3, -3, 0, 4, 1, -1, 4, -4, 3, -3,
            2, -2, 4, -4, 0, 3, -3, 4, -4, 5, 1, -1, 5, -5, 2, -2, 5, -5, 4, -4, 3, -3, 5, -5, 0, 6, 1, -1, 6, -6,
            2, -2, 6, -6, 4, -4, 5, -5, 3, -3, 6, -6, 0, 7, 1, -1, 5, -5, 7, -7, 4, -4, 6, -6, 2, -2, 7, -7, 3, -3,
            7, -7, 5, -5, 6, -6, 8, 4, -4, 7, -7, 8, 8, 6, -6, 8, 5, -5, 7, -7, 8, 6, -6, 7, -7, 8, 7, -7, 8, 8
        };
    }

    internal static int[] CreateDistanceMapY()
    {
        return new int[]
        {
            1, 0, 1, 1, 2, 0, 2, 2, 1, 1, 2, 2, 3, 0, 3, 3, 1, 1, 3, 3, 2, 2, 4, 0, 4, 4, 1, 1, 3, 3,
            4, 4, 2, 2, 5, 4, 4, 3, 3, 0, 5, 5, 1, 1, 5, 5, 2, 2, 4, 4, 5, 5, 3, 3, 6, 0, 6, 6, 1, 1,
            6, 6, 2, 2, 5, 5, 4, 4, 6, 6, 3, 3, 7, 0, 7, 7, 5, 5, 1, 1, 6, 6, 4, 4, 7, 7, 2, 2, 7, 7,
            3, 3, 6, 6, 5, 5, 0, 7, 7, 4, 4, 1, 2, 6, 6, 3, 7, 7, 5, 5, 4, 7, 7, 6, 6, 5, 7, 7, 6, 7
        };
    }
}
