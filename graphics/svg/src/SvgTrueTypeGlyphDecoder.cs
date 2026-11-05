namespace svg;

internal class SvgTrueTypeGlyphCursor
{
    internal byte[]? Bytes;
    internal int Position;
    internal int End;

    internal SvgTrueTypeGlyphCursor(byte[]? bytes, int start, int end)
    {
        Bytes = bytes;
        Position = start;
        End = end;
    }

    internal bool Has(int count)
    {
        return count >= 0 && Position >= 0 && (long)Position + count <= End;
    }

    internal int ReadByte()
    {
        int value = (Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[Position];
        Position = Position + 1;
        return value;
    }

    internal int ReadUInt16()
    {
        int value = (Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[Position] << 8 | Bytes[Position + 1];
        Position = Position + 2;
        return value;
    }

    internal int ReadInt16()
    {
        int value = ReadUInt16();
        return value >= 32768 ? value - 65536 : value;
    }

    internal bool Advance(int count)
    {
        if (!Has(count)) return false;
        Position = Position + count;
        return true;
    }
}

internal class SvgTrueTypeGlyphDecodeState
{
    internal SvgFont Font;
    internal SvgTrueTypeGlyphOptions Options;
    internal SvgTrueTypeGlyphError Error;
    internal int[]? Locations;
    internal bool[] ActiveGlyphs;
    internal int ComponentCount;

    internal SvgTrueTypeGlyphDecodeState(SvgFont font, SvgTrueTypeGlyphOptions options)
    {
        Font = font;
        Options = options;
        Error = SvgTrueTypeGlyphError.None;
        Locations = null;
        ActiveGlyphs = new bool[(font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount];
        ComponentCount = 0;
    }
}
