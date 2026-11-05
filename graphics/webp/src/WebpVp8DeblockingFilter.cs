using System;

namespace webp;

internal class WebpVp8LoopFilterParameters
{
    internal int InteriorLimit;
    internal int MacroblockEdgeLimit;
    internal int SubblockEdgeLimit;
    internal int HighEdgeVarianceThreshold;

    internal WebpVp8LoopFilterParameters()
    {
        InteriorLimit = 0;
        MacroblockEdgeLimit = 0;
        SubblockEdgeLimit = 0;
        HighEdgeVarianceThreshold = 0;
    }
}

internal static class WebpVp8DeblockingFilter
{
    internal static WebpVp8LoopFilterParameters CalculateParameters(int loopFilterLevel, int sharpnessLevel, bool isKeyFrame)
    {
        ValidateLoopFilterLevel(loopFilterLevel);
        ValidateSharpness(sharpnessLevel);

        WebpVp8LoopFilterParameters parameters = new WebpVp8LoopFilterParameters();
        int interiorLimit = loopFilterLevel;
        if (sharpnessLevel != 0)
        {
            if (sharpnessLevel > 4)
            {
                interiorLimit = interiorLimit >> 2;
            }
            else
            {
                interiorLimit = interiorLimit >> 1;
            }

            int sharpnessLimit = 9 - sharpnessLevel;
            if (interiorLimit > sharpnessLimit)
            {
                interiorLimit = sharpnessLimit;
            }
        }

        if (interiorLimit == 0)
        {
            interiorLimit = 1;
        }

        parameters.InteriorLimit = interiorLimit;
        parameters.MacroblockEdgeLimit = ((loopFilterLevel + 2) * 2) + interiorLimit;
        parameters.SubblockEdgeLimit = (loopFilterLevel * 2) + interiorLimit;
        parameters.HighEdgeVarianceThreshold = CalculateHighEdgeVarianceThreshold(loopFilterLevel, isKeyFrame);
        return parameters;
    }

    internal static bool ShouldFilterSimple(int p1, int p0, int q0, int q1, int edgeLimit)
    {
        ValidateThreshold(edgeLimit, "edgeLimit");
        return ((Abs(p0 - q0) * 2) + (Abs(p1 - q1) / 2)) <= edgeLimit;
    }

    internal static bool ShouldFilterNormal(int p3, int p2, int p1, int p0, int q0, int q1, int q2, int q3, int interiorLimit, int edgeLimit)
    {
        ValidateThreshold(interiorLimit, "interiorLimit");
        ValidateThreshold(edgeLimit, "edgeLimit");
        return ((Abs(p0 - q0) * 2) + (Abs(p1 - q1) / 2)) <= edgeLimit
            && Abs(p3 - p2) <= interiorLimit
            && Abs(p2 - p1) <= interiorLimit
            && Abs(p1 - p0) <= interiorLimit
            && Abs(q3 - q2) <= interiorLimit
            && Abs(q2 - q1) <= interiorLimit
            && Abs(q1 - q0) <= interiorLimit;
    }

    internal static bool HasHighEdgeVariance(int p1, int p0, int q0, int q1, int threshold)
    {
        ValidateThreshold(threshold, "threshold");
        return Abs(p1 - p0) > threshold || Abs(q1 - q0) > threshold;
    }

    internal static void FilterSimpleSegment(byte[] segment, int edgeLimit)
    {
        ArgumentNullException.ThrowIfNull(segment);
        ValidateSegment(segment, 4, "segment");
        if (ShouldFilterSimple(segment[0], segment[1], segment[2], segment[3], edgeLimit))
        {
            CommonAdjust(true, segment, 0, 1, 2, 3);
        }
    }

    internal static void FilterSubblockSegment(byte[] segment, int hevThreshold, int interiorLimit, int edgeLimit)
    {
        ArgumentNullException.ThrowIfNull(segment);
        ValidateSegment(segment, 8, "segment");
        if (ShouldFilterNormal(segment[0], segment[1], segment[2], segment[3], segment[4], segment[5], segment[6], segment[7], interiorLimit, edgeLimit))
        {
            int p1 = UnsignedToSigned(segment[2]);
            int q1 = UnsignedToSigned(segment[5]);
            bool highVariance = HasHighEdgeVariance(p1, UnsignedToSigned(segment[3]), UnsignedToSigned(segment[4]), q1, hevThreshold);
            int adjustment = (CommonAdjust(highVariance, segment, 2, 3, 4, 5) + 1) >> 1;
            if (!highVariance)
            {
                segment[5] = SignedToUnsigned(q1 - adjustment);
                segment[2] = SignedToUnsigned(p1 + adjustment);
            }
        }
    }

