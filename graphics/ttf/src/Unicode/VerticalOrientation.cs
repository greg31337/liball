namespace ttf;

internal enum VerticalOrientation
{
    Upright = 0,
    Rotated = 1,
    TransformedUpright = 2,
    TransformedRotated = 3
}

internal class VerticalOrientationMap
{
    VerticalOrientation[] clusterOrientations;
    VerticalOrientation[] runeOrientations;
    int[] runClusterStarts;
    int[] runClusterLengths;
    int[] runRuneStarts;
    int[] runRuneLengths;
    int[] runUtf16Starts;
    int[] runUtf16Lengths;
    VerticalOrientation[] runOrientations;
    int sourceLength;

    internal VerticalOrientationMap(
        VerticalOrientation[] clusterOrientations,
        VerticalOrientation[] runeOrientations,
        int[] runClusterStarts,
        int[] runClusterLengths,
        int[] runRuneStarts,
        int[] runRuneLengths,
        int[] runUtf16Starts,
        int[] runUtf16Lengths,
        VerticalOrientation[] runOrientations,
        int sourceLength)
    {
        this.clusterOrientations = clusterOrientations;
        this.runeOrientations = runeOrientations;
        this.runClusterStarts = runClusterStarts;
        this.runClusterLengths = runClusterLengths;
        this.runRuneStarts = runRuneStarts;
        this.runRuneLengths = runRuneLengths;
        this.runUtf16Starts = runUtf16Starts;
        this.runUtf16Lengths = runUtf16Lengths;
        this.runOrientations = runOrientations;
        this.sourceLength = sourceLength;
    }

    internal int GetClusterCount()
    {
        return clusterOrientations.Length;
    }

    internal int GetRuneCount()
    {
        return runeOrientations.Length;
    }

    internal VerticalOrientation GetClusterOrientation(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        return clusterOrientations[clusterIndex];
    }

    internal VerticalOrientation GetRuneOrientation(int runeIndex)
    {
        ValidateRuneIndex(runeIndex);
        return runeOrientations[runeIndex];
    }

    internal bool UsesUprightFallback(int runeIndex)
    {
        VerticalOrientation orientation = GetRuneOrientation(runeIndex);
        return orientation == VerticalOrientation.Upright ||
            orientation == VerticalOrientation.TransformedUpright;
    }

    internal bool IsRuneTransformed(int runeIndex)
    {
        VerticalOrientation orientation = GetRuneOrientation(runeIndex);
        return orientation == VerticalOrientation.TransformedUpright ||
            orientation == VerticalOrientation.TransformedRotated;
    }

    internal bool NeedsFallbackClockwiseRotation(int runeIndex)
    {
        VerticalOrientation orientation = GetRuneOrientation(runeIndex);
        return orientation == VerticalOrientation.Rotated ||
            orientation == VerticalOrientation.TransformedRotated;
    }

    internal int GetRunCount()
    {
        return runOrientations.Length;
    }

    internal VerticalOrientation GetRunOrientation(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runOrientations[runIndex];
    }

    internal int GetRunClusterStart(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runClusterStarts[runIndex];
    }

    internal int GetRunClusterLength(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runClusterLengths[runIndex];
    }

    internal int GetRunClusterEnd(int runIndex)
    {
        return GetRunClusterStart(runIndex) + GetRunClusterLength(runIndex);
    }

    internal int GetRunRuneStart(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runRuneStarts[runIndex];
    }

    internal int GetRunRuneLength(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runRuneLengths[runIndex];
    }

    internal int GetRunRuneEnd(int runIndex)
    {
        return GetRunRuneStart(runIndex) + GetRunRuneLength(runIndex);
    }

    internal int GetRunUtf16Start(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runUtf16Starts[runIndex];
    }

    internal int GetRunUtf16Length(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runUtf16Lengths[runIndex];
    }

