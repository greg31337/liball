namespace jpeg;

internal class JpegByteReader
{
    internal byte[] Data = new byte[0];
    internal int Offset;
    internal int Length;
    internal System.IO.Stream? InputStream;
    internal bool UsesStream;
    internal long AbsoluteOffset;
    internal byte[] LookaheadData = new byte[0];
    internal int LookaheadOffset;

    internal JpegByteReader(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        Data = data;
        Offset = 0;
        Length = data.Length;
        InputStream = null;
        UsesStream = false;
        AbsoluteOffset = 0;
        LookaheadData = new byte[0];
        LookaheadOffset = 0;
    }

    internal static JpegByteReader FromStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new System.ArgumentException("Stream must be readable.", "stream");
        }

        JpegByteReader reader = new JpegByteReader(new byte[0]);
        reader.InputStream = stream;
        reader.UsesStream = true;
        return reader;
    }

    internal byte[] PeekBytes(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (!UsesStream)
        {
            int available = Length - Offset;
            if (available > count)
            {
                available = count;
            }

            byte[] result = new byte[available];
            System.Array.Copy(Data, Offset, result, 0, available);
            return result;
        }

        int buffered = LookaheadData.Length - LookaheadOffset;
        if (buffered >= count)
        {
            byte[] existing = new byte[count];
            System.Array.Copy(LookaheadData, LookaheadOffset, existing, 0, count);
            return existing;
        }

        System.IO.Stream? stream = InputStream;
        if (stream == null)
        {
            throw new System.InvalidOperationException("Reader does not have a stream.");
        }

        byte[] candidate = new byte[count];
        if (buffered > 0)
        {
            System.Array.Copy(LookaheadData, LookaheadOffset, candidate, 0, buffered);
        }

        int total = buffered;
        while (total < count)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                break;
            }

            candidate[total] = (byte)value;
            total = total + 1;
        }

        byte[] lookahead = new byte[total];
        System.Array.Copy(candidate, 0, lookahead, 0, total);
        LookaheadData = lookahead;
        LookaheadOffset = 0;

        byte[] resultBytes = new byte[total];
        System.Array.Copy(lookahead, 0, resultBytes, 0, total);
        return resultBytes;
    }

    internal static int ReadUInt16(byte[] data, int offset)
    {
        ValidateByteArrayRange(data, offset, 2);
        return (data[offset] << 8) | data[offset + 1];
    }

    internal static int ReadInt16(byte[] data, int offset)
    {
        int value = ReadUInt16(data, offset);
        if (value >= 32768)
        {
            return value - 65536;
        }

        return value;
    }

    internal static long ReadUInt32(byte[] data, int offset)
    {
        ValidateByteArrayRange(data, offset, 4);
        return ((long)data[offset] << 24) | ((long)data[offset + 1] << 16) | ((long)data[offset + 2] << 8) | (long)data[offset + 3];
    }

    internal int ReadByte()
    {
        if (UsesStream)
        {
            return ReadStreamByte();
        }

        if (Offset >= Length)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }

        int value = Data[Offset];
        Offset = Offset + 1;
        AbsoluteOffset = AbsoluteOffset + 1;
        return value;
    }

    internal int ReadUInt16()
    {
        int high = ReadByte();
        int low = ReadByte();
        return (high << 8) | low;
    }

    internal int ReadInt16()
    {
        int value = ReadUInt16();
        if (value >= 32768)
        {
            return value - 65536;
        }

        return value;
    }

    internal long ReadUInt32()
    {
        int first = ReadByte();
        int second = ReadByte();
        int third = ReadByte();
        int fourth = ReadByte();
        return ((long)first << 24) | ((long)second << 16) | ((long)third << 8) | (long)fourth;
    }

    internal byte[] ReadBytes(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        byte[] result = new byte[count];
        if (UsesStream)
        {
            ReadStreamBytes(result, count);
            AbsoluteOffset = AbsoluteOffset + count;
            return result;
        }

        if (count > Length - Offset)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }

        System.Array.Copy(Data, Offset, result, 0, count);
        Offset = Offset + count;
        AbsoluteOffset = AbsoluteOffset + count;
        return result;
    }

    internal void Skip(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (UsesStream)
        {
            byte[] buffer = new byte[256];
            int remaining = count;
            while (remaining > 0)
            {
                int requested = remaining;
                if (requested > buffer.Length)
                {
                    requested = buffer.Length;
                }

                byte[] chunk = ReadBytes(requested);
                if (chunk.Length == 0 && requested > 0)
                {
                    throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
                }

                remaining = remaining - requested;
            }

            return;
        }

        if (count > Length - Offset)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }

        Offset = Offset + count;
        AbsoluteOffset = AbsoluteOffset + count;
    }

    internal int Remaining()
    {
        if (UsesStream)
        {
            return -1;
        }

        return Length - Offset;
    }

    internal static void ValidateByteArrayRange(byte[] data, int offset, int count)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (offset > data.Length || count > data.Length - offset)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }
    }

    internal int ReadStreamByte()
    {
        if (LookaheadOffset < LookaheadData.Length)
        {
            int bufferedValue = LookaheadData[LookaheadOffset];
            LookaheadOffset = LookaheadOffset + 1;
            AbsoluteOffset = AbsoluteOffset + 1;
            return bufferedValue;
        }

        System.IO.Stream? stream = InputStream;
        if (stream == null)
        {
            throw new System.InvalidOperationException("Reader does not have a stream.");
        }

        int value = stream.ReadByte();
        if (value < 0)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }

        AbsoluteOffset = AbsoluteOffset + 1;
        return value;
    }

    internal void ReadStreamBytes(byte[] result, int count)
    {
        System.IO.Stream? stream = InputStream;
        if (stream == null)
        {
            throw new System.InvalidOperationException("Reader does not have a stream.");
        }

        int total = 0;
        int buffered = LookaheadData.Length - LookaheadOffset;
        if (buffered > count)
        {
            buffered = count;
        }

        if (buffered > 0)
        {
            System.Array.Copy(LookaheadData, LookaheadOffset, result, 0, buffered);
            LookaheadOffset = LookaheadOffset + buffered;
            total = buffered;
        }

        while (total < count)
        {
            int read = stream.Read(result, total, count - total);
            if (read == 0)
            {
                throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
            }

            total = total + read;
        }
    }
}
