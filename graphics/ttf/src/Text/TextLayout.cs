namespace ttf;

internal class TextLayout
{
    string? sourceText;
    double maximumAdvance;
    TextWritingMode writingMode;
    TextLine[] lines;
    bool truncated;
    int truncationClusterIndex;
    int truncationUtf16Index;
    TextInkBounds advanceBounds;
    TextInkBounds inkBounds;
    PositionedRun[] positionedRuns;
    int[] logicalToVisualRunIndexes;
    int[] visualToLogicalRunIndexes;
    FontInstance[] resolvedFonts;
    TextBreakMap breaks;
    TextCaretStop[] caretStops;

    internal TextLayout(
        string? sourceText,
        double maximumAdvance,
        TextWritingMode writingMode,
        TextBreakMap breaks,
        TextLine[] lines,
        bool truncated,
        int truncationClusterIndex,
        int truncationUtf16Index)
    {
        this.sourceText = sourceText;
        this.maximumAdvance = maximumAdvance;
        this.writingMode = writingMode;
        this.breaks = breaks;
        this.lines = lines;
        this.truncated = truncated;
        this.truncationClusterIndex = truncationClusterIndex;
        this.truncationUtf16Index = truncationUtf16Index;
        advanceBounds = BuildBounds(lines, false);
        inkBounds = BuildBounds(lines, true);
        positionedRuns = FlattenRuns(lines);
        logicalToVisualRunIndexes = BuildLogicalToVisual(positionedRuns);
        visualToLogicalRunIndexes = Invert(logicalToVisualRunIndexes);
        resolvedFonts = BuildResolvedFonts(positionedRuns);
        caretStops = TextInteraction.BuildCaretStops(lines, breaks);
    }

    internal string? GetSourceText()
    {
        return sourceText;
    }

    internal double GetMaximumAdvance()
    {
        return maximumAdvance;
    }

    internal TextWritingMode GetWritingMode()
    {
        return writingMode;
    }

    internal int GetLineCount()
    {
        return lines.Length;
    }

