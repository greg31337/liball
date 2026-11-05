namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class MeshQuantizationTests
{
    [TestCase("POSITION", GltfAccessorType.Vector3, GltfComponentType.SignedByte, false, true)]
    [TestCase("POSITION", GltfAccessorType.Vector3, GltfComponentType.UnsignedShort, true, true)]
    [TestCase("NORMAL", GltfAccessorType.Vector3, GltfComponentType.SignedByte, true, true)]
    [TestCase("NORMAL", GltfAccessorType.Vector3, GltfComponentType.SignedByte, false, false)]
    [TestCase("TANGENT", GltfAccessorType.Vector4, GltfComponentType.SignedShort, true, true)]
    [TestCase("TEXCOORD_0", GltfAccessorType.Vector2, GltfComponentType.SignedShort, true, true)]
    [TestCase("TEXCOORD_0", GltfAccessorType.Vector2, GltfComponentType.UnsignedByte, false, true)]
    [TestCase("TEXCOORD_0", GltfAccessorType.Vector2, GltfComponentType.UnsignedByte, true, true)]
    public void BaseAttributeFormats_ApplyTheExpandedFormatMatrix(
        string name,
        GltfAccessorType type,
        GltfComponentType componentType,
        bool normalized,
        bool expected)
    {
        GltfDocument document = CreateEnabledDocument();
        GltfAccessor accessor = CreateAccessor(type, componentType, normalized);
        AttributeSemantic semantic = new AttributeSemanticParser().ParseAttributeSemantic(name);

        bool valid = new MeshPrimitiveValidator().ValidateBaseAttributeFormat(
            document,
            semantic,
            accessor);

        Assert.That(valid, Is.EqualTo(expected));
    }

    [TestCase("POSITION", GltfAccessorType.Vector3, GltfComponentType.SignedByte, false, true)]
    [TestCase("POSITION", GltfAccessorType.Vector3, GltfComponentType.UnsignedByte, false, false)]
    [TestCase("NORMAL", GltfAccessorType.Vector3, GltfComponentType.SignedShort, true, true)]
    [TestCase("TANGENT", GltfAccessorType.Vector3, GltfComponentType.SignedByte, true, true)]
    [TestCase("TEXCOORD_0", GltfAccessorType.Vector2, GltfComponentType.SignedShort, false, true)]
    [TestCase("TEXCOORD_0", GltfAccessorType.Vector2, GltfComponentType.SignedShort, true, false)]
    public void MorphAttributeFormats_ApplyTheExpandedFormatMatrix(
        string name,
        GltfAccessorType type,
        GltfComponentType componentType,
        bool normalized,
        bool expected)
    {
        GltfDocument document = CreateEnabledDocument();
        GltfAccessor accessor = CreateAccessor(type, componentType, normalized);
        GltfAccessor baseAccessor = CreateAccessor(type, GltfComponentType.Float, false);
        AttributeSemantic semantic = new AttributeSemanticParser().ParseAttributeSemantic(name);

        bool valid = new MeshPrimitiveValidator().ValidateMorphAttributeFormat(
            document,
            semantic,
            accessor,
            baseAccessor);

        Assert.That(valid, Is.EqualTo(expected));
    }

    [Test]
    public void UpdateDeclarations_AddsAndRemovesTheRequiredMarkerWithoutDuplicates()
    {
        GltfDocument document = CreatePrimitiveDocument(
            GltfSemantics.Position,
            CreateAccessor(
                GltfAccessorType.Vector3,
                GltfComponentType.SignedShort,
                false));
        document.ExtensionsUsed.Add("EXT_example");
        MeshQuantization quantization = new MeshQuantization();

        quantization.UpdateDeclarations(document);
        quantization.UpdateDeclarations(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                document.ExtensionsUsed.Count(value =>
                    value == GltfExtensionNames.KhrMeshQuantization),
                Is.EqualTo(1));
            Assert.That(
                document.ExtensionsRequired,
                Is.EqualTo(new[] { GltfExtensionNames.KhrMeshQuantization }));
            Assert.That(quantization.IsEnabled(document), Is.True);
        });

        document.Accessors[0].ComponentType = GltfComponentType.Float;
        quantization.UpdateDeclarations(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.ExtensionsUsed, Is.EqualTo(new[] { "EXT_example" }));
            Assert.That(document.ExtensionsRequired, Is.Empty);
            Assert.That(quantization.IsEnabled(document), Is.False);
        });
    }

    [Test]
    public void ValidateDocumentRules_RequiresBothDeclarationsForExpandedFormats()
    {
        GltfDocument document = CreatePrimitiveDocument(
            GltfSemantics.Position,
            CreateAccessor(
                GltfAccessorType.Vector3,
                GltfComponentType.SignedShort,
                true));

        GltfValidationReport undeclared = new DocumentRuleValidator()
            .ValidateDocumentRules(document);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshQuantization);
        GltfValidationReport optional = new DocumentRuleValidator()
            .ValidateDocumentRules(document);
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrMeshQuantization);
        GltfValidationReport required = new DocumentRuleValidator()
            .ValidateDocumentRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(
                undeclared,
                GltfIssueCodes.UndeclaredExtension,
                "/meshes/0/primitives/0/attributes/POSITION"), Is.True);
            Assert.That(HasIssue(
                undeclared,
                GltfIssueCodes.UnsatisfiedDependency,
                "/extensionsRequired"), Is.True);
            Assert.That(HasIssue(
                optional,
                GltfIssueCodes.UnsatisfiedDependency,
                "/extensionsRequired"), Is.True);
            Assert.That(required.IsValid, Is.True);
        });
    }

    [Test]
    public void CalculateAccessorBounds_ExposesStoredAndDequantizedBounds()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 6;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[] { 128, 0, 64, 127, 129, 0 };
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 6;
        document.BufferViews.Add(view);
        GltfAccessor accessor = CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.SignedByte,
            true);
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.Count = 2;
        document.Accessors.Add(accessor);

        AccessorBoundsResult result = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Minimum, Is.EqualTo(new double[] { -128.0, -127.0, 0.0 }));
            Assert.That(result.Maximum, Is.EqualTo(new double[] { 127.0, 0.0, 64.0 }));
            Assert.That(result.DequantizedMinimum, Is.EqualTo(new double[] { -1.0, -1.0, 0.0 }));
            Assert.That(result.DequantizedMaximum[0], Is.EqualTo(1.0));
            Assert.That(result.DequantizedMaximum[1], Is.EqualTo(0.0));
            Assert.That(result.DequantizedMaximum[2], Is.EqualTo(64.0 / 127.0));
        });
    }

    [Test]
    public void ValidateDocumentMeshes_RequiresFourByteAlignmentForEveryQuantizedElement()
    {
        GltfDocument packed = CreateQuantizedPositionStorage(false);
        GltfDocument strided = CreateQuantizedPositionStorage(true);

        GltfValidationReport packedReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(packed);
        GltfValidationReport stridedReport = new MeshPrimitiveValidator()
            .ValidateDocumentMeshes(strided);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(
                packedReport,
                GltfIssueCodes.MeshPrimitiveAccessorUnaligned,
                "/meshes/0/primitives/0/attributes/POSITION"), Is.True);
            Assert.That(HasIssue(
                stridedReport,
                GltfIssueCodes.MeshPrimitiveAccessorUnaligned,
                "/meshes/0/primitives/0/attributes/POSITION"), Is.False);
        });
    }

    private static GltfDocument CreateEnabledDocument()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMeshQuantization);
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrMeshQuantization);
        return document;
    }

    private static GltfAccessor CreateAccessor(
        GltfAccessorType type,
        GltfComponentType componentType,
        bool normalized)
    {
        GltfAccessor accessor = new GltfAccessor();
        accessor.Type = type;
        accessor.ComponentType = componentType;
        accessor.Normalized = normalized;
        if (normalized)
        {
            accessor.HasNormalized = true;
        }

        accessor.Count = 1;
        return accessor;
    }

    private static GltfDocument CreatePrimitiveDocument(
        string semantic,
        GltfAccessor accessor)
    {
        GltfDocument document = new GltfDocument();
        document.Accessors.Add(accessor);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = 0;
        primitive.Attributes.Add(attribute);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static GltfDocument CreateQuantizedPositionStorage(bool strided)
    {
        GltfDocument document = CreateEnabledDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = strided ? 8 : 6;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[buffer.ByteLength];
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = buffer.ByteLength;
        if (strided)
        {
            view.HasByteStride = true;
            view.ByteStride = 4;
        }

        document.BufferViews.Add(view);
        GltfAccessor accessor = CreateAccessor(
            GltfAccessorType.Vector3,
            GltfComponentType.SignedByte,
            false);
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.Count = 2;
        accessor.Minimum = new double[] { 0.0, 0.0, 0.0 };
        accessor.Maximum = new double[] { 0.0, 0.0, 0.0 };
        document.Accessors.Add(accessor);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = GltfSemantics.Position;
        attribute.Accessor = 0;
        primitive.Attributes.Add(attribute);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static bool HasIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        return report.Issues.Any(issue =>
            issue.Code == code && issue.JsonPointer == pointer);
    }
}
