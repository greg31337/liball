namespace ttf;

internal class PositionedGlyph
{
    int glyphId;
    int clusterIndex;
    int clusterEnd;
    int utf16Start;
    int utf16Length;
    int logicalIndex;
    int visualIndex;
    double x;
    double y;
    double xAdvance;
    double yAdvance;
    double justificationAdvance;
    TextInkBounds inkBounds;
    FontInstance resolvedFont;

    internal PositionedGlyph(
        int glyphId,
        int clusterIndex,
        int clusterEnd,
        int utf16Start,
        int utf16Length,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        double justificationAdvance,
        TextInkBounds inkBounds,
        FontInstance resolvedFont)
    {
        this.glyphId = glyphId;
        this.clusterIndex = clusterIndex;
        this.clusterEnd = clusterEnd;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.logicalIndex = logicalIndex;
        this.visualIndex = visualIndex;
        this.x = x;
        this.y = y;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.justificationAdvance = justificationAdvance;
        this.inkBounds = inkBounds;
        this.resolvedFont = resolvedFont;
    }

    internal int GetGlyphId() { return glyphId; }
    internal int GetClusterIndex() { return clusterIndex; }
    internal int GetClusterEnd() { return clusterEnd; }
    internal int GetUtf16Start() { return utf16Start; }
    internal int GetUtf16Length() { return utf16Length; }
    internal int GetLogicalIndex() { return logicalIndex; }
    internal int GetVisualIndex() { return visualIndex; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
    internal double GetXAdvance() { return xAdvance; }
    internal double GetYAdvance() { return yAdvance; }
    internal double GetJustificationAdvance() { return justificationAdvance; }
    internal TextInkBounds GetInkBounds() { return inkBounds; }
    internal FontInstance GetResolvedFont() { return resolvedFont; }
    internal FontInstance GetFontInstance() { return resolvedFont; }
    internal FontFace GetFontFace() { return resolvedFont.GetFace(); }
}
