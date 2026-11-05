namespace ttf;

internal static class UnicodeNormalizer
{
    const string RuneLimitMessage =
        "Unicode normalization exceeds the configured rune limit.";

    internal static RuneBuffer Normalize(
        RuneBuffer input,
        UnicodeData unicodeData,
        UnicodeNormalizationMode mode,
        int maximumRuneCount)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        ValidateMode(mode);

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        if (mode == UnicodeNormalizationMode.None)
        {
            if (input.GetCount() > maximumRuneCount)
            {
                throw FontLimitException.Create(RuneLimitMessage);
            }

            return input;
        }

        System.Text.NormalizationForm decompositionForm =
            GetDecompositionForm(mode);
        int decomposedCount = CountDecomposedRunes(
            input,
            decompositionForm,
            maximumRuneCount);
        System.Text.Rune[] runes = new System.Text.Rune[decomposedCount];
        int[] starts = new int[decomposedCount];
        int[] lengths = new int[decomposedCount];
        int[] combiningClasses = new int[decomposedCount];
        DecomposeAndOrder(
            input,
            unicodeData,
            decompositionForm,
            runes,
            starts,
            lengths,
            combiningClasses);

        int resultCount = decomposedCount;

        if (mode == UnicodeNormalizationMode.Nfc ||
            mode == UnicodeNormalizationMode.Nfkc)
        {
            resultCount = Compose(
                runes,
                starts,
                lengths,
                combiningClasses,
                decomposedCount);
        }

