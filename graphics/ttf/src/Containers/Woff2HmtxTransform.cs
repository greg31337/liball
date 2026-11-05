namespace ttf;

internal static class Woff2HmtxTransform
{
    internal static Woff2HmtxReconstruction Reconstruct(
        Woff2Font font,
        FontLoadOptions options)
    {
        return Reconstruct(font, 0, options);
    }

    internal static Woff2HmtxReconstruction Reconstruct(
        Woff2Font font,
        int faceIndex,
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

        ValidateFaceIndex(font, faceIndex);
        options.GetCancellationToken().ThrowIfCancellationRequested();

        Woff2Table? hmtxTable = FindFaceTable(font, faceIndex, FontTags.Hmtx);
        Woff2Table? hheaTable = FindFaceTable(font, faceIndex, FontTags.Hhea);
        Woff2Table? maxpTable = FindFaceTable(font, faceIndex, FontTags.Maxp);
        Woff2Table? glyfTable = FindFaceTable(font, faceIndex, FontTags.Glyf);
        Woff2Table? locaTable = FindFaceTable(font, faceIndex, FontTags.Loca);

        if (hmtxTable == null || hheaTable == null || maxpTable == null ||
            glyfTable == null || locaTable == null)
        {
            throw FontFormatException.Create(
                "Transformed WOFF 2 hmtx reconstruction requires hmtx, hhea, maxp, glyf, and loca tables.");
        }

        ValidateHmtxTable(hmtxTable);
        int glyphCount = ReadGlyphCount(maxpTable, options.GetLimits());
        int metricCount = ReadMetricCount(hheaTable, glyphCount);
        short[] xMins = ReadXMins(
            font,
            faceIndex,
            glyfTable,
            locaTable,
            glyphCount,
            options);

        return ReconstructTable(
            hmtxTable,
            glyphCount,
            metricCount,
            xMins,
            options);
    }

    static void ValidateFaceIndex(Woff2Font font, int faceIndex)
    {
        if (font.GetCollection().IsPresent())
        {
            if (faceIndex < 0 || faceIndex >= font.GetCollection().GetFaceCount())
            {
                throw new System.ArgumentOutOfRangeException("faceIndex");
            }

            return;
        }

        if (faceIndex != 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }
    }

    static Woff2Table? FindFaceTable(Woff2Font font, int faceIndex, int tag)
    {
        Woff2Table? result = null;

        if (!font.GetCollection().IsPresent())
        {
            int tableIndex = 0;

            while (tableIndex < font.GetTableCount())
            {
                Woff2Table candidate = font.GetTable(tableIndex);

                if (candidate.GetTag() == tag)
                {
                    if (result != null)
                    {
                        throw FontFormatException.Create("A WOFF 2 font contains duplicate dependency tables.");
                    }

                    result = candidate;
                }

                tableIndex += 1;
            }

            return result;
        }

        Woff2CollectionFace face = font.GetCollection().GetFace(faceIndex);
        int faceTableIndex = 0;

        while (faceTableIndex < face.GetTableCount())
        {
            Woff2Table candidate = font.GetTable(face.GetTableIndex(faceTableIndex));

            if (candidate.GetTag() == tag)
            {
                if (result != null)
                {
                    throw FontFormatException.Create("A WOFF 2 collection face contains duplicate dependency tables.");
                }

                result = candidate;
            }

            faceTableIndex += 1;
        }

        return result;
    }

    static void ValidateHmtxTable(Woff2Table table)
    {
        if (!table.IsTransformed() || table.GetTransformVersion() != 1)
        {
            throw new System.InvalidOperationException(
                "The WOFF 2 hmtx table does not use transform version one.");
        }
    }

