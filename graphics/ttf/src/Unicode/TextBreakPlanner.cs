namespace ttf;

internal enum TextBreakKind
{
    Prohibited = 0,
    Ordinary = 1,
    Mandatory = 2,
    SoftHyphen = 3,
    Emergency = 4
}

internal class TextBreakMap
{
    int[] runeStarts;
    int[] runeLengths;
    int[] utf16Starts;
    int[] utf16Lengths;
    bool[] tabs;
    bool[] hardBreaks;
    bool[] softHyphens;
    bool[] nonbreaking;
    bool[] trimmableWhitespace;
    bool[] unsafeBoundaries;
    TextBreakKind[] breaks;
    int sourceLength;

    internal TextBreakMap(
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        bool[] tabs,
        bool[] hardBreaks,
        bool[] softHyphens,
        bool[] nonbreaking,
        bool[] trimmableWhitespace,
        bool[] unsafeBoundaries,
        TextBreakKind[] breaks,
        int sourceLength)
    {
        this.runeStarts = runeStarts;
        this.runeLengths = runeLengths;
        this.utf16Starts = utf16Starts;
        this.utf16Lengths = utf16Lengths;
        this.tabs = tabs;
        this.hardBreaks = hardBreaks;
        this.softHyphens = softHyphens;
        this.nonbreaking = nonbreaking;
        this.trimmableWhitespace = trimmableWhitespace;
        this.unsafeBoundaries = unsafeBoundaries;
        this.breaks = breaks;
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

    internal bool IsTab(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return tabs[clusterIndex];
    }

    internal bool IsHardBreak(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return hardBreaks[clusterIndex];
    }

    internal bool IsSoftHyphen(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return softHyphens[clusterIndex];
    }

    internal bool IsNonbreaking(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return nonbreaking[clusterIndex];
    }

    internal bool IsTrimmableWhitespace(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return trimmableWhitespace[clusterIndex];
    }

    internal bool CanTrimAt(int boundaryIndex)
    {
        if (boundaryIndex < 0 || boundaryIndex >= unsafeBoundaries.Length)
        {
            throw new System.ArgumentOutOfRangeException("boundaryIndex");
        }

        return !unsafeBoundaries[boundaryIndex];
    }

    internal TextBreakKind GetBreak(int boundaryIndex)
    {
        if (boundaryIndex < 0 || boundaryIndex >= breaks.Length)
        {
            throw new System.ArgumentOutOfRangeException("boundaryIndex");
        }

        return breaks[boundaryIndex];
    }

    internal bool CanBreak(int boundaryIndex)
    {
        return GetBreak(boundaryIndex) != TextBreakKind.Prohibited;
    }

    internal bool IsEmergency(int boundaryIndex)
    {
        return GetBreak(boundaryIndex) == TextBreakKind.Emergency;
    }

    internal bool RequiresHyphen(int boundaryIndex)
    {
        return GetBreak(boundaryIndex) == TextBreakKind.SoftHyphen;
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    internal void MarkUnsafeBoundary(int boundaryIndex)
    {
        if (boundaryIndex <= 0 || boundaryIndex >= unsafeBoundaries.Length - 1)
        {
            return;
        }
        unsafeBoundaries[boundaryIndex] = true;
        breaks[boundaryIndex] = TextBreakKind.Prohibited;
    }

    internal void MarkHyphenationBoundary(int boundaryIndex)
    {
        if (boundaryIndex <= 0 || boundaryIndex >= breaks.Length - 1)
        {
            throw new System.ArgumentOutOfRangeException("boundaryIndex");
        }
        if (!unsafeBoundaries[boundaryIndex])
        {
            breaks[boundaryIndex] = TextBreakKind.SoftHyphen;
        }
    }

    void ValidateClusterIndex(int clusterIndex)
    {
        if (clusterIndex < 0 || clusterIndex >= runeStarts.Length)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }
    }
}

internal static class TextBreakPlanner
{
    const string ClusterLimitMessage =
        "Text break planning exceeds the configured cluster limit.";

    internal static TextBreakMap Plan(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        LineBreakMap lineBreaks,
        bool allowEmergencyBreaks,
        int maximumClusterCount)
    {
        return Plan(
            input,
            clusters,
            lineBreaks,
            new ShapedRun[0],
            allowEmergencyBreaks,
            maximumClusterCount);
    }

