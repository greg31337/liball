namespace ttf;

internal static class Woff2GlyfTransform
{
    const int HeaderLength = 36;
    const int MoreComponents = 0x0020;
    const int HaveScale = 0x0008;
    const int HaveXAndYScale = 0x0040;
    const int HaveTwoByTwo = 0x0080;
    const int HaveInstructions = 0x0100;

    internal static Woff2GlyfReconstruction Reconstruct(
        Woff2Font font,
        FontLoadOptions options)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        Woff2Table? glyfTable = font.FindTable(FontTags.Glyf);
        Woff2Table? locaTable = font.FindTable(FontTags.Loca);

        if (glyfTable == null || locaTable == null)
        {
            throw FontFormatException.Create("Transformed WOFF 2 glyf reconstruction requires a glyf and loca table pair.");
        }

        return Reconstruct(glyfTable, locaTable, options);
    }

    internal static Woff2GlyfReconstruction Reconstruct(
        Woff2Table glyfTable,
        Woff2Table locaTable,
        FontLoadOptions options)
    {
        if (glyfTable == null)
        {
            throw new System.ArgumentNullException("glyfTable");
        }

        if (locaTable == null)
        {
            throw new System.ArgumentNullException("locaTable");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateTables(glyfTable, locaTable);
        FontLimits limits = options.GetLimits();
        System.Threading.CancellationToken cancellationToken = options.GetCancellationToken();
        cancellationToken.ThrowIfCancellationRequested();

        FontDataSlice transformedData = glyfTable.GetDataSlice();
        BigEndianReader header = new BigEndianReader(transformedData);

        if (header.GetRemainingLength() < HeaderLength)
        {
            throw FontFormatException.Create("The transformed WOFF 2 glyf header is truncated.");
        }

        int reserved = header.ReadUInt16();
        int optionFlags = header.ReadUInt16();
        int numGlyphs = header.ReadUInt16();
        int indexFormat = header.ReadUInt16();

        if (reserved != 0)
        {
            throw FontFormatException.Create("The transformed WOFF 2 glyf reserved field is not zero.");
        }

        if ((optionFlags & 0xFFFE) != 0)
        {
            throw FontFormatException.Create("The transformed WOFF 2 glyf option flags use reserved bits.");
        }

        if (indexFormat != 0 && indexFormat != 1)
        {
            throw FontFormatException.Create("The transformed WOFF 2 glyf index format is invalid.");
        }

        if (numGlyphs > limits.GetMaxGlyphsPerFace())
        {
            throw FontLimitException.Create("The transformed WOFF 2 glyf glyph count exceeds the configured limit.");
        }

        int nContourSize = ReadStreamSize(header);
        int nPointsSize = ReadStreamSize(header);
        int flagSize = ReadStreamSize(header);
        int glyphSize = ReadStreamSize(header);
        int compositeSize = ReadStreamSize(header);
        int bboxSize = ReadStreamSize(header);
        int instructionSize = ReadStreamSize(header);
        int overlapSize = (optionFlags & 1) == 0 ? 0 : (numGlyphs + 7) / 8;

        if (nContourSize != numGlyphs * 2)
        {
            throw FontFormatException.Create("The transformed WOFF 2 contour stream has an invalid size.");
        }

        int streamOffset = HeaderLength;
        BigEndianReader nContourReader = CreateStreamReader(transformedData, streamOffset, nContourSize);
        streamOffset = AddStreamOffset(streamOffset, nContourSize);
        BigEndianReader nPointsReader = CreateStreamReader(transformedData, streamOffset, nPointsSize);
        streamOffset = AddStreamOffset(streamOffset, nPointsSize);
        BigEndianReader flagReader = CreateStreamReader(transformedData, streamOffset, flagSize);
        streamOffset = AddStreamOffset(streamOffset, flagSize);
        BigEndianReader glyphReader = CreateStreamReader(transformedData, streamOffset, glyphSize);
        streamOffset = AddStreamOffset(streamOffset, glyphSize);
        BigEndianReader compositeReader = CreateStreamReader(transformedData, streamOffset, compositeSize);
        streamOffset = AddStreamOffset(streamOffset, compositeSize);
        BigEndianReader bboxReader = CreateStreamReader(transformedData, streamOffset, bboxSize);
        streamOffset = AddStreamOffset(streamOffset, bboxSize);
        BigEndianReader instructionReader = CreateStreamReader(transformedData, streamOffset, instructionSize);
        streamOffset = AddStreamOffset(streamOffset, instructionSize);
        BigEndianReader overlapReader = CreateStreamReader(transformedData, streamOffset, overlapSize);
        streamOffset = AddStreamOffset(streamOffset, overlapSize);

        if (streamOffset != transformedData.GetLength())
        {
            throw FontFormatException.Create("The transformed WOFF 2 glyf stream sizes do not cover the table.");
        }

        int bboxBitmapSize = ((numGlyphs + 31) / 32) * 4;

        if (bboxSize < bboxBitmapSize)
        {
            throw FontFormatException.Create("The transformed WOFF 2 bounding-box bitmap is truncated.");
        }

        byte[] bboxBitmap = ReadBytes(bboxReader, bboxBitmapSize);
        byte[] overlapBitmap = ReadBytes(overlapReader, overlapSize);
        int locaLength = (numGlyphs + 1) * (indexFormat == 0 ? 2 : 4);

        if (locaTable.GetOriginalLength() != locaLength)
        {
            throw FontFormatException.Create("The transformed WOFF 2 loca original length is invalid.");
        }

        if (locaLength > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create("The reconstructed WOFF 2 loca table exceeds the configured output limit.");
        }

        BigEndianWriter glyfWriter = BigEndianWriter.Create(limits.GetMaxOutputBytes());
        int[] locaOffsets = new int[numGlyphs + 1];
        short[] xMins = new short[numGlyphs];
        short[] yMins = new short[numGlyphs];
        short[] xMaxs = new short[numGlyphs];
        short[] yMaxs = new short[numGlyphs];
        int glyphIndex = 0;

        while (glyphIndex < numGlyphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            locaOffsets[glyphIndex] = glyfWriter.GetLength();
            short contourCount = nContourReader.ReadInt16();
            bool hasExplicitBounds = IsBitmapBitSet(bboxBitmap, glyphIndex);

            if (contourCount == 0)
            {
                if (hasExplicitBounds)
                {
                    throw FontFormatException.Create("An empty transformed WOFF 2 glyph has explicit bounds.");
                }
            }
            else if (contourCount > 0)
            {
                ReconstructSimpleGlyph(
                    glyfWriter,
                    contourCount,
                    glyphIndex,
                    hasExplicitBounds,
                    IsBitmapBitSet(overlapBitmap, glyphIndex),
                    nPointsReader,
                    flagReader,
                    glyphReader,
                    bboxReader,
                    instructionReader,
                    xMins,
                    yMins,
                    xMaxs,
                    yMaxs,
                    limits);
                PadGlyph(glyfWriter);
            }
            else if (contourCount == -1)
            {
                if (!hasExplicitBounds)
                {
                    throw FontFormatException.Create("A composite transformed WOFF 2 glyph is missing explicit bounds.");
                }

                ReconstructCompositeGlyph(
                    glyfWriter,
                    glyphIndex,
                    numGlyphs,
                    glyphReader,
                    compositeReader,
                    bboxReader,
                    instructionReader,
                    xMins,
                    yMins,
                    xMaxs,
                    yMaxs,
                    limits);
                PadGlyph(glyfWriter);
            }
            else
            {
                throw FontFormatException.Create("A transformed WOFF 2 glyph has an invalid contour count.");
            }

            glyphIndex += 1;
        }

        locaOffsets[numGlyphs] = glyfWriter.GetLength();
        EnsureConsumed(nContourReader, "contour");
        EnsureConsumed(nPointsReader, "point-count");
        EnsureConsumed(flagReader, "flag");
        EnsureConsumed(glyphReader, "glyph");
        EnsureConsumed(compositeReader, "composite");
        EnsureConsumed(bboxReader, "bounding-box");
        EnsureConsumed(instructionReader, "instruction");
        EnsureConsumed(overlapReader, "overlap");

        if (glyfWriter.GetLength() > limits.GetMaxOutputBytes() - locaLength)
        {
            throw FontLimitException.Create("The reconstructed WOFF 2 glyf and loca tables exceed the configured output limit.");
        }

        byte[] locaData = BuildLoca(locaOffsets, indexFormat, limits.GetMaxOutputBytes());
        return new Woff2GlyfReconstruction(
            glyfWriter.CopyBytes(),
            locaData,
            numGlyphs,
            indexFormat,
            xMins,
            yMins,
            xMaxs,
            yMaxs);
    }

    static void ValidateTables(Woff2Table glyfTable, Woff2Table locaTable)
    {
        if (glyfTable.GetTag() != FontTags.Glyf || locaTable.GetTag() != FontTags.Loca)
        {
            throw new System.ArgumentException("The WOFF 2 transform requires glyf and loca tables.");
        }

        if (!glyfTable.IsTransformed() || glyfTable.GetTransformVersion() != 0 ||
            !locaTable.IsTransformed() || locaTable.GetTransformVersion() != 0)
        {
            throw new System.InvalidOperationException("The WOFF 2 glyf and loca tables do not use transform version zero.");
        }

        if (locaTable.GetTransformLength() != 0 || locaTable.GetDataLength() != 0)
        {
            throw FontFormatException.Create("The transformed WOFF 2 loca table contains prohibited data.");
        }
    }

    static int ReadStreamSize(BigEndianReader reader)
    {
        uint value = reader.ReadUInt32();

        if (value > int.MaxValue)
        {
            throw FontLimitException.Create("A transformed WOFF 2 glyf stream is too large.");
        }

        return (int)value;
    }

    static BigEndianReader CreateStreamReader(FontDataSlice data, int offset, int length)
    {
        return new BigEndianReader(data.GetSlice(offset, length));
    }

    static int AddStreamOffset(int offset, int length)
    {
        return CheckedFontMath.AddOffset(offset, length);
    }

    static byte[] ReadBytes(BigEndianReader reader, int length)
    {
        byte[] result = new byte[length];
        int index = 0;

        while (index < length)
        {
            result[index] = reader.ReadUInt8();
            index += 1;
        }

        return result;
    }

    static bool IsBitmapBitSet(byte[] bitmap, int index)
    {
        if (bitmap.Length == 0)
        {
            return false;
        }

        return (bitmap[index / 8] & (0x80 >> (index % 8))) != 0;
    }

    static void ReconstructSimpleGlyph(
        BigEndianWriter writer,
        short contourCount,
        int glyphIndex,
        bool hasExplicitBounds,
        bool hasOverlap,
        BigEndianReader nPointsReader,
        BigEndianReader flagReader,
        BigEndianReader glyphReader,
        BigEndianReader bboxReader,
        BigEndianReader instructionReader,
        short[] xMins,
        short[] yMins,
        short[] xMaxs,
        short[] yMaxs,
        FontLimits limits)
    {
        if (contourCount > limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create("A transformed WOFF 2 glyph exceeds the contour limit.");
        }

        ushort[] endPoints = new ushort[contourCount];
        int pointCount = 0;
        int contourIndex = 0;

        while (contourIndex < contourCount)
        {
            int contourPoints = Woff2Integer.Read255UInt16(nPointsReader);

            if (contourPoints == 0)
            {
                throw FontFormatException.Create("A transformed WOFF 2 simple contour has no points.");
            }

            if (pointCount > 65536 - contourPoints)
            {
                throw FontFormatException.Create("A transformed WOFF 2 glyph has too many encodable points.");
            }

            pointCount += contourPoints;

            if (pointCount > limits.GetMaxPointsPerGlyph())
            {
                throw FontLimitException.Create("A transformed WOFF 2 glyph exceeds the point limit.");
            }

            endPoints[contourIndex] = (ushort)(pointCount - 1);
            contourIndex += 1;
        }

        byte[] transformedFlags = ReadBytes(flagReader, pointCount);
        byte[] outputFlags = new byte[pointCount];
        int[] xCoordinates = new int[pointCount];
        int[] yCoordinates = new int[pointCount];
        int pointIndex = 0;
        int previousX = 0;
        int previousY = 0;

        while (pointIndex < pointCount)
        {
            int transformedFlag = transformedFlags[pointIndex];
            long packedDelta = DecodeTriplet(glyphReader, transformedFlag & 0x7F);
            int deltaX = (int)(packedDelta >> 32);
            uint encodedDeltaY = (uint)(packedDelta & 0xFFFFFFFFL);
            int deltaY = CheckedFontMath.ReinterpretUInt32AsInt32(encodedDeltaY);
            long nextX = (long)previousX + deltaX;
            long nextY = (long)previousY + deltaY;

            if (nextX < short.MinValue || nextX > short.MaxValue ||
                nextY < short.MinValue || nextY > short.MaxValue)
            {
                throw FontFormatException.Create("A transformed WOFF 2 point coordinate exceeds the Int16 range.");
            }

            xCoordinates[pointIndex] = (int)nextX;
            yCoordinates[pointIndex] = (int)nextY;
            outputFlags[pointIndex] = BuildPointFlag(
                (transformedFlag & 0x80) == 0,
                deltaX,
                deltaY,
                hasOverlap && pointIndex == 0);
            previousX = (int)nextX;
            previousY = (int)nextY;
            pointIndex += 1;
        }

        short xMin = (short)xCoordinates[0];
        short yMin = (short)yCoordinates[0];
        short xMax = xMin;
        short yMax = yMin;
        pointIndex = 1;

        while (pointIndex < pointCount)
        {
            if (xCoordinates[pointIndex] < xMin)
            {
                xMin = (short)xCoordinates[pointIndex];
            }

            if (xCoordinates[pointIndex] > xMax)
            {
                xMax = (short)xCoordinates[pointIndex];
            }

            if (yCoordinates[pointIndex] < yMin)
            {
                yMin = (short)yCoordinates[pointIndex];
            }

            if (yCoordinates[pointIndex] > yMax)
            {
                yMax = (short)yCoordinates[pointIndex];
            }

            pointIndex += 1;
        }

        if (hasExplicitBounds)
        {
            xMin = bboxReader.ReadInt16();
            yMin = bboxReader.ReadInt16();
            xMax = bboxReader.ReadInt16();
            yMax = bboxReader.ReadInt16();
        }

        xMins[glyphIndex] = xMin;
        yMins[glyphIndex] = yMin;
        xMaxs[glyphIndex] = xMax;
        yMaxs[glyphIndex] = yMax;
        int instructionLength = Woff2Integer.Read255UInt16(glyphReader);
        byte[] instructions = ReadBytes(instructionReader, instructionLength);

        writer.WriteInt16(contourCount);
        writer.WriteInt16(xMin);
        writer.WriteInt16(yMin);
        writer.WriteInt16(xMax);
        writer.WriteInt16(yMax);
        contourIndex = 0;

        while (contourIndex < endPoints.Length)
        {
            writer.WriteUInt16(endPoints[contourIndex]);
            contourIndex += 1;
        }

        writer.WriteUInt16((ushort)instructionLength);
        writer.WriteBytes(instructions);
        writer.WriteBytes(outputFlags);
        WriteCoordinateDeltas(writer, xCoordinates, outputFlags, true);
        WriteCoordinateDeltas(writer, yCoordinates, outputFlags, false);
    }

    static long DecodeTriplet(BigEndianReader reader, int flag)
    {
        int deltaX;
        int deltaY;

        if (flag < 10)
        {
            deltaX = 0;
            deltaY = ApplyTripletSign(flag, ((flag & 14) << 7) + reader.ReadUInt8());
        }
        else if (flag < 20)
        {
            deltaX = ApplyTripletSign(flag, (((flag - 10) & 14) << 7) + reader.ReadUInt8());
            deltaY = 0;
        }
        else if (flag < 84)
        {
            int adjustedFlag = flag - 20;
            int packed = reader.ReadUInt8();
            deltaX = ApplyTripletSign(flag, 1 + (adjustedFlag & 0x30) + (packed >> 4));
            deltaY = ApplyTripletSign(flag >> 1, 1 + ((adjustedFlag & 0x0C) << 2) + (packed & 0x0F));
        }
        else if (flag < 120)
        {
            int adjustedFlag = flag - 84;
            deltaX = ApplyTripletSign(flag, 1 + (adjustedFlag / 12 << 8) + reader.ReadUInt8());
            deltaY = ApplyTripletSign(flag >> 1, 1 + ((adjustedFlag % 12) / 4 << 8) + reader.ReadUInt8());
        }
        else if (flag < 124)
        {
            int first = reader.ReadUInt8();
            int second = reader.ReadUInt8();
            int packed = reader.ReadUInt8();
            deltaX = ApplyTripletSign(flag, (first << 4) + (packed >> 4));
            deltaY = ApplyTripletSign(flag >> 1, (second << 4) + (packed & 0x0F));
        }
        else
        {
            int first = reader.ReadUInt8();
            int second = reader.ReadUInt8();
            int third = reader.ReadUInt8();
            int fourth = reader.ReadUInt8();
            deltaX = ApplyTripletSign(flag, (first << 8) + second);
            deltaY = ApplyTripletSign(flag >> 1, (third << 8) + fourth);
        }

        uint encodedDeltaX = CheckedFontMath.ReinterpretInt32AsUInt32(deltaX);
        uint encodedDeltaY = CheckedFontMath.ReinterpretInt32AsUInt32(deltaY);
        return ((long)encodedDeltaX << 32) | encodedDeltaY;
    }

    static int ApplyTripletSign(int flag, int magnitude)
    {
        return (flag & 1) == 0 ? -magnitude : magnitude;
    }

    static byte BuildPointFlag(bool onCurve, int deltaX, int deltaY, bool overlap)
    {
        int flag = onCurve ? 1 : 0;

        if (deltaX == 0)
        {
            flag |= 0x10;
        }
        else if (deltaX >= -255 && deltaX <= 255)
        {
            flag |= 0x02;

            if (deltaX > 0)
            {
                flag |= 0x10;
            }
        }

        if (deltaY == 0)
        {
            flag |= 0x20;
        }
        else if (deltaY >= -255 && deltaY <= 255)
        {
            flag |= 0x04;

            if (deltaY > 0)
            {
                flag |= 0x20;
            }
        }

        if (overlap)
        {
            flag |= 0x40;
        }

        return (byte)flag;
    }

    static void WriteCoordinateDeltas(
        BigEndianWriter writer,
        int[] coordinates,
        byte[] flags,
        bool writeX)
    {
        int previous = 0;
        int index = 0;

        while (index < coordinates.Length)
        {
            int delta = coordinates[index] - previous;
            int shortMask = writeX ? 0x02 : 0x04;
            int sameMask = writeX ? 0x10 : 0x20;

            if ((flags[index] & shortMask) != 0)
            {
                writer.WriteUInt8((byte)(delta < 0 ? -delta : delta));
            }
            else if ((flags[index] & sameMask) == 0)
            {
                if (delta < short.MinValue || delta > short.MaxValue)
                {
                    throw FontFormatException.Create("A reconstructed TrueType coordinate delta exceeds the Int16 range.");
                }

                writer.WriteInt16((short)delta);
            }

            previous = coordinates[index];
            index += 1;
        }
    }

    static void ReconstructCompositeGlyph(
        BigEndianWriter writer,
        int glyphIndex,
        int numGlyphs,
        BigEndianReader glyphReader,
        BigEndianReader compositeReader,
        BigEndianReader bboxReader,
        BigEndianReader instructionReader,
        short[] xMins,
        short[] yMins,
        short[] xMaxs,
        short[] yMaxs,
        FontLimits limits)
    {
        short xMin = bboxReader.ReadInt16();
        short yMin = bboxReader.ReadInt16();
        short xMax = bboxReader.ReadInt16();
        short yMax = bboxReader.ReadInt16();
        xMins[glyphIndex] = xMin;
        yMins[glyphIndex] = yMin;
        xMaxs[glyphIndex] = xMax;
        yMaxs[glyphIndex] = yMax;

        BigEndianWriter componentWriter = BigEndianWriter.Create(limits.GetMaxOutputBytes());
        int flags = MoreComponents;
        int finalFlagOffset = 0;
        bool hasInstructions = false;
        int componentCount = 0;

        while ((flags & MoreComponents) != 0)
        {
            finalFlagOffset = componentWriter.GetLength();
            flags = compositeReader.ReadUInt16();
            int transformKinds = 0;

            if ((flags & HaveScale) != 0)
            {
                transformKinds += 1;
            }

            if ((flags & HaveXAndYScale) != 0)
            {
                transformKinds += 1;
            }

            if ((flags & HaveTwoByTwo) != 0)
            {
                transformKinds += 1;
            }

            if (transformKinds > 1 || (flags & 0xE010) != 0 || (flags & 0x1800) == 0x1800)
            {
                throw FontFormatException.Create("A transformed WOFF 2 composite component has invalid flags.");
            }

            componentWriter.WriteUInt16((ushort)flags);
            int referencedGlyph = compositeReader.ReadUInt16();

            if (referencedGlyph >= numGlyphs)
            {
                throw FontFormatException.Create("A transformed WOFF 2 composite references an invalid glyph.");
            }

            componentWriter.WriteUInt16((ushort)referencedGlyph);
            int argumentLength = (flags & 1) == 0 ? 2 : 4;
            componentWriter.WriteBytes(ReadBytes(compositeReader, argumentLength));

            if ((flags & HaveScale) != 0)
            {
                componentWriter.WriteBytes(ReadBytes(compositeReader, 2));
            }
            else if ((flags & HaveXAndYScale) != 0)
            {
                componentWriter.WriteBytes(ReadBytes(compositeReader, 4));
            }
            else if ((flags & HaveTwoByTwo) != 0)
            {
                componentWriter.WriteBytes(ReadBytes(compositeReader, 8));
            }

            if ((flags & HaveInstructions) != 0)
            {
                hasInstructions = true;
            }

            componentCount += 1;

            if (componentCount > limits.GetMaxPointsPerGlyph())
            {
                throw FontLimitException.Create("A transformed WOFF 2 composite exceeds the component limit.");
            }
        }

        byte[] componentData = componentWriter.CopyBytes();

        if (hasInstructions)
        {
            componentData[finalFlagOffset] |= 0x01;
        }

        writer.WriteInt16(-1);
        writer.WriteInt16(xMin);
        writer.WriteInt16(yMin);
        writer.WriteInt16(xMax);
        writer.WriteInt16(yMax);
        writer.WriteBytes(componentData);

        if (hasInstructions)
        {
            int instructionLength = Woff2Integer.Read255UInt16(glyphReader);
            writer.WriteUInt16((ushort)instructionLength);
            writer.WriteBytes(ReadBytes(instructionReader, instructionLength));
        }
    }

    static void PadGlyph(BigEndianWriter writer)
    {
        if ((writer.GetLength() & 1) != 0)
        {
            writer.WriteUInt8(0);
        }
    }

    static void EnsureConsumed(BigEndianReader reader, string streamName)
    {
        if (reader.GetRemainingLength() != 0)
        {
            throw FontFormatException.Create($"The transformed WOFF 2 {streamName} stream has trailing data.");
        }
    }

    static byte[] BuildLoca(int[] offsets, int indexFormat, int maximumLength)
    {
        BigEndianWriter writer = BigEndianWriter.Create(maximumLength);
        int index = 0;

        while (index < offsets.Length)
        {
            int offset = offsets[index];

            if (indexFormat == 0)
            {
                if ((offset & 1) != 0 || offset / 2 > ushort.MaxValue)
                {
                    throw FontFormatException.Create("The reconstructed WOFF 2 glyf offsets do not fit short loca format.");
                }

                writer.WriteUInt16((ushort)(offset / 2));
            }
            else
            {
                writer.WriteUInt32((uint)offset);
            }

            index += 1;
        }

        return writer.CopyBytes();
    }
}

