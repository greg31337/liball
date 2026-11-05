namespace ttf;

internal class PreparedText
{
    string? sourceText;
    TextBreakMap? breaks;
    double[]? clusterAdvances;
    int[]? sourceRunIndexes;
    int[]? bidiLevels;
    TextLayoutOptions? options;
    TextJustificationOpportunity[]? opportunities;
    PreparedRunMetrics[]? runMetrics;
    double hyphenAdvance;
    double ellipsisAdvance;
    double lineStep;

    internal static PreparedText Create(
        string sourceText,
        TextBreakMap breaks,
        double[] clusterAdvances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        TextJustificationOpportunity[] opportunities,
        double hyphenAdvance,
        double ellipsisAdvance,
        double lineStep)
    {
        return new PreparedText(
            sourceText,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            opportunities,
            new PreparedRunMetrics[0],
            hyphenAdvance,
            ellipsisAdvance,
            lineStep);
    }

    internal PreparedText(
        string sourceText,
        TextBreakMap breaks,
        double[] clusterAdvances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        TextJustificationOpportunity[] opportunities,
        PreparedRunMetrics[] runMetrics,
        double hyphenAdvance,
        double ellipsisAdvance,
        double lineStep)
    {
        ValidateInputs(
            sourceText,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            opportunities,
            hyphenAdvance,
            ellipsisAdvance,
            lineStep);
        ValidateRunMetrics(
            runMetrics,
            breaks.GetClusterCount(),
            sourceRunIndexes);
        Initialize(
            sourceText,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            opportunities,
            runMetrics,
            hyphenAdvance,
            ellipsisAdvance,
            lineStep);
    }

    internal static PreparedText Create(
        string sourceText,
        TextBreakMap breaks,
        double[] clusterAdvances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        TextJustificationOpportunity[] opportunities,
        PreparedRunMetrics[] runMetrics,
        double hyphenAdvance,
        double ellipsisAdvance,
        double lineStep)
    {
        return new PreparedText(
            sourceText,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            opportunities,
            runMetrics,
            hyphenAdvance,
            ellipsisAdvance,
            lineStep);
    }

    internal TextLayout Layout(double maximumAdvance)
    {
        if (!double.IsFinite(maximumAdvance) || maximumAdvance < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumAdvance");
        }

        GreedyLineFitResult fits = GreedyLineFitter.Fit(
            breaks,
            clusterAdvances,
            maximumAdvance,
            options,
            hyphenAdvance,
            ellipsisAdvance);
        TextLine[] lines = new TextLine[fits.GetLineCount()];
        int index = 0;
        while (index < lines.Length)
        {
            GreedyLineFit fit = fits.GetLine(index);
            LineJustificationPlan justification =
                TextJustificationPlanner.Plan(
                    fit,
                    index == lines.Length - 1,
                    breaks,
                    maximumAdvance,
                    options,
                    opportunities);
            double linePosition = GetLinePosition(index);
            lines[index] = TextLineConstructor.Construct(
                fit,
                breaks,
                BuildFragments(fit),
                justification,
                options,
                linePosition);
            index++;
        }

        System.ArgumentNullException.ThrowIfNull(breaks);
        int truncationClusterIndex = breaks.GetClusterCount();
        if (fits.IsTruncated())
        {
            truncationClusterIndex = fits.GetTruncationClusterIndex();
        }

        int truncationUtf16Index = GetUtf16Boundary(
            truncationClusterIndex);
        System.ArgumentNullException.ThrowIfNull(options);
        return new TextLayout(
            sourceText,
            maximumAdvance,
            options.GetWritingMode(),
            breaks,
            lines,
            fits.IsTruncated(),
            truncationClusterIndex,
            truncationUtf16Index);
    }

