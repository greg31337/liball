using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class Woff1Builder
{
    readonly List<Woff1BuilderTable> tables = new();
    byte[] metadata = Array.Empty<byte>();
    byte[] privateData = Array.Empty<byte>();

    internal Woff1Builder AddTable(string tag, byte[] data, bool compress = false)
    {
        tables.Add(new Woff1BuilderTable((int)TestSfntEncoding.EncodeTag(tag), data.ToArray(), compress));
        return this;
    }

    internal Woff1Builder AddMetadata(string xml)
    {
        metadata = Encoding.UTF8.GetBytes(xml);
        return this;
    }

    internal Woff1Builder AddPrivateData(byte[] data)
    {
        privateData = data.ToArray();
        return this;
    }

    internal byte[] Build()
    {
        if (tables.Count == 0)
        {
            throw new InvalidOperationException("At least one table is required.");
        }

        ApplyWholeFontChecksumAdjustment();

        foreach (Woff1BuilderTable table in tables)
        {
            table.PrepareStoredData();
        }

        List<Woff1BuilderTable> directory = tables
            .OrderBy(table => (uint)table.Tag)
            .ToList();
        int offset = 44 + directory.Count * 20;

        foreach (Woff1BuilderTable table in tables)
        {
            table.Offset = offset;
            offset = AlignFour(offset + table.StoredData.Length);
        }

        byte[] compressedMetadata = metadata.Length == 0
            ? Array.Empty<byte>()
            : Compress(metadata);
        int metadataOffset = 0;

        if (compressedMetadata.Length != 0)
        {
            metadataOffset = offset;
            offset += compressedMetadata.Length;
        }

        int privateOffset = 0;

        if (privateData.Length != 0)
        {
            offset = AlignFour(offset);
            privateOffset = offset;
            offset += privateData.Length;
        }

        int totalSfntSize = 12 + 16 * tables.Count +
            tables.Sum(table => AlignFour(table.Data.Length));
        byte[] result = new byte[offset];
        Span<byte> header = result;
        WriteUInt32(header, 0, 0x774F4646U);
        WriteUInt32(header, 4, 0x00010000U);
        WriteUInt32(header, 8, (uint)result.Length);
        WriteUInt16(header, 12, (ushort)tables.Count);
        WriteUInt16(header, 14, 0);
        WriteUInt32(header, 16, (uint)totalSfntSize);
        WriteUInt16(header, 20, 1);
        WriteUInt16(header, 22, 0);
        WriteUInt32(header, 24, (uint)metadataOffset);
        WriteUInt32(header, 28, (uint)compressedMetadata.Length);
        WriteUInt32(header, 32, (uint)metadata.Length);
        WriteUInt32(header, 36, (uint)privateOffset);
        WriteUInt32(header, 40, (uint)privateData.Length);

        for (int index = 0; index < directory.Count; index += 1)
        {
            Woff1BuilderTable table = directory[index];
            int recordOffset = 44 + index * 20;
            WriteUInt32(result, recordOffset, (uint)table.Tag);
            WriteUInt32(result, recordOffset + 4, (uint)table.Offset);
            WriteUInt32(result, recordOffset + 8, (uint)table.StoredData.Length);
            WriteUInt32(result, recordOffset + 12, (uint)table.Data.Length);
            WriteUInt32(result, recordOffset + 16, GetChecksum(table));
        }

        foreach (Woff1BuilderTable table in tables)
        {
            table.StoredData.CopyTo(result, table.Offset);
        }

        if (compressedMetadata.Length != 0)
        {
            compressedMetadata.CopyTo(result, metadataOffset);
        }

        if (privateData.Length != 0)
        {
            privateData.CopyTo(result, privateOffset);
        }

        return result;
    }

    void ApplyWholeFontChecksumAdjustment()
    {
        Woff1BuilderTable? head = tables.FirstOrDefault(table => table.Tag == FontTags.Head);

        if (head == null || head.Data.Length < 12)
        {
            return;
        }

        head.Data.AsSpan(8, 4).Clear();
        List<Woff1BuilderTable> directory = tables
            .OrderBy(table => (uint)table.Tag)
            .ToList();
        int totalLength = 12 + 16 * tables.Count +
            tables.Sum(table => AlignFour(table.Data.Length));
        byte[] sfnt = new byte[totalLength];
        WriteUInt32(sfnt, 0, 0x00010000U);
        WriteUInt16(sfnt, 4, (ushort)tables.Count);
        int maximumPower = 1;
        int selector = 0;

        while (maximumPower <= tables.Count / 2)
        {
            maximumPower *= 2;
            selector += 1;
        }

        WriteUInt16(sfnt, 6, (ushort)(maximumPower * 16));
        WriteUInt16(sfnt, 8, (ushort)selector);
        WriteUInt16(sfnt, 10, (ushort)(tables.Count * 16 - maximumPower * 16));
        int dataOffset = 12 + 16 * tables.Count;

        foreach (Woff1BuilderTable table in tables)
        {
            table.SfntOffset = dataOffset;
            table.Data.CopyTo(sfnt, dataOffset);
            dataOffset += AlignFour(table.Data.Length);
        }

        for (int index = 0; index < directory.Count; index += 1)
        {
            Woff1BuilderTable table = directory[index];
            int recordOffset = 12 + index * 16;
            WriteUInt32(sfnt, recordOffset, (uint)table.Tag);
            WriteUInt32(sfnt, recordOffset + 4, GetChecksum(table));
            WriteUInt32(sfnt, recordOffset + 8, (uint)table.SfntOffset);
            WriteUInt32(sfnt, recordOffset + 12, (uint)table.Data.Length);
        }

        uint adjustment = FontChecksum.CalculateChecksumAdjustment(sfnt, head.SfntOffset);
        WriteUInt32(head.Data, 8, adjustment);
    }

    static uint GetChecksum(Woff1BuilderTable table)
    {
        return table.Tag == FontTags.Head
            ? FontChecksum.CalculateHeadTable(table.Data)
            : FontChecksum.CalculateTable(table.Data);
    }

    static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new();

        using (ZLibStream compressor = new(output, CompressionLevel.SmallestSize, true))
        {
            compressor.Write(data);
        }

        return output.ToArray();
    }

    static int AlignFour(int value)
    {
        return (value + 3) & ~3;
    }

    static void WriteUInt16(Span<byte> data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), value);
    }

    static void WriteUInt32(Span<byte> data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), value);
    }

    internal static byte[] CompressForTest(byte[] data)
    {
        return Compress(data);
    }
}

internal class Woff1BuilderTable
{
    internal int Tag;
    internal byte[] Data;
    internal bool Compress;
    internal byte[] StoredData = Array.Empty<byte>();
    internal int Offset;
    internal int SfntOffset;

    internal Woff1BuilderTable(int tag, byte[] data, bool compress)
    {
        Tag = tag;
        Data = data;
        Compress = compress;
    }

    internal void PrepareStoredData()
    {
        byte[] compressed = Compress ? Woff1Builder.CompressForTest(Data) : Data;
        StoredData = Compress && compressed.Length < Data.Length ? compressed : Data;
    }
}
