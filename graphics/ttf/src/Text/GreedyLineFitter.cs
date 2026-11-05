namespace ttf;

internal class GreedyLineFit
{
    int clusterStart;
    int clusterEnd;
    int visibleClusterStart;
    int visibleClusterEnd;
    double contentAdvance;
    double advance;
    double inlineOffset;
    bool requiresHyphen;
    bool hasEllipsis;
    bool endedByHardBreak;
    string? ellipsisText;

    internal GreedyLineFit(
        int clusterStart,
        int clusterEnd,
        int visibleClusterStart,
        int visibleClusterEnd,
        double contentAdvance,
        double advance,
        double inlineOffset,
        bool requiresHyphen,
        bool hasEllipsis,
        bool endedByHardBreak,
        string? ellipsisText)
    {
        this.clusterStart = clusterStart;
        this.clusterEnd = clusterEnd;
        this.visibleClusterStart = visibleClusterStart;
        this.visibleClusterEnd = visibleClusterEnd;
        this.contentAdvance = contentAdvance;
        this.advance = advance;
        this.inlineOffset = inlineOffset;
        this.requiresHyphen = requiresHyphen;
        this.hasEllipsis = hasEllipsis;
        this.endedByHardBreak = endedByHardBreak;
        this.ellipsisText = ellipsisText;
    }

    internal int GetClusterStart()
    {
        return clusterStart;
    }

    internal int GetClusterEnd()
    {
        return clusterEnd;
    }

    internal int GetVisibleClusterStart()
    {
        return visibleClusterStart;
    }

    internal int GetVisibleClusterEnd()
    {
        return visibleClusterEnd;
    }

    internal double GetContentAdvance()
    {
        return contentAdvance;
    }

    internal double GetAdvance()
    {
        return advance;
    }

    internal double GetInlineOffset()
    {
        return inlineOffset;
    }

    internal bool RequiresHyphen()
    {
        return requiresHyphen;
    }

    internal bool HasEllipsis()
    {
        return hasEllipsis;
    }

    internal bool EndedByHardBreak()
    {
        return endedByHardBreak;
    }

    internal string? GetEllipsisText()
    {
        if (!hasEllipsis)
        {
            throw new System.InvalidOperationException(
                "This fitted line has no ellipsis.");
        }

        return ellipsisText;
    }
}

internal class GreedyLineFitResult
{
    GreedyLineFit[] lines;
    bool truncated;
    int truncationClusterIndex;

    internal GreedyLineFitResult(
        GreedyLineFit[] lines,
        bool truncated,
        int truncationClusterIndex)
    {
        this.lines = lines;
        this.truncated = truncated;
        this.truncationClusterIndex = truncationClusterIndex;
    }

    internal int GetLineCount()
    {
        return lines.Length;
    }

    internal GreedyLineFit GetLine(int index)
    {
        if (index < 0 || index >= lines.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return lines[index];
    }

    internal bool IsTruncated()
    {
        return truncated;
    }

    internal int GetTruncationClusterIndex()
    {
        if (!truncated)
        {
            throw new System.InvalidOperationException(
                "The fitted text was not truncated.");
        }

        return truncationClusterIndex;
    }
}

internal class GreedyLineFitterContext
{
    TextBreakMap? breaks;
    double[] prefixAdvances;
    double maximumAdvance;
    TextLayoutOptions? options;
    double hyphenAdvance;
    double ellipsisAdvance;

    internal GreedyLineFitterContext(
        TextBreakMap? breaks,
        double[] prefixAdvances,
        double maximumAdvance,
        TextLayoutOptions? options,
        double hyphenAdvance,
        double ellipsisAdvance)
    {
        this.breaks = breaks;
        this.prefixAdvances = prefixAdvances;
        this.maximumAdvance = maximumAdvance;
        this.options = options;
        this.hyphenAdvance = hyphenAdvance;
        this.ellipsisAdvance = ellipsisAdvance;
    }

    internal GreedyLineFitResult Fit()
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int clusterCount = breaks.GetClusterCount();
        System.Collections.Generic.List<GreedyLineFit> lines =
            new System.Collections.Generic.List<GreedyLineFit>();
        if (clusterCount == 0)
        {
            lines.Add(BuildLine(0, 0));
            return new GreedyLineFitResult(lines.ToArray(), false, 0);
        }

