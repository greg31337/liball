namespace ttf;

internal class TextLineRunFragment
{
    int sourceRunIndex;
    int clusterStart;
    int clusterEnd;
    int bidiLevel;
    double advance;
    PreparedRunMetrics? runMetrics;
    double[]? clusterAdvances;

    internal static TextLineRunFragment Create(
        int sourceRunIndex,
        int clusterStart,
        int clusterEnd,
        int bidiLevel,
        double advance)
    {
        return new TextLineRunFragment(
            sourceRunIndex,
            clusterStart,
            clusterEnd,
            bidiLevel,
            advance,
            null,
            null);
    }

    internal TextLineRunFragment(
        int sourceRunIndex,
        int clusterStart,
        int clusterEnd,
        int bidiLevel,
        double advance,
        PreparedRunMetrics? runMetrics,
        double[]? clusterAdvances)
    {
        if (sourceRunIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceRunIndex");
        }

        if (clusterStart < 0)
        {
            throw new System.ArgumentOutOfRangeException("clusterStart");
        }

        if (clusterEnd <= clusterStart)
        {
            throw new System.ArgumentOutOfRangeException("clusterEnd");
        }

        if (bidiLevel < 0 || bidiLevel > 125)
        {
            throw new System.ArgumentOutOfRangeException("bidiLevel");
        }

        if (!double.IsFinite(advance) || advance < 0)
        {
            throw new System.ArgumentOutOfRangeException("advance");
        }

        if (runMetrics != null &&
            runMetrics.GetSourceRunIndex() != sourceRunIndex)
        {
            throw new System.ArgumentException(
                "Prepared metrics must match the source run index.",
                "runMetrics");
        }

        if (clusterAdvances != null &&
            clusterAdvances.Length != clusterEnd - clusterStart)
        {
            throw new System.ArgumentException(
                "Cluster advances must match the fragment range.",
                "clusterAdvances");
        }

        double total = 0;
        int index = 0;
        while (clusterAdvances != null && index < clusterAdvances.Length)
        {
            if (!double.IsFinite(clusterAdvances[index]) ||
                clusterAdvances[index] < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    "clusterAdvances");
            }

            total += clusterAdvances[index];
            index++;
        }

        if (clusterAdvances != null &&
            (!double.IsFinite(total) || total != advance))
        {
            throw new System.ArgumentException(
                "Cluster advances must sum to the fragment advance.",
                "clusterAdvances");
        }

        this.sourceRunIndex = sourceRunIndex;
        this.clusterStart = clusterStart;
        this.clusterEnd = clusterEnd;
        this.bidiLevel = bidiLevel;
        this.advance = advance;
        this.runMetrics = runMetrics;
        if (clusterAdvances != null)
        {
            this.clusterAdvances = new double[clusterAdvances.Length];
            System.Array.Copy(
                clusterAdvances,
                this.clusterAdvances,
                clusterAdvances.Length);
        }
    }

    internal static TextLineRunFragment Create(
        int sourceRunIndex,
        int clusterStart,
        int clusterEnd,
        int bidiLevel,
        double advance,
        PreparedRunMetrics? runMetrics,
        double[] clusterAdvances)
    {
        return new TextLineRunFragment(
            sourceRunIndex,
            clusterStart,
            clusterEnd,
            bidiLevel,
            advance,
            runMetrics,
            clusterAdvances);
    }

    internal int GetSourceRunIndex() { return sourceRunIndex; }
    internal int GetClusterStart() { return clusterStart; }
    internal int GetClusterEnd() { return clusterEnd; }
    internal int GetBidiLevel() { return bidiLevel; }
    internal double GetAdvance() { return advance; }
    internal PreparedRunMetrics? GetRunMetrics() { return runMetrics; }

    internal bool HasClusterAdvances() { return clusterAdvances != null; }

    internal double GetClusterAdvance(int clusterIndex)
    {
        if (clusterAdvances == null ||
            clusterIndex < clusterStart ||
            clusterIndex >= clusterEnd)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }

        return clusterAdvances[clusterIndex - clusterStart];
    }
}

internal class TextLineConstructorContext
{
    GreedyLineFit fit;
    TextBreakMap? breaks;
    TextLineRunFragment[] logicalFragments;
    LineJustificationPlan justification;
    TextLayoutOptions? options;
    double linePosition;

