namespace ttf;

internal static class UnicodeDataTags
{
    internal const int Bidi = 0x42494449;
    internal const int Bracket = 0x42524B54;
    internal const int CanonicalCombiningClass = 0x4343434C;
    internal const int CompositionExclusion = 0x43455843;
    internal const int CanonicalDecomposition = 0x4445434F;
    internal const int DefaultIgnorable = 0x4449474E;
    internal const int Emoji = 0x454D4F4A;
    internal const int EmojiComponent = 0x454D434F;
    internal const int EmojiModifier = 0x454D4D44;
    internal const int EmojiModifierBase = 0x454D4D42;
    internal const int EmojiPresentation = 0x454D5052;
    internal const int EastAsianWidth = 0x45415744;
    internal const int ExtendedPictographic = 0x45585450;
    internal const int GeneralCategory = 0x47434154;
    internal const int GraphemeBreak = 0x47525048;
    internal const int HangulSyllableType = 0x48535459;
    internal const int IndicPositionalCategory = 0x494E5043;
    internal const int IndicConjunctBreak = 0x494E4342;
    internal const int IndicSyllabicCategory = 0x494E5343;
    internal const int JoiningGroup = 0x4A4F4E47;
    internal const int JoiningType = 0x4A4F4E54;
    internal const int LineBreak = 0x4C494E45;
    internal const int Mirror = 0x4D495252;
    internal const int ModifierCombiningMark = 0x4D434D4B;
    internal const int NfcQuickCheck = 0x4E464351;
    internal const int NfdQuickCheck = 0x4E464451;
    internal const int NfkcQuickCheck = 0x4E464B51;
    internal const int NfkdQuickCheck = 0x4E464B44;
    internal const int ScriptExtensions = 0x53434558;
    internal const int Script = 0x53435054;
    internal const int VariationSelector = 0x56534C43;
    internal const int VerticalOrientation = 0x56455254;
}

internal class UnicodeDataSection
{
    int tag;
    int kind;
    int defaultValue;
    string[] names;
    int[] rangeStarts;
    int[] rangeEnds;
    int[] rangeValues;
    int[] mappingKeys;
    int[] mappingValues;
    int[] mappingAuxiliary;
    int[] sequenceKeys;
    int[] sequenceOffsets;
    int[] sequenceLengths;
    int[] sequencePool;

