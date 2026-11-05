namespace ttf;

/// <summary>Represents conditional OpenType FeatureVariations data.</summary>
public class FontLayoutFeatureVariations : IFontLayoutFeatureVariations
{
    int majorVersion;
    int minorVersion;
    int[][] conditionFormats;
    int[][] conditionAxes;
    double[][] conditionMinimums;
    double[][] conditionMaximums;
    int[][] substitutedFeatureIndices;
    FontLayoutFeature[][] alternateFeatures;

    internal FontLayoutFeatureVariations(
        SfntLayoutTable table,
        FontLayoutTable owner)
    {
        byte[] bytes = table.GetDataSlice().CopyBytes();
        int baseOffset = table.GetFeatureVariationsOffset();
        Check(bytes, baseOffset, 8);
        majorVersion = ReadUInt16(bytes, baseOffset);
        minorVersion = ReadUInt16(bytes, baseOffset + 2);
        if (majorVersion != 1 || minorVersion != 0)
        {
            throw FontFormatException.Create(
                "The FeatureVariations table version is unsupported.");
        }
        long countValue = ReadUInt32(bytes, baseOffset + 4);
        if (countValue > int.MaxValue)
        {
            throw FontFormatException.Create(
                "The FeatureVariations record count is too large.");
        }
        int count = (int)countValue;
        Check(bytes, baseOffset + 8, CheckedLength(count, 8));
        conditionFormats = new int[count][];
        conditionAxes = new int[count][];
        conditionMinimums = new double[count][];
        conditionMaximums = new double[count][];
        substitutedFeatureIndices = new int[count][];
        alternateFeatures = new FontLayoutFeature[count][];
        int recordIndex = 0;
        while (recordIndex < count)
        {
            int recordOffset = baseOffset + 8 + recordIndex * 8;
            int conditionOffset = ResolveRelative(
                bytes,
                baseOffset,
                ReadUInt32(bytes, recordOffset));
            int substitutionOffset = ResolveRelative(
                bytes,
                baseOffset,
                ReadUInt32(bytes, recordOffset + 4));
            ParseConditions(bytes, recordIndex, conditionOffset);
            ParseSubstitutions(
                bytes,
                recordIndex,
                substitutionOffset,
                table,
                owner);
            recordIndex += 1;
        }
    }

    /// <summary>Gets the major FeatureVariations version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return majorVersion; }

    /// <summary>Gets the minor FeatureVariations version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return minorVersion; }

    /// <summary>Gets the variation-record count.</summary>
    /// <returns>The record count.</returns>
    public int GetRecordCount() { return conditionFormats.Length; }

    /// <summary>Gets one record's condition count.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The condition count.</returns>
    public int GetConditionCount(int recordIndex)
    {
        ValidateRecord(recordIndex);
        return conditionFormats[recordIndex].Length;
    }

    /// <summary>Gets one condition format.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The condition format.</returns>
    public int GetConditionFormat(int recordIndex, int conditionIndex)
    {
        ValidateCondition(recordIndex, conditionIndex);
        return conditionFormats[recordIndex][conditionIndex];
    }

    /// <summary>Gets one format 1 condition's axis index.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The variation-axis index.</returns>
    public int GetConditionAxisIndex(int recordIndex, int conditionIndex)
    {
        RequireFormatOne(recordIndex, conditionIndex);
        return conditionAxes[recordIndex][conditionIndex];
    }

    /// <summary>Gets one format 1 condition's inclusive minimum.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The normalized minimum.</returns>
    public double GetConditionMinimum(int recordIndex, int conditionIndex)
    {
        RequireFormatOne(recordIndex, conditionIndex);
        return conditionMinimums[recordIndex][conditionIndex];
    }

    /// <summary>Gets one format 1 condition's inclusive maximum.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The normalized maximum.</returns>
    public double GetConditionMaximum(int recordIndex, int conditionIndex)
    {
        RequireFormatOne(recordIndex, conditionIndex);
        return conditionMaximums[recordIndex][conditionIndex];
    }

    /// <summary>Gets one record's feature-substitution count.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The substitution count.</returns>
    public int GetSubstitutionCount(int recordIndex)
    {
        ValidateRecord(recordIndex);
        return substitutedFeatureIndices[recordIndex].Length;
    }

    /// <summary>Gets one substituted feature-list index.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="substitutionIndex">The zero-based substitution index.</param>
    /// <returns>The feature-list index.</returns>
    public int GetSubstitutedFeatureIndex(
        int recordIndex,
        int substitutionIndex)
    {
        ValidateSubstitution(recordIndex, substitutionIndex);
        return substitutedFeatureIndices[recordIndex][substitutionIndex];
    }

