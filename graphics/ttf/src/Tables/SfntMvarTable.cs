namespace ttf;

internal static class SfntMvarValueTags
{
    internal const int CapHeight = 0x63706874;
    internal const int GaspRange0 = 0x67737030;
    internal const int GaspRange1 = 0x67737031;
    internal const int GaspRange2 = 0x67737032;
    internal const int GaspRange3 = 0x67737033;
    internal const int GaspRange4 = 0x67737034;
    internal const int GaspRange5 = 0x67737035;
    internal const int GaspRange6 = 0x67737036;
    internal const int GaspRange7 = 0x67737037;
    internal const int GaspRange8 = 0x67737038;
    internal const int GaspRange9 = 0x67737039;
    internal const int HorizontalAscender = 0x68617363;
    internal const int HorizontalClippingAscent = 0x68636C61;
    internal const int HorizontalClippingDescent = 0x68636C64;
    internal const int HorizontalCaretOffset = 0x68636F66;
    internal const int HorizontalCaretRun = 0x6863726E;
    internal const int HorizontalCaretRise = 0x68637273;
    internal const int HorizontalDescender = 0x68647363;
    internal const int HorizontalLineGap = 0x686C6770;
    internal const int SubscriptXOffset = 0x7362786F;
    internal const int SubscriptXSize = 0x73627873;
    internal const int SubscriptYOffset = 0x7362796F;
    internal const int SubscriptYSize = 0x73627973;
    internal const int SuperscriptXOffset = 0x7370786F;
    internal const int SuperscriptXSize = 0x73707873;
    internal const int SuperscriptYOffset = 0x7370796F;
    internal const int SuperscriptYSize = 0x73707973;
    internal const int StrikeoutPosition = 0x7374726F;
    internal const int StrikeoutSize = 0x73747273;
    internal const int UnderlinePosition = 0x756E646F;
    internal const int UnderlineSize = 0x756E6473;
    internal const int VerticalAscender = 0x76617363;
    internal const int VerticalCaretOffset = 0x76636F66;
    internal const int VerticalCaretRun = 0x7663726E;
    internal const int VerticalCaretRise = 0x76637273;
    internal const int VerticalDescender = 0x76647363;
    internal const int VerticalLineGap = 0x766C6770;
    internal const int XHeight = 0x78686774;

    internal static bool IsRecognized(int tag)
    {
        return tag == CapHeight ||
            GetGaspRangeIndex(tag) >= 0 ||
            tag == HorizontalAscender ||
            tag == HorizontalClippingAscent ||
            tag == HorizontalClippingDescent ||
            tag == HorizontalCaretOffset ||
            tag == HorizontalCaretRun ||
            tag == HorizontalCaretRise ||
            tag == HorizontalDescender ||
            tag == HorizontalLineGap ||
            tag == SubscriptXOffset ||
            tag == SubscriptXSize ||
            tag == SubscriptYOffset ||
            tag == SubscriptYSize ||
            tag == SuperscriptXOffset ||
            tag == SuperscriptXSize ||
            tag == SuperscriptYOffset ||
            tag == SuperscriptYSize ||
            tag == StrikeoutPosition ||
            tag == StrikeoutSize ||
            tag == UnderlinePosition ||
            tag == UnderlineSize ||
            tag == VerticalAscender ||
            tag == VerticalCaretOffset ||
            tag == VerticalCaretRun ||
            tag == VerticalCaretRise ||
            tag == VerticalDescender ||
            tag == VerticalLineGap ||
            tag == XHeight;
    }

    internal static int GetGaspRangeIndex(int tag)
    {
        if (tag < GaspRange0 || tag > GaspRange9)
        {
            return -1;
        }

        return tag - GaspRange0;
    }
}

internal class SfntMvarValueRecord
{
    int valueTag;
    SfntDeltaSetIndex deltaSetIndex;

    internal SfntMvarValueRecord(
        int valueTag,
        SfntDeltaSetIndex deltaSetIndex)
    {
        this.valueTag = valueTag;
        this.deltaSetIndex = deltaSetIndex;
    }

    internal int GetValueTag()
    {
        return valueTag;
    }

    internal SfntDeltaSetIndex GetDeltaSetIndex()
    {
        return deltaSetIndex;
    }

    internal bool IsRecognized()
    {
        return SfntMvarValueTags.IsRecognized(valueTag);
    }
}

internal class SfntMvarTable
{
    int majorVersion;
    int minorVersion;
    int valueRecordSize;
    int itemVariationStoreOffset;
    int axisCount;
    SfntMvarValueRecord[] records;
    SfntItemVariationStore? variationStore;

    internal SfntMvarTable(
        int majorVersion,
        int minorVersion,
        int valueRecordSize,
        int itemVariationStoreOffset,
        int axisCount,
        SfntMvarValueRecord[] records,
        SfntItemVariationStore? variationStore)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.valueRecordSize = valueRecordSize;
        this.itemVariationStoreOffset = itemVariationStoreOffset;
        this.axisCount = axisCount;
        this.records = records;
        this.variationStore = variationStore;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetValueRecordSize()
    {
        return valueRecordSize;
    }

