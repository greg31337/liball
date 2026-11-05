using System.Buffers.Binary;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfAccessorCodecTests
{
    [Test]
    public void CreateDenseAccessor_StoresOwnedValuesAndReadsFloatingAndRawForms()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorWriteRequest write = new GltfAccessorWriteRequest();
        write.Document = document;
        write.ComponentType = GltfComponentType.Float;
        write.Type = GltfAccessorType.Vector2;
        write.FloatingValues = new double[] { 1.5, -2.0, 3.25, 4.0 };
        GltfAccessorCodec codec = new GltfAccessorCodec();

        GltfAccessorData created = codec.CreateDenseAccessor(write);
        write.FloatingValues[0] = 99.0;
        GltfAccessorReadRequest read = CreateReadRequest(document, created.Accessor);
        GltfAccessorData floating = codec.ReadFloatingValues(read);
        GltfAccessorData raw = codec.ReadRawElements(read);

        Assert.Multiple(() =>
        {
            Assert.That(created.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(created.HasAccessor, Is.True);
            Assert.That(created.Count, Is.EqualTo(2));
            Assert.That(created.FloatingValues[0], Is.EqualTo(1.5));
            Assert.That(document.Accessors, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(floating.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(floating.FloatingValues, Is.EqualTo(new double[] { 1.5, -2.0, 3.25, 4.0 }));
            Assert.That(raw.RawBytes, Has.Length.EqualTo(16));
            Assert.That(BinaryPrimitives.ReadSingleLittleEndian(raw.RawBytes.AsSpan(0, 4)), Is.EqualTo(1.5F));
            Assert.That(document.Accessors[0].Minimum, Is.EqualTo(new double[] { 1.5, -2.0 }));
            Assert.That(document.Accessors[0].Maximum, Is.EqualTo(new double[] { 3.25, 4.0 }));
        });
    }

    [Test]
    public void TypedReaders_DecodeSignedUnsignedIndicesAndNormalizedMatrices()
    {
        GltfAccessorCodec codec = new GltfAccessorCodec();

        GltfDocument signedDocument = new GltfDocument();
        GltfAccessorWriteRequest signedWrite = new GltfAccessorWriteRequest();
        signedWrite.Document = signedDocument;
        signedWrite.ComponentType = GltfComponentType.SignedShort;
        signedWrite.Type = GltfAccessorType.Vector2;
        signedWrite.SignedValues = new int[] { -2, 3, 4, -5 };
        GltfAccessorData signedCreated = codec.CreateDenseAccessor(signedWrite);
        GltfAccessorData signed = codec.ReadSignedValues(
            CreateReadRequest(signedDocument, signedCreated.Accessor));

        GltfDocument unsignedDocument = new GltfDocument();
        GltfAccessorWriteRequest unsignedWrite = new GltfAccessorWriteRequest();
        unsignedWrite.Document = unsignedDocument;
        unsignedWrite.ComponentType = GltfComponentType.UnsignedShort;
        unsignedWrite.Type = GltfAccessorType.Vector2;
        unsignedWrite.UnsignedValues = new uint[] { 1U, 2U, 65535U, 4U };
        GltfAccessorData unsignedCreated = codec.CreateDenseAccessor(unsignedWrite);
        GltfAccessorData unsigned = codec.ReadUnsignedValues(
            CreateReadRequest(unsignedDocument, unsignedCreated.Accessor));

        GltfDocument indexDocument = new GltfDocument();
        GltfAccessorWriteRequest indexWrite = new GltfAccessorWriteRequest();
        indexWrite.Document = indexDocument;
        indexWrite.ComponentType = GltfComponentType.UnsignedInt;
        indexWrite.UnsignedValues = new uint[] { 0U, 2U, 70000U };
        GltfAccessorData indexCreated = codec.CreateDenseAccessor(indexWrite);
        GltfAccessorData indices = codec.ReadIndices(
            CreateReadRequest(indexDocument, indexCreated.Accessor));

        GltfDocument matrixDocument = new GltfDocument();
        GltfAccessorWriteRequest matrixWrite = new GltfAccessorWriteRequest();
        matrixWrite.Document = matrixDocument;
        matrixWrite.ComponentType = GltfComponentType.SignedByte;
        matrixWrite.Type = GltfAccessorType.Matrix2;
        matrixWrite.Normalized = true;
        matrixWrite.FloatingValues = new double[] { -1.0, 0.0, 0.5, 1.0 };
        GltfAccessorData matrixCreated = codec.CreateDenseAccessor(matrixWrite);
        GltfAccessorData matrix = codec.ReadMatrices(
            CreateReadRequest(matrixDocument, matrixCreated.Accessor));

        Assert.Multiple(() =>
        {
            Assert.That(signed.SignedValues, Is.EqualTo(new int[] { -2, 3, 4, -5 }));
            Assert.That(unsigned.UnsignedValues, Is.EqualTo(new uint[] { 1U, 2U, 65535U, 4U }));
            Assert.That(indices.Indices, Is.EqualTo(new uint[] { 0U, 2U, 70000U }));
            Assert.That(indexDocument.BufferViews[0].Target, Is.EqualTo(GltfBufferViewTarget.ElementArrayBuffer));
            Assert.That(matrix.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(matrix.Normalized, Is.True);
            Assert.That(matrix.MatrixValues[0], Is.EqualTo(-1.0));
            Assert.That(matrix.MatrixValues[1], Is.EqualTo(0.0));
            Assert.That(matrix.MatrixValues[2], Is.EqualTo(64.0 / 127.0).Within(0.000001));
            Assert.That(matrix.MatrixValues[3], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CreateSparseAccessor_GeneratesSparseOnlyStorageAndRoundTripsValues()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorWriteRequest write = new GltfAccessorWriteRequest();
        write.Document = document;
        write.ComponentType = GltfComponentType.Float;
        write.Type = GltfAccessorType.Scalar;
        write.FloatingValues = new double[] { 0.0, 5.0, 0.0, -2.0 };
        write.AllowSparse = true;
        GltfAccessorCodec codec = new GltfAccessorCodec();

        GltfAccessorData created = codec.CreateSparseAccessor(write);
        GltfAccessorData decoded = codec.ReadFloatingValues(
            CreateReadRequest(document, created.Accessor));

        Assert.Multiple(() =>
        {
            Assert.That(created.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(document.Accessors[0].HasBufferView, Is.False);
            Assert.That(document.Accessors[0].Sparse, Is.Not.Null);
            Assert.That(document.Accessors[0].Sparse!.Count, Is.EqualTo(2));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(2));
            Assert.That(decoded.FloatingValues, Is.EqualTo(new double[] { 0.0, 5.0, 0.0, -2.0 }));
        });
    }

    [Test]
    public void ReadOperations_ReportPreciseFailureCategories()
    {
        GltfAccessorCodec codec = new GltfAccessorCodec();

        GltfDocument unresolvedDocument = CreateBufferBackedScalar(false);
        GltfAccessorData unresolved = codec.ReadFloatingValues(
            CreateReadRequest(unresolvedDocument, 0));

        GltfDocument invalidDocument = CreateBufferBackedScalar(true);
        invalidDocument.BufferViews[0].HasByteStride = true;
        invalidDocument.BufferViews[0].ByteStride = 3;
        GltfAccessorData invalid = codec.ReadFloatingValues(
            CreateReadRequest(invalidDocument, 0));

        GltfDocument incompatibleDocument = CreateBufferBackedScalar(true);
        GltfAccessorData incompatible = codec.ReadIndices(
            CreateReadRequest(incompatibleDocument, 0));

        GltfDocument compressedDocument = CreateBufferBackedScalar(false);
        compressedDocument.BufferViews[0].PropertyJson.Extensions.Add(
            GltfExtensionNames.ExtMeshoptCompression,
            null);
        GltfAccessorData compressed = codec.ReadFloatingValues(
            CreateReadRequest(compressedDocument, 0));

        Assert.Multiple(() =>
        {
            AssertFailure(unresolved, GltfOperationStatus.Unavailable, GltfIssueCodes.UnresolvedReference);
            AssertFailure(invalid, GltfOperationStatus.Invalid, GltfIssueCodes.BufferViewInvalidByteStride);
            AssertFailure(incompatible, GltfOperationStatus.Invalid, GltfIssueCodes.ArrayTypeMismatch);
            AssertFailure(compressed, GltfOperationStatus.Unavailable, GltfIssueCodes.UnsupportedExtension);
        });
    }

    [Test]
    public void WriteOperations_RejectInvalidInputWithoutMutatingTheDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorWriteRequest incompatible = new GltfAccessorWriteRequest();
        incompatible.Document = document;
        incompatible.ComponentType = GltfComponentType.Float;
        incompatible.SignedValues = new int[] { 1 };
        GltfAccessorData incompatibleResult = new GltfAccessorCodec().CreateDenseAccessor(incompatible);

        GltfAccessorWriteRequest sparseDisabled = new GltfAccessorWriteRequest();
        sparseDisabled.Document = document;
        sparseDisabled.FloatingValues = new double[] { 1.0 };
        GltfAccessorData sparseResult = new GltfAccessorCodec().CreateSparseAccessor(sparseDisabled);

        Assert.Multiple(() =>
        {
            AssertFailure(incompatibleResult, GltfOperationStatus.Invalid, GltfIssueCodes.ArrayTypeMismatch);
            AssertFailure(sparseResult, GltfOperationStatus.Invalid, GltfIssueCodes.ArrayTypeMismatch);
            Assert.That(document.Accessors, Is.Empty);
            Assert.That(document.BufferViews, Is.Empty);
            Assert.That(document.Buffers, Is.Empty);
        });
    }

    private static GltfAccessorReadRequest CreateReadRequest(
        GltfDocument document,
        int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessor;
        return request;
    }

    private static GltfDocument CreateBufferBackedScalar(bool resolved)
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 4;
        buffer.Resolved = resolved;
        buffer.ResolvedBytes = new byte[4];
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 4;
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        document.Buffers.Add(buffer);
        document.BufferViews.Add(view);
        document.Accessors.Add(accessor);
        return document;
    }

    private static void AssertFailure(
        GltfAccessorData result,
        GltfOperationStatus status,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
    }
}
