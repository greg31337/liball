namespace ttf;

internal enum KhmerSyllabicCategory
{
    Other = 0,
    Consonant = 1,
    Vowel = 2,
    Coeng = 3,
    NonJoiner = 4,
    Joiner = 5,
    Placeholder = 6,
    DottedCircle = 7,
    Ra = 8,
    VowelAbove = 9,
    VowelBelow = 10,
    VowelPre = 11,
    VowelPost = 12,
    Robatic = 13,
    XGroup = 14,
    YGroup = 15
}

internal enum KhmerSyllableType
{
    Consonant = 0,
    Broken = 1,
    NonKhmer = 2
}

internal class KhmerSyllableMap
{
    KhmerSyllabicCategory[] categories;
    int[] runeSyllableIndexes;
    int[] starts;
    int[] lengths;
    KhmerSyllableType[] types;
    bool hasBrokenSyllable;

    internal KhmerSyllableMap(
        KhmerSyllabicCategory[] categories,
        int[] runeSyllableIndexes,
        int[] starts,
        int[] lengths,
        KhmerSyllableType[] types,
        bool hasBrokenSyllable)
    {
        this.categories = categories;
        this.runeSyllableIndexes = runeSyllableIndexes;
        this.starts = starts;
        this.lengths = lengths;
        this.types = types;
        this.hasBrokenSyllable = hasBrokenSyllable;
    }

    internal int GetRuneCount()
    {
        return categories.Length;
    }

    internal KhmerSyllabicCategory GetCategory(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return categories[runeIndex];
    }

    internal int GetRuneSyllableIndex(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return runeSyllableIndexes[runeIndex];
    }

    internal int GetSyllableCount()
    {
        return types.Length;
    }

    internal int GetSyllableStart(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return starts[syllableIndex];
    }

    internal int GetSyllableLength(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return lengths[syllableIndex];
    }

    internal int GetSyllableEnd(int syllableIndex)
    {
        return GetSyllableStart(syllableIndex) +
            GetSyllableLength(syllableIndex);
    }

    internal KhmerSyllableType GetSyllableType(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return types[syllableIndex];
    }

    internal bool HasBrokenSyllable()
    {
        return hasBrokenSyllable;
    }

    void ValidateRuneIndex(int runeIndex)
    {
        if (runeIndex < 0 || runeIndex >= categories.Length)
        {
            throw new System.ArgumentOutOfRangeException("runeIndex");
        }
    }

    void ValidateSyllableIndex(int syllableIndex)
    {
        if (syllableIndex < 0 || syllableIndex >= types.Length)
        {
            throw new System.ArgumentOutOfRangeException("syllableIndex");
        }
    }
}

internal static class KhmerSyllableRecognizer
{
    const string SyllableLimitMessage =
        "Khmer syllable recognition exceeds the configured syllable limit.";

    internal static KhmerSyllableMap Recognize(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (maximumSyllableCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSyllableCount");
        }

        int runeCount = buffer.GetCount();
        KhmerSyllabicCategory[] categories =
            new KhmerSyllabicCategory[runeCount];
        LoadCategories(buffer, unicodeData, categories);
        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<KhmerSyllableType> types =
            new System.Collections.Generic.List<KhmerSyllableType>();
        int[] runeSyllableIndexes = new int[runeCount];
        bool hasBroken = false;
        int index = 0;

        while (index < runeCount)
        {
            int end = -1;
            KhmerSyllableType type = KhmerSyllableType.NonKhmer;
            int candidateEnd = ParseConsonantSyllable(categories, index);
            if (candidateEnd > end)
            {
                end = candidateEnd;
                type = KhmerSyllableType.Consonant;
            }

            candidateEnd = ParseBrokenCluster(categories, index);
            if (candidateEnd > end)
            {
                end = candidateEnd;
                type = KhmerSyllableType.Broken;
            }

            if (end <= index)
            {
                end = index + 1;
                type = KhmerSyllableType.NonKhmer;
            }

            if (types.Count >= maximumSyllableCount)
            {
                throw FontLimitException.Create(SyllableLimitMessage);
            }

            int syllableIndex = types.Count;
            starts.Add(index);
            lengths.Add(end - index);
            types.Add(type);
            int runeIndex = index;
            while (runeIndex < end)
            {
                runeSyllableIndexes[runeIndex] = syllableIndex;
                runeIndex++;
            }

            if (type == KhmerSyllableType.Broken)
            {
                hasBroken = true;
            }

            index = end;
        }

        ApplySyllables(buffer, starts, lengths, types);
        return new KhmerSyllableMap(
            categories,
            runeSyllableIndexes,
            CopyIntegers(starts),
            CopyIntegers(lengths),
            CopyTypes(types),
            hasBroken);
    }

