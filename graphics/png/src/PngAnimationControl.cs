using System.IO;

namespace png;

internal class PngAnimationControlParser
{
    internal const int DataLength = 8;

    internal static PngAnimationControl Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngAnimationControl Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);

        uint frameCount = PngBinaryReader.ReadUInt32(data, offset);
        ValidateFrameCount(frameCount);

        PngAnimationControl control = new PngAnimationControl();
        control.FrameCount = frameCount;
        control.PlayCount = PngBinaryReader.ReadUInt32(data, offset + 4);
        return control;
    }

    internal static PngAnimationControl Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkAnimationControl)
        {
            throw new InvalidDataException("Chunk type must be acTL.");
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
        if (length != DataLength)
        {
            throw new InvalidDataException("acTL data must contain exactly 8 bytes.");
        }
    }

    internal static void ValidateFrameCount(uint frameCount)
    {
        if (frameCount == 0)
        {
            throw new InvalidDataException("acTL frame count must not be 0.");
        }
    }
}

internal class PngAnimationControlWriter
{
    internal static byte[] WriteData(PngAnimationControl control)
    {
        byte[] data = new byte[PngAnimationControlParser.DataLength];
        WriteData(data, 0, control);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngAnimationControl control)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(control);

        ValidateWriteRange(data, offset, PngAnimationControlParser.DataLength);
        ValidateFrameCount(control.FrameCount);
        PngBinaryWriter.WriteUInt32(data, offset, control.FrameCount);
        PngBinaryWriter.WriteUInt32(data, offset + 4, control.PlayCount);
    }

    internal static PngChunk WriteChunk(PngAnimationControl control)
    {
        return CreateChunk(WriteData(control));
    }

    internal static void ValidateFrameCount(uint frameCount)
    {
        if (frameCount == 0)
        {
            throw new ArgumentOutOfRangeException("FrameCount");
        }
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
        chunk.Type = PngConstants.ChunkAnimationControl;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = false;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}
