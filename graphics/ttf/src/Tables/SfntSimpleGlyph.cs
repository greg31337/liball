namespace ttf;

internal class SfntGlyphHeader
{
    int contourCount;
    int xMinimum;
    int yMinimum;
    int xMaximum;
    int yMaximum;

    internal SfntGlyphHeader(
        int contourCount,
        int xMinimum,
        int yMinimum,
        int xMaximum,
        int yMaximum)
    {
        this.contourCount = contourCount;
        this.xMinimum = xMinimum;
        this.yMinimum = yMinimum;
        this.xMaximum = xMaximum;
        this.yMaximum = yMaximum;
    }

    internal int GetContourCount()
    {
        return contourCount;
    }

    internal int GetXMinimum()
    {
        return xMinimum;
    }

    internal int GetYMinimum()
    {
        return yMinimum;
    }

    internal int GetXMaximum()
    {
        return xMaximum;
    }

    internal int GetYMaximum()
    {
        return yMaximum;
    }
}

internal class SfntSimpleGlyphPoint
{
    int pointIndex;
    int flags;
    int x;
    int y;
    int xDelta;
    int yDelta;

    internal SfntSimpleGlyphPoint(
        int pointIndex,
        int flags,
        int x,
        int y,
        int xDelta,
        int yDelta)
    {
        this.pointIndex = pointIndex;
        this.flags = flags;
        this.x = x;
        this.y = y;
        this.xDelta = xDelta;
        this.yDelta = yDelta;
    }

    internal int GetPointIndex()
    {
        return pointIndex;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool IsOnCurve()
    {
        return (flags & 0x01) != 0;
    }

    internal int GetX()
    {
        return x;
    }

    internal int GetY()
    {
        return y;
    }

    internal int GetXDelta()
    {
        return xDelta;
    }

    internal int GetYDelta()
    {
        return yDelta;
    }
}

internal class SfntSimpleGlyph
{
    int glyphId;
    SfntGlyphHeader header;
    int[] contourEndPoints;
    byte[] instructions;
    SfntSimpleGlyphPoint[] points;
    bool overlappingContours;
    int trailingPaddingLength;
    int maximumNormalizedPointCount;
    SfntGlyphOutline? normalizedOutline;
    object normalizationSync;

    internal SfntSimpleGlyph(
        int glyphId,
        SfntGlyphHeader header,
        int[] contourEndPoints,
        byte[] instructions,
        SfntSimpleGlyphPoint[] points,
        bool overlappingContours,
        int trailingPaddingLength,
        int maximumNormalizedPointCount)
    {
        this.glyphId = glyphId;
        this.header = header;
        this.contourEndPoints = contourEndPoints;
        this.instructions = instructions;
        this.points = points;
        this.overlappingContours = overlappingContours;
        this.trailingPaddingLength = trailingPaddingLength;
        this.maximumNormalizedPointCount = maximumNormalizedPointCount;
        normalizationSync = new object();
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal SfntGlyphHeader GetHeader()
    {
        return header;
    }

    internal int GetContourCount()
    {
        return contourEndPoints.Length;
    }

    internal int GetContourStartPoint(int contourIndex)
    {
        ValidateContourIndex(contourIndex);
        return contourIndex == 0 ? 0 : contourEndPoints[contourIndex - 1] + 1;
    }

    internal int GetContourEndPoint(int contourIndex)
    {
        ValidateContourIndex(contourIndex);
        return contourEndPoints[contourIndex];
    }

    internal int GetPointCount()
    {
        return points.Length;
    }

    internal SfntSimpleGlyphPoint GetPoint(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= points.Length)
        {
            throw new System.ArgumentOutOfRangeException("pointIndex");
        }

        return points[pointIndex];
    }

    internal int GetInstructionLength()
    {
        return instructions.Length;
    }

    internal byte[] CopyInstructions()
    {
        return instructions.AsSpan().ToArray();
    }

    internal bool HasOverlappingContours()
    {
        return overlappingContours;
    }

    internal int GetTrailingPaddingLength()
    {
        return trailingPaddingLength;
    }

