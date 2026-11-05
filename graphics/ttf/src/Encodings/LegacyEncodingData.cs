namespace ttf;

internal class LegacyDecodedText
{
    string value;
    int replacementCount;

    internal LegacyDecodedText(string value, int replacementCount)
    {
        this.value = value;
        this.replacementCount = replacementCount;
    }

    internal string GetValue()
    {
        return value;
    }

    internal int GetReplacementCount()
    {
        return replacementCount;
    }
}

internal class LegacyEncodingMap
{
    string[] compressedParts;
    object initializationLock = new object();
    bool initialized;
    int[]? encodedValues;
    int[]? unicodeValues;
    int[]? reverseUnicodeValues;
    int[]? reverseEncodedValues;
    int[]? distinctUnicodeValues;
    bool[]? leadBytes;

    internal LegacyEncodingMap(string[] compressedParts)
    {
        this.compressedParts = compressedParts;
    }

    internal int DecodeEncodedValue(int encodedValue)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            int index = FindValue(encodedValues, encodedValue);
            System.ArgumentNullException.ThrowIfNull(unicodeValues);
            return index < 0 ? -1 : unicodeValues[index];
        }
    }

    internal int EncodeUnicodeValue(int unicodeValue)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            int index = FindFirstValue(reverseUnicodeValues, unicodeValue);
            System.ArgumentNullException.ThrowIfNull(reverseEncodedValues);
            return index < 0 ? -1 : reverseEncodedValues[index];
        }
    }

    internal bool IsLeadByte(int value)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            System.ArgumentNullException.ThrowIfNull(leadBytes);
            return value >= 0 && value < leadBytes.Length && leadBytes[value];
        }
    }

    internal int GetUnicodeValueCount()
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            System.ArgumentNullException.ThrowIfNull(distinctUnicodeValues);
            return distinctUnicodeValues.Length;
        }
    }

    internal int GetUnicodeValue(int index)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            System.ArgumentNullException.ThrowIfNull(distinctUnicodeValues);

            if (index < 0 || index >= distinctUnicodeValues.Length)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            return distinctUnicodeValues[index];
        }
    }

    internal int GetEncodedValueCount(int unicodeValue)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            int index = FindFirstValue(reverseUnicodeValues, unicodeValue);

            if (index < 0)
            {
                return 0;
            }

            int count = 0;

            System.ArgumentNullException.ThrowIfNull(reverseUnicodeValues);
            while (index + count < reverseUnicodeValues.Length &&
                reverseUnicodeValues[index + count] == unicodeValue)
            {
                count += 1;
            }

            return count;
        }
    }

    internal int GetEncodedValue(int unicodeValue, int index)
    {
        lock (initializationLock)
        {
            EnsureInitialized();
            int firstIndex = FindFirstValue(reverseUnicodeValues, unicodeValue);
            int count = 0;

            if (firstIndex >= 0)
            {
                System.ArgumentNullException.ThrowIfNull(reverseUnicodeValues);
                while (firstIndex + count < reverseUnicodeValues.Length &&
                    reverseUnicodeValues[firstIndex + count] == unicodeValue)
                {
                    count += 1;
                }
            }

            if (index < 0 || index >= count)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            System.ArgumentNullException.ThrowIfNull(reverseEncodedValues);
            return reverseEncodedValues[firstIndex + index];
        }
    }

    void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        string encodedData = System.String.Concat(compressedParts);
        byte[] compressedData = System.Convert.FromBase64String(encodedData);
        byte[] mappingData;

        using (System.IO.MemoryStream input =
            new System.IO.MemoryStream(compressedData, false))
        using (System.IO.Compression.GZipStream inflater =
            new System.IO.Compression.GZipStream(
                input,
                System.IO.Compression.CompressionMode.Decompress,
                false))
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            inflater.CopyTo(output);
            mappingData = output.ToArray();
        }

        if ((mappingData.Length & 3) != 0)
        {
            throw new System.InvalidOperationException(
                "The generated legacy encoding data is malformed.");
        }

        int count = mappingData.Length / 4;
        encodedValues = new int[count];
        unicodeValues = new int[count];
        leadBytes = new bool[256];
        int index = 0;

        while (index < count)
        {
            int offset = index * 4;
            int encodedValue = mappingData[offset] << 8 |
                mappingData[offset + 1];
            int unicodeValue = mappingData[offset + 2] << 8 |
                mappingData[offset + 3];
            encodedValues[index] = encodedValue;
            unicodeValues[index] = unicodeValue;

            if (encodedValue > 0xFF)
            {
                leadBytes[encodedValue >> 8] = true;
            }

            index += 1;
        }

        BuildReverseMapping();
        compressedParts = new string[0];
        initialized = true;
    }

    void BuildReverseMapping()
    {
        System.ArgumentNullException.ThrowIfNull(unicodeValues);
        int[] sortedUnicodeValues = new int[unicodeValues.Length];
        System.ArgumentNullException.ThrowIfNull(encodedValues);
        int[] sortedEncodedValues = new int[encodedValues.Length];
        System.Array.Copy(
            unicodeValues,
            sortedUnicodeValues,
            unicodeValues.Length);
        System.Array.Copy(
            encodedValues,
            sortedEncodedValues,
            encodedValues.Length);
        System.Array.Sort(sortedUnicodeValues, sortedEncodedValues);
        int uniqueCount = CountUniqueValues(sortedUnicodeValues);
        reverseUnicodeValues = sortedUnicodeValues;
        reverseEncodedValues = sortedEncodedValues;
        distinctUnicodeValues = new int[uniqueCount];
        int sourceIndex = 0;
        int targetIndex = 0;

        while (sourceIndex < sortedUnicodeValues.Length)
        {
            int unicodeValue = sortedUnicodeValues[sourceIndex];
            int groupStart = sourceIndex;
            sourceIndex += 1;

            while (sourceIndex < sortedUnicodeValues.Length &&
                sortedUnicodeValues[sourceIndex] == unicodeValue)
            {
                sourceIndex += 1;
            }

            System.Array.Sort(
                sortedEncodedValues,
                groupStart,
                sourceIndex - groupStart);
            distinctUnicodeValues[targetIndex] = unicodeValue;
            targetIndex += 1;
        }
    }

    static int CountUniqueValues(int[] values)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        int count = 1;
        int index = 1;

        while (index < values.Length)
        {
            if (values[index] != values[index - 1])
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static int FindValue(int[]? values, int value)
    {
        int low = 0;
        System.ArgumentNullException.ThrowIfNull(values);
        int high = values.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if (value < values[middle])
            {
                high = middle - 1;
            }
            else if (value > values[middle])
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return -1;
    }

    static int FindFirstValue(int[]? values, int value)
    {
        int index = FindValue(values, value);
        System.ArgumentNullException.ThrowIfNull(values);

        if (index < 0)
        {
            return -1;
        }

        while (index > 0 && values[index - 1] == value)
        {
            index -= 1;
        }

        return index;
    }
}

