using System.IO;

namespace png;

internal class PngGammaParser
{
    internal const int DataLength = 4;

    internal static PngGamma Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngGamma Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "gAMA data must contain exactly 4 bytes.");

        PngGamma gamma = new PngGamma();
        gamma.Value = PngBinaryReader.ReadUInt32(data, offset);
        return gamma;
    }

    internal static PngGamma Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkGamma)
        {
            throw new InvalidDataException("Chunk type must be gAMA.");
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

internal class PngGammaWriter
{
    internal static byte[] WriteData(PngGamma gamma)
    {
        byte[] data = new byte[PngGammaParser.DataLength];
        WriteData(data, 0, gamma);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngGamma gamma)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(gamma);

        ValidateWriteRange(data, offset, PngGammaParser.DataLength);
        PngBinaryWriter.WriteUInt32(data, offset, gamma.Value);
    }

    internal static PngChunk WriteChunk(PngGamma gamma)
    {
        return CreateChunk(WriteData(gamma));
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
        chunk.Type = PngConstants.ChunkGamma;
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