    internal SfntGlyphOutline GetNormalizedOutline()
    {
        lock (normalizationSync)
        {
            if (normalizedOutline == null)
            {
                normalizedOutline = SfntSimpleGlyphNormalizer.Normalize(this);
            }

            return normalizedOutline;
        }
    }

    internal int GetMaximumNormalizedPointCount()
    {
        return maximumNormalizedPointCount;
    }

    void ValidateContourIndex(int contourIndex)
    {
        if (contourIndex < 0 || contourIndex >= contourEndPoints.Length)
        {
            throw new System.ArgumentOutOfRangeException("contourIndex");
        }
    }
}

internal static class SfntGlyphHeaderParser
{
    const int HeaderLength = 10;

    internal static SfntGlyphHeader? Parse(SfntGlyfRecord record)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        if (record.IsEmpty())
        {
            return null;
        }

        if (record.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create(
                "A nonempty glyf record is too short for its header.");
        }

        BigEndianReader reader = new BigEndianReader(record.GetDataSlice());
        return new SfntGlyphHeader(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16());
    }
}

internal static class SfntSimpleGlyphParser
{
    const int HeaderLength = 10;
    const int RepeatFlag = 0x08;
    const int XShortVector = 0x02;
    const int YShortVector = 0x04;
    const int XSameOrPositive = 0x10;
    const int YSameOrPositive = 0x20;
    const int OverlapSimple = 0x40;
    const int ReservedFlag = 0x80;