        int start = 0;
        while (start < clusterCount)
        {
            int end = FindLineEnd(start);
            GreedyLineFit line = BuildLine(start, end);
            lines.Add(line);
            System.ArgumentNullException.ThrowIfNull(options);
            bool reachedLineLimit = options.GetMaximumLines() != 0 &&
                lines.Count == options.GetMaximumLines();
            bool hasTerminalEmptyLine = end == clusterCount &&
                line.EndedByHardBreak();
            bool hasHiddenLines = reachedLineLimit &&
                (end < clusterCount || hasTerminalEmptyLine);
            bool needsWidthTrimming = end == clusterCount &&
                line.GetAdvance() > maximumAdvance &&
                options.GetTrimmingMode() != TextTrimmingMode.None;
            if (hasHiddenLines || needsWidthTrimming)
            {
                int truncationIndex = end;
                if (options.GetTrimmingMode() != TextTrimmingMode.None)
                {
                    GreedyLineFit trimmed = BuildEllipsizedLine(start, end);
                    lines[lines.Count - 1] = trimmed;
                    truncationIndex = trimmed.GetClusterEnd();
                }

                return new GreedyLineFitResult(
                    lines.ToArray(),
                    true,
                    truncationIndex);
            }

            start = end;
        }

        if (lines.Count != 0 &&
            lines[lines.Count - 1].EndedByHardBreak())
        {
            lines.Add(BuildLine(clusterCount, clusterCount));
        }

        return new GreedyLineFitResult(lines.ToArray(), false, clusterCount);
    }

    int FindLineEnd(int start)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int clusterCount = breaks.GetClusterCount();
        int lastFittingBreak = -1;
        int boundary = start + 1;
        while (boundary <= clusterCount)
        {
            if (breaks.GetBreak(boundary) == TextBreakKind.Mandatory &&
                boundary > 0 &&
                breaks.IsHardBreak(boundary - 1))
            {
                return boundary;
            }

            double contentAdvance = GetVisibleAdvance(start, boundary);
            double candidateAdvance = contentAdvance;
            if (breaks.RequiresHyphen(boundary))
            {
                candidateAdvance += hyphenAdvance;
            }

            if (breaks.CanBreak(boundary) && candidateAdvance <= maximumAdvance)
            {
                lastFittingBreak = boundary;
            }

            if (contentAdvance > maximumAdvance)
            {
                if (lastFittingBreak > start)
                {
                    return lastFittingBreak;
                }

                if (breaks.CanBreak(boundary))
                {
                    return boundary;
                }
            }

            boundary++;
        }

        return clusterCount;
    }

    GreedyLineFit BuildLine(int start, int end)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int visibleStart = GetVisibleStart(start, end);
        int visibleEnd = GetVisibleEnd(start, end);
        double contentAdvance = GetAdvance(visibleStart, visibleEnd);
        bool requiresHyphen = end > start &&
            end < breaks.GetClusterCount() &&
            breaks.RequiresHyphen(end);
        double advance = contentAdvance +
            (requiresHyphen ? hyphenAdvance : 0);
        bool hardBreak = end > start && breaks.IsHardBreak(end - 1);
        return new GreedyLineFit(
            start,
            end,
            visibleStart,
            visibleEnd,
            contentAdvance,
            advance,
            GetInlineOffset(advance),
            requiresHyphen,
            false,
            hardBreak,
            null);
    }

    GreedyLineFit BuildEllipsizedLine(int start, int end)
    {
        int candidateEnd = end;
        double available = maximumAdvance - ellipsisAdvance;
        int selectedEnd = start;
        int boundary = start;
        while (boundary <= candidateEnd)
        {
            bool allowed = boundary == start || CanEllipsizeAt(boundary);
            int visibleEnd = GetVisibleEnd(start, boundary);
            int visibleStart = GetVisibleStart(start, visibleEnd);
            double candidateAdvance = GetAdvance(visibleStart, visibleEnd);
            if (allowed && candidateAdvance <= available)
            {
                selectedEnd = boundary;
            }

            boundary++;
        }

        int selectedVisibleStart = GetVisibleStart(start, selectedEnd);
        int selectedVisibleEnd = GetVisibleEnd(start, selectedEnd);
        double contentAdvance = GetAdvance(
            selectedVisibleStart,
            selectedVisibleEnd);
        double advance = contentAdvance + ellipsisAdvance;
        System.ArgumentNullException.ThrowIfNull(options);
        return new GreedyLineFit(
            start,
            selectedEnd,
            selectedVisibleStart,
            selectedVisibleEnd,
            contentAdvance,
            advance,
            GetInlineOffset(advance),
            false,
            true,
            false,
            options.GetEllipsisText());
    }

    bool CanEllipsizeAt(int boundary)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        if (!breaks.CanTrimAt(boundary))
        {
            return false;
        }

        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetTrimmingMode() == TextTrimmingMode.CharacterEllipsis)
        {
            return true;
        }

        return breaks.CanBreak(boundary) && !breaks.IsEmergency(boundary);
    }

    int GetVisibleStart(int start, int end)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int visibleStart = start;
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.ShouldTrimLineWhitespace())
        {
            while (visibleStart < end &&
                breaks.IsTrimmableWhitespace(visibleStart))
            {
                visibleStart++;
            }
        }

        return visibleStart;
    }

    int GetVisibleEnd(int start, int end)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int visibleEnd = end;
        if (visibleEnd > start && breaks.IsHardBreak(visibleEnd - 1))
        {
            visibleEnd--;
        }

        System.ArgumentNullException.ThrowIfNull(options);
        if (options.ShouldTrimLineWhitespace())
        {
            while (visibleEnd > start &&
                breaks.IsTrimmableWhitespace(visibleEnd - 1))
            {
                visibleEnd--;
            }
        }

        return visibleEnd;
    }

    double GetVisibleAdvance(int start, int end)
    {
        int visibleStart = GetVisibleStart(start, end);
        int visibleEnd = GetVisibleEnd(start, end);
        return GetAdvance(visibleStart, visibleEnd);
    }

    double GetAdvance(int start, int end)
    {
        return prefixAdvances[end] - prefixAdvances[start];
    }

    double GetInlineOffset(double advance)
    {
        double remaining = maximumAdvance - advance;
        if (remaining < 0)
        {
            remaining = 0;
        }

        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetAlignment() == TextAlignment.End)
        {
            return remaining;
        }

        if (options.GetAlignment() == TextAlignment.Center)
        {
            return remaining / 2;
        }

        return 0;
    }
}

