namespace gltf.Tests.Internal.Conversion;

[TestFixture]
public class GltfWriterPreflightSparseTests
{
    [Test]
    public void ValidateDocument_AcceptsValidSparsePayloadsWithResolvedIndices()
    {
        GltfDocument document = CreateSparseDocument();

        Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(document));
    }

    [TestCase(0, 4)]
    [TestCase(5, 4)]
    public void ValidateDocument_RejectsInvalidSparseCount(int sparseCount, int accessorCount)
    {
        GltfDocument document = CreateSparseDocument();
        document.Accessors[0].Count = accessorCount;
        document.Accessors[0].Sparse!.Count = sparseCount;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                $"Sparse count /accessors/0/sparse/count value {sparseCount} must be positive and no greater than accessor count {accessorCount}."));
    }

    [Test]
    public void ValidateDocument_RejectsSignedSparseIndexComponents()
    {
        GltfDocument document = CreateSparseDocument();
        document.Accessors[0].Sparse!.Indices.ComponentType = GltfComponentType.SignedShort;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Sparse index component type 5122 at /accessors/0/sparse/indices/componentType must be unsigned byte, unsigned short, or unsigned int."));
    }

    [Test]
    public void ValidateDocument_RejectsStrideOnSparseIndexView()
    {
        GltfDocument document = CreateSparseDocument();
        document.BufferViews[0].HasByteStride = true;
        document.BufferViews[0].ByteStride = 4;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Sparse payload buffer view referenced by /accessors/0/sparse/indices must not define byteStride."));
    }

    [Test]
    public void ValidateDocument_RejectsTargetOnSparseValueView()
    {
        GltfDocument document = CreateSparseDocument();
        document.BufferViews[1].HasTarget = true;
        document.BufferViews[1].Target = GltfBufferViewTarget.ArrayBuffer;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Sparse payload buffer view referenced by /accessors/0/sparse/values must not define target."));
    }

    [Test]
    public void ValidateDocument_RejectsSparseIndexRangeBeyondItsView()
    {
        GltfDocument document = CreateSparseDocument();
        document.BufferViews[0].ByteLength = 1;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Sparse index byte range /accessors/0/sparse/indices ends at 2, beyond buffer view 0 byteLength 1."));
    }

    [Test]
    public void ValidateDocument_RejectsSparseValueRangeBeyondItsView()
    {
        GltfDocument document = CreateSparseDocument();
        document.BufferViews[1].ByteLength = 7;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Sparse value byte range /accessors/0/sparse/values ends at 8, beyond buffer view 1 byteLength 7."));
    }

    [Test]
    public void ValidateDocument_RejectsMisalignedSparsePayloadOffsets()
    {
        GltfDocument indicesDocument = CreateSparseDocument();
        GltfAccessorSparse indicesSparse = indicesDocument.Accessors[0].Sparse!;
        indicesSparse.Indices.ComponentType = GltfComponentType.UnsignedShort;
        indicesSparse.Indices.HasByteOffset = true;
        indicesSparse.Indices.ByteOffset = 1;
        GltfDocument valuesDocument = CreateSparseDocument();
        GltfAccessorSparse valuesSparse = valuesDocument.Accessors[0].Sparse!;
        valuesSparse.Values.HasByteOffset = true;
        valuesSparse.Values.ByteOffset = 1;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfWriterPreflight().ValidateDocument(indicesDocument),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                    "Sparse index byte offset /accessors/0/sparse/indices/byteOffset value 1 is not aligned to component byte length 2."));
            Assert.That(
                () => new GltfWriterPreflight().ValidateDocument(valuesDocument),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                    "Sparse value byte offset /accessors/0/sparse/values/byteOffset value 1 is not aligned to component byte length 2."));
        });
    }

    [TestCase(new byte[] { 1, 1 }, "Sparse index 1 at /accessors/0/sparse/indices/1 is not strictly greater than 1.")]
    [TestCase(new byte[] { 1, 4 }, "Sparse index 4 at /accessors/0/sparse/indices/1 is outside accessor count 4.")]
    public void ValidateDocument_RejectsInvalidResolvedSparseIndices(byte[] indices, string message)
    {
        GltfDocument document = CreateSparseDocument();
        document.Buffers[0].ResolvedBytes[0] = indices[0];
        document.Buffers[0].ResolvedBytes[1] = indices[1];

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
    }

    private static GltfDocument CreateSparseDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 64;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[64];
        buffer.ResolvedBytes[0] = 1;
        buffer.ResolvedBytes[1] = 3;
        document.Buffers.Add(buffer);

        GltfBufferView indicesView = new GltfBufferView();
        indicesView.Buffer = 0;
        indicesView.HasByteOffset = true;
        indicesView.ByteOffset = 0;
        indicesView.ByteLength = 16;
        document.BufferViews.Add(indicesView);
        GltfBufferView valuesView = new GltfBufferView();
        valuesView.Buffer = 0;
        valuesView.HasByteOffset = true;
        valuesView.ByteOffset = 16;
        valuesView.ByteLength = 32;
        document.BufferViews.Add(valuesView);

        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.UnsignedShort;
        accessor.Count = 4;
        accessor.Type = GltfAccessorType.Vector2;
        GltfAccessorSparse sparse = new GltfAccessorSparse();
        sparse.Count = 2;
        sparse.Indices.BufferView = 0;
        sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        sparse.Values.BufferView = 1;
        accessor.Sparse = sparse;
        document.Accessors.Add(accessor);
        return document;
    }
}
