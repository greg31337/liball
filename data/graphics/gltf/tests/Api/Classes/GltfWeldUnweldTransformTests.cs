using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfWeldUnweldTransformTests
{
    [Test]
    public void Weld_UsesEveryAttributeAndMorphElementAndPreservesPrimitiveState()
    {
        GltfDocument source = CreateIndexedDocument();
        GltfMeshPrimitive sourcePrimitive = source.Meshes[0].Primitives[0];
        int sourcePosition = FindAttribute(sourcePrimitive, "POSITION");
        int sourceNormal = FindAttribute(sourcePrimitive, "NORMAL");
        int sourceColor = FindAttribute(sourcePrimitive, "COLOR_0");
        int sourceMorph = sourcePrimitive.Targets[0].Attributes[0].Accessor;
        byte[] expectedPosition = SelectElements(
            ReadRaw(source, sourcePosition),
            12,
            new[] { 0, 1, 3 });
        byte[] expectedNormal = SelectElements(
            ReadRaw(source, sourceNormal),
            12,
            new[] { 0, 1, 3 });
        byte[] expectedColor = SelectElements(
            ReadRaw(source, sourceColor),
            4,
            new[] { 0, 1, 3 });
        byte[] expectedMorph = SelectElements(
            ReadRaw(source, sourceMorph),
            12,
            new[] { 0, 1, 3 });

        GltfTransformReport report = Transform(source, GltfTransformKind.Weld);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];

        Assert.Multiple(() =>
        {
            Assert.That(ReadRaw(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(expectedPosition));
            Assert.That(ReadRaw(result, FindAttribute(primitive, "NORMAL")),
                Is.EqualTo(expectedNormal));
            Assert.That(ReadRaw(result, FindAttribute(primitive, "COLOR_0")),
                Is.EqualTo(expectedColor));
            Assert.That(ReadRaw(result, primitive.Targets[0].Attributes[0].Accessor),
                Is.EqualTo(expectedMorph));
            Assert.That(ReadIndices(result, primitive.Indices),
                Is.EqualTo(new uint[] { 0, 1, 0, 2, 0, 1 }));
            Assert.That(result.Accessors[FindAttribute(primitive, "POSITION")].Count,
                Is.EqualTo(3));
            Assert.That(result.Accessors[FindAttribute(primitive, "COLOR_0")].Normalized,
                Is.True);
            Assert.That(result.Accessors[FindAttribute(primitive, "POSITION")].Name,
                Is.EqualTo("positions"));
            Assert.That(result.Accessors[FindAttribute(primitive, "POSITION")]
                .PropertyJson.Extras!["owner"]!.GetValue<string>(), Is.EqualTo("mesh"));
            Assert.That(primitive.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
            Assert.That(primitive.Material, Is.EqualTo(0));
            Assert.That(primitive.PropertyJson.TypedExtensions,
                Does.ContainKey(GltfExtensionNames.KhrMaterialsVariants));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[]
                {
                    "meshes/0/primitives/0",
                    "bufferViews/4"
                }));
            Assert.That(source.Accessors[sourcePosition].Count, Is.EqualTo(4));
            Assert.That(ReadIndices(source, sourcePrimitive.Indices),
                Is.EqualTo(new uint[] { 0, 1, 2, 3, 2, 1 }));
        });
    }

    [Test]
    public void Unweld_ExpandsIndexedStreamsExactlyAndRemovesOnlyTheIndexBinding()
    {
        GltfDocument source = Transform(
            CreateIndexedDocument(),
            GltfTransformKind.Weld).Document!;
        GltfMeshPrimitive sourcePrimitive = source.Meshes[0].Primitives[0];
        int sourcePosition = FindAttribute(sourcePrimitive, "POSITION");
        int sourceNormal = FindAttribute(sourcePrimitive, "NORMAL");
        int sourceColor = FindAttribute(sourcePrimitive, "COLOR_0");
        int sourceMorph = sourcePrimitive.Targets[0].Attributes[0].Accessor;
        int[] sequence = [0, 1, 0, 2, 0, 1];
        byte[] expectedPosition = SelectElements(
            ReadRaw(source, sourcePosition), 12, sequence);
        byte[] expectedNormal = SelectElements(
            ReadRaw(source, sourceNormal), 12, sequence);
        byte[] expectedColor = SelectElements(
            ReadRaw(source, sourceColor), 4, sequence);
        byte[] expectedMorph = SelectElements(
            ReadRaw(source, sourceMorph), 12, sequence);

        GltfTransformReport report = Transform(source, GltfTransformKind.Unweld);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];

        Assert.Multiple(() =>
        {
            Assert.That(primitive.HasIndices, Is.False);
            Assert.That(primitive.Indices, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(ReadRaw(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(expectedPosition));
            Assert.That(ReadRaw(result, FindAttribute(primitive, "NORMAL")),
                Is.EqualTo(expectedNormal));
            Assert.That(ReadRaw(result, FindAttribute(primitive, "COLOR_0")),
                Is.EqualTo(expectedColor));
            Assert.That(ReadRaw(result, primitive.Targets[0].Attributes[0].Accessor),
                Is.EqualTo(expectedMorph));
            Assert.That(result.Accessors[FindAttribute(primitive, "POSITION")].Count,
                Is.EqualTo(6));
            Assert.That(primitive.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
            Assert.That(primitive.Material, Is.EqualTo(0));
            Assert.That(primitive.PropertyJson.TypedExtensions,
                Does.ContainKey(GltfExtensionNames.KhrMaterialsVariants));
            Assert.That(sourcePrimitive.HasIndices, Is.True);
        });
    }

    [Test]
    public void Weld_UnindexedPrimitiveCreatesSmallestExactIndexAccessor()
    {
        GltfDocument source = new GltfDocument();
        int position = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 0,
                1, 0, 0,
                0, 0, 0
            });
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasMode = true,
            Mode = GltfPrimitiveMode.LineStrip
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        source.Meshes.Add(mesh);

        GltfDocument result = Transform(source, GltfTransformKind.Weld).Document!;
        GltfMeshPrimitive welded = result.Meshes[0].Primitives[0];

        Assert.Multiple(() =>
        {
            Assert.That(welded.HasIndices, Is.True);
            Assert.That(ReadIndices(result, welded.Indices),
                Is.EqualTo(new uint[] { 0, 1, 0 }));
            Assert.That(result.Accessors[welded.Indices].ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(result.BufferViews[result.Accessors[welded.Indices].BufferView]
                .Target, Is.EqualTo(GltfBufferViewTarget.ElementArrayBuffer));
            Assert.That(welded.Mode, Is.EqualTo(GltfPrimitiveMode.LineStrip));
        });
    }

    [Test]
    public void VertexTransforms_SkipIncompatibleExtensionsAndKeepMaterialBoundaries()
    {
        GltfDocument source = CreateIndexedDocument();
        GltfMeshPrimitive first = source.Meshes[0].Primitives[0];
        first.PropertyJson.Extensions["EXT_vertex_semantics"] = new JsonObject();
        GltfMeshPrimitive second = new GltfMeshPrimitive
        {
            HasIndices = first.HasIndices,
            Indices = first.Indices,
            HasMaterial = true,
            Material = 1
        };
        int position = FindAttribute(first, "POSITION");
        second.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        source.Meshes[0].Primitives.Add(second);
        source.Materials.Add(new GltfMaterial { Name = "second" });

        GltfTransformReport report = Transform(source, GltfTransformKind.Weld);
        GltfDocument result = report.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Meshes[0].Primitives, Has.Count.EqualTo(2));
            Assert.That(result.Meshes[0].Primitives[0].Indices, Is.EqualTo(first.Indices));
            Assert.That(result.Meshes[0].Primitives[0].Material, Is.EqualTo(0));
            Assert.That(result.Meshes[0].Primitives[1].Material, Is.EqualTo(1));
            Assert.That(result.Meshes[0].Primitives[1].Indices,
                Is.Not.EqualTo(second.Indices));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "meshes/0/primitives/1" }));
        });
    }

    [Test]
    public void Weld_DefaultValidationAcceptsGeneratedAccessorsAndIndices()
    {
        GltfDocument source = new GltfDocument();
        int position = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 0,
                1, 0, 0,
                0, 0, 0
            });
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        source.Meshes.Add(mesh);
        GltfTransformStep step = new GltfTransformStep
        {
            Kind = GltfTransformKind.Weld
        };

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan { Steps = [step] },
            new GltfTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document!.Meshes[0].Primitives[0].HasIndices, Is.True);
            Assert.That(report.Document.Accessors[
                report.Document.Meshes[0].Primitives[0].Indices].Maximum,
                Is.EqualTo(new[] { 1.0 }));
        });
    }

    private static GltfDocument CreateIndexedDocument()
    {
        GltfDocument result = new GltfDocument();
        int position = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 0,
                1, 0, 0,
                0, 0, 0,
                0, 0, 0
            });
        result.Accessors[position].Name = "positions";
        result.Accessors[position].PropertyJson.HasExtras = true;
        result.Accessors[position].PropertyJson.Extras =
            new JsonObject { ["owner"] = "mesh" };
        int normal = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 1,
                0, 0, 1,
                0, 0, 1,
                0, 0, -1
            });
        int color = CreateUnsignedAccessor(
            result,
            GltfAccessorType.Vector4,
            true,
            new uint[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255,
                255, 0, 0, 255,
                255, 0, 0, 255
            });
        int morph = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 0,
                0.1, 0, 0,
                0, 0, 0,
                0, 0, 0
            });
        int indices = CreateIndexAccessor(
            result,
            new uint[] { 0, 1, 2, 3, 2, 1 });
        result.Materials.Add(new GltfMaterial { Name = "first" });
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = indices,
            HasMaterial = true,
            Material = 0,
            HasMode = true,
            Mode = GltfPrimitiveMode.Triangles
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "NORMAL",
            Accessor = normal
        });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "COLOR_0",
            Accessor = color
        });
        GltfMorphTarget target = new GltfMorphTarget();
        target.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = morph
        });
        primitive.Targets.Add(target);
        AddMaterialVariant(primitive);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        return result;
    }

    private static void AddMaterialVariant(GltfMeshPrimitive primitive)
    {
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping
        {
            Material = 0
        };
        mapping.Variants.Add(0);
        GltfMaterialsVariantsPrimitiveExtension extension =
            new GltfMaterialsVariantsPrimitiveExtension();
        extension.Mappings.Add(mapping);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsVariantsPrimitive = extension;
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVariants] = typed;
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.Float,
            Type = type,
            FloatingValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int CreateUnsignedAccessor(
        GltfDocument document,
        GltfAccessorType type,
        bool normalized,
        uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedByte,
            Type = type,
            Normalized = normalized,
            UnsignedValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int CreateIndexAccessor(GltfDocument document, uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedShort,
            Type = GltfAccessorType.Scalar,
            UnsignedValues = values
        };
        int accessor = new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
        GltfBufferView view = document.BufferViews[document.Accessors[accessor].BufferView];
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ElementArrayBuffer;
        return accessor;
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformKind kind)
    {
        GltfTransformStep step = new GltfTransformStep { Kind = kind };
        GltfTransformOptions options = new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan { Steps = [step] },
            options);
    }

    private static int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        int index = 0;
        while (primitive.Attributes[index].Semantic != semantic)
        {
            index++;
        }

        return primitive.Attributes[index].Accessor;
    }

    private static byte[] ReadRaw(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadRawElements(request).RawBytes;
    }

    private static uint[] ReadIndices(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadIndices(request).Indices;
    }

    private static byte[] SelectElements(
        byte[] source,
        int elementByteLength,
        int[] elements)
    {
        byte[] result = new byte[elementByteLength * elements.Length];
        int outputIndex = 0;
        while (outputIndex < elements.Length)
        {
            int byteIndex = 0;
            while (byteIndex < elementByteLength)
            {
                result[outputIndex * elementByteLength + byteIndex] =
                    source[elements[outputIndex] * elementByteLength + byteIndex];
                byteIndex++;
            }

            outputIndex++;
        }

        return result;
    }
}
