namespace ttf;

internal enum LineBreakClass
{
    Ambiguous = 0,
    Aksara = 1,
    Alphabetic = 2,
    AksaraPrebase = 3,
    AksaraStart = 4,
    BreakBoth = 5,
    BreakAfter = 6,
    BreakBefore = 7,
    MandatoryBreak = 8,
    ContingentBreak = 9,
    ConditionalJapaneseStarter = 10,
    ClosePunctuation = 11,
    CombiningMark = 12,
    CloseParenthesis = 13,
    CarriageReturn = 14,
    EmojiBase = 15,
    EmojiModifier = 16,
    Exclamation = 17,
    Glue = 18,
    HangulLv = 19,
    HangulLvt = 20,
    UnambiguousHyphen = 21,
    HebrewLetter = 22,
    Hyphen = 23,
    Ideographic = 24,
    Inseparable = 25,
    InfixNumeric = 26,
    HangulL = 27,
    HangulT = 28,
    HangulV = 29,
    LineFeed = 30,
    NextLine = 31,
    Nonstarter = 32,
    Numeric = 33,
    OpenPunctuation = 34,
    PostfixNumeric = 35,
    PrefixNumeric = 36,
    Quotation = 37,
    RegionalIndicator = 38,
    ComplexContext = 39,
    Surrogate = 40,
    Space = 41,
    BreakSymbols = 42,
    ViramaFinal = 43,
    Virama = 44,
    WordJoiner = 45,
    Unknown = 46,
    ZeroWidthSpace = 47,
    ZeroWidthJoiner = 48
}

internal enum LineBreakOpportunity
{
    Prohibited = 0,
    Allowed = 1,
    Mandatory = 2
}

internal class LineBreakMap
{
    LineBreakClass[] classes;
    LineBreakOpportunity[] opportunities;
    int sourceLength;

    internal LineBreakMap(
        LineBreakClass[] classes,
        LineBreakOpportunity[] opportunities,
        int sourceLength)
    {
        this.classes = classes;
        this.opportunities = opportunities;
        this.sourceLength = sourceLength;
    }

    internal int GetRuneCount()
    {
        return classes.Length;
    }

    internal LineBreakClass GetClass(int runeIndex)
    {
        if (runeIndex < 0 || runeIndex >= classes.Length)
        {
            throw new System.ArgumentOutOfRangeException("runeIndex");
        }

        return classes[runeIndex];
    }

    internal LineBreakOpportunity GetOpportunity(int boundaryIndex)
    {
        if (boundaryIndex < 0 || boundaryIndex >= opportunities.Length)
        {
            throw new System.ArgumentOutOfRangeException("boundaryIndex");
        }

        return opportunities[boundaryIndex];
    }

    internal bool CanBreak(int boundaryIndex)
    {
        return GetOpportunity(boundaryIndex) !=
            LineBreakOpportunity.Prohibited;
    }

    internal bool IsMandatory(int boundaryIndex)
    {
        return GetOpportunity(boundaryIndex) ==
            LineBreakOpportunity.Mandatory;
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }
}

internal static class LineBreakAlgorithm
{
    const string RuneLimitMessage =
        "Line-break analysis exceeds the configured rune limit.";

    internal static LineBreakMap Analyze(
        RuneBuffer input,
        UnicodeData unicodeData,
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

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        int count = input.GetCount();
        if (count > maximumRuneCount)
        {
            throw FontLimitException.Create(RuneLimitMessage);
        }

        int[] scalars = new int[count];
        LineBreakClass[] original = new LineBreakClass[count];
        LineBreakClass[] resolved = new LineBreakClass[count];
        bool[] combining = new bool[count];
        LoadClasses(input, unicodeData, scalars, original, resolved);
        ResolveCombiningSequences(original, resolved, combining);
        LineBreakOpportunity[] opportunities =
            new LineBreakOpportunity[count + 1];
        int boundary = 0;
        while (boundary <= count)
        {
            opportunities[boundary] = ResolveBoundary(
                boundary,
                scalars,
                original,
                resolved,
                combining,
                unicodeData);
            boundary += 1;
        }

        return new LineBreakMap(
            resolved,
            opportunities,
            input.GetSourceLength());
    }