internal class Woff2GlyfReconstruction
{
    FontData glyfData;
    FontData locaData;
    int glyphCount;
    int indexFormat;
    short[] xMins;
    short[] yMins;
    short[] xMaxs;
    short[] yMaxs;

    internal Woff2GlyfReconstruction(
        byte[] glyfData,
        byte[] locaData,
        int glyphCount,
        int indexFormat,
        short[] xMins,
        short[] yMins,
        short[] xMaxs,
        short[] yMaxs)
    {
        this.glyfData = FontData.Create(glyfData);
        this.locaData = FontData.Create(locaData);
        this.glyphCount = glyphCount;
        this.indexFormat = indexFormat;
        this.xMins = xMins;
        this.yMins = yMins;
        this.xMaxs = xMaxs;
        this.yMaxs = yMaxs;
    }

    internal int GetGlyphCount()
    {
        return glyphCount;
    }

    internal int GetIndexFormat()
    {
        return indexFormat;
    }

    internal byte[] CopyGlyfData()
    {
        return glyfData.CopyBytes();
    }

    internal byte[] CopyLocaData()
    {
        return locaData.CopyBytes();
    }

    internal short GetXMin(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        return xMins[glyphIndex];
    }

    internal short GetYMin(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        return yMins[glyphIndex];
    }

    internal short GetXMax(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        return xMaxs[glyphIndex];
    }

    internal short GetYMax(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        return yMaxs[glyphIndex];
    }

    void ValidateGlyphIndex(int glyphIndex)
    {
        if (glyphIndex < 0 || glyphIndex >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }
    }
}
