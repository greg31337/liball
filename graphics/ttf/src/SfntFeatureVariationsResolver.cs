namespace ttf;

internal static class SfntFeatureVariationsResolver
{
    const uint Version10 = 0x00010000U;
    const uint Version11 = 0x00010001U;

    internal static byte[] Resolve(
        byte[] source,
        NormalizedFontCoordinates coordinates,
        FontLimits limits)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        CheckRange(source, 0, 10);
        uint version = ReadUInt32(source, 0);

        if (version == Version10)
        {
            return source;
        }

        if (version != Version11)
        {
            throw FontFormatException.Create(
                "Static instancing cannot resolve an unsupported layout-table version.");
        }

        CheckRange(source, 0, 14);
        int scriptOffset = ReadUInt16(source, 4);
        int featureOffset = ReadUInt16(source, 6);
        int lookupOffset = ReadUInt16(source, 8);
        long featureVariationsLong = ReadUInt32(source, 10);

        if (featureVariationsLong > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A FeatureVariations offset exceeds the supported range.");
        }

        int featureVariationsOffset = (int)featureVariationsLong;
        int lookupEnd = featureVariationsOffset == 0
            ? source.Length
            : featureVariationsOffset;

        if (scriptOffset < 14 || scriptOffset >= featureOffset ||
            featureOffset >= lookupOffset || lookupOffset >= lookupEnd ||
            lookupEnd > source.Length)
        {
            throw FontFormatException.Create(
                "Static FeatureVariations resolution requires ordered layout sections.");
        }

