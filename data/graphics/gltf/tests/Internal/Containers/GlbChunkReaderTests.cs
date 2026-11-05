namespace gltf.Tests.Internal.Containers;

[TestFixture]
public class GlbChunkReaderTests
{
    [Test]
    public void ReadChunks_ReturnsOwnedKnownChunksAndTrailingBytes()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbBinChunkType],
            [[0x7B, 0x7D, 0x20, 0x20], [1, 2, 3, 0]],
            [9, 8]);
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);

        GltfContainerData container = reader.ReadChunks(bytes, header, true);
        bytes[20] = 0;
        bytes[32] = 0;
        bytes[36] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(container.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(container.JsonBytes, Is.EqualTo(new byte[] { 0x7B, 0x7D, 0x20, 0x20 }));
            Assert.That(container.HasBinChunk, Is.True);
            Assert.That(container.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(container.UnknownChunks, Is.Empty);
            Assert.That(container.TrailingBytes, Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(container.OriginalByteLength, Is.EqualTo(38));
        });
    }

    [Test]
    public void ReadChunks_PreservesOrSkipsUnknownChunksByPolicy()
    {
        GlbReader reader = new GlbReader();
        uint unknownType = 0x12345678U;
        byte[] bytes = CreateGlb(
            [GltfConstants.GlbJsonChunkType, unknownType],
            [[0x7B, 0x7D, 0x20, 0x20], [4, 5, 6, 7]],
            []);
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);

        GltfContainerData preserved = reader.ReadChunks(bytes, header, true);
        GltfContainerData skipped = reader.ReadChunks(bytes, header, false);
        bytes[32] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(preserved.UnknownChunks, Has.Count.EqualTo(1));
            Assert.That(preserved.UnknownChunks[0].Type, Is.EqualTo(unknownType));
            Assert.That(preserved.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(skipped.UnknownChunks, Is.Empty);
        });
    }

    [Test]
    public void ReadChunks_RequiresJsonAsTheFirstChunk()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(
            [GltfConstants.GlbBinChunkType],
            [[0, 0, 0, 0]],
            []);
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);

        Assert.That(
            () => reader.ReadChunks(bytes, header, true),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("The first GLB chunk is not JSON at byte 16."));
    }

    [Test]
    public void ReadChunks_RejectsDuplicateJsonAndBinChunks()
    {
        GlbReader reader = new GlbReader();
        byte[] duplicateJson = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20, 0x20], [0x7B, 0x7D, 0x20, 0x20]],
            []);
        byte[] duplicateBin = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbBinChunkType, GltfConstants.GlbBinChunkType],
            [[0x7B, 0x7D, 0x20, 0x20], [1, 2, 3, 0], [4, 5, 6, 0]],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadChunks(
                    duplicateJson,
                    reader.ReadHeader(duplicateJson, duplicateJson.Length),
                    true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB contains a duplicate JSON chunk at byte 28."));
            Assert.That(
                () => reader.ReadChunks(
                    duplicateBin,
                    reader.ReadHeader(duplicateBin, duplicateBin.Length),
                    true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB contains a duplicate BIN chunk at byte 40."));
        });
    }

    [Test]
    public void ReadChunks_RejectsUnalignedAndOverflowingChunkLengths()
    {
        GlbReader reader = new GlbReader();
        byte[] unaligned = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20]],
            []);
        byte[] overflowing = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20, 0x20]],
            []);
        WriteUInt32LittleEndian(overflowing, 12, 0x80000000U);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadChunks(unaligned, reader.ReadHeader(unaligned, unaligned.Length), true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB chunk length 3 at byte 12 is not aligned to 4 bytes."));
            Assert.That(
                () => reader.ReadChunks(overflowing, reader.ReadHeader(overflowing, overflowing.Length), true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "GLB chunk length 2147483648 at byte 12 exceeds the supported signed length."));
        });
    }

    [Test]
    public void ReadChunks_RejectsChunkDataBeyondTheDeclaredContainerLength()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20, 0x20]],
            []);
        WriteUInt32LittleEndian(bytes, 12, 8U);
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);

        Assert.That(
            () => reader.ReadChunks(bytes, header, true),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("GLB chunk at byte 12 declares 8 data bytes beyond container length 24."));
    }

    [Test]
    public void ReadChunks_RejectsIncompleteChunkHeaderAtDeclaredEnd()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20, 0x20]],
            [0, 0, 0, 0]);
        WriteUInt32LittleEndian(bytes, 8, 28U);
        GlbHeader header = reader.ReadHeader(bytes, bytes.Length);

        Assert.That(
            () => reader.ReadChunks(bytes, header, true),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("GLB chunk header is truncated at byte 24; 4 bytes remain."));
    }

    [Test]
    public void ReadChunks_RejectsEmptyJsonAndInvalidJsonPadding()
    {
        GlbReader reader = new GlbReader();
        byte[] emptyJson = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[]],
            []);
        byte[] invalidPadding = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x00, 0x20]],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadChunks(emptyJson, reader.ReadHeader(emptyJson, emptyJson.Length), true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB JSON chunk is empty at byte 20."));
            Assert.That(
                () => reader.ReadChunks(
                    invalidPadding,
                    reader.ReadHeader(invalidPadding, invalidPadding.Length),
                    true),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("Invalid GLB JSON padding byte at byte 22; expected 0x20."));
        });
    }

    [Test]
    public void ReadChunks_RejectsInvalidHeaderArgument()
    {
        GlbReader reader = new GlbReader();
        GlbHeader invalidHeader = new GlbHeader();
        invalidHeader.DeclaredByteLength = 100;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadChunks([], null!, true),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("header"));
            Assert.That(
                () => reader.ReadChunks([], invalidHeader, true),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("header"));
        });
    }

    [Test]
    public void ReaderBytes_ValidatesChunksBeforeDocumentParsing()
    {
        GltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();
        byte[] valid = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}} ")],
            []);
        byte[] invalidFirstChunk = CreateGlb(
            [GltfConstants.GlbBinChunkType],
            [[0, 0, 0, 0]],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadBytes(invalidFirstChunk, options),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The first GLB chunk is not JSON at byte 16."));
            Assert.That(reader.ReadBytes(valid, options).Document, Is.Not.Null);
        });
    }

    private static byte[] CreateGlb(uint[] chunkTypes, byte[][] chunkData, byte[] trailingBytes)
    {
        int declaredByteLength = GltfConstants.GlbHeaderByteLength;
        int index = 0;
        while (index < chunkData.Length)
        {
            declaredByteLength += GltfConstants.GlbChunkHeaderByteLength + chunkData[index].Length;
            index++;
        }

        byte[] bytes = new byte[declaredByteLength + trailingBytes.Length];
        WriteUInt32LittleEndian(bytes, 0, GltfConstants.GlbMagic);
        WriteUInt32LittleEndian(bytes, 4, (uint)GltfConstants.GlbVersion);
        WriteUInt32LittleEndian(bytes, 8, (uint)declaredByteLength);

        int offset = GltfConstants.GlbHeaderByteLength;
        index = 0;
        while (index < chunkData.Length)
        {
            WriteUInt32LittleEndian(bytes, offset, (uint)chunkData[index].Length);
            WriteUInt32LittleEndian(bytes, offset + 4, chunkTypes[index]);
            chunkData[index].CopyTo(bytes, offset + GltfConstants.GlbChunkHeaderByteLength);
            offset += GltfConstants.GlbChunkHeaderByteLength + chunkData[index].Length;
            index++;
        }

        trailingBytes.CopyTo(bytes, declaredByteLength);
        return bytes;
    }

    private static void WriteUInt32LittleEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }
}