    internal UnicodeDataSection(
        int tag,
        int kind,
        int defaultValue,
        string[] names,
        int[] rangeStarts,
        int[] rangeEnds,
        int[] rangeValues,
        int[] mappingKeys,
        int[] mappingValues,
        int[] mappingAuxiliary,
        int[] sequenceKeys,
        int[] sequenceOffsets,
        int[] sequenceLengths,
        int[] sequencePool)
    {
        this.tag = tag;
        this.kind = kind;
        this.defaultValue = defaultValue;
        this.names = names;
        this.rangeStarts = rangeStarts;
        this.rangeEnds = rangeEnds;
        this.rangeValues = rangeValues;
        this.mappingKeys = mappingKeys;
        this.mappingValues = mappingValues;
        this.mappingAuxiliary = mappingAuxiliary;
        this.sequenceKeys = sequenceKeys;
        this.sequenceOffsets = sequenceOffsets;
        this.sequenceLengths = sequenceLengths;
        this.sequencePool = sequencePool;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal int GetKind()
    {
        return kind;
    }

    internal int GetEnumeratedValue(int codePoint)
    {
        EnsureKind(1);
        int index = FindRange(codePoint);
        return index < 0 ? defaultValue : rangeValues[index];
    }

    internal string GetEnumeratedName(int codePoint)
    {
        return names[GetEnumeratedValue(codePoint)];
    }

    internal int GetMappingValue(int codePoint)
    {
        EnsureKind(2);
        int index = FindKey(mappingKeys, codePoint);
        return index < 0 ? -1 : mappingValues[index];
    }

    internal int GetMappingAuxiliary(int codePoint)
    {
        EnsureKind(2);
        int index = FindKey(mappingKeys, codePoint);
        return index < 0 ? 0 : mappingAuxiliary[index];
    }

    internal int GetSequenceLength(int codePoint)
    {
        EnsureKind(3);
        int index = FindKey(sequenceKeys, codePoint);
        return index < 0 ? 0 : sequenceLengths[index];
    }

    internal int GetSequenceValue(int codePoint, int sequenceIndex)
    {
        EnsureKind(3);
        int index = FindKey(sequenceKeys, codePoint);

        if (index < 0 || sequenceIndex < 0 ||
            sequenceIndex >= sequenceLengths[index])
        {
            throw new System.ArgumentOutOfRangeException("sequenceIndex");
        }

        return sequencePool[sequenceOffsets[index] + sequenceIndex];
    }

    int FindRange(int codePoint)
    {
        int low = 0;
        int high = rangeStarts.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if (codePoint < rangeStarts[middle])
            {
                high = middle - 1;
            }
            else if (codePoint > rangeEnds[middle])
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

    static int FindKey(int[] keys, int codePoint)
    {
        int low = 0;
        int high = keys.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = keys[middle];

            if (candidate == codePoint)
            {
                return middle;
            }

            if (candidate < codePoint)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    void EnsureKind(int expected)
    {
        if (kind != expected)
        {
            throw new System.InvalidOperationException(
                "The Unicode data section has a different value kind.");
        }
    }
}

internal class UnicodeData
{
    const int HeaderSize = 48;
    const int DirectoryEntrySize = 12;
    const int MaximumCodePoint = 0x10FFFF;
    byte[] bytes;
    int formatVersion;
    int unicodeMajorVersion;
    int unicodeMinorVersion;
    int unicodePatchVersion;
    byte[]? fingerprint;
    UnicodeDataSection[]? sections;

    internal UnicodeData(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        this.bytes = new byte[bytes.Length];
        System.Array.Copy(bytes, this.bytes, bytes.Length);
        Parse();
    }

    internal static UnicodeData LoadDefault()
    {
        System.Reflection.Assembly assembly =
            System.Reflection.Assembly.GetExecutingAssembly();
        using (System.IO.Stream? stream = assembly.GetManifestResourceStream(
            "ttf.UnicodeData.bin"))
        {
            if (stream == null)
            {
                throw FontFormatException.Create(
                    "The embedded Unicode data resource is missing.");
            }

            using (System.IO.MemoryStream output =
                new System.IO.MemoryStream())
            {
                stream.CopyTo(output);
                return new UnicodeData(output.ToArray());
            }
        }
    }

    internal int GetFormatVersion()
    {
        return formatVersion;
    }

    internal int GetUnicodeMajorVersion()
    {
        return unicodeMajorVersion;
    }

    internal int GetUnicodeMinorVersion()
    {
        return unicodeMinorVersion;
    }

    internal int GetUnicodePatchVersion()
    {
        return unicodePatchVersion;
    }

    internal int GetSectionCount()
    {
        System.ArgumentNullException.ThrowIfNull(sections);
        return sections.Length;
    }

    internal byte[] CopyFingerprint()
    {
        System.ArgumentNullException.ThrowIfNull(fingerprint);
        byte[] copy = new byte[fingerprint.Length];
        System.Array.Copy(fingerprint, copy, fingerprint.Length);
        return copy;
    }

    internal byte[] CopyBytes()
    {
        byte[] copy = new byte[bytes.Length];
        System.Array.Copy(bytes, copy, bytes.Length);
        return copy;
    }

    internal bool HasSection(int tag)
    {
        return FindSection(tag) != null;
    }

    internal string GetPropertyName(int tag, int codePoint)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetEnumeratedName(codePoint);
    }

    internal int GetPropertyValue(int tag, int codePoint)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetEnumeratedValue(codePoint);
    }

    internal int GetMapping(int tag, int codePoint)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetMappingValue(codePoint);
    }

