using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfPrimitiveOrganizationTransformTests
{
    [Test]
    public void JoinPrimitives_ConcatenatesCompatibleGeometryAndRebasesIndices()
    {
        GltfDocument source = CreateSceneDocument();
        source.Materials.Add(new GltfMaterial { Name = "shared" });
        GltfMesh mesh = source.Meshes[0];
        mesh.Primitives.Add(CreateTrianglePrimitive(source, 0.0, 0, true));
        mesh.Primitives.Add(CreateTrianglePrimitive(source, 2.0, 0, true));

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.JoinPrimitives,
            null,
            true);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Meshes[0].Primitives, Has.Count.EqualTo(1));
            Assert.That(ReadFloating(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(new double[]
                {
                    0, 0, 0,
                    1, 0, 0,
                    0, 1, 0,
                    2, 0, 0,
                    3, 0, 0,
                    2, 1, 0
                }));
            Assert.That(ReadIndices(result, primitive.Indices),
                Is.EqualTo(new uint[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(primitive.Material, Is.EqualTo(0));
            Assert.That(primitive.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
            Assert.That(source.Meshes[0].Primitives, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void SplitPrimitives_PreservesTriangleUnitsAttributesAndMaterial()
    {
        GltfDocument source = CreateSceneDocument();
        source.Materials.Add(new GltfMaterial { Name = "shared" });
        GltfMeshPrimitive primitive = CreateSixVertexPrimitive(source);
        source.Meshes[0].Primitives.Add(primitive);
        JsonObject settings = new JsonObject { ["maxVertices"] = 3 };

        GltfDocument result = Transform(
            source,
            GltfTransformKind.SplitPrimitives,
            settings,
            true).Document!;
        GltfMeshPrimitive first = result.Meshes[0].Primitives[0];
        GltfMeshPrimitive second = result.Meshes[0].Primitives[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Meshes[0].Primitives, Has.Count.EqualTo(2));
            Assert.That(ReadFloating(result, FindAttribute(first, "POSITION")),
                Is.EqualTo(new double[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }));
            Assert.That(ReadFloating(result, FindAttribute(second, "POSITION")),
                Is.EqualTo(new double[] { 2, 0, 0, 3, 0, 0, 2, 1, 0 }));
            Assert.That(ReadIndices(result, first.Indices),
                Is.EqualTo(new uint[] { 0, 1, 2 }));
            Assert.That(ReadIndices(result, second.Indices),
                Is.EqualTo(new uint[] { 0, 1, 2 }));
            Assert.That(first.Material, Is.EqualTo(0));
            Assert.That(second.Material, Is.EqualTo(0));
            Assert.That(first.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
            Assert.That(second.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
        });
    }

    [Test]
    public void JoinPrimitives_ExcludesDifferentMaterialsLayoutsAndUnsafeState()
    {
        GltfDocument source = new GltfDocument();
        source.Materials.Add(new GltfMaterial());
        source.Materials.Add(new GltfMaterial());

        GltfMesh materials = new GltfMesh();
        materials.Primitives.Add(CreateTrianglePrimitive(source, 0.0, 0, true));
        materials.Primitives.Add(CreateTrianglePrimitive(source, 2.0, 1, true));
        source.Meshes.Add(materials);

        GltfMesh layouts = new GltfMesh();
        layouts.Primitives.Add(CreateTrianglePrimitive(source, 4.0, 0, true));
        layouts.Primitives.Add(CreateTrianglePrimitive(source, 6.0, 0, false));
        source.Meshes.Add(layouts);

        GltfMesh unknown = new GltfMesh();
        GltfMeshPrimitive unknownFirst = CreateTrianglePrimitive(source, 8.0, 0, true);
        unknownFirst.PropertyJson.Extensions["EXT_unknown"] = new JsonObject();
        unknown.Primitives.Add(unknownFirst);
        unknown.Primitives.Add(CreateTrianglePrimitive(source, 10.0, 0, true));
        source.Meshes.Add(unknown);

        GltfMesh morph = new GltfMesh();
        GltfMeshPrimitive morphFirst = CreateTrianglePrimitive(source, 12.0, 0, true);
        morphFirst.Targets.Add(new GltfMorphTarget());
        morph.Primitives.Add(morphFirst);
        morph.Primitives.Add(CreateTrianglePrimitive(source, 14.0, 0, true));
        source.Meshes.Add(morph);

        GltfMesh accessorExtension = new GltfMesh();
        GltfMeshPrimitive accessorFirst =
            CreateTrianglePrimitive(source, 16.0, 0, true);
        int positionAccessor = FindAttribute(accessorFirst, "POSITION");
        source.Accessors[positionAccessor].PropertyJson.Extensions["EXT_unknown"] =
            new JsonObject();
        accessorExtension.Primitives.Add(accessorFirst);
        accessorExtension.Primitives.Add(
            CreateTrianglePrimitive(source, 18.0, 0, true));
        source.Meshes.Add(accessorExtension);

        GltfDocument result = Transform(
            source,
            GltfTransformKind.JoinPrimitives,
            null,
            false).Document!;

        Assert.That(result.Meshes.Select(mesh => mesh.Primitives.Count),
            Is.EqualTo(new[] { 2, 2, 2, 2, 2 }));
    }

    [Test]
    public void CreateGpuInstances_ReplacesPlainRepeatedRootsWithExactTrsAccessors()
    {
        GltfDocument source = CreateSceneDocument();
        source.Meshes[0].Primitives.Add(CreateTrianglePrimitive(source, 0.0, -1, true));
        GltfNode first = source.Nodes[0];
        first.HasTranslation = true;
        first.Translation = [1.0, 2.0, 3.0];
        first.HasScale = true;
        first.Scale = [2.0, 2.0, 2.0];
        GltfNode second = new GltfNode
        {
            HasMesh = true,
            Mesh = 0,
            HasTranslation = true,
            Translation = [4.0, 5.0, 6.0],
            HasRotation = true,
            Rotation = [0.0, 0.0, 0.0, 1.0]
        };
        source.Nodes.Add(second);
        source.Scenes[0].Nodes.Add(1);

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.CreateGpuInstances,
            null,
            true);
        GltfDocument result = report.Document!;
        GltfNode node = result.Nodes[0];
        GltfMeshGpuInstancingExtension extension = node.PropertyJson.TypedExtensions[
            GltfExtensionNames.ExtMeshGpuInstancing].MeshGpuInstancing!;

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
            Assert.That(result.Nodes, Has.Count.EqualTo(2));
            Assert.That(node.HasTranslation, Is.False);
            Assert.That(node.HasRotation, Is.False);
            Assert.That(node.HasScale, Is.False);
            Assert.That(ReadFloating(result, extension.Attributes["TRANSLATION"]),
                Is.EqualTo(new double[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(ReadFloating(result, extension.Attributes["ROTATION"]),
                Is.EqualTo(new double[] { 0, 0, 0, 1, 0, 0, 0, 1 }));
            Assert.That(ReadFloating(result, extension.Attributes["SCALE"]),
                Is.EqualTo(new double[] { 2, 2, 2, 1, 1, 1 }));
            Assert.That(result.ExtensionsUsed,
                Does.Contain(GltfExtensionNames.ExtMeshGpuInstancing));
            Assert.That(result.ExtensionsRequired,
                Does.Contain(GltfExtensionNames.ExtMeshGpuInstancing));
            Assert.That(source.Scenes[0].Nodes, Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void CreateGpuInstances_ExcludesSkinAnimationMorphAndUnknownNodeState()
    {
        GltfDocument source = new GltfDocument();
        int pair = 0;
        while (pair < 4)
        {
            GltfMesh mesh = new GltfMesh();
            mesh.Primitives.Add(CreateTrianglePrimitive(source, pair * 2.0, -1, true));
            source.Meshes.Add(mesh);
            source.Nodes.Add(new GltfNode { HasMesh = true, Mesh = pair });
            source.Nodes.Add(new GltfNode { HasMesh = true, Mesh = pair });
            pair++;
        }

        source.Nodes[0].HasSkin = true;
        source.Nodes[0].Skin = 0;
        source.Skins.Add(new GltfSkin());

        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Target.HasNode = true;
        channel.Target.Node = 2;
        GltfAnimation animation = new GltfAnimation();
        animation.Channels.Add(channel);
        source.Animations.Add(animation);

        source.Meshes[2].Weights = [0.0];
        source.Nodes[6].PropertyJson.Extensions["EXT_unknown"] = new JsonObject();

        GltfScene scene = new GltfScene();
        int nodeIndex = 0;
        while (nodeIndex < source.Nodes.Count)
        {
            scene.Nodes.Add(nodeIndex);
            nodeIndex++;
        }

        source.Scenes.Add(scene);
        GltfDocument result = Transform(
            source,
            GltfTransformKind.CreateGpuInstances,
            null,
            false).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Scenes[0].Nodes, Has.Count.EqualTo(8));
            Assert.That(result.ExtensionsUsed,
                Does.Not.Contain(GltfExtensionNames.ExtMeshGpuInstancing));
            Assert.That(result.Nodes.All(node => !node.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.ExtMeshGpuInstancing)), Is.True);
        });
    }

    [Test]
    public void SplitPrimitives_RejectsInvalidMaxVerticesSetting()
    {
        GltfDocument source = CreateSceneDocument();
        source.Meshes[0].Primitives.Add(CreateSixVertexPrimitive(source));
        JsonObject settings = new JsonObject { ["maxVertices"] = 2 };

        Assert.That(
            () => Transform(
                source,
                GltfTransformKind.SplitPrimitives,
                settings,
                false),
            Throws.ArgumentException.With.Message.Contains("at least three"));
    }

    private static GltfDocument CreateSceneDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Meshes.Add(new GltfMesh());
        result.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 0 });
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        result.Scenes.Add(scene);
        result.HasDefaultScene = true;
        result.DefaultScene = 0;
        return result;
    }

    private static GltfMeshPrimitive CreateTrianglePrimitive(
        GltfDocument document,
        double x,
        int material,
        bool includeNormal)
    {
        int position = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            new double[]
            {
                x, 0, 0,
                x + 1, 0, 0,
                x, 1, 0
            });
        GltfMeshPrimitive result = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = CreateIndexAccessor(document, new uint[] { 0, 1, 2 }),
            HasMode = true,
            Mode = GltfPrimitiveMode.Triangles
        };
        if (material >= 0)
        {
            result.HasMaterial = true;
            result.Material = material;
        }

        result.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        if (includeNormal)
        {
            int normal = CreateFloatingAccessor(
                document,
                GltfAccessorType.Vector3,
                new double[]
                {
                    0, 0, 1,
                    0, 0, 1,
                    0, 0, 1
                });
            result.Attributes.Add(new GltfAttribute
            {
                Semantic = "NORMAL",
                Accessor = normal
            });
        }

        return result;
    }

    private static GltfMeshPrimitive CreateSixVertexPrimitive(GltfDocument document)
    {
        int position = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            new double[]
            {
                0, 0, 0,
                1, 0, 0,
                0, 1, 0,
                2, 0, 0,
                3, 0, 0,
                2, 1, 0
            });
        GltfMeshPrimitive result = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = CreateIndexAccessor(
                document,
                new uint[] { 0, 1, 2, 3, 4, 5 }),
            HasMaterial = true,
            Material = 0,
            HasMode = true,
            Mode = GltfPrimitiveMode.Triangles
        };
        result.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        return result;
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
        GltfTransformKind kind,
        JsonObject? settings,
        bool validate)
    {
        GltfTransformStep step = new GltfTransformStep { Kind = kind };
        if (settings != null)
        {
            step.Settings = settings;
        }

        GltfTransformOptions options = new GltfTransformOptions
        {
            ValidateBefore = validate,
            ValidateAfter = validate
        };
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan { Steps = [step] },
            options);
    }

    private static int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        return primitive.Attributes.Single(attribute => attribute.Semantic == semantic)
            .Accessor;
    }

    private static double[] ReadFloating(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadFloatingValues(request).FloatingValues;
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
}