    internal static void FilterMacroblockSegment(byte[] segment, int hevThreshold, int interiorLimit, int edgeLimit)
    {
        ArgumentNullException.ThrowIfNull(segment);
        ValidateSegment(segment, 8, "segment");
        int p3 = UnsignedToSigned(segment[0]);
        int p2 = UnsignedToSigned(segment[1]);
        int p1 = UnsignedToSigned(segment[2]);
        int p0 = UnsignedToSigned(segment[3]);
        int q0 = UnsignedToSigned(segment[4]);
        int q1 = UnsignedToSigned(segment[5]);
        int q2 = UnsignedToSigned(segment[6]);
        int q3 = UnsignedToSigned(segment[7]);
        if (ShouldFilterNormal(p3, p2, p1, p0, q0, q1, q2, q3, interiorLimit, edgeLimit))
        {
            if (!HasHighEdgeVariance(p1, p0, q0, q1, hevThreshold))
            {
                int w = ClampSigned(ClampSigned(p1 - q1) + (3 * (q0 - p0)));
                int adjustment = ClampSigned((27 * w + 63) >> 7);
                segment[4] = SignedToUnsigned(q0 - adjustment);
                segment[3] = SignedToUnsigned(p0 + adjustment);
                adjustment = ClampSigned((18 * w + 63) >> 7);
                segment[5] = SignedToUnsigned(q1 - adjustment);
                segment[2] = SignedToUnsigned(p1 + adjustment);
                adjustment = ClampSigned((9 * w + 63) >> 7);
                segment[6] = SignedToUnsigned(q2 - adjustment);
                segment[1] = SignedToUnsigned(p2 + adjustment);
            }
            else
            {
                CommonAdjust(true, segment, 2, 3, 4, 5);
            }
        }
    }

    internal static void FilterVerticalSimpleEdge(byte[] plane, int width, int height, int stride, int edgeX, int startY, int length, int edgeLimit)
    {
        ValidateEdgeAccess(plane, width, height, stride, edgeX, startY, length, true, 2);
        int row = 0;
        while (row < length)
        {
            int offset = (startY + row) * stride + edgeX;
            byte[] segment = new byte[] { plane[offset - 2], plane[offset - 1], plane[offset], plane[offset + 1] };
            FilterSimpleSegment(segment, edgeLimit);
            plane[offset - 1] = segment[1];
            plane[offset] = segment[2];
            row = row + 1;
        }
    }

    internal static void FilterHorizontalSimpleEdge(byte[] plane, int width, int height, int stride, int edgeY, int startX, int length, int edgeLimit)
    {
        ValidateEdgeAccess(plane, width, height, stride, startX, edgeY, length, false, 2);
        int column = 0;
        while (column < length)
        {
            int offset = edgeY * stride + startX + column;
            byte[] segment = new byte[] { plane[offset - (2 * stride)], plane[offset - stride], plane[offset], plane[offset + stride] };
            FilterSimpleSegment(segment, edgeLimit);
            plane[offset - stride] = segment[1];
            plane[offset] = segment[2];
            column = column + 1;
        }
    }

    internal static void FilterVerticalNormalSubblockEdge(byte[] plane, int width, int height, int stride, int edgeX, int startY, int length, WebpVp8LoopFilterParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ValidateEdgeAccess(plane, width, height, stride, edgeX, startY, length, true, 4);
        int row = 0;
        while (row < length)
        {
            int offset = (startY + row) * stride + edgeX;
            byte[] segment = new byte[] { plane[offset - 4], plane[offset - 3], plane[offset - 2], plane[offset - 1], plane[offset], plane[offset + 1], plane[offset + 2], plane[offset + 3] };
            FilterSubblockSegment(segment, parameters.HighEdgeVarianceThreshold, parameters.InteriorLimit, parameters.SubblockEdgeLimit);
            WriteVerticalSegment(plane, offset, segment);
            row = row + 1;
        }
    }

    internal static void FilterHorizontalNormalSubblockEdge(byte[] plane, int width, int height, int stride, int edgeY, int startX, int length, WebpVp8LoopFilterParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ValidateEdgeAccess(plane, width, height, stride, startX, edgeY, length, false, 4);
        int column = 0;
        while (column < length)
        {
            int offset = edgeY * stride + startX + column;
            byte[] segment = ReadHorizontalSegment(plane, offset, stride);
            FilterSubblockSegment(segment, parameters.HighEdgeVarianceThreshold, parameters.InteriorLimit, parameters.SubblockEdgeLimit);
            WriteHorizontalSegment(plane, offset, stride, segment);
            column = column + 1;
        }
    }