    TextLineRunFragment[] BuildFragments(GreedyLineFit fit)
    {
        System.Collections.Generic.List<TextLineRunFragment> fragments =
            new System.Collections.Generic.List<TextLineRunFragment>();
        int index = fit.GetVisibleClusterStart();
        while (index < fit.GetVisibleClusterEnd())
        {
            System.ArgumentNullException.ThrowIfNull(sourceRunIndexes);
            if (sourceRunIndexes[index] < 0)
            {
                index++;
                continue;
            }

            int start = index;
            int sourceRunIndex = sourceRunIndexes[index];
            System.ArgumentNullException.ThrowIfNull(bidiLevels);
            int bidiLevel = bidiLevels[index];
            double advance = 0;
            while (index < fit.GetVisibleClusterEnd() &&
                sourceRunIndexes[index] == sourceRunIndex &&
                bidiLevels[index] == bidiLevel)
            {
                System.ArgumentNullException.ThrowIfNull(clusterAdvances);
                advance += clusterAdvances[index];
                index++;
            }

            fragments.Add(TextLineRunFragment.Create(
                sourceRunIndex,
                start,
                index,
                bidiLevel,
                advance,
                FindRunMetrics(sourceRunIndex),
                CopyClusterAdvances(start, index)));
        }

        return fragments.ToArray();
    }

    double[] CopyClusterAdvances(int start, int end)
    {
        double[] values = new double[end - start];
        int index = start;
        while (index < end)
        {
            System.ArgumentNullException.ThrowIfNull(clusterAdvances);
            values[index - start] = clusterAdvances[index];
            index++;
        }

        return values;
    }

    PreparedRunMetrics? FindRunMetrics(int sourceRunIndex)
    {
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(runMetrics);
        while (index < runMetrics.Length)
        {
            if (runMetrics[index].GetSourceRunIndex() == sourceRunIndex)
            {
                return runMetrics[index];
            }

            index++;
        }

        return null;
    }

    void Initialize(
        string sourceText,
        TextBreakMap breaks,
        double[] clusterAdvances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        TextJustificationOpportunity[] opportunities,
        PreparedRunMetrics[] runMetrics,
        double hyphenAdvance,
        double ellipsisAdvance,
        double lineStep)
    {
        this.sourceText = sourceText;
        this.breaks = breaks;
        this.clusterAdvances = Copy(clusterAdvances);
        this.sourceRunIndexes = Copy(sourceRunIndexes);
        this.bidiLevels = Copy(bidiLevels);
        this.options = options.Copy();
        this.opportunities = Copy(opportunities);
        this.runMetrics = Copy(runMetrics);
        this.hyphenAdvance = hyphenAdvance;
        this.ellipsisAdvance = ellipsisAdvance;
        this.lineStep = lineStep;
    }

    double GetLinePosition(int lineIndex)
    {
        double position = -lineIndex * lineStep;
        if (!double.IsFinite(position))
        {
            throw FontLimitException.Create(
                "Prepared text line positions exceed finite layout coordinates.");
        }

        return position;
    }

