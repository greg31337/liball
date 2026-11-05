using System.IO;

namespace png;

internal class PngContentLightLevelInformationParser
{
    internal const int DataLength = 8;

    internal static PngContentLightLevelInformation Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngContentLightLevelInformation Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "cLLI data must contain exactly 8 bytes.");

        PngContentLightLevelInformation lightLevels = new PngContentLightLevelInformation();
        lightLevels.MaximumContentLightLevel = PngBinaryReader.ReadUInt32(data, offset);
        lightLevels.MaximumFrameAverageLightLevel = PngBinaryReader.ReadUInt32(data, offset + 4);
        return lightLevels;
    }

    internal static PngContentLightLevelInformation Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkContentLightLevelInformation)
        {
            throw new InvalidDataException("Chunk type must be cLLI.");
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

internal class PngContentLightLevelInformationWriter
{
    internal static byte[] WriteData(PngContentLightLevelInformation lightLevels)
    {
        byte[] data = new byte[PngContentLightLevelInformationParser.DataLength];
        WriteData(data, 0, lightLevels);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngContentLightLevelInformation lightLevels)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(lightLevels);

        ValidateWriteRange(data, offset, PngContentLightLevelInformationParser.DataLength);
        PngBinaryWriter.WriteUInt32(data, offset, lightLevels.MaximumContentLightLevel);
        PngBinaryWriter.WriteUInt32(data, offset + 4, lightLevels.MaximumFrameAverageLightLevel);
    }

    internal static PngChunk WriteChunk(PngContentLightLevelInformation lightLevels)
    {
        return CreateChunk(WriteData(lightLevels));
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
        chunk.Type = PngConstants.ChunkContentLightLevelInformation;
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
