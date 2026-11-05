using System.IO;

namespace png;

internal class PngBackgroundColorParser
{
    internal const int IndexedDataLength = 1;
    internal const int GrayscaleDataLength = 2;
    internal const int TruecolorDataLength = 6;

    internal static PngBackgroundColor ParseIndexed(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseIndexed(data, 0, data.Length);
    }

    internal static PngBackgroundColor ParseIndexed(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, IndexedDataLength, "Indexed bKGD data must contain exactly 1 byte.");

        PngBackgroundColor background = new PngBackgroundColor();
        background.PaletteIndex = data[offset];
        return background;
    }

    internal static PngBackgroundColor ParseIndexed(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseIndexed(chunk.Data);
    }

    internal static PngBackgroundColor ParseGrayscale(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseGrayscale(data, 0, data.Length);
    }

    internal static PngBackgroundColor ParseGrayscale(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, GrayscaleDataLength, "Grayscale bKGD data must contain exactly 2 bytes.");

        PngBackgroundColor background = new PngBackgroundColor();
        background.Gray = PngBinaryReader.ReadUInt16(data, offset);
        return background;
    }

    internal static PngBackgroundColor ParseGrayscale(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseGrayscale(chunk.Data);
    }

    internal static PngBackgroundColor ParseTruecolor(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseTruecolor(data, 0, data.Length);
    }

    internal static PngBackgroundColor ParseTruecolor(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length, TruecolorDataLength, "Truecolor bKGD data must contain exactly 6 bytes.");

        PngBackgroundColor background = new PngBackgroundColor();
        background.Red = PngBinaryReader.ReadUInt16(data, offset);
        background.Green = PngBinaryReader.ReadUInt16(data, offset + 2);
        background.Blue = PngBinaryReader.ReadUInt16(data, offset + 4);
        return background;
    }

    internal static PngBackgroundColor ParseTruecolor(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return ParseTruecolor(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkBackgroundColor)
        {
            throw new InvalidDataException("Chunk type must be bKGD.");
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

internal class PngBackgroundColorWriter
{
    internal static byte[] WriteIndexedData(PngBackgroundColor background)
    {
        byte[] data = new byte[PngBackgroundColorParser.IndexedDataLength];
        WriteIndexedData(data, 0, background);
        return data;
    }

    internal static void WriteIndexedData(byte[] data, int offset, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(background);

        ValidateWriteRange(data, offset, PngBackgroundColorParser.IndexedDataLength);
        data[offset] = background.PaletteIndex;
    }

    internal static PngChunk WriteIndexedChunk(PngBackgroundColor background)
    {
        return CreateChunk(WriteIndexedData(background));
    }

    internal static byte[] WriteGrayscaleData(PngBackgroundColor background)
    {
        byte[] data = new byte[PngBackgroundColorParser.GrayscaleDataLength];
        WriteGrayscaleData(data, 0, background);
        return data;
    }

    internal static void WriteGrayscaleData(byte[] data, int offset, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(background);

        ValidateWriteRange(data, offset, PngBackgroundColorParser.GrayscaleDataLength);
        PngBinaryWriter.WriteUInt16(data, offset, background.Gray);
    }

    internal static PngChunk WriteGrayscaleChunk(PngBackgroundColor background)
    {
        return CreateChunk(WriteGrayscaleData(background));
    }

    internal static byte[] WriteTruecolorData(PngBackgroundColor background)
    {
        byte[] data = new byte[PngBackgroundColorParser.TruecolorDataLength];
        WriteTruecolorData(data, 0, background);
        return data;
    }

    internal static void WriteTruecolorData(byte[] data, int offset, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(background);

        ValidateWriteRange(data, offset, PngBackgroundColorParser.TruecolorDataLength);
        PngBinaryWriter.WriteUInt16(data, offset, background.Red);
        PngBinaryWriter.WriteUInt16(data, offset + 2, background.Green);
        PngBinaryWriter.WriteUInt16(data, offset + 4, background.Blue);
    }

    internal static PngChunk WriteTruecolorChunk(PngBackgroundColor background)
    {
        return CreateChunk(WriteTruecolorData(background));
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
        chunk.Type = PngConstants.ChunkBackgroundColor;
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
