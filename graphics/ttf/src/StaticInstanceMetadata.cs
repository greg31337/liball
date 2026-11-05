namespace ttf;

internal class SfntStaticInstanceMetadata
{
    const int WeightTag = 0x77676874;
    const int WidthTag = 0x77647468;
    const int ItalicTag = 0x6974616C;
    const int SlantTag = 0x736C6E74;
    FontFace face;
    NormalizedFontCoordinates coordinates;
    SfntMvarLocation? mvar;
    int[] bounds;

    internal SfntStaticInstanceMetadata(FontInstance instance, int[] bounds)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (bounds == null || bounds.Length != 4)
        {
            throw new System.ArgumentException(
                "Static instance bounds require four values.",
                "bounds");
        }

        face = instance.GetFace();
        coordinates = instance.GetCoordinates();
        this.bounds = bounds;

        if (!coordinates.IsDefaultUserLocation())
        {
            SfntMvarTable? table = face.GetMvarTable();

            if (table != null)
            {
                mvar = table.CreateLocation(coordinates.GetNormalizedValues());
            }
            else if (face.HasTable(FontTags.Mvar))
            {
                throw FontFormatException.Create(
                    "Static instancing requires a supported MVAR table.");
            }
        }
    }

    internal byte[] PatchTable(int tag, byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (tag == FontTags.Head) { PatchHead(data); }
        else if (tag == FontTags.Hhea) { PatchHorizontalHeader(data); }
        else if (tag == FontTags.Vhea) { PatchVerticalHeader(data); }
        else if (tag == FontTags.Os2) { PatchOs2(data); }
        else if (tag == FontTags.Post) { PatchPost(data); }
        else if (tag == FontTags.Gasp) { PatchGasp(data); }
        else if (tag == FontTags.Name) { return CreateNameTable(data); }
        else if (tag == FontTags.Gsub || tag == FontTags.Gpos)
        {
            return SfntFeatureVariationsResolver.Resolve(
                data,
                coordinates,
                face.GetTableCache().GetLimits());
        }
        return data;
    }

    internal byte[] CreateTrueTypeMaxp(byte[] source, int[] maxima)
    {
        if (source == null || source.Length < 32)
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires a complete maxp table.");
        }

        if (maxima == null || maxima.Length != 7)
        {
            throw new System.ArgumentException(
                "TrueType maxima require seven values.",
                "maxima");
        }

        byte[] result = source.AsSpan().ToArray();
        WriteUInt16(result, 6, maxima[0]);
        WriteUInt16(result, 8, maxima[1]);
        WriteUInt16(result, 10, maxima[2]);
        WriteUInt16(result, 12, maxima[3]);
        WriteUInt16(result, 26, maxima[4]);
        WriteUInt16(result, 28, maxima[5]);
        WriteUInt16(result, 30, maxima[6]);
        return result;
    }

    void PatchHead(byte[] data)
    {
        if (data.Length < 54) { return; }
        WriteInt16(data, 36, bounds[0]);
        WriteInt16(data, 38, bounds[1]);
        WriteInt16(data, 40, bounds[2]);
        WriteInt16(data, 42, bounds[3]);
        int style = ReadUInt16(data, 44);
        bool bold = GetAxisValue(WeightTag, 0) >= 700;
        bool italic = GetAxisValue(ItalicTag, 0) != 0 ||
            GetAxisValue(SlantTag, 0) != 0;
        style = SetBit(style, 0, bold);
        style = SetBit(style, 1, italic);
        WriteUInt16(data, 44, style);
    }

    void PatchHorizontalHeader(byte[] data)
    {
        if (data.Length < 36 || mvar == null) { return; }
        AddInt16Delta(data, 18, SfntMvarValueTags.HorizontalCaretRise);
        AddInt16Delta(data, 20, SfntMvarValueTags.HorizontalCaretRun);
        AddInt16Delta(data, 22, SfntMvarValueTags.HorizontalCaretOffset);
    }

    void PatchVerticalHeader(byte[] data)
    {
        if (data.Length < 36 || mvar == null) { return; }
        AddInt16Delta(data, 4, SfntMvarValueTags.VerticalAscender);
        AddInt16Delta(data, 6, SfntMvarValueTags.VerticalDescender);
        AddInt16Delta(data, 8, SfntMvarValueTags.VerticalLineGap);
        AddInt16Delta(data, 18, SfntMvarValueTags.VerticalCaretRise);
        AddInt16Delta(data, 20, SfntMvarValueTags.VerticalCaretRun);
        AddInt16Delta(data, 22, SfntMvarValueTags.VerticalCaretOffset);
    }

    void PatchOs2(byte[] data)
    {
        if (data.Length < 78) { return; }
        double weight = GetAxisValue(WeightTag, ReadUInt16(data, 4));
        double width = GetAxisValue(WidthTag, 100);
        WriteUInt16(data, 4, Clamp((int)System.Math.Round(weight), 1, 1000));
        WriteUInt16(data, 6, WidthClass(width));
        int selection = ReadUInt16(data, 62);
        bool bold = weight >= 700;
        bool italic = GetAxisValue(ItalicTag, 0) != 0;
        bool oblique = !italic && GetAxisValue(SlantTag, 0) != 0;
        selection = SetBit(selection, 0, italic);
        selection = SetBit(selection, 5, bold);
        selection = SetBit(selection, 6, !bold && !italic && !oblique);
        selection = SetBit(selection, 9, oblique);
        WriteUInt16(data, 62, selection);

        if (mvar == null) { return; }
        AddInt16Delta(data, 10, SfntMvarValueTags.SubscriptXSize);
        AddInt16Delta(data, 12, SfntMvarValueTags.SubscriptYSize);
        AddInt16Delta(data, 14, SfntMvarValueTags.SubscriptXOffset);
        AddInt16Delta(data, 16, SfntMvarValueTags.SubscriptYOffset);
        AddInt16Delta(data, 18, SfntMvarValueTags.SuperscriptXSize);
        AddInt16Delta(data, 20, SfntMvarValueTags.SuperscriptYSize);
        AddInt16Delta(data, 22, SfntMvarValueTags.SuperscriptXOffset);
        AddInt16Delta(data, 24, SfntMvarValueTags.SuperscriptYOffset);
        AddInt16Delta(data, 26, SfntMvarValueTags.StrikeoutSize);
        AddInt16Delta(data, 28, SfntMvarValueTags.StrikeoutPosition);
        AddInt16Delta(data, 68, SfntMvarValueTags.HorizontalAscender);
        AddInt16Delta(data, 70, SfntMvarValueTags.HorizontalDescender);
        AddInt16Delta(data, 72, SfntMvarValueTags.HorizontalLineGap);
        AddUInt16Delta(data, 74, SfntMvarValueTags.HorizontalClippingAscent);
        AddUInt16Delta(data, 76, SfntMvarValueTags.HorizontalClippingDescent);

        if (data.Length >= 90)
        {
            AddInt16Delta(data, 86, SfntMvarValueTags.XHeight);
            AddInt16Delta(data, 88, SfntMvarValueTags.CapHeight);
        }
    }

    void PatchPost(byte[] data)
    {
        if (data.Length < 12 || mvar == null) { return; }
        AddInt16Delta(data, 8, SfntMvarValueTags.UnderlinePosition);
        AddInt16Delta(data, 10, SfntMvarValueTags.UnderlineSize);
    }

    void PatchGasp(byte[] data)
    {
        if (data.Length < 4 || mvar == null) { return; }
        int count = ReadUInt16(data, 2);
        int index = 0;

        while (index + 1 < count && 4 + index * 4 + 2 <= data.Length)
        {
            AddUInt16Delta(
                data,
                4 + index * 4,
                SfntMvarValueTags.GaspRange0 + index);
            index += 1;
        }
    }

    byte[] CreateNameTable(byte[] source)
    {
        SfntNameTable? table = face.GetNameTable();
        string? style = GetStyleName();

        if (table == null || style == null)
        {
            return source;
        }

        string? family = SelectName(table, 16);

        if (family == null) { family = SelectName(table, 1); }
        if (family == null) { family = "Unnamed"; }
        string postScript = GetPostScriptName(table, family, style);
        int sourceRecordCount = table.GetRecordCount();
        int[] standardIds = new int[] { 2, 4, 6, 17 };
        int addedRecordCount = 0;
        int standardIndex = 0;

        while (standardIndex < standardIds.Length)
        {
            if (!HasUnicodeName(table, standardIds[standardIndex]))
            {
                addedRecordCount += 1;
            }
            standardIndex += 1;
        }

        int recordCount = sourceRecordCount + addedRecordCount;
        byte[][] strings = new byte[recordCount][];
        int storageLength = 0;
        int index = 0;

        while (index < sourceRecordCount)
        {
            SfntNameRecord record = table.GetRecord(index);
            string? replacement = ReplacementName(
                record.GetNameId(),
                family,
                style,
                postScript);
            strings[index] = replacement != null && record.IsUnicodeEncoding()
                ? System.Text.Encoding.BigEndianUnicode.GetBytes(replacement)
                : record.CopyStringBytes();
            storageLength += strings[index].Length;
            index += 1;
        }

        standardIndex = 0;
        while (standardIndex < standardIds.Length)
        {
            int nameId = standardIds[standardIndex];

            if (!HasUnicodeName(table, nameId))
            {
                string? replacement = ReplacementName(
                    nameId,
                    family,
                    style,
                    postScript);
                System.ArgumentNullException.ThrowIfNull(replacement);
                strings[index] = System.Text.Encoding.BigEndianUnicode
                    .GetBytes(replacement);
                storageLength += strings[index].Length;
                index += 1;
            }
            standardIndex += 1;
        }

        int languageTagCount = table.GetLanguageTagCount();
        int[] recordOrder = CreateNameRecordOrder(
            table,
            sourceRecordCount,
            standardIds,
            recordCount);
        int headerLength = 6 + recordCount * 12 +
            (table.GetVersion() == 1 ? 2 + languageTagCount * 4 : 0);
        BigEndianWriter writer = BigEndianWriter.Create(
            headerLength + storageLength + table.GetStorageLength());
        writer.WriteUInt16((ushort)table.GetVersion());
        writer.WriteUInt16((ushort)recordCount);
        writer.WriteUInt16((ushort)headerLength);
        int offset = 0;
        index = 0;

        while (index < recordOrder.Length)
        {
            int recordIndex = recordOrder[index];
            writer.WriteUInt16((ushort)GetNamePlatformId(
                table,
                sourceRecordCount,
                recordIndex));
            writer.WriteUInt16((ushort)GetNameEncodingId(
                table,
                sourceRecordCount,
                recordIndex));
            writer.WriteUInt16((ushort)GetNameLanguageId(
                table,
                sourceRecordCount,
                recordIndex));
            writer.WriteUInt16((ushort)GetNameId(
                table,
                sourceRecordCount,
                standardIds,
                recordIndex));
            writer.WriteUInt16((ushort)strings[recordIndex].Length);
            writer.WriteUInt16((ushort)offset);
            offset += strings[recordIndex].Length;
            index += 1;
        }

        if (table.GetVersion() == 1)
        {
            writer.WriteUInt16((ushort)languageTagCount);
            index = 0;

            while (index < languageTagCount)
            {
                byte[] value = table.GetLanguageTagRecord(index)
                    .CopyStringBytes();
                writer.WriteUInt16((ushort)value.Length);
                writer.WriteUInt16((ushort)offset);
                offset += value.Length;
                index += 1;
            }
        }

        index = 0;
        while (index < recordOrder.Length)
        {
            writer.WriteBytes(strings[recordOrder[index]]);
            index += 1;
        }
        index = 0;
        while (index < languageTagCount)
        {
            writer.WriteBytes(table.GetLanguageTagRecord(index).CopyStringBytes());
            index += 1;
        }
        return writer.CopyBytes();
    }

    string? GetStyleName()
    {
        if (coordinates.HasNamedInstance())
        {
            SfntFvarTable? fvar = face.GetFvarTable();
            System.ArgumentNullException.ThrowIfNull(fvar);
            SfntFvarInstance named = fvar.GetInstance(
                coordinates.GetNamedInstanceIndex());
            return named.GetSubfamilyName().GetValue();
        }

        if (coordinates.IsDefaultUserLocation()) { return null; }
        string value = "";
        int index = 0;

        while (index < coordinates.GetAxisCount())
        {
            if (index != 0) { value += " "; }
            value += TagText(coordinates.GetAxisTag(index)) + " " +
                coordinates.GetUserValue(index).ToString(
                    "0.###",
                    System.Globalization.CultureInfo.InvariantCulture);
            index += 1;
        }
        return value;
    }

    string GetPostScriptName(SfntNameTable table, string family, string style)
    {
        if (coordinates.HasNamedInstance())
        {
            SfntFvarTable? fvar = face.GetFvarTable();
            System.ArgumentNullException.ThrowIfNull(fvar);
            SfntFvarInstance named = fvar.GetInstance(
                coordinates.GetNamedInstanceIndex());

            if (named.HasPostScriptName())
            {
                SfntLocalizedName? postScriptName = named.GetPostScriptName();
                System.ArgumentNullException.ThrowIfNull(postScriptName);
                return postScriptName.GetValue();
            }
        }

        string? prefix = SelectName(table, 25);
        if (prefix == null) { prefix = family; }
        return SfntPostScriptNameNormalizer.Normalize(prefix + "-" + style)
            .GetNormalizedName();
    }

    static string? ReplacementName(
        int nameId,
        string family,
        string style,
        string postScript)
    {
        if (nameId == 2 || nameId == 17) { return style; }
        if (nameId == 4) { return family + " " + style; }
        if (nameId == 6) { return postScript; }
        return null;
    }

    static string? SelectName(SfntNameTable table, int nameId)
    {
        SfntLocalizedName? name = table.SelectDefaultName(nameId);
        return name == null ? null : name.GetValue();
    }

    static bool HasUnicodeName(SfntNameTable table, int nameId)
    {
        int index = 0;
        while (index < table.GetRecordCount())
        {
            SfntNameRecord record = table.GetRecord(index);
            if (record.GetNameId() == nameId && record.IsUnicodeEncoding())
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static int[] CreateNameRecordOrder(
        SfntNameTable table,
        int sourceRecordCount,
        int[] standardIds,
        int recordCount)
    {
        int[] order = new int[recordCount];
        int index = 0;
        while (index < order.Length)
        {
            order[index] = index;
            int insertionIndex = index;

            while (insertionIndex > 0 && CompareNameRecords(
                table,
                sourceRecordCount,
                standardIds,
                order[insertionIndex - 1],
                order[insertionIndex]) > 0)
            {
                int previous = order[insertionIndex - 1];
                order[insertionIndex - 1] = order[insertionIndex];
                order[insertionIndex] = previous;
                insertionIndex -= 1;
            }
            index += 1;
        }
        return order;
    }

    static int CompareNameRecords(
        SfntNameTable table,
        int sourceRecordCount,
        int[] standardIds,
        int first,
        int second)
    {
        int difference = GetNamePlatformId(
            table, sourceRecordCount, first) - GetNamePlatformId(
                table, sourceRecordCount, second);
        if (difference != 0) { return difference; }
        difference = GetNameEncodingId(
            table, sourceRecordCount, first) - GetNameEncodingId(
                table, sourceRecordCount, second);
        if (difference != 0) { return difference; }
        difference = GetNameLanguageId(
            table, sourceRecordCount, first) - GetNameLanguageId(
                table, sourceRecordCount, second);
        if (difference != 0) { return difference; }
        return GetNameId(table, sourceRecordCount, standardIds, first) -
            GetNameId(table, sourceRecordCount, standardIds, second);
    }

    static int GetNamePlatformId(
        SfntNameTable table,
        int sourceRecordCount,
        int recordIndex)
    {
        return recordIndex < sourceRecordCount
            ? table.GetRecord(recordIndex).GetPlatformId()
            : 3;
    }

    static int GetNameEncodingId(
        SfntNameTable table,
        int sourceRecordCount,
        int recordIndex)
    {
        return recordIndex < sourceRecordCount
            ? table.GetRecord(recordIndex).GetEncodingId()
            : 10;
    }

    static int GetNameLanguageId(
        SfntNameTable table,
        int sourceRecordCount,
        int recordIndex)
    {
        return recordIndex < sourceRecordCount
            ? table.GetRecord(recordIndex).GetLanguageId()
            : 0x0409;
    }

    static int GetNameId(
        SfntNameTable table,
        int sourceRecordCount,
        int[] standardIds,
        int recordIndex)
    {
        if (recordIndex < sourceRecordCount)
        {
            return table.GetRecord(recordIndex).GetNameId();
        }

        int addedIndex = recordIndex - sourceRecordCount;
        int observed = 0;
        int index = 0;
        while (index < standardIds.Length)
        {
            if (!HasUnicodeName(table, standardIds[index]))
            {
                if (observed == addedIndex) { return standardIds[index]; }
                observed += 1;
            }
            index += 1;
        }
        throw new System.InvalidOperationException(
            "A synthesized static name record is unavailable.");
    }

    double GetAxisValue(int tag, double fallback)
    {
        int index = 0;
        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == tag)
            {
                return coordinates.GetUserValue(index);
            }
            index += 1;
        }
        return fallback;
    }

    void AddInt16Delta(byte[] data, int offset, int tag)
    {
        System.ArgumentNullException.ThrowIfNull(mvar);
        WriteInt16(
            data,
            offset,
            ReadInt16(data, offset) + RoundDelta(mvar.CalculateDelta(tag)));
    }

    void AddUInt16Delta(byte[] data, int offset, int tag)
    {
        System.ArgumentNullException.ThrowIfNull(mvar);
        WriteUInt16(
            data,
            offset,
            ReadUInt16(data, offset) + RoundDelta(mvar.CalculateDelta(tag)));
    }

    static int WidthClass(double width)
    {
        if (width <= 50) { return 1; }
        if (width <= 62.5) { return 2; }
        if (width <= 75) { return 3; }
        if (width <= 87.5) { return 4; }
        if (width < 112.5) { return 5; }
        if (width <= 125) { return 6; }
        if (width <= 150) { return 7; }
        if (width <= 200) { return 8; }
        return 9;
    }

    static int RoundDelta(double value)
    {
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced MVAR delta is outside the supported range.");
        }
        return (int)System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);
    }

    static int SetBit(int value, int bit, bool enabled)
    {
        return enabled ? value | 1 << bit : value & ~(1 << bit);
    }

    static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum) { return minimum; }
        if (value > maximum) { return maximum; }
        return value;
    }

    static string TagText(int tag)
    {
        char[] characters = new char[4];
        characters[0] = (char)(tag >> 24 & 0xFF);
        characters[1] = (char)(tag >> 16 & 0xFF);
        characters[2] = (char)(tag >> 8 & 0xFF);
        characters[3] = (char)(tag & 0xFF);
        return new string(characters);
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] << 8 | data[offset + 1];
    }

    static int ReadInt16(byte[] data, int offset)
    {
        int value = ReadUInt16(data, offset);
        return value >= 0x8000 ? value - 0x10000 : value;
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced metadata value exceeds the UInt16 range.");
        }
        data[offset] = (byte)((value >> 8) & 0xFF);
        data[offset + 1] = (byte)(value & 0xFF);
    }

    static void WriteInt16(byte[] data, int offset, int value)
    {
        if (value < short.MinValue || value > short.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced metadata value exceeds the Int16 range.");
        }
        int encoded = value < 0 ? value + 65536 : value;
        data[offset] = (byte)((encoded >> 8) & 0xFF);
        data[offset + 1] = (byte)(encoded & 0xFF);
    }
}