    internal int GetRunUtf16End(int runIndex)
    {
        return GetRunUtf16Start(runIndex) + GetRunUtf16Length(runIndex);
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    void ValidateClusterIndex(int clusterIndex)
    {
        if (clusterIndex < 0 || clusterIndex >= clusterOrientations.Length)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }
    }

    void ValidateRuneIndex(int runeIndex)
    {
        if (runeIndex < 0 || runeIndex >= runeOrientations.Length)
        {
            throw new System.ArgumentOutOfRangeException("runeIndex");
        }
    }

    void ValidateRunIndex(int runIndex)
    {
        if (runIndex < 0 || runIndex >= runOrientations.Length)
        {
            throw new System.ArgumentOutOfRangeException("runIndex");
        }
    }
}

internal static class VerticalOrientationResolver
{
    const string RuneLimitMessage =
        "Vertical orientation resolution exceeds the configured rune limit.";
    const string RunLimitMessage =
        "Vertical orientation resolution exceeds the configured run limit.";

    internal static VerticalOrientationMap Resolve(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        int maximumRuneCount,
        int maximumRunCount)
    {
        ValidateArguments(
            input,
            clusters,
            unicodeData,
            maximumRuneCount,
            maximumRunCount);
        ValidateClusters(input, clusters);
        int clusterCount = clusters.GetClusterCount();
        VerticalOrientation[] clusterOrientations =
            new VerticalOrientation[clusterCount];
        VerticalOrientation[] runeOrientations =
            new VerticalOrientation[input.GetCount()];
        int clusterIndex = 0;
        while (clusterIndex < clusterCount)
        {
            VerticalOrientation orientation = ResolveCluster(
                input,
                clusters,
                unicodeData,
                clusterIndex);
            clusterOrientations[clusterIndex] = orientation;
            int runeIndex = clusters.GetRuneStart(clusterIndex);
            int runeEnd = clusters.GetRuneEnd(clusterIndex);
            while (runeIndex < runeEnd)
            {
                runeOrientations[runeIndex] = orientation;
                runeIndex++;
            }

            clusterIndex++;
        }

        int runCount = CountRuns(clusterOrientations);
        if (runCount > maximumRunCount)
        {
            throw FontLimitException.Create(RunLimitMessage);
        }

        int[] runClusterStarts = new int[runCount];
        int[] runClusterLengths = new int[runCount];
        int[] runRuneStarts = new int[runCount];
        int[] runRuneLengths = new int[runCount];
        int[] runUtf16Starts = new int[runCount];
        int[] runUtf16Lengths = new int[runCount];
        VerticalOrientation[] runOrientations =
            new VerticalOrientation[runCount];
        FillRuns(
            clusters,
            clusterOrientations,
            runClusterStarts,
            runClusterLengths,
            runRuneStarts,
            runRuneLengths,
            runUtf16Starts,
            runUtf16Lengths,
            runOrientations);
        return new VerticalOrientationMap(
            clusterOrientations,
            runeOrientations,
            runClusterStarts,
            runClusterLengths,
            runRuneStarts,
            runRuneLengths,
            runUtf16Starts,
            runUtf16Lengths,
            runOrientations,
            input.GetSourceLength());
    }

    static void ValidateArguments(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        int maximumRuneCount,
        int maximumRunCount)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (clusters == null)
        {
            throw new System.ArgumentNullException("clusters");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        if (maximumRunCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRunCount");
        }

        if (input.GetCount() > maximumRuneCount)
        {
            throw FontLimitException.Create(RuneLimitMessage);
        }
    }