    internal int GetValueRecordCount()
    {
        return records.Length;
    }

    internal int GetItemVariationStoreOffset()
    {
        return itemVariationStoreOffset;
    }

    internal SfntMvarValueRecord GetValueRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }

    internal SfntItemVariationStore? GetVariationStore()
    {
        return variationStore;
    }

    internal SfntMvarValueRecord? FindValueRecord(int valueTag)
    {
        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int middleTag = records[middle].GetValueTag();

            if (middleTag == valueTag)
            {
                return records[middle];
            }

            if (CheckedFontMath.ReinterpretInt32AsUInt32(middleTag) <
                CheckedFontMath.ReinterpretInt32AsUInt32(valueTag))
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    internal SfntMvarLocation CreateLocation(double[] normalizedCoordinates)
    {
        ValidateCoordinates(normalizedCoordinates);
        SfntVariationLocation? location = variationStore == null ? null :
            variationStore.CreateLocation(normalizedCoordinates);
        return new SfntMvarLocation(this, location);
    }

    void ValidateCoordinates(double[] normalizedCoordinates)
    {
        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        if (normalizedCoordinates.Length != axisCount)
        {
            throw new System.ArgumentException(
                "MVAR coordinates do not match the variation axes.",
                "normalizedCoordinates");
        }

        int index = 0;

        while (index < normalizedCoordinates.Length)
        {
            double value = normalizedCoordinates[index];

            if (!double.IsFinite(value) || value < -1 || value > 1)
            {
                throw new System.ArgumentOutOfRangeException(
                    "normalizedCoordinates");
            }

            index += 1;
        }
    }
}

internal class SfntMvarLocation
{
    SfntMvarTable table;
    SfntVariationLocation? location;

    internal SfntMvarLocation(
        SfntMvarTable table,
        SfntVariationLocation? location)
    {
        this.table = table;
        this.location = location;
    }

    internal bool HasValue(int valueTag)
    {
        return table.FindValueRecord(valueTag) != null;
    }

    internal double CalculateDelta(int valueTag)
    {
        SfntMvarValueRecord? record = table.FindValueRecord(valueTag);

        if (record == null || location == null)
        {
            return 0;
        }

        return location.CalculateDelta(record.GetDeltaSetIndex());
    }
}

internal static class SfntMvarTableParser
{
    const int HeaderLength = 12;
    const int BaseRecordLength = 8;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Mvar,
        1,
        1,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntMvarTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntMvarTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The MVAR table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int reserved = reader.ReadUInt16();
        int recordSize = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();
        int storeOffset = reader.ReadOffset16();

        if (majorVersion != version.GetMajorVersion() ||
            minorVersion != version.GetMinorVersion())
        {
            throw FontFormatException.Create("The MVAR table version is inconsistent.");
        }

        if (reserved != 0)
        {
            throw FontFormatException.Create("The MVAR reserved field is not zero.");
        }

        if (recordSize < BaseRecordLength ||
            (support == SfntTableVersionSupport.Supported &&
            recordSize != BaseRecordLength))
        {
            throw FontFormatException.Create("The MVAR value-record size is invalid.");
        }

        if (recordCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The MVAR value-record count exceeds the configured coverage limit.");
        }

        int recordsEnd = CheckedFontMath.GetRangeEnd(
            HeaderLength,
            recordCount,
            recordSize);

        if (recordsEnd > tableData.GetLength())
        {
            throw FontFormatException.Create("The MVAR value records are truncated.");
        }

