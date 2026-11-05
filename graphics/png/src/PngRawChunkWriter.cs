using System.IO;

namespace png;

internal class PngRawChunkWriter
{
    internal static int WriteSpan(Span<byte> destination, PngChunk chunk, PngSecurityLimits limits)
    {
        PngChunkType chunkType = ValidateChunk(chunk, limits);
        if (chunk.Data.Length > int.MaxValue - 12)
        {
            throw new OverflowException();
        }

        int requiredLength = chunk.Data.Length + 12;
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException("Destination span is too small for the framed PNG chunk.");
        }

        uint crc = PngCrc32.CalculateChunkCrcSpan(chunk.Type, chunk.Data);
        PngBinaryWriter.WriteUInt32Span(destination, 0, (uint)chunk.Data.Length);
        destination[4] = (byte)chunk.Type[0];
        destination[5] = (byte)chunk.Type[1];
        destination[6] = (byte)chunk.Type[2];
        destination[7] = (byte)chunk.Type[3];
        int index = 0;
        while (index < chunk.Data.Length)
        {
            destination[8 + index] = chunk.Data[index];
            index += 1;
        }

        PngBinaryWriter.WriteUInt32Span(destination, 8 + chunk.Data.Length, crc);
        UpdateChunkFields(chunk, chunkType, crc, 0);
        return requiredLength;
    }

    internal static byte[] WriteToBytes(PngChunk chunk)
    {
        return WriteToBytes(chunk, new PngSecurityLimits());
    }

    internal static byte[] WriteToBytes(PngChunk chunk, PngSecurityLimits limits)
    {
        PngChunkType chunkType = ValidateChunk(chunk, limits);

        if (chunk.Data.Length > int.MaxValue - 12)
        {
            throw new OverflowException();
        }

        byte[] bytes = new byte[chunk.Data.Length + 12];
        uint crc = PngCrc32.CalculateChunkCrc(chunk.Type, chunk.Data);
        PngBinaryWriter.WriteUInt32(bytes, 0, (uint)chunk.Data.Length);
        WriteTypeBytes(bytes, 4, chunk.Type);

        int index = 0;
        while (index < chunk.Data.Length)
        {
            bytes[8 + index] = chunk.Data[index];
            index += 1;
        }

        PngBinaryWriter.WriteUInt32(bytes, 8 + chunk.Data.Length, crc);
        UpdateChunkFields(chunk, chunkType, crc, 0);
        return bytes;
    }

    internal static void Write(Stream stream, PngChunk chunk)
    {
        Write(stream, chunk, new PngSecurityLimits());
    }

    internal static void Write(Stream stream, PngChunk chunk, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);

        PngChunkType chunkType = ValidateChunk(chunk, limits);
        long offset = 0;
        if (stream.CanSeek)
        {
            offset = stream.Position;
        }

        uint crc = PngCrc32.CalculateChunkCrc(chunk.Type, chunk.Data);
        PngBinaryWriter.WriteUInt32(stream, (uint)chunk.Data.Length);
        WriteTypeBytes(stream, chunk.Type);
        stream.Write(chunk.Data, 0, chunk.Data.Length);
        PngBinaryWriter.WriteUInt32(stream, crc);
        UpdateChunkFields(chunk, chunkType, crc, offset);
    }

    internal static PngChunkType ValidateChunk(PngChunk chunk, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunk.Type);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(validationResult.Errors[0].Message);
        }

        PngChunkType chunkType = PngChunkTypeParser.Parse(
            (byte)chunk.Type[0],
            (byte)chunk.Type[1],
            (byte)chunk.Type[2],
            (byte)chunk.Type[3]);
        long limit = GetChunkDataLimit(chunkType, limits);
        PngSizeCalculator.CalculateChunkAllocation((uint)chunk.Data.Length, limit);
        return chunkType;
    }

    internal static long GetChunkDataLimit(PngChunkType chunkType, PngSecurityLimits limits)
    {
        if (chunkType.Name == PngConstants.ChunkIDAT)
        {
            return limits.MaxIdatBytes;
        }

        if (PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return limits.MaxMetadataBytes;
        }

        return 0;
    }

    internal static void WriteTypeBytes(byte[] bytes, int offset, string chunkType)
    {
        bytes[offset] = (byte)chunkType[0];
        bytes[offset + 1] = (byte)chunkType[1];
        bytes[offset + 2] = (byte)chunkType[2];
        bytes[offset + 3] = (byte)chunkType[3];
    }

    internal static void WriteTypeBytes(Stream stream, string chunkType)
    {
        stream.WriteByte((byte)chunkType[0]);
        stream.WriteByte((byte)chunkType[1]);
        stream.WriteByte((byte)chunkType[2]);
        stream.WriteByte((byte)chunkType[3]);
    }

    internal static void UpdateChunkFields(PngChunk chunk, PngChunkType chunkType, uint crc, long offset)
    {
        chunk.Length = (uint)chunk.Data.Length;
        chunk.Crc = crc;
        chunk.Offset = offset;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunkType.Name);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
    }
}