    internal TextLineConstructorContext(
        GreedyLineFit fit,
        TextBreakMap? breaks,
        TextLineRunFragment[] logicalFragments,
        LineJustificationPlan justification,
        TextLayoutOptions? options,
        double linePosition)
    {
        this.fit = fit;
        this.breaks = breaks;
        this.logicalFragments = logicalFragments;
        this.justification = justification;
        this.options = options;
        this.linePosition = linePosition;
    }

    internal TextLine Build()
    {
        int[] visualToLogical = BuildVisualOrder(logicalFragments);
        int[] logicalToVisual = new int[visualToLogical.Length];
        int index = 0;
        while (index < visualToLogical.Length)
        {
            logicalToVisual[visualToLogical[index]] = index;
            index++;
        }

        PositionedRun[] positioned = new PositionedRun[visualToLogical.Length];
        double cursor = fit.GetInlineOffset();
        int visualIndex = 0;
        while (visualIndex < visualToLogical.Length)
        {
            int logicalIndex = visualToLogical[visualIndex];
            TextLineRunFragment fragment = logicalFragments[logicalIndex];
            double positionedAdvance = fragment.GetAdvance() +
                GetJustificationAdvance(fragment);
            double x = GetX(cursor);
            double y = GetY(cursor);
            PositionedGlyph[] glyphs = BuildPositionedGlyphs(
                fragment,
                x,
                y);
            TextInkBounds runInkBounds = BuildInkBounds(glyphs);
            PreparedRunMetrics? metrics = fragment.GetRunMetrics();
            double runAscent = metrics == null ? 0 : metrics.GetAscent();
            double runDescent = metrics == null ? 0 : metrics.GetDescent();
            double runLineGap = metrics == null ? 0 : metrics.GetLineGap();
            FontInstance? resolvedFont = metrics == null ?
                null : metrics.GetResolvedFont();
            int runUtf16Start = GetUtf16Boundary(fragment.GetClusterStart());
            int runUtf16End = GetUtf16Boundary(fragment.GetClusterEnd());
            positioned[visualIndex] = new PositionedRun(
                fragment.GetSourceRunIndex(),
                logicalIndex,
                visualIndex,
                fragment.GetClusterStart(),
                fragment.GetClusterEnd(),
                runUtf16Start,
                runUtf16End - runUtf16Start,
                fragment.GetBidiLevel(),
                GetDirection(fragment.GetBidiLevel()),
                x,
                y,
                fragment.GetAdvance(),
                positionedAdvance,
                runAscent,
                runDescent,
                runLineGap,
                glyphs,
                runInkBounds,
                resolvedFont);
            cursor += positionedAdvance;
            visualIndex++;
        }

        int utf16Start = GetUtf16Boundary(fit.GetClusterStart());
        int utf16End = GetUtf16Boundary(fit.GetClusterEnd());
        int visibleUtf16Start =
            GetUtf16Boundary(fit.GetVisibleClusterStart());
        int visibleUtf16End =
            GetUtf16Boundary(fit.GetVisibleClusterEnd());
        double ascent = GetMaximumAscent(positioned);
        double descent = GetMaximumDescent(positioned);
        double lineGap = GetMaximumLineGap(positioned);
        TextInkBounds inkBounds = BuildRunInkBounds(positioned);
        TextInkBounds advanceBounds = BuildAdvanceBounds(
            ascent,
            descent);
        TextLineCaretPosition[] caretPositions = BuildCaretPositions(
            logicalFragments,
            positioned,
            logicalToVisual);
        System.ArgumentNullException.ThrowIfNull(options);
        return new TextLine(
            fit.GetClusterStart(),
            fit.GetClusterEnd(),
            fit.GetVisibleClusterStart(),
            fit.GetVisibleClusterEnd(),
            utf16Start,
            utf16End - utf16Start,
            visibleUtf16Start,
            visibleUtf16End - visibleUtf16Start,
            options.GetWritingMode(),
            linePosition,
            fit.GetInlineOffset(),
            fit.GetContentAdvance(),
            justification.GetJustifiedAdvance(),
            ascent,
            descent,
            lineGap,
            advanceBounds,
            inkBounds,
            positioned,
            logicalToVisual,
            visualToLogical,
            caretPositions,
            justification,
            GetTabCount(),
            WasWhitespaceTrimmed(),
            fit.RequiresHyphen(),
            fit.HasEllipsis(),
            fit.EndedByHardBreak(),
            fit.HasEllipsis() ? fit.GetEllipsisText() : null);
    }

