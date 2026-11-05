namespace image;

internal class BmffByteReader
{
    internal byte[] Data;
    internal int Position;
    internal bool Valid;

    internal BmffByteReader(byte[]? data)
    {
        Data = data == null ? new byte[0] : data;
        Position = 0;
        Valid = true;
    }

    internal int Remaining()
    {
        return Data.Length - Position;
    }

    internal bool CanRead(int count)
    {
        return count >= 0 && count <= Remaining();
    }

    internal int ReadByte()
    {
        if (!CanRead(1))
        {
            Valid = false;
            return 0;
        }

        int value = Data[Position];
        Position = Position + 1;
        return value;
    }

    internal uint ReadUnsigned16()
    {
        return (uint)ReadUnsigned(2);
    }

    internal uint ReadUnsigned32()
    {
        return (uint)ReadUnsigned(4);
    }

    internal int ReadSigned32()
    {
        return IntegerBitPattern.ToSigned32(ReadUnsigned32());
    }

    internal ulong ReadUnsigned(int count)
    {
        if (count == 0)
        {
            return 0;
        }

        if (count < 0 || count > 8 || !CanRead(count))
        {
            Valid = false;
            return 0;
        }

        ulong value = 0;
        int index = 0;
        while (index < count)
        {
            value = (value << 8) | Data[Position];
            Position = Position + 1;
            index = index + 1;
        }

        return value;
    }

    internal string ReadFourCc()
    {
        return ReadLatin1(4);
    }

    internal string ReadLatin1(int count)
    {
        if (!CanRead(count))
        {
            Valid = false;
            return "";
        }

        string value = MetadataStringReader.DecodeLatin1Range(Data, Position, count, null, "");
        Position = Position + count;
        return value;
    }

    internal string ReadNullTerminatedLatin1()
    {
        int start = Position;
        while (Position < Data.Length && Data[Position] != 0)
        {
            Position = Position + 1;
        }

        string value = MetadataStringReader.DecodeLatin1Range(Data, start, Position - start, null, "");
        if (Position >= Data.Length)
        {
            Valid = false;
            return value;
        }

        Position = Position + 1;
        return value;
    }

    internal byte[] ReadRemainingBytes()
    {
        int count = Remaining();
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < count)
        {
            bytes[index] = Data[Position + index];
            index = index + 1;
        }

        Position = Data.Length;
        return bytes;
    }
}
