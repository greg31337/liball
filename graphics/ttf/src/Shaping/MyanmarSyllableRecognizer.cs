namespace ttf;

internal enum MyanmarSyllabicCategory
{
    Other = 0,
    Consonant = 1,
    IndependentVowel = 2,
    DotBelow = 3,
    Halant = 4,
    NonJoiner = 5,
    Joiner = 6,
    SyllableModifier = 7,
    Placeholder = 8,
    DottedCircle = 9,
    A = 10,
    Ra = 11,
    ConsonantWithStacker = 12,
    SyllableModifierPost = 13,
    VowelAbove = 14,
    VowelBelow = 15,
    VowelPre = 16,
    VowelPost = 17,
    Asat = 18,
    MedialHa = 19,
    MedialRa = 20,
    MedialWa = 21,
    MedialYa = 22,
    PwoTone = 23,
    VariationSelector = 24,
    MedialLa = 25
}

internal enum MyanmarSyllableType
{
    Consonant = 0,
    Broken = 1,
    NonMyanmar = 2
}

internal class MyanmarSyllableMap
{
    MyanmarSyllabicCategory[] categories;
    int[] runeSyllableIndexes;
    int[] starts;
    int[] lengths;
    MyanmarSyllableType[] types;
    bool hasBrokenSyllable;

