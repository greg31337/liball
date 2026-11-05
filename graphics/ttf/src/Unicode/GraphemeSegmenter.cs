namespace ttf;

internal enum GraphemeBreakClass
{
    Other = 0,
    CarriageReturn = 1,
    LineFeed = 2,
    Control = 3,
    Extend = 4,
    ZeroWidthJoiner = 5,
    RegionalIndicator = 6,
    Prepend = 7,
    SpacingMark = 8,
    HangulL = 9,
    HangulV = 10,
    HangulT = 11,
    HangulLv = 12,
    HangulLvt = 13
}

internal enum IndicConjunctBreakClass
{
    None = 0,
    Consonant = 1,
    Extend = 2,
    Linker = 3
}

internal class GraphemeClusterMap
{
    int[] runeStarts;
    int[] runeLengths;
    int[] utf16Starts;
    int[] utf16Lengths;
    int sourceLength;

    internal GraphemeClusterMap(
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        int sourceLength)
    {
        this.runeStarts = runeStarts;
        this.runeLengths = runeLengths;
        this.utf16Starts = utf16Starts;
        this.utf16Lengths = utf16Lengths;
        this.sourceLength = sourceLength;
    }

    internal int GetClusterCount()
    {
        return runeStarts.Length;
    }

    internal int GetRuneStart(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return runeStarts[clusterIndex];
    }

    internal int GetRuneLength(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return runeLengths[clusterIndex];
    }

    internal int GetRuneEnd(int clusterIndex)
    {
        return GetRuneStart(clusterIndex) + GetRuneLength(clusterIndex);
    }

    internal int GetUtf16Start(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return utf16Starts[clusterIndex];
    }

    internal int GetUtf16Length(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return utf16Lengths[clusterIndex];
    }

    internal int GetUtf16End(int clusterIndex)
    {
        return GetUtf16Start(clusterIndex) + GetUtf16Length(clusterIndex);
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    void ValidateClusterIndex(int clusterIndex)
    {
        if (clusterIndex < 0 || clusterIndex >= runeStarts.Length)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }
    }
}

internal static class GraphemeSegmenter
{
    const string RuneLimitMessage =
        "Grapheme segmentation exceeds the configured rune limit.";
    const string ClusterLimitMessage =
        "Grapheme segmentation exceeds the configured cluster limit.";

    internal static GraphemeClusterMap Segment(
        RuneBuffer input,
        UnicodeData unicodeData,
        int maximumRuneCount,
        int maximumClusterCount)
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

        if (maximumClusterCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumClusterCount");
        }

        int runeCount = input.GetCount();

        if (runeCount > maximumRuneCount)
        {
            throw FontLimitException.Create(RuneLimitMessage);
        }

