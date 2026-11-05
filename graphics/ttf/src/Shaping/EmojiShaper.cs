namespace ttf;

internal enum EmojiPresentation
{
    Text = 0,
    Emoji = 1
}

internal enum EmojiSequenceType
{
    Text = 0,
    EmojiCharacter = 1,
    Variation = 2,
    Modifier = 3,
    Keycap = 4,
    RegionalIndicator = 5,
    Tag = 6,
    ZeroWidthJoiner = 7
}

internal class EmojiSequenceMap
{
    int[] runeSequenceIndexes;
    int[] starts;
    int[] lengths;
    EmojiSequenceType[] types;
    EmojiPresentation[] presentations;
    bool[] explicitPresentations;

    internal EmojiSequenceMap(
        int[] runeSequenceIndexes,
        int[] starts,
        int[] lengths,
        EmojiSequenceType[] types,
        EmojiPresentation[] presentations,
        bool[] explicitPresentations)
    {
        this.runeSequenceIndexes = runeSequenceIndexes;
        this.starts = starts;
        this.lengths = lengths;
        this.types = types;
        this.presentations = presentations;
        this.explicitPresentations = explicitPresentations;
    }

    internal int GetRuneCount()
    {
        return runeSequenceIndexes.Length;
    }

    internal int GetSequenceCount()
    {
        return types.Length;
    }

    internal int GetRuneSequenceIndex(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return runeSequenceIndexes[runeIndex];
    }

    internal int GetSequenceStart(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return starts[sequenceIndex];
    }

    internal int GetSequenceLength(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return lengths[sequenceIndex];
    }

    internal int GetSequenceEnd(int sequenceIndex)
    {
        return GetSequenceStart(sequenceIndex) +
            GetSequenceLength(sequenceIndex);
    }

    internal EmojiSequenceType GetSequenceType(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return types[sequenceIndex];
    }

    internal EmojiPresentation GetPresentation(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return presentations[sequenceIndex];
    }

    internal bool HasExplicitPresentation(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return explicitPresentations[sequenceIndex];
    }

    internal EmojiPresentation GetRunePresentation(int runeIndex)
    {
        return GetPresentation(GetRuneSequenceIndex(runeIndex));
    }

    void ValidateRuneIndex(int index)
    {
        if (index < 0 || index >= runeSequenceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("runeIndex");
        }
    }

    void ValidateSequenceIndex(int index)
    {
        if (index < 0 || index >= types.Length)
        {
            throw new System.ArgumentOutOfRangeException("sequenceIndex");
        }
    }
}

internal class EmojiSequenceMatch
{
    int end;
    EmojiSequenceType type;
    EmojiPresentation presentation;
    bool explicitPresentation;

    internal EmojiSequenceMatch(
        int end,
        EmojiSequenceType type,
        EmojiPresentation presentation,
        bool explicitPresentation)
    {
        this.end = end;
        this.type = type;
        this.presentation = presentation;
        this.explicitPresentation = explicitPresentation;
    }

    internal int GetEnd()
    {
        return end;
    }

    internal EmojiSequenceType GetTypeValue()
    {
        return type;
    }

    internal EmojiPresentation GetPresentation()
    {
        return presentation;
    }

    internal bool HasExplicitPresentation()
    {
        return explicitPresentation;
    }
}

internal static class EmojiShaper
{
    const int TextVariationSelector = 0xFE0E;
    const int EmojiVariationSelector = 0xFE0F;
    const int CombiningEnclosingKeycap = 0x20E3;
    const int ZeroWidthJoiner = 0x200D;
    const int CancelTag = 0xE007F;
    const string SequenceLimitMessage =
        "Emoji shaping exceeds the configured sequence limit.";