    int GetTabCount()
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int count = 0;
        int index = fit.GetClusterStart();
        while (index < fit.GetClusterEnd())
        {
            if (breaks.IsTab(index))
            {
                count += 1;
            }
            index += 1;
        }
        return count;
    }

    bool WasWhitespaceTrimmed()
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        int index = fit.GetClusterStart();
        while (index < fit.GetVisibleClusterStart())
        {
            if (breaks.IsTrimmableWhitespace(index))
            {
                return true;
            }
            index += 1;
        }

        int contentEnd = fit.GetClusterEnd();
        if (contentEnd > fit.GetClusterStart() &&
            breaks.IsHardBreak(contentEnd - 1))
        {
            contentEnd -= 1;
        }
        index = fit.GetVisibleClusterEnd();
        while (index < contentEnd && breaks.IsTrimmableWhitespace(index))
        {
            return true;
        }
        return false;
    }

    TextLineCaretPosition[] BuildCaretPositions(
        TextLineRunFragment[] fragments,
        PositionedRun[] positioned,
        int[] logicalToVisual)
    {
        System.Collections.Generic.List<TextLineCaretPosition> positions =
            new System.Collections.Generic.List<TextLineCaretPosition>();
        int logicalIndex = 0;
        while (logicalIndex < fragments.Length)
        {
            TextLineRunFragment fragment = fragments[logicalIndex];
            PositionedRun run = positioned[logicalToVisual[logicalIndex]];
            if (fragment.HasClusterAdvances())
            {
                int boundary = fragment.GetClusterStart();
                while (boundary <= fragment.GetClusterEnd())
                {
                    if (IsSafeCaretBoundary(fragment, boundary))
                    {
                        AddCaretPosition(
                            positions,
                            CreateCaretPosition(
                                fragment,
                                run,
                                boundary,
                                logicalIndex));
                    }

                    boundary++;
                }
            }
            else
            {
                AddFragmentEndpoint(
                    positions,
                    fragment,
                    run,
                    fragment.GetClusterStart(),
                    logicalIndex);
                AddFragmentEndpoint(
                    positions,
                    fragment,
                    run,
                    fragment.GetClusterEnd(),
                    logicalIndex);
            }

            logicalIndex++;
        }

        if (positions.Count == 0)
        {
            System.ArgumentNullException.ThrowIfNull(options);
            double x = options.GetWritingMode() == TextWritingMode.Horizontal ?
                fit.GetInlineOffset() : linePosition;
            double y = GetFallbackCaretY(fit.GetInlineOffset());
            positions.Add(new TextLineCaretPosition(
                fit.GetVisibleClusterStart(),
                -1,
                x,
                y));
            if (fit.GetVisibleClusterEnd() != fit.GetVisibleClusterStart())
            {
                double endInline = fit.GetInlineOffset() + fit.GetAdvance();
                x = options.GetWritingMode() == TextWritingMode.Horizontal ?
                    endInline : linePosition;
                y = GetFallbackCaretY(endInline);
                positions.Add(new TextLineCaretPosition(
                    fit.GetVisibleClusterEnd(),
                    -1,
                    x,
                    y));
            }
        }

        return positions.ToArray();
    }

    void AddFragmentEndpoint(
        System.Collections.Generic.List<TextLineCaretPosition> positions,
        TextLineRunFragment fragment,
        PositionedRun run,
        int boundary,
        int logicalIndex)
    {
        if (IsSafeCaretBoundary(fragment, boundary))
        {
            AddCaretPosition(
                positions,
                CreateCaretPosition(
                    fragment,
                    run,
                    boundary,
                    logicalIndex));
        }
    }

    bool IsSafeCaretBoundary(
        TextLineRunFragment fragment,
        int boundary)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        if (!breaks.CanTrimAt(boundary))
        {
            return false;
        }

        PreparedRunMetrics? metrics = fragment.GetRunMetrics();
        if (metrics == null)
        {
            return true;
        }

        int index = 0;
        while (index < metrics.GetGlyphCount())
        {
            PreparedGlyphMetrics glyph = metrics.GetGlyph(index);
            if (glyph.GetClusterIndex() < boundary &&
                glyph.GetClusterEnd() > boundary)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    TextLineCaretPosition CreateCaretPosition(
        TextLineRunFragment fragment,
        PositionedRun run,
        int boundary,
        int logicalIndex)
    {
        double prefix = GetBoundaryAdvance(fragment, boundary);
        double x;
        double y;
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.Horizontal)
        {
            x = (fragment.GetBidiLevel() & 1) == 0 ?
                run.GetX() + prefix :
                run.GetX() + run.GetPositionedAdvance() - prefix;
            y = run.GetY();
        }
        else
        {
            x = run.GetX();
            y = options.GetWritingMode() ==
                TextWritingMode.VerticalTopToBottom ?
                run.GetY() - prefix :
                run.GetY() + prefix;
        }

        EnsureFinite(x);
        EnsureFinite(y);
        return new TextLineCaretPosition(
            boundary,
            logicalIndex,
            x,
            y);
    }

    double GetBoundaryAdvance(
        TextLineRunFragment fragment,
        int boundary)
    {
        if (!fragment.HasClusterAdvances())
        {
            return boundary == fragment.GetClusterStart() ?
                0 : fragment.GetAdvance();
        }

        double value = 0;
        int clusterIndex = fragment.GetClusterStart();
        while (clusterIndex < boundary)
        {
            value += fragment.GetClusterAdvance(clusterIndex);
            value += justification.GetBoundaryAdjustment(clusterIndex + 1);
            clusterIndex++;
        }

        return value;
    }

    double GetFallbackCaretY(double inlinePosition)
    {
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.Horizontal)
        {
            return linePosition;
        }

        return options.GetWritingMode() ==
            TextWritingMode.VerticalTopToBottom ?
            -inlinePosition : inlinePosition;
    }

    void AddCaretPosition(
        System.Collections.Generic.List<TextLineCaretPosition> positions,
        TextLineCaretPosition value)
    {
        int index = 0;
        while (index < positions.Count)
        {
            TextLineCaretPosition existing = positions[index];
            if (existing.GetClusterBoundary() == value.GetClusterBoundary() &&
                existing.GetGroupIndex() == value.GetGroupIndex() &&
                existing.GetX() == value.GetX() &&
                existing.GetY() == value.GetY())
            {
                return;
            }

            index++;
        }

        positions.Add(value);
    }

    PositionedGlyph[] BuildPositionedGlyphs(
        TextLineRunFragment fragment,
        double runX,
        double runY)
    {
        PreparedRunMetrics? metrics = fragment.GetRunMetrics();
        if (metrics == null)
        {
            return new PositionedGlyph[0];
        }

        int count = CountGlyphs(fragment, metrics);
        PositionedGlyph[] glyphs = new PositionedGlyph[count];
        if (count == 0)
        {
            return glyphs;
        }

        PreparedGlyphMetrics anchor = GetFirstGlyph(fragment, metrics);
        int outputIndex = 0;
        int index = 0;
        while (index < metrics.GetGlyphCount())
        {
            PreparedGlyphMetrics prepared = metrics.GetGlyph(index);
            if (Contains(fragment, prepared.GetClusterIndex()))
            {
                int logicalIndex = GetLocalLogicalIndex(
                    fragment,
                    metrics,
                    prepared.GetLogicalIndex());
                double adjustment = GetAdjustmentBefore(
                    fragment.GetClusterStart(),
                    prepared.GetClusterIndex());
                double x = runX + prepared.GetX() - anchor.GetX();
                double y = runY + prepared.GetY() - anchor.GetY();
                System.ArgumentNullException.ThrowIfNull(options);
                if (options.GetWritingMode() == TextWritingMode.Horizontal)
                {
                    x += adjustment;
                }
                else if (options.GetWritingMode() ==
                    TextWritingMode.VerticalTopToBottom)
                {
                    y -= adjustment;
                }
                else
                {
                    y += adjustment;
                }

                EnsureFinite(x);
                EnsureFinite(y);
                TextInkBounds bounds = Translate(
                    prepared.GetInkBounds(),
                    x,
                    y);
                int glyphUtf16Start = GetUtf16Boundary(
                    prepared.GetClusterIndex());
                int glyphUtf16End = GetUtf16Boundary(
                    prepared.GetClusterEnd());
                glyphs[outputIndex] = new PositionedGlyph(
                    prepared.GetGlyphId(),
                    prepared.GetClusterIndex(),
                    prepared.GetClusterEnd(),
                    glyphUtf16Start,
                    glyphUtf16End - glyphUtf16Start,
                    logicalIndex,
                    outputIndex,
                    x,
                    y,
                    prepared.GetXAdvance(),
                    prepared.GetYAdvance(),
                    GetGlyphJustificationAdvance(
                        fragment,
                        metrics,
                        index),
                    bounds,
                    metrics.GetResolvedFont());
                outputIndex++;
            }

            index++;
        }

        return glyphs;
    }

    double GetGlyphJustificationAdvance(
        TextLineRunFragment fragment,
        PreparedRunMetrics metrics,
        int visualIndex)
    {
        PreparedGlyphMetrics glyph = metrics.GetGlyph(visualIndex);
        int index = visualIndex + 1;
        while (index < metrics.GetGlyphCount())
        {
            PreparedGlyphMetrics later = metrics.GetGlyph(index);
            if (Contains(fragment, later.GetClusterIndex()) &&
                later.GetClusterIndex() == glyph.GetClusterIndex())
            {
                return 0;
            }

            index++;
        }

        return justification.GetBoundaryAdjustment(
            glyph.GetClusterIndex() + 1);
    }

    int CountGlyphs(
        TextLineRunFragment fragment,
        PreparedRunMetrics metrics)
    {
        int count = 0;
        int index = 0;
        while (index < metrics.GetGlyphCount())
        {
            if (Contains(fragment, metrics.GetGlyph(index).GetClusterIndex()))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    PreparedGlyphMetrics GetFirstGlyph(
        TextLineRunFragment fragment,
        PreparedRunMetrics metrics)
    {
        int index = 0;
        while (index < metrics.GetGlyphCount())
        {
            PreparedGlyphMetrics glyph = metrics.GetGlyph(index);
            if (Contains(fragment, glyph.GetClusterIndex()))
            {
                return glyph;
            }

            index++;
        }

        throw new System.InvalidOperationException(
            "A counted prepared glyph could not be found.");
    }

    int GetLocalLogicalIndex(
        TextLineRunFragment fragment,
        PreparedRunMetrics metrics,
        int logicalIndex)
    {
        int result = 0;
        int index = 0;
        while (index < metrics.GetGlyphCount())
        {
            PreparedGlyphMetrics glyph = metrics.GetGlyph(index);
            if (Contains(fragment, glyph.GetClusterIndex()) &&
                glyph.GetLogicalIndex() < logicalIndex)
            {
                result++;
            }

            index++;
        }

        return result;
    }

    bool Contains(TextLineRunFragment fragment, int clusterIndex)
    {
        return clusterIndex >= fragment.GetClusterStart() &&
            clusterIndex < fragment.GetClusterEnd();
    }

    double GetAdjustmentBefore(int clusterStart, int clusterIndex)
    {
        double adjustment = 0;
        int boundary = clusterStart + 1;
        while (boundary <= clusterIndex)
        {
            adjustment += justification.GetBoundaryAdjustment(boundary);
            boundary++;
        }

        return adjustment;
    }

    TextInkBounds Translate(TextInkBounds? bounds, double x, double y)
    {
        System.ArgumentNullException.ThrowIfNull(bounds);
        if (bounds.IsEmpty())
        {
            return TextInkBounds.Create();
        }

        double xMinimum = x + bounds.GetXMinimum();
        double yMinimum = y + bounds.GetYMinimum();
        double xMaximum = x + bounds.GetXMaximum();
        double yMaximum = y + bounds.GetYMaximum();
        EnsureFinite(xMinimum);
        EnsureFinite(yMinimum);
        EnsureFinite(xMaximum);
        EnsureFinite(yMaximum);
        return TextInkBounds.Create(
            xMinimum,
            yMinimum,
            xMaximum,
            yMaximum);
    }

    TextInkBounds BuildInkBounds(PositionedGlyph[] glyphs)
    {
        TextInkBoundsBuilder builder = new TextInkBoundsBuilder();
        int index = 0;
        while (index < glyphs.Length)
        {
            builder.IncludeBounds(glyphs[index].GetInkBounds(), 0, 0);
            index++;
        }

        return builder.Build();
    }

    TextInkBounds BuildRunInkBounds(PositionedRun[] runs)
    {
        TextInkBoundsBuilder builder = new TextInkBoundsBuilder();
        int index = 0;
        while (index < runs.Length)
        {
            builder.IncludeBounds(runs[index].GetInkBounds(), 0, 0);
            index++;
        }

        return builder.Build();
    }

    TextInkBounds BuildAdvanceBounds(double ascent, double descent)
    {
        double start = fit.GetInlineOffset();
        double end = start + justification.GetJustifiedAdvance();
        EnsureFinite(end);
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.Horizontal)
        {
            return TextInkBounds.Create(
                start,
                linePosition - descent,
                end,
                linePosition + ascent);
        }

        double xMinimum = linePosition - descent;
        double xMaximum = linePosition + ascent;
        if (options.GetWritingMode() == TextWritingMode.VerticalTopToBottom)
        {
            return TextInkBounds.Create(xMinimum, -end, xMaximum, -start);
        }

        return TextInkBounds.Create(xMinimum, start, xMaximum, end);
    }

    double GetMaximumAscent(PositionedRun[] runs)
    {
        double value = 0;
        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index].GetAscent() > value)
            {
                value = runs[index].GetAscent();
            }

            index++;
        }

        return value;
    }

    double GetMaximumDescent(PositionedRun[] runs)
    {
        double value = 0;
        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index].GetDescent() > value)
            {
                value = runs[index].GetDescent();
            }

            index++;
        }

        return value;
    }

    double GetMaximumLineGap(PositionedRun[] runs)
    {
        double value = 0;
        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index].GetLineGap() > value)
            {
                value = runs[index].GetLineGap();
            }

            index++;
        }

        return value;
    }

    void EnsureFinite(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontLimitException.Create(
                "Positioned text metrics exceed finite layout coordinates.");
        }
    }

    double GetJustificationAdvance(TextLineRunFragment fragment)
    {
        double adjustment = 0;
        int boundary = fragment.GetClusterStart() + 1;
        while (boundary <= fragment.GetClusterEnd())
        {
            adjustment += justification.GetBoundaryAdjustment(boundary);
            boundary++;
        }

        return adjustment;
    }

    double GetX(double cursor)
    {
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.Horizontal)
        {
            return cursor;
        }

        return linePosition;
    }

    double GetY(double cursor)
    {
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.Horizontal)
        {
            return linePosition;
        }

        if (options.GetWritingMode() == TextWritingMode.VerticalTopToBottom)
        {
            return -cursor;
        }

        return cursor;
    }

    ShapingDirection GetDirection(int bidiLevel)
    {
        System.ArgumentNullException.ThrowIfNull(options);
        if (options.GetWritingMode() == TextWritingMode.VerticalTopToBottom)
        {
            return ShapingDirection.TopToBottom;
        }

        if (options.GetWritingMode() == TextWritingMode.VerticalBottomToTop)
        {
            return ShapingDirection.BottomToTop;
        }

        return (bidiLevel & 1) == 0 ?
            ShapingDirection.LeftToRight : ShapingDirection.RightToLeft;
    }

    int GetUtf16Boundary(int clusterBoundary)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);
        return clusterBoundary == breaks.GetClusterCount() ?
            breaks.GetSourceLength() :
            breaks.GetUtf16Start(clusterBoundary);
    }

    static int[] BuildVisualOrder(TextLineRunFragment[] fragments)
    {
        int[] order = new int[fragments.Length];
        int maximum = 0;
        int minimumOdd = 127;
        int index = 0;
        while (index < fragments.Length)
        {
            order[index] = index;
            int level = fragments[index].GetBidiLevel();
            if (level > maximum)
            {
                maximum = level;
            }

            if ((level & 1) != 0 && level < minimumOdd)
            {
                minimumOdd = level;
            }

            index++;
        }

        int threshold = maximum;
        while (threshold >= minimumOdd)
        {
            index = 0;
            while (index < order.Length)
            {
                while (index < order.Length &&
                    fragments[order[index]].GetBidiLevel() < threshold)
                {
                    index++;
                }

                int start = index;
                while (index < order.Length &&
                    fragments[order[index]].GetBidiLevel() >= threshold)
                {
                    index++;
                }

                Reverse(order, start, index - 1);
            }

            threshold--;
        }

        return order;
    }

    static void Reverse(int[] values, int start, int end)
    {
        while (start < end)
        {
            int temporary = values[start];
            values[start] = values[end];
            values[end] = temporary;
            start++;
            end--;
        }
    }
}

