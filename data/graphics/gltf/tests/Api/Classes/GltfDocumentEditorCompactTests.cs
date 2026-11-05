namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDocumentEditorCompactTests
{
    [Test]
    public void CompactDocument_RemovesNullHolesAndRemapsEveryCoreCollection()
    {
        GltfDocument source = CreateHoledDocument();
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfMergeResult result = new GltfDocumentEditor().CompactDocument(source, options);
        GltfDocument compacted = result.Document!;
        GltfIndexRemap remap = result.SourceRemaps[0];

        Assert.Multiple(() =>
        {
            Assert.That(compacted.Accessors, Has.Count.EqualTo(2));
            Assert.That(compacted.Animations, Has.Count.EqualTo(2));
            Assert.That(compacted.Buffers, Has.Count.EqualTo(2));
            Assert.That(compacted.BufferViews, Has.Count.EqualTo(2));
            Assert.That(compacted.Cameras, Has.Count.EqualTo(2));
            Assert.That(compacted.Images, Has.Count.EqualTo(2));
            Assert.That(compacted.Materials, Has.Count.EqualTo(2));
            Assert.That(compacted.Meshes, Has.Count.EqualTo(2));
            Assert.That(compacted.Nodes, Has.Count.EqualTo(2));
            Assert.That(compacted.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(compacted.Samplers, Has.Count.EqualTo(2));
            Assert.That(compacted.Scenes, Has.Count.EqualTo(2));
            Assert.That(compacted.Skins, Has.Count.EqualTo(2));
            Assert.That(compacted.Textures, Has.Count.EqualTo(2));
            Assert.That(remap.Accessors, Is.EqualTo(new Dictionary<int, int>
            {
                [0] = 0,
                [2] = 1
            }));
            Assert.That(remap.Animations[2], Is.EqualTo(1));
            Assert.That(remap.Buffers[2], Is.EqualTo(1));
            Assert.That(remap.BufferViews[2], Is.EqualTo(1));
            Assert.That(remap.Cameras[2], Is.EqualTo(1));
            Assert.That(remap.Images[2], Is.EqualTo(1));
            Assert.That(remap.Materials[2], Is.EqualTo(1));
            Assert.That(remap.Meshes[2], Is.EqualTo(1));
            Assert.That(remap.Nodes[2], Is.EqualTo(1));
            Assert.That(remap.Resources[2], Is.EqualTo(1));
            Assert.That(remap.Samplers[2], Is.EqualTo(1));
            Assert.That(remap.Scenes[2], Is.EqualTo(1));
            Assert.That(remap.Skins[2], Is.EqualTo(1));
            Assert.That(remap.Textures[2], Is.EqualTo(1));
            Assert.That(remap.Nodes.ContainsKey(1), Is.False);
        });

        AssertCompactedReferences(compacted);
        Assert.Multiple(() =>
        {
            Assert.That(compacted, Is.Not.SameAs(source));
            Assert.That(compacted.Nodes[1], Is.Not.SameAs(source.Nodes[2]));
            Assert.That(compacted.Resources.Cache["last"], Is.SameAs(
                compacted.Resources.Resources[1]));
            Assert.That(source.Nodes, Has.Count.EqualTo(3));
            Assert.That(source.Nodes[1], Is.Null);
            Assert.That(source.Accessors[2].BufferView, Is.EqualTo(2));
        });
    }

    [Test]
    public void CompactDocument_RejectsRemovalStillReferencedByRetainedObject()
    {
        GltfDocument source = new GltfDocument();
        source.Accessors.Add(null!);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = 0
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        source.Meshes.Add(mesh);
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        Assert.That(
            () => new GltfDocumentEditor().CompactDocument(source, options),
            Throws.TypeOf<GltfValidationException>()
                .With.Message.Contains("accessors index 0"));
    }

    [Test]
    public void RebuildReferences_ReturnsIndependentIdentityRemaps()
    {
        GltfDocument source = new GltfDocument();
        source.Buffers.Add(new GltfBuffer());
        source.BufferViews.Add(new GltfBufferView { Buffer = 0 });
        source.Accessors.Add(new GltfAccessor { HasBufferView = true, BufferView = 0 });
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfMergeResult result = new GltfDocumentEditor().RebuildReferences(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Document, Is.Not.SameAs(source));
            Assert.That(result.Document!.Accessors[0], Is.Not.SameAs(source.Accessors[0]));
            Assert.That(result.Document.Accessors[0].BufferView, Is.EqualTo(0));
            Assert.That(result.SourceRemaps, Has.Count.EqualTo(1));
            Assert.That(result.SourceRemaps[0].Accessors[0], Is.EqualTo(0));
            Assert.That(result.SourceRemaps[0].BufferViews[0], Is.EqualTo(0));
            Assert.That(result.SourceRemaps[0].Buffers[0], Is.EqualTo(0));
        });
    }

    private static void AssertCompactedReferences(GltfDocument document)
    {
        GltfAccessor accessor = document.Accessors[1];
        GltfAnimation animation = document.Animations[1];
        GltfMaterial material = document.Materials[1];
        GltfMeshPrimitive primitive = document.Meshes[1].Primitives[0];
        GltfNode node = document.Nodes[1];

        Assert.Multiple(() =>
        {
            Assert.That(document.DefaultScene, Is.EqualTo(1));
            Assert.That(accessor.BufferView, Is.EqualTo(1));
            Assert.That(accessor.Sparse!.Indices.BufferView, Is.EqualTo(1));
            Assert.That(accessor.Sparse.Values.BufferView, Is.EqualTo(1));
            Assert.That(animation.Samplers[0].Input, Is.EqualTo(1));
            Assert.That(animation.Samplers[0].Output, Is.EqualTo(1));
            Assert.That(animation.Channels[0].Target.Node, Is.EqualTo(1));
            Assert.That(document.Buffers[1].Resource, Is.EqualTo(1));
            Assert.That(document.BufferViews[1].Buffer, Is.EqualTo(1));
            Assert.That(document.Images[1].BufferView, Is.EqualTo(1));
            Assert.That(document.Images[1].Resource, Is.EqualTo(1));
            Assert.That(material.PbrMetallicRoughness!.BaseColorTexture!.Index,
                Is.EqualTo(1));
            Assert.That(primitive.Attributes[0].Accessor, Is.EqualTo(1));
            Assert.That(primitive.Indices, Is.EqualTo(1));
            Assert.That(primitive.Material, Is.EqualTo(1));
            Assert.That(node.Camera, Is.EqualTo(1));
            Assert.That(node.Skin, Is.EqualTo(1));
            Assert.That(node.Mesh, Is.EqualTo(1));
            Assert.That(document.Scenes[1].Nodes, Is.EqualTo(new[] { 1 }));
            Assert.That(document.Skins[1].InverseBindMatrices, Is.EqualTo(1));
            Assert.That(document.Skins[1].Skeleton, Is.EqualTo(1));
            Assert.That(document.Skins[1].Joints, Is.EqualTo(new[] { 1 }));
            Assert.That(document.Textures[1].Sampler, Is.EqualTo(1));
            Assert.That(document.Textures[1].Source, Is.EqualTo(1));
            Assert.That(document.Textures[1].SelectedSource, Is.EqualTo(1));
            Assert.That(material.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsClearcoat]
                .MaterialsClearcoat!.ClearcoatTexture!.Index, Is.EqualTo(1));
            Assert.That(node.PropertyJson.TypedExtensions[
                GltfExtensionNames.ExtMeshGpuInstancing]
                .MeshGpuInstancing!.Attributes["TRANSLATION"], Is.EqualTo(1));
        });
    }

    private static GltfDocument CreateHoledDocument()
    {
        GltfDocument result = new GltfDocument();
        AddAccessorSlots(result);
        AddResourceSlots(result);
        AddBufferSlots(result);
        AddBufferViewSlots(result);
        AddCameraSlots(result);
        AddImageSlots(result);
        AddSamplerSlots(result);
        AddTextureSlots(result);
        AddMaterialSlots(result);
        AddMeshSlots(result);
        AddSkinSlots(result);
        AddNodeSlots(result);
        AddSceneSlots(result);
        AddAnimationSlots(result);
        result.HasDefaultScene = true;
        result.DefaultScene = 2;
        return result;
    }

    private static void AddAccessorSlots(GltfDocument document)
    {
        document.Accessors.Add(new GltfAccessor());
        document.Accessors.Add(null!);
        GltfAccessor accessor = new GltfAccessor
        {
            HasBufferView = true,
            BufferView = 2,
            Sparse = new GltfAccessorSparse()
        };
        accessor.Sparse.Indices.BufferView = 2;
        accessor.Sparse.Values.BufferView = 2;
        document.Accessors.Add(accessor);
    }

    private static void AddResourceSlots(GltfDocument document)
    {
        document.Resources.Resources.Add(new GltfResource { Bytes = [1] });
        document.Resources.Resources.Add(null!);
        GltfResource last = new GltfResource { Bytes = [3], CacheKey = "last" };
        document.Resources.Resources.Add(last);
        document.Resources.Cache["last"] = last;
    }

    private static void AddBufferSlots(GltfDocument document)
    {
        document.Buffers.Add(new GltfBuffer());
        document.Buffers.Add(null!);
        document.Buffers.Add(new GltfBuffer { HasResource = true, Resource = 2 });
    }

    private static void AddBufferViewSlots(GltfDocument document)
    {
        document.BufferViews.Add(new GltfBufferView());
        document.BufferViews.Add(null!);
        document.BufferViews.Add(new GltfBufferView { Buffer = 2 });
    }

    private static void AddCameraSlots(GltfDocument document)
    {
        document.Cameras.Add(new GltfCamera());
        document.Cameras.Add(null!);
        document.Cameras.Add(new GltfCamera());
    }

    private static void AddImageSlots(GltfDocument document)
    {
        document.Images.Add(new GltfImage());
        document.Images.Add(null!);
        document.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = 2,
            HasResource = true,
            Resource = 2
        });
    }

    private static void AddSamplerSlots(GltfDocument document)
    {
        document.Samplers.Add(new GltfSampler());
        document.Samplers.Add(null!);
        document.Samplers.Add(new GltfSampler());
    }

    private static void AddTextureSlots(GltfDocument document)
    {
        document.Textures.Add(new GltfTexture());
        document.Textures.Add(null!);
        document.Textures.Add(new GltfTexture
        {
            HasSampler = true,
            Sampler = 2,
            HasSource = true,
            Source = 2,
            HasSelectedSource = true,
            SelectedSource = 2
        });
    }

    private static void AddMaterialSlots(GltfDocument document)
    {
        document.Materials.Add(new GltfMaterial());
        document.Materials.Add(null!);
        GltfMaterial material = new GltfMaterial
        {
            PbrMetallicRoughness = new GltfPbrMetallicRoughness
            {
                BaseColorTexture = new GltfTextureInfo { Index = 2 }
            }
        };
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsClearcoat = new GltfMaterialsClearcoatExtension
        {
            ClearcoatTexture = new GltfTextureInfo { Index = 2 }
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsClearcoat]
            = typed;
        document.Materials.Add(material);
    }

    private static void AddMeshSlots(GltfDocument document)
    {
        document.Meshes.Add(new GltfMesh());
        document.Meshes.Add(null!);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = 2,
            HasMaterial = true,
            Material = 2
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = 2
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
    }

    private static void AddSkinSlots(GltfDocument document)
    {
        document.Skins.Add(new GltfSkin());
        document.Skins.Add(null!);
        GltfSkin skin = new GltfSkin
        {
            HasInverseBindMatrices = true,
            InverseBindMatrices = 2,
            HasSkeleton = true,
            Skeleton = 2
        };
        skin.Joints.Add(2);
        document.Skins.Add(skin);
    }

    private static void AddNodeSlots(GltfDocument document)
    {
        document.Nodes.Add(new GltfNode());
        document.Nodes.Add(null!);
        GltfNode node = new GltfNode
        {
            HasCamera = true,
            Camera = 2,
            HasSkin = true,
            Skin = 2,
            HasMesh = true,
            Mesh = 2
        };
        GltfMeshGpuInstancingExtension instancing = new GltfMeshGpuInstancingExtension();
        instancing.Attributes["TRANSLATION"] = 2;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MeshGpuInstancing = instancing;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshGpuInstancing] = typed;
        document.Nodes.Add(node);
    }

    private static void AddSceneSlots(GltfDocument document)
    {
        document.Scenes.Add(new GltfScene());
        document.Scenes.Add(null!);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(2);
        document.Scenes.Add(scene);
    }

    private static void AddAnimationSlots(GltfDocument document)
    {
        document.Animations.Add(new GltfAnimation());
        document.Animations.Add(null!);
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(new GltfAnimationSampler { Input = 2, Output = 2 });
        GltfAnimationChannel channel = new GltfAnimationChannel { Sampler = 0 };
        channel.Target.HasNode = true;
        channel.Target.Node = 2;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
    }
}
