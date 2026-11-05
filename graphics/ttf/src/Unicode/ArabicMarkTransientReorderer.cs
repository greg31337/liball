namespace ttf;

internal static class ArabicMarkTransientReorderer
{
    internal static RuneBuffer Reorder(
        RuneBuffer input,
        UnicodeData unicodeData,
        int maximumRuneCount)
    {
        RuneBuffer normalized = UnicodeNormalizer.Normalize(
            input,
            unicodeData,
            UnicodeNormalizationMode.Nfd,
            maximumRuneCount);
        int count = normalized.GetCount();
        System.Text.Rune[] runes = new System.Text.Rune[count];
        int[] starts = new int[count];
        int[] lengths = new int[count];
        int[] combiningClasses = new int[count];
        bool[] modifierCombiningMarks = new bool[count];
        int index = 0;

        while (index < count)
        {
            combiningClasses[index] = GetCombiningClass(
                unicodeData,
                normalized.GetScalarValue(index));
            modifierCombiningMarks[index] = IsModifierCombiningMark(
                unicodeData,
                normalized.GetScalarValue(index));
            index++;
        }

        int outputIndex = 0;
        index = 0;
        while (index < count)
        {
            if (combiningClasses[index] == 0)
            {
                CopyRune(
                    normalized,
                    index,
                    runes,
                    starts,
                    lengths,
                    outputIndex);
                index++;
                outputIndex++;
                continue;
            }

            int sequenceStart = index;
            while (index < count && combiningClasses[index] != 0)
            {
                index++;
            }

            outputIndex = ReorderSequence(
                normalized,
                combiningClasses,
                modifierCombiningMarks,
                sequenceStart,
                index,
                runes,
                starts,
                lengths,
                outputIndex);
        }

        return new RuneBuffer(
            runes,
            starts,
            lengths,
            normalized.GetSourceLength(),
            normalized.GetReplacementCount());
    }

    static int ReorderSequence(
        RuneBuffer input,
        int[] combiningClasses,
        bool[] modifierCombiningMarks,
        int sequenceStart,
        int sequenceEnd,
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int outputIndex)
    {
        int belowStart = FindClassStart(
            combiningClasses,
            sequenceStart,
            sequenceEnd,
            220);
        int belowEnd = FindModifierEnd(
            combiningClasses,
            modifierCombiningMarks,
            belowStart,
            sequenceEnd,
            220);
        int aboveStart = FindClassStart(
            combiningClasses,
            sequenceStart,
            sequenceEnd,
            230);
        int aboveEnd = FindModifierEnd(
            combiningClasses,
            modifierCombiningMarks,
            aboveStart,
            sequenceEnd,
            230);

        outputIndex = CopyRange(
            input,
            belowStart,
            belowEnd,
            runes,
            starts,
            lengths,
            outputIndex);
        outputIndex = CopyRange(
            input,
            aboveStart,
            aboveEnd,
            runes,
            starts,
            lengths,
            outputIndex);

        int index = sequenceStart;
        while (index < sequenceEnd)
        {
            if (combiningClasses[index] == 33)
            {
                CopyRune(
                    input,
                    index,
                    runes,
                    starts,
                    lengths,
                    outputIndex);
                outputIndex++;
            }

            index++;
        }

        index = sequenceStart;
        while (index < sequenceEnd)
        {
            bool movedModifier =
                index >= belowStart && index < belowEnd ||
                index >= aboveStart && index < aboveEnd;
            if (!movedModifier && combiningClasses[index] != 33)
            {
                CopyRune(
                    input,
                    index,
                    runes,
                    starts,
                    lengths,
                    outputIndex);
                outputIndex++;
            }

            index++;
        }

        return outputIndex;
    }

    static int FindClassStart(
        int[] combiningClasses,
        int start,
        int end,
        int targetClass)
    {
        int index = start;
        while (index < end)
        {
            if (combiningClasses[index] == targetClass)
            {
                return index;
            }

            index++;
        }

        return end;
    }

    static int FindModifierEnd(
        int[] combiningClasses,
        bool[] modifierCombiningMarks,
        int start,
        int end,
        int targetClass)
    {
        int index = start;
        while (index < end &&
            combiningClasses[index] == targetClass &&
            modifierCombiningMarks[index])
        {
            index++;
        }

        return index;
    }

    static int CopyRange(
        RuneBuffer input,
        int start,
        int end,
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int outputIndex)
    {
        int index = start;
        while (index < end)
        {
            CopyRune(
                input,
                index,
                runes,
                starts,
                lengths,
                outputIndex);
            index++;
            outputIndex++;
        }

        return outputIndex;
    }

    static void CopyRune(
        RuneBuffer input,
        int inputIndex,
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int outputIndex)
    {
        runes[outputIndex] = input.GetRune(inputIndex);
        starts[outputIndex] = input.GetUtf16Start(inputIndex);
        lengths[outputIndex] = input.GetUtf16Length(inputIndex);
    }

    static int GetCombiningClass(
        UnicodeData unicodeData,
        int codePoint)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.CanonicalCombiningClass,
            codePoint);
        return System.Int32.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    static bool IsModifierCombiningMark(
        UnicodeData unicodeData,
        int codePoint)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.ModifierCombiningMark,
            codePoint) == "Y";
    }
}