    internal MyanmarSyllableMap(
        MyanmarSyllabicCategory[] categories,
        int[] runeSyllableIndexes,
        int[] starts,
        int[] lengths,
        MyanmarSyllableType[] types,
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

    internal MyanmarSyllabicCategory GetCategory(int runeIndex)
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

    internal MyanmarSyllableType GetSyllableType(int syllableIndex)
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

internal static class MyanmarSyllableRecognizer
{
    const string SyllableLimitMessage =
        "Myanmar syllable recognition exceeds the configured syllable limit.";

    internal static MyanmarSyllableMap Recognize(
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
        MyanmarSyllabicCategory[] categories =
            new MyanmarSyllabicCategory[runeCount];
        LoadCategories(buffer, unicodeData, categories);
        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<MyanmarSyllableType> types =
            new System.Collections.Generic.List<MyanmarSyllableType>();
        int[] runeSyllableIndexes = new int[runeCount];
        bool hasBroken = false;
        int index = 0;

        while (index < runeCount)
        {
            int end = ParseConsonantSyllable(categories, index);
            MyanmarSyllableType type = MyanmarSyllableType.Consonant;
            if (end <= index)
            {
                int brokenEnd = ParseBrokenCluster(categories, index);
                bool standalone = IsJoiner(categories, index) ||
                    IsCategory(
                        categories,
                        index,
                        MyanmarSyllabicCategory.SyllableModifierPost);
                if (standalone && brokenEnd <= index + 1)
                {
                    end = index + 1;
                    type = MyanmarSyllableType.NonMyanmar;
                }
                else
                {
                    end = brokenEnd;
                    type = MyanmarSyllableType.Broken;
                }
            }

            if (end <= index)
            {
                end = index + 1;
                type = MyanmarSyllableType.NonMyanmar;
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

            if (type == MyanmarSyllableType.Broken)
            {
                hasBroken = true;
            }

            index = end;
        }

        ApplySyllables(buffer, starts, lengths, types);
        return new MyanmarSyllableMap(
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
        MyanmarSyllabicCategory[] categories)
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
            MyanmarSyllabicCategory category = ConvertCategory(
                codePoint,
                syllabicName,
                positionalName);
            if (!IsMyanmarShapingCodePoint(codePoint))
            {
                category = MyanmarSyllabicCategory.Other;
            }

            categories[index] = category;
            index++;
        }
    }

    static MyanmarSyllabicCategory ConvertCategory(
        int codePoint,
        string syllabicName,
        string positionalName)
    {
        MyanmarSyllabicCategory special = ConvertSpecialCategory(codePoint);
        if (special != MyanmarSyllabicCategory.Other)
        {
            return special;
        }

        if (syllabicName == "Consonant" ||
            syllabicName == "Consonant_Dead" ||
            syllabicName == "Consonant_Head_Letter" ||
            syllabicName == "Consonant_Initial_Postfixed")
        {
            return MyanmarSyllabicCategory.Consonant;
        }

        if (syllabicName == "Vowel" ||
            syllabicName == "Vowel_Independent")
        {
            return MyanmarSyllabicCategory.IndependentVowel;
        }

        if (syllabicName == "Nukta" || syllabicName == "Tone_Mark")
        {
            return MyanmarSyllabicCategory.DotBelow;
        }

        if (syllabicName == "Invisible_Stacker" ||
            syllabicName == "Virama")
        {
            return MyanmarSyllabicCategory.Halant;
        }

        if (syllabicName == "Non_Joiner")
        {
            return MyanmarSyllabicCategory.NonJoiner;
        }

        if (syllabicName == "Joiner")
        {
            return MyanmarSyllabicCategory.Joiner;
        }

        if (syllabicName == "Consonant_With_Stacker")
        {
            return MyanmarSyllabicCategory.ConsonantWithStacker;
        }

        if (IsPlaceholderCategory(syllabicName))
        {
            return MyanmarSyllabicCategory.Placeholder;
        }

        if (syllabicName == "Cantillation_Mark")
        {
            return MyanmarSyllabicCategory.A;
        }

        if (syllabicName == "Bindu" ||
            syllabicName == "Gemination_Mark" ||
            syllabicName == "Syllable_Modifier" ||
            syllabicName == "Visarga")
        {
            if (syllabicName == "Syllable_Modifier" &&
                positionalName == "Not_Applicable")
            {
                return MyanmarSyllabicCategory.SyllableModifierPost;
            }

            return MyanmarSyllabicCategory.SyllableModifier;
        }

        if (syllabicName == "Vowel_Dependent" ||
            syllabicName == "Pure_Killer")
        {
            return ConvertVowelPosition(positionalName);
        }

        return MyanmarSyllabicCategory.Other;
    }

    static MyanmarSyllabicCategory ConvertSpecialCategory(int codePoint)
    {
        if (codePoint >= 0xFE00 && codePoint <= 0xFE0F)
        {
            return MyanmarSyllabicCategory.VariationSelector;
        }

        if (codePoint == 0x25CC)
        {
            return MyanmarSyllabicCategory.DottedCircle;
        }

        if (codePoint == 0x2015 || codePoint == 0x2022 ||
            (codePoint >= 0x25FB && codePoint <= 0x25FE) ||
            codePoint == 0x104A)
        {
            return MyanmarSyllabicCategory.Placeholder;
        }

        if (codePoint == 0x104E)
        {
            return MyanmarSyllabicCategory.Consonant;
        }

        if (codePoint == 0x1004 || codePoint == 0x101B ||
            codePoint == 0x105A)
        {
            return MyanmarSyllabicCategory.Ra;
        }

        if (codePoint == 0x1032 || codePoint == 0x1036)
        {
            return MyanmarSyllabicCategory.A;
        }

        if (codePoint == 0x103A)
        {
            return MyanmarSyllabicCategory.Asat;
        }

        if (codePoint == 0x103E)
        {
            return MyanmarSyllabicCategory.MedialHa;
        }

        if (codePoint == 0x1060)
        {
            return MyanmarSyllabicCategory.MedialLa;
        }

        if (codePoint == 0x103C)
        {
            return MyanmarSyllabicCategory.MedialRa;
        }

        if (codePoint == 0x103D || codePoint == 0x1082)
        {
            return MyanmarSyllabicCategory.MedialWa;
        }

        if (codePoint == 0x103B || codePoint == 0x105E ||
            codePoint == 0x105F)
        {
            return MyanmarSyllabicCategory.MedialYa;
        }

        if (IsPwoTone(codePoint))
        {
            return MyanmarSyllabicCategory.PwoTone;
        }

        if (IsSyllableModifierOverride(codePoint))
        {
            return MyanmarSyllabicCategory.SyllableModifier;
        }

        return MyanmarSyllabicCategory.Other;
    }

    static bool IsPwoTone(int codePoint)
    {
        return codePoint == 0x1063 || codePoint == 0x1064 ||
            (codePoint >= 0x1069 && codePoint <= 0x106D) ||
            codePoint == 0xAA7B;
    }

    static bool IsSyllableModifierOverride(int codePoint)
    {
        return codePoint == 0x1038 ||
            (codePoint >= 0x1087 && codePoint <= 0x108D) ||
            codePoint == 0x108F ||
            (codePoint >= 0x109A && codePoint <= 0x109C);
    }

    static bool IsPlaceholderCategory(string value)
    {
        return value == "Brahmi_Joining_Number" ||
            value == "Consonant_Placeholder" || value == "Number" ||
            value == "Number_Joiner";
    }

    static MyanmarSyllabicCategory ConvertVowelPosition(string value)
    {
        if (value == "Left" || value == "Visual_Order_Left" ||
            value == "Visual_order_left")
        {
            return MyanmarSyllabicCategory.VowelPre;
        }

        if (value == "Bottom" || value == "Top_And_Bottom" ||
            value == "Top_And_Bottom_And_Left")
        {
            return MyanmarSyllabicCategory.VowelBelow;
        }

        if (value == "Right" || value == "Bottom_And_Right" ||
            value == "Left_And_Right" ||
            value == "Top_And_Bottom_And_Right" ||
            value == "Top_And_Left_And_Right" ||
            value == "Top_And_Right")
        {
            return MyanmarSyllabicCategory.VowelPost;
        }

        return MyanmarSyllabicCategory.VowelAbove;
    }

    static bool IsMyanmarShapingCodePoint(int codePoint)
    {
        return (codePoint >= 0x1000 && codePoint <= 0x109F) ||
            (codePoint >= 0xA9E0 && codePoint <= 0xA9FF) ||
            (codePoint >= 0xAA60 && codePoint <= 0xAA7F) ||
            (codePoint >= 0x116D0 && codePoint <= 0x116FF) ||
            codePoint == 0x200C || codePoint == 0x200D ||
            codePoint == 0x2015 || codePoint == 0x2022 ||
            codePoint == 0x25CC ||
            (codePoint >= 0x25FB && codePoint <= 0x25FE) ||
            (codePoint >= 0xFE00 && codePoint <= 0xFE0F);
    }

    static int ParseConsonantSyllable(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = ParseKinzi(categories, start);
        if (index == start && IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.ConsonantWithStacker))
        {
            index++;
        }

        if (!IsBase(categories, index))
        {
            return -1;
        }

        index++;
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VariationSelector))
        {
            index++;
        }