    /// <summary>Gets one alternate feature table.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="substitutionIndex">The zero-based substitution index.</param>
    /// <returns>The alternate feature model.</returns>
    public FontLayoutFeature GetAlternateFeature(
        int recordIndex,
        int substitutionIndex)
    {
        ValidateSubstitution(recordIndex, substitutionIndex);
        return alternateFeatures[recordIndex][substitutionIndex];
    }

    /// <summary>Reports whether all supported conditions in a record match.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="coordinates">The final normalized coordinates.</param>
    /// <returns>True when the record matches; otherwise false.</returns>
    public bool Matches(
        int recordIndex,
        NormalizedFontCoordinates coordinates)
    {
        ValidateRecord(recordIndex);
        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }
        int conditionIndex = 0;
        while (conditionIndex < conditionFormats[recordIndex].Length)
        {
            if (conditionFormats[recordIndex][conditionIndex] != 1)
            {
                return false;
            }
            int axisIndex = conditionAxes[recordIndex][conditionIndex];
            if (axisIndex < 0 || axisIndex >= coordinates.GetAxisCount())
            {
                return false;
            }
            double coordinate = coordinates.GetNormalizedValue(axisIndex);
            if (coordinate < conditionMinimums[recordIndex][conditionIndex] ||
                coordinate > conditionMaximums[recordIndex][conditionIndex])
            {
                return false;
            }
            conditionIndex += 1;
        }
        return true;
    }

    /// <summary>Finds the first matching variation record.</summary>
    /// <param name="coordinates">The final normalized coordinates.</param>
    /// <returns>The first matching record index, or -1.</returns>
    public int FindMatchingRecord(NormalizedFontCoordinates coordinates)
    {
        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }
        int index = 0;
        while (index < conditionFormats.Length)
        {
            if (Matches(index, coordinates)) { return index; }
            index += 1;
        }
        return -1;
    }

    void ParseConditions(byte[] bytes, int recordIndex, int offset)
    {
        if (offset == 0)
        {
            conditionFormats[recordIndex] = new int[0];
            conditionAxes[recordIndex] = new int[0];
            conditionMinimums[recordIndex] = new double[0];
            conditionMaximums[recordIndex] = new double[0];
            return;
        }
        Check(bytes, offset, 2);
        int count = ReadUInt16(bytes, offset);
        Check(bytes, offset + 2, CheckedLength(count, 4));
        conditionFormats[recordIndex] = new int[count];
        conditionAxes[recordIndex] = new int[count];
        conditionMinimums[recordIndex] = new double[count];
        conditionMaximums[recordIndex] = new double[count];
        int conditionIndex = 0;
        while (conditionIndex < count)
        {
            int conditionOffset = ResolveRelative(
                bytes,
                offset,
                ReadUInt32(bytes, offset + 2 + conditionIndex * 4));
            if (conditionOffset == 0)
            {
                throw FontFormatException.Create(
                    "A FeatureVariations condition offset is null.");
            }
            Check(bytes, conditionOffset, 2);
            int format = ReadUInt16(bytes, conditionOffset);
            conditionFormats[recordIndex][conditionIndex] = format;
            conditionAxes[recordIndex][conditionIndex] = -1;
            if (format == 1)
            {
                Check(bytes, conditionOffset, 8);
                conditionAxes[recordIndex][conditionIndex] =
                    ReadUInt16(bytes, conditionOffset + 2);
                conditionMinimums[recordIndex][conditionIndex] =
                    ReadInt16(bytes, conditionOffset + 4) / 16384.0;
                conditionMaximums[recordIndex][conditionIndex] =
                    ReadInt16(bytes, conditionOffset + 6) / 16384.0;
            }
            conditionIndex += 1;
        }
    }

    void ParseSubstitutions(
        byte[] bytes,
        int recordIndex,
        int offset,
        SfntLayoutTable table,
        FontLayoutTable owner)
    {
        if (offset == 0)
        {
            substitutedFeatureIndices[recordIndex] = new int[0];
            alternateFeatures[recordIndex] = new FontLayoutFeature[0];
            return;
        }
        Check(bytes, offset, 6);
        int major = ReadUInt16(bytes, offset);
        int minor = ReadUInt16(bytes, offset + 2);
        if (major != 1 || minor != 0)
        {
            substitutedFeatureIndices[recordIndex] = new int[0];
            alternateFeatures[recordIndex] = new FontLayoutFeature[0];
            return;
        }
        int count = ReadUInt16(bytes, offset + 4);
        Check(bytes, offset + 6, CheckedLength(count, 6));
        substitutedFeatureIndices[recordIndex] = new int[count];
        alternateFeatures[recordIndex] = new FontLayoutFeature[count];
        int previousFeatureIndex = -1;
        int substitutionIndex = 0;
        while (substitutionIndex < count)
        {
            int recordOffset = offset + 6 + substitutionIndex * 6;
            int featureIndex = ReadUInt16(bytes, recordOffset);
            if (featureIndex <= previousFeatureIndex ||
                featureIndex >= owner.GetFeatureCount())
            {
                throw FontFormatException.Create(
                    "FeatureTableSubstitution records are invalid or unordered.");
            }
            int alternateOffset = ResolveRelative(
                bytes,
                offset,
                ReadUInt32(bytes, recordOffset + 2));
            if (alternateOffset == 0)
            {
                throw FontFormatException.Create(
                    "An alternate Feature table offset is null.");
            }
            substitutedFeatureIndices[recordIndex][substitutionIndex] =
                featureIndex;
            alternateFeatures[recordIndex][substitutionIndex] =
                ParseAlternateFeature(
                    bytes,
                    alternateOffset,
                    featureIndex,
                    table,
                    owner);
            previousFeatureIndex = featureIndex;
            substitutionIndex += 1;
        }
    }

    FontLayoutFeature ParseAlternateFeature(
        byte[] bytes,
        int offset,
        int featureIndex,
        SfntLayoutTable table,
        FontLayoutTable owner)
    {
        Check(bytes, offset, 4);
        int parameterRelative = ReadUInt16(bytes, offset);
        int lookupCount = ReadUInt16(bytes, offset + 2);
        Check(bytes, offset + 4, CheckedLength(lookupCount, 2));
        int[] lookupIndices = new int[lookupCount];
        int index = 0;
        while (index < lookupCount)
        {
            int lookupIndex = ReadUInt16(bytes, offset + 4 + index * 2);
            if (lookupIndex >= owner.GetLookupCount())
            {
                throw FontFormatException.Create(
                    "An alternate Feature references an invalid lookup.");
            }
            lookupIndices[index] = lookupIndex;
            index += 1;
        }
        int parameterOffset = parameterRelative == 0 ? 0 :
            ResolveRelative(bytes, offset, (uint)parameterRelative);
        SfntLayoutFeature source = table.GetFeatureList().GetFeature(featureIndex);
        return new FontLayoutFeature(
            featureIndex,
            source.GetFeatureTag(),
            parameterOffset,
            lookupIndices,
            table.GetDataSlice());
    }

    void ValidateRecord(int index)
    {
        if (index < 0 || index >= conditionFormats.Length)
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }
    }

    void ValidateCondition(int recordIndex, int conditionIndex)
    {
        ValidateRecord(recordIndex);
        if (conditionIndex < 0 ||
            conditionIndex >= conditionFormats[recordIndex].Length)
        {
            throw new System.ArgumentOutOfRangeException("conditionIndex");
        }
    }

    void ValidateSubstitution(int recordIndex, int substitutionIndex)
    {
        ValidateRecord(recordIndex);
        if (substitutionIndex < 0 || substitutionIndex >=
            substitutedFeatureIndices[recordIndex].Length)
        {
            throw new System.ArgumentOutOfRangeException("substitutionIndex");
        }
    }

    void RequireFormatOne(int recordIndex, int conditionIndex)
    {
        ValidateCondition(recordIndex, conditionIndex);
        if (conditionFormats[recordIndex][conditionIndex] != 1)
        {
            throw new System.InvalidOperationException(
                "The condition is not a supported format 1 condition.");
        }
    }

    int ResolveRelative(byte[] bytes, int baseOffset, uint relative)
    {
        if (relative == 0) { return 0; }
        long absolute = (long)baseOffset + relative;
        if (absolute < 0 || absolute >= bytes.Length)
        {
            throw FontFormatException.Create(
                "A FeatureVariations relative offset is invalid.");
        }
        return (int)absolute;
    }

    int CheckedLength(int count, int itemSize)
    {
        long length = (long)count * itemSize;
        if (length > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A FeatureVariations array is too large.");
        }
        return (int)length;
    }

    void Check(byte[] bytes, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > bytes.Length - length)
        {
            throw FontFormatException.Create(
                "FeatureVariations data is truncated or out of bounds.");
        }
    }

    int ReadUInt16(byte[] bytes, int offset)
    {
        Check(bytes, offset, 2);
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    int ReadInt16(byte[] bytes, int offset)
    {
        int value = ReadUInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }

    uint ReadUInt32(byte[] bytes, int offset)
    {
        Check(bytes, offset, 4);
        return (uint)bytes[offset] << 24 |
            (uint)bytes[offset + 1] << 16 |
            (uint)bytes[offset + 2] << 8 |
            bytes[offset + 3];
    }
}
