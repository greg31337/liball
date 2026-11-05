using System.Buffers.Binary;
using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfSparseAccessorCoverageTests
{
    [TestCase(GltfComponentType.UnsignedByte, false)]
    [TestCase(GltfComponentType.UnsignedByte, true)]
    [TestCase(GltfComponentType.UnsignedShort, false)]
    [TestCase(GltfComponentType.UnsignedShort, true)]
    [TestCase(GltfComponentType.UnsignedInt, false)]
    [TestCase(GltfComponentType.UnsignedInt, true)]
    public void ReadFloatingValues_OverlaysBoundariesForEverySparseIndexType(
        GltfComponentType indexType,
        bool hasBase)
    {
        GltfDocument document = CreateSparseDocument(
            indexType,
            hasBase,
            new uint[] { 0U, 3U });
        GltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorReadRequest withSparse = CreateReadRequest(document, true);
        GltfAccessorReadRequest withoutSparse = CreateReadRequest(document, false);

        GltfAccessorData overlaid = codec.ReadFloatingValues(withSparse);
        GltfAccessorData baseOnly = codec.ReadFloatingValues(withoutSparse);

        double[] expectedBase = hasBase
            ? new double[] { 10.0, 20.0, 30.0, 40.0 }
            : new double[] { 0.0, 0.0, 0.0, 0.0 };
        double[] expectedOverlay = hasBase
            ? new double[] { 1.5, 20.0, 30.0, -2.5 }
            : new double[] { 1.5, 0.0, 0.0, -2.5 };
        Assert.Multiple(() =>
        {
            Assert.That(overlaid.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(overlaid.FloatingValues, Is.EqualTo(expectedOverlay));
            Assert.That(baseOnly.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(baseOnly.FloatingValues, Is.EqualTo(expectedBase));
        });
    }

    [TestCase(1U, 1U)]
    [TestCase(2U, 1U)]
    [TestCase(1U, 4U)]
    public void ReadFloatingValues_RejectsDuplicateDecreasingAndOutOfRangeSparseIndices(
        uint first,
        uint second)
    {
        GltfDocument document = CreateSparseDocument(
            GltfComponentType.UnsignedByte,
            false,
            new uint[] { first, second });

        GltfAccessorData result = new GltfAccessorCodec().ReadFloatingValues(
            CreateReadRequest(document, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(GltfIssueCodes.ArrayTypeMismatch));
            Assert.That(result.Diagnostic.JsonPointer, Is.EqualTo("/accessors/0/sparse/indices"));
        });
    }

    [TestCase(256, GltfComponentType.UnsignedByte)]
    [TestCase(257, GltfComponentType.UnsignedShort)]
    [TestCase(65537, GltfComponentType.UnsignedInt)]
    public void CreateSparseAccessor_SelectsSmallestIndexTypeAndMaterializesBoundaries(
        int count,
        GltfComponentType expectedIndexType)
    {
        double[] values = new double[count];
        values[0] = 1.0;
        values[count - 1] = 2.0;
        GltfDocument document = new GltfDocument();
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.Float;
        request.Type = GltfAccessorType.Scalar;
        request.FloatingValues = values;
        request.AllowSparse = true;
        GltfAccessorCodec codec = new GltfAccessorCodec();

        GltfAccessorData created = codec.CreateSparseAccessor(request);
        GltfAccessorData sparseValues = codec.ReadFloatingValues(
            CreateReadRequest(document, true));
        GltfComponentType generatedIndexType = document.Accessors[0].Sparse!.Indices.ComponentType;
        AccessorSparseConversionResult materialized = new AccessorSparseConverter()
            .MaterializeSparseAccessor(document, created.Accessor);
        GltfAccessorData denseValues = codec.ReadFloatingValues(
            CreateReadRequest(document, true));

        Assert.Multiple(() =>
        {
            Assert.That(created.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(document.Accessors[0].Sparse, Is.Null);
            Assert.That(materialized.Changed, Is.True);
            Assert.That(sparseValues.FloatingValues[0], Is.EqualTo(1.0));
            Assert.That(sparseValues.FloatingValues[count / 2], Is.EqualTo(0.0));
            Assert.That(sparseValues.FloatingValues[count - 1], Is.EqualTo(2.0));
            Assert.That(denseValues.FloatingValues, Is.EqualTo(values));
        });

        Assert.That(
            document.BufferViews[materialized.DenseBufferView].ByteLength,
            Is.EqualTo(count * 4));
        Assert.That(document.Accessors[0].ComponentType, Is.EqualTo(GltfComponentType.Float));
        Assert.That(created.Count, Is.EqualTo(count));
        Assert.That(generatedIndexType, Is.EqualTo(expectedIndexType));
    }

    [TestCase(GltfComponentType.UnsignedByte)]
    [TestCase(GltfComponentType.UnsignedShort)]
    [TestCase(GltfComponentType.UnsignedInt)]
    public void GlbRoundTrip_PreservesSparseBinaryPayloadAndBoundaryValues(
        GltfComponentType indexType)
    {
        GltfContainerData source = CreateSparseGlbContainer(indexType);
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Glb;
        GltfWriteResult written = new GltfWriter().WriteContainerBytes(source, writeOptions);
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;

        GltfReadResult read = new GltfReader().ReadBytes(written.PrimaryBytes, readOptions);
        GltfAccessorData decoded = new GltfAccessorCodec().ReadFloatingValues(
            CreateReadRequest(read.Document!, true));

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(read.Container, Is.Not.Null);
            Assert.That(read.Container!.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(read.Document!.Accessors[0].Sparse, Is.Not.Null);
            Assert.That(
                read.Document.Accessors[0].Sparse!.Indices.ComponentType,
                Is.EqualTo(indexType));
            Assert.That(
                decoded.FloatingValues,
                Is.EqualTo(new double[] { 1.5, 0.0, 0.0, -2.5 }));
        });
    }

    private static GltfAccessorReadRequest CreateReadRequest(
        GltfDocument document,
        bool applySparse)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = 0;
        request.ApplySparseValues = applySparse;
        return request;
    }

    private static GltfDocument CreateSparseDocument(
        GltfComponentType indexType,
        bool hasBase,
        uint[] indices)
    {
        GltfDocument document = new GltfDocument();
        int baseView = GltfConstants.AbsentIndex;
        if (hasBase)
        {
            baseView = AddResolvedView(document, EncodeFloats(new float[] { 10F, 20F, 30F, 40F }));
        }

        int indicesView = AddResolvedView(document, EncodeIndices(indexType, indices));
        int valuesView = AddResolvedView(document, EncodeFloats(new float[] { 1.5F, -2.5F }));
        GltfAccessorSparse sparse = new GltfAccessorSparse();
        sparse.Count = indices.Length;
        sparse.Indices.BufferView = indicesView;
        sparse.Indices.ComponentType = indexType;
        sparse.Values.BufferView = valuesView;
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 4;
        accessor.Type = GltfAccessorType.Scalar;
        accessor.Sparse = sparse;
        if (hasBase)
        {
            accessor.HasBufferView = true;
            accessor.BufferView = baseView;
        }

        document.Accessors.Add(accessor);
        return document;
    }

    private static int AddResolvedView(GltfDocument document, byte[] bytes)
    {
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        int bufferIndex = document.Buffers.Count;
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = bufferIndex;
        view.ByteLength = bytes.Length;
        int viewIndex = document.BufferViews.Count;
        document.BufferViews.Add(view);
        return viewIndex;
    }

    private static byte[] EncodeFloats(float[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        int index = 0;
        while (index < values.Length)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(index * 4, 4), values[index]);
            index++;
        }

        return bytes;
    }

    private static byte[] EncodeIndices(GltfComponentType indexType, uint[] values)
    {
        int componentSize = GetIndexComponentSize(indexType);
        byte[] bytes = new byte[values.Length * componentSize];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * componentSize;
            if (indexType == GltfComponentType.UnsignedByte)
            {
                bytes[offset] = (byte)values[index];
            }
            else if (indexType == GltfComponentType.UnsignedShort)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(
                    bytes.AsSpan(offset, componentSize),
                    (ushort)values[index]);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(
                    bytes.AsSpan(offset, componentSize),
                    values[index]);
            }

            index++;
        }

        return bytes;
    }

    private static int GetIndexComponentSize(GltfComponentType indexType)
    {
        if (indexType == GltfComponentType.UnsignedByte)
        {
            return 1;
        }

        if (indexType == GltfComponentType.UnsignedShort)
        {
            return 2;
        }

        return 4;
    }

    private static GltfContainerData CreateSparseGlbContainer(GltfComponentType indexType)
    {
        byte[] indices = EncodeIndices(indexType, new uint[] { 0U, 3U });
        int valuesOffset = (indices.Length + 3) & ~3;
        byte[] values = EncodeFloats(new float[] { 1.5F, -2.5F });
        byte[] binary = new byte[valuesOffset + values.Length];
        indices.CopyTo(binary, 0);
        values.CopyTo(binary, valuesOffset);
        string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"byteLength\":" + binary.Length + "}],"
            + "\"bufferViews\":["
            + "{\"buffer\":0,\"byteOffset\":0,\"byteLength\":" + indices.Length + "},"
            + "{\"buffer\":0,\"byteOffset\":" + valuesOffset
            + ",\"byteLength\":" + values.Length + "}],"
            + "\"accessors\":[{\"componentType\":5126,\"count\":4,\"type\":\"SCALAR\","
            + "\"sparse\":{\"count\":2,\"indices\":{\"bufferView\":0,\"componentType\":"
            + (int)indexType + "},\"values\":{\"bufferView\":1}}}]}";
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.JsonBytes = Encoding.UTF8.GetBytes(json);
        container.HasBinChunk = true;
        container.BinBytes = binary;
        return container;
    }
}
