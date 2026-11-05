using System.IO;

namespace png;

internal class PngChromaticitiesParser
{
    internal const int DataLength = 32;

    internal static PngChromaticities Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngChromaticities Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "cHRM data must contain exactly 32 bytes.");

        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = PngBinaryReader.ReadUInt32(data, offset);
        chromaticities.WhitePointY = PngBinaryReader.ReadUInt32(data, offset + 4);
        chromaticities.RedX = PngBinaryReader.ReadUInt32(data, offset + 8);
        chromaticities.RedY = PngBinaryReader.ReadUInt32(data, offset + 12);
        chromaticities.GreenX = PngBinaryReader.ReadUInt32(data, offset + 16);
        chromaticities.GreenY = PngBinaryReader.ReadUInt32(data, offset + 20);
        chromaticities.BlueX = PngBinaryReader.ReadUInt32(data, offset + 24);
        chromaticities.BlueY = PngBinaryReader.ReadUInt32(data, offset + 28);
        return chromaticities;
    }

    internal static PngChromaticities Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkChromaticities)
        {
            throw new InvalidDataException("Chunk type must be cHRM.");
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

    internal static void ValidateLength(int length, int requiredLength, string message)
    {
        if (length != requiredLength)
        {
            throw new InvalidDataException(message);
        }
    }
}

internal class PngChromaticitiesWriter
{
    internal static byte[] WriteData(PngChromaticities chromaticities)
    {
        byte[] data = new byte[PngChromaticitiesParser.DataLength];
        WriteData(data, 0, chromaticities);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngChromaticities chromaticities)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(chromaticities);

        ValidateWriteRange(data, offset, PngChromaticitiesParser.DataLength);
        PngBinaryWriter.WriteUInt32(data, offset, chromaticities.WhitePointX);
        PngBinaryWriter.WriteUInt32(data, offset + 4, chromaticities.WhitePointY);
        PngBinaryWriter.WriteUInt32(data, offset + 8, chromaticities.RedX);
        PngBinaryWriter.WriteUInt32(data, offset + 12, chromaticities.RedY);
        PngBinaryWriter.WriteUInt32(data, offset + 16, chromaticities.GreenX);
        PngBinaryWriter.WriteUInt32(data, offset + 20, chromaticities.GreenY);
        PngBinaryWriter.WriteUInt32(data, offset + 24, chromaticities.BlueX);
        PngBinaryWriter.WriteUInt32(data, offset + 28, chromaticities.BlueY);
    }

    internal static PngChunk WriteChunk(PngChromaticities chromaticities)
    {
        return CreateChunk(WriteData(chromaticities));
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
        chunk.Type = PngConstants.ChunkChromaticities;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}
