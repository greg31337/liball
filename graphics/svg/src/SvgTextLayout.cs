namespace svg;

internal class SvgResolvedTextPosition
{
    internal bool HasX;
    internal bool HasY;
    internal double X;
    internal double Y;
    internal double Dx;
    internal double Dy;
    internal double Rotation;

    internal SvgResolvedTextPosition()
    {
        HasX = false;
        HasY = false;
        X = 0;
        Y = 0;
        Dx = 0;
        Dy = 0;
        Rotation = 0;
    }
}

internal class SvgTextLayoutItem
{
    internal SvgShapedGlyph? Glyph;
    internal int CharacterIndex;
    internal int ChunkIndex;
    internal SvgResolvedTextPosition? Position;
    internal bool WordSpace;
    internal double InlineDelta;
    internal double CrossDelta;
    internal double Advance;
    internal double Gap;
    internal double Scale;

    internal SvgTextLayoutItem()
    {
        Glyph = null;
        CharacterIndex = -1;
        ChunkIndex = -1;
        Position = null;
        WordSpace = false;
        InlineDelta = 0;
        CrossDelta = 0;
        Advance = 0;
        Gap = 0;
        Scale = 1;
    }
}