internal static class TextLineConstructor
{
    internal static TextLine Construct(
        GreedyLineFit fit,
        TextBreakMap? breaks,
        TextLineRunFragment[] fragments,
        LineJustificationPlan justification,
        TextLayoutOptions? options,
        double linePosition)
    {
        ValidateInputs(
            fit,
            breaks,
            fragments,
            justification,
            options,
            linePosition);
        TextLineRunFragment[] logicalFragments = CopyAndSort(fragments);
        ValidateFragmentRanges(fit, logicalFragments);
        TextLineConstructorContext context = new TextLineConstructorContext(
            fit,
            breaks,
            logicalFragments,
            justification,
            options,
            linePosition);
        return context.Build();
    }

    static TextLineRunFragment[] CopyAndSort(TextLineRunFragment[] values)
    {
        TextLineRunFragment[] copy = new TextLineRunFragment[values.Length];
        System.Array.Copy(values, copy, values.Length);
        int index = 1;
        while (index < copy.Length)
        {
            TextLineRunFragment value = copy[index];
            int target = index;
            while (target > 0 &&
                ComesAfter(copy[target - 1], value))
            {
                copy[target] = copy[target - 1];
                target--;
            }

            copy[target] = value;
            index++;
        }

        return copy;
    }

    static bool ComesAfter(
        TextLineRunFragment first,
        TextLineRunFragment second)
    {
        if (first.GetClusterStart() != second.GetClusterStart())
        {
            return first.GetClusterStart() > second.GetClusterStart();
        }

        if (first.GetClusterEnd() != second.GetClusterEnd())
        {
            return first.GetClusterEnd() > second.GetClusterEnd();
        }

        return first.GetSourceRunIndex() > second.GetSourceRunIndex();
    }