    static int ReadGlyphCount(Woff2Table maxpTable, FontLimits limits)
    {
        if (maxpTable.IsTransformed() || maxpTable.GetDataLength() < 32)
        {
            throw FontFormatException.Create("The maxp dependency for transformed WOFF 2 hmtx is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(maxpTable.GetDataSlice());

        if (reader.ReadUInt32() != 0x00010000U)
        {
            throw FontFormatException.Create("Transformed WOFF 2 hmtx requires a TrueType maxp table.");
        }

        int glyphCount = reader.ReadUInt16();

        if (glyphCount == 0)
        {
            throw FontFormatException.Create("The maxp dependency has no glyphs.");
        }

        if (glyphCount > limits.GetMaxGlyphsPerFace())
        {
            throw FontLimitException.Create("The maxp glyph count exceeds the configured glyph limit.");
        }

        return glyphCount;
    }

    static int ReadMetricCount(Woff2Table hheaTable, int glyphCount)
    {
        if (hheaTable.IsTransformed() || hheaTable.GetDataLength() < 36)
        {
            throw FontFormatException.Create("The hhea dependency for transformed WOFF 2 hmtx is invalid.");
        }

        BigEndianReader versionReader = new BigEndianReader(hheaTable.GetDataSlice());

        if (versionReader.ReadUInt32() != 0x00010000U)
        {
            throw FontFormatException.Create("The hhea dependency has an unsupported version.");
        }

        BigEndianReader metricReader = new BigEndianReader(hheaTable.GetDataSlice().GetSlice(34, 2));
        int metricCount = metricReader.ReadUInt16();

        if (metricCount == 0 || metricCount > glyphCount)
        {
            throw FontFormatException.Create("The hhea numberOfHMetrics value is inconsistent with maxp.");
        }

        return metricCount;
    }

    static short[] ReadXMins(
        Woff2Font font,
        int faceIndex,
        Woff2Table glyfTable,
        Woff2Table locaTable,
        int glyphCount,
        FontLoadOptions options)
    {
        if (glyfTable.IsTransformed() || locaTable.IsTransformed())
        {
            if (!glyfTable.IsTransformed() || !locaTable.IsTransformed())
            {
                throw FontFormatException.Create("The WOFF 2 glyf and loca dependency transforms do not match.");
            }

            Woff2GlyfReconstruction reconstruction = Woff2GlyfTransform.Reconstruct(
                glyfTable,
                locaTable,
                options);

            if (reconstruction.GetGlyphCount() != glyphCount)
            {
                throw FontFormatException.Create("The transformed glyf glyph count does not match maxp.");
            }

            short[] transformedXMins = new short[glyphCount];
            int glyphIndex = 0;

            while (glyphIndex < glyphCount)
            {
                transformedXMins[glyphIndex] = reconstruction.GetXMin(glyphIndex);
                glyphIndex += 1;
            }

            return transformedXMins;
        }

        Woff2Table? headTable = FindFaceTable(font, faceIndex, FontTags.Head);

        if (headTable == null)
        {
            throw FontFormatException.Create("Null-transform glyf bounds require a head dependency.");
        }

        return ReadNullTransformXMins(
            headTable,
            glyfTable,
            locaTable,
            glyphCount,
            options.GetCancellationToken());
    }

    static short[] ReadNullTransformXMins(
        Woff2Table headTable,
        Woff2Table glyfTable,
        Woff2Table locaTable,
        int glyphCount,
        System.Threading.CancellationToken cancellationToken)
    {
        if (headTable.IsTransformed() || glyfTable.IsTransformed() || locaTable.IsTransformed() ||
            headTable.GetDataLength() < 54)
        {
            throw FontFormatException.Create("The null-transform glyf dependencies for hmtx are invalid.");
        }

        BigEndianReader formatReader = new BigEndianReader(headTable.GetDataSlice().GetSlice(50, 2));
        int indexFormat = formatReader.ReadInt16();

        if (indexFormat != 0 && indexFormat != 1)
        {
            throw FontFormatException.Create("The head indexToLocFormat value is invalid.");
        }

        int expectedLocaLength = (glyphCount + 1) * (indexFormat == 0 ? 2 : 4);

        if (locaTable.GetDataLength() != expectedLocaLength ||
            locaTable.GetOriginalLength() != expectedLocaLength)
        {
            throw FontFormatException.Create("The loca dependency length is inconsistent with maxp and head.");
        }

        BigEndianReader locaReader = new BigEndianReader(locaTable.GetDataSlice());
        int[] offsets = new int[glyphCount + 1];
        int offsetIndex = 0;

        while (offsetIndex < offsets.Length)
        {
            uint encodedOffset = indexFormat == 0 ? locaReader.ReadUInt16() : locaReader.ReadUInt32();
            long decodedOffset = indexFormat == 0 ? encodedOffset * 2L : encodedOffset;

            if (decodedOffset > int.MaxValue)
            {
                throw FontFormatException.Create("A loca dependency offset exceeds the supported range.");
            }

            offsets[offsetIndex] = (int)decodedOffset;

            if (offsetIndex != 0 && offsets[offsetIndex] < offsets[offsetIndex - 1])
            {
                throw FontFormatException.Create("The loca dependency offsets are not monotonic.");
            }

            offsetIndex += 1;
        }

        if (offsets[glyphCount] > glyfTable.GetDataLength())
        {
            throw FontFormatException.Create("The loca dependency points outside the glyf table.");
        }

        short[] xMins = new short[glyphCount];
        int glyphIndex = 0;

        while (glyphIndex < glyphCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int glyphLength = offsets[glyphIndex + 1] - offsets[glyphIndex];

            if (glyphLength != 0)
            {
                if (glyphLength < 10)
                {
                    throw FontFormatException.Create("A glyf dependency record is too short for its bounds.");
                }

                BigEndianReader glyphReader = new BigEndianReader(
                    glyfTable.GetDataSlice().GetSlice(offsets[glyphIndex], glyphLength));
                short contourCount = glyphReader.ReadInt16();
                short xMin = glyphReader.ReadInt16();
                short yMin = glyphReader.ReadInt16();
                short xMax = glyphReader.ReadInt16();
                short yMax = glyphReader.ReadInt16();

                if (contourCount == 0)
                {
                    if (xMin != 0 || yMin != 0 || xMax != 0 || yMax != 0)
                    {
                        throw FontFormatException.Create(
                            "An empty glyf dependency has nonzero bounds.");
                    }
                }
                else
                {
                    xMins[glyphIndex] = xMin;
                }
            }

            glyphIndex += 1;
        }

        return xMins;
    }

    static Woff2HmtxReconstruction ReconstructTable(
        Woff2Table hmtxTable,
        int glyphCount,
        int metricCount,
        short[] xMins,
        FontLoadOptions options)
    {
        int expectedLength = metricCount * 4 + (glyphCount - metricCount) * 2;

        if (hmtxTable.GetOriginalLength() != expectedLength)
        {
            throw FontFormatException.Create("The transformed WOFF 2 hmtx original length is invalid.");
        }

        if (expectedLength > options.GetLimits().GetMaxOutputBytes())
        {
            throw FontLimitException.Create("The reconstructed WOFF 2 hmtx exceeds the configured output limit.");
        }

        BigEndianReader reader = new BigEndianReader(hmtxTable.GetDataSlice());
        int flags = reader.ReadUInt8();

        if ((flags & 3) == 0 || (flags & 0xFC) != 0)
        {
            throw FontFormatException.Create("The transformed WOFF 2 hmtx flags are invalid.");
        }

        ushort[] advanceWidths = new ushort[metricCount];
        short[] leftSideBearings = new short[glyphCount];
        int index = 0;

        while (index < metricCount)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            advanceWidths[index] = reader.ReadUInt16();
            index += 1;
        }

        index = 0;

        while (index < metricCount)
        {
            leftSideBearings[index] = (flags & 1) == 0 ? reader.ReadInt16() : xMins[index];
            index += 1;
        }

        while (index < glyphCount)
        {
            leftSideBearings[index] = (flags & 2) == 0 ? reader.ReadInt16() : xMins[index];
            index += 1;
        }

        if (reader.GetRemainingLength() != 0)
        {
            throw FontFormatException.Create("The transformed WOFF 2 hmtx table has trailing data.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            expectedLength,
            options.GetLimits().GetMaxOutputBytes());
        index = 0;

        while (index < metricCount)
        {
            writer.WriteUInt16(advanceWidths[index]);
            writer.WriteInt16(leftSideBearings[index]);
            index += 1;
        }

        while (index < glyphCount)
        {
            writer.WriteInt16(leftSideBearings[index]);
            index += 1;
        }

        return new Woff2HmtxReconstruction(
            writer.CopyBytes(),
            glyphCount,
            metricCount,
            advanceWidths,
            leftSideBearings);
    }
}

internal class Woff2HmtxReconstruction
{
    FontData data;
    int glyphCount;
    int metricCount;
    ushort[] advanceWidths;
    short[] leftSideBearings;

    internal Woff2HmtxReconstruction(
        byte[] data,
        int glyphCount,
        int metricCount,
        ushort[] advanceWidths,
        short[] leftSideBearings)
    {
        this.data = FontData.Create(data);
        this.glyphCount = glyphCount;
        this.metricCount = metricCount;
        this.advanceWidths = advanceWidths;
        this.leftSideBearings = leftSideBearings;
    }

    internal int GetGlyphCount()
    {
        return glyphCount;
    }

    internal int GetMetricCount()
    {
        return metricCount;
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }

    internal ushort GetAdvanceWidth(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        int metricIndex = glyphIndex < metricCount ? glyphIndex : metricCount - 1;
        return advanceWidths[metricIndex];
    }

    internal short GetLeftSideBearing(int glyphIndex)
    {
        ValidateGlyphIndex(glyphIndex);
        return leftSideBearings[glyphIndex];
    }

    void ValidateGlyphIndex(int glyphIndex)
    {
        if (glyphIndex < 0 || glyphIndex >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }
    }
}