    internal static TextBreakMap Plan(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        LineBreakMap lineBreaks,
        ShapedRun[] shapedRuns,
        bool allowEmergencyBreaks,
        int maximumClusterCount)
    {
        ValidateInputs(
            input,
            clusters,
            lineBreaks,
            shapedRuns,
            maximumClusterCount);
        int clusterCount = clusters.GetClusterCount();
        int[] runeStarts = new int[clusterCount];
        int[] runeLengths = new int[clusterCount];
        int[] utf16Starts = new int[clusterCount];
        int[] utf16Lengths = new int[clusterCount];
        bool[] tabs = new bool[clusterCount];
        bool[] hardBreaks = new bool[clusterCount];
        bool[] softHyphens = new bool[clusterCount];
        bool[] nonbreaking = new bool[clusterCount];
        bool[] trimmableWhitespace = new bool[clusterCount];
        LoadClusters(
            input,
            clusters,
            lineBreaks,
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            tabs,
            hardBreaks,
            softHyphens,
            nonbreaking,
            trimmableWhitespace);
        bool[] unsafeBoundaries = ResolveUnsafeBoundaries(
            clusters,
            shapedRuns);
        TextBreakKind[] breaks = ResolveBreaks(
            clusters,
            lineBreaks,
            unsafeBoundaries,
            hardBreaks,
            softHyphens,
            nonbreaking,
            allowEmergencyBreaks);
        return new TextBreakMap(
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            tabs,
            hardBreaks,
            softHyphens,
            nonbreaking,
            trimmableWhitespace,
            unsafeBoundaries,
            breaks,
            input.GetSourceLength());
    }

    static void LoadClusters(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        LineBreakMap lineBreaks,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        bool[] tabs,
        bool[] hardBreaks,
        bool[] softHyphens,
        bool[] nonbreaking,
        bool[] trimmableWhitespace)
    {
        int clusterIndex = 0;
        while (clusterIndex < clusters.GetClusterCount())
        {
            int runeStart = clusters.GetRuneStart(clusterIndex);
            int runeLength = clusters.GetRuneLength(clusterIndex);
            runeStarts[clusterIndex] = runeStart;
            runeLengths[clusterIndex] = runeLength;
            utf16Starts[clusterIndex] = clusters.GetUtf16Start(clusterIndex);
            utf16Lengths[clusterIndex] = clusters.GetUtf16Length(clusterIndex);
            int runeIndex = runeStart;
            int runeEnd = runeStart + runeLength;
            while (runeIndex < runeEnd)
            {
                int scalar = input.GetScalarValue(runeIndex);
                LineBreakClass lineClass = lineBreaks.GetClass(runeIndex);
                tabs[clusterIndex] = tabs[clusterIndex] || scalar == 0x0009;
                hardBreaks[clusterIndex] = hardBreaks[clusterIndex] ||
                    IsHardBreakScalar(scalar);
                softHyphens[clusterIndex] = softHyphens[clusterIndex] ||
                    scalar == 0x00AD;
                nonbreaking[clusterIndex] = nonbreaking[clusterIndex] ||
                    lineClass == LineBreakClass.Glue ||
                    lineClass == LineBreakClass.WordJoiner;
                trimmableWhitespace[clusterIndex] =
                    trimmableWhitespace[clusterIndex] ||
                    lineClass == LineBreakClass.Space ||
                    scalar == 0x0009;
                runeIndex += 1;
            }

            clusterIndex += 1;
        }
    }

