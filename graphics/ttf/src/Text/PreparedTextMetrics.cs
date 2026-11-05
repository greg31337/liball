namespace ttf;

internal class PreparedGlyphMetrics
{
    int glyphId;
    int clusterIndex;
    int clusterEnd;
    int logicalIndex;
    int visualIndex;
    double x;
    double y;
    double xAdvance;
    double yAdvance;
    TextInkBounds? inkBounds;

    internal static PreparedGlyphMetrics Create(
        int glyphId,
        int clusterIndex,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        TextInkBounds inkBounds)
    {
        if (clusterIndex == int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }

        return new PreparedGlyphMetrics(
            glyphId,
            clusterIndex,
            clusterIndex + 1,
            logicalIndex,
            visualIndex,
            x,
            y,
            xAdvance,
            yAdvance,
            inkBounds);
    }

    internal PreparedGlyphMetrics(
        int glyphId,
        int clusterIndex,
        int clusterEnd,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        TextInkBounds inkBounds)
    {
        Initialize(
            glyphId,
            clusterIndex,
            clusterEnd,
            logicalIndex,
            visualIndex,
            x,
            y,
            xAdvance,
            yAdvance,
            inkBounds);
    }

    internal static PreparedGlyphMetrics Create(
        int glyphId,
        int clusterIndex,
        int clusterEnd,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        TextInkBounds inkBounds)
    {
        return new PreparedGlyphMetrics(
            glyphId,
            clusterIndex,
            clusterEnd,
            logicalIndex,
            visualIndex,
            x,
            y,
            xAdvance,
            yAdvance,
            inkBounds);
    }

    void Initialize(
        int glyphId,
        int clusterIndex,
        int clusterEnd,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        TextInkBounds inkBounds)
    {
        if (glyphId < 0)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        if (clusterIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("clusterIndex");
        }

        if (clusterEnd <= clusterIndex)
        {
            throw new System.ArgumentOutOfRangeException("clusterEnd");
        }

        if (logicalIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("logicalIndex");
        }

        if (visualIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("visualIndex");
        }

        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        ValidateFinite(xAdvance, "xAdvance");
        ValidateFinite(yAdvance, "yAdvance");
        if (inkBounds == null)
        {
            throw new System.ArgumentNullException("inkBounds");
        }

        this.glyphId = glyphId;
        this.clusterIndex = clusterIndex;
        this.clusterEnd = clusterEnd;
        this.logicalIndex = logicalIndex;
        this.visualIndex = visualIndex;
        this.x = x;
        this.y = y;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.inkBounds = inkBounds;
    }

    internal int GetGlyphId() { return glyphId; }
    internal int GetClusterIndex() { return clusterIndex; }
    internal int GetClusterEnd() { return clusterEnd; }
    internal int GetLogicalIndex() { return logicalIndex; }
    internal int GetVisualIndex() { return visualIndex; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
    internal double GetXAdvance() { return xAdvance; }
    internal double GetYAdvance() { return yAdvance; }
    internal TextInkBounds? GetInkBounds() { return inkBounds; }

    static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal class PreparedRunMetrics
{
    int sourceRunIndex;
    FontInstance resolvedFont;
    double ascent;
    double descent;
    double lineGap;
    PreparedGlyphMetrics[] glyphs;

    internal PreparedRunMetrics(
        int sourceRunIndex,
        FontInstance resolvedFont,
        double ascent,
        double descent,
        double lineGap,
        PreparedGlyphMetrics[] glyphs)
    {
        if (sourceRunIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceRunIndex");
        }

        if (resolvedFont == null)
        {
            throw new System.ArgumentNullException("resolvedFont");
        }

        ValidateNonnegativeFinite(ascent, "ascent");
        ValidateNonnegativeFinite(descent, "descent");
        ValidateNonnegativeFinite(lineGap, "lineGap");
        if (glyphs == null)
        {
            throw new System.ArgumentNullException("glyphs");
        }

        PreparedGlyphMetrics[] copy = new PreparedGlyphMetrics[glyphs.Length];
        int index = 0;
        while (index < glyphs.Length)
        {
            if (glyphs[index] == null)
            {
                throw new System.ArgumentException(
                    "A prepared glyph cannot be null.",
                    "glyphs");
            }

            copy[index] = glyphs[index];
            index++;
        }

        SortByVisualIndex(copy);
        ValidateMappings(copy);
        this.sourceRunIndex = sourceRunIndex;
        this.resolvedFont = resolvedFont;
        this.ascent = ascent;
        this.descent = descent;
        this.lineGap = lineGap;
        this.glyphs = copy;
    }

    internal int GetSourceRunIndex() { return sourceRunIndex; }
    internal FontInstance GetResolvedFont() { return resolvedFont; }
    internal double GetAscent() { return ascent; }
    internal double GetDescent() { return descent; }
    internal double GetLineGap() { return lineGap; }
    internal int GetGlyphCount() { return glyphs.Length; }

    internal PreparedGlyphMetrics GetGlyph(int visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= glyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("visualIndex");
        }

        return glyphs[visualIndex];
    }

    static void SortByVisualIndex(PreparedGlyphMetrics[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            PreparedGlyphMetrics value = values[index];
            int target = index;
            while (target > 0 &&
                values[target - 1].GetVisualIndex() > value.GetVisualIndex())
            {
                values[target] = values[target - 1];
                target--;
            }

            values[target] = value;
            index++;
        }
    }

    static void ValidateMappings(PreparedGlyphMetrics[] values)
    {
        bool[] logical = new bool[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            PreparedGlyphMetrics glyph = values[index];
            if (glyph.GetVisualIndex() != index ||
                glyph.GetLogicalIndex() >= values.Length ||
                logical[glyph.GetLogicalIndex()])
            {
                throw new System.ArgumentException(
                    "Prepared glyph indexes must form logical and visual permutations.",
                    "glyphs");
            }

            logical[glyph.GetLogicalIndex()] = true;
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