internal static class LegacyEncodingData
{
    const char ReplacementCharacter = '\uFFFD';

    internal static bool IsCodePageSupported(int codePage)
    {
        return LegacyEncodingTables.GetMap(codePage) != null;
    }

    internal static int DecodeEncodedValue(int codePage, int encodedValue)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);
        return map == null ? -1 : map.DecodeEncodedValue(encodedValue);
    }

    internal static int EncodeUnicodeValue(int codePage, int unicodeValue)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);
        return map == null ? -1 : map.EncodeUnicodeValue(unicodeValue);
    }

    internal static int GetUnicodeValueCount(int codePage)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);
        return map == null ? 0 : map.GetUnicodeValueCount();
    }

    internal static int GetUnicodeValue(int codePage, int index)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);

        if (map == null)
        {
            throw new System.ArgumentOutOfRangeException("codePage");
        }

        return map.GetUnicodeValue(index);
    }

    internal static int GetEncodedValueCount(int codePage, int unicodeValue)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);
        return map == null ? 0 : map.GetEncodedValueCount(unicodeValue);
    }

    internal static int GetEncodedValue(
        int codePage,
        int unicodeValue,
        int index)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);

        if (map == null)
        {
            throw new System.ArgumentOutOfRangeException("codePage");
        }

        return map.GetEncodedValue(unicodeValue, index);
    }

    internal static LegacyDecodedText? Decode(
        FontDataSlice data,
        int codePage)
    {
        LegacyEncodingMap map = LegacyEncodingTables.GetMap(codePage);

        if (map == null)
        {
            return null;
        }

        ReadOnlySpan<byte> bytes = data.GetMemory().Span;
        char[] characters = new char[bytes.Length];
        int byteIndex = 0;
        int characterIndex = 0;
        int replacementCount = 0;

        while (byteIndex < bytes.Length)
        {
            int firstByte = bytes[byteIndex];
            int unicodeValue = -1;

            if (map.IsLeadByte(firstByte))
            {
                if (byteIndex + 1 < bytes.Length)
                {
                    int encodedValue = firstByte << 8 | bytes[byteIndex + 1];
                    unicodeValue = map.DecodeEncodedValue(encodedValue);

                    if (unicodeValue >= 0)
                    {
                        byteIndex += 2;
                    }
                    else
                    {
                        byteIndex += 1;
                    }
                }
                else
                {
                    byteIndex += 1;
                }
            }
            else
            {
                unicodeValue = map.DecodeEncodedValue(firstByte);
                byteIndex += 1;
            }

            if (unicodeValue < 0)
            {
                characters[characterIndex] = ReplacementCharacter;
                replacementCount += 1;
            }
            else
            {
                characters[characterIndex] = (char)unicodeValue;
            }

            characterIndex += 1;
        }

        return new LegacyDecodedText(
            new string(characters, 0, characterIndex),
            replacementCount);
    }

    internal static int GetCodePage(int platformId, int encodingId)
    {
        if (platformId == 1)
        {
            return GetMacintoshCodePage(encodingId);
        }

        if (platformId == 3)
        {
            if (encodingId == 2)
            {
                return 932;
            }

            if (encodingId == 3)
            {
                return 936;
            }

            if (encodingId == 4)
            {
                return 950;
            }

            if (encodingId == 5)
            {
                return 949;
            }
        }

        return 0;
    }

    static int GetMacintoshCodePage(int encodingId)
    {
        if (encodingId >= 0 && encodingId <= 7)
        {
            return 10000 + encodingId;
        }

        if (encodingId == 21)
        {
            return 10021;
        }

        if (encodingId == 25)
        {
            return 10008;
        }

        if (encodingId == 29)
        {
            return 10029;
        }

        if (encodingId == 35)
        {
            return 10081;
        }

        if (encodingId == 37)
        {
            return 10079;
        }

        return 0;
    }
}
