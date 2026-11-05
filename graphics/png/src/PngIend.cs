using System.IO;

namespace png;

internal class PngIendParser
{
    internal const int DataLength = 0;

    internal static PngIend Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != DataLength)
        {
            throw new InvalidDataException("IEND data must be empty.");
        }

        return CreateIend();
    }

    internal static PngIend Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length != DataLength)
        {
            throw new InvalidDataException("IEND data must be empty.");
        }

        return CreateIend();
    }

    internal static PngIend Parse(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkIEND)
        {
            throw new InvalidDataException("Chunk type must be IEND.");
        }

        return Parse(chunk.Data);
    }

    internal static PngIend CreateIend()
    {
        PngIend iend = new PngIend();
        iend.IsEnd = true;
        return iend;
    }
}

internal class PngIendWriter
{
    internal static byte[] WriteData()
    {
        return new byte[0];
    }

    internal static void WriteData(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal static PngChunk WriteChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIEND;
        chunk.Data = WriteData();
        chunk.Length = DataLength();
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static uint DataLength()
    {
        return 0;
    }
}
