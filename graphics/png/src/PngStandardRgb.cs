using System.IO;

namespace png;

internal class PngStandardRgbParser
{
    internal const int DataLength = 1;
    internal const byte MinimumRenderingIntent = 0;
    internal const byte MaximumRenderingIntent = 3;

    internal static PngStandardRgb Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngStandardRgb Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "sRGB data must contain exactly 1 byte.");
        ValidateRenderingIntent(data[offset]);

        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = data[offset];
        return standardRgb;
    }

    internal static PngStandardRgb Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkStandardRgb)
        {
            throw new InvalidDataException("Chunk type must be sRGB.");
        }
    }

    internal static void ValidateRenderingIntent(byte renderingIntent)
    {
        if (renderingIntent < MinimumRenderingIntent || renderingIntent > MaximumRenderingIntent)
        {
            throw new InvalidDataException("sRGB rendering intent must be in the range 0 through 3.");
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

internal class PngStandardRgbWriter
{
    internal static byte[] WriteData(PngStandardRgb standardRgb)
    {
        byte[] data = new byte[PngStandardRgbParser.DataLength];
        WriteData(data, 0, standardRgb);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngStandardRgb standardRgb)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(standardRgb);

        ValidateWriteRange(data, offset, PngStandardRgbParser.DataLength);
        ValidateRenderingIntent(standardRgb.RenderingIntent);
        data[offset] = standardRgb.RenderingIntent;
    }

    internal static PngChunk WriteChunk(PngStandardRgb standardRgb)
    {
        return CreateChunk(WriteData(standardRgb));
    }

    internal static void ValidateRenderingIntent(byte renderingIntent)
    {
        if (renderingIntent < PngStandardRgbParser.MinimumRenderingIntent || renderingIntent > PngStandardRgbParser.MaximumRenderingIntent)
        {
            throw new ArgumentOutOfRangeException("RenderingIntent");
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
        chunk.Type = PngConstants.ChunkStandardRgb;
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