    internal static int CommonAdjust(bool useOuterTaps, byte[] segment, int p1Index, int p0Index, int q0Index, int q1Index)
    {
        int p1 = UnsignedToSigned(segment[p1Index]);
        int p0 = UnsignedToSigned(segment[p0Index]);
        int q0 = UnsignedToSigned(segment[q0Index]);
        int q1 = UnsignedToSigned(segment[q1Index]);
        int outer = 0;
        if (useOuterTaps)
        {
            outer = ClampSigned(p1 - q1);
        }

        int adjustment = ClampSigned(outer + (3 * (q0 - p0)));
        int balancedAdjustment = ClampSigned(adjustment + 3) >> 3;
        adjustment = ClampSigned(adjustment + 4) >> 3;
        segment[q0Index] = SignedToUnsigned(q0 - adjustment);
        segment[p0Index] = SignedToUnsigned(p0 + balancedAdjustment);
        return adjustment;
    }

    internal static byte[] ReadHorizontalSegment(byte[] plane, int offset, int stride)
    {
        return new byte[]
        {
            plane[offset - (4 * stride)],
            plane[offset - (3 * stride)],
            plane[offset - (2 * stride)],
            plane[offset - stride],
            plane[offset],
            plane[offset + stride],
            plane[offset + (2 * stride)],
            plane[offset + (3 * stride)]
        };
    }

    internal static void WriteVerticalSegment(byte[] plane, int offset, byte[] segment)
    {
        plane[offset - 3] = segment[1];
        plane[offset - 2] = segment[2];
        plane[offset - 1] = segment[3];
        plane[offset] = segment[4];
        plane[offset + 1] = segment[5];
        plane[offset + 2] = segment[6];
    }

    internal static void WriteHorizontalSegment(byte[] plane, int offset, int stride, byte[] segment)
    {
        plane[offset - (3 * stride)] = segment[1];
        plane[offset - (2 * stride)] = segment[2];
        plane[offset - stride] = segment[3];
        plane[offset] = segment[4];
        plane[offset + stride] = segment[5];
        plane[offset + (2 * stride)] = segment[6];
    }

    internal static int CalculateHighEdgeVarianceThreshold(int loopFilterLevel, bool isKeyFrame)
    {
        if (isKeyFrame)
        {
            if (loopFilterLevel >= 40)
            {
                return 2;
            }

            if (loopFilterLevel >= 15)
            {
                return 1;
            }

            return 0;
        }

        if (loopFilterLevel >= 40)
        {
            return 3;
        }

        if (loopFilterLevel >= 20)
        {
            return 2;
        }

        if (loopFilterLevel >= 15)
        {
            return 1;
        }

        return 0;
    }

    internal static int UnsignedToSigned(byte value)
    {
        return value - 128;
    }

    internal static byte SignedToUnsigned(int value)
    {
        return (byte)(ClampSigned(value) + 128);
    }

    internal static int ClampSigned(int value)
    {
        if (value < -128)
        {
            return -128;
        }

        if (value > 127)
        {
            return 127;
        }

        return value;
    }

    internal static int Abs(int value)
    {
        if (value < 0)
        {
            return -value;
        }

        return value;
    }

    internal static void ValidateLoopFilterLevel(int value)
    {
        if (value < 0 || value > 63)
        {
            throw new ArgumentOutOfRangeException("loopFilterLevel");
        }
    }

    internal static void ValidateSharpness(int value)
    {
        if (value < 0 || value > 7)
        {
            throw new ArgumentOutOfRangeException("sharpnessLevel");
        }
    }

    internal static void ValidateThreshold(int value, string name)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateSegment(byte[] segment, int length, string name)
    {
        if (segment.Length != length)
        {
            throw new ArgumentException("VP8 loop filter segment length is invalid", name);
        }
    }

    internal static void ValidateEdgeAccess(byte[] plane, int width, int height, int stride, int edgeX, int edgeY, int length, bool vertical, int radius)
    {
        ArgumentNullException.ThrowIfNull(plane);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (stride < width)
        {
            throw new ArgumentOutOfRangeException("stride");
        }

        if (plane.Length < stride * height)
        {
            throw new ArgumentException("VP8 loop filter plane is too small", "plane");
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (vertical)
        {
            if (edgeX < radius || edgeX + radius - 1 >= width || edgeY < 0 || edgeY + length > height)
            {
                throw new ArgumentOutOfRangeException("edgeX");
            }
        }
        else
        {
            if (edgeY < radius || edgeY + radius - 1 >= height || edgeX < 0 || edgeX + length > width)
            {
                throw new ArgumentOutOfRangeException("edgeY");
            }
        }
    }
}
