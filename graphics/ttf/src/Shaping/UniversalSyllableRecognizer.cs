namespace ttf;

internal enum UniversalCategory
{
    Other = 0,
    Base = 1,
    BaseNumber = 2,
    BaseOther = 3,
    CombiningJoiner = 4,
    ConsonantSubjoined = 5,
    Halant = 6,
    HalantNumber = 7,
    NonJoiner = 8,
    WordJoiner = 9,
    Repha = 10,
    VowelPre = 11,
    VowelModifierPre = 12,
    ConsonantFinalAbove = 13,
    ConsonantFinalBelow = 14,
    ConsonantFinalPost = 15,
    ConsonantMedialAbove = 16,
    ConsonantMedialBelow = 17,
    ConsonantMedialPost = 18,
    ConsonantMedialPre = 19,
    ConsonantModifierAbove = 20,
    ConsonantModifierBelow = 21,
    VowelAbove = 22,
    VowelBelow = 23,
    VowelPost = 24,
    VowelModifierAbove = 25,
    VowelModifierBelow = 26,
    VowelModifierPost = 27,
    SymbolModifierAbove = 28,
    SymbolModifierBelow = 29,
    ConsonantWithStacker = 30,
    InvisibleStacker = 31,
    ConsonantFinalModifierAbove = 32,
    ConsonantFinalModifierBelow = 33,
    ConsonantFinalModifierPost = 34,
    Sakot = 35,
    Hieroglyph = 36,
    HieroglyphJoiner = 37,
    HieroglyphSegmentBegin = 38,
    HieroglyphSegmentEnd = 39,
    HalantOrVowelModifier = 40,
    HieroglyphModifier = 41,
    HieroglyphMirror = 42,
    ReorderingKiller = 43
}

internal enum UniversalSyllableType
{
    ViramaTerminated = 0,
    SakotTerminated = 1,
    Standard = 2,
    NumberJoinerTerminated = 3,
    Numeral = 4,
    Symbol = 5,
    Hieroglyph = 6,
    Broken = 7,
    NonCluster = 8
}

internal class UniversalSyllableMap
{
    UniversalCategory[] categories;
    int[] runeSyllableIndexes;
    int[] starts;
    int[] lengths;
    UniversalSyllableType[] types;
    bool hasBrokenSyllable;