        int featureCount = ReadFeatureCount(
            source,
            featureOffset,
            lookupOffset);
        CheckRange(source, lookupOffset, 2);
        int lookupCount = ReadUInt16(source, lookupOffset);
        int[] substitutions = featureVariationsOffset == 0
            ? new int[featureCount]
            : SelectSubstitutions(
                source,
                featureVariationsOffset,
                coordinates,
                featureCount);
        byte[] featureList = CreateFeatureList(
            source,
            featureOffset,
            lookupOffset,
            substitutions,
            lookupCount,
            limits);
        byte[] scriptList = CopyRange(
            source,
            scriptOffset,
            featureOffset - scriptOffset);
        byte[] lookupList = CopyRange(
            source,
            lookupOffset,
            lookupEnd - lookupOffset);
        int newScriptOffset = 10;
        int newFeatureOffset = CheckedLength(
            (long)newScriptOffset + scriptList.Length,
            limits);
        int newLookupOffset = CheckedLength(
            (long)newFeatureOffset + featureList.Length,
            limits);
        int outputLength = CheckedLength(
            (long)newLookupOffset + lookupList.Length,
            limits);
        ValidateOffset16(newScriptOffset);
        ValidateOffset16(newFeatureOffset);
        ValidateOffset16(newLookupOffset);
        BigEndianWriter writer = BigEndianWriter.Create(
            outputLength,
            limits.GetMaxOutputBytes());
        writer.WriteUInt32(Version10);
        writer.WriteUInt16((ushort)newScriptOffset);
        writer.WriteUInt16((ushort)newFeatureOffset);
        writer.WriteUInt16((ushort)newLookupOffset);
        writer.WriteBytes(scriptList);
        writer.WriteBytes(featureList);
        writer.WriteBytes(lookupList);
        return writer.CopyBytes();
    }

    static int ReadFeatureCount(
        byte[] source,
        int featureOffset,
        int featureEnd)
    {
        CheckRange(source, featureOffset, 2);
        int count = ReadUInt16(source, featureOffset);
        long recordsEnd = (long)featureOffset + 2L + count * 6L;

        if (recordsEnd > featureEnd)
        {
            throw FontFormatException.Create(
                "A layout FeatureList record array is truncated.");
        }

        return count;
    }

    static int[] SelectSubstitutions(
        byte[] source,
        int featureVariationsOffset,
        NormalizedFontCoordinates coordinates,
        int featureCount)
    {
        CheckRange(source, featureVariationsOffset, 8);

        if (ReadUInt16(source, featureVariationsOffset) != 1 ||
            ReadUInt16(source, featureVariationsOffset + 2) != 0)
        {
            throw FontFormatException.Create(
                "The FeatureVariations table version is unsupported.");
        }

        long recordCountLong = ReadUInt32(
            source,
            featureVariationsOffset + 4);

        if (recordCountLong > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A FeatureVariations record count exceeds the supported range.");
        }

        int recordCount = (int)recordCountLong;
        CheckRange(
            source,
            featureVariationsOffset + 8,
            CheckedArrayLength(recordCount, 8));
        int index = 0;

        while (index < recordCount)
        {
            int recordOffset = featureVariationsOffset + 8 + index * 8;
            int conditionOffset = ReadRelativeOffset32(
                source,
                recordOffset,
                featureVariationsOffset);
            int substitutionOffset = ReadRelativeOffset32(
                source,
                recordOffset + 4,
                featureVariationsOffset);

            if (conditionOffset == featureVariationsOffset ||
                MatchesConditionSet(
                    source,
                    conditionOffset,
                    coordinates))
            {
                if (substitutionOffset == featureVariationsOffset)
                {
                    return new int[featureCount];
                }

                CheckRange(source, substitutionOffset, 4);

                if (ReadUInt16(source, substitutionOffset) == 1 &&
                    ReadUInt16(source, substitutionOffset + 2) == 0)
                {
                    return ReadSubstitutions(
                        source,
                        substitutionOffset,
                        featureCount);
                }
            }

            index += 1;
        }

        return new int[featureCount];
    }

    static bool MatchesConditionSet(
        byte[] source,
        int conditionSetOffset,
        NormalizedFontCoordinates coordinates)
    {
        if (conditionSetOffset == 0)
        {
            return true;
        }

        CheckRange(source, conditionSetOffset, 2);
        int count = ReadUInt16(source, conditionSetOffset);
        CheckRange(
            source,
            conditionSetOffset + 2,
            CheckedArrayLength(count, 4));
        int index = 0;

        while (index < count)
        {
            int conditionOffset = ReadRelativeOffset32(
                source,
                conditionSetOffset + 2 + index * 4,
                conditionSetOffset);

            if (conditionOffset == conditionSetOffset ||
                !MatchesCondition(source, conditionOffset, coordinates))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static bool MatchesCondition(
        byte[] source,
        int conditionOffset,
        NormalizedFontCoordinates coordinates)
    {
        CheckRange(source, conditionOffset, 2);

        if (ReadUInt16(source, conditionOffset) != 1)
        {
            return false;
        }

        CheckRange(source, conditionOffset, 8);
        int axisIndex = ReadUInt16(source, conditionOffset + 2);
        double minimum = ReadInt16(source, conditionOffset + 4) / 16384.0;
        double maximum = ReadInt16(source, conditionOffset + 6) / 16384.0;

        if (axisIndex >= coordinates.GetAxisCount() || minimum > maximum)
        {
            return false;
        }

        double value = coordinates.GetNormalizedValue(axisIndex);
        return value >= minimum && value <= maximum;
    }

    static int[] ReadSubstitutions(
        byte[] source,
        int substitutionOffset,
        int featureCount)
    {
        CheckRange(source, substitutionOffset, 6);
        int count = ReadUInt16(source, substitutionOffset + 4);
        CheckRange(
            source,
            substitutionOffset + 6,
            CheckedArrayLength(count, 6));
        int[] substitutions = new int[featureCount];
        int previousFeature = -1;
        int index = 0;

        while (index < count)
        {
            int recordOffset = substitutionOffset + 6 + index * 6;
            int featureIndex = ReadUInt16(source, recordOffset);

            if (featureIndex <= previousFeature || featureIndex >= featureCount)
            {
                throw FontFormatException.Create(
                    "FeatureTableSubstitution records are invalid or unordered.");
            }

            int alternateOffset = ReadRelativeOffset32(
                source,
                recordOffset + 2,
                substitutionOffset);

            if (alternateOffset == substitutionOffset)
            {
                throw FontFormatException.Create(
                    "An alternate Feature table offset is null.");
            }

            substitutions[featureIndex] = alternateOffset;
            previousFeature = featureIndex;
            index += 1;
        }

        return substitutions;
    }

    static byte[] CreateFeatureList(
        byte[] source,
        int featureOffset,
        int featureEnd,
        int[] substitutions,
        int lookupCount,
        FontLimits limits)
    {
        int count = substitutions.Length;
        int recordsLength = 2 + CheckedArrayLength(count, 6);
        byte[][] tags = new byte[count][];
        byte[][] features = new byte[count][];
        int index = 0;

        while (index < count)
        {
            int recordOffset = featureOffset + 2 + index * 6;
            tags[index] = CopyRange(source, recordOffset, 4);
            int defaultRelative = ReadUInt16(source, recordOffset + 4);
            int defaultOffset = CheckedRelativeOffset16(
                featureOffset,
                defaultRelative,
                featureOffset + recordsLength,
                featureEnd);
            features[index] = substitutions[index] == 0
                ? CopyDefaultFeature(
                    source,
                    featureOffset,
                    featureEnd,
                    count,
                    defaultOffset,
                    lookupCount)
                : CopyAlternateFeature(
                    source,
                    substitutions[index],
                    substitutions,
                    lookupCount);
            index += 1;
        }

        int totalLength = recordsLength;
        index = 0;
        while (index < features.Length)
        {
            totalLength = CheckedLength(
                (long)totalLength + features[index].Length,
                limits);
            index += 1;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            totalLength,
            limits.GetMaxOutputBytes());
        writer.WriteUInt16((ushort)count);
        int featureDataOffset = recordsLength;
        index = 0;

        while (index < count)
        {
            ValidateOffset16(featureDataOffset);
            writer.WriteBytes(tags[index]);
            writer.WriteUInt16((ushort)featureDataOffset);
            featureDataOffset += features[index].Length;
            index += 1;
        }

        index = 0;
        while (index < features.Length)
        {
            writer.WriteBytes(features[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CopyDefaultFeature(
        byte[] source,
        int featureOffset,
        int featureEnd,
        int featureCount,
        int selectedOffset,
        int lookupCount)
    {
        int selectedEnd = featureEnd;
        int index = 0;

        while (index < featureCount)
        {
            int recordOffset = featureOffset + 2 + index * 6;
            int candidate = featureOffset + ReadUInt16(source, recordOffset + 4);

            if (candidate > selectedOffset && candidate < selectedEnd)
            {
                selectedEnd = candidate;
            }

            index += 1;
        }

        ValidateFeature(
            source,
            selectedOffset,
            selectedEnd,
            lookupCount);
        return CopyRange(source, selectedOffset, selectedEnd - selectedOffset);
    }

    static byte[] CopyAlternateFeature(
        byte[] source,
        int featureOffset,
        int[] substitutions,
        int lookupCount)
    {
        CheckRange(source, featureOffset, 4);
        int parameterOffset = ReadUInt16(source, featureOffset);
        int featureLookupCount = ReadUInt16(source, featureOffset + 2);
        int minimumLength = 4 + CheckedArrayLength(featureLookupCount, 2);
        int featureEnd = featureOffset + minimumLength;

        if (parameterOffset != 0)
        {
            featureEnd = source.Length;
            int index = 0;
            while (index < substitutions.Length)
            {
                int candidate = substitutions[index];
                if (candidate > featureOffset && candidate < featureEnd)
                {
                    featureEnd = candidate;
                }
                index += 1;
            }
        }

        ValidateFeature(source, featureOffset, featureEnd, lookupCount);
        return CopyRange(source, featureOffset, featureEnd - featureOffset);
    }

    static void ValidateFeature(
        byte[] source,
        int featureOffset,
        int featureEnd,
        int lookupCount)
    {
        CheckRange(source, featureOffset, 4);
        int parameterOffset = ReadUInt16(source, featureOffset);
        int featureLookupCount = ReadUInt16(source, featureOffset + 2);
        int minimumLength = 4 + CheckedArrayLength(featureLookupCount, 2);

        if (featureOffset + minimumLength > featureEnd ||
            (parameterOffset != 0 &&
            (parameterOffset < minimumLength ||
            featureOffset + parameterOffset >= featureEnd)))
        {
            throw FontFormatException.Create(
                "A selected layout Feature table is malformed.");
        }

        int index = 0;
        while (index < featureLookupCount)
        {
            if (ReadUInt16(source, featureOffset + 4 + index * 2) >=
                lookupCount)
            {
                throw FontFormatException.Create(
                    "A selected Feature table references an invalid lookup index.");
            }
            index += 1;
        }
    }

    static int CheckedRelativeOffset16(
        int baseOffset,
        int relativeOffset,
        int minimum,
        int maximum)
    {
        int result = baseOffset + relativeOffset;
        if (relativeOffset == 0 || result < minimum || result >= maximum)
        {
            throw FontFormatException.Create(
                "A layout Feature table offset is invalid.");
        }
        return result;
    }

    static int ReadRelativeOffset32(
        byte[] source,
        int offset,
        int baseOffset)
    {
        long relative = ReadUInt32(source, offset);

        if (relative == 0)
        {
            return baseOffset;
        }

        long absolute = (long)baseOffset + relative;
        if (absolute < 0 || absolute >= source.Length)
        {
            throw FontFormatException.Create(
                "A FeatureVariations relative offset is invalid.");
        }
        return (int)absolute;
    }

    static int CheckedArrayLength(int count, int itemSize)
    {
        long length = (long)count * itemSize;
        if (length > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A FeatureVariations array exceeds the supported range.");
        }
        return (int)length;
    }

    static int CheckedLength(long length, FontLimits limits)
    {
        if (length < 0 || length > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "A resolved layout table exceeds the output limit.");
        }
        return (int)length;
    }

    static void ValidateOffset16(int offset)
    {
        if (offset < 0 || offset > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A resolved layout-table offset exceeds the UInt16 range.");
        }
    }

    static byte[] CopyRange(byte[] source, int offset, int length)
    {
        CheckRange(source, offset, length);
        return source.AsSpan(offset, length).ToArray();
    }

    static void CheckRange(byte[] source, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > source.Length - length)
        {
            throw FontFormatException.Create(
                "FeatureVariations data is truncated or out of bounds.");
        }
    }

    static int ReadUInt16(byte[] source, int offset)
    {
        CheckRange(source, offset, 2);
        return source[offset] << 8 | source[offset + 1];
    }

    static int ReadInt16(byte[] source, int offset)
    {
        int value = ReadUInt16(source, offset);
        return value >= 0x8000 ? value - 0x10000 : value;
    }

    static uint ReadUInt32(byte[] source, int offset)
    {
        CheckRange(source, offset, 4);
        return (uint)source[offset] << 24 |
            (uint)source[offset + 1] << 16 |
            (uint)source[offset + 2] << 8 |
            source[offset + 3];
    }
}
