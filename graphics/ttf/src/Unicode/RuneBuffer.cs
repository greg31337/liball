namespace ttf;

internal class RuneBuffer
{
    System.Text.Rune[] runes;
    int[] utf16Starts;
    int[] utf16Lengths;
    int sourceLength;
    int replacementCount;

    internal RuneBuffer(
        System.Text.Rune[] runes,
        int[] utf16Starts,
        int[] utf16Lengths,
        int sourceLength,
        int replacementCount)
    {
        this.runes = runes;
        this.utf16Starts = utf16Starts;
        this.utf16Lengths = utf16Lengths;
        this.sourceLength = sourceLength;
        this.replacementCount = replacementCount;
    }

    internal int GetCount()
    {
        return runes.Length;
    }

    internal System.Text.Rune GetRune(int index)
    {
        ValidateIndex(index);
        return runes[index];
    }

    internal int GetScalarValue(int index)
    {
        return GetRune(index).Value;
    }

    internal int GetUtf16Start(int index)
    {
        ValidateIndex(index);
        return utf16Starts[index];
    }

    internal int GetUtf16Length(int index)
    {
        ValidateIndex(index);
        return utf16Lengths[index];
    }

    internal int GetUtf16End(int index)
    {
        return GetUtf16Start(index) + GetUtf16Length(index);
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    internal int GetReplacementCount()
    {
        return replacementCount;
    }

    internal bool HadReplacements()
    {
        return replacementCount != 0;
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= runes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal static class Utf16Decoder
{
    const string RuneLimitMessage =
        "UTF-16 decoding exceeds the configured rune limit.";
    const string UnpairedSurrogateMessage =
        "The UTF-16 text contains an unpaired surrogate.";

    internal static RuneBuffer Decode(
        string text,
        Utf16DecodingMode mode,
        int maximumRuneCount)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        ValidateMode(mode);

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        int runeCount = CountRunes(text, mode, maximumRuneCount);
        System.Text.Rune[] runes = new System.Text.Rune[runeCount];
        int[] starts = new int[runeCount];
        int[] lengths = new int[runeCount];
        int sourceIndex = 0;
        int runeIndex = 0;
        int replacementCount = 0;

        while (sourceIndex < text.Length)
        {
            char first = text[sourceIndex];

            if (char.IsHighSurrogate(first) &&
                sourceIndex + 1 < text.Length &&
                char.IsLowSurrogate(text[sourceIndex + 1]))
            {
                char second = text[sourceIndex + 1];
                int scalarValue = 0x10000 +
                    ((first - 0xD800) << 10) +
                    (second - 0xDC00);
                runes[runeIndex] = new System.Text.Rune(scalarValue);
                starts[runeIndex] = sourceIndex;
                lengths[runeIndex] = 2;
                sourceIndex += 2;
            }
            else if (char.IsSurrogate(first))
            {
                runes[runeIndex] = System.Text.Rune.ReplacementChar;
                starts[runeIndex] = sourceIndex;
                lengths[runeIndex] = 1;
                replacementCount++;
                sourceIndex++;
            }
            else
            {
                runes[runeIndex] = new System.Text.Rune(first);
                starts[runeIndex] = sourceIndex;
                lengths[runeIndex] = 1;
                sourceIndex++;
            }

            runeIndex++;
        }

        return new RuneBuffer(
            runes,
            starts,
            lengths,
            text.Length,
            replacementCount);
    }

    static int CountRunes(
        string text,
        Utf16DecodingMode mode,
        int maximumRuneCount)
    {
        int sourceIndex = 0;
        int runeCount = 0;

        while (sourceIndex < text.Length)
        {
            char first = text[sourceIndex];

            if (char.IsHighSurrogate(first) &&
                sourceIndex + 1 < text.Length &&
                char.IsLowSurrogate(text[sourceIndex + 1]))
            {
                sourceIndex += 2;
            }
            else
            {
                if (char.IsSurrogate(first) &&
                    mode == Utf16DecodingMode.Strict)
                {
                    throw new System.Text.DecoderFallbackException(
                        UnpairedSurrogateMessage + " Index: " + sourceIndex + ".");
                }

                sourceIndex++;
            }

            if (runeCount == maximumRuneCount)
            {
                throw FontLimitException.Create(RuneLimitMessage);
            }

            runeCount++;
        }

        return runeCount;
    }

    static void ValidateMode(Utf16DecodingMode mode)
    {
        if (mode != Utf16DecodingMode.Strict &&
            mode != Utf16DecodingMode.Replacement)
        {
            throw new System.ArgumentOutOfRangeException("mode");
        }
    }
}
