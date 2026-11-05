namespace ttf;

internal enum IndicShapingModel
{
    Version1 = 0,
    Version2 = 1
}

internal enum IndicSyllabicCategory
{
    Other = 0,
    Consonant = 1,
    Vowel = 2,
    Nukta = 3,
    Halant = 4,
    NonJoiner = 5,
    Joiner = 6,
    Matra = 7,
    SyllableModifier = 8,
    Accent = 9,
    Placeholder = 10,
    DottedCircle = 11,
    RegisterShifter = 12,
    PostMatra = 13,
    Repha = 14,
    Ra = 15,
    ConsonantMedial = 16,
    Symbol = 17,
    ConsonantWithStacker = 18,
    SyllableModifierPost = 19
}

internal enum IndicPositionalCategory
{
    NotApplicable = 0,
    Left = 1,
    Top = 2,
    Bottom = 3,
    Right = 4,
    BottomAndRight = 5,
    LeftAndRight = 6,
    TopAndBottom = 7,
    TopAndBottomAndLeft = 8,
    TopAndBottomAndRight = 9,
    TopAndLeft = 10,
    TopAndLeftAndRight = 11,
    TopAndRight = 12,
    Overstruck = 13,
    VisualOrderLeft = 14
}

internal enum IndicSyllableType
{
    Consonant = 0,
    Vowel = 1,
    Standalone = 2,
    Symbol = 3,
    Broken = 4,
    NonIndic = 5
}

internal class IndicSyllableMap
{
    IndicShapingModel model;
    IndicSyllabicCategory[] categories;
    IndicPositionalCategory[] positions;
    int[] runeSyllableIndexes;
    int[] starts;
    int[] lengths;
    IndicSyllableType[] types;
    bool hasBrokenSyllable;

    internal IndicSyllableMap(
        IndicShapingModel model,
        IndicSyllabicCategory[] categories,
        IndicPositionalCategory[] positions,
        int[] runeSyllableIndexes,
        int[] starts,
        int[] lengths,
        IndicSyllableType[] types,
        bool hasBrokenSyllable)
    {
        this.model = model;
        this.categories = categories;
        this.positions = positions;
        this.runeSyllableIndexes = runeSyllableIndexes;
        this.starts = starts;
        this.lengths = lengths;
        this.types = types;
        this.hasBrokenSyllable = hasBrokenSyllable;
    }

    internal IndicShapingModel GetModel()
    {
        return model;
    }

    internal int GetRuneCount()
    {
        return categories.Length;
    }

    internal IndicSyllabicCategory GetCategory(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return categories[runeIndex];
    }

