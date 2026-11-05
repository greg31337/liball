namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class SparseAccessorDecoderTests
{
    [Test]
    public void OverlayRawElements_UsesZeroInitializedBaseWhenDenseViewIsAbsent()
    {
        SparseAccessorDecoder sparseDecoder = new SparseAccessorDecoder();
        AccessorByteRange indicesRange = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            2,
            0,
            0,
            0);
        AccessorByteRange valuesRange = CreateRange(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            2,
            0,
            0,
            0);

        byte[] raw = sparseDecoder.OverlayRawElements(
            [],
            null,
            new byte[] { 1, 3 },
            indicesRange,
            new byte[] { 10, 0, 20, 0, 30, 0, 40, 0 },
            valuesRange,
            4);
        uint[] result = new AccessorDecoder().DecodeUnsignedValues(
            raw,
            CreateRange(
                GltfComponentType.UnsignedShort,
                GltfAccessorType.Vector2,
                4,
                0,
                0,
                0));

        Assert.That(
            result,
            Is.EqualTo(new uint[] { 0, 0, 10, 20, 0, 0, 30, 40 }));
    }

    [Test]
    public void OverlayRawElements_PacksInterleavedBaseBeforeReplacingElements()
    {
        byte[] baseSource =
        {
            1, 0, 2, 0, 0xa1, 0xa2, 0xa3, 0xa4,
            3, 0, 4, 0, 0xb1, 0xb2, 0xb3, 0xb4,
            5, 0, 6, 0
        };
        AccessorByteRange baseRange = CreateRange(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            3,
            0,
            0,
            8);
        AccessorByteRange indicesRange = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        AccessorByteRange valuesRange = CreateRange(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            1,
            0,
            0,
            0);
        SparseAccessorDecoder sparseDecoder = new SparseAccessorDecoder();

        byte[] raw = sparseDecoder.OverlayRawElements(
            baseSource,
            baseRange,
            new byte[] { 1 },
            indicesRange,
            new byte[] { 9, 0, 10, 0 },
            valuesRange,
            3);
        uint[] result = new AccessorDecoder().DecodeUnsignedValues(
            raw,
            CreateRange(
                GltfComponentType.UnsignedShort,
                GltfAccessorType.Vector2,
                3,
                0,
                0,
                0));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new uint[] { 1, 2, 9, 10, 5, 6 }));
            Assert.That(baseSource[8], Is.EqualTo(3));
        });
    }

    [Test]
    public void DecodeSparseIndices_ReadsEveryUnsignedIndexWidth()
    {
        SparseAccessorDecoder decoder = new SparseAccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                decoder.DecodeSparseIndices(
                    new byte[] { 1, 4 },
                    CreateRange(GltfComponentType.UnsignedByte, GltfAccessorType.Scalar, 2, 0, 0, 0),
                    5),
                Is.EqualTo(new uint[] { 1, 4 }));
            Assert.That(
                decoder.DecodeSparseIndices(
                    new byte[] { 1, 0, 4, 0 },
                    CreateRange(GltfComponentType.UnsignedShort, GltfAccessorType.Scalar, 2, 0, 0, 0),
                    5),
                Is.EqualTo(new uint[] { 1, 4 }));
            Assert.That(
                decoder.DecodeSparseIndices(
                    new byte[] { 1, 0, 0, 0, 4, 0, 0, 0 },
                    CreateRange(GltfComponentType.UnsignedInt, GltfAccessorType.Scalar, 2, 0, 0, 0),
                    5),
                Is.EqualTo(new uint[] { 1, 4 }));
        });
    }

    [TestCase(new byte[] { 1, 1 }, "not strictly greater")]
    [TestCase(new byte[] { 2, 1 }, "not strictly greater")]
    [TestCase(new byte[] { 1, 5 }, "outside accessor count")]
    public void DecodeSparseIndices_RejectsInvalidSequences(byte[] source, string message)
    {
        AccessorByteRange range = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            2,
            0,
            0,
            0);

        Assert.That(
            () => new SparseAccessorDecoder().DecodeSparseIndices(source, range, 5),
            Throws.Exception.With.Message.Contains(message));
    }

    [Test]
    public void DecodeSparseIndices_RejectsSignedAndMisalignedLayouts()
    {
        AccessorByteRange signed = CreateRange(
            GltfComponentType.SignedShort,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        AccessorByteRange misaligned = CreateRange(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            1,
            0,
            1,
            0);
        SparseAccessorDecoder decoder = new SparseAccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.DecodeSparseIndices(new byte[2], signed, 2),
                Throws.ArgumentException.With.Message.StartsWith(
                    "Unsigned decoding requires"));
            Assert.That(
                () => decoder.DecodeSparseIndices(new byte[3], misaligned, 2),
                Throws.ArgumentException.With.Message.StartsWith(
                    "Sparse indices are not aligned"));
        });
    }

    [Test]
    public void SparseLayouts_RejectStrideAndTargetOnPayloadViews()
    {
        AccessorByteRange stridedIndices = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            4);
        AccessorByteRange targetedIndices = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        targetedIndices.BufferView!.HasTarget = true;
        targetedIndices.BufferView.Target = GltfBufferViewTarget.ArrayBuffer;
        AccessorByteRange values = CreateRange(
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        values.BufferView!.HasByteStride = true;
        values.BufferView.ByteStride = 4;
        SparseAccessorDecoder decoder = new SparseAccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.ValidateSparseIndicesLayout(stridedIndices, 1),
                Throws.ArgumentException.With.Message.Contains("byteStride"));
            Assert.That(
                () => decoder.ValidateSparseIndicesLayout(targetedIndices, 1),
                Throws.ArgumentException.With.Message.Contains("target"));
            Assert.That(
                () => decoder.ValidateSparseValuesLayout(values, 1, 1),
                Throws.ArgumentException.With.Message.Contains("byteStride"));
        });
    }

    [TestCase(0, 3)]
    [TestCase(4, 3)]
    public void SparseLayouts_RejectInvalidSparseCounts(int sparseCount, int accessorCount)
    {
        AccessorByteRange range = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            sparseCount,
            0,
            0,
            0);

        Assert.That(
            () => new SparseAccessorDecoder().ValidateSparseIndicesLayout(range, accessorCount),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void OverlayRawElements_RejectsTruncatedValuesAndMismatchedBaseLayout()
    {
        AccessorByteRange indices = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        AccessorByteRange values = CreateRange(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            1,
            0,
            0,
            0);
        AccessorByteRange mismatchedBase = CreateRange(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            2,
            0,
            0,
            0);
        SparseAccessorDecoder decoder = new SparseAccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.OverlayRawElements(
                    [], null, new byte[] { 0 }, indices, new byte[7], values, 2),
                Throws.ArgumentException.With.Message.StartsWith(
                    "The accessor byte range is outside"));
            Assert.That(
                () => decoder.OverlayRawElements(
                    new byte[24],
                    mismatchedBase,
                    new byte[] { 0 },
                    indices,
                    new byte[8],
                    values,
                    2),
                Throws.ArgumentException.With.Message.StartsWith(
                    "The base and sparse value element layouts do not match"));
        });
    }

    [Test]
    public void CalculateSparseRanges_UsesSparseTypesCountsAndOffsets()
    {
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 8;
        accessor.Type = GltfAccessorType.Vector3;
        GltfAccessorSparse sparse = new GltfAccessorSparse();
        sparse.Count = 2;
        sparse.Indices.BufferView = 1;
        sparse.Indices.HasByteOffset = true;
        sparse.Indices.ByteOffset = 2;
        sparse.Indices.ComponentType = GltfComponentType.UnsignedShort;
        sparse.Values.BufferView = 2;
        sparse.Values.HasByteOffset = true;
        sparse.Values.ByteOffset = 4;
        BufferViewByteRange indicesView = CreateBufferViewRange(10, 20);
        BufferViewByteRange valuesView = CreateBufferViewRange(30, 40);
        SparseAccessorDecoder decoder = new SparseAccessorDecoder();

        AccessorByteRange indices = decoder.CalculateSparseIndicesRange(sparse, indicesView);
        AccessorByteRange values = decoder.CalculateSparseValuesRange(accessor, sparse, valuesView);

        Assert.Multiple(() =>
        {
            Assert.That(indices.Count, Is.EqualTo(2));
            Assert.That(indices.ElementLayout.ComponentType, Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(indices.FirstElementBufferByteOffset, Is.EqualTo(12));
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.ElementLayout.Type, Is.EqualTo(GltfAccessorType.Vector3));
            Assert.That(values.FirstElementBufferByteOffset, Is.EqualTo(34));
            Assert.That(values.BufferByteEnd, Is.EqualTo(58));
        });
    }

    private static AccessorByteRange CreateRange(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        int bufferViewByteOffset,
        int accessorByteOffset,
        int byteStride)
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 0;
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = bufferViewByteOffset;
        bufferView.ByteLength = 1024;
        if (byteStride > 0)
        {
            bufferView.HasByteStride = true;
            bufferView.ByteStride = byteStride;
        }

        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.HasByteOffset = true;
        accessor.ByteOffset = accessorByteOffset;
        accessor.ComponentType = componentType;
        accessor.Count = count;
        accessor.Type = accessorType;
        AccessorLayout layout = new AccessorLayout();
        return layout.CalculateAccessorByteRange(
            accessor,
            layout.CalculateBufferViewRange(bufferView));
    }

    private static BufferViewByteRange CreateBufferViewRange(int byteOffset, int byteLength)
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 0;
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = byteOffset;
        bufferView.ByteLength = byteLength;
        return new AccessorLayout().CalculateBufferViewRange(bufferView);
    }
}
