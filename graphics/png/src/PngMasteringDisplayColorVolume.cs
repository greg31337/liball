using System.IO;

namespace png;

internal class PngMasteringDisplayColorVolumeParser
{
    internal const int DataLength = 24;

    internal static PngMasteringDisplayColorVolume Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngMasteringDisplayColorVolume Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "mDCV data must contain exactly 24 bytes.");

        PngMasteringDisplayColorVolume volume = new PngMasteringDisplayColorVolume();
        volume.RedX = PngBinaryReader.ReadUInt16(data, offset);
        volume.RedY = PngBinaryReader.ReadUInt16(data, offset + 2);
        volume.GreenX = PngBinaryReader.ReadUInt16(data, offset + 4);
        volume.GreenY = PngBinaryReader.ReadUInt16(data, offset + 6);
        volume.BlueX = PngBinaryReader.ReadUInt16(data, offset + 8);
        volume.BlueY = PngBinaryReader.ReadUInt16(data, offset + 10);
        volume.WhitePointX = PngBinaryReader.ReadUInt16(data, offset + 12);
        volume.WhitePointY = PngBinaryReader.ReadUInt16(data, offset + 14);
        volume.MaximumLuminance = PngBinaryReader.ReadUInt32(data, offset + 16);
        volume.MinimumLuminance = PngBinaryReader.ReadUInt32(data, offset + 20);
        return volume;
    }

    internal static PngMasteringDisplayColorVolume Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkMasteringDisplayColorVolume)
        {
            throw new InvalidDataException("Chunk type must be mDCV.");
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

internal class PngMasteringDisplayColorVolumeWriter
{
    internal static byte[] WriteData(PngMasteringDisplayColorVolume volume)
    {
        byte[] data = new byte[PngMasteringDisplayColorVolumeParser.DataLength];
        WriteData(data, 0, volume);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngMasteringDisplayColorVolume volume)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(volume);

        ValidateWriteRange(data, offset, PngMasteringDisplayColorVolumeParser.DataLength);
        PngBinaryWriter.WriteUInt16(data, offset, volume.RedX);
        PngBinaryWriter.WriteUInt16(data, offset + 2, volume.RedY);
        PngBinaryWriter.WriteUInt16(data, offset + 4, volume.GreenX);
        PngBinaryWriter.WriteUInt16(data, offset + 6, volume.GreenY);
        PngBinaryWriter.WriteUInt16(data, offset + 8, volume.BlueX);
        PngBinaryWriter.WriteUInt16(data, offset + 10, volume.BlueY);
        PngBinaryWriter.WriteUInt16(data, offset + 12, volume.WhitePointX);
        PngBinaryWriter.WriteUInt16(data, offset + 14, volume.WhitePointY);
        PngBinaryWriter.WriteUInt32(data, offset + 16, volume.MaximumLuminance);
        PngBinaryWriter.WriteUInt32(data, offset + 20, volume.MinimumLuminance);
    }

    internal static PngChunk WriteChunk(PngMasteringDisplayColorVolume volume)
    {
        return CreateChunk(WriteData(volume));
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
        chunk.Type = PngConstants.ChunkMasteringDisplayColorVolume;
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
