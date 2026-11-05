using System.Buffers.Binary;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfBufferAccessorSemanticValidationTests
{
    [Test]
    public void ValidateDocument_ReportsResolvedExternalAndGlbBufferStates()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer shortBuffer = CreateResolvedBuffer(new byte[4]);
        shortBuffer.ByteLength = 8;
        document.Buffers.Add(shortBuffer);
        GltfBuffer missingBytes = new GltfBuffer();
        missingBytes.ByteLength = 4;
        missingBytes.Resolved = true;
        missingBytes.ResolvedBytes = null!;
        document.Buffers.Add(missingBytes);
        GltfBuffer external = new GltfBuffer();
        external.ByteLength = 4;
        external.Uri = "mesh.bin";
        document.Buffers.Add(external);
        GltfBuffer missingGlb = new GltfBuffer();
        missingGlb.ByteLength = 4;
        document.Buffers.Add(missingGlb);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.BufferByteLengthMismatch,
                GltfValidationSeverity.Error,
                "/buffers/0/byteLength");
            AssertIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                GltfValidationSeverity.Error,
                "/buffers/1");
            AssertIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                GltfValidationSeverity.Information,
                "/buffers/2");
            AssertIssue(
                report,
                GltfIssueCodes.BufferMissingGlbData,
                GltfValidationSeverity.Error,
                "/buffers/3");
        });

        GltfDocument glb = new GltfDocument();
        GltfBuffer glbBuffer = new GltfBuffer();
        glbBuffer.ByteLength = 4;
        glb.Buffers.Add(glbBuffer);
        glb.Container = new GltfContainerData();
        glb.Container.Format = GltfContainerFormat.Glb;
        glb.Container.HasBinChunk = true;
        glb.Container.BinBytes = new byte[8];

        GltfValidationReport glbReport = Validate(glb, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                glbReport,
                GltfIssueCodes.ResourceUnavailable,
                GltfValidationSeverity.Warning,
                "/buffers/0");
            AssertIssue(
                glbReport,
                GltfIssueCodes.BufferGlbChunkTooBig,
                GltfValidationSeverity.Warning,
                "/buffers/0/byteLength");
        });
    }

    [Test]
    public void ValidateDocument_ReportsViewTargetsSharedStrideAndPhysicalOverlap()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[32]));
        document.BufferViews.Add(CreateBufferView(0, 0, 12));
        GltfBufferView indexView = CreateBufferView(0, 8, 4);
        indexView.HasTarget = true;
        indexView.Target = GltfBufferViewTarget.ArrayBuffer;
        document.BufferViews.Add(indexView);

        GltfAccessor position = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1);
        position.Minimum = new double[] { 0.0, 0.0, 0.0 };
        position.Maximum = new double[] { 0.0, 0.0, 0.0 };
        document.Accessors.Add(position);
        document.Accessors.Add(CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1));
        document.Accessors.Add(CreateAccessor(
            1,
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            2));

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(CreateAttribute(GltfSemantics.Position, 0));
        primitive.Attributes.Add(CreateAttribute(GltfSemantics.Normal, 1));
        primitive.HasIndices = true;
        primitive.Indices = 2;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveAccessorWithoutBytestride,
                GltfValidationSeverity.Error,
                "/bufferViews/0");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTargetMissing,
                GltfValidationSeverity.Information,
                "/bufferViews/0");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                GltfValidationSeverity.Error,
                "/bufferViews/1/target");
            AssertRelatedIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                "/bufferViews/1",
                "/bufferViews/0");
        });
    }

    [Test]
    public void ValidateDocument_ReportsAccessorNormalizationAndAlignmentRules()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[32]));
        GltfBufferView view = CreateBufferView(0, 1, 8);
        view.HasByteStride = true;
        view.ByteStride = 8;
        document.BufferViews.Add(view);
        GltfAccessor misaligned = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1);
        misaligned.HasByteOffset = true;
        misaligned.ByteOffset = 1;
        document.Accessors.Add(misaligned);

        GltfAccessor invalidNormalized = new GltfAccessor();
        invalidNormalized.ComponentType = GltfComponentType.Float;
        invalidNormalized.Type = GltfAccessorType.Scalar;
        invalidNormalized.Count = 1;
        invalidNormalized.HasNormalized = true;
        invalidNormalized.Normalized = true;
        document.Accessors.Add(invalidNormalized);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorOffsetAlignment,
                GltfValidationSeverity.Error,
                "/accessors/0/byteOffset");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorTotalOffsetAlignment,
                GltfValidationSeverity.Error,
                "/accessors/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorSmallBytestride,
                GltfValidationSeverity.Error,
                "/accessors/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorTooLong,
                GltfValidationSeverity.Error,
                "/accessors/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorNormalizedInvalid,
                GltfValidationSeverity.Error,
                "/accessors/1/normalized");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsEveryNormalizedIntegerRange()
    {
        byte[] bytes = new byte[12];
        bytes[0] = 0x80;
        bytes[1] = 0x7F;
        bytes[2] = 0;
        bytes[3] = 255;
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(4, 2), short.MinValue);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(6, 2), short.MaxValue);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8, 2), ushort.MinValue);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10, 2), ushort.MaxValue);

        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(bytes));
        AddNormalizedAccessor(
            document,
            0,
            2,
            GltfComponentType.SignedByte,
            -128.0,
            127.0);
        AddNormalizedAccessor(
            document,
            2,
            2,
            GltfComponentType.UnsignedByte,
            0.0,
            255.0);
        AddNormalizedAccessor(
            document,
            4,
            4,
            GltfComponentType.SignedShort,
            -32768.0,
            32767.0);
        AddNormalizedAccessor(
            document,
            8,
            4,
            GltfComponentType.UnsignedShort,
            0.0,
            65535.0);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True, FormatIssues(report));
            AssertNoIssue(report, GltfIssueCodes.AccessorNormalizedInvalid);
            AssertNoIssue(report, GltfIssueCodes.AccessorMinMismatch);
            AssertNoIssue(report, GltfIssueCodes.AccessorMaxMismatch);
        });
    }

    [Test]
    public void ValidateDocument_ReportsNonFiniteStoredValuesAndInvalidBounds()
    {
        byte[] bytes = new byte[12];
        WriteFloat(bytes, 0, float.NaN);
        WriteFloat(bytes, 4, 2.0F);
        WriteFloat(bytes, 8, 4.0F);
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(bytes));
        document.BufferViews.Add(CreateBufferView(0, 0, 4));
        document.BufferViews.Add(CreateBufferView(0, 4, 8));
        document.Accessors.Add(CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            1));
        GltfAccessor mismatched = CreateAccessor(
            1,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            2);
        mismatched.Minimum = new double[] { 1.0 };
        mismatched.Maximum = new double[] { 5.0 };
        document.Accessors.Add(mismatched);
        GltfAccessor reversed = new GltfAccessor();
        reversed.ComponentType = GltfComponentType.Float;
        reversed.Type = GltfAccessorType.Scalar;
        reversed.Count = 1;
        reversed.Minimum = new double[] { 2.0 };
        reversed.Maximum = new double[] { 1.0 };
        document.Accessors.Add(reversed);
        GltfAccessor outOfRange = new GltfAccessor();
        outOfRange.ComponentType = GltfComponentType.UnsignedByte;
        outOfRange.Type = GltfAccessorType.Scalar;
        outOfRange.Count = 1;
        outOfRange.Minimum = new double[] { -1.0 };
        outOfRange.Maximum = new double[] { 256.0 };
        document.Accessors.Add(outOfRange);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorInvalidFloat,
                GltfValidationSeverity.Error,
                "/accessors/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorMinMismatch,
                GltfValidationSeverity.Error,
                "/accessors/1/min");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorMaxMismatch,
                GltfValidationSeverity.Error,
                "/accessors/1/max");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/accessors/2/min/0");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/accessors/3/min/0");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/accessors/3/max/0");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsValidSparseDataAndReportsInvalidSparsePayloads()
    {
        GltfValidationReport validReport = Validate(CreateValidSparseDocument(), false);

        GltfDocument sequence = new GltfDocument();
        sequence.Buffers.Add(CreateResolvedBuffer(
            new byte[] { 2, 1, 5, 0, 10, 20, 30, 0 }));
        sequence.BufferViews.Add(CreateBufferView(0, 0, 3));
        sequence.BufferViews.Add(CreateBufferView(0, 4, 3));
        GltfAccessor invalidIndices = CreateSparseAccessor(5, 3, 0, 1);
        sequence.Accessors.Add(invalidIndices);
        GltfAccessor invalidCount = CreateSparseAccessor(2, 3, 0, 1);
        sequence.Accessors.Add(invalidCount);

        GltfValidationReport sequenceReport = Validate(sequence, false);

        GltfDocument overlap = new GltfDocument();
        overlap.Buffers.Add(CreateResolvedBuffer(new byte[8]));
        overlap.BufferViews.Add(CreateBufferView(0, 0, 8));
        overlap.Accessors.Add(CreateSparseAccessor(2, 2, 0, 0));

        GltfValidationReport overlapReport = Validate(overlap, false);

        Assert.Multiple(() =>
        {
            Assert.That(validReport.IsValid, Is.True, FormatIssues(validReport));
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseIndicesNonIncreasing,
                GltfValidationSeverity.Error,
                "/accessors/0/sparse/indices");
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseIndexOob,
                GltfValidationSeverity.Error,
                "/accessors/0/sparse/indices");
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseCountOutOfRange,
                GltfValidationSeverity.Error,
                "/accessors/1/sparse/count");
            AssertRelatedIssue(
                overlapReport,
                GltfIssueCodes.BufferViewTargetOverride,
                "/accessors/0/sparse/values/bufferView",
                "/accessors/0/sparse/indices/bufferView");
        });
    }

    [Test]
    public void ValidateDocument_DefersValueChecksForUnresolvedAndCompressionOnlyStorage()
    {
        GltfDocument unresolved = new GltfDocument();
        GltfBuffer external = new GltfBuffer();
        external.ByteLength = 4;
        external.Uri = "values.bin";
        unresolved.Buffers.Add(external);
        unresolved.BufferViews.Add(CreateBufferView(0, 0, 4));
        GltfAccessor unresolvedAccessor = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            1);
        unresolvedAccessor.Minimum = new double[] { 1.0 };
        unresolvedAccessor.Maximum = new double[] { 2.0 };
        unresolved.Accessors.Add(unresolvedAccessor);

        GltfValidationReport unresolvedReport = Validate(unresolved, false);

        GltfDocument compressed = CreateCompressionOnlyDocument();
        GltfValidationReport compressedReport = Validate(compressed, true);

        Assert.Multiple(() =>
        {
            AssertIssue(
                unresolvedReport,
                GltfIssueCodes.ResourceUnavailable,
                GltfValidationSeverity.Information,
                "/buffers/0");
            AssertNoStoredValueIssue(unresolvedReport);
            Assert.That(compressedReport.IsValid, Is.True, FormatIssues(compressedReport));
            AssertNoStoredValueIssue(compressedReport);
        });
    }

    private static GltfDocument CreateValidSparseDocument()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(
            new byte[] { 1, 3, 0, 0, 10, 20, 30, 40 }));
        document.BufferViews.Add(CreateBufferView(0, 0, 2));
        document.BufferViews.Add(CreateBufferView(0, 4, 4));
        GltfAccessor accessor = CreateSparseAccessor(4, 2, 0, 1);
        accessor.Type = GltfAccessorType.Vector2;
        accessor.Minimum = new double[] { 0.0, 0.0 };
        accessor.Maximum = new double[] { 30.0, 40.0 };
        document.Accessors.Add(accessor);
        return document;
    }

    private static GltfAccessor CreateSparseAccessor(
        int count,
        int sparseCount,
        int indicesView,
        int valuesView)
    {
        GltfAccessor result = new GltfAccessor();
        result.ComponentType = GltfComponentType.UnsignedByte;
        result.Type = GltfAccessorType.Scalar;
        result.Count = count;
        result.Sparse = new GltfAccessorSparse();
        result.Sparse.Count = sparseCount;
        result.Sparse.Indices.BufferView = indicesView;
        result.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        result.Sparse.Values.BufferView = valuesView;
        return result;
    }

    private static GltfDocument CreateCompressionOnlyDocument()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrDracoMeshCompression);
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrDracoMeshCompression);
        document.Buffers.Add(CreateResolvedBuffer(new byte[4]));
        document.BufferViews.Add(CreateBufferView(0, 0, 4));

        GltfAccessor position = new GltfAccessor();
        position.ComponentType = GltfComponentType.Float;
        position.Type = GltfAccessorType.Vector3;
        position.Count = 3;
        position.Minimum = new double[] { 0.0, 0.0, 0.0 };
        position.Maximum = new double[] { 1.0, 1.0, 1.0 };
        document.Accessors.Add(position);

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(CreateAttribute(GltfSemantics.Position, 0));
        GltfDracoMeshCompressionExtension draco =
            new GltfDracoMeshCompressionExtension();
        draco.BufferView = 0;
        draco.Attributes[GltfSemantics.Position] = 0;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.DracoMeshCompression = draco;
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = typed;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static void AddNormalizedAccessor(
        GltfDocument document,
        int byteOffset,
        int byteLength,
        GltfComponentType componentType,
        double minimum,
        double maximum)
    {
        int bufferView = document.BufferViews.Count;
        document.BufferViews.Add(CreateBufferView(0, byteOffset, byteLength));
        GltfAccessor accessor = CreateAccessor(
            bufferView,
            componentType,
            GltfAccessorType.Scalar,
            2);
        accessor.HasNormalized = true;
        accessor.Normalized = true;
        accessor.Minimum = new double[] { minimum };
        accessor.Maximum = new double[] { maximum };
        document.Accessors.Add(accessor);
    }

    private static GltfBuffer CreateResolvedBuffer(byte[] bytes)
    {
        GltfBuffer result = new GltfBuffer();
        result.ByteLength = bytes.Length;
        result.Resolved = true;
        result.ResolvedBytes = bytes;
        return result;
    }

    private static GltfBufferView CreateBufferView(
        int buffer,
        int byteOffset,
        int byteLength)
    {
        GltfBufferView result = new GltfBufferView();
        result.Buffer = buffer;
        if (byteOffset != 0)
        {
            result.HasByteOffset = true;
            result.ByteOffset = byteOffset;
        }

        result.ByteLength = byteLength;
        return result;
    }

    private static GltfAccessor CreateAccessor(
        int bufferView,
        GltfComponentType componentType,
        GltfAccessorType type,
        int count)
    {
        GltfAccessor result = new GltfAccessor();
        result.HasBufferView = true;
        result.BufferView = bufferView;
        result.ComponentType = componentType;
        result.Type = type;
        result.Count = count;
        return result;
    }

    private static GltfAttribute CreateAttribute(string semantic, int accessor)
    {
        GltfAttribute result = new GltfAttribute();
        result.Semantic = semantic;
        result.Accessor = accessor;
        return result;
    }

    private static void WriteFloat(byte[] bytes, int offset, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset, 4), value);
    }

    private static GltfValidationReport Validate(
        GltfDocument document,
        bool validateExtensions)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateResources = false;
        options.ValidateExtensions = validateExtensions;
        return new GltfValidator().ValidateDocument(document, options);
    }

    private static void AssertNoStoredValueIssue(GltfValidationReport report)
    {
        AssertNoIssue(report, GltfIssueCodes.AccessorInvalidFloat);
        AssertNoIssue(report, GltfIssueCodes.AccessorMinMismatch);
        AssertNoIssue(report, GltfIssueCodes.AccessorMaxMismatch);
    }

    private static void AssertNoIssue(GltfValidationReport report, string code)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code),
            Is.False,
            FormatIssues(report));
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code
                && issue.Severity == severity
                && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {severity} {code} at '{pointer}', got: {FormatIssues(report)}");
    }

    private static void AssertRelatedIssue(
        GltfValidationReport report,
        string code,
        string pointer,
        string relatedPointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code
                && issue.JsonPointer == pointer
                && issue.RelatedJsonPointers.Contains(relatedPointer)),
            Is.True,
            $"Expected {code} at '{pointer}' related to '{relatedPointer}', got: "
                + FormatIssues(report));
    }

    private static string FormatIssues(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(
                issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
