namespace ttf;

internal class SfntStaticCffMetricData
{
    byte[] metrics;
    byte[] header;

    internal SfntStaticCffMetricData(byte[] metrics, byte[] header)
    {
        this.metrics = metrics;
        this.header = header;
    }

    internal byte[] GetMetrics()
    {
        return metrics;
    }

    internal byte[] GetHeader()
    {
        return header;
    }
}

internal static class SfntStaticCff2Instancer
{
    const int FirstCustomSid = 391;
    const int MaximumCustomStringCount = 65145;

    internal static byte[] Create(FontInstance instance)
    {
        ValidateInstance(instance);
        FontFace face = instance.GetFace();
        FontLimits limits = face.GetTableCache().GetLimits();

        int[] globalBounds = CalculateGlobalBounds(instance);
        byte[] cff = CreateCff(instance, globalBounds, limits);
        SfntStaticCffMetricData horizontal = CreateMetricData(
            instance,
            face,
            false,
            limits);
        bool hasVhea = face.HasTable(FontTags.Vhea);
        bool hasVmtx = face.HasTable(FontTags.Vmtx);

        if (hasVhea != hasVmtx)
        {
            throw FontFormatException.Create(
                "Static CFF2 instancing requires paired vertical metric tables.");
        }

        SfntStaticCffMetricData vertical = CreateMetricData(
            instance,
            face,
            true,
            limits);
        byte[]? vorg = face.HasTable(FontTags.Vorg) ?
            CreateVorg(instance, limits) : null;
        SfntStaticInstanceMetadata metadata =
            new SfntStaticInstanceMetadata(
                instance,
                globalBounds);
        SfntTableStore store = CreateTableStore(
            face,
            cff,
            horizontal,
            vertical,
            vorg,
            metadata);
        return SfntFaceWriter.Write(
            0x4F54544FU,
            store,
            limits);
    }

    internal static byte[] CreateCffProgram(FontInstance instance)
    {
        ValidateInstance(instance);
        FontLimits limits = instance.GetFace()
            .GetTableCache().GetLimits();
        return CreateCff(
            instance,
            CalculateGlobalBounds(instance),
            limits);
    }

    static void ValidateInstance(FontInstance instance)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        FontFace face = instance.GetFace();

        if (!face.IsStructurallyValid())
        {
            throw new System.InvalidOperationException(
                "A structurally invalid face cannot be instantiated.");
        }

        if (face.GetOutlineKind() != FontOutlineKind.Cff2)
        {
            throw FontFormatException.Create(
                "Static CFF2 instancing requires CFF2 outlines.");
        }

        SfntCff2Table? cff2 = face.GetCff2Table();