    static void ValidateClusters(
        RuneBuffer input,
        GraphemeClusterMap clusters)
    {
        if (input.GetSourceLength() != clusters.GetSourceLength())
        {
            throw new System.ArgumentException(
                "The grapheme clusters do not describe the rune buffer.",
                "clusters");
        }

        int expectedRuneStart = 0;
        int clusterIndex = 0;
        while (clusterIndex < clusters.GetClusterCount())
        {
            if (clusters.GetRuneStart(clusterIndex) != expectedRuneStart ||
                clusters.GetRuneLength(clusterIndex) <= 0)
            {
                throw new System.ArgumentException(
                    "The grapheme clusters do not describe the rune buffer.",
                    "clusters");
            }

            expectedRuneStart = clusters.GetRuneEnd(clusterIndex);
            clusterIndex++;
        }

        if (expectedRuneStart != input.GetCount())
        {
            throw new System.ArgumentException(
                "The grapheme clusters do not describe the rune buffer.",
                "clusters");
        }
    }

    static VerticalOrientation ResolveCluster(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        int clusterIndex)
    {
        int runeIndex = clusters.GetRuneStart(clusterIndex);
        int runeEnd = clusters.GetRuneEnd(clusterIndex);
        int index = runeIndex;
        while (index < runeEnd)
        {
            System.Globalization.UnicodeCategory category =
                System.Text.Rune.GetUnicodeCategory(input.GetRune(index));
            if (category == System.Globalization.UnicodeCategory.EnclosingMark)
            {
                return VerticalOrientation.Upright;
            }

            index++;
        }

        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.VerticalOrientation,
            input.GetScalarValue(runeIndex));
        return ParseOrientation(value);
    }

    static VerticalOrientation ParseOrientation(string value)
    {
        if (value == "U")
        {
            return VerticalOrientation.Upright;
        }

        if (value == "R")
        {
            return VerticalOrientation.Rotated;
        }

        if (value == "Tu")
        {
            return VerticalOrientation.TransformedUpright;
        }

        if (value == "Tr")
        {
            return VerticalOrientation.TransformedRotated;
        }

        throw new System.InvalidOperationException(
            "The Unicode vertical orientation is not recognized: " +
            value + ".");
    }

    static int CountRuns(VerticalOrientation[] orientations)
    {
        if (orientations.Length == 0)
        {
            return 0;
        }

        int count = 1;
        int index = 1;
        while (index < orientations.Length)
        {
            if (orientations[index] != orientations[index - 1])
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static void FillRuns(
        GraphemeClusterMap clusters,
        VerticalOrientation[] orientations,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        VerticalOrientation[] runOrientations)
    {
        int runIndex = 0;
        int runStart = 0;
        int clusterIndex = 0;
        while (clusterIndex <= orientations.Length)
        {
            bool boundary = clusterIndex == orientations.Length;
            if (!boundary && clusterIndex > runStart)
            {
                boundary = orientations[clusterIndex] !=
                    orientations[clusterIndex - 1];
            }

            if (boundary && clusterIndex > runStart)
            {
                int lastCluster = clusterIndex - 1;
                int runeStart = clusters.GetRuneStart(runStart);
                int runeEnd = clusters.GetRuneEnd(lastCluster);
                int utf16Start = clusters.GetUtf16Start(runStart);
                int utf16End = clusters.GetUtf16End(lastCluster);
                clusterStarts[runIndex] = runStart;
                clusterLengths[runIndex] = clusterIndex - runStart;
                runeStarts[runIndex] = runeStart;
                runeLengths[runIndex] = runeEnd - runeStart;
                utf16Starts[runIndex] = utf16Start;
                utf16Lengths[runIndex] = utf16End - utf16Start;
                runOrientations[runIndex] = orientations[runStart];
                runIndex++;
                runStart = clusterIndex;
            }

            clusterIndex++;
        }
    }
}

internal class VerticalFeaturePlan
{
    const int VerticalAlternatesTag = 0x76657274;
    const int VerticalAlternatesAndRotationTag = 0x76727432;
    const int VerticalKerningTag = 0x766B726E;
    VerticalOrientationMap orientations;
    int substitutionFeatureTag;
    SfntLayoutFeaturePlan substitutionPlan;
    SfntLayoutSelection? substitutionSelection;
    SfntLayoutFeaturePlan positioningPlan;
    SfntLayoutSelection? positioningSelection;

    internal VerticalFeaturePlan(
        VerticalOrientationMap orientations,
        int substitutionFeatureTag,
        SfntLayoutFeaturePlan substitutionPlan,
        SfntLayoutSelection? substitutionSelection,
        SfntLayoutFeaturePlan positioningPlan,
        SfntLayoutSelection? positioningSelection)
    {
        this.orientations = orientations;
        this.substitutionFeatureTag = substitutionFeatureTag;
        this.substitutionPlan = substitutionPlan;
        this.substitutionSelection = substitutionSelection;
        this.positioningPlan = positioningPlan;
        this.positioningSelection = positioningSelection;
    }

    internal VerticalOrientationMap GetOrientations()
    {
        return orientations;
    }

    internal bool HasSubstitutionFeature()
    {
        return substitutionFeatureTag != 0;
    }

    internal int GetSubstitutionFeatureTag()
    {
        return substitutionFeatureTag;
    }

    internal bool UsesVerticalAlternates()
    {
        return substitutionFeatureTag == VerticalAlternatesTag;
    }

    internal bool UsesVerticalAlternatesAndRotation()
    {
        return substitutionFeatureTag ==
            VerticalAlternatesAndRotationTag;
    }

    internal SfntLayoutFeaturePlan GetSubstitutionPlan()
    {
        return substitutionPlan;
    }

    internal SfntLayoutSelection? GetSubstitutionSelection()
    {
        return substitutionSelection;
    }

    internal bool UsesVerticalKerning()
    {
        return positioningSelection != null &&
            positioningSelection.HasFeature(VerticalKerningTag);
    }

    internal SfntLayoutFeaturePlan GetPositioningPlan()
    {
        return positioningPlan;
    }

    internal SfntLayoutSelection? GetPositioningSelection()
    {
        return positioningSelection;
    }

    internal bool UsesVerticalMetrics()
    {
        return true;
    }

    internal int GetAdvance(GlyphMetrics metrics)
    {
        ValidateMetrics(metrics);
        return metrics.GetAdvanceHeight();
    }

    internal double GetVerticalOriginX(GlyphMetrics metrics)
    {
        ValidateMetrics(metrics);
        return metrics.GetAdvanceWidth() / 2.0;
    }

    internal int GetVerticalOriginY(GlyphMetrics metrics)
    {
        ValidateMetrics(metrics);
        return metrics.GetVerticalOriginY();
    }

    internal bool UsesSynthesizedVerticalMetrics(GlyphMetrics metrics)
    {
        ValidateMetrics(metrics);
        return metrics.UsesSynthesizedVerticalAdvance();
    }

    static void ValidateMetrics(GlyphMetrics metrics)
    {
        if (metrics == null)
        {
            throw new System.ArgumentNullException("metrics");
        }
    }
}

internal static class VerticalFeaturePlanBuilder
{
    const int VerticalAlternatesTag = 0x76657274;
    const int VerticalAlternatesAndRotationTag = 0x76727432;
    const int VerticalKerningTag = 0x766B726E;

    internal static VerticalFeaturePlan Build(
        VerticalOrientationMap orientations,
        SfntLayoutTable substitutionTable,
        SfntLayoutTable positioningTable,
        int scriptTag,
        int languageTag)
    {
        if (orientations == null)
        {
            throw new System.ArgumentNullException("orientations");
        }

        int textLength = orientations.GetRuneCount();
        int substitutionTag = SelectSubstitutionTag(
            substitutionTable,
            scriptTag,
            languageTag,
            textLength);
        SfntLayoutFeaturePlan substitutionPlan =
            BuildSubstitutionPlan(
                orientations,
                substitutionTag,
                textLength);
        SfntLayoutSelection? substitutionSelection =
            substitutionTag == 0 ? null : CreateSelection(
                substitutionPlan,
                substitutionTable,
                scriptTag,
                languageTag,
                textLength);
        bool verticalKerning = HasFeature(
            positioningTable,
            scriptTag,
            languageTag,
            VerticalKerningTag,
            textLength);
        SfntLayoutFeaturePlan positioningPlan =
            SfntLayoutFeaturePlanBuilder.Build(
                new int[0],
                verticalKerning ? new int[] { VerticalKerningTag } :
                    new int[0],
                new int[0],
                new SfntUserFeature[0],
                textLength);
        SfntLayoutSelection? positioningSelection =
            verticalKerning ? CreateSelection(
                positioningPlan,
                positioningTable,
                scriptTag,
                languageTag,
                textLength) : null;
        return new VerticalFeaturePlan(
            orientations,
            substitutionTag,
            substitutionPlan,
            substitutionSelection,
            positioningPlan,
            positioningSelection);
    }

    static SfntLayoutSelection CreateSelection(
        SfntLayoutFeaturePlan plan,
        SfntLayoutTable table,
        int scriptTag,
        int languageTag,
        int textLength)
    {
        SfntUserFeature[] selections =
            new SfntUserFeature[plan.GetSelectionCount()];
        int index = 0;
        while (index < selections.Length)
        {
            selections[index] = plan.GetSelection(index);
            index++;
        }

        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            selections,
            textLength);
    }

    static int SelectSubstitutionTag(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag,
        int textLength)
    {
        if (HasFeature(
            table,
            scriptTag,
            languageTag,
            VerticalAlternatesAndRotationTag,
            textLength))
        {
            return VerticalAlternatesAndRotationTag;
        }

        if (HasFeature(
            table,
            scriptTag,
            languageTag,
            VerticalAlternatesTag,
            textLength))
        {
            return VerticalAlternatesTag;
        }

        return 0;
    }

    static bool HasFeature(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag,
        int featureTag,
        int textLength)
    {
        if (table == null)
        {
            return false;
        }

        SfntLayoutSelection selection = SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            new SfntUserFeature[0],
            textLength);
        return selection.HasFeature(featureTag);
    }

    static SfntLayoutFeaturePlan BuildSubstitutionPlan(
        VerticalOrientationMap orientations,
        int featureTag,
        int textLength)
    {
        if (featureTag == 0)
        {
            return SfntLayoutFeaturePlanBuilder.Build(
                new int[0],
                new int[0],
                new int[0],
                new SfntUserFeature[0],
                textLength);
        }

        SfntUserFeature[] ranges;
        if (featureTag == VerticalAlternatesAndRotationTag)
        {
            ranges = textLength == 0 ? new SfntUserFeature[0] :
                new SfntUserFeature[]
                {
                    new SfntUserFeature(featureTag, 1, 0, textLength)
                };
        }
        else
        {
            ranges = BuildTransformedRanges(orientations, featureTag);
        }

        return SfntLayoutFeaturePlanBuilder.Build(
            new int[0],
            new int[0],
            new int[] { featureTag },
            ranges,
            textLength);
    }

    static SfntUserFeature[] BuildTransformedRanges(
        VerticalOrientationMap orientations,
        int featureTag)
    {
        int rangeCount = 0;
        int index = 0;
        while (index < orientations.GetRuneCount())
        {
            if (!orientations.IsRuneTransformed(index))
            {
                index++;
                continue;
            }

            rangeCount++;
            while (index < orientations.GetRuneCount() &&
                orientations.IsRuneTransformed(index))
            {
                index++;
            }
        }

        SfntUserFeature[] ranges = new SfntUserFeature[rangeCount];
        int rangeIndex = 0;
        index = 0;
        while (index < orientations.GetRuneCount())
        {
            if (!orientations.IsRuneTransformed(index))
            {
                index++;
                continue;
            }

            int start = index;
            while (index < orientations.GetRuneCount() &&
                orientations.IsRuneTransformed(index))
            {
                index++;
            }

            ranges[rangeIndex] = new SfntUserFeature(
                featureTag,
                1,
                start,
                index);
            rangeIndex++;
        }

        return ranges;
    }
}