        GraphemeBreakClass[] breakClasses =
            new GraphemeBreakClass[runeCount];
        IndicConjunctBreakClass[] conjunctClasses =
            new IndicConjunctBreakClass[runeCount];
        bool[] extendedPictographic = new bool[runeCount];
        LoadProperties(
            input,
            unicodeData,
            breakClasses,
            conjunctClasses,
            extendedPictographic);
        int clusterCount = CountClusters(
            breakClasses,
            conjunctClasses,
            extendedPictographic,
            maximumClusterCount);
        int[] runeStarts = new int[clusterCount];
        int[] runeLengths = new int[clusterCount];
        int[] utf16Starts = new int[clusterCount];
        int[] utf16Lengths = new int[clusterCount];
        FillClusters(
            input,
            breakClasses,
            conjunctClasses,
            extendedPictographic,
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths);
        return new GraphemeClusterMap(
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            input.GetSourceLength());
    }

    static void LoadProperties(
        RuneBuffer input,
        UnicodeData unicodeData,
        GraphemeBreakClass[] breakClasses,
        IndicConjunctBreakClass[] conjunctClasses,
        bool[] extendedPictographic)
    {
        int index = 0;

        while (index < input.GetCount())
        {
            int codePoint = input.GetScalarValue(index);
            breakClasses[index] = GetBreakClass(unicodeData, codePoint);
            conjunctClasses[index] = GetConjunctClass(unicodeData, codePoint);
            extendedPictographic[index] = unicodeData.GetPropertyName(
                UnicodeDataTags.ExtendedPictographic,
                codePoint) == "Y";
            index++;
        }
    }

    static int CountClusters(
        GraphemeBreakClass[] breakClasses,
        IndicConjunctBreakClass[] conjunctClasses,
        bool[] extendedPictographic,
        int maximumClusterCount)
    {
        if (breakClasses.Length == 0)
        {
            return 0;
        }

        if (maximumClusterCount == 0)
        {
            throw FontLimitException.Create(ClusterLimitMessage);
        }

        int clusterCount = 1;
        int regionalIndicatorCount =
            breakClasses[0] == GraphemeBreakClass.RegionalIndicator ? 1 : 0;
        int index = 1;

        while (index < breakClasses.Length)
        {
            if (ShouldBreak(
                breakClasses,
                conjunctClasses,
                extendedPictographic,
                index,
                regionalIndicatorCount))
            {
                if (clusterCount == maximumClusterCount)
                {
                    throw FontLimitException.Create(ClusterLimitMessage);
                }

                clusterCount++;
            }

            regionalIndicatorCount = NextRegionalIndicatorCount(
                breakClasses,
                index,
                regionalIndicatorCount);
            index++;
        }

        return clusterCount;
    }

    static void FillClusters(
        RuneBuffer input,
        GraphemeBreakClass[] breakClasses,
        IndicConjunctBreakClass[] conjunctClasses,
        bool[] extendedPictographic,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths)
    {
        if (breakClasses.Length == 0)
        {
            return;
        }

        int clusterIndex = 0;
        int clusterStart = 0;
        int regionalIndicatorCount =
            breakClasses[0] == GraphemeBreakClass.RegionalIndicator ? 1 : 0;
        int index = 1;

        while (index < breakClasses.Length)
        {
            if (ShouldBreak(
                breakClasses,
                conjunctClasses,
                extendedPictographic,
                index,
                regionalIndicatorCount))
            {
                FillCluster(
                    input,
                    clusterStart,
                    index,
                    runeStarts,
                    runeLengths,
                    utf16Starts,
                    utf16Lengths,
                    clusterIndex);
                clusterIndex++;
                clusterStart = index;
            }

            regionalIndicatorCount = NextRegionalIndicatorCount(
                breakClasses,
                index,
                regionalIndicatorCount);
            index++;
        }

        FillCluster(
            input,
            clusterStart,
            breakClasses.Length,
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            clusterIndex);
    }

    static void FillCluster(
        RuneBuffer input,
        int runeStart,
        int runeEnd,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        int clusterIndex)
    {
        int utf16Start = input.GetUtf16Start(runeStart);
        int utf16End = utf16Start + input.GetUtf16Length(runeStart);
        int index = runeStart + 1;

        while (index < runeEnd)
        {
            int candidateStart = input.GetUtf16Start(index);
            int candidateEnd = candidateStart + input.GetUtf16Length(index);

            if (candidateStart < utf16Start)
            {
                utf16Start = candidateStart;
            }

            if (candidateEnd > utf16End)
            {
                utf16End = candidateEnd;
            }

            index++;
        }

        runeStarts[clusterIndex] = runeStart;
        runeLengths[clusterIndex] = runeEnd - runeStart;
        utf16Starts[clusterIndex] = utf16Start;
        utf16Lengths[clusterIndex] = utf16End - utf16Start;
    }

    static bool ShouldBreak(
        GraphemeBreakClass[] breakClasses,
        IndicConjunctBreakClass[] conjunctClasses,
        bool[] extendedPictographic,
        int index,
        int regionalIndicatorCount)
    {
        GraphemeBreakClass previous = breakClasses[index - 1];
        GraphemeBreakClass current = breakClasses[index];

        if (previous == GraphemeBreakClass.CarriageReturn &&
            current == GraphemeBreakClass.LineFeed)
        {
            return false;
        }

        if (IsControl(previous) || IsControl(current))
        {
            return true;
        }

        if (previous == GraphemeBreakClass.HangulL &&
            (current == GraphemeBreakClass.HangulL ||
                current == GraphemeBreakClass.HangulV ||
                current == GraphemeBreakClass.HangulLv ||
                current == GraphemeBreakClass.HangulLvt))
        {
            return false;
        }

        if ((previous == GraphemeBreakClass.HangulLv ||
                previous == GraphemeBreakClass.HangulV) &&
            (current == GraphemeBreakClass.HangulV ||
                current == GraphemeBreakClass.HangulT))
        {
            return false;
        }

        if ((previous == GraphemeBreakClass.HangulLvt ||
                previous == GraphemeBreakClass.HangulT) &&
            current == GraphemeBreakClass.HangulT)
        {
            return false;
        }

        if (current == GraphemeBreakClass.Extend ||
            current == GraphemeBreakClass.ZeroWidthJoiner ||
            current == GraphemeBreakClass.SpacingMark)
        {
            return false;
        }

        if (previous == GraphemeBreakClass.Prepend)
        {
            return false;
        }

        if (MatchesIndicConjunct(conjunctClasses, index))
        {
            return false;
        }

        if (MatchesEmojiJoiner(
            breakClasses,
            extendedPictographic,
            index))
        {
            return false;
        }

        if (previous == GraphemeBreakClass.RegionalIndicator &&
            current == GraphemeBreakClass.RegionalIndicator &&
            (regionalIndicatorCount & 1) != 0)
        {
            return false;
        }

        return true;
    }

    static bool MatchesIndicConjunct(
        IndicConjunctBreakClass[] conjunctClasses,
        int index)
    {
        if (conjunctClasses[index] != IndicConjunctBreakClass.Consonant)
        {
            return false;
        }

        bool hasLinker = false;
        int candidate = index - 1;

        while (candidate >= 0 &&
            (conjunctClasses[candidate] ==
                IndicConjunctBreakClass.Extend ||
                conjunctClasses[candidate] ==
                IndicConjunctBreakClass.Linker))
        {
            if (conjunctClasses[candidate] ==
                IndicConjunctBreakClass.Linker)
            {
                hasLinker = true;
            }

            candidate--;
        }

        return hasLinker && candidate >= 0 &&
            conjunctClasses[candidate] ==
                IndicConjunctBreakClass.Consonant;
    }

    static bool MatchesEmojiJoiner(
        GraphemeBreakClass[] breakClasses,
        bool[] extendedPictographic,
        int index)
    {
        if (!extendedPictographic[index] ||
            breakClasses[index - 1] != GraphemeBreakClass.ZeroWidthJoiner)
        {
            return false;
        }

        int candidate = index - 2;

        while (candidate >= 0 &&
            breakClasses[candidate] == GraphemeBreakClass.Extend)
        {
            candidate--;
        }

        return candidate >= 0 && extendedPictographic[candidate];
    }

    static int NextRegionalIndicatorCount(
        GraphemeBreakClass[] breakClasses,
        int index,
        int regionalIndicatorCount)
    {
        if (breakClasses[index] != GraphemeBreakClass.RegionalIndicator)
        {
            return 0;
        }

        if (breakClasses[index - 1] ==
            GraphemeBreakClass.RegionalIndicator)
        {
            return regionalIndicatorCount + 1;
        }

        return 1;
    }

    static bool IsControl(GraphemeBreakClass value)
    {
        return value == GraphemeBreakClass.Control ||
            value == GraphemeBreakClass.CarriageReturn ||
            value == GraphemeBreakClass.LineFeed;
    }

    static GraphemeBreakClass GetBreakClass(
        UnicodeData unicodeData,
        int codePoint)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.GraphemeBreak,
            codePoint);

        if (value == "CR")
        {
            return GraphemeBreakClass.CarriageReturn;
        }

        if (value == "LF")
        {
            return GraphemeBreakClass.LineFeed;
        }

        if (value == "Control")
        {
            return GraphemeBreakClass.Control;
        }

        if (value == "Extend")
        {
            return GraphemeBreakClass.Extend;
        }

        if (value == "ZWJ")
        {
            return GraphemeBreakClass.ZeroWidthJoiner;
        }

        if (value == "Regional_Indicator")
        {
            return GraphemeBreakClass.RegionalIndicator;
        }

        if (value == "Prepend")
        {
            return GraphemeBreakClass.Prepend;
        }

        if (value == "SpacingMark")
        {
            return GraphemeBreakClass.SpacingMark;
        }

        if (value == "L")
        {
            return GraphemeBreakClass.HangulL;
        }

        if (value == "V")
        {
            return GraphemeBreakClass.HangulV;
        }

        if (value == "T")
        {
            return GraphemeBreakClass.HangulT;
        }

        if (value == "LV")
        {
            return GraphemeBreakClass.HangulLv;
        }

        if (value == "LVT")
        {
            return GraphemeBreakClass.HangulLvt;
        }

        return GraphemeBreakClass.Other;
    }

    static IndicConjunctBreakClass GetConjunctClass(
        UnicodeData unicodeData,
        int codePoint)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.IndicConjunctBreak,
            codePoint);

        if (value == "Consonant")
        {
            return IndicConjunctBreakClass.Consonant;
        }

        if (value == "Extend")
        {
            return IndicConjunctBreakClass.Extend;
        }

        if (value == "Linker")
        {
            return IndicConjunctBreakClass.Linker;
        }

        return IndicConjunctBreakClass.None;
    }
}
