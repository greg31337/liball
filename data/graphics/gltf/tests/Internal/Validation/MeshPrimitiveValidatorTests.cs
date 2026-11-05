namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class MeshPrimitiveValidatorTests
{
    [Test]
    public void ValidateDocumentMeshes_AcceptsAValidTrianglePrimitive()
    {
        GltfDocument document = CreatePrimitiveDocument(3);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [TestCase(GltfPrimitiveMode.Points, 0, true)]
    [TestCase(GltfPrimitiveMode.Points, 1, false)]
    [TestCase(GltfPrimitiveMode.Lines, 3, true)]
    [TestCase(GltfPrimitiveMode.Lines, 4, false)]
    [TestCase(GltfPrimitiveMode.LineLoop, 1, true)]
    [TestCase(GltfPrimitiveMode.LineStrip, 2, false)]
    [TestCase(GltfPrimitiveMode.Triangles, 4, true)]
    [TestCase(GltfPrimitiveMode.Triangles, 6, false)]
    [TestCase(GltfPrimitiveMode.TriangleStrip, 2, true)]
    [TestCase(GltfPrimitiveMode.TriangleFan, 3, false)]
    public void ValidateDocumentMeshes_ChecksTopologyDrawCounts(
        GltfPrimitiveMode mode,
        int count,
        bool expectsIssue)
    {
        GltfDocument document = CreatePrimitiveDocument(count);
        document.Meshes[0].Primitives[0].Mode = mode;
        document.Meshes[0].Primitives[0].HasMode = true;

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.That(
            HasIssue(report, GltfIssueCodes.MeshPrimitiveIncompatibleMode),
            Is.EqualTo(expectsIssue));
    }

    [Test]
    public void ValidateDocumentMeshes_ReportsUnequalAttributeCountsAndInvalidMaterialReference()
    {
        GltfDocument document = CreatePrimitiveDocument(3);
        GltfAccessor normal = CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            2,
            false);
        document.Accessors.Add(normal);
        AddAttribute(document.Meshes[0].Primitives[0], GltfSemantics.Normal, 1);
        document.Meshes[0].Primitives[0].HasMaterial = true;
        document.Meshes[0].Primitives[0].Material = 4;

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveUnequalAccessorCount),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.UnresolvedReference),
                Is.True);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ChecksIndexBoundsAndPrimitiveRestartValues()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorWriteRequest positions = new GltfAccessorWriteRequest();
        positions.Document = document;
        positions.Type = GltfAccessorType.Vector3;
        positions.FloatingValues = new double[]
        {
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0
        };
        GltfAccessorData positionData = codec.CreateDenseAccessor(positions);
        GltfAccessorWriteRequest indices = new GltfAccessorWriteRequest();
        indices.Document = document;
        indices.ComponentType = GltfComponentType.UnsignedByte;
        indices.UnsignedValues = new uint[] { 0U, 3U, byte.MaxValue };
        GltfAccessorData indexData = codec.CreateDenseAccessor(indices);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Mode = GltfPrimitiveMode.Points;
        primitive.HasMode = true;
        primitive.HasIndices = true;
        primitive.Indices = indexData.Accessor;
        AddAttribute(primitive, GltfSemantics.Position, positionData.Accessor);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorIndexOob), Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.AccessorIndexPrimitiveRestart),
                Is.True);
            Assert.That(
                FindIssue(report, GltfIssueCodes.AccessorIndexOob).JsonPointer,
                Is.EqualTo($"/accessors/{indexData.Accessor}"));
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ReportsInvalidIndexFormatAndStride()
    {
        GltfDocument invalidFormat = CreatePrimitiveDocument(3);
        GltfAccessor signedIndices = CreateAccessor(
            GltfAccessorType.Vector2,
            GltfComponentType.SignedShort,
            3,
            true);
        invalidFormat.Accessors.Add(signedIndices);
        invalidFormat.Meshes[0].Primitives[0].HasIndices = true;
        invalidFormat.Meshes[0].Primitives[0].Indices = 1;

        GltfDocument strided = new GltfDocument();
        GltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorWriteRequest positionWrite = new GltfAccessorWriteRequest();
        positionWrite.Document = strided;
        positionWrite.Type = GltfAccessorType.Vector3;
        positionWrite.FloatingValues = new double[9];
        GltfAccessorData positionData = codec.CreateDenseAccessor(positionWrite);
        GltfAccessorWriteRequest indexWrite = new GltfAccessorWriteRequest();
        indexWrite.Document = strided;
        indexWrite.ComponentType = GltfComponentType.UnsignedInt;
        indexWrite.UnsignedValues = new uint[] { 0U, 1U, 2U };
        GltfAccessorData indexData = codec.CreateDenseAccessor(indexWrite);
        strided.BufferViews[strided.Accessors[indexData.Accessor].BufferView].HasByteStride = true;
        strided.BufferViews[strided.Accessors[indexData.Accessor].BufferView].ByteStride = 4;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasIndices = true;
        primitive.Indices = indexData.Accessor;
        AddAttribute(primitive, GltfSemantics.Position, positionData.Accessor);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        strided.Meshes.Add(mesh);

        GltfValidationReport invalidFormatReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(invalidFormat);
        GltfValidationReport strideReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(strided);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(
                    invalidFormatReport,
                    GltfIssueCodes.MeshPrimitiveIndicesAccessorInvalidFormat),
                Is.True);
            Assert.That(
                HasIssue(
                    strideReport,
                    GltfIssueCodes.MeshPrimitiveIndicesAccessorWithBytestride),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ChecksMorphBaseCountAndSemanticShape()
    {
        GltfDocument document = CreatePrimitiveDocument(3);
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false));
        AddAttribute(document.Meshes[0].Primitives[0], GltfSemantics.Tangent, 1);
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            2,
            false));
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false));
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            3,
            false));
        GltfMorphTarget target = new GltfMorphTarget();
        AddTargetAttribute(target, GltfSemantics.Position, 2);
        AddTargetAttribute(target, GltfSemantics.Tangent, 3);
        AddTargetAttribute(target, GltfSemantics.Normal, 4);
        document.Meshes[0].Primitives[0].Targets.Add(target);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveMorphTargetInvalidAttributeCount),
                Is.True);
            Assert.That(
                HasIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveMorphTargetNoBaseAccessor),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ChecksMeshWideTargetAndWeightCounts()
    {
        GltfDocument document = CreatePrimitiveDocument(3);
        document.Meshes[0].Weights = new double[] { 0.0, 1.0 };
        GltfMorphTarget target = new GltfMorphTarget();
        AddTargetAttribute(target, GltfSemantics.Position, 0);
        document.Meshes[0].Primitives[0].Targets.Add(target);
        GltfMeshPrimitive second = new GltfMeshPrimitive();
        AddAttribute(second, GltfSemantics.Position, 0);
        document.Meshes[0].Primitives.Add(second);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitivesUnequalTargetsCount),
                Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.MeshInvalidWeightsCount), Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ValidatesSemanticFormatsAndRelationships()
    {
        GltfDocument document = new GltfDocument();
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false));
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector2,
            GltfComponentType.Float,
            3,
            false));
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedByte,
            3,
            false));
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Scalar,
            GltfComponentType.UnsignedInt,
            3,
            false));
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Mode = GltfPrimitiveMode.Points;
        AddAttribute(primitive, GltfSemantics.Tangent, 0);
        AddAttribute(primitive, "TEXCOORD_1", 1);
        AddAttribute(primitive, "JOINTS_0", 2);
        AddAttribute(primitive, "_ID", 3);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.MeshPrimitiveNoPosition), Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveTangentWithoutNormal),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveIndexedSemanticContinuity),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveAttributesAccessorUnsignedInt),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ValidatesPositionBoundsAndCoreFormats()
    {
        GltfDocument document = CreatePrimitiveDocument(3);
        document.Accessors[0].Minimum = null;
        GltfBufferView view = new GltfBufferView();
        view.HasByteOffset = true;
        view.ByteOffset = 2;
        document.BufferViews.Add(view);
        document.Accessors[0].HasBufferView = true;
        document.Accessors[0].BufferView = 0;
        document.Accessors.Add(CreateAccessor(
            GltfAccessorType.Vector2,
            GltfComponentType.Float,
            3,
            false));
        AddAttribute(document.Meshes[0].Primitives[0], GltfSemantics.Normal, 1);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(
                    report,
                    GltfIssueCodes.MeshPrimitivePositionAccessorWithoutBounds),
                Is.True);
            Assert.That(
                HasIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat),
                Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.MeshPrimitiveAccessorUnaligned),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ValidatesDecodedDirectionsColorsAndWeights()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorCodec codec = new GltfAccessorCodec();
        int position = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 0.0, 0.0 });
        int normal = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector3,
            new double[] { 2.0, 0.0, 0.0 });
        int tangent = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector4,
            new double[] { 1.0, 0.0, 0.0, 0.0 });
        int color = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector4,
            new double[] { 1.2, 0.0, 0.0, 1.0 });
        GltfAccessorWriteRequest jointWrite = new GltfAccessorWriteRequest();
        jointWrite.Document = document;
        jointWrite.Type = GltfAccessorType.Vector4;
        jointWrite.ComponentType = GltfComponentType.UnsignedByte;
        jointWrite.UnsignedValues = new uint[] { 0U, 0U, 0U, 0U };
        int joints = codec.CreateDenseAccessor(jointWrite).Accessor;
        int weights = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector4,
            new double[] { -0.1, 0.2, 0.3, 0.4 });
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Mode = GltfPrimitiveMode.Points;
        AddAttribute(primitive, GltfSemantics.Position, position);
        AddAttribute(primitive, GltfSemantics.Normal, normal);
        AddAttribute(primitive, GltfSemantics.Tangent, tangent);
        AddAttribute(primitive, "COLOR_0", color);
        AddAttribute(primitive, "JOINTS_0", joints);
        AddAttribute(primitive, "WEIGHTS_0", weights);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorVector3NonUnit), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorInvalidSign), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorNonClamped), Is.True);
            Assert.That(HasIssue(report, GltfIssueCodes.AccessorWeightsNegative), Is.True);
            Assert.That(
                HasIssue(report, GltfIssueCodes.AccessorWeightsNonNormalized),
                Is.True);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_AllowsQuantizedFormatsOnlyWhenRequired()
    {
        GltfDocument unsupported = CreatePrimitiveDocument(3);
        unsupported.Accessors[0].ComponentType = GltfComponentType.SignedShort;
        unsupported.Accessors[0].Normalized = true;
        GltfDocument supported = CreatePrimitiveDocument(3);
        supported.Accessors[0].ComponentType = GltfComponentType.SignedShort;
        supported.Accessors[0].Normalized = true;
        supported.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshQuantization);
        supported.ExtensionsRequired.Add(GltfExtensionNames.KhrMeshQuantization);

        GltfValidationReport unsupportedReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(unsupported);
        GltfValidationReport supportedReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(supported);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(
                    unsupportedReport,
                    GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat),
                Is.True);
            Assert.That(
                HasIssue(
                    supportedReport,
                    GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat),
                Is.False);
        });
    }

    [Test]
    public void ValidateDocumentMeshes_ReportsDegenerateIndexedTriangles()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessorCodec codec = new GltfAccessorCodec();
        int positions = CreateFloatingAccessor(
            codec,
            document,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        GltfAccessorWriteRequest indexWrite = new GltfAccessorWriteRequest();
        indexWrite.Document = document;
        indexWrite.ComponentType = GltfComponentType.UnsignedShort;
        indexWrite.UnsignedValues = new uint[] { 0U, 0U, 1U };
        int indices = codec.CreateDenseAccessor(indexWrite).Accessor;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasIndices = true;
        primitive.Indices = indices;
        AddAttribute(primitive, GltfSemantics.Position, positions);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MeshPrimitiveValidator().ValidateDocumentMeshes(document);

        Assert.That(
            HasIssue(report, GltfIssueCodes.AccessorIndexTriangleDegenerate),
            Is.True);
    }

    private static GltfDocument CreatePrimitiveDocument(int count)
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor position = CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            count,
            false);
        position.Minimum = new double[] { 0.0, 0.0, 0.0 };
        position.Maximum = new double[] { 1.0, 1.0, 1.0 };
        document.Accessors.Add(position);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, GltfSemantics.Position, 0);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static GltfAccessor CreateAccessor(
        GltfAccessorType type,
        GltfComponentType componentType,
        int count,
        bool normalized)
    {
        GltfAccessor accessor = new GltfAccessor();
        accessor.Type = type;
        accessor.ComponentType = componentType;
        accessor.Count = count;
        accessor.Normalized = normalized;
        accessor.HasNormalized = normalized;
        return accessor;
    }

    private static int CreateFloatingAccessor(
        GltfAccessorCodec codec,
        GltfDocument document,
        GltfAccessorType type,
        double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.Type = type;
        request.FloatingValues = values;
        return codec.CreateDenseAccessor(request).Accessor;
    }

    private static void AddAttribute(
        GltfMeshPrimitive primitive,
        string semantic,
        int accessor)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = accessor;
        primitive.Attributes.Add(attribute);
    }

    private static void AddTargetAttribute(
        GltfMorphTarget target,
        string semantic,
        int accessor)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = accessor;
        target.Attributes.Add(attribute);
    }

    private static bool HasIssue(GltfValidationReport report, string code)
    {
        int index = 0;
        while (index < report.Issues.Count)
        {
            if (report.Issues[index].Code == code)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    private static GltfValidationIssue FindIssue(GltfValidationReport report, string code)
    {
        int index = 0;
        while (index < report.Issues.Count)
        {
            if (report.Issues[index].Code == code)
            {
                return report.Issues[index];
            }

            index++;
        }

        throw new AssertionException($"Issue {code} was not found.");
    }
}
