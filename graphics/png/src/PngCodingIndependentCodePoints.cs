using System.IO;

namespace png;

internal class PngCodingIndependentCodePointsParser
{
    internal const int DataLength = 4;
    internal const byte MaximumVideoFullRangeFlag = 1;

    internal static PngCodingIndependentCodePoints Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngCodingIndependentCodePoints Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, DataLength, "cICP data must contain exactly 4 bytes.");
        ValidateVideoFullRangeFlag(data[offset + 3]);

        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = data[offset];
        codePoints.TransferCharacteristics = data[offset + 1];
        codePoints.MatrixCoefficients = data[offset + 2];
        codePoints.VideoFullRangeFlag = data[offset + 3];
        return codePoints;
    }

    internal static PngCodingIndependentCodePoints Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkCodingIndependentCodePoints)
        {
            throw new InvalidDataException("Chunk type must be cICP.");
        }
    }

    internal static void ValidateVideoFullRangeFlag(byte videoFullRangeFlag)
    {
        if (videoFullRangeFlag > MaximumVideoFullRangeFlag)
        {
            throw new InvalidDataException("cICP video full range flag must be 0 or 1.");
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

internal class PngCodingIndependentCodePointsWriter
{
    internal static byte[] WriteData(PngCodingIndependentCodePoints codePoints)
    {
        byte[] data = new byte[PngCodingIndependentCodePointsParser.DataLength];
        WriteData(data, 0, codePoints);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngCodingIndependentCodePoints codePoints)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(codePoints);

        ValidateWriteRange(data, offset, PngCodingIndependentCodePointsParser.DataLength);
        ValidateVideoFullRangeFlag(codePoints.VideoFullRangeFlag);
        data[offset] = codePoints.ColorPrimaries;
        data[offset + 1] = codePoints.TransferCharacteristics;
        data[offset + 2] = codePoints.MatrixCoefficients;
        data[offset + 3] = codePoints.VideoFullRangeFlag;
    }

    internal static PngChunk WriteChunk(PngCodingIndependentCodePoints codePoints)
    {
        return CreateChunk(WriteData(codePoints));
    }

    internal static void ValidateVideoFullRangeFlag(byte videoFullRangeFlag)
    {
        if (videoFullRangeFlag > PngCodingIndependentCodePointsParser.MaximumVideoFullRangeFlag)
        {
            throw new ArgumentOutOfRangeException("VideoFullRangeFlag");
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
        chunk.Type = PngConstants.ChunkCodingIndependentCodePoints;
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