    static void LoadClasses(
        RuneBuffer input,
        UnicodeData unicodeData,
        int[] scalars,
        LineBreakClass[] original,
        LineBreakClass[] resolved)
    {
        int index = 0;
        while (index < input.GetCount())
        {
            int scalar = input.GetScalarValue(index);
            LineBreakClass value = GetClass(unicodeData, scalar);
            scalars[index] = scalar;
            original[index] = value;
            resolved[index] = ResolveClass(value, unicodeData, scalar);
            index += 1;
        }
    }

    static void ResolveCombiningSequences(
        LineBreakClass[] original,
        LineBreakClass[] resolved,
        bool[] combining)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (resolved[index] == LineBreakClass.CombiningMark ||
                original[index] == LineBreakClass.CombiningMark ||
                original[index] == LineBreakClass.ZeroWidthJoiner)
            {
                int baseIndex = index - 1;
                while (baseIndex >= 0 &&
                    (resolved[baseIndex] == LineBreakClass.CombiningMark ||
                    original[baseIndex] == LineBreakClass.CombiningMark ||
                    original[baseIndex] == LineBreakClass.ZeroWidthJoiner))
                {
                    baseIndex -= 1;
                }

                if (baseIndex >= 0 && CanBaseCombining(resolved[baseIndex]))
                {
                    resolved[index] = resolved[baseIndex];
                    combining[index] = true;
                }
                else
                {
                    resolved[index] = LineBreakClass.Alphabetic;
                }
            }

