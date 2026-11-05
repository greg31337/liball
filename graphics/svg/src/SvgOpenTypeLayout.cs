namespace svg;

internal class SvgLayoutReader
{
    internal byte[]? Bytes;
    internal int Start;
    internal int Length;

    internal SvgLayoutReader(byte[]? bytes, SvgOpenTypeTable table)
    {
        Bytes = bytes;
        Start = table.Offset;
        Length = table.Length;
    }

    internal bool Valid(int offset, int length)
    {
        return offset >= 0 && length >= 0 && (long)offset + length <= Length;
    }

    internal int U16(int offset)
    {
        if (!Valid(offset, 2)) return -1;
        return (Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[Start + offset] << 8 | Bytes[Start + offset + 1];
    }

    internal int I16(int offset)
    {
        int value = U16(offset);
        if (value < 0) return 0;
        return value >= 32768 ? value - 65536 : value;
    }

    internal uint U32(int offset)
    {
        if (!Valid(offset, 4)) return 0;
        return (uint)(Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[Start + offset] << 24 | (uint)Bytes[Start + offset + 1] << 16 | (uint)Bytes[Start + offset + 2] << 8 | Bytes[Start + offset + 3];
    }

    internal string Tag(int offset)
    {
        if (!Valid(offset, 4)) return "";
        return new string(new char[] { (char)(Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[Start + offset], (char)Bytes[Start + offset + 1], (char)Bytes[Start + offset + 2], (char)Bytes[Start + offset + 3] });
    }
}

internal class SvgLayoutLookupSelection
{
    internal int Index;
    internal string Feature;

    internal SvgLayoutLookupSelection(int index, string feature)
    {
        Index = index;
        Feature = feature;
    }
}

internal class SvgLayoutValue
{
    internal int XPlacement;
    internal int YPlacement;
    internal int XAdvance;
    internal int YAdvance;
    internal int Size;

    internal SvgLayoutValue()
    {
    }
}

internal class SvgShapingState
{
    internal SvgFont? Font;
    internal SvgShapingOptions Options;
    internal SvgShapingResult Result;
    internal SvgUnicodeText Text;
    internal int Operations;
    internal double Scale;
    internal bool GposKerning;

    internal SvgShapingState(SvgFont? font, SvgUnicodeText text, SvgShapingOptions options, SvgShapingResult result)
    {
        Font = font;
        Text = text;
        Options = options;
        Result = result;
        Operations = 0;
        Scale = options.EmSize / ((font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).UnitsPerEm;
        GposKerning = false;
    }
}
