using System.IO;

namespace png;

internal class PngSignificantBitsParser
{
    internal const int GrayscaleDataLength = 1;
    internal const int GrayscaleAlphaDataLength = 2;
    internal const int TruecolorDataLength = 3;
    internal const int TruecolorAlphaDataLength = 4;

    internal static PngSignificantBits ParseGrayscale(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseGrayscale(data, 0, data.Length);
    }

    internal static PngSignificantBits ParseGrayscale(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, GrayscaleDataLength, "Grayscale sBIT data must contain exactly 1 byte.");

        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = data[offset];
        return bits;
    }

    internal static PngSignificantBits ParseGrayscale(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseGrayscale(chunk.Data);
    }

    internal static PngSignificantBits ParseGrayscaleAlpha(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseGrayscaleAlpha(data, 0, data.Length);
    }

    internal static PngSignificantBits ParseGrayscaleAlpha(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, GrayscaleAlphaDataLength, "Grayscale-alpha sBIT data must contain exactly 2 bytes.");

        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = data[offset];
        bits.Alpha = data[offset + 1];
        return bits;
    }

    internal static PngSignificantBits ParseGrayscaleAlpha(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseGrayscaleAlpha(chunk.Data);
    }

    internal static PngSignificantBits ParseIndexed(byte[] data)
    {
        return ParseTruecolor(data);
    }

    internal static PngSignificantBits ParseIndexed(byte[] data, int offset, int length)
    {
        return ParseTruecolor(data, offset, length);
    }

    internal static PngSignificantBits ParseIndexed(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseIndexed(chunk.Data);
    }

    internal static PngSignificantBits ParseTruecolor(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseTruecolor(data, 0, data.Length);
    }

    internal static PngSignificantBits ParseTruecolor(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, TruecolorDataLength, "Truecolor sBIT data must contain exactly 3 bytes.");

        PngSignificantBits bits = new PngSignificantBits();
        bits.Red = data[offset];
        bits.Green = data[offset + 1];
        bits.Blue = data[offset + 2];
        return bits;
    }

    internal static PngSignificantBits ParseTruecolor(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseTruecolor(chunk.Data);
    }

    internal static PngSignificantBits ParseTruecolorAlpha(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseTruecolorAlpha(data, 0, data.Length);
    }

    internal static PngSignificantBits ParseTruecolorAlpha(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, TruecolorAlphaDataLength, "Truecolor-alpha sBIT data must contain exactly 4 bytes.");

        PngSignificantBits bits = new PngSignificantBits();
        bits.Red = data[offset];
        bits.Green = data[offset + 1];
        bits.Blue = data[offset + 2];
        bits.Alpha = data[offset + 3];
        return bits;
    }

    internal static PngSignificantBits ParseTruecolorAlpha(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseTruecolorAlpha(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkSignificantBits)
        {
            throw new InvalidDataException("Chunk type must be sBIT.");
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

internal class PngSignificantBitsWriter
{
    internal static byte[] WriteGrayscaleData(PngSignificantBits bits)
    {
        byte[] data = new byte[PngSignificantBitsParser.GrayscaleDataLength];
        WriteGrayscaleData(data, 0, bits);
        return data;
    }

    internal static void WriteGrayscaleData(byte[] data, int offset, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(bits);

        ValidateWriteRange(data, offset, PngSignificantBitsParser.GrayscaleDataLength);
        data[offset] = bits.Gray;
    }

    internal static PngChunk WriteGrayscaleChunk(PngSignificantBits bits)
    {
        return CreateChunk(WriteGrayscaleData(bits));
    }

    internal static byte[] WriteGrayscaleAlphaData(PngSignificantBits bits)
    {
        byte[] data = new byte[PngSignificantBitsParser.GrayscaleAlphaDataLength];
        WriteGrayscaleAlphaData(data, 0, bits);
        return data;
    }

    internal static void WriteGrayscaleAlphaData(byte[] data, int offset, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(bits);

        ValidateWriteRange(data, offset, PngSignificantBitsParser.GrayscaleAlphaDataLength);
        data[offset] = bits.Gray;
        data[offset + 1] = bits.Alpha;
    }

    internal static PngChunk WriteGrayscaleAlphaChunk(PngSignificantBits bits)
    {
        return CreateChunk(WriteGrayscaleAlphaData(bits));
    }

    internal static byte[] WriteIndexedData(PngSignificantBits bits)
    {
        return WriteTruecolorData(bits);
    }

    internal static void WriteIndexedData(byte[] data, int offset, PngSignificantBits bits)
    {
        WriteTruecolorData(data, offset, bits);
    }

    internal static PngChunk WriteIndexedChunk(PngSignificantBits bits)
    {
        return CreateChunk(WriteIndexedData(bits));
    }

    internal static byte[] WriteTruecolorData(PngSignificantBits bits)
    {
        byte[] data = new byte[PngSignificantBitsParser.TruecolorDataLength];
        WriteTruecolorData(data, 0, bits);
        return data;
    }

    internal static void WriteTruecolorData(byte[] data, int offset, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(bits);

        ValidateWriteRange(data, offset, PngSignificantBitsParser.TruecolorDataLength);
        data[offset] = bits.Red;
        data[offset + 1] = bits.Green;
        data[offset + 2] = bits.Blue;
    }

    internal static PngChunk WriteTruecolorChunk(PngSignificantBits bits)
    {
        return CreateChunk(WriteTruecolorData(bits));
    }

    internal static byte[] WriteTruecolorAlphaData(PngSignificantBits bits)
    {
        byte[] data = new byte[PngSignificantBitsParser.TruecolorAlphaDataLength];
        WriteTruecolorAlphaData(data, 0, bits);
        return data;
    }

    internal static void WriteTruecolorAlphaData(byte[] data, int offset, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(bits);

        ValidateWriteRange(data, offset, PngSignificantBitsParser.TruecolorAlphaDataLength);
        data[offset] = bits.Red;
        data[offset + 1] = bits.Green;
        data[offset + 2] = bits.Blue;
        data[offset + 3] = bits.Alpha;
    }

    internal static PngChunk WriteTruecolorAlphaChunk(PngSignificantBits bits)
    {
        return CreateChunk(WriteTruecolorAlphaData(bits));
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
        chunk.Type = PngConstants.ChunkSignificantBits;
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