        if (cff2 == null ||
            cff2.GetCharStringsIndex().GetCount() != instance.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "Static CFF2 instancing requires matching CFF2 and maxp glyph counts.");
        }
    }

    static byte[] CreateCff(
        FontInstance instance,
        int[] globalBounds,
        FontLimits limits)
    {
        int glyphCount = instance.GetGlyphCount();
        int customStringCount = System.Math.Min(
            glyphCount - 1,
            MaximumCustomStringCount);
        byte[][] charStrings = new byte[glyphCount][];
        byte[][] strings = new byte[customStringCount][];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            charStrings[glyphId] = CreateCharString(
                instance.GetGlyphOutline(glyphId),
                limits);

            if (glyphId > 0 && glyphId <= customStringCount)
            {
                string glyphName = "gid" + glyphId.ToString(
                    "D4",
                    System.Globalization.CultureInfo.InvariantCulture);
                strings[glyphId - 1] = System.Text.Encoding.ASCII.GetBytes(
                    glyphName);
            }

            glyphId += 1;
        }

        byte[] nameIndex = CreateIndex(
            new byte[][]
            {
                System.Text.Encoding.ASCII.GetBytes("StaticInstance")
            },
            limits);
        byte[] stringIndex = CreateIndex(strings, limits);
        byte[] globalSubroutines = CreateIndex(new byte[0][], limits);
        byte[] charset = CreateCharset(glyphCount, limits);
        byte[] charStringIndex = CreateIndex(charStrings, limits);
        SfntCff2Table? cff2 = instance.GetFace().GetCff2Table();
        System.ArgumentNullException.ThrowIfNull(cff2);
        double[] fontMatrix = cff2
            .GetTopDictionary().CopyFontMatrix();
        byte[] initialTop = CreateTopDictionary(
            0,
            0,
            globalBounds,
            fontMatrix,
            limits);
        byte[] initialTopIndex = CreateIndex(
            new byte[][] { initialTop },
            limits);
        int fixedLength = CheckedLength(
            4L + nameIndex.Length + initialTopIndex.Length +
            stringIndex.Length + globalSubroutines.Length,
            limits);
        int charsetOffset = fixedLength;
        int charStringsOffset = CheckedLength(
            (long)charsetOffset + charset.Length,
            limits);
        byte[] top = CreateTopDictionary(
            charsetOffset,
            charStringsOffset,
            globalBounds,
            fontMatrix,
            limits);
        byte[] topIndex = CreateIndex(new byte[][] { top }, limits);

        if (topIndex.Length != initialTopIndex.Length)
        {
            throw FontFormatException.Create(
                "The static CFF1 Top DICT layout did not stabilize.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt8(1);
        writer.WriteUInt8(0);
        writer.WriteUInt8(4);
        writer.WriteUInt8(4);
        writer.WriteBytes(nameIndex);
        writer.WriteBytes(topIndex);
        writer.WriteBytes(stringIndex);
        writer.WriteBytes(globalSubroutines);
        writer.WriteBytes(charset);
        writer.WriteBytes(charStringIndex);
        return writer.CopyBytes();
    }

    static byte[] CreateTopDictionary(
        int charsetOffset,
        int charStringsOffset,
        int[] globalBounds,
        double[] fontMatrix,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        int boundIndex = 0;
        while (boundIndex < globalBounds.Length)
        {
            WriteDictionaryInteger(writer, globalBounds[boundIndex]);
            boundIndex += 1;
        }
        writer.WriteUInt8(5);
        WriteDictionaryInteger(writer, charsetOffset);
        writer.WriteUInt8(15);
        WriteDictionaryInteger(writer, charStringsOffset);
        writer.WriteUInt8(17);
        WriteDictionaryInteger(writer, 0);
        WriteDictionaryInteger(writer, 0);
        writer.WriteUInt8(18);
        int matrixIndex = 0;

        while (matrixIndex < fontMatrix.Length)
        {
            WriteDictionaryReal(writer, fontMatrix[matrixIndex]);
            matrixIndex += 1;
        }

        writer.WriteUInt8(12);
        writer.WriteUInt8(7);
        return writer.CopyBytes();
    }

    static byte[] CreateCharset(int glyphCount, FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());

        if (glyphCount == 1)
        {
            writer.WriteUInt8(0);
            return writer.CopyBytes();
        }

        if (glyphCount - 1 <= MaximumCustomStringCount)
        {
            writer.WriteUInt8(2);
            writer.WriteUInt16(FirstCustomSid);
            writer.WriteUInt16((ushort)(glyphCount - 2));
            return writer.CopyBytes();
        }

        writer.WriteUInt8(0);
        int glyphId = 1;

        while (glyphId < glyphCount)
        {
            int sid = glyphId <= MaximumCustomStringCount ?
                FirstCustomSid + glyphId - 1 :
                glyphId - MaximumCustomStringCount;
            writer.WriteUInt16((ushort)sid);
            glyphId += 1;
        }

        return writer.CopyBytes();
    }

    static byte[] CreateIndex(byte[][] objects, FontLimits limits)
    {
        if (objects.Length > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A static CFF1 INDEX contains too many objects.");
        }

        long maximumOffset = 1;
        int index = 0;

        while (index < objects.Length)
        {
            if (objects[index] == null)
            {
                throw FontFormatException.Create(
                    "A static CFF1 INDEX object is unavailable.");
            }

            maximumOffset += objects[index].Length;
            index += 1;
        }

        int boundedMaximumOffset = CheckedLength(maximumOffset, limits);
        int offsetSize = GetOffsetSize(boundedMaximumOffset);
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16((ushort)objects.Length);

        if (objects.Length == 0)
        {
            return writer.CopyBytes();
        }

        writer.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteOffset(writer, offset, offsetSize);
        index = 0;

        while (index < objects.Length)
        {
            offset += objects[index].Length;
            WriteOffset(writer, offset, offsetSize);
            index += 1;
        }

        index = 0;

        while (index < objects.Length)
        {
            writer.WriteBytes(objects[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static int GetOffsetSize(int maximumOffset)
    {
        if (maximumOffset <= byte.MaxValue) { return 1; }
        if (maximumOffset <= ushort.MaxValue) { return 2; }
        if (maximumOffset <= 0x00FFFFFF) { return 3; }
        return 4;
    }

    static void WriteOffset(
        BigEndianWriter writer,
        int offset,
        int offsetSize)
    {
        if (offsetSize == 1) { writer.WriteUInt8((byte)offset); return; }
        if (offsetSize == 2) { writer.WriteUInt16((ushort)offset); return; }
        if (offsetSize == 3) { writer.WriteUInt24((uint)offset); return; }
        writer.WriteUInt32((uint)offset);
    }

    static byte[] CreateCharString(
        GlyphOutline outline,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        double currentX = 0;
        double currentY = 0;
        int commandIndex = 0;

        while (commandIndex < outline.GetCommandCount())
        {
            GlyphPathCommand command = outline.GetCommand(commandIndex);
            GlyphPathCommandKind kind = command.GetKind();

            if (kind == GlyphPathCommandKind.Move)
            {
                WriteType2Number(writer, command.GetEndX() - currentX);
                WriteType2Number(writer, command.GetEndY() - currentY);
                writer.WriteUInt8(21);
                currentX = command.GetEndX();
                currentY = command.GetEndY();
            }
            else if (kind == GlyphPathCommandKind.Line)
            {
                WriteType2Number(writer, command.GetEndX() - currentX);
                WriteType2Number(writer, command.GetEndY() - currentY);
                writer.WriteUInt8(5);
                currentX = command.GetEndX();
                currentY = command.GetEndY();
            }
            else if (kind == GlyphPathCommandKind.Cubic)
            {
                WriteType2Number(writer, command.GetControlX() - currentX);
                WriteType2Number(writer, command.GetControlY() - currentY);
                WriteType2Number(writer,
                    command.GetSecondControlX() - command.GetControlX());
                WriteType2Number(writer,
                    command.GetSecondControlY() - command.GetControlY());
                WriteType2Number(writer,
                    command.GetEndX() - command.GetSecondControlX());
                WriteType2Number(writer,
                    command.GetEndY() - command.GetSecondControlY());
                writer.WriteUInt8(8);
                currentX = command.GetEndX();
                currentY = command.GetEndY();
            }
            else if (kind != GlyphPathCommandKind.Close)
            {
                throw FontFormatException.Create(
                    "A static CFF1 charstring cannot encode a quadratic path.");
            }

            commandIndex += 1;
        }

        writer.WriteUInt8(14);
        return writer.CopyBytes();
    }

    static void WriteType2Number(BigEndianWriter writer, double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A static CFF1 coordinate is not finite.");
        }

        double integer = System.Math.Truncate(value);

        if (integer == value && value >= short.MinValue &&
            value <= short.MaxValue)
        {
            WriteType2Integer(writer, (int)value);
            return;
        }

        double scaled = System.Math.Round(
            value * 65536.0,
            System.MidpointRounding.AwayFromZero);

        if (scaled < int.MinValue || scaled > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A static CFF1 coordinate exceeds the 16.16 range.");
        }

        writer.WriteUInt8(255);
        writer.WriteInt32((int)scaled);
    }

    static void WriteType2Integer(BigEndianWriter writer, int value)
    {
        if (value >= -107 && value <= 107)
        {
            writer.WriteUInt8((byte)(value + 139));
        }
        else if (value >= 108 && value <= 1131)
        {
            int adjusted = value - 108;
            writer.WriteUInt8((byte)(247 + adjusted / 256));
            writer.WriteUInt8((byte)(adjusted % 256));
        }
        else if (value >= -1131 && value <= -108)
        {
            int adjusted = -value - 108;
            writer.WriteUInt8((byte)(251 + adjusted / 256));
            writer.WriteUInt8((byte)(adjusted % 256));
        }
        else
        {
            writer.WriteUInt8(28);
            writer.WriteInt16((short)value);
        }
    }

    static void WriteDictionaryInteger(BigEndianWriter writer, int value)
    {
        writer.WriteUInt8(29);
        writer.WriteInt32(value);
    }

    static void WriteDictionaryReal(BigEndianWriter writer, double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A static CFF1 DICT real operand is not finite.");
        }

        string text = value.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture);
        writer.WriteUInt8(30);
        int highNibble = -1;
        int index = 0;

        while (index < text.Length)
        {
            char character = text[index];
            int nibble;

            if ((character == 'E' || character == 'e') &&
                index + 1 < text.Length && text[index + 1] == '-')
            {
                nibble = 12;
                index += 1;
            }
            else if ((character == 'E' || character == 'e') &&
                index + 1 < text.Length && text[index + 1] == '+')
            {
                nibble = 11;
                index += 1;
            }
            else
            {
                nibble = GetRealNibble(character);
            }

            if (highNibble < 0)
            {
                highNibble = nibble;
            }
            else
            {
                writer.WriteUInt8((byte)(highNibble * 16 + nibble));
                highNibble = -1;
            }

            index += 1;
        }

        if (highNibble < 0)
        {
            highNibble = 15;
        }
        else
        {
            writer.WriteUInt8((byte)(highNibble * 16 + 15));
            return;
        }

        writer.WriteUInt8((byte)(highNibble * 16 + 15));
    }

    static int GetRealNibble(char value)
    {
        if (value >= '0' && value <= '9') { return value - '0'; }
        if (value == '.') { return 10; }
        if (value == 'E' || value == 'e') { return 11; }
        if (value == '-') { return 14; }
        throw FontFormatException.Create(
            "A static CFF1 DICT real operand has an unsupported character.");
    }

    static SfntStaticCffMetricData CreateMetricData(
        FontInstance instance,
        FontFace face,
        bool vertical,
        FontLimits limits)
    {
        int glyphCount = instance.GetGlyphCount();
        int[] advances = new int[glyphCount];
        int[] bearings = new int[glyphCount];
        int maximumAdvance = 0;
        int minimumLeadingBearing = short.MaxValue;
        int minimumTrailingBearing = short.MaxValue;
        int maximumExtent = short.MinValue;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            GlyphMetrics metrics = instance.GetGlyphMetrics(glyphId);
            advances[glyphId] = vertical ?
                metrics.GetAdvanceHeight() : metrics.GetAdvanceWidth();
            bearings[glyphId] = vertical ?
                metrics.GetTopSideBearing() : metrics.GetLeftSideBearing();
            ValidateMetric(advances[glyphId], bearings[glyphId]);
            maximumAdvance = System.Math.Max(
                maximumAdvance,
                advances[glyphId]);
            GlyphBounds? bounds = instance.GetGlyphOutline(glyphId).GetBounds();
            int extent = 0;

            System.ArgumentNullException.ThrowIfNull(bounds);
            if (!bounds.IsEmpty())
            {
                extent = vertical
                    ? RoundMaximum(bounds.GetYMaximum()) -
                        RoundMinimum(bounds.GetYMinimum())
                    : RoundMaximum(bounds.GetXMaximum()) -
                        RoundMinimum(bounds.GetXMinimum());
            }

            int trailingBearing = advances[glyphId] -
                bearings[glyphId] - extent;
            int metricExtent = bearings[glyphId] + extent;
            ValidateSignedMetricSummary(trailingBearing, metricExtent);
            minimumLeadingBearing = System.Math.Min(
                minimumLeadingBearing,
                bearings[glyphId]);
            minimumTrailingBearing = System.Math.Min(
                minimumTrailingBearing,
                trailingBearing);
            maximumExtent = System.Math.Max(maximumExtent, metricExtent);
            glyphId += 1;
        }

        int metricCount = glyphCount;

        while (metricCount > 1 &&
            advances[metricCount - 1] == advances[metricCount - 2])
        {
            metricCount -= 1;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        glyphId = 0;

        while (glyphId < metricCount)
        {
            writer.WriteUInt16((ushort)advances[glyphId]);
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId += 1;
        }

        while (glyphId < glyphCount)
        {
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId += 1;
        }

        int headerTag = vertical ? FontTags.Vhea : FontTags.Hhea;
        SfntTableData? headerTable = face.GetTableStore().FindTable(headerTag);

        if (headerTable == null && vertical)
        {
            headerTable = face.GetTableStore().FindTable(FontTags.Hhea);
        }

        if (headerTable == null || headerTable.GetLength() < 36)
        {
            throw FontFormatException.Create(
                "Static CFF2 instancing requires a complete metric header.");
        }

        byte[] header = headerTable.GetDataSlice().CopyBytes();
        WriteUInt16(header, 10, maximumAdvance);
        WriteInt16(header, 12, minimumLeadingBearing);
        WriteInt16(header, 14, minimumTrailingBearing);
        WriteInt16(header, 16, maximumExtent);
        WriteUInt16(header, 34, metricCount);
        return new SfntStaticCffMetricData(writer.CopyBytes(), header);
    }

    static byte[] CreateVorg(FontInstance instance, FontLimits limits)
    {
        int defaultOrigin = instance.GetGlyphMetrics(0).GetVerticalOriginY();
        ValidateInt16(defaultOrigin,
            "A static CFF1 default vertical origin exceeds the Int16 range.");
        int overrideCount = 0;
        int glyphId = 1;

        while (glyphId < instance.GetGlyphCount())
        {
            int origin = instance.GetGlyphMetrics(glyphId).GetVerticalOriginY();
            ValidateInt16(origin,
                "A static CFF1 vertical origin exceeds the Int16 range.");

            if (origin != defaultOrigin)
            {
                overrideCount += 1;
            }

            glyphId += 1;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteInt16((short)defaultOrigin);
        writer.WriteUInt16((ushort)overrideCount);
        glyphId = 1;

        while (glyphId < instance.GetGlyphCount())
        {
            int origin = instance.GetGlyphMetrics(glyphId).GetVerticalOriginY();

            if (origin != defaultOrigin)
            {
                writer.WriteUInt16((ushort)glyphId);
                writer.WriteInt16((short)origin);
            }

            glyphId += 1;
        }

        return writer.CopyBytes();
    }

    static SfntTableStore CreateTableStore(
        FontFace face,
        byte[] cff,
        SfntStaticCffMetricData horizontal,
        SfntStaticCffMetricData vertical,
        byte[]? vorg,
        SfntStaticInstanceMetadata metadata)
    {
        SfntTableStore source = face.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            SfntTableData sourceTable = source.GetTable(index);
            int tag = sourceTable.GetTag();

            if (!ShouldDrop(tag))
            {
                byte[] data = SelectTableData(
                    sourceTable,
                    horizontal,
                    vertical,
                    vorg);
                data = metadata.PatchTable(tag, data);
                AddTable(
                    tables,
                    tag,
                    data,
                    sourceTable.GetClassification());
            }

            index += 1;
        }

        AddTable(
            tables,
            FontTags.Cff,
            cff,
            SfntTableClassification.Known);

        if (!source.HasTable(FontTags.Vhea))
        {
            AddTable(
                tables,
                FontTags.Vhea,
                vertical.GetHeader(),
                SfntTableClassification.Known);
        }

        if (!source.HasTable(FontTags.Vmtx))
        {
            AddTable(
                tables,
                FontTags.Vmtx,
                vertical.GetMetrics(),
                SfntTableClassification.Known);
        }

        SfntTableData[] ordered = tables.ToArray();
        SortTables(ordered);
        return new SfntTableStore(
            ordered,
            Array.Empty<FontDiagnostic>());
    }

    static byte[] SelectTableData(
        SfntTableData source,
        SfntStaticCffMetricData horizontal,
        SfntStaticCffMetricData vertical,
        byte[]? vorg)
    {
        int tag = source.GetTag();

        if (tag == FontTags.Hhea) { return horizontal.GetHeader(); }
        if (tag == FontTags.Hmtx) { return horizontal.GetMetrics(); }
        if (tag == FontTags.Vhea) { return vertical.GetHeader(); }
        if (tag == FontTags.Vmtx) { return vertical.GetMetrics(); }
        if (tag == FontTags.Vorg && vorg != null) { return vorg; }
        return source.GetDataSlice().CopyBytes();
    }

    static bool ShouldDrop(int tag)
    {
        return tag == FontTags.Avar ||
            tag == FontTags.Cff2 ||
            tag == FontTags.Cvar ||
            tag == FontTags.Dsig ||
            tag == FontTags.Fvar ||
            tag == FontTags.Gvar ||
            tag == FontTags.Hvar ||
            tag == FontTags.Mvar ||
            tag == FontTags.Stat ||
            tag == FontTags.Vvar;
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] data,
        SfntTableClassification classification)
    {
        FontData owned = FontData.Create(data);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(data),
            owned.GetSlice(0, owned.GetLength()),
            classification,
            true));
    }

    static void SortTables(SfntTableData[] tables)
    {
        int index = 1;

        while (index < tables.Length)
        {
            SfntTableData current = tables[index];
            uint currentTag = CheckedFontMath.ReinterpretInt32AsUInt32(
                current.GetTag());
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    tables[insertionIndex - 1].GetTag()) > currentTag)
            {
                tables[insertionIndex] = tables[insertionIndex - 1];
                insertionIndex -= 1;
            }

            tables[insertionIndex] = current;
            index += 1;
        }
    }

    static int CheckedLength(long value, FontLimits limits)
    {
        if (value < 0 || value > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "The static CFF1 program exceeds the configured output limit.");
        }

        return (int)value;
    }

    static int[] CalculateGlobalBounds(FontInstance instance)
    {
        int[] result = new int[] { 0, 0, 0, 0 };
        bool hasOutline = false;
        int glyphId = 0;

        while (glyphId < instance.GetGlyphCount())
        {
            GlyphBounds? bounds = instance.GetGlyphOutline(glyphId).GetBounds();

            System.ArgumentNullException.ThrowIfNull(bounds);
            if (!bounds.IsEmpty())
            {
                int xMinimum = RoundMinimum(bounds.GetXMinimum());
                int yMinimum = RoundMinimum(bounds.GetYMinimum());
                int xMaximum = RoundMaximum(bounds.GetXMaximum());
                int yMaximum = RoundMaximum(bounds.GetYMaximum());

                if (!hasOutline)
                {
                    result = new int[]
                    {
                        xMinimum, yMinimum, xMaximum, yMaximum
                    };
                    hasOutline = true;
                }
                else
                {
                    result[0] = System.Math.Min(result[0], xMinimum);
                    result[1] = System.Math.Min(result[1], yMinimum);
                    result[2] = System.Math.Max(result[2], xMaximum);
                    result[3] = System.Math.Max(result[3], yMaximum);
                }
            }

            glyphId += 1;
        }

        return result;
    }

    static int RoundMinimum(double value)
    {
        return CheckedBound(System.Math.Floor(value));
    }

    static int RoundMaximum(double value)
    {
        return CheckedBound(System.Math.Ceiling(value));
    }

    static int CheckedBound(double value)
    {
        if (!double.IsFinite(value) || value < short.MinValue ||
            value > short.MaxValue)
        {
            throw FontFormatException.Create(
                "A static CFF1 glyph bound exceeds the FWORD range.");
        }
        return (int)value;
    }

    static void ValidateSignedMetricSummary(int bearing, int extent)
    {
        ValidateInt16(
            bearing,
            "A static CFF1 metric summary exceeds the Int16 range.");
        ValidateInt16(
            extent,
            "A static CFF1 metric summary exceeds the Int16 range.");
    }

    static void ValidateMetric(int advance, int bearing)
    {
        if (advance < 0 || advance > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A static CFF1 glyph advance exceeds the UInt16 range.");
        }

        ValidateInt16(
            bearing,
            "A static CFF1 glyph bearing exceeds the Int16 range.");
    }

    static void ValidateInt16(int value, string message)
    {
        if (value < short.MinValue || value > short.MaxValue)
        {
            throw FontFormatException.Create(message);
        }
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A static CFF1 UInt16 field exceeds its encoded range.");
        }

        data[offset] = (byte)((value >> 8) & 0xFF);
        data[offset + 1] = (byte)(value & 0xFF);
    }

    static void WriteInt16(byte[] data, int offset, int value)
    {
        ValidateInt16(
            value,
            "A static CFF1 Int16 field exceeds its encoded range.");
        int encoded = value < 0 ? value + 65536 : value;
        data[offset] = (byte)((encoded >> 8) & 0xFF);
        data[offset + 1] = (byte)(encoded & 0xFF);
    }
}