    static void LoadCategories(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        KhmerSyllabicCategory[] categories)
    {
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            string syllabicName = unicodeData.GetPropertyName(
                UnicodeDataTags.IndicSyllabicCategory,
                codePoint);
            string positionalName = unicodeData.GetPropertyName(
                UnicodeDataTags.IndicPositionalCategory,
                codePoint);
            KhmerSyllabicCategory category = ConvertCategory(
                codePoint,
                syllabicName,
                positionalName);
            if (!IsKhmerShapingCodePoint(codePoint))
            {
                category = KhmerSyllabicCategory.Other;
            }

            categories[index] = category;
            index++;
        }
    }

    static KhmerSyllabicCategory ConvertCategory(
        int codePoint,
        string syllabicName,
        string positionalName)
    {
        if (codePoint == 0x179A)
        {
            return KhmerSyllabicCategory.Ra;
        }

        if (codePoint == 0x17CC || codePoint == 0x17C9 ||
            codePoint == 0x17CA)
        {
            return KhmerSyllabicCategory.Robatic;
        }

        if (codePoint == 0x17C6 || codePoint == 0x17CB ||
            (codePoint >= 0x17CD && codePoint <= 0x17D1))
        {
            return KhmerSyllabicCategory.XGroup;
        }

        if (codePoint == 0x17C7 || codePoint == 0x17C8 ||
            codePoint == 0x17DD || codePoint == 0x17D3)
        {
            return KhmerSyllabicCategory.YGroup;
        }

        if (codePoint == 0x17D9)
        {
            return KhmerSyllabicCategory.Placeholder;
        }

        if (codePoint == 0x25CC)
        {
            return KhmerSyllabicCategory.DottedCircle;
        }

        if (syllabicName == "Consonant" ||
            syllabicName == "Consonant_Dead" ||
            syllabicName == "Consonant_Head_Letter")
        {
            return KhmerSyllabicCategory.Consonant;
        }

        if (syllabicName == "Vowel" ||
            syllabicName == "Vowel_Independent")
        {
            return KhmerSyllabicCategory.Vowel;
        }

        if (syllabicName == "Invisible_Stacker")
        {
            return KhmerSyllabicCategory.Coeng;
        }

        if (syllabicName == "Non_Joiner")
        {
            return KhmerSyllabicCategory.NonJoiner;
        }

        if (syllabicName == "Joiner")
        {
            return KhmerSyllabicCategory.Joiner;
        }

        if (syllabicName == "Consonant_Placeholder")
        {
            return KhmerSyllabicCategory.Placeholder;
        }

        if (syllabicName == "Vowel_Dependent")
        {
            return ConvertVowelPosition(positionalName);
        }

        return KhmerSyllabicCategory.Other;
    }

    static KhmerSyllabicCategory ConvertVowelPosition(string value)
    {
        if (value == "Left" || value == "Visual_order_left" ||
            value == "Visual_Order_Left")
        {
            return KhmerSyllabicCategory.VowelPre;
        }

        if (value == "Bottom" || value == "Top_And_Bottom" ||
            value == "Top_And_Bottom_And_Left")
        {
            return KhmerSyllabicCategory.VowelBelow;
        }

        if (value == "Right" || value == "Bottom_And_Right" ||
            value == "Left_And_Right" ||
            value == "Top_And_Bottom_And_Right" ||
            value == "Top_And_Left_And_Right" ||
            value == "Top_And_Right")
        {
            return KhmerSyllabicCategory.VowelPost;
        }

        return KhmerSyllabicCategory.VowelAbove;
    }

    static bool IsKhmerShapingCodePoint(int codePoint)
    {
        return (codePoint >= 0x1780 && codePoint <= 0x17FF) ||
            codePoint == 0x200C || codePoint == 0x200D ||
            codePoint == 0x25CC;
    }

    static int ParseConsonantSyllable(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int prefixEnd = ParseConsonantUnit(categories, start);
        if (prefixEnd < 0 &&
            !IsCategory(
                categories,
                start,
                KhmerSyllabicCategory.Placeholder) &&
            !IsCategory(
                categories,
                start,
                KhmerSyllabicCategory.DottedCircle))
        {
            return -1;
        }

        if (prefixEnd < 0)
        {
            prefixEnd = start + 1;
        }

        return ParseBrokenTail(categories, prefixEnd);
    }

    static int ParseBrokenCluster(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int end = ParseBrokenTail(categories, start);
        return end > start ? end : -1;
    }

    static int ParseBrokenTail(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsCategory(categories, index, KhmerSyllabicCategory.Robatic))
        {
            index++;
        }

        while (IsCategory(categories, index, KhmerSyllabicCategory.Coeng))
        {
            int consonantEnd = ParseConsonantUnit(categories, index + 1);
            if (consonantEnd < 0)
            {
                break;
            }

            index = consonantEnd;
        }

        int finalCoengEnd = IsCategory(
            categories,
            index,
            KhmerSyllabicCategory.Coeng) ? index + 1 : index;
        int tailEnd = ParseSyllableTail(categories, index);
        return tailEnd > finalCoengEnd ? tailEnd : finalCoengEnd;
    }

    static int ParseConsonantUnit(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        if (!IsConsonant(categories, start))
        {
            return -1;
        }

        int index = start + 1;
        int modifierStart = index;
        if (IsJoiner(categories, index))
        {
            index++;
        }

        if (IsCategory(categories, index, KhmerSyllabicCategory.Robatic))
        {
            return index + 1;
        }

        return modifierStart;
    }

    static int ParseSyllableTail(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int index = ParseXGroup(categories, start);
        index = ParseMatraGroup(categories, index);
        index = ParseXGroup(categories, index);
        if (IsCategory(categories, index, KhmerSyllabicCategory.Coeng) &&
            IsConsonant(categories, index + 1))
        {
            index += 2;
        }

        while (IsCategory(categories, index, KhmerSyllabicCategory.YGroup))
        {
            index++;
        }

        return index;
    }

    static int ParseMatraGroup(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsCategory(categories, index, KhmerSyllabicCategory.VowelPre))
        {
            index++;
        }

        index = ParseXGroup(categories, index);
        if (IsCategory(categories, index, KhmerSyllabicCategory.VowelBelow))
        {
            index++;
        }

        index = ParseXGroup(categories, index);
        int aboveStart = index;
        if (IsJoiner(categories, index))
        {
            index++;
        }

        if (IsCategory(categories, index, KhmerSyllabicCategory.VowelAbove))
        {
            index++;
        }
        else
        {
            index = aboveStart;
        }

        index = ParseXGroup(categories, index);
        if (IsCategory(categories, index, KhmerSyllabicCategory.VowelPost))
        {
            index++;
        }

        return index;
    }

    static int ParseXGroup(
        KhmerSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        while (true)
        {
            int groupStart = index;
            while (IsJoiner(categories, index))
            {
                index++;
            }

            if (!IsCategory(categories, index, KhmerSyllabicCategory.XGroup))
            {
                index = groupStart;
                break;
            }

            index++;
        }

        return index;
    }

    static bool IsConsonant(
        KhmerSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(categories, index, KhmerSyllabicCategory.Consonant) ||
            IsCategory(categories, index, KhmerSyllabicCategory.Ra) ||
            IsCategory(categories, index, KhmerSyllabicCategory.Vowel);
    }

    static bool IsJoiner(
        KhmerSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(categories, index, KhmerSyllabicCategory.Joiner) ||
            IsCategory(categories, index, KhmerSyllabicCategory.NonJoiner);
    }

    static bool IsCategory(
        KhmerSyllabicCategory[] categories,
        int index,
        KhmerSyllabicCategory category)
    {
        return index >= 0 && index < categories.Length &&
            categories[index] == category;
    }

    static void ApplySyllables(
        ShapingBuffer buffer,
        System.Collections.Generic.List<int> starts,
        System.Collections.Generic.List<int> lengths,
        System.Collections.Generic.List<KhmerSyllableType> types)
    {
        int serial = 1;
        int syllableIndex = 0;
        while (syllableIndex < types.Count)
        {
            int value = serial * 16 + (int)types[syllableIndex];
            int index = starts[syllableIndex];
            int end = index + lengths[syllableIndex];
            while (index < end)
            {
                buffer.SetSyllable(index, value);
                index++;
            }

            serial++;
            if (serial == 16)
            {
                serial = 1;
            }

            syllableIndex++;
        }
    }

    static int[] CopyIntegers(
        System.Collections.Generic.List<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static KhmerSyllableType[] CopyTypes(
        System.Collections.Generic.List<KhmerSyllableType> values)
    {
        KhmerSyllableType[] result =
            new KhmerSyllableType[values.Count];
        values.CopyTo(result, 0);
        return result;
    }
}