    internal UniversalSyllableMap(
        UniversalCategory[] categories,
        int[] runeSyllableIndexes,
        int[] starts,
        int[] lengths,
        UniversalSyllableType[] types,
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

    internal UniversalCategory GetCategory(int runeIndex)
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

    internal UniversalSyllableType GetSyllableType(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return types[syllableIndex];
    }

    internal bool HasBrokenSyllable()
    {
        return hasBrokenSyllable;
    }

    internal void SetCategory(int runeIndex, UniversalCategory category)
    {
        ValidateRuneIndex(runeIndex);
        categories[runeIndex] = category;
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

internal static class UniversalCategoryData
{
    internal static UniversalCategory GetCategory(
        UnicodeData unicodeData,
        int codePoint)
    {
        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (!System.Text.Rune.IsValid(codePoint))
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }

        string script = unicodeData.GetPropertyName(
            UnicodeDataTags.Script,
            codePoint);
        if (IsDisabledScript(script))
        {
            return UniversalCategory.Other;
        }

        string syllabic = unicodeData.GetPropertyName(
            UnicodeDataTags.IndicSyllabicCategory,
            codePoint);
        string positional = unicodeData.GetPropertyName(
            UnicodeDataTags.IndicPositionalCategory,
            codePoint);
        syllabic = ApplySyllabicOverride(codePoint, syllabic);
        positional = ApplyPositionalOverride(codePoint, positional);
        return ResolveCategory(
            unicodeData,
            codePoint,
            syllabic,
            positional);
    }

    static UniversalCategory ResolveCategory(
        UnicodeData unicodeData,
        int codePoint,
        string syllabic,
        string positional)
    {
        System.Globalization.UnicodeCategory general =
            System.Text.Rune.GetUnicodeCategory(
                new System.Text.Rune(codePoint));
        bool letterOther = general ==
            System.Globalization.UnicodeCategory.OtherLetter;
        bool defaultIgnorable = unicodeData.GetPropertyValue(
            UnicodeDataTags.DefaultIgnorable,
            codePoint) != 0;
        string joining = unicodeData.GetPropertyName(
            UnicodeDataTags.JoiningType,
            codePoint);

        if (syllabic == "Non_Joiner")
        {
            return UniversalCategory.NonJoiner;
        }

        if (syllabic == "Joiner" ||
            (defaultIgnorable && IsMark(general)))
        {
            return UniversalCategory.CombiningJoiner;
        }

        if (IsWordJoiner(
            codePoint,
            syllabic,
            general,
            defaultIgnorable))
        {
            return UniversalCategory.WordJoiner;
        }

        if (syllabic == "Brahmi_Joining_Number")
        {
            return UniversalCategory.BaseNumber;
        }

        if (IsBaseOther(codePoint, syllabic))
        {
            return UniversalCategory.BaseOther;
        }

        if (IsBase(syllabic, joining, letterOther))
        {
            return UniversalCategory.Base;
        }

        if (syllabic == "Consonant_With_Stacker")
        {
            return UniversalCategory.ConsonantWithStacker;
        }

        if (syllabic == "Consonant_Subjoined" && !letterOther)
        {
            return UniversalCategory.ConsonantSubjoined;
        }

        if (syllabic == "Consonant_Preceding_Repha" ||
            syllabic == "Consonant_Prefixed")
        {
            return UniversalCategory.Repha;
        }

        if (syllabic == "Number_Joiner")
        {
            return UniversalCategory.HalantNumber;
        }

        if (codePoint == 0x0DCA)
        {
            return UniversalCategory.HalantOrVowelModifier;
        }

        if (codePoint == 0x1A60)
        {
            return UniversalCategory.Sakot;
        }

        if (syllabic == "Virama")
        {
            return UniversalCategory.Halant;
        }

        if (syllabic == "Invisible_Stacker")
        {
            return UniversalCategory.InvisibleStacker;
        }

        if (syllabic == "Reordering_Killer")
        {
            return UniversalCategory.ReorderingKiller;
        }

        UniversalCategory hieroglyph = ResolveHieroglyph(
            codePoint,
            syllabic);
        if (hieroglyph != UniversalCategory.Other)
        {
            return hieroglyph;
        }

        if ((syllabic == "Consonant_Final" && !letterOther) ||
            syllabic == "Consonant_Succeeding_Repha")
        {
            return ResolvePosition(
                positional,
                UniversalCategory.ConsonantFinalAbove,
                UniversalCategory.ConsonantFinalBelow,
                UniversalCategory.ConsonantFinalPost,
                UniversalCategory.ConsonantFinalPost);
        }

        if (syllabic == "Syllable_Modifier" ||
            syllabic == "Consonant_Final_Modifier")
        {
            return ResolvePosition(
                positional,
                UniversalCategory.ConsonantFinalModifierAbove,
                UniversalCategory.ConsonantFinalModifierBelow,
                UniversalCategory.ConsonantFinalModifierPost,
                UniversalCategory.ConsonantFinalModifierPost);
        }

        if ((syllabic == "Consonant_Medial" && !letterOther) ||
            syllabic == "Consonant_Initial_Postfixed")
        {
            return ResolvePosition(
                positional,
                UniversalCategory.ConsonantMedialAbove,
                UniversalCategory.ConsonantMedialBelow,
                UniversalCategory.ConsonantMedialPost,
                UniversalCategory.ConsonantMedialPre);
        }

        if (syllabic == "Nukta" ||
            syllabic == "Gemination_Mark" ||
            syllabic == "Consonant_Killer")
        {
            return ResolvePosition(
                positional,
                UniversalCategory.ConsonantModifierAbove,
                UniversalCategory.ConsonantModifierBelow,
                UniversalCategory.ConsonantModifierAbove,
                UniversalCategory.ConsonantModifierAbove);
        }

        if (syllabic == "Symbol_Modifier")
        {
            return IsBelow(positional) ?
                UniversalCategory.SymbolModifierBelow :
                UniversalCategory.SymbolModifierAbove;
        }

        if (IsVowelCategory(syllabic, letterOther))
        {
            return ResolvePosition(
                positional,
                UniversalCategory.VowelAbove,
                UniversalCategory.VowelBelow,
                UniversalCategory.VowelPost,
                UniversalCategory.VowelPre);
        }

        if (IsVowelModifierCategory(syllabic, letterOther))
        {
            return ResolvePosition(
                positional,
                UniversalCategory.VowelModifierAbove,
                UniversalCategory.VowelModifierBelow,
                UniversalCategory.VowelModifierPost,
                UniversalCategory.VowelModifierPre);
        }

        return UniversalCategory.Other;
    }

    static UniversalCategory ResolveHieroglyph(
        int codePoint,
        string syllabic)
    {
        if (syllabic == "Hieroglyph" ||
            (codePoint >= 0x13000 && codePoint <= 0x1342F) ||
            (codePoint >= 0x1343C && codePoint <= 0x1343F) ||
            (codePoint >= 0x13441 && codePoint <= 0x13446) ||
            (codePoint >= 0x13460 && codePoint <= 0x143FA))
        {
            return UniversalCategory.Hieroglyph;
        }

        if (syllabic == "Hieroglyph_Joiner" ||
            (codePoint >= 0x13430 && codePoint <= 0x13436) ||
            (codePoint >= 0x13439 && codePoint <= 0x1343B))
        {
            return UniversalCategory.HieroglyphJoiner;
        }

        if (syllabic == "Hieroglyph_Mirror")
        {
            return UniversalCategory.HieroglyphMirror;
        }

        if (syllabic == "Hieroglyph_Modifier")
        {
            return UniversalCategory.HieroglyphModifier;
        }

        if (syllabic == "Hieroglyph_Mark_Begin" ||
            syllabic == "Hieroglyph_Segment_Begin" ||
            codePoint == 0x005B || codePoint == 0x007B ||
            codePoint == 0x27E6 || codePoint == 0x27E8 ||
            codePoint == 0x2E22 || codePoint == 0x2E24 ||
            codePoint == 0x13437)
        {
            return UniversalCategory.HieroglyphSegmentBegin;
        }

        if (syllabic == "Hieroglyph_Mark_End" ||
            syllabic == "Hieroglyph_Segment_End" ||
            codePoint == 0x005D || codePoint == 0x007D ||
            codePoint == 0x27E7 || codePoint == 0x27E9 ||
            codePoint == 0x2E23 || codePoint == 0x2E25 ||
            codePoint == 0x13438)
        {
            return UniversalCategory.HieroglyphSegmentEnd;
        }

        return UniversalCategory.Other;
    }

    static UniversalCategory ResolvePosition(
        string positional,
        UniversalCategory above,
        UniversalCategory below,
        UniversalCategory post,
        UniversalCategory pre)
    {
        if (IsPre(positional))
        {
            return pre;
        }

        if (IsBelow(positional))
        {
            return below;
        }

        if (IsPost(positional))
        {
            return post;
        }

        return above;
    }

    static bool IsBase(
        string syllabic,
        string joining,
        bool letterOther)
    {
        if (syllabic == "Number" || syllabic == "Consonant" ||
            syllabic == "Consonant_Head_Letter" ||
            syllabic == "Tone_Letter" ||
            syllabic == "Vowel_Independent")
        {
            return true;
        }

        if ((joining == "C" || joining == "D" || joining == "L" ||
            joining == "R") && syllabic != "Joiner")
        {
            return true;
        }

        return letterOther &&
            (syllabic == "Avagraha" || syllabic == "Bindu" ||
            syllabic == "Consonant_Final" ||
            syllabic == "Consonant_Medial" ||
            syllabic == "Consonant_Subjoined" ||
            syllabic == "Vowel" || syllabic == "Vowel_Dependent");
    }

    static bool IsBaseOther(int codePoint, string syllabic)
    {
        return syllabic == "Consonant_Placeholder" ||
            codePoint == 0x25CC || codePoint == 0x2015 ||
            codePoint == 0x2022 ||
            (codePoint >= 0x25FB && codePoint <= 0x25FE);
    }

    static bool IsVowelCategory(string syllabic, bool letterOther)
    {
        return syllabic == "Pure_Killer" ||
            (!letterOther &&
            (syllabic == "Vowel" || syllabic == "Vowel_Dependent"));
    }

    static bool IsVowelModifierCategory(
        string syllabic,
        bool letterOther)
    {
        return syllabic == "Tone_Mark" ||
            syllabic == "Cantillation_Mark" ||
            syllabic == "Register_Shifter" || syllabic == "Visarga" ||
            (!letterOther && syllabic == "Bindu");
    }

    static bool IsWordJoiner(
        int codePoint,
        string syllabic,
        System.Globalization.UnicodeCategory general,
        bool defaultIgnorable)
    {
        if (general == System.Globalization.UnicodeCategory.OtherNotAssigned)
        {
            return true;
        }

        if (!defaultIgnorable || syllabic != "Other")
        {
            return false;
        }

        return codePoint != 0x115F && codePoint != 0x1160 &&
            codePoint != 0x3164 && codePoint != 0xFFA0 &&
            (codePoint < 0x1BCA0 || codePoint > 0x1BCA3);
    }

    static bool IsMark(System.Globalization.UnicodeCategory category)
    {
        return category == System.Globalization.UnicodeCategory.SpacingCombiningMark ||
            category == System.Globalization.UnicodeCategory.EnclosingMark ||
            category == System.Globalization.UnicodeCategory.NonSpacingMark;
    }

    static bool IsPre(string positional)
    {
        return positional == "Left" || positional == "Visual_Order_Left" ||
            positional == "Left_And_Right" ||
            positional == "Top_And_Bottom_And_Left" ||
            positional == "Top_And_Left" ||
            positional == "Top_And_Left_And_Right";
    }

    static bool IsBelow(string positional)
    {
        return positional == "Bottom" || positional == "Overstruck" ||
            positional == "Bottom_And_Left" ||
            positional == "Bottom_And_Right";
    }

    static bool IsPost(string positional)
    {
        return positional == "Right";
    }

    static bool IsDisabledScript(string script)
    {
        return script == "Arabic" || script == "Lao" ||
            script == "Samaritan" || script == "Syriac" ||
            script == "Thai";
    }

    static string ApplySyllabicOverride(int codePoint, string value)
    {
        if (codePoint >= 0x1CE2 && codePoint <= 0x1CE8)
        {
            return "Cantillation_Mark";
        }

        if ((codePoint >= 0x0F18 && codePoint <= 0x0F19) ||
            (codePoint >= 0x0F3E && codePoint <= 0x0F3F))
        {
            return "Vowel_Dependent";
        }

        if (codePoint == 0x1CED)
        {
            return "Tone_Mark";
        }

        return value;
    }

    static string ApplyPositionalOverride(int codePoint, string value)
    {
        if (codePoint == 0x11302 || codePoint == 0x11303 ||
            codePoint == 0x114C1)
        {
            return "Top";
        }

        return value;
    }
}

internal class UniversalSyllableMatch
{
    int end = -1;
    UniversalSyllableType type = UniversalSyllableType.NonCluster;

    internal void Consider(
        int candidateEnd,
        UniversalSyllableType candidateType)
    {
        if (candidateEnd > end)
        {
            end = candidateEnd;
            type = candidateType;
        }
    }

    internal int GetEnd()
    {
        return end;
    }

    internal UniversalSyllableType GetTypeValue()
    {
        return type;
    }
}

internal static class UniversalSyllableRecognizer
{
    const string SyllableLimitMessage =
        "USE syllable recognition exceeds the configured syllable limit.";

    internal static UniversalSyllableMap Recognize(
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

        UniversalCategory[] categories =
            new UniversalCategory[buffer.GetCount()];
        int index = 0;
        while (index < categories.Length)
        {
            categories[index] = UniversalCategoryData.GetCategory(
                unicodeData,
                buffer.GetScalarValue(index));
            index++;
        }

        System.Collections.Generic.List<UniversalCategory> machineValues =
            new System.Collections.Generic.List<UniversalCategory>();
        System.Collections.Generic.List<int> machineSourceIndexes =
            new System.Collections.Generic.List<int>();
        BuildMachineView(
            buffer,
            categories,
            machineValues,
            machineSourceIndexes);
        UniversalCategory[] machineCategories =
            new UniversalCategory[machineValues.Count];
        machineValues.CopyTo(machineCategories, 0);

        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<UniversalSyllableType> types =
            new System.Collections.Generic.List<UniversalSyllableType>();
        int[] runeSyllableIndexes = new int[categories.Length];
        bool hasBroken = false;
        index = 0;
        while (index < machineCategories.Length)
        {
            UniversalSyllableMatch match = MatchSyllable(
                machineCategories,
                index);
            int end = match.GetEnd();
            UniversalSyllableType type = match.GetTypeValue();
            if (end <= index)
            {
                end = index + 1;
                type = UniversalSyllableType.NonCluster;
            }

            if (end < machineCategories.Length &&
                machineCategories[end] == UniversalCategory.NonJoiner)
            {
                end++;
            }

            if (types.Count >= maximumSyllableCount)
            {
                throw FontLimitException.Create(SyllableLimitMessage);
            }

            int syllableIndex = types.Count;
            int originalStart = index == 0 ? 0 :
                machineSourceIndexes[index];
            int originalEnd = end < machineCategories.Length ?
                machineSourceIndexes[end] : categories.Length;
            starts.Add(originalStart);
            lengths.Add(originalEnd - originalStart);
            types.Add(type);
            int runeIndex = originalStart;
            while (runeIndex < originalEnd)
            {
                runeSyllableIndexes[runeIndex] = syllableIndex;
                buffer.SetSyllable(
                    runeIndex,
                    ((syllableIndex % 15) + 1) * 16 + (int)type);
                runeIndex++;
            }

            hasBroken = hasBroken || type == UniversalSyllableType.Broken;
            index = end;
        }

        if (categories.Length > 0 && machineCategories.Length == 0)
        {
            if (maximumSyllableCount == 0)
            {
                throw FontLimitException.Create(SyllableLimitMessage);
            }

            starts.Add(0);
            lengths.Add(categories.Length);
            types.Add(UniversalSyllableType.NonCluster);
            int runeIndex = 0;
            while (runeIndex < categories.Length)
            {
                buffer.SetSyllable(
                    runeIndex,
                    16 + (int)UniversalSyllableType.NonCluster);
                runeIndex++;
            }
        }

        return new UniversalSyllableMap(
            categories,
            runeSyllableIndexes,
            CopyIntegers(starts),
            CopyIntegers(lengths),
            CopyTypes(types),
            hasBroken);
    }

    static void BuildMachineView(
        ShapingBuffer buffer,
        UniversalCategory[] categories,
        System.Collections.Generic.List<UniversalCategory> values,
        System.Collections.Generic.List<int> sourceIndexes)
    {
        int index = 0;
        while (index < categories.Length)
        {
            bool include = categories[index] !=
                UniversalCategory.CombiningJoiner;
            if (include && categories[index] == UniversalCategory.NonJoiner)
            {
                int next = index + 1;
                while (next < categories.Length && categories[next] ==
                    UniversalCategory.CombiningJoiner)
                {
                    next++;
                }

                if (next < categories.Length && IsUnicodeMark(
                    buffer.GetRune(next)))
                {
                    include = false;
                }
            }

            if (include)
            {
                values.Add(categories[index]);
                sourceIndexes.Add(index);
            }

            index++;
        }
    }

    static bool IsUnicodeMark(System.Text.Rune value)
    {
        System.Globalization.UnicodeCategory category =
            System.Text.Rune.GetUnicodeCategory(value);
        return category ==
                System.Globalization.UnicodeCategory.SpacingCombiningMark ||
            category == System.Globalization.UnicodeCategory.EnclosingMark ||
            category ==
                System.Globalization.UnicodeCategory.NonSpacingMark;
    }

    static UniversalSyllableMatch MatchSyllable(
        UniversalCategory[] categories,
        int start)
    {
        UniversalSyllableMatch match = new UniversalSyllableMatch();
        match.Consider(ParseViramaTerminated(categories, start),
            UniversalSyllableType.ViramaTerminated);
        match.Consider(ParseSakotTerminated(categories, start),
            UniversalSyllableType.SakotTerminated);
        match.Consider(ParseStandard(categories, start),
            UniversalSyllableType.Standard);
        match.Consider(ParseNumberJoinerTerminated(categories, start),
            UniversalSyllableType.NumberJoinerTerminated);
        match.Consider(ParseNumeral(categories, start),
            UniversalSyllableType.Numeral);
        match.Consider(ParseSymbol(categories, start),
            UniversalSyllableType.Symbol);
        match.Consider(ParseHieroglyph(categories, start),
            UniversalSyllableType.Hieroglyph);
        if (IsCategory(categories, start,
            UniversalCategory.ConsonantFinalModifierPost))
        {
            match.Consider(start + 1, UniversalSyllableType.NonCluster);
        }

        match.Consider(ParseBroken(categories, start),
            UniversalSyllableType.Broken);
        return match;
    }

    static int ParseViramaTerminated(
        UniversalCategory[] values,
        int start)
    {
        int index = ParseComplexStart(values, start);
        if (index < 0)
        {
            return -1;
        }

        index = ParseConsonantModifiers(values, index);
        if (IsCategory(values, index, UniversalCategory.InvisibleStacker) ||
            IsCategory(values, index, UniversalCategory.ReorderingKiller))
        {
            return index + 1;
        }

        return -1;
    }

    static int ParseSakotTerminated(
        UniversalCategory[] values,
        int start)
    {
        int index = ParseComplexStart(values, start);
        if (index < 0)
        {
            return -1;
        }

        index = ParseComplexMiddle(values, index);
        return IsCategory(values, index, UniversalCategory.Sakot) ?
            index + 1 : -1;
    }

    static int ParseStandard(UniversalCategory[] values, int start)
    {
        int index = ParseComplexStart(values, start);
        return index < 0 ? -1 : ParseComplexTail(values, index);
    }

    static int ParseNumberJoinerTerminated(
        UniversalCategory[] values,
        int start)
    {
        if (!IsCategory(values, start, UniversalCategory.BaseNumber))
        {
            return -1;
        }

        int index = start + 1;
        while (IsCategory(values, index, UniversalCategory.HalantNumber) &&
            IsCategory(values, index + 1, UniversalCategory.BaseNumber))
        {
            index += 2;
        }

        return IsCategory(values, index, UniversalCategory.HalantNumber) ?
            index + 1 : -1;
    }

    static int ParseNumeral(UniversalCategory[] values, int start)
    {
        if (!IsCategory(values, start, UniversalCategory.BaseNumber))
        {
            return -1;
        }

        int index = start + 1;
        while (IsCategory(values, index, UniversalCategory.HalantNumber) &&
            IsCategory(values, index + 1, UniversalCategory.BaseNumber))
        {
            index += 2;
        }

        return index;
    }

    static int ParseSymbol(UniversalCategory[] values, int start)
    {
        if (!IsCategory(values, start, UniversalCategory.Other) &&
            !IsCategory(values, start, UniversalCategory.BaseOther) &&
            !IsCategory(
                values,
                start,
                UniversalCategory.HieroglyphSegmentBegin))
        {
            return -1;
        }

        int tail = ParseTail(values, start + 1);
        return tail > start + 1 ? tail : start + 1;
    }

    static int ParseHieroglyph(UniversalCategory[] values, int start)
    {
        int index = start;
        while (IsCategory(
            values,
            index,
            UniversalCategory.HieroglyphSegmentBegin))
        {
            index++;
        }

        if (!IsCategory(values, index, UniversalCategory.Hieroglyph))
        {
            return -1;
        }

        index++;
        index = ParseHieroglyphSuffix(values, index);
        while (IsCategory(values, index,
            UniversalCategory.HieroglyphJoiner))
        {
            index++;
            while (IsCategory(
                values,
                index,
                UniversalCategory.HieroglyphSegmentBegin))
            {
                index++;
            }

            if (IsCategory(values, index, UniversalCategory.Hieroglyph))
            {
                index++;
                index = ParseHieroglyphSuffix(values, index);
            }
        }

        return index;
    }

    static int ParseHieroglyphSuffix(
        UniversalCategory[] values,
        int index)
    {
        if (IsCategory(values, index, UniversalCategory.HieroglyphMirror))
        {
            index++;
        }

        if (IsCategory(values, index, UniversalCategory.HieroglyphModifier))
        {
            index++;
        }

        while (IsCategory(
            values,
            index,
            UniversalCategory.HieroglyphSegmentEnd))
        {
            index++;
        }

        return index;
    }

    static int ParseBroken(UniversalCategory[] values, int start)
    {
        int index = start;
        if (IsCategory(values, index, UniversalCategory.Repha))
        {
            index++;
        }

        int best = ParseTail(values, index);
        int candidate = ParseNumberJoinerTail(values, index);
        if (candidate > best)
        {
            best = candidate;
        }

        candidate = ParseNumeralTail(values, index);
        if (candidate > best)
        {
            best = candidate;
        }

        return best > start ? best : -1;
    }

    static int ParseTail(UniversalCategory[] values, int start)
    {
        int best = ParseComplexTail(values, start);
        int candidate = ParseSakotTail(values, start);
        if (candidate > best)
        {
            best = candidate;
        }

        candidate = ParseSymbolModifierTail(values, start);
        if (candidate > best)
        {
            best = candidate;
        }

        candidate = ParseViramaTail(values, start);
        return candidate > best ? candidate : best;
    }

    static int ParseComplexStart(
        UniversalCategory[] values,
        int start)
    {
        int index = start;
        if (IsCategory(values, index, UniversalCategory.Repha) ||
            IsCategory(values, index, UniversalCategory.ConsonantWithStacker))
        {
            index++;
        }

        if (IsCategory(values, index, UniversalCategory.Base) ||
            IsCategory(values, index, UniversalCategory.BaseOther))
        {
            return index + 1;
        }

        return -1;
    }

    static int ParseComplexTail(
        UniversalCategory[] values,
        int start)
    {
        int index = ParseComplexMiddle(values, start);
        while (IsCategory(values, index,
            UniversalCategory.ConsonantFinalAbove))
        {
            index++;
        }

        while (IsCategory(values, index,
            UniversalCategory.ConsonantFinalBelow))
        {
            index++;
        }

        while (IsCategory(values, index,
            UniversalCategory.ConsonantFinalPost))
        {
            index++;
        }

        int first = index;
        while (IsCategory(values, index,
            UniversalCategory.ConsonantFinalModifierAbove))
        {
            index++;
        }

        while (IsCategory(values, index,
            UniversalCategory.ConsonantFinalModifierBelow))
        {
            index++;
        }

        if (index == first && IsCategory(values, index,
            UniversalCategory.ConsonantFinalModifierPost))
        {
            index++;
        }

        return index;
    }

    static int ParseComplexMiddle(
        UniversalCategory[] values,
        int start)
    {
        int index = ParseConsonantModifiers(values, start);
        index = ConsumeOptional(values, index,
            UniversalCategory.ConsonantMedialPre);
        index = ConsumeOptional(values, index,
            UniversalCategory.ConsonantMedialAbove);
        index = ConsumeOptional(values, index,
            UniversalCategory.ConsonantMedialBelow);
        index = ConsumeOptional(values, index,
            UniversalCategory.ConsonantMedialPost);
        int vowelSequence = index;
        vowelSequence = ConsumeMany(values, vowelSequence,
            UniversalCategory.VowelPre);
        vowelSequence = ConsumeMany(values, vowelSequence,
            UniversalCategory.VowelAbove);
        vowelSequence = ConsumeMany(values, vowelSequence,
            UniversalCategory.VowelBelow);
        vowelSequence = ConsumeMany(values, vowelSequence,
            UniversalCategory.VowelPost);
        if (IsCategory(values, index, UniversalCategory.Halant) &&
            index + 1 > vowelSequence)
        {
            vowelSequence = index + 1;
        }

        index = vowelSequence;
        index = ConsumeOptional(values, index,
            UniversalCategory.HalantOrVowelModifier);
        index = ConsumeMany(values, index,
            UniversalCategory.VowelModifierPre);
        index = ConsumeMany(values, index,
            UniversalCategory.VowelModifierAbove);
        index = ConsumeMany(values, index,
            UniversalCategory.VowelModifierBelow);
        index = ConsumeMany(values, index,
            UniversalCategory.VowelModifierPost);
        while (IsCategory(values, index, UniversalCategory.Sakot) &&
            IsCategory(values, index + 1, UniversalCategory.Base))
        {
            index += 2;
        }

        return index;
    }

    static int ParseConsonantModifiers(
        UniversalCategory[] values,
        int start)
    {
        int index = ConsumeMany(values, start,
            UniversalCategory.ConsonantModifierAbove);
        index = ConsumeMany(values, index,
            UniversalCategory.ConsonantModifierBelow);
        while (true)
        {
            int next = index;
            if (IsHalant(values, next) &&
                IsCategory(values, next + 1, UniversalCategory.Base))
            {
                next += 2;
            }
            else if (IsCategory(
                values,
                next,
                UniversalCategory.ConsonantSubjoined))
            {
                next++;
            }
            else
            {
                break;
            }

            next = ConsumeMany(values, next,
                UniversalCategory.ConsonantModifierAbove);
            next = ConsumeMany(values, next,
                UniversalCategory.ConsonantModifierBelow);
            index = next;
        }

        return index;
    }

    static int ParseViramaTail(UniversalCategory[] values, int start)
    {
        int index = ParseConsonantModifiers(values, start);
        if (IsCategory(values, index, UniversalCategory.InvisibleStacker) ||
            IsCategory(values, index, UniversalCategory.ReorderingKiller))
        {
            return index + 1;
        }

        return -1;
    }

    static int ParseSakotTail(UniversalCategory[] values, int start)
    {
        int index = ParseComplexMiddle(values, start);
        return IsCategory(values, index, UniversalCategory.Sakot) ?
            index + 1 : -1;
    }

    static int ParseSymbolModifierTail(
        UniversalCategory[] values,
        int start)
    {
        int index = start;
        if (IsCategory(values, index,
            UniversalCategory.SymbolModifierAbove))
        {
            index = ConsumeMany(values, index,
                UniversalCategory.SymbolModifierAbove);
            return ConsumeMany(values, index,
                UniversalCategory.SymbolModifierBelow);
        }

        if (IsCategory(values, index,
            UniversalCategory.SymbolModifierBelow))
        {
            return ConsumeMany(values, index,
                UniversalCategory.SymbolModifierBelow);
        }

        return -1;
    }

    static int ParseNumberJoinerTail(
        UniversalCategory[] values,
        int start)
    {
        int index = start;
        while (IsCategory(values, index, UniversalCategory.HalantNumber) &&
            IsCategory(values, index + 1, UniversalCategory.BaseNumber))
        {
            index += 2;
        }

        return IsCategory(values, index, UniversalCategory.HalantNumber) ?
            index + 1 : -1;
    }

    static int ParseNumeralTail(
        UniversalCategory[] values,
        int start)
    {
        int index = start;
        bool found = false;
        while (IsCategory(values, index, UniversalCategory.HalantNumber) &&
            IsCategory(values, index + 1, UniversalCategory.BaseNumber))
        {
            found = true;
            index += 2;
        }

        return found ? index : -1;
    }

    static int ConsumeOptional(
        UniversalCategory[] values,
        int index,
        UniversalCategory category)
    {
        return IsCategory(values, index, category) ? index + 1 : index;
    }

    static int ConsumeMany(
        UniversalCategory[] values,
        int index,
        UniversalCategory category)
    {
        while (IsCategory(values, index, category))
        {
            index++;
        }

        return index;
    }

    static bool IsHalant(UniversalCategory[] values, int index)
    {
        return IsCategory(values, index, UniversalCategory.Halant) ||
            IsCategory(values, index,
                UniversalCategory.HalantOrVowelModifier) ||
            IsCategory(values, index, UniversalCategory.InvisibleStacker) ||
            IsCategory(values, index, UniversalCategory.Sakot);
    }

    static bool IsCategory(
        UniversalCategory[] values,
        int index,
        UniversalCategory category)
    {
        return index >= 0 && index < values.Length &&
            values[index] == category;
    }

    static int[] CopyIntegers(
        System.Collections.Generic.List<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static UniversalSyllableType[] CopyTypes(
        System.Collections.Generic.List<UniversalSyllableType> values)
    {
        UniversalSyllableType[] result =
            new UniversalSyllableType[values.Count];
        values.CopyTo(result, 0);
        return result;
    }
}