    int GetUtf16Boundary(int clusterBoundary)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        return clusterBoundary == breaks.GetClusterCount() ?
            breaks.GetSourceLength() :
            breaks.GetUtf16Start(clusterBoundary);
    }

    static double[] Copy(double[] values)
    {
        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[] Copy(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static TextJustificationOpportunity[] Copy(
        TextJustificationOpportunity[] values)
    {
        TextJustificationOpportunity[] copy =
            new TextJustificationOpportunity[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PreparedRunMetrics[] Copy(PreparedRunMetrics[] values)
    {
        PreparedRunMetrics[] copy = new PreparedRunMetrics[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static void ValidateRunMetrics(
        PreparedRunMetrics[] values,
        int clusterCount,
        int[] sourceRunIndexes)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("runMetrics");
        }

        int index = 0;
        while (index < values.Length)
        {
            PreparedRunMetrics run = values[index];
            if (run == null)
            {
                throw new System.ArgumentException(
                    "Prepared run metrics cannot be null.",
                    "runMetrics");
            }

            int previous = 0;
            while (previous < index)
            {
                if (values[previous].GetSourceRunIndex() ==
                    run.GetSourceRunIndex())
                {
                    throw new System.ArgumentException(
                        "Prepared run indexes must be unique.",
                        "runMetrics");
                }

                previous++;
            }

            int glyphIndex = 0;
            while (glyphIndex < run.GetGlyphCount())
            {
                PreparedGlyphMetrics glyph = run.GetGlyph(glyphIndex);
                int clusterIndex = glyph.GetClusterIndex();
                if (clusterIndex >= clusterCount ||
                    glyph.GetClusterEnd() > clusterCount)
                {
                    throw new System.ArgumentException(
                        "Prepared glyph clusters must belong to their source run.",
                        "runMetrics");
                }

                while (clusterIndex < glyph.GetClusterEnd())
                {
                    if (sourceRunIndexes[clusterIndex] !=
                        run.GetSourceRunIndex())
                    {
                        throw new System.ArgumentException(
                            "Prepared glyph clusters must belong to their source run.",
                            "runMetrics");
                    }

                    clusterIndex++;
                }

                glyphIndex++;
            }

            index++;
        }
    }

    static void ValidateInputs(
        string sourceText,
        TextBreakMap breaks,
        double[] clusterAdvances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        TextJustificationOpportunity[] opportunities,
        double hyphenAdvance,
        double ellipsisAdvance,
        double lineStep)
    {
        if (sourceText == null)
        {
            throw new System.ArgumentNullException("sourceText");
        }

        if (breaks == null)
        {
            throw new System.ArgumentNullException("breaks");
        }

        if (clusterAdvances == null)
        {
            throw new System.ArgumentNullException("clusterAdvances");
        }

        if (sourceRunIndexes == null)
        {
            throw new System.ArgumentNullException("sourceRunIndexes");
        }

        if (bidiLevels == null)
        {
            throw new System.ArgumentNullException("bidiLevels");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (opportunities == null)
        {
            throw new System.ArgumentNullException("opportunities");
        }

        int clusterCount = breaks.GetClusterCount();
        if (sourceText.Length != breaks.GetSourceLength())
        {
            throw new System.ArgumentException(
                "Source text must match the text break map.",
                "sourceText");
        }

        if (clusterAdvances.Length != clusterCount)
        {
            throw new System.ArgumentException(
                "Cluster advances must match the text break map.",
                "clusterAdvances");
        }

        if (sourceRunIndexes.Length != clusterCount)
        {
            throw new System.ArgumentException(
                "Source run indexes must match the text break map.",
                "sourceRunIndexes");
        }

        if (bidiLevels.Length != clusterCount)
        {
            throw new System.ArgumentException(
                "Bidi levels must match the text break map.",
                "bidiLevels");
        }

        ValidateNonnegativeFinite(hyphenAdvance, "hyphenAdvance");
        ValidateNonnegativeFinite(ellipsisAdvance, "ellipsisAdvance");
        ValidateNonnegativeFinite(lineStep, "lineStep");
        double sum = 0;
        int index = 0;
        while (index < clusterCount)
        {
            ValidateNonnegativeFinite(
                clusterAdvances[index],
                "clusterAdvances");
            sum += clusterAdvances[index];
            if (!double.IsFinite(sum))
            {
                throw new System.ArgumentOutOfRangeException(
                    "clusterAdvances");
            }

            if (sourceRunIndexes[index] < -1)
            {
                throw new System.ArgumentOutOfRangeException(
                    "sourceRunIndexes");
            }

            if (bidiLevels[index] < 0 || bidiLevels[index] > 125)
            {
                throw new System.ArgumentOutOfRangeException("bidiLevels");
            }

            index++;
        }

        index = 0;
        while (index < opportunities.Length)
        {
            if (opportunities[index] == null ||
                opportunities[index].GetClusterBoundary() > clusterCount)
            {
                throw new System.ArgumentException(
                    "Justification opportunities must belong to the text.",
                    "opportunities");
            }

            index++;
        }
    }

    static void ValidateNonnegativeFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