        return ParseSyllableTail(categories, index);
    }

    static int ParseBrokenCluster(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = ParseKinzi(categories, start);
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VariationSelector))
        {
            index++;
        }

        index = ParseSyllableTail(categories, index);
        return index > start ? index : -1;
    }

    static int ParseKinzi(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        if (IsCategory(categories, start, MyanmarSyllabicCategory.Ra) &&
            IsCategory(
                categories,
                start + 1,
                MyanmarSyllabicCategory.Asat) &&
            IsCategory(
                categories,
                start + 2,
                MyanmarSyllabicCategory.Halant))
        {
            return start + 3;
        }

        return start;
    }

    static int ParseSyllableTail(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        while (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Halant) &&
            IsStackedBase(categories, index + 1))
        {
            index += 2;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.VariationSelector))
            {
                index++;
            }
        }

        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Halant))
        {
            return index + 1;
        }

        return ParseComplexTail(categories, index);
    }

    static int ParseComplexTail(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = ConsumeCategory(
            categories,
            start,
            MyanmarSyllabicCategory.Asat);
        index = ParseMedialGroup(categories, index);
        index = ParseMainVowelGroup(categories, index);
        while (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VowelPost))
        {
            index = ParsePostVowelGroup(categories, index);
        }

        while (IsToneStart(categories, index))
        {
            index = ParseToneGroup(categories, index);
        }

        if (IsJoiner(categories, index))
        {
            index++;
        }

        return index;
    }

    static int ParseMedialGroup(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialYa))
        {
            index++;
        }

        if (IsCategory(categories, index, MyanmarSyllabicCategory.Asat))
        {
            index++;
        }

        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialRa))
        {
            index++;
        }

        bool matched = false;
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialWa))
        {
            index++;
            matched = true;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.MedialHa))
            {
                index++;
            }

            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.MedialLa))
            {
                index++;
            }
        }
        else if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialHa))
        {
            index++;
            matched = true;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.MedialLa))
            {
                index++;
            }
        }
        else if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialLa))
        {
            index++;
            matched = true;
        }

        if (matched && IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Asat))
        {
            index++;
        }

        return index;
    }

    static int ParseMainVowelGroup(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        while (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VowelPre))
        {
            index++;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.VariationSelector))
            {
                index++;
            }
        }

        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VowelAbove);
        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VowelBelow);
        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.A);
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.DotBelow))
        {
            index++;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.Asat))
            {
                index++;
            }
        }

        return index;
    }

    static int ParsePostVowelGroup(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        int index = start + 1;
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialHa))
        {
            index++;
        }

        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.MedialLa))
        {
            index++;
        }

        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Asat);
        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.VowelAbove);
        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.A);
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.DotBelow))
        {
            index++;
            if (IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.Asat))
            {
                index++;
            }
        }

        return index;
    }

    static int ParseToneGroup(
        MyanmarSyllabicCategory[] categories,
        int start)
    {
        if (IsCategory(
            categories,
            start,
            MyanmarSyllabicCategory.SyllableModifier) ||
            IsCategory(
                categories,
                start,
                MyanmarSyllabicCategory.SyllableModifierPost))
        {
            return start + 1;
        }

        int index = start + 1;
        index = ConsumeCategory(
            categories,
            index,
            MyanmarSyllabicCategory.A);
        if (IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.DotBelow))
        {
            index++;
        }

        if (IsCategory(categories, index, MyanmarSyllabicCategory.Asat))
        {
            index++;
        }

        return index;
    }

    static int ConsumeCategory(
        MyanmarSyllabicCategory[] categories,
        int start,
        MyanmarSyllabicCategory category)
    {
        int index = start;
        while (IsCategory(categories, index, category))
        {
            index++;
        }

        return index;
    }

    static bool IsBase(
        MyanmarSyllabicCategory[] categories,
        int index)
    {
        return IsStackedBase(categories, index) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.Placeholder) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.DottedCircle);
    }

    static bool IsStackedBase(
        MyanmarSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Consonant) ||
            IsCategory(categories, index, MyanmarSyllabicCategory.Ra) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.IndependentVowel);
    }

    static bool IsToneStart(
        MyanmarSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.SyllableModifier) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.SyllableModifierPost) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.PwoTone);
    }

    static bool IsJoiner(
        MyanmarSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(
            categories,
            index,
            MyanmarSyllabicCategory.Joiner) ||
            IsCategory(
                categories,
                index,
                MyanmarSyllabicCategory.NonJoiner);
    }

    static bool IsCategory(
        MyanmarSyllabicCategory[] categories,
        int index,
        MyanmarSyllabicCategory category)
    {
        return index >= 0 && index < categories.Length &&
            categories[index] == category;
    }

    static void ApplySyllables(
        ShapingBuffer buffer,
        System.Collections.Generic.List<int> starts,
        System.Collections.Generic.List<int> lengths,
        System.Collections.Generic.List<MyanmarSyllableType> types)
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

    static MyanmarSyllableType[] CopyTypes(
        System.Collections.Generic.List<MyanmarSyllableType> values)
    {
        MyanmarSyllableType[] result =
            new MyanmarSyllableType[values.Count];
        values.CopyTo(result, 0);
        return result;
    }
}
