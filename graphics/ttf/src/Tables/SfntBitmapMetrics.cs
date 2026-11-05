namespace ttf;

internal class SfntBitmapLineMetrics
{
    int ascender;
    int descender;
    int widthMax;
    int caretSlopeNumerator;
    int caretSlopeDenominator;
    int caretOffset;
    int minOriginSideBearing;
    int minAdvanceSideBearing;
    int maxBeforeBaseline;
    int minAfterBaseline;

    internal SfntBitmapLineMetrics(
        int ascender,
        int descender,
        int widthMax,
        int caretSlopeNumerator,
        int caretSlopeDenominator,
        int caretOffset,
        int minOriginSideBearing,
        int minAdvanceSideBearing,
        int maxBeforeBaseline,
        int minAfterBaseline)
    {
        this.ascender = ascender;
        this.descender = descender;
        this.widthMax = widthMax;
        this.caretSlopeNumerator = caretSlopeNumerator;
        this.caretSlopeDenominator = caretSlopeDenominator;
        this.caretOffset = caretOffset;
        this.minOriginSideBearing = minOriginSideBearing;
        this.minAdvanceSideBearing = minAdvanceSideBearing;
        this.maxBeforeBaseline = maxBeforeBaseline;
        this.minAfterBaseline = minAfterBaseline;
    }

    internal int GetAscender()
    {
        return ascender;
    }

    internal int GetDescender()
    {
        return descender;
    }

    internal int GetWidthMax()
    {
        return widthMax;
    }

    internal int GetCaretSlopeNumerator()
    {
        return caretSlopeNumerator;
    }

    internal int GetCaretSlopeDenominator()
    {
        return caretSlopeDenominator;
    }

    internal int GetCaretOffset()
    {
        return caretOffset;
    }

    internal int GetMinOriginSideBearing()
    {
        return minOriginSideBearing;
    }

    internal int GetMinAdvanceSideBearing()
    {
        return minAdvanceSideBearing;
    }

    internal int GetMaxBeforeBaseline()
    {
        return maxBeforeBaseline;
    }

    internal int GetMinAfterBaseline()
    {
        return minAfterBaseline;
    }
}

internal class SfntBitmapGlyphMetrics
{
    bool big;
    bool horizontal;
    int height;
    int width;
    int bearingX;
    int bearingY;
    int advance;
    int verticalBearingX;
    int verticalBearingY;
    int verticalAdvance;

    internal SfntBitmapGlyphMetrics(
        bool big,
        bool horizontal,
        int height,
        int width,
        int bearingX,
        int bearingY,
        int advance,
        int verticalBearingX,
        int verticalBearingY,
        int verticalAdvance)
    {
        this.big = big;
        this.horizontal = horizontal;
        this.height = height;
        this.width = width;
        this.bearingX = bearingX;
        this.bearingY = bearingY;
        this.advance = advance;
        this.verticalBearingX = verticalBearingX;
        this.verticalBearingY = verticalBearingY;
        this.verticalAdvance = verticalAdvance;
    }

    internal bool IsBig()
    {
        return big;
    }

    internal bool IsHorizontal()
    {
        return horizontal;
    }

    internal int GetHeight()
    {
        return height;
    }

    internal int GetWidth()
    {
        return width;
    }

    internal int GetBearingX()
    {
        return bearingX;
    }

    internal int GetBearingY()
    {
        return bearingY;
    }

    internal int GetAdvance()
    {
        return advance;
    }

    internal int GetVerticalBearingX()
    {
        if (!big)
        {
            throw new System.InvalidOperationException(
                "Small bitmap metrics do not contain both layout directions.");
        }

        return verticalBearingX;
    }

    internal int GetVerticalBearingY()
    {
        if (!big)
        {
            throw new System.InvalidOperationException(
                "Small bitmap metrics do not contain both layout directions.");
        }

        return verticalBearingY;
    }

    internal int GetVerticalAdvance()
    {
        if (!big)
        {
            throw new System.InvalidOperationException(
                "Small bitmap metrics do not contain both layout directions.");
        }

        return verticalAdvance;
    }
}

internal class SfntBitmapComponent
{
    int glyphId;
    int xOffset;
    int yOffset;

    internal SfntBitmapComponent(int glyphId, int xOffset, int yOffset)
    {
        this.glyphId = glyphId;
        this.xOffset = xOffset;
        this.yOffset = yOffset;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetXOffset()
    {
        return xOffset;
    }

    internal int GetYOffset()
    {
        return yOffset;
    }
}