    internal static EmojiSequenceMap Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int maximumSequenceCount)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (maximumSequenceCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSequenceCount");
        }

        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<EmojiSequenceType> types =
            new System.Collections.Generic.List<EmojiSequenceType>();
        System.Collections.Generic.List<EmojiPresentation> presentations =
            new System.Collections.Generic.List<EmojiPresentation>();
        System.Collections.Generic.List<bool> explicitPresentations =
            new System.Collections.Generic.List<bool>();
        int[] runeSequenceIndexes = new int[buffer.GetCount()];
        int index = 0;
        while (index < buffer.GetCount())
        {
            if (types.Count >= maximumSequenceCount)
            {
                throw FontLimitException.Create(SequenceLimitMessage);
            }

            EmojiSequenceMatch match = MatchSequence(
                buffer,
                unicodeData,
                index);
            int end = match.GetEnd();
            int sequenceIndex = types.Count;
            starts.Add(index);
            lengths.Add(end - index);
            types.Add(match.GetTypeValue());
            presentations.Add(match.GetPresentation());
            explicitPresentations.Add(match.HasExplicitPresentation());
            int runeIndex = index;
            while (runeIndex < end)
            {
                runeSequenceIndexes[runeIndex] = sequenceIndex;
                bool defaultIgnorable = HasProperty(
                    unicodeData,
                    UnicodeDataTags.DefaultIgnorable,
                    buffer.GetScalarValue(runeIndex));
                buffer.SetDefaultIgnorable(runeIndex, defaultIgnorable);
                if (end - index > 1)
                {
                    buffer.SetUnsafeToBreak(runeIndex, true);
                }

                runeIndex++;
            }

            if (end - index > 1)
            {
                buffer.MergeClusters(index, end);
            }

            index = end;
        }

        return new EmojiSequenceMap(
            runeSequenceIndexes,
            CopyIntegers(starts),
            CopyIntegers(lengths),
            CopyTypes(types),
            CopyPresentations(presentations),
            CopyBooleans(explicitPresentations));
    }

    static EmojiSequenceMatch MatchSequence(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        EmojiSequenceMatch? match = MatchZeroWidthJoiner(
            buffer,
            unicodeData,
            start);
        if (match != null)
        {
            return match;
        }

        match = MatchTag(buffer, unicodeData, start);
        if (match != null)
        {
            return match;
        }

        match = MatchKeycap(buffer, start);
        if (match != null)
        {
            return match;
        }

        match = MatchModifier(buffer, unicodeData, start);
        if (match != null)
        {
            return match;
        }

        match = MatchRegionalIndicator(buffer, unicodeData, start);
        if (match != null)
        {
            return match;
        }

        match = MatchVariation(buffer, unicodeData, start);
        if (match != null)
        {
            return match;
        }

        int codePoint = buffer.GetScalarValue(start);
        if (HasProperty(unicodeData, UnicodeDataTags.Emoji, codePoint))
        {
            EmojiPresentation presentation = HasProperty(
                unicodeData,
                UnicodeDataTags.EmojiPresentation,
                codePoint) ? EmojiPresentation.Emoji :
                EmojiPresentation.Text;
            return new EmojiSequenceMatch(
                start + 1,
                EmojiSequenceType.EmojiCharacter,
                presentation,
                false);
        }

        return new EmojiSequenceMatch(
            start + 1,
            EmojiSequenceType.Text,
            EmojiPresentation.Text,
            false);
    }

    static EmojiSequenceMatch? MatchVariation(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        if (!HasProperty(
            unicodeData,
            UnicodeDataTags.Emoji,
            buffer.GetScalarValue(start)) || start + 1 >= buffer.GetCount())
        {
            return null;
        }

        int selector = buffer.GetScalarValue(start + 1);
        if (selector != TextVariationSelector &&
            selector != EmojiVariationSelector)
        {
            return null;
        }

        return new EmojiSequenceMatch(
            start + 2,
            EmojiSequenceType.Variation,
            selector == EmojiVariationSelector ?
                EmojiPresentation.Emoji : EmojiPresentation.Text,
            true);
    }

    static EmojiSequenceMatch? MatchModifier(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        int codePoint = buffer.GetScalarValue(start);
        if (!HasProperty(
            unicodeData,
            UnicodeDataTags.EmojiModifierBase,
            codePoint))
        {
            return null;
        }

        int index = start + 1;
        bool explicitPresentation = false;
        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            TextVariationSelector)
        {
            return null;
        }

        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            EmojiVariationSelector)
        {
            explicitPresentation = true;
            index++;
        }

        if (index >= buffer.GetCount() || !HasProperty(
            unicodeData,
            UnicodeDataTags.EmojiModifier,
            buffer.GetScalarValue(index)))
        {
            return null;
        }

        return new EmojiSequenceMatch(
            index + 1,
            EmojiSequenceType.Modifier,
            EmojiPresentation.Emoji,
            explicitPresentation);
    }

    static EmojiSequenceMatch? MatchKeycap(
        ShapingBuffer buffer,
        int start)
    {
        if (!IsKeycapBase(buffer.GetScalarValue(start)))
        {
            return null;
        }

        int index = start + 1;
        bool explicitPresentation = false;
        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            TextVariationSelector)
        {
            return null;
        }

        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            EmojiVariationSelector)
        {
            explicitPresentation = true;
            index++;
        }

        if (index >= buffer.GetCount() ||
            buffer.GetScalarValue(index) != CombiningEnclosingKeycap)
        {
            return null;
        }

        return new EmojiSequenceMatch(
            index + 1,
            EmojiSequenceType.Keycap,
            EmojiPresentation.Emoji,
            explicitPresentation);
    }

    static EmojiSequenceMatch? MatchRegionalIndicator(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        if (!IsRegionalIndicator(unicodeData, buffer.GetScalarValue(start)) ||
            start + 1 >= buffer.GetCount() ||
            !IsRegionalIndicator(
                unicodeData,
                buffer.GetScalarValue(start + 1)))
        {
            return null;
        }

        return new EmojiSequenceMatch(
            start + 2,
            EmojiSequenceType.RegionalIndicator,
            EmojiPresentation.Emoji,
            false);
    }

    static EmojiSequenceMatch? MatchTag(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        if (!HasProperty(
            unicodeData,
            UnicodeDataTags.Emoji,
            buffer.GetScalarValue(start)))
        {
            return null;
        }

        int index = start + 1;
        int tagStart = index;
        while (index < buffer.GetCount() &&
            IsTagCharacter(buffer.GetScalarValue(index)))
        {
            index++;
        }

        if (index == tagStart || index >= buffer.GetCount() ||
            buffer.GetScalarValue(index) != CancelTag)
        {
            return null;
        }

        return new EmojiSequenceMatch(
            index + 1,
            EmojiSequenceType.Tag,
            EmojiPresentation.Emoji,
            false);
    }

    static EmojiSequenceMatch? MatchZeroWidthJoiner(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        int index = ParseEmojiElement(buffer, unicodeData, start);
        if (index < 0)
        {
            return null;
        }

        bool joined = false;
        while (index < buffer.GetCount() &&
            buffer.GetScalarValue(index) == ZeroWidthJoiner)
        {
            int next = ParseEmojiElement(buffer, unicodeData, index + 1);
            if (next < 0)
            {
                break;
            }

            joined = true;
            index = next;
        }

        if (!joined)
        {
            return null;
        }

        return new EmojiSequenceMatch(
            index,
            EmojiSequenceType.ZeroWidthJoiner,
            EmojiPresentation.Emoji,
            HasPresentationSelector(buffer, start, index));
    }

    static int ParseEmojiElement(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int start)
    {
        if (start < 0 || start >= buffer.GetCount())
        {
            return -1;
        }

        int codePoint = buffer.GetScalarValue(start);
        bool pictographic = HasProperty(
            unicodeData,
            UnicodeDataTags.ExtendedPictographic,
            codePoint);
        bool emoji = HasProperty(
            unicodeData,
            UnicodeDataTags.Emoji,
            codePoint);
        if (!pictographic && !emoji)
        {
            return -1;
        }

        int index = start + 1;
        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            TextVariationSelector)
        {
            return -1;
        }

        if (index < buffer.GetCount() && buffer.GetScalarValue(index) ==
            EmojiVariationSelector)
        {
            index++;
        }

        if (index < buffer.GetCount() &&
            HasProperty(
                unicodeData,
                UnicodeDataTags.EmojiModifier,
                buffer.GetScalarValue(index)) &&
            HasProperty(
                unicodeData,
                UnicodeDataTags.EmojiModifierBase,
                codePoint))
        {
            index++;
        }

        while (index < buffer.GetCount() &&
            IsExtend(unicodeData, buffer.GetScalarValue(index)) &&
            buffer.GetScalarValue(index) != ZeroWidthJoiner)
        {
            index++;
        }

        return index;
    }

    static bool HasPresentationSelector(
        ShapingBuffer buffer,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            if (IsPresentationSelector(buffer.GetScalarValue(index)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsRegionalIndicator(
        UnicodeData unicodeData,
        int codePoint)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.GraphemeBreak,
            codePoint) == "Regional_Indicator";
    }

    static bool IsExtend(UnicodeData unicodeData, int codePoint)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.GraphemeBreak,
            codePoint) == "Extend";
    }

    static bool IsPresentationSelector(int codePoint)
    {
        return codePoint == TextVariationSelector ||
            codePoint == EmojiVariationSelector;
    }

    static bool IsKeycapBase(int codePoint)
    {
        return codePoint == 0x0023 || codePoint == 0x002A ||
            (codePoint >= 0x0030 && codePoint <= 0x0039);
    }

    static bool IsTagCharacter(int codePoint)
    {
        return codePoint >= 0xE0020 && codePoint <= 0xE007E;
    }

    static bool HasProperty(
        UnicodeData unicodeData,
        int tag,
        int codePoint)
    {
        return unicodeData.GetPropertyName(tag, codePoint) == "Y";
    }

    static int[] CopyIntegers(
        System.Collections.Generic.List<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static EmojiSequenceType[] CopyTypes(
        System.Collections.Generic.List<EmojiSequenceType> values)
    {
        EmojiSequenceType[] result = new EmojiSequenceType[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static EmojiPresentation[] CopyPresentations(
        System.Collections.Generic.List<EmojiPresentation> values)
    {
        EmojiPresentation[] result =
            new EmojiPresentation[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static bool[] CopyBooleans(
        System.Collections.Generic.List<bool> values)
    {
        bool[] result = new bool[values.Count];
        values.CopyTo(result, 0);
        return result;
    }
}