    internal IndicPositionalCategory GetPosition(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return positions[runeIndex];
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

    internal IndicSyllableType GetSyllableType(int syllableIndex)
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

internal static class IndicSyllableRecognizer
{
    const string SyllableLimitMessage =
        "Indic syllable recognition exceeds the configured syllable limit.";

    internal static IndicSyllableMap Recognize(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        IndicShapingModel model,
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

        ValidateModel(model);
        if (maximumSyllableCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSyllableCount");
        }

        int runeCount = buffer.GetCount();
        IndicSyllabicCategory[] categories =
            new IndicSyllabicCategory[runeCount];
        IndicPositionalCategory[] positions =
            new IndicPositionalCategory[runeCount];
        LoadCategories(buffer, unicodeData, categories, positions);
        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<IndicSyllableType> types =
            new System.Collections.Generic.List<IndicSyllableType>();
        int[] runeSyllableIndexes = new int[runeCount];
        bool hasBrokenSyllable = false;
        int index = 0;

        while (index < runeCount)
        {
            IndicSyllableType type = IndicSyllableType.NonIndic;
            int end = -1;
            int candidateEnd = ParseConsonantSyllable(categories, index);
            if (candidateEnd > end)
            {
                type = IndicSyllableType.Consonant;
                end = candidateEnd;
            }

            candidateEnd = ParseVowelSyllable(categories, index);
            if (candidateEnd > end)
            {
                type = IndicSyllableType.Vowel;
                end = candidateEnd;
            }

            candidateEnd = ParseStandaloneCluster(categories, index);
            if (candidateEnd > end)
            {
                type = IndicSyllableType.Standalone;
                end = candidateEnd;
            }

            candidateEnd = ParseSymbolCluster(categories, index);
            if (candidateEnd > end)
            {
                type = IndicSyllableType.Symbol;
                end = candidateEnd;
            }

            bool isPostModifier = IsCategory(
                categories,
                index,
                IndicSyllabicCategory.SyllableModifierPost);
            if (isPostModifier && index + 1 > end)
            {
                type = IndicSyllableType.NonIndic;
                end = index + 1;
            }

            candidateEnd = ParseBrokenCluster(categories, index);
            if (candidateEnd > end)
            {
                type = IndicSyllableType.Broken;
                end = candidateEnd;
            }

            if (end <= index)
            {
                type = IndicSyllableType.NonIndic;
                end = index + 1;
            }

            if (type == IndicSyllableType.Broken)
            {
                hasBrokenSyllable = true;
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

            index = end;
        }

        ApplySyllables(buffer, starts, lengths, types);

        return new IndicSyllableMap(
            model,
            categories,
            positions,
            runeSyllableIndexes,
            CopyIntegers(starts),
            CopyIntegers(lengths),
            CopyTypes(types),
            hasBrokenSyllable);
    }

    static void LoadCategories(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        IndicSyllabicCategory[] categories,
        IndicPositionalCategory[] positions)
    {
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            string categoryName = unicodeData.GetPropertyName(
                UnicodeDataTags.IndicSyllabicCategory,
                codePoint);
            string positionName = unicodeData.GetPropertyName(
                UnicodeDataTags.IndicPositionalCategory,
                codePoint);
            IndicSyllabicCategory category = ConvertCategory(
                categoryName);
            category = ApplyCategoryOverride(codePoint, category);
            if (!IsIndicShapingCodePoint(codePoint))
            {
                category = IndicSyllabicCategory.Other;
            }

            IndicPositionalCategory position = ConvertPosition(
                positionName);
            if (category == IndicSyllabicCategory.SyllableModifier &&
                position == IndicPositionalCategory.NotApplicable)
            {
                category = IndicSyllabicCategory.SyllableModifierPost;
            }

            categories[index] = category;
            positions[index] = position;
            index++;
        }
    }

    static int ParseConsonantSyllable(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsCategory(categories, index, IndicSyllabicCategory.Repha) ||
            IsCategory(
                categories,
                index,
                IndicSyllabicCategory.ConsonantWithStacker))
        {
            index++;
        }

        int consonantEnd = ParseConsonantUnit(categories, index);
        if (consonantEnd < 0)
        {
            return -1;
        }

        return ParseComplexTail(categories, consonantEnd);
    }

    static int ParseVowelSyllable(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = ParseOptionalReph(categories, start);
        if (!IsCategory(categories, index, IndicSyllabicCategory.Vowel))
        {
            return -1;
        }

        index++;
        index = ParseNuktaSequence(categories, index);
        int complexEnd = ParseComplexTail(categories, index);
        if (complexEnd > index)
        {
            return complexEnd;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Joiner))
        {
            return index + 1;
        }

        return complexEnd;
    }

    static int ParseStandaloneCluster(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        bool matched = false;
        if (IsCategory(categories, index, IndicSyllabicCategory.Repha) ||
            IsCategory(
                categories,
                index,
                IndicSyllabicCategory.ConsonantWithStacker))
        {
            index++;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Placeholder))
        {
            index++;
            matched = true;
        }
        else
        {
            index = ParseOptionalReph(categories, start);
            if (IsCategory(
                categories,
                index,
                IndicSyllabicCategory.DottedCircle))
            {
                index++;
                matched = true;
            }
        }

        if (!matched)
        {
            return -1;
        }

