using System.IO;

namespace png;

internal class PngPhysicalPixelsParser
{
    internal const int DataLength = 9;
    internal const byte UnitUnknown = 0;
    internal const byte UnitMetre = 1;

    internal static PngPhysicalPixels Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngPhysicalPixels Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);
        ValidateUnitSpecifier(data[offset + 8]);

        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = PngBinaryReader.ReadUInt32(data, offset);
        physicalPixels.PixelsPerUnitY = PngBinaryReader.ReadUInt32(data, offset + 4);
        physicalPixels.UnitSpecifier = data[offset + 8];
        return physicalPixels;
    }

    internal static PngPhysicalPixels Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkPhysicalPixels)
        {
            throw new InvalidDataException("Chunk type must be pHYs.");
        }
    }

    internal static void ValidateUnitSpecifier(byte unitSpecifier)
    {
        if (unitSpecifier > UnitMetre)
        {
            throw new InvalidDataException("pHYs unit specifier must be 0 or 1.");
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
            throw new InvalidDataException("pHYs data must contain exactly 9 bytes.");
        }
    }
}

internal class PngPhysicalPixelsWriter
{
    internal static byte[] WriteData(PngPhysicalPixels physicalPixels)
    {
        byte[] data = new byte[PngPhysicalPixelsParser.DataLength];
        WriteData(data, 0, physicalPixels);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngPhysicalPixels physicalPixels)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(physicalPixels);

        ValidateWriteRange(data, offset, PngPhysicalPixelsParser.DataLength);
        ValidateUnitSpecifier(physicalPixels.UnitSpecifier);
        PngBinaryWriter.WriteUInt32(data, offset, physicalPixels.PixelsPerUnitX);
        PngBinaryWriter.WriteUInt32(data, offset + 4, physicalPixels.PixelsPerUnitY);
        data[offset + 8] = physicalPixels.UnitSpecifier;
    }

    internal static PngChunk WriteChunk(PngPhysicalPixels physicalPixels)
    {
        return CreateChunk(WriteData(physicalPixels));
    }

    internal static void ValidateUnitSpecifier(byte unitSpecifier)
    {
        if (unitSpecifier > PngPhysicalPixelsParser.UnitMetre)
        {
            throw new ArgumentOutOfRangeException("UnitSpecifier");
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
        chunk.Type = PngConstants.ChunkPhysicalPixels;
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
