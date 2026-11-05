namespace ttf;

internal class TextLine
{
    int clusterStart;
    int clusterEnd;
    int visibleClusterStart;
    int visibleClusterEnd;
    int utf16Start;
    int utf16Length;
    int visibleUtf16Start;
    int visibleUtf16Length;
    TextWritingMode writingMode;
    double linePosition;
    double inlineOffset;
    double contentAdvance;
    double advance;
    double ascent;
    double descent;
    double lineGap;
    TextInkBounds advanceBounds;
    TextInkBounds inkBounds;
    PositionedRun[] visualRuns;
    int[] logicalToVisual;
    int[] visualToLogical;
    TextLineCaretPosition[] caretPositions;
    LineJustificationPlan justification;
    int tabCount;
    bool whitespaceTrimmed;
    bool insertedHyphen;
    bool hasEllipsis;
    bool endedByHardBreak;
    string? ellipsisText;

    internal TextLine(
        int clusterStart,
        int clusterEnd,
        int visibleClusterStart,
        int visibleClusterEnd,
        int utf16Start,
        int utf16Length,
        int visibleUtf16Start,
        int visibleUtf16Length,
        TextWritingMode writingMode,
        double linePosition,
        double inlineOffset,
        double contentAdvance,
        double advance,
        double ascent,
        double descent,
        double lineGap,
        TextInkBounds advanceBounds,
        TextInkBounds inkBounds,
        PositionedRun[] visualRuns,
        int[] logicalToVisual,
        int[] visualToLogical,
        TextLineCaretPosition[] caretPositions,
        LineJustificationPlan justification,
        int tabCount,
        bool whitespaceTrimmed,
        bool insertedHyphen,
        bool hasEllipsis,
        bool endedByHardBreak,
        string? ellipsisText)
    {
        this.clusterStart = clusterStart;
        this.clusterEnd = clusterEnd;
        this.visibleClusterStart = visibleClusterStart;
        this.visibleClusterEnd = visibleClusterEnd;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.visibleUtf16Start = visibleUtf16Start;
        this.visibleUtf16Length = visibleUtf16Length;
        this.writingMode = writingMode;
        this.linePosition = linePosition;
        this.inlineOffset = inlineOffset;
        this.contentAdvance = contentAdvance;
        this.advance = advance;
        this.ascent = ascent;
        this.descent = descent;
        this.lineGap = lineGap;
        this.advanceBounds = advanceBounds;
        this.inkBounds = inkBounds;
        this.visualRuns = visualRuns;
        this.logicalToVisual = logicalToVisual;
        this.visualToLogical = visualToLogical;
        this.caretPositions = caretPositions;
        this.justification = justification;
        this.tabCount = tabCount;
        this.whitespaceTrimmed = whitespaceTrimmed;
        this.insertedHyphen = insertedHyphen;
        this.hasEllipsis = hasEllipsis;
        this.endedByHardBreak = endedByHardBreak;
        this.ellipsisText = ellipsisText;
    }

    internal int GetClusterStart() { return clusterStart; }
    internal int GetClusterEnd() { return clusterEnd; }
    internal int GetVisibleClusterStart() { return visibleClusterStart; }
    internal int GetVisibleClusterEnd() { return visibleClusterEnd; }
    internal int GetUtf16Start() { return utf16Start; }
    internal int GetUtf16Length() { return utf16Length; }
    internal int GetVisibleUtf16Start() { return visibleUtf16Start; }
    internal int GetVisibleUtf16Length() { return visibleUtf16Length; }
    internal TextWritingMode GetWritingMode() { return writingMode; }
    internal double GetLinePosition() { return linePosition; }
    internal double GetBaseline() { return linePosition; }
    internal double GetInlineOffset() { return inlineOffset; }
    internal double GetContentAdvance() { return contentAdvance; }
    internal double GetAdvance() { return advance; }
    internal double GetAscent() { return ascent; }
    internal double GetDescent() { return descent; }
    internal double GetLineGap() { return lineGap; }
    internal double GetLineHeight() { return ascent + descent + lineGap; }
    internal TextInkBounds GetAdvanceBounds() { return advanceBounds; }
    internal TextInkBounds GetInkBounds() { return inkBounds; }
    internal int GetTabCount() { return tabCount; }
    internal bool WasWhitespaceTrimmed() { return whitespaceTrimmed; }
    internal bool HasInsertedHyphen() { return insertedHyphen; }
    internal bool HasEllipsis() { return hasEllipsis; }
    internal bool EndedByHardBreak() { return endedByHardBreak; }
    internal double GetNaturalAdvance()
    {
        return justification.GetNaturalAdvance();
    }
    internal double GetJustificationTargetAdvance()
    {
        return justification.GetTargetAdvance();
    }
    internal double GetJustificationExpansion()
    {
        return justification.GetJustifiedAdvance() -
            justification.GetNaturalAdvance();
    }
    internal bool IsFullyJustified()
    {
        return justification.IsFullyJustified();
    }
    internal double GetJustificationBoundaryAdjustment(int clusterBoundary)
    {
        return justification.GetBoundaryAdjustment(clusterBoundary);
    }
    internal int GetJustificationApplicationCount()
    {
        return justification.GetApplicationCount();
    }
    internal AppliedJustification GetJustificationApplication(int index)
    {
        return justification.GetApplication(index);
    }

    internal string GetEllipsisText()
    {
        if (!hasEllipsis)
        {
            throw new System.InvalidOperationException(
                "This line has no ellipsis.");
        }
        System.ArgumentNullException.ThrowIfNull(ellipsisText);
        return ellipsisText;
    }

    internal int GetRunCount()
    {
        return visualRuns.Length;
    }

    internal PositionedRun GetVisualRun(int visualIndex)
    {
        ValidateIndex(visualIndex);
        return visualRuns[visualIndex];
    }

    internal PositionedRun GetLogicalRun(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= logicalToVisual.Length)
        {
            throw new System.ArgumentOutOfRangeException("logicalIndex");
        }

        return visualRuns[logicalToVisual[logicalIndex]];
    }

    internal int GetVisualRunIndex(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= logicalToVisual.Length)
        {
            throw new System.ArgumentOutOfRangeException("logicalIndex");
        }

        return logicalToVisual[logicalIndex];
    }

    internal int GetLogicalRunIndex(int visualIndex)
    {
        ValidateIndex(visualIndex);
        return visualToLogical[visualIndex];
    }

    internal int GetCaretPositionCount()
    {
        return caretPositions.Length;
    }

    internal TextLineCaretPosition GetCaretPosition(int index)
    {
        if (index < 0 || index >= caretPositions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return caretPositions[index];
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= visualRuns.Length)
        {
            throw new System.ArgumentOutOfRangeException("visualIndex");
        }
    }
}
