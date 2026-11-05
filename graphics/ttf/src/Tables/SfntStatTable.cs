namespace ttf;

internal class SfntStatAxis
{
    int tag;
    int nameId;
    int ordering;
    SfntLocalizedName name;

    internal SfntStatAxis(
        int tag,
        int nameId,
        int ordering,
        SfntLocalizedName name)
    {
        this.tag = tag;
        this.nameId = nameId;
        this.ordering = ordering;
        this.name = name;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal int GetNameId()
    {
        return nameId;
    }

    internal int GetOrdering()
    {
        return ordering;
    }

    internal SfntLocalizedName GetName()
    {
        return name;
    }
}

internal class SfntStatAxisValueRecord
{
    int axisIndex;
    double value;

    internal SfntStatAxisValueRecord(int axisIndex, double value)
    {
        this.axisIndex = axisIndex;
        this.value = value;
    }

    internal int GetAxisIndex()
    {
        return axisIndex;
    }

    internal double GetValue()
    {
        return value;
    }
}

internal class SfntStatAxisValue
{
    int offset;
    int format;
    bool supported;
    int axisIndex;
    int flags;
    int nameId;
    double value;
    double nominalValue;
    double rangeMinimumValue;
    double rangeMaximumValue;
    double linkedValue;
    SfntLocalizedName? name;
    SfntStatAxisValueRecord[] records;

