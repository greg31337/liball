using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDocumentEditorCloneTests
{
    [Test]
    public void CloneDocument_DeepClonesCoreJsonExtensionsResourcesAndContainer()
    {
        GltfDocument source = CreatePopulatedDocument();
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfDocument clone = new GltfDocumentEditor().CloneDocument(source, options);
        GltfMaterialsPbrSpecularGlossinessExtension sourceExtension = source.Materials[0]
            .PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness]
            .MaterialsPbrSpecularGlossiness!;
        GltfMaterialsPbrSpecularGlossinessExtension cloneExtension = clone.Materials[0]
            .PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness]
            .MaterialsPbrSpecularGlossiness!;
        GltfWriteOptions writeOptions = new GltfWriteOptions { Validate = false };
        JsonNode sourceJson = JsonNode.Parse(
            new GltfWriter().WriteJsonText(source, writeOptions).JsonText!)!;
        JsonNode cloneJson = JsonNode.Parse(
            new GltfWriter().WriteJsonText(clone, writeOptions).JsonText!)!;

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(JsonNode.DeepEquals(cloneJson, sourceJson), Is.True);
            AssertCoreCollectionsAreIndependent(source, clone);
            Assert.That(clone.Asset, Is.Not.SameAs(source.Asset));
            Assert.That(clone.PropertyJson, Is.Not.SameAs(source.PropertyJson));
            Assert.That(clone.PropertyJson.Extensions, Is.Not.SameAs(
                source.PropertyJson.Extensions));
            Assert.That(clone.PropertyJson.Extensions["EXT_test"], Is.Not.SameAs(
                source.PropertyJson.Extensions["EXT_test"]));
            Assert.That(JsonNode.DeepEquals(
                clone.PropertyJson.Extensions["EXT_test"],
                source.PropertyJson.Extensions["EXT_test"]), Is.True);
            Assert.That(clone.PropertyJson.Extras, Is.Not.SameAs(source.PropertyJson.Extras));
            Assert.That(clone.PropertyJson.UnknownMembers["future"], Is.Not.SameAs(
                source.PropertyJson.UnknownMembers["future"]));
            Assert.That(cloneExtension, Is.Not.SameAs(sourceExtension));
            Assert.That(cloneExtension.DiffuseFactor, Is.Not.SameAs(
                sourceExtension.DiffuseFactor));
            Assert.That(cloneExtension.PropertyJson, Is.Not.SameAs(
                sourceExtension.PropertyJson));
            Assert.That(clone.Nodes[0].Translation, Is.Not.SameAs(
                source.Nodes[0].Translation));
            Assert.That(clone.Accessors[0].Maximum, Is.Not.SameAs(
                source.Accessors[0].Maximum));
            Assert.That(clone.Resources, Is.Not.SameAs(source.Resources));
            Assert.That(clone.Resources.Resources[0], Is.Not.SameAs(
                source.Resources.Resources[0]));
            Assert.That(clone.Resources.Resources[0].Bytes, Is.Not.SameAs(
                source.Resources.Resources[0].Bytes));
            Assert.That(clone.Resources.Cache["shared"], Is.SameAs(
                clone.Resources.Resources[0]));
            Assert.That(clone.Buffers[0].ResolvedBytes, Is.SameAs(
                clone.Resources.Resources[0].Bytes));
            Assert.That(clone.Buffers[0].PendingResourceRequest, Is.Not.SameAs(
                source.Buffers[0].PendingResourceRequest));
            Assert.That(clone.Buffers[0].PendingResourceRequest!.Policy.FileRoots,
                Is.Not.SameAs(source.Buffers[0].PendingResourceRequest!.Policy.FileRoots));
            Assert.That(clone.Images[0].ResolvedBytes, Is.Not.SameAs(
                source.Images[0].ResolvedBytes));
            Assert.That(clone.Textures[0].DecodedImage, Is.Not.SameAs(
                source.Textures[0].DecodedImage));
            Assert.That(clone.Textures[0].DecodedImage!.Bytes, Is.Not.SameAs(
                source.Textures[0].DecodedImage!.Bytes));
            Assert.That(clone.Container, Is.Not.SameAs(source.Container));
            Assert.That(clone.Container!.JsonBytes, Is.Not.SameAs(
                source.Container!.JsonBytes));
            Assert.That(clone.Container.BinBytes, Is.Not.SameAs(source.Container.BinBytes));
            Assert.That(clone.Container.TrailingBytes, Is.Not.SameAs(
                source.Container.TrailingBytes));
            Assert.That(clone.Container.UnknownChunks[0], Is.Not.SameAs(
                source.Container.UnknownChunks[0]));
            Assert.That(clone.Container.UnknownChunks[0].Data, Is.Not.SameAs(
                source.Container.UnknownChunks[0].Data));
            AssertPresenceStateIsEqual(source, clone);
        });

        clone.ExtensionsUsed.Add("EXT_clone_only");
        clone.Nodes[0].Translation[0] = 99.0;
        clone.PropertyJson.Extensions["EXT_test"]!["value"] = 9;
        cloneExtension.DiffuseFactor[0] = 0.1;
        clone.Resources.Resources[0].Bytes[0] = 90;
        clone.Container!.UnknownChunks[0].Data[0] = 80;

        Assert.Multiple(() =>
        {
            Assert.That(source.ExtensionsUsed, Does.Not.Contain("EXT_clone_only"));
            Assert.That(source.Nodes[0].Translation[0], Is.EqualTo(1.0));
            Assert.That(source.PropertyJson.Extensions["EXT_test"]!["value"]!
                .GetValue<int>(), Is.EqualTo(1));
            Assert.That(sourceExtension.DiffuseFactor[0], Is.EqualTo(0.8));
            Assert.That(source.Resources.Resources[0].Bytes[0], Is.EqualTo(1));
            Assert.That(source.Container!.UnknownChunks[0].Data[0], Is.EqualTo(7));
        });
    }

    [Test]
    public void CloneDocument_RespectsJsonAndResourceByteOwnershipOptions()
    {
        GltfDocument source = new GltfDocument();
        JsonObject extension = new JsonObject { ["value"] = 1 };
        source.PropertyJson.Extensions["EXT_test"] = extension;
        source.PropertyJson.HasExtras = true;
        source.PropertyJson.Extras = new JsonArray(1, 2);
        source.Nodes.Add(new GltfNode { Translation = [1.0, 2.0, 3.0] });
        GltfResource resource = new GltfResource { Bytes = [1, 2, 3] };
        source.Resources.Resources.Add(resource);
        source.Container = new GltfContainerData { JsonBytes = [4, 5, 6] };
        GltfEditOptions options = new GltfEditOptions
        {
            CloneJson = false,
            CloneResourceBytes = false,
            ValidateResult = false
        };

        GltfDocument clone = new GltfDocumentEditor().CloneDocument(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(clone.PropertyJson, Is.Not.SameAs(source.PropertyJson));
            Assert.That(clone.PropertyJson.Extensions, Is.Not.SameAs(
                source.PropertyJson.Extensions));
            Assert.That(clone.PropertyJson.Extensions["EXT_test"], Is.SameAs(extension));
            Assert.That(clone.PropertyJson.Extras, Is.SameAs(source.PropertyJson.Extras));
            Assert.That(clone.Resources.Resources[0], Is.Not.SameAs(resource));
            Assert.That(clone.Resources.Resources[0].Bytes, Is.SameAs(resource.Bytes));
            Assert.That(clone.Container!.JsonBytes, Is.SameAs(source.Container.JsonBytes));
            Assert.That(clone.Nodes[0].Translation, Is.Not.SameAs(
                source.Nodes[0].Translation));
        });
    }

    [Test]
    public void CloneDocument_ValidatesOnlyWhenRequested()
    {
        GltfDocument source = new GltfDocument();
        source.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 5 });
        GltfEditOptions uncheckedOptions = new GltfEditOptions { ValidateResult = false };

        GltfDocument clone = new GltfDocumentEditor().CloneDocument(
            source,
            uncheckedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(clone.Nodes[0].Mesh, Is.EqualTo(5));
            Assert.That(
                () => new GltfDocumentEditor().CloneDocument(
                    source,
                    new GltfEditOptions()),
                Throws.TypeOf<GltfValidationException>());
        });
    }

    private static GltfDocument CreatePopulatedDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Asset.Generator = "clone test";
        result.HasDefaultScene = true;
        result.DefaultScene = 0;
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness);
        result.ExtensionsRequired.Add(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness);
        result.PropertyJson.Extensions["EXT_test"] = new JsonObject { ["value"] = 1 };
        result.PropertyJson.HasExtras = true;
        result.PropertyJson.Extras = new JsonObject { ["owner"] = "source" };
        result.PropertyJson.UnknownMembers["future"] = new JsonArray(true, null);

        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.HasByteOffset = true;
        accessor.ByteOffset = 0;
        accessor.HasNormalized = true;
        accessor.Normalized = false;
        accessor.Count = 1;
        accessor.Maximum = [3.0];
        accessor.Minimum = [1.0];
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Count = 1;
        accessor.Sparse.Indices.BufferView = 1;
        accessor.Sparse.Values.BufferView = 2;
        result.Accessors.Add(accessor);

        GltfAnimation animation = new GltfAnimation();
        GltfAnimationSampler animationSampler = new GltfAnimationSampler();
        animationSampler.HasInterpolation = true;
        animationSampler.Input = 0;
        animationSampler.Output = 0;
        animation.Samplers.Add(animationSampler);
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Sampler = 0;
        channel.Target.HasNode = true;
        channel.Target.Node = 0;
        animation.Channels.Add(channel);
        result.Animations.Add(animation);

        GltfResource sharedResource = new GltfResource();
        sharedResource.Bytes = [1, 2, 3, 4, 0, 0, 7, 8, 9, 10, 11, 12];
        sharedResource.CacheKey = "shared";
        result.Resources.Resources.Add(sharedResource);
        result.Resources.Cache["shared"] = sharedResource;
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 12;
        buffer.Resolved = true;
        buffer.ResolvedBytes = sharedResource.Bytes;
        buffer.HasResource = true;
        buffer.Resource = 0;
        buffer.PendingResourceRequest = new GltfResourceRequest();
        buffer.PendingResourceRequest.SourceUri = "buffer.bin";
        buffer.PendingResourceRequest.Policy.FileRoots.Add("assets");
        result.Buffers.Add(buffer);

        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.HasByteOffset = true;
        view.ByteOffset = 0;
        view.ByteLength = 4;
        view.HasByteStride = true;
        view.ByteStride = 4;
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ArrayBuffer;
        result.BufferViews.Add(view);
        GltfBufferView sparseIndicesView = new GltfBufferView();
        sparseIndicesView.Buffer = 0;
        sparseIndicesView.HasByteOffset = true;
        sparseIndicesView.ByteOffset = 4;
        sparseIndicesView.ByteLength = 2;
        result.BufferViews.Add(sparseIndicesView);
        GltfBufferView sparseValuesView = new GltfBufferView();
        sparseValuesView.Buffer = 0;
        sparseValuesView.HasByteOffset = true;
        sparseValuesView.ByteOffset = 8;
        sparseValuesView.ByteLength = 4;
        result.BufferViews.Add(sparseValuesView);

        GltfCamera camera = new GltfCamera();
        camera.Perspective = new GltfPerspectiveCamera();
        camera.Perspective.HasAspectRatio = true;
        camera.Perspective.AspectRatio = 1.5;
        camera.Perspective.HasFarPlane = true;
        camera.Perspective.FarPlane = 100.0;
        camera.Perspective.NearPlane = 0.1;
        camera.Perspective.VerticalFieldOfView = 1.0;
        result.Cameras.Add(camera);

        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 0;
        image.MimeType = GltfMimeTypes.Png;
        image.Resolved = true;
        image.ResolvedBytes = [5, 6];
        image.HasResource = true;
        image.Resource = 0;
        image.PendingResourceRequest = new GltfResourceRequest();
        result.Images.Add(image);

        GltfMaterial material = new GltfMaterial();
        material.HasEmissiveFactor = true;
        material.EmissiveFactor = [0.1, 0.2, 0.3];
        material.HasAlphaMode = true;
        material.HasAlphaCutoff = true;
        material.HasDoubleSided = true;
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        material.PbrMetallicRoughness.HasBaseColorFactor = true;
        material.PbrMetallicRoughness.BaseColorTexture = new GltfTextureInfo();
        material.PbrMetallicRoughness.BaseColorTexture.Index = 0;
        GltfMaterialsPbrSpecularGlossinessExtension archived =
            new GltfMaterialsPbrSpecularGlossinessExtension();
        archived.HasDiffuseFactor = true;
        archived.DiffuseFactor = [0.8, 0.7, 0.6, 0.5];
        archived.PropertyJson.UnknownMembers["future"] = JsonValue.Create(4);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsPbrSpecularGlossiness = archived;
        material.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness] = typed;
        result.Materials.Add(material);

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasIndices = true;
        primitive.Indices = 0;
        primitive.HasMaterial = true;
        primitive.Material = 0;
        primitive.HasMode = true;
        primitive.Attributes.Add(new GltfAttribute { Semantic = "POSITION", Accessor = 0 });
        GltfMorphTarget target = new GltfMorphTarget();
        target.Attributes.Add(new GltfAttribute { Semantic = "POSITION", Accessor = 0 });
        primitive.Targets.Add(target);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        mesh.Weights = [0.5];
        result.Meshes.Add(mesh);

        GltfNode node = new GltfNode();
        node.HasCamera = true;
        node.Camera = 0;
        node.HasSkin = true;
        node.Skin = 0;
        node.HasMesh = true;
        node.Mesh = 0;
        node.HasMatrix = true;
        node.HasRotation = true;
        node.HasScale = true;
        node.HasTranslation = true;
        node.Translation = [1.0, 2.0, 3.0];
        node.Weights = [0.5];
        result.Nodes.Add(node);

        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.HasMinificationFilter = true;
        sampler.HasWrapS = true;
        sampler.HasWrapT = true;
        result.Samplers.Add(sampler);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        result.Scenes.Add(scene);
        GltfSkin skin = new GltfSkin();
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 0;
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        skin.Joints.Add(0);
        result.Skins.Add(skin);
        GltfTexture texture = new GltfTexture();
        texture.HasSampler = true;
        texture.Sampler = 0;
        texture.HasSource = true;
        texture.Source = 0;
        texture.HasSelectedSource = true;
        texture.SelectedSource = 0;
        texture.DecodedImage = new GltfImageResult { Bytes = [9, 10] };
        result.Textures.Add(texture);

        result.Container = new GltfContainerData();
        result.Container.Format = GltfContainerFormat.Glb;
        result.Container.JsonBytes = [1, 2];
        result.Container.HasBinChunk = true;
        result.Container.BinBytes = [3, 4];
        result.Container.TrailingBytes = [5, 6];
        result.Container.OriginalByteLength = 24;
        result.Container.UnknownChunks.Add(new GltfGlbChunk { Type = 8, Data = [7, 8] });
        return result;
    }

    private static void AssertCoreCollectionsAreIndependent(
        GltfDocument source,
        GltfDocument clone)
    {
        Assert.That(clone.Accessors, Is.Not.SameAs(source.Accessors));
        Assert.That(clone.Accessors[0], Is.Not.SameAs(source.Accessors[0]));
        Assert.That(clone.Animations, Is.Not.SameAs(source.Animations));
        Assert.That(clone.Animations[0], Is.Not.SameAs(source.Animations[0]));
        Assert.That(clone.Buffers, Is.Not.SameAs(source.Buffers));
        Assert.That(clone.Buffers[0], Is.Not.SameAs(source.Buffers[0]));
        Assert.That(clone.BufferViews, Is.Not.SameAs(source.BufferViews));
        Assert.That(clone.BufferViews[0], Is.Not.SameAs(source.BufferViews[0]));
        Assert.That(clone.Cameras, Is.Not.SameAs(source.Cameras));
        Assert.That(clone.Cameras[0], Is.Not.SameAs(source.Cameras[0]));
        Assert.That(clone.Images, Is.Not.SameAs(source.Images));
        Assert.That(clone.Images[0], Is.Not.SameAs(source.Images[0]));
        Assert.That(clone.Materials, Is.Not.SameAs(source.Materials));
        Assert.That(clone.Materials[0], Is.Not.SameAs(source.Materials[0]));
        Assert.That(clone.Meshes, Is.Not.SameAs(source.Meshes));
        Assert.That(clone.Meshes[0], Is.Not.SameAs(source.Meshes[0]));
        Assert.That(clone.Nodes, Is.Not.SameAs(source.Nodes));
        Assert.That(clone.Nodes[0], Is.Not.SameAs(source.Nodes[0]));
        Assert.That(clone.Samplers, Is.Not.SameAs(source.Samplers));
        Assert.That(clone.Samplers[0], Is.Not.SameAs(source.Samplers[0]));
        Assert.That(clone.Scenes, Is.Not.SameAs(source.Scenes));
        Assert.That(clone.Scenes[0], Is.Not.SameAs(source.Scenes[0]));
        Assert.That(clone.Skins, Is.Not.SameAs(source.Skins));
        Assert.That(clone.Skins[0], Is.Not.SameAs(source.Skins[0]));
        Assert.That(clone.Textures, Is.Not.SameAs(source.Textures));
        Assert.That(clone.Textures[0], Is.Not.SameAs(source.Textures[0]));
        Assert.That(clone.ExtensionsUsed, Is.Not.SameAs(source.ExtensionsUsed));
        Assert.That(clone.ExtensionsRequired, Is.Not.SameAs(source.ExtensionsRequired));
    }

    private static void AssertPresenceStateIsEqual(
        GltfDocument source,
        GltfDocument clone)
    {
        Assert.That(clone.HasDefaultScene, Is.EqualTo(source.HasDefaultScene));
        Assert.That(clone.Accessors[0].HasBufferView, Is.EqualTo(
            source.Accessors[0].HasBufferView));
        Assert.That(clone.Accessors[0].HasByteOffset, Is.EqualTo(
            source.Accessors[0].HasByteOffset));
        Assert.That(clone.Accessors[0].HasNormalized, Is.EqualTo(
            source.Accessors[0].HasNormalized));
        Assert.That(clone.Materials[0].HasEmissiveFactor, Is.EqualTo(
            source.Materials[0].HasEmissiveFactor));
        Assert.That(clone.Materials[0].HasAlphaMode, Is.EqualTo(
            source.Materials[0].HasAlphaMode));
        Assert.That(clone.Materials[0].HasAlphaCutoff, Is.EqualTo(
            source.Materials[0].HasAlphaCutoff));
        Assert.That(clone.Materials[0].HasDoubleSided, Is.EqualTo(
            source.Materials[0].HasDoubleSided));
        Assert.That(clone.Nodes[0].HasMatrix, Is.EqualTo(source.Nodes[0].HasMatrix));
        Assert.That(clone.Nodes[0].HasRotation, Is.EqualTo(source.Nodes[0].HasRotation));
        Assert.That(clone.Nodes[0].HasScale, Is.EqualTo(source.Nodes[0].HasScale));
        Assert.That(clone.Nodes[0].HasTranslation, Is.EqualTo(
            source.Nodes[0].HasTranslation));
        Assert.That(clone.Samplers[0].HasWrapS, Is.EqualTo(source.Samplers[0].HasWrapS));
        Assert.That(clone.Samplers[0].HasWrapT, Is.EqualTo(source.Samplers[0].HasWrapT));
        Assert.That(clone.Container!.HasBinChunk, Is.EqualTo(source.Container!.HasBinChunk));
    }
}