        if ((recordCount == 0 && storeOffset != 0) ||
            (recordCount > 0 &&
            (storeOffset < recordsEnd || storeOffset > tableData.GetLength() - 4)))
        {
            throw FontFormatException.Create(
                "The MVAR variation-store offset is invalid.");
        }

        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create(
                "The MVAR table requires a supported fvar table.");
        }

        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        budget.AddEntries(recordCount);
        SfntMvarValueRecord[] records = new SfntMvarValueRecord[recordCount];
        int previousTag = 0;
        int index = 0;

        while (index < recordCount)
        {
            int offset = CheckedFontMath.GetRangeEnd(
                HeaderLength,
                index,
                recordSize);
            BigEndianReader recordReader = new BigEndianReader(
                tableData.GetSlice(offset, BaseRecordLength));
            int valueTag = recordReader.ReadTag();
            SfntDeltaSetIndex deltaSetIndex = new SfntDeltaSetIndex(
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16());

            if (index > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(valueTag) <=
                CheckedFontMath.ReinterpretInt32AsUInt32(previousTag))
            {
                throw FontFormatException.Create(
                    "The MVAR value records are not strictly ordered.");
            }

            records[index] = new SfntMvarValueRecord(valueTag, deltaSetIndex);
            previousTag = valueTag;
            index += 1;
        }

        SfntItemVariationStore? store = null;

        if (recordCount > 0)
        {
            store = SfntItemVariationStoreParser.Parse(
                tableData,
                storeOffset,
                recordsEnd,
                fvar.GetAxisCount(),
                limits,
                budget);
            index = 0;

            while (index < records.Length)
            {
                store.ValidateDeltaSetIndex(
                    records[index].GetDeltaSetIndex());
                index += 1;
            }
        }

        ValidateRecognizedTargets(records, cache);
        return new SfntMvarTable(
            majorVersion,
            minorVersion,
            recordSize,
            storeOffset,
            fvar.GetAxisCount(),
            records,
            store);
    }

    static void ValidateRecognizedTargets(
        SfntMvarValueRecord[] records,
        SfntTableCache cache)
    {
        SfntOs2Table? os2 = null;
        SfntHheaTable? hhea = null;
        SfntVheaTable? vhea = null;
        SfntPostTable? post = null;
        SfntGaspTable? gasp = null;
        int index = 0;

        while (index < records.Length)
        {
            int tag = records[index].GetValueTag();
            int gaspIndex = SfntMvarValueTags.GetGaspRangeIndex(tag);

            if (IsOs2Tag(tag))
            {
                if (os2 == null)
                {
                    os2 = SfntOs2TableParser.FindTable(cache);
                }

                if (os2 == null ||
                    ((tag == SfntMvarValueTags.CapHeight ||
                    tag == SfntMvarValueTags.XHeight) &&
                    !os2.HasVersion2Metrics()))
                {
                    throw FontFormatException.Create(
                        "An MVAR value record references a missing OS/2 field.");
                }
            }
            else if (IsHorizontalHeaderTag(tag))
            {
                if (hhea == null)
                {
                    hhea = SfntHheaTableParser.FindTable(cache);
                }

                if (hhea == null)
                {
                    throw FontFormatException.Create(
                        "An MVAR value record requires a supported hhea table.");
                }
            }
            else if (IsVerticalHeaderTag(tag))
            {
                if (vhea == null)
                {
                    vhea = SfntVheaTableParser.FindTable(cache);
                }

                if (vhea == null)
                {
                    throw FontFormatException.Create(
                        "An MVAR value record requires a supported vhea table.");
                }
            }
            else if (tag == SfntMvarValueTags.UnderlinePosition ||
                tag == SfntMvarValueTags.UnderlineSize)
            {
                if (post == null)
                {
                    post = SfntPostTableParser.FindTable(cache);
                }

                if (post == null)
                {
                    throw FontFormatException.Create(
                        "An MVAR value record requires a supported post table.");
                }
            }
            else if (gaspIndex >= 0)
            {
                if (gasp == null)
                {
                    gasp = SfntGaspTableParser.FindTable(cache);
                }

                if (gasp == null || gaspIndex >= gasp.GetRangeCount() - 1)
                {
                    throw FontFormatException.Create(
                        "An MVAR gasp value record references a missing range.");
                }
            }

            index += 1;
        }
    }

    static bool IsOs2Tag(int tag)
    {
        return tag == SfntMvarValueTags.CapHeight ||
            tag == SfntMvarValueTags.HorizontalAscender ||
            tag == SfntMvarValueTags.HorizontalClippingAscent ||
            tag == SfntMvarValueTags.HorizontalClippingDescent ||
            tag == SfntMvarValueTags.HorizontalDescender ||
            tag == SfntMvarValueTags.HorizontalLineGap ||
            tag == SfntMvarValueTags.SubscriptXOffset ||
            tag == SfntMvarValueTags.SubscriptXSize ||
            tag == SfntMvarValueTags.SubscriptYOffset ||
            tag == SfntMvarValueTags.SubscriptYSize ||
            tag == SfntMvarValueTags.SuperscriptXOffset ||
            tag == SfntMvarValueTags.SuperscriptXSize ||
            tag == SfntMvarValueTags.SuperscriptYOffset ||
            tag == SfntMvarValueTags.SuperscriptYSize ||
            tag == SfntMvarValueTags.StrikeoutPosition ||
            tag == SfntMvarValueTags.StrikeoutSize ||
            tag == SfntMvarValueTags.XHeight;
    }

    static bool IsHorizontalHeaderTag(int tag)
    {
        return tag == SfntMvarValueTags.HorizontalCaretOffset ||
            tag == SfntMvarValueTags.HorizontalCaretRun ||
            tag == SfntMvarValueTags.HorizontalCaretRise;
    }

    static bool IsVerticalHeaderTag(int tag)
    {
        return tag == SfntMvarValueTags.VerticalAscender ||
            tag == SfntMvarValueTags.VerticalCaretOffset ||
            tag == SfntMvarValueTags.VerticalCaretRun ||
            tag == SfntMvarValueTags.VerticalCaretRise ||
            tag == SfntMvarValueTags.VerticalDescender ||
            tag == SfntMvarValueTags.VerticalLineGap;
    }
}
