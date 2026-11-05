using System.Buffers.Binary;

namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorSparseConverterTests
{
    [Test]
    public void GenerateSparseAccessor_CreatesAlignedViewsAndDeterministicPayloads()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            5,
            new byte[] { 0, 0, 7, 0, 0, 0, 9, 0, 0, 0 },
            0);
        AccessorSparseConverter converter = new AccessorSparseConverter();

        AccessorSparseConversionResult result = converter.GenerateSparseAccessor(
            document,
            0,
            0.0);

        GltfAccessor accessor = document.Accessors[0];
        GltfAccessorSparse sparse = accessor.Sparse!;
        GltfBuffer generatedBuffer = document.Buffers[result.Buffer];
        GltfBufferView indicesView = document.BufferViews[result.SparseIndicesBufferView];
        GltfBufferView valuesView = document.BufferViews[result.SparseValuesBufferView];
        Assert.Multiple(() =>
        {
            Assert.That(result.Changed, Is.True);
            Assert.That(result.SparseCount, Is.EqualTo(2));
            Assert.That(accessor.HasBufferView, Is.False);
            Assert.That(accessor.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(sparse.Count, Is.EqualTo(2));
            Assert.That(sparse.Indices.ComponentType, Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(indicesView.Buffer, Is.EqualTo(result.Buffer));
            Assert.That(indicesView.HasByteOffset, Is.False);
            Assert.That(indicesView.ByteLength, Is.EqualTo(2));
            Assert.That(indicesView.HasByteStride, Is.False);
            Assert.That(indicesView.HasTarget, Is.False);
            Assert.That(valuesView.Buffer, Is.EqualTo(result.Buffer));
            Assert.That(valuesView.ByteOffset, Is.EqualTo(4));
            Assert.That(valuesView.ByteLength, Is.EqualTo(4));
            Assert.That(generatedBuffer.Resolved, Is.True);
            Assert.That(
                generatedBuffer.ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 3, 0, 0, 7, 0, 9, 0 }));
            Assert.That(document.Buffers[0].ResolvedBytes[2], Is.EqualTo(7));
            Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(document));
        });
    }

    [Test]
    public void MaterializeSparseAccessor_RestoresDenseValuesFromGeneratedSparseData()
    {
        byte[] original = { 0, 0, 7, 0, 0, 0, 9, 0, 0, 0 };
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            5,
            original,
            0);
        AccessorSparseConverter converter = new AccessorSparseConverter();
        converter.GenerateSparseAccessor(document, 0, 0.0);

        AccessorSparseConversionResult result = converter.MaterializeSparseAccessor(
            document,
            0);

        GltfAccessor accessor = document.Accessors[0];
        GltfBufferView denseView = document.BufferViews[result.DenseBufferView];
        Assert.Multiple(() =>
        {
            Assert.That(result.Changed, Is.True);
            Assert.That(accessor.Sparse, Is.Null);
            Assert.That(accessor.HasBufferView, Is.True);
            Assert.That(accessor.BufferView, Is.EqualTo(result.DenseBufferView));
            Assert.That(accessor.HasByteOffset, Is.False);
            Assert.That(denseView.Buffer, Is.EqualTo(result.Buffer));
            Assert.That(denseView.ByteLength, Is.EqualTo(original.Length));
            Assert.That(document.Buffers[result.Buffer].ResolvedBytes, Is.EqualTo(original));
            Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(document));
        });
    }

    [Test]
    public void MaterializeSparseAccessor_PacksAnInterleavedBaseAndPreservesItsTarget()
    {
        byte[] baseBytes =
        {
            1, 0, 2, 0, 0xa1, 0xa2, 0xa3, 0xa4,
            3, 0, 4, 0
        };
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            2,
            baseBytes,
            8);
        document.BufferViews[0].HasTarget = true;
        document.BufferViews[0].Target = GltfBufferViewTarget.ArrayBuffer;
        AddSparsePayload(
            document,
            new byte[] { 1, 0, 0, 0, 9, 0, 10, 0 },
            0,
            4,
            GltfComponentType.UnsignedByte);
        AccessorSparseConverter converter = new AccessorSparseConverter();

        AccessorSparseConversionResult result = converter.MaterializeSparseAccessor(
            document,
            0);

        GltfBufferView denseView = document.BufferViews[result.DenseBufferView];
        Assert.Multiple(() =>
        {
            Assert.That(
                document.Buffers[result.Buffer].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 0, 2, 0, 9, 0, 10, 0 }));
            Assert.That(denseView.HasByteStride, Is.False);
            Assert.That(denseView.HasTarget, Is.True);
            Assert.That(denseView.Target, Is.EqualTo(GltfBufferViewTarget.ArrayBuffer));
        });
    }

    [Test]
    public void GenerateSparseAccessor_AppliesToleranceAndKeepsNonFiniteFloats()
    {
        byte[] bytes = new byte[16];
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(0, 4), -0.01F);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(4, 4), 0.0101F);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(8, 4), float.NaN);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(12, 4), 0.0F);
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            4,
            bytes,
            0);

        AccessorSparseConversionResult result = new AccessorSparseConverter()
            .GenerateSparseAccessor(document, 0, 0.01);

        byte[] generated = document.Buffers[result.Buffer].ResolvedBytes;
        Assert.Multiple(() =>
        {
            Assert.That(result.SparseCount, Is.EqualTo(2));
            Assert.That(generated[0], Is.EqualTo(1));
            Assert.That(generated[1], Is.EqualTo(2));
            Assert.That(
                generated.AsSpan(4, 8).ToArray(),
                Is.EqualTo(bytes.AsSpan(4, 8).ToArray()));
        });
    }

    [Test]
    public void GenerateSparseAccessor_AppliesToleranceToNormalizedValues()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            3,
            new byte[] { 0, 1, 2 },
            0);
        document.Accessors[0].HasNormalized = true;
        document.Accessors[0].Normalized = true;

        AccessorSparseConversionResult result = new AccessorSparseConverter()
            .GenerateSparseAccessor(document, 0, 1.0 / 255.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.SparseCount, Is.EqualTo(1));
            Assert.That(document.Buffers[result.Buffer].ResolvedBytes[0], Is.EqualTo(2));
            Assert.That(document.Buffers[result.Buffer].ResolvedBytes[4], Is.EqualTo(2));
        });
    }

    [Test]
    public void GenerateSparseAccessor_IgnoresMatrixPaddingWhenTestingForZero()
    {
        byte[] elements =
        {
            0, 0, 0xfe, 0xfd, 0, 0, 0xfc, 0xfb,
            0, 0, 0xaa, 0xbb, 1, 0, 0xcc, 0xdd
        };
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix2,
            2,
            elements,
            0);

        AccessorSparseConversionResult result = new AccessorSparseConverter()
            .GenerateSparseAccessor(document, 0, 0.0);

        byte[] generated = document.Buffers[result.Buffer].ResolvedBytes;
        Assert.Multiple(() =>
        {
            Assert.That(result.SparseCount, Is.EqualTo(1));
            Assert.That(generated[0], Is.EqualTo(1));
            Assert.That(
                generated.AsSpan(4, 8).ToArray(),
                Is.EqualTo(elements.AsSpan(8, 8).ToArray()));
        });
    }

    [Test]
    public void GenerateSparseAccessor_UsesTheSmallestLegalIndexComponentType()
    {
        AccessorSparseConverter converter = new AccessorSparseConverter();

        Assert.Multiple(() =>
        {
            Assert.That(
                converter.SelectSparseIndexComponentType(255),
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(
                converter.SelectSparseIndexComponentType(256),
                Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(
                converter.SelectSparseIndexComponentType(65535),
                Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(
                converter.SelectSparseIndexComponentType(65536),
                Is.EqualTo(GltfComponentType.UnsignedInt));
            Assert.That(
                converter.EncodeSparseIndices(
                    new List<int> { 1, 299 },
                    GltfComponentType.UnsignedShort),
                Is.EqualTo(new byte[] { 1, 0, 0x2b, 0x01 }));
            Assert.That(
                () => converter.EncodeSparseIndices(
                    new List<int>(),
                    GltfComponentType.Float),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
        });
    }

    [Test]
    public void GenerateSparseAccessor_RepresentsAllZeroDataWithoutPayloadViews()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            3,
            new byte[24],
            0);
        int originalBufferCount = document.Buffers.Count;
        int originalViewCount = document.BufferViews.Count;

        AccessorSparseConversionResult result = new AccessorSparseConverter()
            .GenerateSparseAccessor(document, 0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Changed, Is.True);
            Assert.That(result.SparseCount, Is.Zero);
            Assert.That(document.Buffers, Has.Count.EqualTo(originalBufferCount));
            Assert.That(document.BufferViews, Has.Count.EqualTo(originalViewCount));
            Assert.That(document.Accessors[0].HasBufferView, Is.False);
            Assert.That(document.Accessors[0].Sparse, Is.Null);
        });
    }

    [Test]
    public void GenerateSparseAccessor_FailureDoesNotPartiallyMutateTheDocument()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            2,
            new byte[8],
            0);
        GltfAccessor accessor = document.Accessors[0];

        Assert.That(
            () => new AccessorSparseConverter().GenerateSparseAccessor(document, 0, 0.0),
            Throws.ArgumentException);
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(accessor.HasBufferView, Is.True);
            Assert.That(accessor.BufferView, Is.Zero);
            Assert.That(accessor.Sparse, Is.Null);
        });
    }

    [TestCase(-0.001)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void GenerateSparseAccessor_RejectsInvalidToleranceWithoutMutation(double tolerance)
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1,
            new byte[] { 1 },
            0);

        Assert.That(
            () => new AccessorSparseConverter().GenerateSparseAccessor(document, 0, tolerance),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Accessors[0].HasBufferView, Is.True);
        });
    }

    private static GltfDocument CreateDenseDocument(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        byte[] bytes,
        int byteStride)
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = bytes.Length;
        if (byteStride > 0)
        {
            view.HasByteStride = true;
            view.ByteStride = byteStride;
        }

        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = componentType;
        accessor.Count = count;
        accessor.Type = accessorType;
        document.Accessors.Add(accessor);
        return document;
    }

    private static void AddSparsePayload(
        GltfDocument document,
        byte[] bytes,
        int indicesOffset,
        int valuesOffset,
        GltfComponentType indexComponentType)
    {
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        int bufferIndex = document.Buffers.Count;
        document.Buffers.Add(buffer);
        GltfBufferView indicesView = new GltfBufferView();
        indicesView.Buffer = bufferIndex;
        indicesView.HasByteOffset = true;
        indicesView.ByteOffset = indicesOffset;
        indicesView.ByteLength = 1;
        int indicesViewIndex = document.BufferViews.Count;
        document.BufferViews.Add(indicesView);
        GltfBufferView valuesView = new GltfBufferView();
        valuesView.Buffer = bufferIndex;
        valuesView.HasByteOffset = true;
        valuesView.ByteOffset = valuesOffset;
        valuesView.ByteLength = bytes.Length - valuesOffset;
        int valuesViewIndex = document.BufferViews.Count;
        document.BufferViews.Add(valuesView);
        GltfAccessorSparse sparse = new GltfAccessorSparse();
        sparse.Count = 1;
        sparse.Indices.BufferView = indicesViewIndex;
        sparse.Indices.ComponentType = indexComponentType;
        sparse.Values.BufferView = valuesViewIndex;
        document.Accessors[0].Sparse = sparse;
    }
}
