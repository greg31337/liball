using System.IO;

namespace png;

internal class PngRawChunkReader
{
    internal static PngChunk ReadSpan(ReadOnlySpan<byte> data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (offset > data.Length - 12)
        {
            throw new EndOfStreamException();
        }

        uint length = PngBinaryReader.ReadUInt32Span(data, offset);
        PngChunkType chunkType = PngChunkTypeParser.Parse(data[offset + 4], data[offset + 5], data[offset + 6], data[offset + 7]);
        ValidateChunkType(chunkType);

        long limit = GetChunkDataLimit(chunkType, limits);
        int allocation = PngSizeCalculator.CalculateChunkAllocation(length, limit);
        int dataOffset = offset + 8;
        if (allocation > data.Length - dataOffset - 4)
        {
            throw new EndOfStreamException();
        }

        byte[] chunkData = new byte[allocation];
        int index = 0;
        while (index < allocation)
        {
            chunkData[index] = data[dataOffset + index];
            index += 1;
        }

        uint crc = PngBinaryReader.ReadUInt32Span(data, dataOffset + allocation);
        return CreateChunk(chunkType, chunkData, crc, offset);
    }

    internal static PngChunk Read(byte[] data, int offset)
    {
        return Read(data, offset, new PngSecurityLimits());
    }

    internal static PngChunk Read(byte[] data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (offset > data.Length - 12)
        {
            throw new EndOfStreamException();
        }

        uint length = PngBinaryReader.ReadUInt32(data, offset);
        PngChunkType chunkType = PngChunkTypeParser.Parse(data, offset + 4);
        ValidateChunkType(chunkType);

        long limit = GetChunkDataLimit(chunkType, limits);
        int allocation = PngSizeCalculator.CalculateChunkAllocation(length, limit);
        int dataOffset = offset + 8;
        if (allocation > data.Length - dataOffset - 4)
        {
            throw new EndOfStreamException();
        }

        byte[] chunkData = new byte[allocation];
        int index = 0;
        while (index < allocation)
        {
            chunkData[index] = data[dataOffset + index];
            index += 1;
        }

        uint crc = PngBinaryReader.ReadUInt32(data, dataOffset + allocation);
        return CreateChunk(chunkType, chunkData, crc, offset);
    }

    internal static PngChunk Read(Stream stream)
    {
        return Read(stream, new PngSecurityLimits());
    }

    internal static PngChunk Read(Stream stream, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        long offset = 0;
        if (stream.CanSeek)
        {
            offset = stream.Position;
        }

        uint length = PngBinaryReader.ReadUInt32(stream);
        byte typeByte0 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte1 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte2 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte3 = PngBinaryReader.ReadUInt8(stream);
        PngChunkType chunkType = PngChunkTypeParser.Parse(typeByte0, typeByte1, typeByte2, typeByte3);
        ValidateChunkType(chunkType);

        long limit = GetChunkDataLimit(chunkType, limits);
        int allocation = PngSizeCalculator.CalculateChunkAllocation(length, limit);
        byte[] chunkData = new byte[allocation];
        int totalRead = 0;
        while (totalRead < allocation)
        {
            int read = stream.Read(chunkData, totalRead, allocation - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            totalRead += read;
        }

        uint crc = PngBinaryReader.ReadUInt32(stream);
        return CreateChunk(chunkType, chunkData, crc, offset);
    }

    internal static void ValidateChunkType(PngChunkType chunkType)
    {
        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunkType);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(validationResult.Errors[0].Message);
        }
    }

    internal static long GetChunkDataLimit(PngChunkType chunkType, PngSecurityLimits limits)
    {
        if (chunkType.Name == PngConstants.ChunkIDAT)
        {
            return limits.MaxIdatBytes;
        }

        if (chunkType.Name == PngConstants.ChunkFrameData)
        {
            if (limits.MaxIdatBytes <= 0 || limits.MaxIdatBytes > long.MaxValue - PngFrameDataParser.SequenceNumberLength)
            {
                return 0;
            }

            return limits.MaxIdatBytes + PngFrameDataParser.SequenceNumberLength;
        }

        if (PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return limits.MaxMetadataBytes;
        }

        return 0;
    }

    internal static PngChunk CreateChunk(PngChunkType chunkType, byte[] data, uint crc, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType.Name;
        chunk.Data = data;
        chunk.Crc = crc;
        chunk.Offset = offset;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunkType.Name);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
        return chunk;
    }
}

internal class PngKnownChunkTypes
{
    internal static bool IsKnown(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return chunkType == PngConstants.ChunkIHDR
            || chunkType == PngConstants.ChunkPLTE
            || chunkType == PngConstants.ChunkIDAT
            || chunkType == PngConstants.ChunkIEND
            || chunkType == PngConstants.ChunkTransparency
            || chunkType == PngConstants.ChunkChromaticities
            || chunkType == PngConstants.ChunkGamma
            || chunkType == PngConstants.ChunkIccProfile
            || chunkType == PngConstants.ChunkSignificantBits
            || chunkType == PngConstants.ChunkStandardRgb
            || chunkType == PngConstants.ChunkCodingIndependentCodePoints
            || chunkType == PngConstants.ChunkMasteringDisplayColorVolume
            || chunkType == PngConstants.ChunkContentLightLevelInformation
            || chunkType == PngConstants.ChunkText
            || chunkType == PngConstants.ChunkCompressedText
            || chunkType == PngConstants.ChunkInternationalText
            || chunkType == PngConstants.ChunkBackgroundColor
            || chunkType == PngConstants.ChunkPaletteHistogram
            || chunkType == PngConstants.ChunkPhysicalPixels
            || chunkType == PngConstants.ChunkSuggestedPalette
            || chunkType == PngConstants.ChunkExif
            || chunkType == PngConstants.ChunkTime
            || chunkType == PngConstants.ChunkAnimationControl
            || chunkType == PngConstants.ChunkFrameControl
            || chunkType == PngConstants.ChunkFrameData;
    }
}