        System.Text.Rune[] resultRunes = new System.Text.Rune[resultCount];
        int[] resultStarts = new int[resultCount];
        int[] resultLengths = new int[resultCount];
        System.Array.Copy(runes, resultRunes, resultCount);
        System.Array.Copy(starts, resultStarts, resultCount);
        System.Array.Copy(lengths, resultLengths, resultCount);
        return new RuneBuffer(
            resultRunes,
            resultStarts,
            resultLengths,
            input.GetSourceLength(),
            input.GetReplacementCount());
    }

    static int CountDecomposedRunes(
        RuneBuffer input,
        System.Text.NormalizationForm decompositionForm,
        int maximumRuneCount)
    {
        int count = 0;
        int inputIndex = 0;

        while (inputIndex < input.GetCount())
        {
            string decomposed = input.GetRune(inputIndex).ToString().Normalize(
                decompositionForm);
            int sourceIndex = 0;

            while (sourceIndex < decomposed.Length)
            {
                if (count == maximumRuneCount)
                {
                    throw FontLimitException.Create(RuneLimitMessage);
                }

                sourceIndex += GetRuneLength(decomposed, sourceIndex);
                count++;
            }

            inputIndex++;
        }

        return count;
    }

    static void DecomposeAndOrder(
        RuneBuffer input,
        UnicodeData unicodeData,
        System.Text.NormalizationForm decompositionForm,
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int[] combiningClasses)
    {
        int outputIndex = 0;
        int inputIndex = 0;

        while (inputIndex < input.GetCount())
        {
            string decomposed = input.GetRune(inputIndex).ToString().Normalize(
                decompositionForm);
            int sourceIndex = 0;

            while (sourceIndex < decomposed.Length)
            {
                System.Text.Rune rune = ReadRune(decomposed, sourceIndex);
                int combiningClass = GetCombiningClass(unicodeData, rune.Value);
                runes[outputIndex] = rune;
                starts[outputIndex] = input.GetUtf16Start(inputIndex);
                lengths[outputIndex] = input.GetUtf16Length(inputIndex);
                combiningClasses[outputIndex] = combiningClass;
                OrderLastRune(
                    runes,
                    starts,
                    lengths,
                    combiningClasses,
                    outputIndex);
                outputIndex++;
                sourceIndex += rune.Utf16SequenceLength;
            }

            inputIndex++;
        }
    }

    static void OrderLastRune(
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int[] combiningClasses,
        int index)
    {
        int combiningClass = combiningClasses[index];

        if (combiningClass == 0)
        {
            return;
        }

        int current = index;

        while (current > 0 &&
            combiningClasses[current - 1] != 0 &&
            combiningClasses[current - 1] > combiningClass)
        {
            SwapRunes(runes, current - 1, current);
            SwapIntegers(starts, current - 1, current);
            SwapIntegers(lengths, current - 1, current);
            SwapIntegers(combiningClasses, current - 1, current);
            current--;
        }
    }

    static int Compose(
        System.Text.Rune[] runes,
        int[] starts,
        int[] lengths,
        int[] combiningClasses,
        int count)
    {
        int outputCount = 0;
        int starterIndex = -1;
        int lastCombiningClass = 0;
        int inputIndex = 0;

        while (inputIndex < count)
        {
            System.Text.Rune rune = runes[inputIndex];
            int combiningClass = combiningClasses[inputIndex];
            int compositeValue = -1;

            if (starterIndex >= 0 &&
                (lastCombiningClass < combiningClass ||
                    lastCombiningClass == 0))
            {
                compositeValue = GetCompositeValue(
                    runes[starterIndex],
                    rune);
            }

            if (compositeValue >= 0)
            {
                runes[starterIndex] = new System.Text.Rune(compositeValue);
                MergeRanges(
                    starts,
                    lengths,
                    starterIndex,
                    inputIndex);
            }
            else
            {
                runes[outputCount] = rune;
                starts[outputCount] = starts[inputIndex];
                lengths[outputCount] = lengths[inputIndex];
                combiningClasses[outputCount] = combiningClass;

                if (combiningClass == 0)
                {
                    starterIndex = outputCount;
                }

                lastCombiningClass = combiningClass;
                outputCount++;
            }

            inputIndex++;
        }

        return outputCount;
    }

    static int GetCompositeValue(
        System.Text.Rune starter,
        System.Text.Rune candidate)
    {
        string pair = starter.ToString() + candidate.ToString();
        string composed = pair.Normalize(System.Text.NormalizationForm.FormC);

        if (GetRuneLength(composed, 0) != composed.Length)
        {
            return -1;
        }

        return ReadRune(composed, 0).Value;
    }

    static void MergeRanges(
        int[] starts,
        int[] lengths,
        int targetIndex,
        int sourceIndex)
    {
        int start = starts[targetIndex];
        int end = start + lengths[targetIndex];
        int sourceStart = starts[sourceIndex];
        int sourceEnd = sourceStart + lengths[sourceIndex];

        if (sourceStart < start)
        {
            start = sourceStart;
        }

        if (sourceEnd > end)
        {
            end = sourceEnd;
        }

        starts[targetIndex] = start;
        lengths[targetIndex] = end - start;
    }

    static int GetCombiningClass(UnicodeData unicodeData, int codePoint)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.CanonicalCombiningClass,
            codePoint);
        return System.Int32.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    static System.Text.Rune ReadRune(string text, int index)
    {
        char first = text[index];

        if (!char.IsHighSurrogate(first))
        {
            return new System.Text.Rune(first);
        }

        char second = text[index + 1];
        int scalarValue = 0x10000 +
            ((first - 0xD800) << 10) +
            (second - 0xDC00);
        return new System.Text.Rune(scalarValue);
    }

    static int GetRuneLength(string text, int index)
    {
        return char.IsHighSurrogate(text[index]) ? 2 : 1;
    }

    static System.Text.NormalizationForm GetDecompositionForm(
        UnicodeNormalizationMode mode)
    {
        if (mode == UnicodeNormalizationMode.Nfkc ||
            mode == UnicodeNormalizationMode.Nfkd)
        {
            return System.Text.NormalizationForm.FormKD;
        }

        return System.Text.NormalizationForm.FormD;
    }

    static void SwapRunes(
        System.Text.Rune[] values,
        int first,
        int second)
    {
        System.Text.Rune temporary = values[first];
        values[first] = values[second];
        values[second] = temporary;
    }

    static void SwapIntegers(int[] values, int first, int second)
    {
        int temporary = values[first];
        values[first] = values[second];
        values[second] = temporary;
    }

    static void ValidateMode(UnicodeNormalizationMode mode)
    {
        if (mode != UnicodeNormalizationMode.None &&
            mode != UnicodeNormalizationMode.Nfc &&
            mode != UnicodeNormalizationMode.Nfd &&
            mode != UnicodeNormalizationMode.Nfkc &&
            mode != UnicodeNormalizationMode.Nfkd)
        {
            throw new System.ArgumentOutOfRangeException("mode");
        }
    }
}