    internal TextLine GetLine(int index)
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
        ValidateTruncated();
        return truncationClusterIndex;
    }

    internal int GetTruncationUtf16Index()
    {
        ValidateTruncated();
        return truncationUtf16Index;
    }

    internal TextInkBounds GetAdvanceBounds()
    {
        return advanceBounds;
    }

    internal TextInkBounds GetInkBounds()
    {
        return inkBounds;
    }

    internal TextInkBounds GetBounds()
    {
        return inkBounds;
    }

    internal int GetPositionedRunCount()
    {
        return positionedRuns.Length;
    }

    internal PositionedRun GetPositionedRun(int visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= positionedRuns.Length)
        {
            throw new System.ArgumentOutOfRangeException("visualIndex");
        }

        return positionedRuns[visualIndex];
    }

    internal PositionedRun GetLogicalRun(int logicalIndex)
    {
        return GetPositionedRun(GetVisualRunIndex(logicalIndex));
    }

    internal int GetVisualRunIndex(int logicalIndex)
    {
        if (logicalIndex < 0 ||
            logicalIndex >= logicalToVisualRunIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("logicalIndex");
        }

        return logicalToVisualRunIndexes[logicalIndex];
    }

    internal int GetLogicalRunIndex(int visualIndex)
    {
        GetPositionedRun(visualIndex);
        return visualToLogicalRunIndexes[visualIndex];
    }

    internal int GetResolvedFontCount()
    {
        return resolvedFonts.Length;
    }

    internal FontInstance GetResolvedFont(int index)
    {
        if (index < 0 || index >= resolvedFonts.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return resolvedFonts[index];
    }

    internal int GetCaretStopCount()
    {
        return caretStops.Length;
    }

    internal TextCaretStop GetCaretStop(int index)
    {
        if (index < 0 || index >= caretStops.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return caretStops[index];
    }

    internal bool IsCaretBoundary(int utf16Index)
    {
        System.ArgumentNullException.ThrowIfNull(sourceText);
        if (utf16Index < 0 || utf16Index > sourceText.Length)
        {
            throw new System.ArgumentOutOfRangeException("utf16Index");
        }
        int index = 0;
        while (index < caretStops.Length)
        {
            if (caretStops[index].GetUtf16Index() == utf16Index)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    internal TextHitTestData HitTest(double x, double y)
    {
        return TextInteraction.HitTest(
            caretStops,
            lines,
            writingMode,
            x,
            y);
    }

    internal TextSelectionRectangle[] GetSelection(
        int utf16Start,
        int utf16Length)
    {
        System.ArgumentNullException.ThrowIfNull(sourceText);
        return TextInteraction.GetSelection(
            lines,
            caretStops,
            breaks,
            utf16Start,
            utf16Length,
            sourceText.Length);
    }

    void ValidateTruncated()
    {
        if (!truncated)
        {
            throw new System.InvalidOperationException(
                "The text layout was not truncated.");
        }
    }

    static TextInkBounds BuildBounds(TextLine[] lines, bool ink)
    {
        TextInkBoundsBuilder builder = new TextInkBoundsBuilder();
        int index = 0;
        while (index < lines.Length)
        {
            TextInkBounds bounds = ink ?
                lines[index].GetInkBounds() :
                lines[index].GetAdvanceBounds();
            builder.IncludeBounds(bounds, 0, 0);
            index++;
        }

        return builder.Build();
    }

    static PositionedRun[] FlattenRuns(TextLine[] lines)
    {
        System.Collections.Generic.List<PositionedRun> runs =
            new System.Collections.Generic.List<PositionedRun>();
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            TextLine line = lines[lineIndex];
            int runIndex = 0;
            while (runIndex < line.GetRunCount())
            {
                runs.Add(line.GetVisualRun(runIndex));
                runIndex++;
            }

            lineIndex++;
        }

        return runs.ToArray();
    }

    static int[] BuildLogicalToVisual(PositionedRun[] runs)
    {
        int[] visualIndexes = new int[runs.Length];
        int index = 0;
        while (index < runs.Length)
        {
            visualIndexes[index] = index;
            index++;
        }

        index = 1;
        while (index < visualIndexes.Length)
        {
            int visualIndex = visualIndexes[index];
            int target = index;
            while (target > 0 && ComesAfter(
                runs[visualIndexes[target - 1]],
                runs[visualIndex]))
            {
                visualIndexes[target] = visualIndexes[target - 1];
                target--;
            }

            visualIndexes[target] = visualIndex;
            index++;
        }

        return visualIndexes;
    }

    static bool ComesAfter(PositionedRun first, PositionedRun second)
    {
        if (first.GetUtf16Start() != second.GetUtf16Start())
        {
            return first.GetUtf16Start() > second.GetUtf16Start();
        }

        if (first.GetUtf16Length() != second.GetUtf16Length())
        {
            return first.GetUtf16Length() > second.GetUtf16Length();
        }

        return first.GetSourceRunIndex() > second.GetSourceRunIndex();
    }

    static int[] Invert(int[] logicalToVisual)
    {
        int[] visualToLogical = new int[logicalToVisual.Length];
        int index = 0;
        while (index < logicalToVisual.Length)
        {
            visualToLogical[logicalToVisual[index]] = index;
            index++;
        }

        return visualToLogical;
    }

    static FontInstance[] BuildResolvedFonts(PositionedRun[] runs)
    {
        System.Collections.Generic.List<FontInstance> fonts =
            new System.Collections.Generic.List<FontInstance>();
        int runIndex = 0;
        while (runIndex < runs.Length)
        {
            PositionedRun run = runs[runIndex];
            if (run.HasResolvedFont() && !Contains(
                fonts,
                run.GetResolvedFont()))
            {
                fonts.Add(run.GetResolvedFont());
            }

            runIndex++;
        }

        return fonts.ToArray();
    }

    static bool Contains(
        System.Collections.Generic.List<FontInstance> fonts,
        FontInstance value)
    {
        int index = 0;
        while (index < fonts.Count)
        {
            if (fonts[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