    static TextBreakKind[] ResolveBreaks(
        GraphemeClusterMap clusters,
        LineBreakMap lineBreaks,
        bool[] unsafeBoundaries,
        bool[] hardBreaks,
        bool[] softHyphens,
        bool[] nonbreaking,
        bool allowEmergencyBreaks)
    {
        int clusterCount = clusters.GetClusterCount();
        TextBreakKind[] breaks = new TextBreakKind[clusterCount + 1];
        int boundary = 0;
        while (boundary <= clusterCount)
        {
            if (boundary == 0 && clusterCount != 0)
            {
                breaks[boundary] = TextBreakKind.Prohibited;
            }
            else
            {
                int runeBoundary = boundary == clusterCount ?
                    lineBreaks.GetRuneCount() :
                    clusters.GetRuneStart(boundary);
                LineBreakOpportunity unicodeBreak =
                    lineBreaks.GetOpportunity(runeBoundary);
                if (unicodeBreak == LineBreakOpportunity.Mandatory)
                {
                    breaks[boundary] = TextBreakKind.Mandatory;
                }
                else if (unsafeBoundaries[boundary])
                {
                    breaks[boundary] = TextBreakKind.Prohibited;
                }
                else if (unicodeBreak == LineBreakOpportunity.Allowed)
                {
                    breaks[boundary] = boundary > 0 &&
                        softHyphens[boundary - 1] ?
                        TextBreakKind.SoftHyphen :
                        TextBreakKind.Ordinary;
                }
                else if (allowEmergencyBreaks &&
                    CanEmergencyBreak(
                        boundary,
                        clusterCount,
                        hardBreaks,
                        nonbreaking))
                {
                    breaks[boundary] = TextBreakKind.Emergency;
                }
                else
                {
                    breaks[boundary] = TextBreakKind.Prohibited;
                }
            }

            boundary += 1;
        }

        return breaks;
    }

    static bool[] ResolveUnsafeBoundaries(
        GraphemeClusterMap clusters,
        ShapedRun[] shapedRuns)
    {
        bool[] unsafeBoundaries =
            new bool[clusters.GetClusterCount() + 1];
        int boundary = 1;
        while (boundary < clusters.GetClusterCount())
        {
            unsafeBoundaries[boundary] = IsUnsafeBoundary(
                boundary,
                clusters,
                shapedRuns);
            boundary++;
        }

        return unsafeBoundaries;
    }

    static bool CanEmergencyBreak(
        int boundary,
        int clusterCount,
        bool[] hardBreaks,
        bool[] nonbreaking)
    {
        if (boundary <= 0 || boundary >= clusterCount)
        {
            return false;
        }

        return !hardBreaks[boundary - 1] &&
            !hardBreaks[boundary] &&
            !nonbreaking[boundary - 1] &&
            !nonbreaking[boundary];
    }

    static bool IsUnsafeBoundary(
        int boundary,
        GraphemeClusterMap clusters,
        ShapedRun[] shapedRuns)
    {
        if (boundary <= 0 || boundary >= clusters.GetClusterCount())
        {
            return false;
        }

        int utf16Position = clusters.GetUtf16Start(boundary);
        int runIndex = 0;
        while (runIndex < shapedRuns.Length)
        {
            ShapedRun run = shapedRuns[runIndex];
            int clusterIndex = 0;
            while (clusterIndex < run.GetClusterCount())
            {
                ShapedCluster cluster = run.GetCluster(clusterIndex);
                int start = cluster.GetUtf16Start();
                int end = start + cluster.GetUtf16Length();
                if ((utf16Position > start && utf16Position < end) ||
                    (utf16Position == start && cluster.IsUnsafeToBreak()))
                {
                    return true;
                }

                clusterIndex += 1;
            }

            runIndex += 1;
        }

        return false;
    }

    static bool IsHardBreakScalar(int scalar)
    {
        return scalar == 0x000A ||
            scalar == 0x000B ||
            scalar == 0x000C ||
            scalar == 0x000D ||
            scalar == 0x0085 ||
            scalar == 0x2028 ||
            scalar == 0x2029;
    }

    static void ValidateInputs(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        LineBreakMap lineBreaks,
        ShapedRun[] shapedRuns,
        int maximumClusterCount)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (clusters == null)
        {
            throw new System.ArgumentNullException("clusters");
        }

        if (lineBreaks == null)
        {
            throw new System.ArgumentNullException("lineBreaks");
        }

        if (shapedRuns == null)
        {
            throw new System.ArgumentNullException("shapedRuns");
        }

        if (maximumClusterCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumClusterCount");
        }

        if (clusters.GetClusterCount() > maximumClusterCount)
        {
            throw FontLimitException.Create(ClusterLimitMessage);
        }

        if (clusters.GetSourceLength() != input.GetSourceLength() ||
            lineBreaks.GetSourceLength() != input.GetSourceLength() ||
            lineBreaks.GetRuneCount() != input.GetCount())
        {
            throw new System.ArgumentException(
                "Text break inputs do not describe the same source.");
        }

        int runIndex = 0;
        while (runIndex < shapedRuns.Length)
        {
            if (shapedRuns[runIndex] == null)
            {
                throw new System.ArgumentException(
                    "A shaped run cannot be null.");
            }

            runIndex += 1;
        }
    }
}
