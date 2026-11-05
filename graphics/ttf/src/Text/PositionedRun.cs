namespace ttf;

internal class PositionedRun
{
    int sourceRunIndex;
    int logicalIndex;
    int visualIndex;
    int clusterStart;
    int clusterEnd;
    int utf16Start;
    int utf16Length;
    int bidiLevel;
    ShapingDirection direction;
    double x;
    double y;
    double naturalAdvance;
    double positionedAdvance;
    double ascent;
    double descent;
    double lineGap;
    PositionedGlyph[] glyphs;
    int[] logicalToVisualGlyphIndexes;
    TextInkBounds inkBounds;
    FontInstance? resolvedFont;

    internal PositionedRun(
        int sourceRunIndex,
        int logicalIndex,
        int visualIndex,
        int clusterStart,
        int clusterEnd,
        int utf16Start,
        int utf16Length,
        int bidiLevel,
        ShapingDirection direction,
        double x,
        double y,
        double naturalAdvance,
        double positionedAdvance,
        double ascent,
        double descent,
        double lineGap,
        PositionedGlyph[] glyphs,
        TextInkBounds inkBounds,
        FontInstance? resolvedFont)
    {
        this.sourceRunIndex = sourceRunIndex;
        this.logicalIndex = logicalIndex;
        this.visualIndex = visualIndex;
        this.clusterStart = clusterStart;
        this.clusterEnd = clusterEnd;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.bidiLevel = bidiLevel;
        this.direction = direction;
        this.x = x;
        this.y = y;
        this.naturalAdvance = naturalAdvance;
        this.positionedAdvance = positionedAdvance;
        this.ascent = ascent;
        this.descent = descent;
        this.lineGap = lineGap;
        this.glyphs = glyphs;
        this.inkBounds = inkBounds;
        this.resolvedFont = resolvedFont;
        logicalToVisualGlyphIndexes = BuildLogicalToVisual(glyphs);
    }

    internal int GetSourceRunIndex() { return sourceRunIndex; }
    internal int GetLogicalIndex() { return logicalIndex; }
    internal int GetVisualIndex() { return visualIndex; }
    internal int GetClusterStart() { return clusterStart; }
    internal int GetClusterEnd() { return clusterEnd; }
    internal int GetUtf16Start() { return utf16Start; }
    internal int GetUtf16Length() { return utf16Length; }
    internal int GetBidiLevel() { return bidiLevel; }
    internal ShapingDirection GetDirection() { return direction; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
    internal double GetNaturalAdvance() { return naturalAdvance; }
    internal double GetPositionedAdvance() { return positionedAdvance; }
    internal double GetAscent() { return ascent; }
    internal double GetDescent() { return descent; }
    internal double GetLineGap() { return lineGap; }
    internal TextInkBounds GetInkBounds() { return inkBounds; }

    internal bool HasResolvedFont()
    {
        return resolvedFont != null;
    }

    internal FontInstance GetResolvedFont()
    {
        if (resolvedFont == null)
        {
            throw new System.InvalidOperationException(
                "This positioned run has no resolved font metrics.");
        }

        return resolvedFont;
    }

    internal FontInstance GetFontInstance()
    {
        return GetResolvedFont();
    }

    internal FontFace GetFontFace()
    {
        return GetResolvedFont().GetFace();
    }

    internal int GetGlyphCount()
    {
        return glyphs.Length;
    }

    internal PositionedGlyph GetGlyph(int visualGlyphIndex)
    {
        if (visualGlyphIndex < 0 || visualGlyphIndex >= glyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("visualGlyphIndex");
        }

        return glyphs[visualGlyphIndex];
    }

    internal PositionedGlyph GetLogicalGlyph(int logicalGlyphIndex)
    {
        return GetGlyph(GetVisualGlyphIndex(logicalGlyphIndex));
    }

    internal int GetVisualGlyphIndex(int logicalGlyphIndex)
    {
        if (logicalGlyphIndex < 0 ||
            logicalGlyphIndex >= logicalToVisualGlyphIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("logicalGlyphIndex");
        }

        return logicalToVisualGlyphIndexes[logicalGlyphIndex];
    }

    internal int GetLogicalGlyphIndex(int visualGlyphIndex)
    {
        return GetGlyph(visualGlyphIndex).GetLogicalIndex();
    }

    static int[] BuildLogicalToVisual(PositionedGlyph[] values)
    {
        int[] mapping = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            mapping[values[index].GetLogicalIndex()] = index;
            index++;
        }

        return mapping;
    }
}