    internal int GetMappingAuxiliary(int tag, int codePoint)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetMappingAuxiliary(codePoint);
    }

    internal int GetSequenceLength(int tag, int codePoint)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetSequenceLength(codePoint);
    }

    internal int GetSequenceValue(
        int tag,
        int codePoint,
        int sequenceIndex)
    {
        ValidateCodePoint(codePoint);
        return RequireSection(tag).GetSequenceValue(
            codePoint,
            sequenceIndex);
    }

    void Parse()
    {
        if (bytes.Length < HeaderSize ||
            bytes[0] != 0x54 || bytes[1] != 0x54 ||
            bytes[2] != 0x46 || bytes[3] != 0x55 ||
            bytes[4] != 0x43 || bytes[5] != 0x44 ||
            bytes[6] != 0 || bytes[7] != 0)
        {
            throw FontFormatException.Create(
                "The Unicode data header is invalid.");
        }

        formatVersion = ReadUInt16(bytes, 8);
        unicodeMajorVersion = bytes[10];
        unicodeMinorVersion = bytes[11];
        unicodePatchVersion = bytes[12];

        if (formatVersion != 1 || bytes[13] != 0)
        {
            throw FontFormatException.Create(
                "The Unicode data version is not supported.");
        }

        fingerprint = new byte[32];
        System.Array.Copy(bytes, 14, fingerprint, 0, 32);
        int sectionCount = ReadUInt16(bytes, 46);
        long directoryEnd = HeaderSize +
            (long)sectionCount * DirectoryEntrySize;

        if (directoryEnd > bytes.Length)
        {
            throw FontFormatException.Create(
                "The Unicode data section directory is truncated.");
        }

        sections = new UnicodeDataSection[sectionCount];
        int previousTag = -1;
        int previousEnd = AlignFour((int)directoryEnd);
        int index = 0;

        while (index < sectionCount)
        {
            int directoryOffset = HeaderSize + index * DirectoryEntrySize;
            int tag = ReadInt32(bytes, directoryOffset);
            long offsetValue = ReadUInt32(bytes, directoryOffset + 4);
            long lengthValue = ReadUInt32(bytes, directoryOffset + 8);

            if (tag <= previousTag || offsetValue > int.MaxValue ||
                lengthValue > int.MaxValue)
            {
                throw FontFormatException.Create(
                    "The Unicode data section directory is invalid.");
            }

            int offset = (int)offsetValue;
            int length = (int)lengthValue;
            long end = offsetValue + lengthValue;

            if ((offset & 3) != 0 || offset < previousEnd ||
                end > bytes.Length)
            {
                throw FontFormatException.Create(
                    "A Unicode data section is outside the artifact bounds.");
            }

            sections[index] = ParseSection(tag, offset, length);
            previousTag = tag;
            previousEnd = AlignFour((int)end);
            index += 1;
        }
    }

    UnicodeDataSection ParseSection(int tag, int offset, int length)
    {
        if (length < 1)
        {
            throw FontFormatException.Create(
                "A Unicode data section is empty.");
        }

        int kind = bytes[offset];

        if (kind == 1)
        {
            return ParseEnumeratedSection(tag, offset, length);
        }

        if (kind == 2)
        {
            return ParseMappingSection(tag, offset, length);
        }

        if (kind == 3)
        {
            return ParseSequenceSection(tag, offset, length);
        }

        throw FontFormatException.Create(
            "A Unicode data section kind is not supported.");
    }

    UnicodeDataSection ParseEnumeratedSection(
        int tag,
        int offset,
        int length)
    {
        if (length < 16 || bytes[offset + 1] != 0 ||
            ReadUInt16(bytes, offset + 6) != 0)
        {
            throw FontFormatException.Create(
                "An enumerated Unicode data section header is invalid.");
        }

        int valueCount = ReadUInt16(bytes, offset + 2);
        int defaultValue = ReadUInt16(bytes, offset + 4);
        long rangeCountValue = ReadUInt32(bytes, offset + 8);
        long nameLengthValue = ReadUInt32(bytes, offset + 12);

        if (valueCount == 0 || defaultValue >= valueCount ||
            rangeCountValue > int.MaxValue ||
            nameLengthValue > int.MaxValue)
        {
            throw FontFormatException.Create(
                "An enumerated Unicode data section count is invalid.");
        }

        int rangeCount = (int)rangeCountValue;
        int nameLength = (int)nameLengthValue;
        long offsetsEndValue = 16L + (valueCount + 1L) * 4L;
        long namesEndValue = offsetsEndValue + nameLength;
        long rangesOffsetValue = AlignFour(namesEndValue);
        long expectedLength = rangesOffsetValue + rangeCount * 12L;

        if (expectedLength != length)
        {
            throw FontFormatException.Create(
                "An enumerated Unicode data section length is invalid.");
        }

        int offsetsStart = offset + 16;
        int namesStart = offset + (int)offsetsEndValue;
        string[] names = new string[valueCount];
        int previousNameOffset = 0;
        int nameIndex = 0;
        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, false);

        while (nameIndex < valueCount)
        {
            int start = ReadBoundedUInt32(
                bytes,
                offsetsStart + nameIndex * 4,
                nameLength,
                "A Unicode property name offset is invalid.");
            int end = ReadBoundedUInt32(
                bytes,
                offsetsStart + (nameIndex + 1) * 4,
                nameLength,
                "A Unicode property name offset is invalid.");

            if (start < previousNameOffset || end < start)
            {
                throw FontFormatException.Create(
                    "Unicode property names are not ordered.");
            }

            ValidateUtf8(bytes, namesStart + start, end - start);
            names[nameIndex] = encoding.GetString(
                bytes,
                namesStart + start,
                end - start);

            previousNameOffset = end;
            nameIndex += 1;
        }

        if (previousNameOffset != nameLength)
        {
            throw FontFormatException.Create(
                "The Unicode property name pool length is invalid.");
        }

        int[] starts = new int[rangeCount];
        int[] ends = new int[rangeCount];
        int[] values = new int[rangeCount];
        int rangesOffset = offset + (int)rangesOffsetValue;
        int previousRangeEnd = -1;
        int rangeIndex = 0;

        while (rangeIndex < rangeCount)
        {
            int recordOffset = rangesOffset + rangeIndex * 12;
            int start = ReadCodePoint(bytes, recordOffset);
            int end = ReadCodePoint(bytes, recordOffset + 4);
            int value = ReadUInt16(bytes, recordOffset + 8);

            if (start > end || start <= previousRangeEnd ||
                value >= valueCount ||
                ReadUInt16(bytes, recordOffset + 10) != 0)
            {
                throw FontFormatException.Create(
                    "A Unicode property range is invalid.");
            }

            starts[rangeIndex] = start;
            ends[rangeIndex] = end;
            values[rangeIndex] = value;
            previousRangeEnd = end;
            rangeIndex += 1;
        }

        return CreateSection(
            tag,
            1,
            defaultValue,
            names,
            starts,
            ends,
            values,
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }

    UnicodeDataSection ParseMappingSection(
        int tag,
        int offset,
        int length)
    {
        if (length < 8 || bytes[offset + 1] != 0 ||
            ReadUInt16(bytes, offset + 2) != 0)
        {
            throw FontFormatException.Create(
                "A mapped Unicode data section header is invalid.");
        }

        long countValue = ReadUInt32(bytes, offset + 4);

        if (countValue > int.MaxValue || 8L + countValue * 12L != length)
        {
            throw FontFormatException.Create(
                "A mapped Unicode data section length is invalid.");
        }

        int count = (int)countValue;
        int[] keys = new int[count];
        int[] values = new int[count];
        int[] auxiliary = new int[count];
        int previousKey = -1;
        int index = 0;

        while (index < count)
        {
            int recordOffset = offset + 8 + index * 12;
            int key = ReadCodePoint(bytes, recordOffset);
            int value = ReadCodePoint(bytes, recordOffset + 4);
            long auxiliaryValue = ReadUInt32(bytes, recordOffset + 8);

            if (key <= previousKey || auxiliaryValue > int.MaxValue)
            {
                throw FontFormatException.Create(
                    "A mapped Unicode data record is invalid.");
            }

            keys[index] = key;
            values[index] = value;
            auxiliary[index] = (int)auxiliaryValue;
            previousKey = key;
            index += 1;
        }

        return CreateSection(
            tag,
            2,
            0,
            new string[0],
            new int[0],
            new int[0],
            new int[0],
            keys,
            values,
            auxiliary,
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }

    UnicodeDataSection ParseSequenceSection(
        int tag,
        int offset,
        int length)
    {
        if (length < 12 || bytes[offset + 1] != 0 ||
            ReadUInt16(bytes, offset + 2) != 0)
        {
            throw FontFormatException.Create(
                "A sequence Unicode data section header is invalid.");
        }

        long countValue = ReadUInt32(bytes, offset + 4);
        long poolCountValue = ReadUInt32(bytes, offset + 8);

        if (countValue > int.MaxValue || poolCountValue > int.MaxValue ||
            12L + countValue * 12L + poolCountValue * 4L != length)
        {
            throw FontFormatException.Create(
                "A sequence Unicode data section length is invalid.");
        }

        int count = (int)countValue;
        int poolCount = (int)poolCountValue;
        int[] keys = new int[count];
        int[] offsets = new int[count];
        int[] lengths = new int[count];
        int previousKey = -1;
        int index = 0;

        while (index < count)
        {
            int recordOffset = offset + 12 + index * 12;
            int key = ReadCodePoint(bytes, recordOffset);
            long sequenceOffset = ReadUInt32(bytes, recordOffset + 4);
            int sequenceLength = ReadUInt16(bytes, recordOffset + 8);

            if (key <= previousKey ||
                sequenceOffset > poolCount ||
                sequenceLength > poolCount - sequenceOffset ||
                ReadUInt16(bytes, recordOffset + 10) != 0)
            {
                throw FontFormatException.Create(
                    "A Unicode sequence record is invalid.");
            }

            keys[index] = key;
            offsets[index] = (int)sequenceOffset;
            lengths[index] = sequenceLength;
            previousKey = key;
            index += 1;
        }

        int[] pool = new int[poolCount];
        int poolOffset = offset + 12 + count * 12;
        index = 0;

        while (index < poolCount)
        {
            pool[index] = ReadCodePoint(bytes, poolOffset + index * 4);
            index += 1;
        }

        return CreateSection(
            tag,
            3,
            0,
            new string[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            keys,
            offsets,
            lengths,
            pool);
    }

    static UnicodeDataSection CreateSection(
        int tag,
        int kind,
        int defaultValue,
        string[] names,
        int[] rangeStarts,
        int[] rangeEnds,
        int[] rangeValues,
        int[] mappingKeys,
        int[] mappingValues,
        int[] mappingAuxiliary,
        int[] sequenceKeys,
        int[] sequenceOffsets,
        int[] sequenceLengths,
        int[] sequencePool)
    {
        return new UnicodeDataSection(
            tag,
            kind,
            defaultValue,
            names,
            rangeStarts,
            rangeEnds,
            rangeValues,
            mappingKeys,
            mappingValues,
            mappingAuxiliary,
            sequenceKeys,
            sequenceOffsets,
            sequenceLengths,
            sequencePool);
    }

    UnicodeDataSection RequireSection(int tag)
    {
        UnicodeDataSection? section = FindSection(tag);

        if (section == null)
        {
            throw new System.ArgumentException(
                "The Unicode data section is unavailable.",
                "tag");
        }

        return section;
    }

    UnicodeDataSection? FindSection(int tag)
    {
        int low = 0;
        System.ArgumentNullException.ThrowIfNull(sections);
        int high = sections.Length - 1;
        uint target = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                sections[middle].GetTag());

            if (candidate == target)
            {
                return sections[middle];
            }

            if (candidate < target)
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

    static int ReadUInt16(byte[] data, int offset)
    {
        EnsureAvailable(data, offset, 2);
        return data[offset] << 8 | data[offset + 1];
    }

    static long ReadUInt32(byte[] data, int offset)
    {
        EnsureAvailable(data, offset, 4);
        return (long)data[offset] << 24 |
            (long)data[offset + 1] << 16 |
            (long)data[offset + 2] << 8 |
            data[offset + 3];
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return CheckedFontMath.ReinterpretUInt32AsInt32(
            (uint)ReadUInt32(data, offset));
    }

    static int ReadCodePoint(byte[] data, int offset)
    {
        long value = ReadUInt32(data, offset);

        if (value > MaximumCodePoint)
        {
            throw FontFormatException.Create(
                "A Unicode data code point is invalid.");
        }

        return (int)value;
    }

    static int ReadBoundedUInt32(
        byte[] data,
        int offset,
        int maximum,
        string message)
    {
        long value = ReadUInt32(data, offset);

        if (value > maximum)
        {
            throw FontFormatException.Create(message);
        }

        return (int)value;
    }

    static void EnsureAvailable(byte[] data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > data.Length - length)
        {
            throw FontFormatException.Create(
                "The Unicode data artifact is truncated.");
        }
    }

    static void ValidateUtf8(byte[] data, int offset, int length)
    {
        EnsureAvailable(data, offset, length);
        int end = offset + length;
        int index = offset;

        while (index < end)
        {
            int first = data[index];

            if (first <= 0x7F)
            {
                index += 1;
                continue;
            }

            if (first >= 0xC2 && first <= 0xDF)
            {
                RequireContinuation(data, index + 1, end);
                index += 2;
                continue;
            }

            if (first >= 0xE0 && first <= 0xEF)
            {
                RequireContinuation(data, index + 1, end);
                RequireContinuation(data, index + 2, end);
                int second = data[index + 1];

                if ((first == 0xE0 && second < 0xA0) ||
                    (first == 0xED && second >= 0xA0))
                {
                    throw FontFormatException.Create(
                        "A Unicode property name is not valid UTF-8.");
                }

                index += 3;
                continue;
            }

            if (first >= 0xF0 && first <= 0xF4)
            {
                RequireContinuation(data, index + 1, end);
                RequireContinuation(data, index + 2, end);
                RequireContinuation(data, index + 3, end);
                int second = data[index + 1];

                if ((first == 0xF0 && second < 0x90) ||
                    (first == 0xF4 && second >= 0x90))
                {
                    throw FontFormatException.Create(
                        "A Unicode property name is not valid UTF-8.");
                }

                index += 4;
                continue;
            }

            throw FontFormatException.Create(
                "A Unicode property name is not valid UTF-8.");
        }
    }

    static void RequireContinuation(byte[] data, int index, int end)
    {
        if (index >= end || (data[index] & 0xC0) != 0x80)
        {
            throw FontFormatException.Create(
                "A Unicode property name is not valid UTF-8.");
        }
    }

    static int AlignFour(int value)
    {
        if (value > int.MaxValue - 3)
        {
            throw FontFormatException.Create(
                "The Unicode data artifact is too large.");
        }

        return value + 3 & ~3;
    }

    static long AlignFour(long value)
    {
        if (value > long.MaxValue - 3)
        {
            throw FontFormatException.Create(
                "The Unicode data artifact is too large.");
        }

        return value + 3 & ~3L;
    }

    static void ValidateCodePoint(int codePoint)
    {
        if (codePoint < 0 || codePoint > MaximumCodePoint)
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }
    }
}
