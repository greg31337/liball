namespace ttf;

internal class SfntFvarAxis
{
    int tag;
    double minimumValue;
    double defaultValue;
    double maximumValue;
    int flags;
    int nameId;
    SfntLocalizedName name;

    internal SfntFvarAxis(
        int tag,
        double minimumValue,
        double defaultValue,
        double maximumValue,
        int flags,
        int nameId,
        SfntLocalizedName name)
    {
        this.tag = tag;
        this.minimumValue = minimumValue;
        this.defaultValue = defaultValue;
        this.maximumValue = maximumValue;
        this.flags = flags;
        this.nameId = nameId;
        this.name = name;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal double GetMinimumValue()
    {
        return minimumValue;
    }

    internal double GetDefaultValue()
    {
        return defaultValue;
    }

    internal double GetMaximumValue()
    {
        return maximumValue;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool IsHidden()
    {
        return (flags & 1) != 0;
    }

    internal int GetNameId()
    {
        return nameId;
    }

    internal SfntLocalizedName GetName()
    {
        return name;
    }
}

internal class SfntFvarInstance
{
    int subfamilyNameId;
    int flags;
    double[] coordinates;
    bool hasPostScriptName;
    int postScriptNameId;
    SfntLocalizedName subfamilyName;
    SfntLocalizedName? postScriptName;

    internal SfntFvarInstance(
        int subfamilyNameId,
        int flags,
        double[] coordinates,
        bool hasPostScriptName,
        int postScriptNameId,
        SfntLocalizedName subfamilyName,
        SfntLocalizedName? postScriptName)
    {
        this.subfamilyNameId = subfamilyNameId;
        this.flags = flags;
        this.coordinates = coordinates;
        this.hasPostScriptName = hasPostScriptName;
        this.postScriptNameId = postScriptNameId;
        this.subfamilyName = subfamilyName;
        this.postScriptName = postScriptName;
    }

    internal int GetSubfamilyNameId()
    {
        return subfamilyNameId;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal int GetCoordinateCount()
    {
        return coordinates.Length;
    }

    internal double GetCoordinate(int index)
    {
        if (index < 0 || index >= coordinates.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return coordinates[index];
    }

    internal bool HasPostScriptName()
    {
        return hasPostScriptName;
    }

    internal int GetPostScriptNameId()
    {
        return postScriptNameId;
    }

    internal SfntLocalizedName GetSubfamilyName()
    {
        return subfamilyName;
    }

    internal SfntLocalizedName? GetPostScriptName()
    {
        return postScriptName;
    }
}

internal class SfntFvarTable
{
    int majorVersion;
    int minorVersion;
    int axesArrayOffset;
    int axisSize;
    int instanceSize;
    SfntFvarAxis[] axes;
    SfntFvarInstance[] instances;

    internal SfntFvarTable(
        int majorVersion,
        int minorVersion,
        int axesArrayOffset,
        int axisSize,
        int instanceSize,
        SfntFvarAxis[] axes,
        SfntFvarInstance[] instances)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.axesArrayOffset = axesArrayOffset;
        this.axisSize = axisSize;
        this.instanceSize = instanceSize;
        this.axes = axes;
        this.instances = instances;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetAxesArrayOffset()
    {
        return axesArrayOffset;
    }

    internal int GetAxisSize()
    {
        return axisSize;
    }

    internal int GetInstanceSize()
    {
        return instanceSize;
    }

    internal int GetAxisCount()
    {
        return axes.Length;
    }

    internal SfntFvarAxis GetAxis(int index)
    {
        if (index < 0 || index >= axes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return axes[index];
    }

    internal SfntFvarAxis? FindAxis(int tag)
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

    internal int GetInstanceCount()
    {
        return instances.Length;
    }

    internal SfntFvarInstance GetInstance(int index)
    {
        if (index < 0 || index >= instances.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return instances[index];
    }
}

internal static class SfntFvarTableParser
{
    const int HeaderLength = 16;
    const int AxisRecordLength = 20;
    const int CountSizePairCount = 2;
    const int HiddenAxisFlag = 1;
    const int NoPostScriptNameId = 0xFFFF;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Fvar,
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

    internal static SfntFvarTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntFvarTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion tableVersion,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The fvar table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int axesArrayOffset = reader.ReadOffset16();
        int countSizePairs = reader.ReadUInt16();
        int axisCount = reader.ReadUInt16();
        int axisSize = reader.ReadUInt16();
        int instanceCount = reader.ReadUInt16();
        int instanceSize = reader.ReadUInt16();

        if (majorVersion != tableVersion.GetMajorVersion() ||
            minorVersion != tableVersion.GetMinorVersion())
        {
            throw FontFormatException.Create("The fvar table version is inconsistent.");
        }

        if (countSizePairs != CountSizePairCount)
        {
            throw FontFormatException.Create("The fvar count-size pair field is invalid.");
        }

        if (axisCount == 0)
        {
            throw FontFormatException.Create("The fvar table does not define an axis.");
        }

        if (axisCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "The fvar axis count exceeds the configured variation-region limit.");
        }

        if (instanceCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The fvar instance count exceeds the configured coverage-entry limit.");
        }

        int coordinateLength = CheckedFontMath.MultiplyCount(axisCount, 4);
        int minimumInstanceSize = CheckedFontMath.AddOffset(4, coordinateLength);
        ValidateRecordSizes(
            axisSize,
            instanceSize,
            minimumInstanceSize,
            support);

        if (axesArrayOffset < HeaderLength)
        {
            throw FontFormatException.Create("The fvar axes array offset is invalid.");
        }

        int axesLength = CheckedFontMath.MultiplyCount(axisCount, axisSize);
        int instancesOffset = CheckedFontMath.AddOffset(
            axesArrayOffset,
            axesLength);
        int instancesLength = CheckedFontMath.MultiplyCount(
            instanceCount,
            instanceSize);
        int dataEnd = CheckedFontMath.AddOffset(instancesOffset, instancesLength);

        if (dataEnd > tableData.GetLength())
        {
            throw FontFormatException.Create("The fvar axis or instance array is truncated.");
        }

        SfntNameTable? nameTable = SfntNameTableParser.FindTable(cache);

        if (nameTable == null)
        {
            throw FontFormatException.Create(
                "The fvar table requires a supported name table.");
        }

        SfntFvarAxis[] axes = ParseAxes(
            tableData,
            axesArrayOffset,
            axisCount,
            axisSize,
            support,
            nameTable);
        SfntFvarInstance[] instances = ParseInstances(
            tableData,
            instancesOffset,
            instanceCount,
            instanceSize,
            minimumInstanceSize,
            support,
            axes,
            nameTable);

        return new SfntFvarTable(
            majorVersion,
            minorVersion,
            axesArrayOffset,
            axisSize,
            instanceSize,
            axes,
            instances);
    }

    static void ValidateRecordSizes(
        int axisSize,
        int instanceSize,
        int minimumInstanceSize,
        SfntTableVersionSupport support)
    {
        int postScriptInstanceSize = CheckedFontMath.AddOffset(
            minimumInstanceSize,
            2);

        if (support == SfntTableVersionSupport.Supported)
        {
            if (axisSize != AxisRecordLength)
            {
                throw FontFormatException.Create("The fvar axis record size is invalid.");
            }

            if (instanceSize != minimumInstanceSize &&
                instanceSize != postScriptInstanceSize)
            {
                throw FontFormatException.Create("The fvar instance record size is invalid.");
            }

            return;
        }

        if (axisSize < AxisRecordLength)
        {
            throw FontFormatException.Create("The fvar axis record size is invalid.");
        }

        if (instanceSize < minimumInstanceSize)
        {
            throw FontFormatException.Create("The fvar instance record size is invalid.");
        }
    }

    static SfntFvarAxis[] ParseAxes(
        FontDataSlice tableData,
        int axesArrayOffset,
        int axisCount,
        int axisSize,
        SfntTableVersionSupport support,
        SfntNameTable nameTable)
    {
        SfntFvarAxis[] axes = new SfntFvarAxis[axisCount];
        int index = 0;

        while (index < axes.Length)
        {
            int recordOffset = CheckedFontMath.AddOffset(
                axesArrayOffset,
                CheckedFontMath.MultiplyCount(index, axisSize));
            BigEndianReader reader = new BigEndianReader(
                tableData.GetSlice(recordOffset, axisSize));
            int tag = reader.ReadTag();
            double minimumValue = reader.ReadFixed16Dot16();
            double defaultValue = reader.ReadFixed16Dot16();
            double maximumValue = reader.ReadFixed16Dot16();
            int flags = reader.ReadUInt16();
            int nameId = reader.ReadUInt16();

            ValidateAxisTag(tag, axes, index);

            if (minimumValue > defaultValue || defaultValue > maximumValue)
            {
                throw FontFormatException.Create("An fvar axis value range is invalid.");
            }

            if (support == SfntTableVersionSupport.Supported &&
                (flags & ~HiddenAxisFlag) != 0)
            {
                throw FontFormatException.Create("An fvar axis reserved flag is nonzero.");
            }

            ValidateAxisNameId(nameId);
            SfntLocalizedName name = ResolveName(
                nameTable,
                nameId,
                "An fvar axis name cannot be resolved.");
            axes[index] = new SfntFvarAxis(
                tag,
                minimumValue,
                defaultValue,
                maximumValue,
                flags,
                nameId,
                name);
            index += 1;
        }

        return axes;
    }

    static SfntFvarInstance[] ParseInstances(
        FontDataSlice tableData,
        int instancesOffset,
        int instanceCount,
        int instanceSize,
        int minimumInstanceSize,
        SfntTableVersionSupport support,
        SfntFvarAxis[] axes,
        SfntNameTable nameTable)
    {
        SfntFvarInstance[] instances = new SfntFvarInstance[instanceCount];
        bool hasPostScriptNameField = instanceSize >=
            CheckedFontMath.AddOffset(minimumInstanceSize, 2);
        int instanceIndex = 0;

        while (instanceIndex < instances.Length)
        {
            int recordOffset = CheckedFontMath.AddOffset(
                instancesOffset,
                CheckedFontMath.MultiplyCount(instanceIndex, instanceSize));
            BigEndianReader reader = new BigEndianReader(
                tableData.GetSlice(recordOffset, instanceSize));
            int subfamilyNameId = reader.ReadUInt16();
            int flags = reader.ReadUInt16();

            if (support == SfntTableVersionSupport.Supported && flags != 0)
            {
                throw FontFormatException.Create(
                    "An fvar instance reserved flag is nonzero.");
            }

            ValidateSubfamilyNameId(subfamilyNameId);
            double[] coordinates = ParseCoordinates(reader, axes);
            int postScriptNameId = NoPostScriptNameId;
            bool hasPostScriptName = false;
            SfntLocalizedName? postScriptName = null;

            if (hasPostScriptNameField)
            {
                postScriptNameId = reader.ReadUInt16();
                ValidatePostScriptNameId(postScriptNameId);

                if (postScriptNameId != NoPostScriptNameId)
                {
                    hasPostScriptName = true;
                    postScriptName = ResolveName(
                        nameTable,
                        postScriptNameId,
                        "An fvar instance PostScript name cannot be resolved.");
                }
            }

            SfntLocalizedName subfamilyName = ResolveName(
                nameTable,
                subfamilyNameId,
                "An fvar instance subfamily name cannot be resolved.");
            instances[instanceIndex] = new SfntFvarInstance(
                subfamilyNameId,
                flags,
                coordinates,
                hasPostScriptName,
                postScriptNameId,
                subfamilyName,
                postScriptName);
            instanceIndex += 1;
        }

        return instances;
    }

    static double[] ParseCoordinates(
        BigEndianReader reader,
        SfntFvarAxis[] axes)
    {
        double[] coordinates = new double[axes.Length];
        int index = 0;

        while (index < coordinates.Length)
        {
            double coordinate = reader.ReadFixed16Dot16();

            if (coordinate < axes[index].GetMinimumValue() ||
                coordinate > axes[index].GetMaximumValue())
            {
                throw FontFormatException.Create(
                    "An fvar instance coordinate is outside its axis range.");
            }

            coordinates[index] = coordinate;
            index += 1;
        }

        return coordinates;
    }

    static void ValidateAxisTag(
        int tag,
        SfntFvarAxis[] axes,
        int populatedCount)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int first = (int)((value >> 24) & 0xFFU);
        bool firstUpper = first >= 0x41 && first <= 0x5A;
        bool firstLower = first >= 0x61 && first <= 0x7A;

        if (!firstUpper && !firstLower)
        {
            throw FontFormatException.Create("An fvar axis tag is invalid.");
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
                throw FontFormatException.Create("An fvar axis tag is invalid.");
            }

            if (firstUpper && !upper && !digit)
            {
                throw FontFormatException.Create("An fvar private axis tag is invalid.");
            }

            characterIndex += 1;
        }

        int axisIndex = 0;

        while (axisIndex < populatedCount)
        {
            if (axes[axisIndex].GetTag() == tag)
            {
                throw FontFormatException.Create("The fvar table contains duplicate axis tags.");
            }

            axisIndex += 1;
        }
    }

    static void ValidateAxisNameId(int nameId)
    {
        if (nameId <= 255 || nameId >= 32768)
        {
            throw FontFormatException.Create("An fvar axis name ID is invalid.");
        }
    }

    static void ValidateSubfamilyNameId(int nameId)
    {
        if (nameId != 2 && nameId != 17 &&
            (nameId <= 255 || nameId >= 32768))
        {
            throw FontFormatException.Create(
                "An fvar instance subfamily name ID is invalid.");
        }
    }

    static void ValidatePostScriptNameId(int nameId)
    {
        if (nameId != 6 && nameId != NoPostScriptNameId &&
            (nameId <= 255 || nameId >= 32768))
        {
            throw FontFormatException.Create(
                "An fvar instance PostScript name ID is invalid.");
        }
    }

    static SfntLocalizedName ResolveName(
        SfntNameTable nameTable,
        int nameId,
        string message)
    {
        SfntLocalizedName? name = nameTable.SelectDefaultName(nameId);

        if (name == null)
        {
            throw FontFormatException.Create(message);
        }

        return name;
    }
}
