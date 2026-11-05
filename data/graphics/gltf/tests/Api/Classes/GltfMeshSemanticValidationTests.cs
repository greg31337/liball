namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfMeshSemanticValidationTests
{
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
    public void ValidateDocument_ChecksTopologyDrawCountBoundaries(
        GltfPrimitiveMode mode,
        int count,
        bool expectsIssue)
    {
        GltfDocument document = CreateMinimalMeshDocument(count);
        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        primitive.HasMode = true;
        primitive.Mode = mode;

        GltfValidationReport report = Validate(document);

        Assert.That(
            HasIssue(
                report,
                GltfIssueCodes.MeshPrimitiveIncompatibleMode,
                GltfValidationSeverity.Warning,
                "/meshes/0/primitives/0/mode"),
            Is.EqualTo(expectsIssue),
            FormatIssues(report));
    }

    [Test]
    public void ValidateDocument_ChecksIndexFormatBoundsRestartAndDegeneracy()
    {
        GltfDocument valid = CreateIndexedDocument(new uint[] { 0U, 1U, 2U });
        GltfDocument invalid = CreateIndexedDocument(new uint[] { 0U, 3U, 255U });
        GltfDocument degenerate = CreateIndexedDocument(new uint[] { 0U, 0U, 1U });

        GltfDocument invalidFormat = CreateMinimalMeshDocument(3);
        GltfAccessor signedVector = CreateAccessor(
            GltfAccessorType.Vector2,
            GltfComponentType.SignedShort,
            3,
            true);
        invalidFormat.Accessors.Add(signedVector);
        invalidFormat.Meshes[0].Primitives[0].HasIndices = true;
        invalidFormat.Meshes[0].Primitives[0].Indices = 1;

        GltfValidationReport validReport = Validate(valid);
        GltfValidationReport invalidReport = Validate(invalid);
        GltfValidationReport degenerateReport = Validate(degenerate);
        GltfValidationReport formatReport = Validate(invalidFormat);

        int invalidAccessor = invalid.Meshes[0].Primitives[0].Indices;
        int degenerateAccessor = degenerate.Meshes[0].Primitives[0].Indices;
        Assert.Multiple(() =>
        {
            Assert.That(validReport.IsValid, Is.True, FormatIssues(validReport));
            AssertNoIssue(validReport, GltfIssueCodes.AccessorIndexOob);
            AssertNoIssue(validReport, GltfIssueCodes.AccessorIndexPrimitiveRestart);
            AssertNoIssue(validReport, GltfIssueCodes.AccessorIndexTriangleDegenerate);
            AssertIssue(
                invalidReport,
                GltfIssueCodes.AccessorIndexOob,
                GltfValidationSeverity.Error,
                $"/accessors/{invalidAccessor}");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.AccessorIndexPrimitiveRestart,
                GltfValidationSeverity.Error,
                $"/accessors/{invalidAccessor}");
            AssertIssue(
                degenerateReport,
                GltfIssueCodes.AccessorIndexTriangleDegenerate,
                GltfValidationSeverity.Information,
                $"/accessors/{degenerateAccessor}");
            AssertIssue(
                formatReport,
                GltfIssueCodes.MeshPrimitiveIndicesAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/indices");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsStandardCustomAttributesAndDefaultMaterial()
    {
        GltfDocument document = new GltfDocument();
        int position = AddAccessor(
            document,
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            3,
            false);
        document.Accessors[position].Minimum = new double[] { 0.0, 0.0, 0.0 };
        document.Accessors[position].Maximum = new double[] { 1.0, 1.0, 1.0 };
        int normal = AddAccessor(
            document,
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            3,
            false);
        int tangent = AddAccessor(
            document,
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false);
        int texcoord = AddAccessor(
            document,
            GltfAccessorType.Vector2,
            GltfComponentType.UnsignedShort,
            3,
            true);
        int color = AddAccessor(
            document,
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedByte,
            3,
            true);
        int joints = AddAccessor(
            document,
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedShort,
            3,
            false);
        int weights = AddAccessor(
            document,
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedShort,
            3,
            true);
        int custom = AddAccessor(
            document,
            GltfAccessorType.Scalar,
            GltfComponentType.SignedShort,
            3,
            false);

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, GltfSemantics.Position, position);
        AddAttribute(primitive, GltfSemantics.Normal, normal);
        AddAttribute(primitive, GltfSemantics.Tangent, tangent);
        AddAttribute(primitive, "TEXCOORD_0", texcoord);
        AddAttribute(primitive, "COLOR_0", color);
        AddAttribute(primitive, "JOINTS_0", joints);
        AddAttribute(primitive, "WEIGHTS_0", weights);
        AddAttribute(primitive, "_TEMPERATURE", custom);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            Assert.That(primitive.HasMaterial, Is.False);
            Assert.That(report.IsValid, Is.True, FormatIssues(report));
            AssertNoIssue(report, GltfIssueCodes.UnresolvedReference);
            AssertNoIssue(report, GltfIssueCodes.MeshPrimitiveInvalidAttribute);
            AssertNoIssue(
                report,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat);
            AssertNoIssue(report, GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch);
        });
    }

    [Test]
    public void ValidateDocument_ReportsAttributeRelationshipsAndMaterialReference()
    {
        GltfDocument document = CreateMinimalMeshDocument(1);
        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        primitive.HasMode = true;
        primitive.Mode = GltfPrimitiveMode.Points;
        AddAttribute(primitive, GltfSemantics.Position, 0);
        AddAttribute(
            primitive,
            "TEXCOORD_01",
            AddAccessor(
                document,
                GltfAccessorType.Vector2,
                GltfComponentType.Float,
                1,
                false));
        AddAttribute(
            primitive,
            "TEXCOORD_1",
            AddAccessor(
                document,
                GltfAccessorType.Vector2,
                GltfComponentType.Float,
                1,
                false));
        AddAttribute(
            primitive,
            GltfSemantics.Tangent,
            AddAccessor(
                document,
                GltfAccessorType.Vector4,
                GltfComponentType.Float,
                1,
                false));
        AddAttribute(
            primitive,
            "JOINTS_0",
            AddAccessor(
                document,
                GltfAccessorType.Vector4,
                GltfComponentType.UnsignedByte,
                1,
                false));
        AddAttribute(
            primitive,
            "COLOR_0",
            AddAccessor(
                document,
                GltfAccessorType.Vector4,
                GltfComponentType.Float,
                2,
                false));
        AddAttribute(
            primitive,
            "_OBJECT_ID",
            AddAccessor(
                document,
                GltfAccessorType.Scalar,
                GltfComponentType.UnsignedInt,
                1,
                false));
        primitive.HasMaterial = true;
        primitive.Material = 4;

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes/POSITION");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes/TEXCOORD_01");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveIndexedSemanticContinuity,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveTangentWithoutNormal,
                GltfValidationSeverity.Warning,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveUnequalAccessorCount,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes/COLOR_0");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorUnsignedInt,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes/_OBJECT_ID");
            AssertIssue(
                report,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/material");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsQuantizedFormatsOnlyWhenDeclaredRequired()
    {
        GltfDocument unsupported = CreateMinimalMeshDocument(3);
        unsupported.Accessors[0].ComponentType = GltfComponentType.SignedShort;
        unsupported.Accessors[0].HasNormalized = true;
        unsupported.Accessors[0].Normalized = true;
        unsupported.Accessors[0].Minimum = new double[] { -32767.0, -32767.0, -32767.0 };
        unsupported.Accessors[0].Maximum = new double[] { 32767.0, 32767.0, 32767.0 };

        GltfDocument supported = CreateQuantizedMeshDocument();

        GltfValidationReport unsupportedReport = Validate(unsupported);
        GltfValidationReport supportedReport = Validate(supported);

        Assert.Multiple(() =>
        {
            AssertIssue(
                unsupportedReport,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes/POSITION");
            Assert.That(supportedReport.IsValid, Is.True, FormatIssues(supportedReport));
            AssertNoIssue(
                supportedReport,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat);
            AssertNoIssue(supportedReport, GltfIssueCodes.UndeclaredExtension);
            AssertNoIssue(supportedReport, GltfIssueCodes.UnsatisfiedDependency);
        });
    }

    [Test]
    public void ValidateDocument_ChecksDecodedNormalsTangentsColorsAndWeights()
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
        primitive.HasMode = true;
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

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorVector3NonUnit,
                GltfValidationSeverity.Error,
                $"/accessors/{normal}");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorInvalidSign,
                GltfValidationSeverity.Error,
                $"/accessors/{tangent}");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorNonClamped,
                GltfValidationSeverity.Error,
                $"/accessors/{color}");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorWeightsNegative,
                GltfValidationSeverity.Error,
                $"/accessors/{weights}");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorWeightsNonNormalized,
                GltfValidationSeverity.Error,
                $"/accessors/{weights}");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsValidMorphTargetsAndReportsInvalidRelationships()
    {
        GltfDocument valid = CreateValidMorphDocument();
        GltfValidationReport validReport = Validate(valid);

        GltfDocument invalid = CreateMinimalMeshDocument(3);
        GltfMeshPrimitive primitive = invalid.Meshes[0].Primitives[0];
        int baseTangent = AddAccessor(
            invalid,
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false);
        AddAttribute(primitive, GltfSemantics.Tangent, baseTangent);
        GltfMorphTarget target = new GltfMorphTarget();
        AddTargetAttribute(
            target,
            GltfSemantics.Position,
            AddAccessor(
                invalid,
                GltfAccessorType.Vector3,
                GltfComponentType.Float,
                2,
                false));
        AddTargetAttribute(
            target,
            GltfSemantics.Normal,
            AddAccessor(
                invalid,
                GltfAccessorType.Vector3,
                GltfComponentType.Float,
                3,
                false));
        AddTargetAttribute(
            target,
            GltfSemantics.Tangent,
            AddAccessor(
                invalid,
                GltfAccessorType.Vector4,
                GltfComponentType.Float,
                3,
                false));
        primitive.Targets.Add(target);
        invalid.Meshes[0].Weights = new double[] { 0.0, 1.0 };
        GltfMeshPrimitive second = new GltfMeshPrimitive();
        AddAttribute(second, GltfSemantics.Position, 0);
        invalid.Meshes[0].Primitives.Add(second);

        GltfValidationReport invalidReport = Validate(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validReport.IsValid, Is.True, FormatIssues(validReport));
            AssertNoIssue(
                validReport,
                GltfIssueCodes.MeshPrimitiveMorphTargetInvalidAttributeCount);
            AssertNoIssue(
                validReport,
                GltfIssueCodes.MeshPrimitiveMorphTargetNoBaseAccessor);
            AssertIssue(
                invalidReport,
                GltfIssueCodes.MeshPrimitiveMorphTargetInvalidAttributeCount,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/targets/0/POSITION");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.MeshPrimitiveMorphTargetNoBaseAccessor,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/targets/0/NORMAL");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/targets/0/TANGENT");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.MeshInvalidWeightsCount,
                GltfValidationSeverity.Error,
                "/meshes/0/weights");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.MeshPrimitivesUnequalTargetsCount,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/1/targets");
        });
    }

    [Test]
    public void ValidateDocument_AcceptsExplicitAndImplicitDefaultMaterialSelection()
    {
        GltfDocument implicitDefault = CreateMinimalMeshDocument(3);
        GltfDocument explicitMaterial = CreateMinimalMeshDocument(3);
        explicitMaterial.Materials.Add(new GltfMaterial());
        explicitMaterial.Meshes[0].Primitives[0].HasMaterial = true;
        explicitMaterial.Meshes[0].Primitives[0].Material = 0;
        GltfDocument invalidMaterial = CreateMinimalMeshDocument(3);
        invalidMaterial.Meshes[0].Primitives[0].HasMaterial = true;
        invalidMaterial.Meshes[0].Primitives[0].Material = 0;

        GltfValidationReport implicitReport = Validate(implicitDefault);
        GltfValidationReport explicitReport = Validate(explicitMaterial);
        GltfValidationReport invalidReport = Validate(invalidMaterial);

        Assert.Multiple(() =>
        {
            Assert.That(implicitReport.IsValid, Is.True, FormatIssues(implicitReport));
            Assert.That(explicitReport.IsValid, Is.True, FormatIssues(explicitReport));
            AssertNoIssue(implicitReport, GltfIssueCodes.UnresolvedReference);
            AssertNoIssue(explicitReport, GltfIssueCodes.UnresolvedReference);
            AssertIssue(
                invalidReport,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/material");
        });
    }

    private static GltfDocument CreateMinimalMeshDocument(int count)
    {
        GltfDocument result = new GltfDocument();
        GltfAccessor position = CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            count,
            false);
        position.Minimum = new double[] { 0.0, 0.0, 0.0 };
        position.Maximum = new double[] { 1.0, 1.0, 1.0 };
        result.Accessors.Add(position);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, GltfSemantics.Position, 0);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        return result;
    }

    private static GltfDocument CreateIndexedDocument(uint[] indices)
    {
        GltfDocument result = new GltfDocument();
        GltfAccessorCodec codec = new GltfAccessorCodec();
        int position = CreateFloatingAccessor(
            codec,
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        GltfAccessorWriteRequest write = new GltfAccessorWriteRequest();
        write.Document = result;
        write.ComponentType = GltfComponentType.UnsignedByte;
        write.Type = GltfAccessorType.Scalar;
        write.UnsignedValues = indices;
        int indexAccessor = codec.CreateDenseAccessor(write).Accessor;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasIndices = true;
        primitive.Indices = indexAccessor;
        AddAttribute(primitive, GltfSemantics.Position, position);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        return result;
    }

    private static GltfDocument CreateQuantizedMeshDocument()
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshQuantization);
        result.ExtensionsRequired.Add(GltfExtensionNames.KhrMeshQuantization);
        int position = AddAccessor(
            result,
            GltfAccessorType.Vector3,
            GltfComponentType.SignedShort,
            3,
            true);
        result.Accessors[position].Minimum = new double[]
        {
            -32767.0,
            -32767.0,
            -32767.0
        };
        result.Accessors[position].Maximum = new double[]
        {
            32767.0,
            32767.0,
            32767.0
        };
        int normal = AddAccessor(
            result,
            GltfAccessorType.Vector3,
            GltfComponentType.SignedByte,
            3,
            true);
        int tangent = AddAccessor(
            result,
            GltfAccessorType.Vector4,
            GltfComponentType.SignedShort,
            3,
            true);
        int texcoord = AddAccessor(
            result,
            GltfAccessorType.Vector2,
            GltfComponentType.SignedShort,
            3,
            false);
        int color = AddAccessor(
            result,
            GltfAccessorType.Vector3,
            GltfComponentType.UnsignedByte,
            3,
            true);
        int joints = AddAccessor(
            result,
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedByte,
            3,
            false);
        int weights = AddAccessor(
            result,
            GltfAccessorType.Vector4,
            GltfComponentType.UnsignedByte,
            3,
            true);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, GltfSemantics.Position, position);
        AddAttribute(primitive, GltfSemantics.Normal, normal);
        AddAttribute(primitive, GltfSemantics.Tangent, tangent);
        AddAttribute(primitive, "TEXCOORD_0", texcoord);
        AddAttribute(primitive, "COLOR_0", color);
        AddAttribute(primitive, "JOINTS_0", joints);
        AddAttribute(primitive, "WEIGHTS_0", weights);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        return result;
    }

    private static GltfDocument CreateValidMorphDocument()
    {
        GltfDocument result = CreateMinimalMeshDocument(3);
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        int normal = AddAccessor(
            result,
            GltfAccessorType.Vector3,
            GltfComponentType.Float,
            3,
            false);
        int tangent = AddAccessor(
            result,
            GltfAccessorType.Vector4,
            GltfComponentType.Float,
            3,
            false);
        int custom = AddAccessor(
            result,
            GltfAccessorType.Vector2,
            GltfComponentType.SignedShort,
            3,
            false);
        AddAttribute(primitive, GltfSemantics.Normal, normal);
        AddAttribute(primitive, GltfSemantics.Tangent, tangent);
        AddAttribute(primitive, "_WIND", custom);

        GltfMorphTarget target = new GltfMorphTarget();
        AddTargetAttribute(
            target,
            GltfSemantics.Position,
            AddAccessor(
                result,
                GltfAccessorType.Vector3,
                GltfComponentType.Float,
                3,
                false));
        AddTargetAttribute(
            target,
            GltfSemantics.Normal,
            AddAccessor(
                result,
                GltfAccessorType.Vector3,
                GltfComponentType.Float,
                3,
                false));
        AddTargetAttribute(
            target,
            GltfSemantics.Tangent,
            AddAccessor(
                result,
                GltfAccessorType.Vector3,
                GltfComponentType.Float,
                3,
                false));
        AddTargetAttribute(
            target,
            "_WIND",
            AddAccessor(
                result,
                GltfAccessorType.Vector2,
                GltfComponentType.SignedShort,
                3,
                false));
        primitive.Targets.Add(target);
        result.Meshes[0].Weights = new double[] { 0.0 };
        return result;
    }

    private static GltfAccessor CreateAccessor(
        GltfAccessorType type,
        GltfComponentType componentType,
        int count,
        bool normalized)
    {
        GltfAccessor result = new GltfAccessor();
        result.Type = type;
        result.ComponentType = componentType;
        result.Count = count;
        result.HasNormalized = normalized;
        result.Normalized = normalized;
        return result;
    }

    private static int AddAccessor(
        GltfDocument document,
        GltfAccessorType type,
        GltfComponentType componentType,
        int count,
        bool normalized)
    {
        int result = document.Accessors.Count;
        document.Accessors.Add(CreateAccessor(type, componentType, count, normalized));
        return result;
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

    private static GltfValidationReport Validate(GltfDocument document)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateResources = false;
        options.ValidateExtensions = false;
        return new GltfValidator().ValidateDocument(document, options);
    }

    private static bool HasIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        return report.Issues.Any(issue => issue.Code == code
            && issue.Severity == severity
            && issue.JsonPointer == pointer);
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
            HasIssue(report, code, severity, pointer),
            Is.True,
            $"Expected {severity} {code} at '{pointer}', got: {FormatIssues(report)}");
    }

    private static string FormatIssues(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(
                issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
