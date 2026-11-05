using System.Buffers.Binary;
using System.IO.Compression;

namespace ttf.Tests;

internal class Woff2Builder
{
    readonly List<Woff2BuilderTable> tables = new();
    readonly List<Woff2BuilderFace> faces = new();
    byte[] metadata = Array.Empty<byte>();
    byte[] privateData = Array.Empty<byte>();
    uint collectionVersion = 0x00010000U;

    internal Woff2Builder AddKnownTable(int tagIndex, byte[] data)
    {
        int transformVersion = tagIndex == 10 || tagIndex == 11 ? 3 : 0;
        tables.Add(new Woff2BuilderTable(
            tagIndex,
            0,
            transformVersion,
            (uint)data.Length,
            data.ToArray(),
            false));
        return this;
    }

    internal Woff2Builder AddCustomTable(uint tag, byte[] data)
    {
        tables.Add(new Woff2BuilderTable(
            63,
            tag,
            0,
            (uint)data.Length,
            data.ToArray(),
            false));
        return this;
    }

    internal Woff2Builder AddTransformedKnownTable(
        int tagIndex,
        int transformVersion,
        uint originalLength,
        byte[] transformedData)
    {
        tables.Add(new Woff2BuilderTable(
            tagIndex,
            0,
            transformVersion,
            originalLength,
            transformedData.ToArray(),
            true));
        return this;
    }

    internal Woff2Builder AddCollectionFace(uint flavor, params int[] tableIndices)
    {
        faces.Add(new Woff2BuilderFace(flavor, tableIndices.ToArray()));
        return this;
    }

    internal Woff2Builder SetCollectionVersion(uint version)
    {
        collectionVersion = version;
        return this;
    }

    internal Woff2Builder AddMetadata(byte[] data)
    {
        metadata = data.ToArray();
        return this;
    }

    internal Woff2Builder AddPrivateData(byte[] data)
    {
        privateData = data.ToArray();
        return this;
    }

    internal byte[] Build()
    {
        List<byte> directory = new();

        foreach (Woff2BuilderTable table in tables)
        {
            directory.Add((byte)((table.TransformVersion << 6) | table.TagIndex));

            if (table.TagIndex == 63)
            {
                WriteUInt32(directory, table.Tag);
            }

            WriteUIntBase128(directory, table.OriginalLength);

            if (table.HasTransformLength)
            {
                WriteUIntBase128(directory, (uint)table.Data.Length);
            }
        }

        if (faces.Count != 0)
        {
            WriteUInt32(directory, collectionVersion);
            Write255UInt16(directory, faces.Count);

            foreach (Woff2BuilderFace face in faces)
            {
                Write255UInt16(directory, face.TableIndices.Length);
                WriteUInt32(directory, face.Flavor);

                foreach (int tableIndex in face.TableIndices)
                {
                    Write255UInt16(directory, tableIndex);
                }
            }
        }

        byte[] uncompressedTables = tables.SelectMany(table => table.Data).ToArray();
        byte[] compressedTables = Compress(uncompressedTables);
        byte[] compressedMetadata = metadata.Length == 0
            ? Array.Empty<byte>()
            : Compress(metadata);
        int compressedOffset = 48 + directory.Count;
        int offset = compressedOffset + compressedTables.Length;
        int metadataOffset = 0;

        if (compressedMetadata.Length != 0)
        {
            offset = AlignFour(offset);
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

        byte[] result = new byte[offset];
        WriteUInt32(result, 0, 0x774F4632U);
        WriteUInt32(result, 4, faces.Count == 0 ? 0x00010000U : 0x74746366U);
        WriteUInt32(result, 8, (uint)result.Length);
        WriteUInt16(result, 12, (ushort)tables.Count);
        WriteUInt16(result, 14, 0);
        WriteUInt32(result, 16, CalculateReferenceSfntSize());
        WriteUInt32(result, 20, (uint)compressedTables.Length);
        WriteUInt16(result, 24, 1);
        WriteUInt16(result, 26, 0);
        WriteUInt32(result, 28, (uint)metadataOffset);
        WriteUInt32(result, 32, (uint)compressedMetadata.Length);
        WriteUInt32(result, 36, (uint)metadata.Length);
        WriteUInt32(result, 40, (uint)privateOffset);
        WriteUInt32(result, 44, (uint)privateData.Length);
        directory.CopyTo(result, 48);
        compressedTables.CopyTo(result, compressedOffset);

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

    uint CalculateReferenceSfntSize()
    {
        long size = 12 + tables.Count * 16;

        foreach (Woff2BuilderTable table in tables)
        {
            size += ((long)table.OriginalLength + 3L) & -4L;

            if (size > uint.MaxValue)
            {
                return uint.MaxValue;
            }
        }

        return (uint)size;
    }

    static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new();

        using (BrotliStream compressor = new(output, CompressionLevel.SmallestSize, true))
        {
            compressor.Write(data);
        }

        return output.ToArray();
    }

    static void WriteUIntBase128(List<byte> output, uint value)
    {
        int shift = 28;

        while (shift > 0 && ((value >> shift) & 0x7FU) == 0)
        {
            shift -= 7;
        }

        while (shift > 0)
        {
            output.Add((byte)(((value >> shift) & 0x7FU) | 0x80U));
            shift -= 7;
        }

        output.Add((byte)(value & 0x7FU));
    }

    static void Write255UInt16(List<byte> output, int value)
    {
        if (value < 253)
        {
            output.Add((byte)value);
            return;
        }

        output.Add(253);
        output.Add((byte)(value >> 8));
        output.Add((byte)value);
    }

    static int AlignFour(int value)
    {
        return (value + 3) & ~3;
    }

    static void WriteUInt32(List<byte> output, uint value)
    {
        output.Add((byte)(value >> 24));
        output.Add((byte)(value >> 16));
        output.Add((byte)(value >> 8));
        output.Add((byte)value);
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}

internal class Woff2BuilderTable
{
    internal int TagIndex;
    internal uint Tag;
    internal int TransformVersion;
    internal uint OriginalLength;
    internal byte[] Data;
    internal bool HasTransformLength;

    internal Woff2BuilderTable(
        int tagIndex,
        uint tag,
        int transformVersion,
        uint originalLength,
        byte[] data,
        bool hasTransformLength)
    {
        TagIndex = tagIndex;
        Tag = tag;
        TransformVersion = transformVersion;
        OriginalLength = originalLength;
        Data = data;
        HasTransformLength = hasTransformLength;
    }
}

internal class Woff2BuilderFace
{
    internal uint Flavor;
    internal int[] TableIndices;

    internal Woff2BuilderFace(uint flavor, int[] tableIndices)
    {
        Flavor = flavor;
        TableIndices = tableIndices;
    }
}