    internal static SfntSimpleGlyph? Parse(
        SfntGlyfRecord record,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        if (maxp == null)
        {
            throw new System.ArgumentNullException("maxp");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        SfntGlyphHeader? header = SfntGlyphHeaderParser.Parse(record);

        if (header == null || header.GetContourCount() < 0)
        {
            return null;
        }

        int contourCount = header.GetContourCount();

        if (contourCount > limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create(
                "A simple glyph exceeds the configured contour-count limit.");
        }

        BigEndianReader reader = new BigEndianReader(record.GetDataSlice());
        SkipHeader(reader);
        int[] contourEndPoints = ReadContourEndPoints(reader, contourCount);
        int pointCount = contourCount == 0
            ? 0
            : contourEndPoints[contourCount - 1] + 1;

        if (contourCount == 0 && reader.GetRemainingLength() == 0)
        {
            maxp.VerifySimpleGlyph(0, 0, 0, limits);
            return new SfntSimpleGlyph(
                record.GetGlyphId(),
                header,
                contourEndPoints,
                new byte[0],
                new SfntSimpleGlyphPoint[0],
                false,
                0,
                limits.GetMaxPointsPerGlyph());
        }

        int instructionLength = reader.ReadUInt16();
        maxp.VerifySimpleGlyph(
            pointCount,
            contourCount,
            instructionLength,
            limits);
        byte[] instructions = ReadBytes(reader, instructionLength);
        int[] flags = ReadFlags(reader, pointCount);
        int[] xCoordinates = new int[pointCount];
        int[] yCoordinates = new int[pointCount];
        int[] xDeltas = new int[pointCount];
        int[] yDeltas = new int[pointCount];
        ReadCoordinates(reader, flags, true, xCoordinates, xDeltas);
        ReadCoordinates(reader, flags, false, yCoordinates, yDeltas);
        int trailingPaddingLength = ValidateTrailingPadding(reader);
        SfntSimpleGlyphPoint[] points = CreatePoints(
            flags,
            xCoordinates,
            yCoordinates,
            xDeltas,
            yDeltas);
        bool overlappingContours = pointCount != 0 &&
            (flags[0] & OverlapSimple) != 0;

        return new SfntSimpleGlyph(
            record.GetGlyphId(),
            header,
            contourEndPoints,
            instructions,
            points,
            overlappingContours,
            trailingPaddingLength,
            limits.GetMaxPointsPerGlyph());
    }

    static void SkipHeader(BigEndianReader reader)
    {
        int remaining = HeaderLength;

        while (remaining > 0)
        {
            reader.ReadUInt8();
            remaining -= 1;
        }
    }

    static int[] ReadContourEndPoints(
        BigEndianReader reader,
        int contourCount)
    {
        int[] contourEndPoints = new int[contourCount];
        int previousEndPoint = -1;
        int contourIndex = 0;

        while (contourIndex < contourCount)
        {
            int endPoint = reader.ReadUInt16();

            if (endPoint <= previousEndPoint)
            {
                throw FontFormatException.Create(
                    "Simple glyph contour endpoints are not strictly increasing.");
            }

            contourEndPoints[contourIndex] = endPoint;
            previousEndPoint = endPoint;
            contourIndex += 1;
        }

        return contourEndPoints;
    }

    static byte[] ReadBytes(BigEndianReader reader, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;

        while (index < bytes.Length)
        {
            bytes[index] = reader.ReadUInt8();
            index += 1;
        }

        return bytes;
    }

    static int[] ReadFlags(BigEndianReader reader, int pointCount)
    {
        int[] flags = new int[pointCount];
        int pointIndex = 0;

        while (pointIndex < pointCount)
        {
            int encodedFlag = reader.ReadUInt8();

            if ((encodedFlag & ReservedFlag) != 0)
            {
                throw FontFormatException.Create(
                    "A simple glyph flag uses the reserved bit.");
            }

            if ((encodedFlag & OverlapSimple) != 0 && pointIndex != 0)
            {
                throw FontFormatException.Create(
                    "The simple-overlap flag is not on the first flag entry.");
            }

            int repeatCount = (encodedFlag & RepeatFlag) == 0
                ? 0
                : reader.ReadUInt8();

            if (repeatCount > pointCount - pointIndex - 1)
            {
                throw FontFormatException.Create(
                    "A repeated simple-glyph flag exceeds the point count.");
            }

            int normalizedFlag = encodedFlag & ~RepeatFlag;
            int finalPointIndex = pointIndex + repeatCount;

            while (pointIndex <= finalPointIndex)
            {
                flags[pointIndex] = normalizedFlag;
                pointIndex += 1;
            }
        }

        return flags;
    }

    static void ReadCoordinates(
        BigEndianReader reader,
        int[] flags,
        bool readX,
        int[] coordinates,
        int[] deltas)
    {
        int shortMask = readX ? XShortVector : YShortVector;
        int sameOrPositiveMask = readX ? XSameOrPositive : YSameOrPositive;
        int previousCoordinate = 0;
        int pointIndex = 0;

        while (pointIndex < flags.Length)
        {
            int delta;

            if ((flags[pointIndex] & shortMask) != 0)
            {
                int magnitude = reader.ReadUInt8();
                delta = (flags[pointIndex] & sameOrPositiveMask) != 0
                    ? magnitude
                    : -magnitude;
            }
            else if ((flags[pointIndex] & sameOrPositiveMask) != 0)
            {
                delta = 0;
            }
            else
            {
                delta = reader.ReadInt16();
            }

            long coordinate = (long)previousCoordinate + delta;

            if (coordinate < short.MinValue || coordinate > short.MaxValue)
            {
                throw FontFormatException.Create(
                    "A simple glyph point coordinate exceeds the Int16 range.");
            }

            deltas[pointIndex] = delta;
            coordinates[pointIndex] = (int)coordinate;
            previousCoordinate = (int)coordinate;
            pointIndex += 1;
        }
    }

    static int ValidateTrailingPadding(BigEndianReader reader)
    {
        int paddingLength = reader.GetRemainingLength();

        while (reader.GetRemainingLength() != 0)
        {
            if (reader.ReadUInt8() != 0)
            {
                throw FontFormatException.Create(
                    "A simple glyph has nonzero trailing data.");
            }
        }

        return paddingLength;
    }

    static SfntSimpleGlyphPoint[] CreatePoints(
        int[] flags,
        int[] xCoordinates,
        int[] yCoordinates,
        int[] xDeltas,
        int[] yDeltas)
    {
        SfntSimpleGlyphPoint[] points = new SfntSimpleGlyphPoint[flags.Length];
        int pointIndex = 0;

        while (pointIndex < points.Length)
        {
            points[pointIndex] = new SfntSimpleGlyphPoint(
                pointIndex,
                flags[pointIndex],
                xCoordinates[pointIndex],
                yCoordinates[pointIndex],
                xDeltas[pointIndex],
                yDeltas[pointIndex]);
            pointIndex += 1;
        }

        return points;
    }
}
