using System.IO;

namespace png;

internal class PngFrameDataParser
{
    internal const int SequenceNumberLength = 4;

    internal static PngFrameData Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngFrameData Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);

        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = PngBinaryReader.ReadUInt32(data, offset);
        frameData.Data = CopyRange(data, offset + SequenceNumberLength, length - SequenceNumberLength);
        return frameData;
    }

    internal static PngFrameData Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkFrameData)
        {
            throw new InvalidDataException("Chunk type must be fdAT.");
        }
    }

    internal static void ValidateRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }
    }

    internal static void ValidateLength(int length)
    {
        if (length < SequenceNumberLength)
        {
            throw new InvalidDataException("fdAT data must contain at least a sequence number.");
        }
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        CopyBytes(data, offset, copy, 0, length);
        return copy;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}

internal class PngFrameDataWriter
{
    internal static byte[] WriteData(PngFrameData frameData)
    {
        ArgumentNullException.ThrowIfNull(frameData);
        ArgumentNullException.ThrowIfNull(frameData.Data);

        byte[] data = new byte[PngFrameDataParser.SequenceNumberLength + frameData.Data.Length];
        WriteData(data, 0, frameData);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngFrameData frameData)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(frameData);
        ArgumentNullException.ThrowIfNull(frameData.Data);

        int length = PngFrameDataParser.SequenceNumberLength + frameData.Data.Length;
        ValidateWriteRange(data, offset, length);
        PngBinaryWriter.WriteUInt32(data, offset, frameData.SequenceNumber);
        CopyBytes(frameData.Data, 0, data, offset + PngFrameDataParser.SequenceNumberLength, frameData.Data.Length);
    }

    internal static PngChunk WriteChunk(PngFrameData frameData)
    {
        return CreateChunk(WriteData(frameData));
    }

    internal static void ValidateWriteRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal static PngChunk CreateChunk(byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkFrameData;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = false;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
