namespace jpeg;

internal class JpegByteWriter
{
    internal byte[] Buffer = new byte[256];
    internal int Length;
    internal System.IO.Stream? OutputStream;
    internal bool UsesStream;
    internal long AbsoluteOffset;

    internal JpegByteWriter()
    {
        Buffer = new byte[256];
        Length = 0;
        OutputStream = null;
        UsesStream = false;
        AbsoluteOffset = 0;
    }

    internal static JpegByteWriter WithCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("capacity");
        }

        if (capacity == 0)
        {
            capacity = 1;
        }

        JpegByteWriter writer = new JpegByteWriter();
        writer.Buffer = new byte[capacity];
        return writer;
    }

    internal static JpegByteWriter FromStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.", "stream");
        }

        JpegByteWriter writer = new JpegByteWriter();
        writer.Buffer = new byte[0];
        writer.OutputStream = stream;
        writer.UsesStream = true;
        return writer;
    }

    internal static void WriteByte(byte[] data, int offset, int value)
    {
        ValidateByteValue(value);
        JpegByteReader.ValidateByteArrayRange(data, offset, 1);
        data[offset] = (byte)value;
    }

    internal static void WriteUInt16(byte[] data, int offset, int value)
    {
        ValidateUInt16Value(value);
        JpegByteReader.ValidateByteArrayRange(data, offset, 2);
        data[offset] = (byte)((value >> 8) & 255);
        data[offset + 1] = (byte)(value & 255);
    }

    internal static void WriteInt16(byte[] data, int offset, int value)
    {
        ValidateInt16Value(value);
        if (value < 0)
        {
            value = value + 65536;
        }

        WriteUInt16(data, offset, value);
    }

    internal static void WriteUInt32(byte[] data, int offset, long value)
    {
        ValidateUInt32Value(value);
        JpegByteReader.ValidateByteArrayRange(data, offset, 4);
        data[offset] = (byte)((value >> 24) & 255L);
        data[offset + 1] = (byte)((value >> 16) & 255L);
        data[offset + 2] = (byte)((value >> 8) & 255L);
        data[offset + 3] = (byte)(value & 255L);
    }

    internal void WriteByte(int value)
    {
        ValidateByteValue(value);
        if (UsesStream)
        {
            System.IO.Stream? stream = OutputStream;
            if (stream == null)
            {
                throw new System.InvalidOperationException("Writer does not have a stream.");
            }

            stream.WriteByte((byte)value);
            AbsoluteOffset = AbsoluteOffset + 1;
            return;
        }

        EnsureCapacity(1);
        Buffer[Length] = (byte)value;
        Length = Length + 1;
        AbsoluteOffset = AbsoluteOffset + 1;
    }

    internal void WriteUInt16(int value)
    {
        ValidateUInt16Value(value);
        WriteByte((value >> 8) & 255);
        WriteByte(value & 255);
    }

    internal void WriteInt16(int value)
    {
        ValidateInt16Value(value);
        if (value < 0)
        {
            value = value + 65536;
        }

        WriteUInt16(value);
    }

    internal void WriteUInt32(long value)
    {
        ValidateUInt32Value(value);
        WriteByte((int)((value >> 24) & 255L));
        WriteByte((int)((value >> 16) & 255L));
        WriteByte((int)((value >> 8) & 255L));
        WriteByte((int)(value & 255L));
    }

    internal void WriteBytes(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        WriteBytesRange(data, 0, data.Length);
    }

    internal void WriteBytesRange(byte[] data, int offset, int count)
    {
        JpegByteReader.ValidateByteArrayRange(data, offset, count);
        if (UsesStream)
        {
            System.IO.Stream? stream = OutputStream;
            if (stream == null)
            {
                throw new System.InvalidOperationException("Writer does not have a stream.");
            }

            stream.Write(data, offset, count);
            AbsoluteOffset = AbsoluteOffset + count;
            return;
        }

        EnsureCapacity(count);
        System.Array.Copy(data, offset, Buffer, Length, count);
        Length = Length + count;
        AbsoluteOffset = AbsoluteOffset + count;
    }

    internal byte[] ToArray()
    {
        if (UsesStream)
        {
            throw new System.InvalidOperationException("Stream-backed writers do not own an expandable byte array.");
        }

        byte[] result = new byte[Length];
        System.Array.Copy(Buffer, 0, result, 0, Length);
        return result;
    }

    internal void EnsureCapacity(int additionalBytes)
    {
        if (additionalBytes < 0)
        {
            throw new System.ArgumentOutOfRangeException("additionalBytes");
        }

        if (additionalBytes > int.MaxValue - Length)
        {
            throw JpegResourceLimitException.Create("Expandable byte writer exceeded maximum buffer length.");
        }

        int required = Length + additionalBytes;
        if (required <= Buffer.Length)
        {
            return;
        }

        int capacity = Buffer.Length;
        if (capacity == 0)
        {
            capacity = 1;
        }

        while (capacity < required)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = required;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        byte[] replacement = new byte[capacity];
        System.Array.Copy(Buffer, 0, replacement, 0, Length);
        Buffer = replacement;
    }

    internal static void ValidateByteValue(int value)
    {
        if (value < 0 || value > 255)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }

    internal static void ValidateUInt16Value(int value)
    {
        if (value < 0 || value > 65535)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }

    internal static void ValidateInt16Value(int value)
    {
        if (value < -32768 || value > 32767)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }

    internal static void ValidateUInt32Value(long value)
    {
        if (value < 0 || value > 4294967295L)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
