using System.IO;

namespace png;

internal class PngPaletteHistogramParser
{
    internal const int BytesPerEntry = 2;
    internal const int MaximumEntryCount = 256;
    internal const int MaximumDataLength = 512;

    internal static PngPaletteHistogram Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngPaletteHistogram Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);

        int entryCount = length / BytesPerEntry;
        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = new ushort[entryCount];
        int index = 0;
        int dataOffset = offset;
        while (index < entryCount)
        {
            histogram.Frequencies[index] = PngBinaryReader.ReadUInt16(data, dataOffset);
            dataOffset += BytesPerEntry;
            index += 1;
        }

        return histogram;
    }

    internal static PngPaletteHistogram Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkPaletteHistogram)
        {
            throw new InvalidDataException("Chunk type must be hIST.");
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
        if (length == 0)
        {
            throw new InvalidDataException("hIST data must contain at least one frequency entry.");
        }

        if (length > MaximumDataLength)
        {
            throw new InvalidDataException("hIST data must contain no more than 256 frequency entries.");
        }

        if (length % BytesPerEntry != 0)
        {
            throw new InvalidDataException("hIST data length must be a multiple of 2 bytes.");
        }
    }
}

internal class PngPaletteHistogramWriter
{
    internal static byte[] WriteData(PngPaletteHistogram histogram)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        ArgumentNullException.ThrowIfNull(histogram.Frequencies);

        ValidateEntryCount(histogram.Frequencies.Length);
        byte[] data = new byte[histogram.Frequencies.Length * PngPaletteHistogramParser.BytesPerEntry];
        WriteData(data, 0, histogram);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngPaletteHistogram histogram)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(histogram);
        ArgumentNullException.ThrowIfNull(histogram.Frequencies);

        ValidateEntryCount(histogram.Frequencies.Length);
        int length = histogram.Frequencies.Length * PngPaletteHistogramParser.BytesPerEntry;
        ValidateWriteRange(data, offset, length);

        int index = 0;
        int dataOffset = offset;
        while (index < histogram.Frequencies.Length)
        {
            PngBinaryWriter.WriteUInt16(data, dataOffset, histogram.Frequencies[index]);
            dataOffset += PngPaletteHistogramParser.BytesPerEntry;
            index += 1;
        }
    }

    internal static PngChunk WriteChunk(PngPaletteHistogram histogram)
    {
        return CreateChunk(WriteData(histogram));
    }

    internal static void ValidateEntryCount(int count)
    {
        if (count <= 0 || count > PngPaletteHistogramParser.MaximumEntryCount)
        {
            throw new ArgumentOutOfRangeException("Frequencies");
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
        chunk.Type = PngConstants.ChunkPaletteHistogram;
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