    static void ValidateFragmentRanges(
        GreedyLineFit fit,
        TextLineRunFragment[] fragments)
    {
        int previousEnd = fit.GetVisibleClusterStart();
        int index = 0;
        while (index < fragments.Length)
        {
            TextLineRunFragment fragment = fragments[index];
            if (fragment.GetClusterStart() < fit.GetVisibleClusterStart() ||
                fragment.GetClusterEnd() > fit.GetVisibleClusterEnd())
            {
                throw new System.ArgumentException(
                    "A run fragment is outside the visible fitted line.",
                    "fragments");
            }

            if (fragment.GetClusterStart() < previousEnd)
            {
                throw new System.ArgumentException(
                    "Run fragments overlap in logical source order.",
                    "fragments");
            }

            previousEnd = fragment.GetClusterEnd();
            index++;
        }
    }

    static void ValidateInputs(
        GreedyLineFit fit,
        TextBreakMap? breaks,
        TextLineRunFragment[] fragments,
        LineJustificationPlan justification,
        TextLayoutOptions? options,
        double linePosition)
    {
        if (fit == null)
        {
            throw new System.ArgumentNullException("fit");
        }

        if (breaks == null)
        {
            throw new System.ArgumentNullException("breaks");
        }

        if (fragments == null)
        {
            throw new System.ArgumentNullException("fragments");
        }

        if (justification == null)
        {
            throw new System.ArgumentNullException("justification");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!double.IsFinite(linePosition))
        {
            throw new System.ArgumentOutOfRangeException("linePosition");
        }

        if (fit.GetClusterStart() < 0 ||
            fit.GetClusterEnd() > breaks.GetClusterCount())
        {
            throw new System.ArgumentException(
                "The fitted line does not belong to the text break map.",
                "fit");
        }

        if (justification.GetClusterStart() != fit.GetClusterStart() ||
            justification.GetClusterEnd() != fit.GetClusterEnd())
        {
            throw new System.ArgumentException(
                "The justification plan does not belong to the fitted line.",
                "justification");
        }

        int index = 0;
        while (index < fragments.Length)
        {
            if (fragments[index] == null)
            {
                throw new System.ArgumentException(
                    "A line run fragment cannot be null.",
                    "fragments");
            }

            index++;
        }
    }
}
