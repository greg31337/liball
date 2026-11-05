namespace svg;

internal class SvgWebFontTable
{
    internal string? Tag;
    internal int SourceOffset;
    internal int SourceLength;
    internal int OriginalLength;
    internal uint OriginalChecksum;
    internal int TransformVersion;
    internal bool Transformed;
    internal int Order;
    internal int OutputOffset;
    internal byte[]? Data;

    internal SvgWebFontTable(string? tag)
    {
        Tag = tag;
        SourceOffset = 0;
        SourceLength = 0;
        OriginalLength = 0;
        OriginalChecksum = 0;
        TransformVersion = 0;
        Transformed = false;
        Order = 0;
        OutputOffset = 0;
        Data = null;
    }
}

internal class SvgWebFontCursor
{
    internal byte[] Bytes;
    internal int Position;
    internal int End;

    internal SvgWebFontCursor(byte[] bytes, int start, int length)
    {
        Bytes = bytes;
        Position = start;
        End = start + length;
    }

    internal int Remaining()
    {
        return End - Position;
    }

    internal int ReadByte()
    {
        if (Position >= End) return -1;
        int value = Bytes[Position];
        Position = Position + 1;
        return value;
    }

    internal int ReadUInt16()
    {
        if (Remaining() < 2) return -1;
        int value = Bytes[Position] << 8 | Bytes[Position + 1];
        Position = Position + 2;
        return value;
    }

    internal long ReadUInt32()
    {
        if (Remaining() < 4) return -1;
        uint value = (uint)Bytes[Position] << 24 | (uint)Bytes[Position + 1] << 16 | (uint)Bytes[Position + 2] << 8 | Bytes[Position + 3];
        Position = Position + 4;
        return value;
    }

    internal string? ReadTag()
    {
        if (Remaining() < 4) return null;
        char[] value = new char[4];
        value[0] = (char)Bytes[Position];
        value[1] = (char)Bytes[Position + 1];
        value[2] = (char)Bytes[Position + 2];
        value[3] = (char)Bytes[Position + 3];
        Position = Position + 4;
        return new string(value);
    }
}

internal class SvgWebFontNumber
{
    internal bool Valid;
    internal int Value;

    internal SvgWebFontNumber(bool valid, int value)
    {
        Valid = valid;
        Value = value;
    }
}
