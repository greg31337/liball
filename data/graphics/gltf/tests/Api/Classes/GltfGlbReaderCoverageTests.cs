using System.Text;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfGlbReaderCoverageTests
{
    private const uint UnknownChunkType = 0x12345678U;

    [Test]
    public void GlbEntryPoints_ReadDocumentsAndRawContainersFromEveryInputShape()
    {
        byte[] glb = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{\"asset\":{\"version\":\"2.0\",\"generator\":\"GLB paths\"}}")],
            []);
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        string path = workspace.WriteBytes("model.glb", glb);
        IGltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();
        HostileTestStream documentStream = CreateReadStream(glb);
        HostileTestStream containerStream = CreateReadStream(glb);

        GltfReadResult fileResult = reader.ReadFile(path, options);
        GltfReadResult byteResult = reader.ReadBytes(glb, options);
        GltfReadResult streamResult = reader.ReadStream(documentStream, options);
        GltfReadResult rawByteResult = reader.ReadContainerBytes(glb, options);
        GltfReadResult rawStreamResult = reader.ReadContainerStream(
            containerStream,
            options);
        GltfReadResult suppliedResult = reader.ReadContainer(
            rawByteResult.Container!,
            options);

        Assert.Multiple(() =>
        {
            AssertGlbDocument(fileResult, "GLB paths");
            AssertGlbDocument(byteResult, "GLB paths");
            AssertGlbDocument(streamResult, "GLB paths");
            AssertGlbDocument(suppliedResult, "GLB paths");
            AssertRawGlb(rawByteResult);
            AssertRawGlb(rawStreamResult);
            Assert.That(documentStream.SeekAttempted, Is.False);
            Assert.That(containerStream.SeekAttempted, Is.False);
            Assert.That(documentStream.Disposed, Is.False);
            Assert.That(containerStream.Disposed, Is.False);
        });

        documentStream.Dispose();
        containerStream.Dispose();
    }

    [Test]
    public void ReadBytes_AcceptsEmptyAssetAndAlignedBinAsset()
    {
        byte[] emptyAsset = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{\"asset\":{\"version\":\"2.0\"}}")],
            []);
        byte[] binAsset = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbBinChunkType],
            [
                PadJson(
                    "{\"asset\":{\"version\":\"2.0\"},"
                    + "\"buffers\":[{\"byteLength\":3}]}"),
                [1, 2, 3, 0]
            ],
            []);

        GltfReadResult empty = new GltfReader().ReadBytes(
            emptyAsset,
            new GltfReadOptions());
        GltfReadResult binary = new GltfReader().ReadBytes(
            binAsset,
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(empty.Document, Is.Not.Null);
            Assert.That(empty.Document!.Buffers, Is.Empty);
            Assert.That(empty.Container!.HasBinChunk, Is.False);
            Assert.That(binary.Document, Is.Not.Null);
            Assert.That(binary.Container!.HasBinChunk, Is.True);
            Assert.That(binary.Container.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(binary.Document!.Buffers[0].Resolved, Is.True);
            Assert.That(binary.Document.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(binary.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(binary.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
        });
    }

    [Test]
    public void ContainerBytes_RejectsEveryInvalidHeaderShape()
    {
        GltfReadOptions forcedGlb = new GltfReadOptions
        {
            Format = GltfContainerFormat.Glb
        };
        byte[] wrongMagic = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(wrongMagic, 0, 0x46546C68U);
        byte[] wrongVersion = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(wrongVersion, 4, 1U);
        byte[] tooShort = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(tooShort, 8, 19U);
        byte[] truncated = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(truncated, 8, 100U);
        byte[] signedOverflow = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(signedOverflow, 8, 0x80000000U);

        Assert.Multiple(() =>
        {
            AssertGlbParseFailure(new byte[11], forcedGlb, "header is truncated");
            AssertGlbParseFailure(wrongMagic, forcedGlb, "Invalid GLB magic");
            AssertGlbParseFailure(wrongVersion, forcedGlb, "Unsupported GLB version");
            AssertGlbParseFailure(tooShort, forcedGlb, "smaller than the minimum");
            AssertGlbParseFailure(truncated, forcedGlb, "GLB is truncated");
            AssertGlbParseFailure(signedOverflow, forcedGlb,
                "exceeds the supported signed length");
        });
    }

    [Test]
    public void ContainerBytes_RejectsInvalidChunkOrderLengthsAndTruncation()
    {
        byte[] firstBin = CreateGlb(
            [GltfConstants.GlbBinChunkType],
            [[0, 0, 0, 0]],
            []);
        byte[] duplicateJson = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbJsonChunkType],
            [PadJson("{}"), PadJson("{}")],
            []);
        byte[] duplicateBin = CreateGlb(
            [
                GltfConstants.GlbJsonChunkType,
                GltfConstants.GlbBinChunkType,
                GltfConstants.GlbBinChunkType
            ],
            [PadJson("{}"), [0, 0, 0, 0], [0, 0, 0, 0]],
            []);
        byte[] lateBin = CreateGlb(
            [
                GltfConstants.GlbJsonChunkType,
                UnknownChunkType,
                GltfConstants.GlbBinChunkType
            ],
            [PadJson("{}"), [1, 2, 3, 4], [0, 0, 0, 0]],
            []);
        byte[] unaligned = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x20]],
            []);
        byte[] overflowing = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(overflowing, 12, 0x80000000U);
        byte[] beyondEnd = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            []);
        WriteUInt32LittleEndian(beyondEnd, 12, 8U);
        byte[] partialHeader = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{}")],
            [0, 0, 0, 0]);
        WriteUInt32LittleEndian(partialHeader, 8, (uint)partialHeader.Length);

        Assert.Multiple(() =>
        {
            AssertGlbParseFailure(firstBin, new GltfReadOptions(),
                "first GLB chunk is not JSON");
            AssertGlbParseFailure(duplicateJson, new GltfReadOptions(),
                "duplicate JSON chunk");
            AssertGlbParseFailure(duplicateBin, new GltfReadOptions(),
                "duplicate BIN chunk");
            AssertGlbParseFailure(lateBin, new GltfReadOptions(),
                "BIN chunk is not the second chunk");
            AssertGlbParseFailure(unaligned, new GltfReadOptions(),
                "is not aligned to 4 bytes");
            AssertGlbParseFailure(overflowing, new GltfReadOptions(),
                "exceeds the supported signed length");
            AssertGlbParseFailure(beyondEnd, new GltfReadOptions(),
                "beyond container length");
            AssertGlbParseFailure(partialHeader, new GltfReadOptions(),
                "chunk header is truncated");
        });
    }

    [Test]
    public void ReadBytes_ValidatesJsonAndBinPadding()
    {
        byte[] invalidJsonPadding = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [[0x7B, 0x7D, 0x00, 0x20]],
            []);
        byte[] invalidBinPadding = CreateGlb(
            [GltfConstants.GlbJsonChunkType, GltfConstants.GlbBinChunkType],
            [
                PadJson(
                    "{\"asset\":{\"version\":\"2.0\"},"
                    + "\"buffers\":[{\"byteLength\":3}]}"),
                [1, 2, 3, 9]
            ],
            []);
        GltfReadResult raw = new GltfReader().ReadContainerBytes(
            invalidBinPadding,
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(raw.Container!.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 9 }));
            AssertGlbParseFailure(invalidJsonPadding, new GltfReadOptions(),
                "Invalid GLB JSON padding byte");
            AssertGlbDocumentFailure(invalidBinPadding,
                "Invalid GLB BIN padding byte at BIN chunk offset 3; expected 0x00.");
        });
    }

    [Test]
    public void ContainerPaths_PreserveOrDiscardUnknownChunksAndOwnTrailingBytes()
    {
        byte[] glb = CreateGlb(
            [
                GltfConstants.GlbJsonChunkType,
                GltfConstants.GlbBinChunkType,
                UnknownChunkType
            ],
            [
                PadJson(
                    "{\"asset\":{\"version\":\"2.0\"},"
                    + "\"buffers\":[{\"byteLength\":3}]}"),
                [1, 2, 3, 0],
                [4, 5, 6, 7]
            ],
            [9, 8, 7]);
        byte[] expected = glb.ToArray();
        GltfReader reader = new GltfReader();
        GltfReadResult preserved = reader.ReadBytes(glb, new GltfReadOptions());
        GltfReadOptions discardOptions = new GltfReadOptions
        {
            PreserveUnknownChunks = false
        };
        GltfReadResult discarded = reader.ReadContainerBytes(glb, discardOptions);
        glb[^1] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Container!.UnknownChunks, Has.Count.EqualTo(1));
            Assert.That(preserved.Container.UnknownChunks[0].Type,
                Is.EqualTo(UnknownChunkType));
            Assert.That(preserved.Container.UnknownChunks[0].Data,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(preserved.Container.TrailingBytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(preserved.Container.OriginalByteLength,
                Is.EqualTo(expected.Length));
            Assert.That(discarded.Container!.UnknownChunks, Is.Empty);
            Assert.That(discarded.Container.TrailingBytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void NonSeekableGlbStreams_ApplyLimitsAndOwnershipOnFailure()
    {
        byte[] glb = CreateGlb(
            [GltfConstants.GlbJsonChunkType],
            [PadJson("{\"asset\":{\"version\":\"2.0\"}}")],
            []);
        HostileTestStream stream = CreateReadStream(glb);
        GltfReadOptions options = new GltfReadOptions
        {
            LeaveStreamOpen = false
        };
        options.Limits.MaximumInputByteLength = glb.Length - 1;

        Assert.That(
            () => new GltfReader().ReadContainerStream(stream, options),
            Throws.TypeOf<GltfParseException>()
                .With.Message.Contains("Input stream exceeds the configured maximum"));
        Assert.Multiple(() =>
        {
            Assert.That(stream.Disposed, Is.True);
            Assert.That(stream.SeekAttempted, Is.False);
            Assert.That(stream.ReadByteLength, Is.EqualTo(glb.Length));
        });
    }

    private static HostileTestStream CreateReadStream(byte[] bytes)
    {
        return new HostileTestStream(
            bytes,
            true,
            false,
            false,
            3,
            int.MaxValue);
    }

    private static void AssertGlbDocument(GltfReadResult result, string generator)
    {
        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Document!.Asset.Generator, Is.EqualTo(generator));
        Assert.That(result.Container, Is.SameAs(result.Document.Container));
        Assert.That(result.Container!.Format, Is.EqualTo(GltfContainerFormat.Glb));
    }

    private static void AssertRawGlb(GltfReadResult result)
    {
        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(result.Document, Is.Null);
        Assert.That(result.Container, Is.Not.Null);
        Assert.That(result.Container!.Format, Is.EqualTo(GltfContainerFormat.Glb));
    }

    private static void AssertGlbParseFailure(
        byte[] bytes,
        GltfReadOptions options,
        string message)
    {
        Assert.That(
            () => new GltfReader().ReadContainerBytes(bytes, options),
            Throws.TypeOf<GltfParseException>().With.Message.Contains(message));
    }

    private static void AssertGlbDocumentFailure(byte[] bytes, string message)
    {
        Assert.That(
            () => new GltfReader().ReadBytes(bytes, new GltfReadOptions()),
            Throws.TypeOf<GltfParseException>().With.Message.EqualTo(message));
    }

    private static byte[] PadJson(string json)
    {
        byte[] source = Encoding.UTF8.GetBytes(json);
        int padding = (GltfConstants.AlignmentByteLength
            - source.Length % GltfConstants.AlignmentByteLength)
            % GltfConstants.AlignmentByteLength;
        byte[] result = new byte[source.Length + padding];
        source.CopyTo(result, 0);
        int index = source.Length;
        while (index < result.Length)
        {
            result[index] = GltfConstants.GlbJsonPaddingByte;
            index++;
        }

        return result;
    }

    private static byte[] CreateGlb(
        uint[] chunkTypes,
        byte[][] chunkData,
        byte[] trailingBytes)
    {
        int declaredByteLength = GltfConstants.GlbHeaderByteLength;
        int index = 0;
        while (index < chunkData.Length)
        {
            declaredByteLength += GltfConstants.GlbChunkHeaderByteLength
                + chunkData[index].Length;
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
            chunkData[index].CopyTo(
                bytes,
                offset + GltfConstants.GlbChunkHeaderByteLength);
            offset += GltfConstants.GlbChunkHeaderByteLength
                + chunkData[index].Length;
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