internal static class GreedyLineFitter
{
    internal static GreedyLineFitResult Fit(
        TextBreakMap? breaks,
        double[]? clusterAdvances,
        double maximumAdvance,
        TextLayoutOptions? options,
        double hyphenAdvance,
        double ellipsisAdvance)
    {
        ValidateInputs(
            breaks,
            clusterAdvances,
            maximumAdvance,
            options,
            hyphenAdvance,
            ellipsisAdvance);
        double[] prefixAdvances = BuildPrefixAdvances(clusterAdvances);
        GreedyLineFitterContext context = new GreedyLineFitterContext(
            breaks,
            prefixAdvances,
            maximumAdvance,
            options,
            hyphenAdvance,
            ellipsisAdvance);
        return context.Fit();
    }

    static double[] BuildPrefixAdvances(double[]? clusterAdvances)
    {
        System.ArgumentNullException.ThrowIfNull(clusterAdvances);
        double[] prefix = new double[clusterAdvances.Length + 1];
        int index = 0;
        while (index < clusterAdvances.Length)
        {
            double sum = prefix[index] + clusterAdvances[index];
            if (!double.IsFinite(sum))
            {
                throw new System.ArgumentOutOfRangeException(
                    "clusterAdvances");
            }

            prefix[index + 1] = sum;
            index++;
        }

        return prefix;
    }

    static void ValidateInputs(
        TextBreakMap? breaks,
        double[]? clusterAdvances,
        double maximumAdvance,
        TextLayoutOptions? options,
        double hyphenAdvance,
        double ellipsisAdvance)
    {
        if (breaks == null)
        {
            throw new System.ArgumentNullException("breaks");
        }

        if (clusterAdvances == null)
        {
            throw new System.ArgumentNullException("clusterAdvances");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (clusterAdvances.Length != breaks.GetClusterCount())
        {
            throw new System.ArgumentException(
                "Cluster advances must match the text break map.",
                "clusterAdvances");
        }

        if (!double.IsFinite(maximumAdvance) || maximumAdvance < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumAdvance");
        }

        if (!double.IsFinite(hyphenAdvance) || hyphenAdvance < 0)
        {
            throw new System.ArgumentOutOfRangeException("hyphenAdvance");
        }

        if (!double.IsFinite(ellipsisAdvance) || ellipsisAdvance < 0)
        {
            throw new System.ArgumentOutOfRangeException("ellipsisAdvance");
        }

        int index = 0;
        while (index < clusterAdvances.Length)
        {
            if (!double.IsFinite(clusterAdvances[index]) ||
                clusterAdvances[index] < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    "clusterAdvances");
            }

            index++;
        }
    }
}
