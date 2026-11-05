namespace svg;

internal class SvgOpenTypeMapProbe
{
    internal int GlyphIndex;
    internal bool Valid;

    internal SvgOpenTypeMapProbe(int glyphIndex, bool valid)
    {
        GlyphIndex = glyphIndex;
        Valid = valid;
    }
}

internal class SvgOpenTypeCmapBounds
{
    internal int Start;
    internal int Length;

    internal SvgOpenTypeCmapBounds(int start, int length)
    {
        Start = start;
        Length = length;
    }
}
