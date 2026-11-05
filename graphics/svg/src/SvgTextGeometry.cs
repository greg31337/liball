namespace svg;

internal class SvgTextGeometryInputGlyph
{
    internal SvgPositionedGlyph? Glyph;
    internal SvgPoint? Point;
    internal double Rotation;
    internal double MethodScale;
    internal bool Visible;

    internal SvgTextGeometryInputGlyph()
    {
        Glyph = null;
        Point = null;
        Rotation = 0;
        MethodScale = 1;
        Visible = true;
    }
}

internal class SvgTextGeometryInput
{
    internal string Text;
    internal SvgWritingMode WritingMode;
    internal SvgTextDirection Direction;
    internal double FontSize;
    internal double Ascent;
    internal double Descent;
    internal double XHeight;
    internal System.Collections.Generic.List<SvgTextGeometryInputGlyph> Glyphs;

    internal SvgTextGeometryInput()
    {
        Text = "";
        WritingMode = SvgWritingMode.HorizontalTopToBottom;
        Direction = SvgTextDirection.LeftToRight;
        FontSize = 16;
        Ascent = 12.8;
        Descent = 3.2;
        XHeight = 8;
        Glyphs = new System.Collections.Generic.List<SvgTextGeometryInputGlyph>();
    }
}

internal class SvgTextOutlineCacheEntry
{
    internal SvgTextFontResource Resource;
    internal int GlyphIndex;
    internal SvgPathData Path;

    internal SvgTextOutlineCacheEntry(SvgTextFontResource resource, int glyphIndex, SvgPathData path)
    {
        Resource = resource;
        GlyphIndex = glyphIndex;
        Path = path;
    }
}