            index += 1;
        }
    }

    static LineBreakOpportunity ResolveBoundary(
        int boundary,
        int[] scalars,
        LineBreakClass[] original,
        LineBreakClass[] classes,
        bool[] combining,
        UnicodeData unicodeData)
    {
        int count = classes.Length;
        if (boundary == count)
        {
            return LineBreakOpportunity.Mandatory;
        }

        if (boundary == 0)
        {
            return LineBreakOpportunity.Prohibited;
        }

        LineBreakClass previous = classes[boundary - 1];
        LineBreakClass current = classes[boundary];
        LineBreakClass originalPrevious = original[boundary - 1];
        LineBreakClass originalCurrent = original[boundary];

        if (originalPrevious == LineBreakClass.CarriageReturn &&
            originalCurrent == LineBreakClass.LineFeed)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (IsHardBreak(originalPrevious))
        {
            return LineBreakOpportunity.Mandatory;
        }

        if (IsHardBreak(originalCurrent))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (originalCurrent == LineBreakClass.Space ||
            originalCurrent == LineBreakClass.ZeroWidthSpace)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (FollowsZeroWidthSpace(boundary, original))
        {
            return LineBreakOpportunity.Allowed;
        }

        if (originalPrevious == LineBreakClass.ZeroWidthJoiner)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (combining[boundary])
        {
            return LineBreakOpportunity.Prohibited;
        }

        int previousIndex = PreviousSignificantIndex(
            boundary - 1,
            combining);
        previous = classes[previousIndex];

        if (previous == LineBreakClass.WordJoiner ||
            current == LineBreakClass.WordJoiner)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.Glue)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (current == LineBreakClass.Glue &&
            previous != LineBreakClass.Space &&
            previous != LineBreakClass.BreakAfter &&
            previous != LineBreakClass.Hyphen &&
            previous != LineBreakClass.UnambiguousHyphen)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (current == LineBreakClass.ClosePunctuation ||
            current == LineBreakClass.CloseParenthesis ||
            current == LineBreakClass.Exclamation ||
            current == LineBreakClass.BreakSymbols)
        {
            return LineBreakOpportunity.Prohibited;
        }

        int previousNonspace = PreviousNonspace(
            boundary,
            classes,
            combining);
        if (previousNonspace >= 0 &&
            classes[previousNonspace] == LineBreakClass.OpenPunctuation)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (MatchesInitialPunctuation(
            boundary,
            previousNonspace,
            scalars,
            classes,
            combining,
            unicodeData))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (MatchesFinalPunctuation(
            boundary,
            scalars,
            classes,
            combining,
            unicodeData))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.Space &&
            current == LineBreakClass.InfixNumeric &&
            boundary + 1 < count &&
            classes[boundary + 1] == LineBreakClass.Numeric)
        {
            return LineBreakOpportunity.Allowed;
        }

        if (current == LineBreakClass.InfixNumeric)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previousNonspace >= 0 &&
            (classes[previousNonspace] == LineBreakClass.ClosePunctuation ||
            classes[previousNonspace] == LineBreakClass.CloseParenthesis) &&
            current == LineBreakClass.Nonstarter)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previousNonspace >= 0 &&
            classes[previousNonspace] == LineBreakClass.BreakBoth &&
            current == LineBreakClass.BreakBoth)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.Space)
        {
            return LineBreakOpportunity.Allowed;
        }

        if (MatchesQuotationRules(
            boundary,
            scalars,
            classes,
            combining,
            unicodeData))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.ContingentBreak ||
            current == LineBreakClass.ContingentBreak)
        {
            return LineBreakOpportunity.Allowed;
        }

        if (MatchesWordInitialHyphen(boundary, classes, combining))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (current == LineBreakClass.BreakAfter ||
            current == LineBreakClass.UnambiguousHyphen ||
            current == LineBreakClass.Hyphen ||
            current == LineBreakClass.Nonstarter ||
            previous == LineBreakClass.BreakBefore)
        {
            return LineBreakOpportunity.Prohibited;
        }

        int beforePrevious = PreviousSignificantIndex(
            previousIndex - 1,
            combining);
        if (beforePrevious >= 0 &&
            classes[beforePrevious] == LineBreakClass.HebrewLetter &&
            (previous == LineBreakClass.Hyphen ||
            previous == LineBreakClass.UnambiguousHyphen) &&
            current != LineBreakClass.HebrewLetter)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.BreakSymbols &&
            current == LineBreakClass.HebrewLetter)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (current == LineBreakClass.Inseparable)
        {
            return LineBreakOpportunity.Prohibited;
        }

        if ((IsAlphabetic(previous) && current == LineBreakClass.Numeric) ||
            (previous == LineBreakClass.Numeric && IsAlphabetic(current)))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if ((previous == LineBreakClass.PrefixNumeric &&
            IsIdeographicEmoji(current)) ||
            (IsIdeographicEmoji(previous) &&
            current == LineBreakClass.PostfixNumeric))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if ((IsNumericAffix(previous) && IsAlphabetic(current)) ||
            (IsAlphabetic(previous) && IsNumericAffix(current)))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (MatchesNumericExpression(boundary, classes))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (MatchesHangul(previous, current))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if ((IsHangul(previous) && current == LineBreakClass.PostfixNumeric) ||
            (previous == LineBreakClass.PrefixNumeric && IsHangul(current)))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (IsAlphabetic(previous) && IsAlphabetic(current))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (MatchesBrahmic(boundary, scalars, classes, combining))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.InfixNumeric && IsAlphabetic(current))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if ((IsLetterOrNumber(previous) &&
            current == LineBreakClass.OpenPunctuation &&
            !IsEastAsian(unicodeData, scalars[boundary])) ||
            (previous == LineBreakClass.CloseParenthesis &&
            !IsEastAsian(unicodeData, scalars[previousIndex]) &&
            IsLetterOrNumber(current)))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (previous == LineBreakClass.RegionalIndicator &&
            current == LineBreakClass.RegionalIndicator &&
            HasOddRegionalIndicatorCount(boundary, classes, combining))
        {
            return LineBreakOpportunity.Prohibited;
        }

        if (current == LineBreakClass.EmojiModifier &&
            (previous == LineBreakClass.EmojiBase ||
            IsExtendedPictographicOrUnassigned(
                unicodeData,
                scalars[previousIndex])))
        {
            return LineBreakOpportunity.Prohibited;
        }

        return LineBreakOpportunity.Allowed;
    }

    static bool MatchesInitialPunctuation(
        int boundary,
        int quoteIndex,
        int[] scalars,
        LineBreakClass[] classes,
        bool[] combining,
        UnicodeData unicodeData)
    {
        if (quoteIndex < 0 ||
            !IsInitialPunctuation(unicodeData, scalars[quoteIndex]))
        {
            return false;
        }

        int contextIndex = PreviousSignificantIndex(
            quoteIndex - 1,
            combining);
        if (contextIndex < 0)
        {
            return true;
        }

        LineBreakClass context = classes[contextIndex];
        return IsHardBreak(context) ||
            context == LineBreakClass.OpenPunctuation ||
            context == LineBreakClass.Quotation ||
            context == LineBreakClass.Glue ||
            context == LineBreakClass.Space ||
            context == LineBreakClass.ZeroWidthSpace;
    }

    static bool MatchesFinalPunctuation(
        int boundary,
        int[] scalars,
        LineBreakClass[] classes,
        bool[] combining,
        UnicodeData unicodeData)
    {
        if (!IsFinalPunctuation(unicodeData, scalars[boundary]))
        {
            return false;
        }

        int nextIndex = NextSignificantIndex(
            boundary + 1,
            combining);
        if (nextIndex >= classes.Length)
        {
            return true;
        }

        LineBreakClass next = classes[nextIndex];
        return next == LineBreakClass.Space ||
            next == LineBreakClass.Glue ||
            next == LineBreakClass.WordJoiner ||
            next == LineBreakClass.ClosePunctuation ||
            next == LineBreakClass.Quotation ||
            next == LineBreakClass.CloseParenthesis ||
            next == LineBreakClass.Exclamation ||
            next == LineBreakClass.InfixNumeric ||
            next == LineBreakClass.BreakSymbols ||
            IsHardBreak(next) ||
            next == LineBreakClass.ZeroWidthSpace;
    }

    static bool MatchesQuotationRules(
        int boundary,
        int[] scalars,
        LineBreakClass[] classes,
        bool[] combining,
        UnicodeData unicodeData)
    {
        int previousIndex = boundary - 1;
        while (previousIndex >= 0 && combining[previousIndex])
        {
            previousIndex -= 1;
        }
        bool previousQuotation =
            classes[previousIndex] == LineBreakClass.Quotation;
        bool currentQuotation =
            classes[boundary] == LineBreakClass.Quotation;

        if (currentQuotation &&
            !IsInitialPunctuation(unicodeData, scalars[boundary]))
        {
            return true;
        }

        if (previousQuotation &&
            !IsFinalPunctuation(unicodeData, scalars[previousIndex]))
        {
            return true;
        }

        if (currentQuotation)
        {
            bool previousEastAsian = IsEastAsian(
                unicodeData,
                scalars[previousIndex]);
            bool nextEastAsian = boundary + 1 < classes.Length &&
                IsEastAsian(unicodeData, scalars[boundary + 1]);
            if (!previousEastAsian || !nextEastAsian)
            {
                return true;
            }
        }

        if (previousQuotation)
        {
            bool currentEastAsian = IsEastAsian(
                unicodeData,
                scalars[boundary]);
            bool beforeEastAsian = previousIndex > 0 &&
                IsEastAsian(unicodeData, scalars[previousIndex - 1]);
            if (!currentEastAsian || !beforeEastAsian)
            {
                return true;
            }
        }

        return false;
    }

    static bool MatchesWordInitialHyphen(
        int boundary,
        LineBreakClass[] classes,
        bool[] combining)
    {
        int previousIndex = PreviousSignificantIndex(
            boundary - 1,
            combining);
        if (previousIndex < 0 ||
            (classes[previousIndex] != LineBreakClass.Hyphen &&
            classes[previousIndex] != LineBreakClass.UnambiguousHyphen) ||
            !IsAlphabetic(classes[boundary]))
        {
            return false;
        }

        int contextIndex = PreviousSignificantIndex(
            previousIndex - 1,
            combining);
        if (contextIndex < 0)
        {
            return true;
        }

        LineBreakClass context = classes[contextIndex];
        return IsHardBreak(context) ||
            context == LineBreakClass.Space ||
            context == LineBreakClass.ZeroWidthSpace ||
            context == LineBreakClass.ContingentBreak ||
            context == LineBreakClass.Glue;
    }

    static bool MatchesNumericExpression(
        int boundary,
        LineBreakClass[] classes)
    {
        LineBreakClass previous = classes[boundary - 1];
        LineBreakClass current = classes[boundary];

        if ((previous == LineBreakClass.ClosePunctuation ||
            previous == LineBreakClass.CloseParenthesis) &&
            IsNumericAffix(current))
        {
            int index = boundary - 2;
            while (index >= 0 && IsNumericSeparator(classes[index]))
            {
                index -= 1;
            }

            if (index >= 0 && classes[index] == LineBreakClass.Numeric)
            {
                return true;
            }
        }

        if (IsNumericAffix(current))
        {
            int index = boundary - 1;
            while (index >= 0 && IsNumericSeparator(classes[index]))
            {
                index -= 1;
            }

            if (index >= 0 && classes[index] == LineBreakClass.Numeric)
            {
                return true;
            }
        }

        if (IsNumericAffix(previous))
        {
            if (current == LineBreakClass.Numeric)
            {
                return true;
            }

            if (current == LineBreakClass.OpenPunctuation)
            {
                int next = boundary + 1;
                if (next < classes.Length &&
                    classes[next] == LineBreakClass.Numeric)
                {
                    return true;
                }

                if (next + 1 < classes.Length &&
                    classes[next] == LineBreakClass.InfixNumeric &&
                    classes[next + 1] == LineBreakClass.Numeric)
                {
                    return true;
                }
            }
        }

        if ((previous == LineBreakClass.Hyphen ||
            previous == LineBreakClass.InfixNumeric) &&
            current == LineBreakClass.Numeric)
        {
            return true;
        }

        if (current == LineBreakClass.Numeric)
        {
            int index = boundary - 1;
            while (index >= 0 && IsNumericSeparator(classes[index]))
            {
                index -= 1;
            }

            if (index >= 0 && classes[index] == LineBreakClass.Numeric)
            {
                return true;
            }
        }

        return false;
    }

    static bool MatchesHangul(
        LineBreakClass previous,
        LineBreakClass current)
    {
        if (previous == LineBreakClass.HangulL)
        {
            return current == LineBreakClass.HangulL ||
                current == LineBreakClass.HangulV ||
                current == LineBreakClass.HangulLv ||
                current == LineBreakClass.HangulLvt;
        }

        if (previous == LineBreakClass.HangulV ||
            previous == LineBreakClass.HangulLv)
        {
            return current == LineBreakClass.HangulV ||
                current == LineBreakClass.HangulT;
        }

        if (previous == LineBreakClass.HangulT ||
            previous == LineBreakClass.HangulLvt)
        {
            return current == LineBreakClass.HangulT;
        }

        return false;
    }

    static bool MatchesBrahmic(
        int boundary,
        int[] scalars,
        LineBreakClass[] classes,
        bool[] combining)
    {
        int previousIndex = PreviousSignificantIndex(
            boundary - 1,
            combining);
        LineBreakClass previous = classes[previousIndex];
        LineBreakClass current = classes[boundary];
        bool previousBase = IsBrahmicBase(
            previous,
            scalars[previousIndex]);
        bool currentBase = IsBrahmicBase(current, scalars[boundary]);

        if (previous == LineBreakClass.AksaraPrebase && currentBase)
        {
            return true;
        }

        if (previousBase &&
            (current == LineBreakClass.ViramaFinal ||
            current == LineBreakClass.Virama))
        {
            return true;
        }

        int beforePrevious = PreviousSignificantIndex(
            previousIndex - 1,
            combining);
        if (beforePrevious >= 0 &&
            classes[previousIndex] == LineBreakClass.Virama &&
            IsBrahmicBase(
                classes[beforePrevious],
                scalars[beforePrevious]) &&
            (current == LineBreakClass.Aksara ||
            scalars[boundary] == 0x25CC))
        {
            return true;
        }

        int nextIndex = NextSignificantIndex(boundary + 1, combining);
        return previousBase && currentBase &&
            nextIndex < classes.Length &&
            classes[nextIndex] == LineBreakClass.ViramaFinal;
    }

    static int PreviousNonspace(
        int boundary,
        LineBreakClass[] classes,
        bool[] combining)
    {
        int index = boundary - 1;
        while (index >= 0 &&
            (classes[index] == LineBreakClass.Space || combining[index]))
        {
            index -= 1;
        }

        return index;
    }

    static int PreviousSignificantIndex(int index, bool[] combining)
    {
        while (index >= 0 && combining[index])
        {
            index -= 1;
        }

        return index;
    }

    static int NextSignificantIndex(int index, bool[] combining)
    {
        while (index < combining.Length && combining[index])
        {
            index += 1;
        }

        return index;
    }

    static bool FollowsZeroWidthSpace(
        int boundary,
        LineBreakClass[] original)
    {
        int index = boundary - 1;
        while (index >= 0 && original[index] == LineBreakClass.Space)
        {
            index -= 1;
        }

        return index >= 0 &&
            original[index] == LineBreakClass.ZeroWidthSpace;
    }

    static bool HasOddRegionalIndicatorCount(
        int boundary,
        LineBreakClass[] classes,
        bool[] combining)
    {
        int count = 0;
        int index = PreviousSignificantIndex(boundary - 1, combining);
        while (index >= 0 &&
            classes[index] == LineBreakClass.RegionalIndicator)
        {
            count += 1;
            index = PreviousSignificantIndex(index - 1, combining);
        }

        return (count & 1) != 0;
    }

    static bool CanBaseCombining(LineBreakClass value)
    {
        return !IsHardBreak(value) &&
            value != LineBreakClass.Space &&
            value != LineBreakClass.ZeroWidthSpace;
    }

    static bool IsHardBreak(LineBreakClass value)
    {
        return value == LineBreakClass.MandatoryBreak ||
            value == LineBreakClass.CarriageReturn ||
            value == LineBreakClass.LineFeed ||
            value == LineBreakClass.NextLine;
    }

    static bool IsAlphabetic(LineBreakClass value)
    {
        return value == LineBreakClass.Alphabetic ||
            value == LineBreakClass.HebrewLetter;
    }

    static bool IsLetterOrNumber(LineBreakClass value)
    {
        return IsAlphabetic(value) || value == LineBreakClass.Numeric;
    }

    static bool IsNumericAffix(LineBreakClass value)
    {
        return value == LineBreakClass.PrefixNumeric ||
            value == LineBreakClass.PostfixNumeric;
    }

    static bool IsNumericSeparator(LineBreakClass value)
    {
        return value == LineBreakClass.BreakSymbols ||
            value == LineBreakClass.InfixNumeric;
    }

    static bool IsIdeographicEmoji(LineBreakClass value)
    {
        return value == LineBreakClass.Ideographic ||
            value == LineBreakClass.EmojiBase ||
            value == LineBreakClass.EmojiModifier;
    }

    static bool IsHangul(LineBreakClass value)
    {
        return value == LineBreakClass.HangulL ||
            value == LineBreakClass.HangulV ||
            value == LineBreakClass.HangulT ||
            value == LineBreakClass.HangulLv ||
            value == LineBreakClass.HangulLvt;
    }

    static bool IsBrahmicBase(LineBreakClass value, int scalar)
    {
        return value == LineBreakClass.Aksara ||
            value == LineBreakClass.AksaraStart ||
            scalar == 0x25CC;
    }

    static bool IsInitialPunctuation(
        UnicodeData unicodeData,
        int scalar)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.GeneralCategory,
            scalar) == "Pi";
    }

    static bool IsFinalPunctuation(
        UnicodeData unicodeData,
        int scalar)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.GeneralCategory,
            scalar) == "Pf";
    }

    static bool IsEastAsian(UnicodeData unicodeData, int scalar)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.EastAsianWidth,
            scalar);
        return value == "F" || value == "W" || value == "H";
    }

    static bool IsExtendedPictographicOrUnassigned(
        UnicodeData unicodeData,
        int scalar)
    {
        return unicodeData.GetPropertyName(
            UnicodeDataTags.ExtendedPictographic,
            scalar) == "Y" &&
            unicodeData.GetPropertyName(
                UnicodeDataTags.GeneralCategory,
                scalar) == "Cn";
    }

    static LineBreakClass ResolveClass(
        LineBreakClass value,
        UnicodeData unicodeData,
        int scalar)
    {
        if (value == LineBreakClass.Ambiguous ||
            value == LineBreakClass.Surrogate ||
            value == LineBreakClass.Unknown)
        {
            return LineBreakClass.Alphabetic;
        }

        if (value == LineBreakClass.ConditionalJapaneseStarter)
        {
            return LineBreakClass.Nonstarter;
        }

        if (value == LineBreakClass.ComplexContext)
        {
            string category = unicodeData.GetPropertyName(
                UnicodeDataTags.GeneralCategory,
                scalar);
            if (category == "Mn" || category == "Mc")
            {
                return LineBreakClass.CombiningMark;
            }

            return LineBreakClass.Alphabetic;
        }

        return value;
    }

    static LineBreakClass GetClass(UnicodeData unicodeData, int scalar)
    {
        string name = unicodeData.GetPropertyName(
            UnicodeDataTags.LineBreak,
            scalar);
        if (name == "AI") { return LineBreakClass.Ambiguous; }
        if (name == "AK") { return LineBreakClass.Aksara; }
        if (name == "AL") { return LineBreakClass.Alphabetic; }
        if (name == "AP") { return LineBreakClass.AksaraPrebase; }
        if (name == "AS") { return LineBreakClass.AksaraStart; }
        if (name == "B2") { return LineBreakClass.BreakBoth; }
        if (name == "BA") { return LineBreakClass.BreakAfter; }
        if (name == "BB") { return LineBreakClass.BreakBefore; }
        if (name == "BK") { return LineBreakClass.MandatoryBreak; }
        if (name == "CB") { return LineBreakClass.ContingentBreak; }
        if (name == "CJ") { return LineBreakClass.ConditionalJapaneseStarter; }
        if (name == "CL") { return LineBreakClass.ClosePunctuation; }
        if (name == "CM") { return LineBreakClass.CombiningMark; }
        if (name == "CP") { return LineBreakClass.CloseParenthesis; }
        if (name == "CR") { return LineBreakClass.CarriageReturn; }
        if (name == "EB") { return LineBreakClass.EmojiBase; }
        if (name == "EM") { return LineBreakClass.EmojiModifier; }
        if (name == "EX") { return LineBreakClass.Exclamation; }
        if (name == "GL") { return LineBreakClass.Glue; }
        if (name == "H2") { return LineBreakClass.HangulLv; }
        if (name == "H3") { return LineBreakClass.HangulLvt; }
        if (name == "HH") { return LineBreakClass.UnambiguousHyphen; }
        if (name == "HL") { return LineBreakClass.HebrewLetter; }
        if (name == "HY") { return LineBreakClass.Hyphen; }
        if (name == "ID") { return LineBreakClass.Ideographic; }
        if (name == "IN") { return LineBreakClass.Inseparable; }
        if (name == "IS") { return LineBreakClass.InfixNumeric; }
        if (name == "JL") { return LineBreakClass.HangulL; }
        if (name == "JT") { return LineBreakClass.HangulT; }
        if (name == "JV") { return LineBreakClass.HangulV; }
        if (name == "LF") { return LineBreakClass.LineFeed; }
        if (name == "NL") { return LineBreakClass.NextLine; }
        if (name == "NS") { return LineBreakClass.Nonstarter; }
        if (name == "NU") { return LineBreakClass.Numeric; }
        if (name == "OP") { return LineBreakClass.OpenPunctuation; }
        if (name == "PO") { return LineBreakClass.PostfixNumeric; }
        if (name == "PR") { return LineBreakClass.PrefixNumeric; }
        if (name == "QU") { return LineBreakClass.Quotation; }
        if (name == "RI") { return LineBreakClass.RegionalIndicator; }
        if (name == "SA") { return LineBreakClass.ComplexContext; }
        if (name == "SG") { return LineBreakClass.Surrogate; }
        if (name == "SP") { return LineBreakClass.Space; }
        if (name == "SY") { return LineBreakClass.BreakSymbols; }
        if (name == "VF") { return LineBreakClass.ViramaFinal; }
        if (name == "VI") { return LineBreakClass.Virama; }
        if (name == "WJ") { return LineBreakClass.WordJoiner; }
        if (name == "XX") { return LineBreakClass.Unknown; }
        if (name == "ZW") { return LineBreakClass.ZeroWidthSpace; }
        if (name == "ZWJ") { return LineBreakClass.ZeroWidthJoiner; }
        throw FontFormatException.Create(
            "The Unicode line-break class is not supported.");
    }
}
