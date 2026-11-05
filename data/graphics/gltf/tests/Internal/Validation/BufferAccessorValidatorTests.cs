using System.Buffers.Binary;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class BufferAccessorValidatorTests
{
    [Test]
    public void ValidateBufferAccessorRules_AcceptsDenseMatrixAndSparseLayouts()
    {
        GltfDocument dense = new GltfDocument();
        byte[] denseBytes = new byte[8];
        WriteFloat(denseBytes, 0, -2.0F);
        WriteFloat(denseBytes, 4, 4.0F);
        dense.Buffers.Add(CreateResolvedBuffer(denseBytes));
        dense.BufferViews.Add(CreateBufferView(0, 0, 8));
        GltfAccessor denseAccessor = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            2);
        denseAccessor.Minimum = new double[] { -2.0 };
        denseAccessor.Maximum = new double[] { 4.0 };
        dense.Accessors.Add(denseAccessor);

        GltfDocument matrix = new GltfDocument();
        matrix.Buffers.Add(CreateResolvedBuffer(new byte[12]));
        matrix.BufferViews.Add(CreateBufferView(0, 0, 12));
        matrix.Accessors.Add(CreateAccessor(
            0,
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix3,
            1));

        GltfDocument sparse = CreateValidSparseDocument();

        GltfValidationReport denseReport = Validate(dense);
        GltfValidationReport matrixReport = Validate(matrix);
        GltfValidationReport sparseReport = Validate(sparse);

        Assert.Multiple(() =>
        {
            Assert.That(denseReport.IsValid, Is.True);
            Assert.That(denseReport.Issues, Is.Empty);
            Assert.That(matrixReport.IsValid, Is.True);
            Assert.That(matrixReport.Issues, Is.Empty);
            Assert.That(sparseReport.IsValid, Is.True);
            Assert.That(sparseReport.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsResourceViewAndGlbLengths()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer shortBuffer = CreateResolvedBuffer(new byte[4]);
        shortBuffer.ByteLength = 8;
        document.Buffers.Add(shortBuffer);
        GltfBuffer external = new GltfBuffer();
        external.ByteLength = 4;
        external.Uri = "data.bin";
        document.Buffers.Add(external);
        GltfBufferView view = CreateBufferView(0, 6, 4);
        view.HasByteStride = true;
        view.ByteStride = 300;
        view.HasTarget = true;
        view.Target = (GltfBufferViewTarget)123;
        document.BufferViews.Add(view);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.BufferByteLengthMismatch, "/buffers/0/byteLength");
            AssertIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                GltfValidationSeverity.Information,
                "/buffers/1");
            AssertIssue(report, GltfIssueCodes.BufferViewTooLong, "/bufferViews/0");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewInvalidByteStride,
                "/bufferViews/0/byteStride");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTooBigByteStride,
                "/bufferViews/0/byteStride");
            AssertIssue(report, GltfIssueCodes.InvalidGlValue, "/bufferViews/0/target");
        });

        GltfDocument glb = new GltfDocument();
        GltfBuffer glbBuffer = new GltfBuffer();
        glbBuffer.ByteLength = 4;
        glb.Buffers.Add(glbBuffer);
        glb.Container = new GltfContainerData();
        glb.Container.Format = GltfContainerFormat.Glb;
        glb.Container.HasBinChunk = true;
        glb.Container.BinBytes = new byte[8];

        GltfValidationReport glbReport = Validate(glb);

        AssertIssue(
            glbReport,
            GltfIssueCodes.BufferGlbChunkTooBig,
            GltfValidationSeverity.Warning,
            "/buffers/0/byteLength");
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsAccessorAlignmentStrideAndExtent()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[32]));
        GltfBufferView view = CreateBufferView(0, 1, 8);
        view.HasByteStride = true;
        view.ByteStride = 8;
        document.BufferViews.Add(view);
        GltfAccessor accessor = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1);
        accessor.HasByteOffset = true;
        accessor.ByteOffset = 1;
        document.Accessors.Add(accessor);

        GltfAccessor normalized = new GltfAccessor();
        normalized.ComponentType = GltfComponentType.Float;
        normalized.Type = GltfAccessorType.Scalar;
        normalized.Count = 1;
        normalized.HasNormalized = true;
        normalized.Normalized = true;
        document.Accessors.Add(normalized);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorOffsetAlignment,
                "/accessors/0/byteOffset");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorTotalOffsetAlignment,
                "/accessors/0");
            AssertIssue(report, GltfIssueCodes.AccessorSmallBytestride, "/accessors/0");
            AssertIssue(report, GltfIssueCodes.AccessorTooLong, "/accessors/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorNormalizedInvalid,
                "/accessors/1/normalized");
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsMatrixAlignmentAndMissingPadding()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[16]));
        document.BufferViews.Add(CreateBufferView(0, 2, 9));
        document.Accessors.Add(CreateAccessor(
            0,
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix3,
            1));

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.AccessorMatrixAlignment, "/accessors/0");
            AssertIssue(report, GltfIssueCodes.AccessorTooLong, "/accessors/0");
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsFiniteAndBoundsFailures()
    {
        GltfDocument document = new GltfDocument();
        byte[] bytes = new byte[12];
        WriteFloat(bytes, 0, float.NaN);
        WriteFloat(bytes, 4, 2.0F);
        WriteFloat(bytes, 8, 4.0F);
        document.Buffers.Add(CreateResolvedBuffer(bytes));
        document.BufferViews.Add(CreateBufferView(0, 0, 4));
        document.BufferViews.Add(CreateBufferView(0, 4, 8));

        GltfAccessor invalidFloat = CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            1);
        document.Accessors.Add(invalidFloat);
        GltfAccessor mismatchedBounds = CreateAccessor(
            1,
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            2);
        mismatchedBounds.Minimum = new double[] { 1.0 };
        mismatchedBounds.Maximum = new double[] { 5.0 };
        document.Accessors.Add(mismatchedBounds);
        GltfAccessor invalidDeclarations = new GltfAccessor();
        invalidDeclarations.ComponentType = GltfComponentType.Float;
        invalidDeclarations.Type = GltfAccessorType.Vector2;
        invalidDeclarations.Count = 1;
        invalidDeclarations.Minimum = new double[] { double.NegativeInfinity };
        invalidDeclarations.Maximum = new double[] { 0.0, 1.0, 2.0 };
        document.Accessors.Add(invalidDeclarations);
        GltfAccessor nonFiniteDeclarations = new GltfAccessor();
        nonFiniteDeclarations.ComponentType = GltfComponentType.Float;
        nonFiniteDeclarations.Type = GltfAccessorType.Scalar;
        nonFiniteDeclarations.Count = 1;
        nonFiniteDeclarations.Minimum = new double[] { double.NaN };
        nonFiniteDeclarations.Maximum = new double[] { double.PositiveInfinity };
        document.Accessors.Add(nonFiniteDeclarations);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.AccessorInvalidFloat, "/accessors/0");
            AssertIssue(report, GltfIssueCodes.AccessorMinMismatch, "/accessors/1/min");
            AssertIssue(report, GltfIssueCodes.AccessorMaxMismatch, "/accessors/1/max");
            AssertIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                "/accessors/2/min");
            AssertIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                "/accessors/2/max");
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/accessors/3/min/0");
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/accessors/3/max/0");
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_RejectsIntegerBoundsOutsideStoredRepresentation()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[] { 2 }));
        document.BufferViews.Add(CreateBufferView(0, 0, 1));
        GltfAccessor accessor = CreateAccessor(
            0,
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            1);
        accessor.Minimum = new double[] { 1.5 };
        accessor.Maximum = new double[] { 256.0 };
        document.Accessors.Add(accessor);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/accessors/0/min/0");
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/accessors/0/max/0");
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsSparseCountsIndicesAndOverlap()
    {
        GltfDocument sequence = new GltfDocument();
        byte[] bytes = new byte[] { 2, 1, 5, 0, 10, 20, 30, 0 };
        sequence.Buffers.Add(CreateResolvedBuffer(bytes));
        sequence.BufferViews.Add(CreateBufferView(0, 0, 3));
        sequence.BufferViews.Add(CreateBufferView(0, 4, 3));
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Type = GltfAccessorType.Scalar;
        accessor.Count = 5;
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Count = 3;
        accessor.Sparse.Indices.BufferView = 0;
        accessor.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Sparse.Values.BufferView = 1;
        sequence.Accessors.Add(accessor);
        GltfAccessor invalidCount = new GltfAccessor();
        invalidCount.ComponentType = GltfComponentType.UnsignedByte;
        invalidCount.Type = GltfAccessorType.Scalar;
        invalidCount.Count = 2;
        invalidCount.Sparse = new GltfAccessorSparse();
        invalidCount.Sparse.Count = 3;
        invalidCount.Sparse.Indices.BufferView = 0;
        invalidCount.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        invalidCount.Sparse.Values.BufferView = 1;
        sequence.Accessors.Add(invalidCount);

        GltfValidationReport sequenceReport = Validate(sequence);

        Assert.Multiple(() =>
        {
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseIndicesNonIncreasing,
                "/accessors/0/sparse/indices");
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseIndexOob,
                "/accessors/0/sparse/indices");
            AssertIssue(
                sequenceReport,
                GltfIssueCodes.AccessorSparseCountOutOfRange,
                "/accessors/1/sparse/count");
        });

        GltfDocument overlap = new GltfDocument();
        overlap.Buffers.Add(CreateResolvedBuffer(new byte[8]));
        overlap.BufferViews.Add(CreateBufferView(0, 0, 8));
        GltfAccessor overlappingAccessor = new GltfAccessor();
        overlappingAccessor.ComponentType = GltfComponentType.UnsignedByte;
        overlappingAccessor.Type = GltfAccessorType.Scalar;
        overlappingAccessor.Count = 2;
        overlappingAccessor.Sparse = new GltfAccessorSparse();
        overlappingAccessor.Sparse.Count = 2;
        overlappingAccessor.Sparse.Indices.BufferView = 0;
        overlappingAccessor.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        overlappingAccessor.Sparse.Values.BufferView = 0;
        overlap.Accessors.Add(overlappingAccessor);

        GltfValidationReport overlapReport = Validate(overlap);

            AssertIssue(
                overlapReport,
                GltfIssueCodes.BufferViewTargetOverride,
            "/accessors/0/sparse/values/bufferView");
    }

    [Test]
    public void ValidateBufferAccessorRules_ReportsUsageTargetsAndPhysicalOverlap()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(new byte[32]));
        document.BufferViews.Add(CreateBufferView(0, 0, 12));
        GltfBufferView indexView = CreateBufferView(0, 8, 4);
        indexView.HasTarget = true;
        indexView.Target = GltfBufferViewTarget.ArrayBuffer;
        document.BufferViews.Add(indexView);
        document.BufferViews.Add(CreateBufferView(0, 8, 4));
        document.Accessors.Add(CreateAccessor(
            0,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1));
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
        primitive.Attributes.Add(CreateAttribute("POSITION", 0));
        primitive.Attributes.Add(CreateAttribute("NORMAL", 1));
        primitive.HasIndices = true;
        primitive.Indices = 2;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 2;
        document.Images.Add(image);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveAccessorWithoutBytestride,
                "/bufferViews/0");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTargetMissing,
                GltfValidationSeverity.Information,
                "/bufferViews/0");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                "/bufferViews/1/target");
            AssertIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                "/bufferViews/1",
                "/bufferViews/0");
        });
    }

    [Test]
    public void ValidateBufferAccessorRules_RejectsNullInput()
    {
        Assert.That(
            () => new BufferAccessorValidator().ValidateBufferAccessorRules(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfDocument CreateValidSparseDocument()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(CreateResolvedBuffer(
            new byte[] { 1, 3, 0, 0, 10, 20, 30, 40 }));
        document.BufferViews.Add(CreateBufferView(0, 0, 2));
        document.BufferViews.Add(CreateBufferView(0, 4, 4));
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Type = GltfAccessorType.Vector2;
        accessor.Count = 4;
        accessor.Minimum = new double[] { 0.0, 0.0 };
        accessor.Maximum = new double[] { 30.0, 40.0 };
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Count = 2;
        accessor.Sparse.Indices.BufferView = 0;
        accessor.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Sparse.Values.BufferView = 1;
        document.Accessors.Add(accessor);
        return document;
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

    private static GltfValidationReport Validate(GltfDocument document)
    {
        return new BufferAccessorValidator().ValidateBufferAccessorRules(document);
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {code} at '{pointer}', got: "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Code}@{issue.JsonPointer}")));
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
            $"Expected {severity} {code} at '{pointer}', got: "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}")));
    }

    private static void AssertIssue(
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
            $"Expected {code} at '{pointer}' related to '{relatedPointer}'.");
    }
}