        index = ParseNuktaSequence(categories, index);
        return ParseComplexTail(categories, index);
    }

    static int ParseSymbolCluster(
        IndicSyllabicCategory[] categories,
        int start)
    {
        if (!IsCategory(categories, start, IndicSyllabicCategory.Symbol))
        {
            return -1;
        }

        int index = start + 1;
        if (IsCategory(categories, index, IndicSyllabicCategory.Nukta))
        {
            index++;
        }

        return ParseSyllableTail(categories, index);
    }

    static int ParseBrokenCluster(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = ParseOptionalReph(categories, start);
        index = ParseNuktaSequence(categories, index);
        index = ParseComplexTail(categories, index);
        return index > start ? index : -1;
    }

    static int ParseComplexTail(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        while (true)
        {
            int halantEnd = ParseHalantGroup(categories, index);
            if (halantEnd < 0)
            {
                break;
            }

            int consonantEnd = ParseConsonantUnit(
                categories,
                halantEnd);
            if (consonantEnd < 0)
            {
                break;
            }

            index = consonantEnd;
        }

        if (IsCategory(
            categories,
            index,
            IndicSyllabicCategory.ConsonantMedial))
        {
            index++;
        }

        int finalHalantEnd = ParseFinalHalantGroup(categories, index);
        if (finalHalantEnd >= 0)
        {
            index = finalHalantEnd;
        }
        else
        {
            while (true)
            {
                int matraEnd = ParseMatraGroup(categories, index);
                if (matraEnd < 0)
                {
                    break;
                }

                index = matraEnd;
            }
        }

        return ParseSyllableTail(categories, index);
    }

    static int ParseConsonantUnit(
        IndicSyllabicCategory[] categories,
        int start)
    {
        if (!IsConsonant(categories, start))
        {
            return -1;
        }

        int index = start + 1;
        if (IsCategory(categories, index, IndicSyllabicCategory.Joiner))
        {
            index++;
        }

        return ParseNuktaSequence(categories, index);
    }

    static int ParseNuktaSequence(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsCategory(categories, index, IndicSyllabicCategory.NonJoiner) &&
            IsCategory(
                categories,
                index + 1,
                IndicSyllabicCategory.RegisterShifter))
        {
            index += 2;
        }
        else if (IsCategory(
            categories,
            index,
            IndicSyllabicCategory.RegisterShifter))
        {
            index++;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Nukta))
        {
            index++;
            if (IsCategory(categories, index, IndicSyllabicCategory.Nukta))
            {
                index++;
            }
        }

        return index;
    }

    static int ParseOptionalReph(
        IndicSyllabicCategory[] categories,
        int start)
    {
        if (IsCategory(categories, start, IndicSyllabicCategory.Repha))
        {
            return start + 1;
        }

        if (IsCategory(categories, start, IndicSyllabicCategory.Ra) &&
            IsCategory(categories, start + 1, IndicSyllabicCategory.Halant))
        {
            return start + 2;
        }

        return start;
    }

    static int ParseHalantGroup(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        if (IsJoiner(categories, index))
        {
            index++;
        }

        if (!IsCategory(categories, index, IndicSyllabicCategory.Halant))
        {
            return -1;
        }

        index++;
        if (IsCategory(categories, index, IndicSyllabicCategory.Joiner))
        {
            index++;
            if (IsCategory(categories, index, IndicSyllabicCategory.Nukta))
            {
                index++;
            }
        }

        return index;
    }

    static int ParseFinalHalantGroup(
        IndicSyllabicCategory[] categories,
        int start)
    {
        if (IsCategory(categories, start, IndicSyllabicCategory.Halant) &&
            IsCategory(
                categories,
                start + 1,
                IndicSyllabicCategory.NonJoiner))
        {
            return start + 2;
        }

        return ParseHalantGroup(categories, start);
    }

    static int ParseMatraGroup(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        while (IsJoiner(categories, index))
        {
            index++;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Matra))
        {
            index++;
        }
        else
        {
            if (IsSyllableModifier(categories, index))
            {
                index++;
            }

            if (!IsCategory(
                categories,
                index,
                IndicSyllabicCategory.PostMatra))
            {
                return -1;
            }

            index++;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Nukta))
        {
            index++;
        }

        if (IsCategory(categories, index, IndicSyllabicCategory.Halant))
        {
            index++;
        }

        return index;
    }

    static int ParseSyllableTail(
        IndicSyllabicCategory[] categories,
        int start)
    {
        int index = start;
        int modifierStart = index;
        if (IsJoiner(categories, index))
        {
            index++;
        }

        if (IsSyllableModifier(categories, index))
        {
            index++;
            if (IsSyllableModifier(categories, index))
            {
                index++;
            }

            if (IsCategory(
                categories,
                index,
                IndicSyllabicCategory.NonJoiner))
            {
                index++;
            }
        }
        else
        {
            index = modifierStart;
        }

        while (IsCategory(
            categories,
            index,
            IndicSyllabicCategory.Accent))
        {
            index++;
        }

        return index;
    }

    static bool IsConsonant(
        IndicSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(
            categories,
            index,
            IndicSyllabicCategory.Consonant) ||
            IsCategory(categories, index, IndicSyllabicCategory.Ra);
    }

    static bool IsJoiner(
        IndicSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(categories, index, IndicSyllabicCategory.Joiner) ||
            IsCategory(
                categories,
                index,
                IndicSyllabicCategory.NonJoiner);
    }

    static bool IsSyllableModifier(
        IndicSyllabicCategory[] categories,
        int index)
    {
        return IsCategory(
            categories,
            index,
            IndicSyllabicCategory.SyllableModifier) ||
            IsCategory(
                categories,
                index,
                IndicSyllabicCategory.SyllableModifierPost);
    }

    static bool IsCategory(
        IndicSyllabicCategory[] categories,
        int index,
        IndicSyllabicCategory category)
    {
        return index >= 0 && index < categories.Length &&
            categories[index] == category;
    }

    static IndicSyllabicCategory ConvertCategory(string value)
    {
        if (value == "Consonant" || value == "Consonant_Dead" ||
            value == "Consonant_Head_Letter" ||
            value == "Consonant_Initial_Postfixed")
        {
            return IndicSyllabicCategory.Consonant;
        }

        if (value == "Vowel" || value == "Vowel_Independent")
        {
            return IndicSyllabicCategory.Vowel;
        }

        if (value == "Nukta" || value == "Tone_Mark")
        {
            return IndicSyllabicCategory.Nukta;
        }

        if (value == "Virama" || value == "Invisible_Stacker")
        {
            return IndicSyllabicCategory.Halant;
        }

        if (value == "Non_Joiner")
        {
            return IndicSyllabicCategory.NonJoiner;
        }

        if (value == "Joiner")
        {
            return IndicSyllabicCategory.Joiner;
        }

        if (value == "Vowel_Dependent" || value == "Pure_Killer" ||
            value == "Consonant_Killer")
        {
            return IndicSyllabicCategory.Matra;
        }

        if (value == "Bindu" || value == "Syllable_Modifier" ||
            value == "Gemination_Mark" || value == "Visarga")
        {
            return IndicSyllabicCategory.SyllableModifier;
        }

        if (value == "Cantillation_Mark")
        {
            return IndicSyllabicCategory.Accent;
        }

        if (value == "Consonant_Placeholder" || value == "Number" ||
            value == "Number_Joiner" ||
            value == "Brahmi_Joining_Number")
        {
            return IndicSyllabicCategory.Placeholder;
        }

        if (value == "Register_Shifter")
        {
            return IndicSyllabicCategory.RegisterShifter;
        }

        if (value == "Consonant_Preceding_Repha")
        {
            return IndicSyllabicCategory.Repha;
        }

        if (value == "Consonant_Final" ||
            value == "Consonant_Medial" ||
            value == "Consonant_Subjoined" ||
            value == "Consonant_Succeeding_Repha")
        {
            return IndicSyllabicCategory.ConsonantMedial;
        }

        if (value == "Avagraha")
        {
            return IndicSyllabicCategory.Symbol;
        }

        if (value == "Consonant_With_Stacker")
        {
            return IndicSyllabicCategory.ConsonantWithStacker;
        }

        return IndicSyllabicCategory.Other;
    }

    static IndicSyllabicCategory ApplyCategoryOverride(
        int codePoint,
        IndicSyllabicCategory category)
    {
        if (codePoint == 0x0930 || codePoint == 0x09B0 ||
            codePoint == 0x09F0 || codePoint == 0x0A30 ||
            codePoint == 0x0AB0 || codePoint == 0x0B30 ||
            codePoint == 0x0BB0 || codePoint == 0x0C30 ||
            codePoint == 0x0CB0 || codePoint == 0x0D30)
        {
            return IndicSyllabicCategory.Ra;
        }

        if (codePoint == 0x0953 || codePoint == 0x0954)
        {
            return IndicSyllabicCategory.SyllableModifier;
        }

        if (codePoint == 0x0A40)
        {
            return IndicSyllabicCategory.PostMatra;
        }

        if (codePoint == 0x0A72 || codePoint == 0x0A73)
        {
            return IndicSyllabicCategory.Consonant;
        }

        if ((codePoint >= 0x1CE2 && codePoint <= 0x1CE8) ||
            codePoint == 0x1CED)
        {
            return IndicSyllabicCategory.Accent;
        }

        if ((codePoint >= 0xA8F2 && codePoint <= 0xA8F7) ||
            (codePoint >= 0x1CE9 && codePoint <= 0x1CF1 &&
            codePoint != 0x1CED))
        {
            return IndicSyllabicCategory.Symbol;
        }

        if (codePoint == 0x0A51)
        {
            return IndicSyllabicCategory.Matra;
        }

        if (codePoint >= 0x11301 && codePoint <= 0x11303)
        {
            return IndicSyllabicCategory.SyllableModifier;
        }

        if (codePoint == 0x1133B || codePoint == 0x1133C ||
            codePoint == 0x0AFB || codePoint == 0x0B55)
        {
            return IndicSyllabicCategory.Nukta;
        }

        if (codePoint == 0x09FC || codePoint == 0x0C80 ||
            codePoint == 0x0D04)
        {
            return IndicSyllabicCategory.Placeholder;
        }

        if (codePoint == 0x2015 || codePoint == 0x2022 ||
            (codePoint >= 0x25FB && codePoint <= 0x25FE))
        {
            return IndicSyllabicCategory.Placeholder;
        }

        if (codePoint == 0x25CC)
        {
            return IndicSyllabicCategory.DottedCircle;
        }

        return category;
    }

    static bool IsIndicShapingCodePoint(int codePoint)
    {
        if ((codePoint >= 0x0000 && codePoint <= 0x00FF) ||
            (codePoint >= 0x0900 && codePoint <= 0x0D7F) ||
            (codePoint >= 0x1CD0 && codePoint <= 0x1CFF) ||
            (codePoint >= 0x2000 && codePoint <= 0x209F) ||
            (codePoint >= 0xA830 && codePoint <= 0xA83F) ||
            (codePoint >= 0xA8E0 && codePoint <= 0xA8FF))
        {
            return true;
        }

        return codePoint == 0x25CC ||
            (codePoint >= 0x25FB && codePoint <= 0x25FE) ||
            (codePoint >= 0x11301 && codePoint <= 0x11303) ||
            codePoint == 0x1133B || codePoint == 0x1133C;
    }

    static IndicPositionalCategory ConvertPosition(string value)
    {
        if (value == "Left")
        {
            return IndicPositionalCategory.Left;
        }

        if (value == "Top")
        {
            return IndicPositionalCategory.Top;
        }

        if (value == "Bottom")
        {
            return IndicPositionalCategory.Bottom;
        }

        if (value == "Right")
        {
            return IndicPositionalCategory.Right;
        }

        if (value == "Bottom_And_Right")
        {
            return IndicPositionalCategory.BottomAndRight;
        }

        if (value == "Left_And_Right")
        {
            return IndicPositionalCategory.LeftAndRight;
        }

        if (value == "Top_And_Bottom")
        {
            return IndicPositionalCategory.TopAndBottom;
        }

        if (value == "Top_And_Bottom_And_Left")
        {
            return IndicPositionalCategory.TopAndBottomAndLeft;
        }

        if (value == "Top_And_Bottom_And_Right")
        {
            return IndicPositionalCategory.TopAndBottomAndRight;
        }

        if (value == "Top_And_Left")
        {
            return IndicPositionalCategory.TopAndLeft;
        }

        if (value == "Top_And_Left_And_Right")
        {
            return IndicPositionalCategory.TopAndLeftAndRight;
        }

        if (value == "Top_And_Right")
        {
            return IndicPositionalCategory.TopAndRight;
        }

        if (value == "Overstruck")
        {
            return IndicPositionalCategory.Overstruck;
        }

        if (value == "Visual_Order_Left" ||
            value == "Visual_order_left")
        {
            return IndicPositionalCategory.VisualOrderLeft;
        }

        return IndicPositionalCategory.NotApplicable;
    }

    static int[] CopyIntegers(
        System.Collections.Generic.List<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static void ApplySyllables(
        ShapingBuffer buffer,
        System.Collections.Generic.List<int> starts,
        System.Collections.Generic.List<int> lengths,
        System.Collections.Generic.List<IndicSyllableType> types)
    {
        int serial = 1;
        int syllableIndex = 0;
        while (syllableIndex < types.Count)
        {
            int encodedSyllable = serial * 16 +
                (int)types[syllableIndex];
            int runeIndex = starts[syllableIndex];
            int end = runeIndex + lengths[syllableIndex];
            while (runeIndex < end)
            {
                buffer.SetSyllable(runeIndex, encodedSyllable);
                runeIndex++;
            }

            serial++;
            if (serial == 16)
            {
                serial = 1;
            }

            syllableIndex++;
        }
    }

    static IndicSyllableType[] CopyTypes(
        System.Collections.Generic.List<IndicSyllableType> values)
    {
        IndicSyllableType[] result =
            new IndicSyllableType[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static void ValidateModel(IndicShapingModel model)
    {
        if (model != IndicShapingModel.Version1 &&
            model != IndicShapingModel.Version2)
        {
            throw new System.ArgumentOutOfRangeException("model");
        }
    }
}