    internal SfntStatAxisValue(
        int offset,
        int format,
        bool supported,
        int axisIndex,
        int flags,
        int nameId,
        double value,
        double nominalValue,
        double rangeMinimumValue,
        double rangeMaximumValue,
        double linkedValue,
        SfntLocalizedName? name,
        SfntStatAxisValueRecord[] records)
    {
        this.offset = offset;
        this.format = format;
        this.supported = supported;
        this.axisIndex = axisIndex;
        this.flags = flags;
        this.nameId = nameId;
        this.value = value;
        this.nominalValue = nominalValue;
        this.rangeMinimumValue = rangeMinimumValue;
        this.rangeMaximumValue = rangeMaximumValue;
        this.linkedValue = linkedValue;
        this.name = name;
        this.records = records;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal bool IsSupported()
    {
        return supported;
    }

    internal int GetAxisIndex()
    {
        return axisIndex;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool IsOlderSiblingFontAttribute()
    {
        return (flags & 1) != 0;
    }

    internal bool IsElidable()
    {
        return (flags & 2) != 0;
    }

    internal int GetNameId()
    {
        return nameId;
    }

    internal SfntLocalizedName? GetName()
    {
        return name;
    }

    internal double GetValue()
    {
        return value;
    }

    internal double GetNominalValue()
    {
        return nominalValue;
    }

    internal double GetRangeMinimumValue()
    {
        return rangeMinimumValue;
    }

    internal double GetRangeMaximumValue()
    {
        return rangeMaximumValue;
    }

    internal double GetLinkedValue()
    {
        return linkedValue;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntStatAxisValueRecord GetRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }
}

internal class SfntStatTable
{
    int majorVersion;
    int minorVersion;
    int designAxisSize;
    int designAxesOffset;
    int axisValueOffsetsOffset;
    SfntStatAxis[] axes;
    SfntStatAxisValue[] axisValues;
    bool hasElidedFallbackName;
    int elidedFallbackNameId;
    SfntLocalizedName? elidedFallbackName;

    internal SfntStatTable(
        int majorVersion,
        int minorVersion,
        int designAxisSize,
        int designAxesOffset,
        int axisValueOffsetsOffset,
        SfntStatAxis[] axes,
        SfntStatAxisValue[] axisValues,
        bool hasElidedFallbackName,
        int elidedFallbackNameId,
        SfntLocalizedName? elidedFallbackName)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.designAxisSize = designAxisSize;
        this.designAxesOffset = designAxesOffset;
        this.axisValueOffsetsOffset = axisValueOffsetsOffset;
        this.axes = axes;
        this.axisValues = axisValues;
        this.hasElidedFallbackName = hasElidedFallbackName;
        this.elidedFallbackNameId = elidedFallbackNameId;
        this.elidedFallbackName = elidedFallbackName;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetDesignAxisSize()
    {
        return designAxisSize;
    }

    internal int GetDesignAxesOffset()
    {
        return designAxesOffset;
    }

    internal int GetAxisValueOffsetsOffset()
    {
        return axisValueOffsetsOffset;
    }

    internal int GetAxisCount()
    {
        return axes.Length;
    }

    internal SfntStatAxis GetAxis(int index)
    {
        if (index < 0 || index >= axes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return axes[index];
    }

    internal SfntStatAxis? FindAxis(int tag)
    {
        int index = 0;

        while (index < axes.Length)
        {
            if (axes[index].GetTag() == tag)
            {
                return axes[index];
            }

            index += 1;
        }

        return null;
    }

    internal int GetAxisValueCount()
    {
        return axisValues.Length;
    }

    internal SfntStatAxisValue GetAxisValue(int index)
    {
        if (index < 0 || index >= axisValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return axisValues[index];
    }

    internal bool HasElidedFallbackName()
    {
        return hasElidedFallbackName;
    }

    internal int GetElidedFallbackNameId()
    {
        return elidedFallbackNameId;
    }

    internal SfntLocalizedName? GetElidedFallbackName()
    {
        return elidedFallbackName;
    }
}

internal static class SfntStatTableParser
{
    const int VersionOneHeaderLength = 18;
    const int VersionElevenHeaderLength = 20;
    const int AxisRecordLength = 8;
    const int AxisValueOffsetLength = 2;
    const int KnownFlags = 3;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Stat,
        1,
        1,
        2,
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

    internal static SfntStatTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntStatTable)view.GetParsedData();
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
        int headerLength = version.GetMinorVersion() == 0 ?
            VersionOneHeaderLength : VersionElevenHeaderLength;

        if (tableData.GetLength() < headerLength)
        {
            throw FontFormatException.Create("The STAT table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int designAxisSize = reader.ReadUInt16();
        int designAxisCount = reader.ReadUInt16();
        int designAxesOffset = ReadOffset(reader, "The STAT design-axes offset is too large.");
        int axisValueCount = reader.ReadUInt16();
        int axisValueOffsetsOffset = ReadOffset(
            reader,
            "The STAT axis-value-offsets offset is too large.");
        bool hasElidedFallbackName = minorVersion != 0;
        int elidedFallbackNameId = hasElidedFallbackName ?
            reader.ReadUInt16() : 0;

        if (majorVersion != version.GetMajorVersion() ||
            minorVersion != version.GetMinorVersion())
        {
            throw FontFormatException.Create("The STAT table version is inconsistent.");
        }

        if (majorVersion != 1)
        {
            throw FontFormatException.Create("The STAT table version is not supported.");
        }

        ValidateAxisSize(designAxisSize, support);

        if (designAxisCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "The STAT design-axis count exceeds the configured variation-region limit.");
        }

        if (axisValueCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The STAT axis-value count exceeds the configured coverage-entry limit.");
        }

        if (axisValueCount != 0 && designAxisCount == 0)
        {
            throw FontFormatException.Create(
                "The STAT table defines axis values without design axes.");
        }

        ValidateArrayOffset(
            tableData,
            designAxesOffset,
            designAxisCount,
            designAxisSize,
            headerLength,
            "The STAT design-axes array offset is invalid.");
        ValidateArrayOffset(
            tableData,
            axisValueOffsetsOffset,
            axisValueCount,
            AxisValueOffsetLength,
            headerLength,
            "The STAT axis-value-offsets array offset is invalid.");

        SfntNameTable? nameTable = SfntNameTableParser.FindTable(cache);

        if ((designAxisCount != 0 || axisValueCount != 0 ||
            hasElidedFallbackName) && nameTable == null)
        {
            throw FontFormatException.Create(
                "The STAT table requires a supported name table.");
        }

        SfntStatAxis[] axes = ParseAxes(
            tableData,
            designAxesOffset,
            designAxisCount,
            designAxisSize,
            nameTable);
        SfntStatAxisValue[] axisValues = ParseAxisValues(
            tableData,
            axisValueOffsetsOffset,
            axisValueCount,
            minorVersion,
            support,
            axes,
            nameTable,
            limits);
        SfntLocalizedName? elidedFallbackName = null;

        if (hasElidedFallbackName)
        {
            ValidateNameId(elidedFallbackNameId);
            elidedFallbackName = ResolveName(
                nameTable,
                elidedFallbackNameId,
                "The STAT elided fallback name cannot be resolved.");
        }

        CrossCheckFvar(cache, axes);
        return new SfntStatTable(
            majorVersion,
            minorVersion,
            designAxisSize,
            designAxesOffset,
            axisValueOffsetsOffset,
            axes,
            axisValues,
            hasElidedFallbackName,
            elidedFallbackNameId,
            elidedFallbackName);
    }

    static SfntStatAxis[] ParseAxes(
        FontDataSlice tableData,
        int offset,
        int count,
        int recordSize,
        SfntNameTable? nameTable)
    {
        SfntStatAxis[] axes = new SfntStatAxis[count];
        int index = 0;

        while (index < axes.Length)
        {
            int recordOffset = CheckedFontMath.AddOffset(
                offset,
                CheckedFontMath.MultiplyCount(index, recordSize));
            BigEndianReader reader = new BigEndianReader(
                tableData.GetSlice(recordOffset, recordSize));
            int tag = reader.ReadTag();
            int nameId = reader.ReadUInt16();
            int ordering = reader.ReadUInt16();
            ValidateAxisTag(tag, axes, index);
            ValidateNameId(nameId);
            SfntLocalizedName name = ResolveName(
                nameTable,
                nameId,
                "A STAT axis name cannot be resolved.");
            axes[index] = new SfntStatAxis(tag, nameId, ordering, name);
            index += 1;
        }

        return axes;
    }

    static SfntStatAxisValue[] ParseAxisValues(
        FontDataSlice tableData,
        int offsetsOffset,
        int count,
        int minorVersion,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable,
        FontLimits limits)
    {
        SfntStatAxisValue[] values = new SfntStatAxisValue[count];

        if (count == 0)
        {
            return values;
        }

        int offsetsLength = CheckedFontMath.MultiplyCount(
            count,
            AxisValueOffsetLength);
        BigEndianReader offsetsReader = new BigEndianReader(
            tableData.GetSlice(offsetsOffset, offsetsLength));
        int inspectedRecords = 0;
        int index = 0;

        while (index < values.Length)
        {
            int relativeOffset = offsetsReader.ReadOffset16();

            if (relativeOffset < offsetsLength)
            {
                throw FontFormatException.Create(
                    "A STAT axis-value table offset points into its offset array.");
            }

            int valueOffset = CheckedFontMath.AddOffset(
                offsetsOffset,
                relativeOffset);

            if (valueOffset > tableData.GetLength() - 2)
            {
                throw FontFormatException.Create(
                    "A STAT axis-value table offset is invalid.");
            }

            values[index] = ParseAxisValue(
                tableData,
                valueOffset,
                minorVersion,
                support,
                axes,
                nameTable,
                limits,
                inspectedRecords);
            inspectedRecords = CheckedFontMath.AddCounts(
                inspectedRecords,
                values[index].GetRecordCount());

            if (inspectedRecords > limits.GetMaxCoverageEntries())
            {
                throw FontLimitException.Create(
                    "The STAT format 4 record count exceeds the configured coverage-entry limit.");
            }

            index += 1;
        }

        return values;
    }

    static SfntStatAxisValue ParseAxisValue(
        FontDataSlice tableData,
        int offset,
        int minorVersion,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable,
        FontLimits limits,
        int inspectedRecords)
    {
        BigEndianReader reader = CreateReader(tableData, offset);
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParseFormatOne(
                reader,
                offset,
                support,
                axes,
                nameTable);
        }

        if (format == 2)
        {
            return ParseFormatTwo(
                reader,
                offset,
                support,
                axes,
                nameTable);
        }

        if (format == 3)
        {
            return ParseFormatThree(
                reader,
                offset,
                support,
                axes,
                nameTable);
        }

        if (format == 4 && minorVersion >= 2)
        {
            return ParseFormatFour(
                reader,
                offset,
                support,
                axes,
                nameTable,
                limits,
                inspectedRecords);
        }

        return CreateUnsupportedAxisValue(offset, format);
    }

    static SfntStatAxisValue ParseFormatOne(
        BigEndianReader reader,
        int offset,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable)
    {
        RequireBytes(reader, 10, "A STAT format 1 axis-value table is truncated.");
        int axisIndex = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        int nameId = reader.ReadUInt16();
        double value = reader.ReadFixed16Dot16();
        ValidateAxisIndex(axisIndex, axes.Length);
        ValidateFlags(flags, support);
        ValidateNameId(nameId);
        SfntLocalizedName name = ResolveName(
            nameTable,
            nameId,
            "A STAT axis-value name cannot be resolved.");
        return new SfntStatAxisValue(
            offset,
            1,
            true,
            axisIndex,
            flags,
            nameId,
            value,
            0,
            0,
            0,
            0,
            name,
            new SfntStatAxisValueRecord[0]);
    }

    static SfntStatAxisValue ParseFormatTwo(
        BigEndianReader reader,
        int offset,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable)
    {
        RequireBytes(reader, 18, "A STAT format 2 axis-value table is truncated.");
        int axisIndex = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        int nameId = reader.ReadUInt16();
        double nominalValue = reader.ReadFixed16Dot16();
        double minimumValue = reader.ReadFixed16Dot16();
        double maximumValue = reader.ReadFixed16Dot16();
        ValidateAxisIndex(axisIndex, axes.Length);
        ValidateFlags(flags, support);
        ValidateNameId(nameId);

        if (minimumValue > nominalValue || nominalValue > maximumValue)
        {
            throw FontFormatException.Create(
                "A STAT format 2 axis-value range is invalid.");
        }

        SfntLocalizedName name = ResolveName(
            nameTable,
            nameId,
            "A STAT axis-value name cannot be resolved.");
        return new SfntStatAxisValue(
            offset,
            2,
            true,
            axisIndex,
            flags,
            nameId,
            0,
            nominalValue,
            minimumValue,
            maximumValue,
            0,
            name,
            new SfntStatAxisValueRecord[0]);
    }

    static SfntStatAxisValue ParseFormatThree(
        BigEndianReader reader,
        int offset,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable)
    {
        RequireBytes(reader, 14, "A STAT format 3 axis-value table is truncated.");
        int axisIndex = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        int nameId = reader.ReadUInt16();
        double value = reader.ReadFixed16Dot16();
        double linkedValue = reader.ReadFixed16Dot16();
        ValidateAxisIndex(axisIndex, axes.Length);
        ValidateFlags(flags, support);
        ValidateNameId(nameId);
        SfntLocalizedName name = ResolveName(
            nameTable,
            nameId,
            "A STAT axis-value name cannot be resolved.");
        return new SfntStatAxisValue(
            offset,
            3,
            true,
            axisIndex,
            flags,
            nameId,
            value,
            0,
            0,
            0,
            linkedValue,
            name,
            new SfntStatAxisValueRecord[0]);
    }

    static SfntStatAxisValue ParseFormatFour(
        BigEndianReader reader,
        int offset,
        SfntTableVersionSupport support,
        SfntStatAxis[] axes,
        SfntNameTable? nameTable,
        FontLimits limits,
        int inspectedRecords)
    {
        RequireBytes(reader, 6, "A STAT format 4 axis-value table is truncated.");
        int axisCount = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        int nameId = reader.ReadUInt16();

        if (axisCount == 0 || axisCount > axes.Length)
        {
            throw FontFormatException.Create(
                "A STAT format 4 axis count is invalid.");
        }

        if (axisCount > limits.GetMaxCoverageEntries() - inspectedRecords)
        {
            throw FontLimitException.Create(
                "The STAT format 4 record count exceeds the configured coverage-entry limit.");
        }

        ValidateFlags(flags, support);
        ValidateNameId(nameId);
        int recordsLength = CheckedFontMath.MultiplyCount(axisCount, 6);
        RequireBytes(
            reader,
            recordsLength,
            "A STAT format 4 axis-value record array is truncated.");
        SfntStatAxisValueRecord[] records =
            new SfntStatAxisValueRecord[axisCount];
        int index = 0;

        while (index < records.Length)
        {
            int axisIndex = reader.ReadUInt16();
            double value = reader.ReadFixed16Dot16();
            ValidateAxisIndex(axisIndex, axes.Length);
            int previous = 0;

            while (previous < index)
            {
                if (records[previous].GetAxisIndex() == axisIndex)
                {
                    throw FontFormatException.Create(
                        "A STAT format 4 table repeats an axis index.");
                }

                previous += 1;
            }

            records[index] = new SfntStatAxisValueRecord(axisIndex, value);
            index += 1;
        }

        SfntLocalizedName name = ResolveName(
            nameTable,
            nameId,
            "A STAT axis-value name cannot be resolved.");
        return new SfntStatAxisValue(
            offset,
            4,
            true,
            -1,
            flags,
            nameId,
            0,
            0,
            0,
            0,
            0,
            name,
            records);
    }

    static SfntStatAxisValue CreateUnsupportedAxisValue(
        int offset,
        int format)
    {
        return new SfntStatAxisValue(
            offset,
            format,
            false,
            -1,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            new SfntStatAxisValueRecord[0]);
    }

    static void CrossCheckFvar(
        SfntTableCache cache,
        SfntStatAxis[] axes)
    {
        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            if (cache.FindRawTable(FontTags.Fvar) != null)
            {
                throw FontFormatException.Create(
                    "The STAT table requires a supported fvar table.");
            }

            return;
        }

        if (axes.Length < fvar.GetAxisCount())
        {
            throw FontFormatException.Create(
                "The STAT table omits an fvar axis.");
        }

        int fvarIndex = 0;

        while (fvarIndex < fvar.GetAxisCount())
        {
            SfntFvarAxis fvarAxis = fvar.GetAxis(fvarIndex);
            SfntStatAxis? statAxis = FindAxis(axes, fvarAxis.GetTag());

            if (statAxis == null ||
                statAxis.GetNameId() != fvarAxis.GetNameId())
            {
                throw FontFormatException.Create(
                    "A STAT axis does not match its fvar axis.");
            }

            fvarIndex += 1;
        }
    }

    static SfntStatAxis? FindAxis(SfntStatAxis[] axes, int tag)
    {
        int index = 0;

        while (index < axes.Length)
        {
            if (axes[index].GetTag() == tag)
            {
                return axes[index];
            }

            index += 1;
        }

        return null;
    }

    static int ReadOffset(BigEndianReader reader, string message)
    {
        long encoded = reader.ReadOffset32();

        if (encoded > int.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return (int)encoded;
    }

    static void ValidateAxisSize(
        int size,
        SfntTableVersionSupport support)
    {
        if (support == SfntTableVersionSupport.Supported &&
            size != AxisRecordLength ||
            support == SfntTableVersionSupport.CompatibleExtension &&
            size < AxisRecordLength)
        {
            throw FontFormatException.Create("The STAT design-axis size is invalid.");
        }
    }

    static void ValidateArrayOffset(
        FontDataSlice tableData,
        int offset,
        int count,
        int itemSize,
        int minimumOffset,
        string message)
    {
        if (count == 0)
        {
            if (offset != 0)
            {
                throw FontFormatException.Create(message);
            }

            return;
        }

        int length = CheckedFontMath.MultiplyCount(count, itemSize);

        if (offset < minimumOffset ||
            offset > tableData.GetLength() - length)
        {
            throw FontFormatException.Create(message);
        }
    }

    static void ValidateAxisTag(
        int tag,
        SfntStatAxis[] axes,
        int populatedCount)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int first = (int)((value >> 24) & 0xFFU);
        bool firstUpper = first >= 0x41 && first <= 0x5A;
        bool firstLower = first >= 0x61 && first <= 0x7A;

        if (!firstUpper && !firstLower)
        {
            throw FontFormatException.Create("A STAT axis tag is invalid.");
        }

        int characterIndex = 0;
        bool foundSpace = false;

        while (characterIndex < 4)
        {
            int character = (int)((value >> (24 - characterIndex * 8)) & 0xFFU);
            bool upper = character >= 0x41 && character <= 0x5A;
            bool lower = character >= 0x61 && character <= 0x7A;
            bool digit = character >= 0x30 && character <= 0x39;

            if (character == 0x20)
            {
                foundSpace = true;
            }
            else if (foundSpace || !upper && !lower && !digit)
            {
                throw FontFormatException.Create("A STAT axis tag is invalid.");
            }

            if (firstUpper && !upper && !digit)
            {
                throw FontFormatException.Create("A STAT private axis tag is invalid.");
            }

            characterIndex += 1;
        }

        int index = 0;

        while (index < populatedCount)
        {
            if (axes[index].GetTag() == tag)
            {
                throw FontFormatException.Create(
                    "The STAT table contains duplicate axis tags.");
            }

            index += 1;
        }
    }

    static void ValidateNameId(int nameId)
    {
        bool definedNameId = nameId >= 0 && nameId <= 25;
        bool customNameId = nameId > 255 && nameId < 32768;

        if (!definedNameId && !customNameId)
        {
            throw FontFormatException.Create("A STAT name ID is invalid.");
        }
    }

    static void ValidateAxisIndex(int axisIndex, int axisCount)
    {
        if (axisIndex < 0 || axisIndex >= axisCount)
        {
            throw FontFormatException.Create(
                "A STAT axis-value table references a missing axis.");
        }
    }

    static void ValidateFlags(
        int flags,
        SfntTableVersionSupport support)
    {
        if (support == SfntTableVersionSupport.Supported &&
            (flags & ~KnownFlags) != 0)
        {
            throw FontFormatException.Create(
                "A STAT axis-value reserved flag is nonzero.");
        }
    }

    static SfntLocalizedName ResolveName(
        SfntNameTable? nameTable,
        int nameId,
        string message)
    {
        System.ArgumentNullException.ThrowIfNull(nameTable);
        SfntLocalizedName? name = nameTable.SelectDefaultName(nameId);

        if (name == null)
        {
            throw FontFormatException.Create(message);
        }

        return name;
    }

    static void RequireBytes(
        BigEndianReader reader,
        int count,
        string message)
    {
        if (reader.GetRemainingLength() < count)
        {
            throw FontFormatException.Create(message);
        }
    }

    static BigEndianReader CreateReader(FontDataSlice tableData, int offset)
    {
        return new BigEndianReader(
            tableData.GetSlice(offset, tableData.GetLength() - offset));
    }
}
