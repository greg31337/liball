namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDocumentEditorMergeTests
{
    [Test]
    public void MergeDocument_AppendsEveryCollectionAndResourceInSourceOrder()
    {
        GltfDocument first = CreateReferencedDocument("first", 1);
        GltfDocument second = CreateReferencedDocument("second", 2);
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfMergeResult result = new GltfDocumentEditor().MergeDocument(
            [first, second],
            options);
        GltfDocument merged = result.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.SourceRemaps, Has.Count.EqualTo(2));
            Assert.That(merged.Accessors.Select(value => value.Name),
                Is.EqualTo(new[] { "first accessor", "second accessor" }));
            Assert.That(merged.Animations.Select(value => value.Name),
                Is.EqualTo(new[] { "first animation", "second animation" }));
            Assert.That(merged.Buffers.Select(value => value.Name),
                Is.EqualTo(new[] { "first buffer", "second buffer" }));
            Assert.That(merged.BufferViews.Select(value => value.Name),
                Is.EqualTo(new[] { "first view", "second view" }));
            Assert.That(merged.Cameras.Select(value => value.Name),
                Is.EqualTo(new[] { "first camera", "second camera" }));
            Assert.That(merged.Images.Select(value => value.Name),
                Is.EqualTo(new[] { "first image", "second image" }));
            Assert.That(merged.Materials.Select(value => value.Name),
                Is.EqualTo(new[] { "first material", "second material" }));
            Assert.That(merged.Meshes.Select(value => value.Name),
                Is.EqualTo(new[] { "first mesh", "second mesh" }));
            Assert.That(merged.Nodes.Select(value => value.Name),
                Is.EqualTo(new[]
                {
                    "first node", "first joint", "second node", "second joint"
                }));
            Assert.That(merged.Samplers.Select(value => value.Name),
                Is.EqualTo(new[] { "first sampler", "second sampler" }));
            Assert.That(merged.Scenes.Select(value => value.Name),
                Is.EqualTo(new[] { "first scene", "second scene" }));
            Assert.That(merged.Skins.Select(value => value.Name),
                Is.EqualTo(new[] { "first skin", "second skin" }));
            Assert.That(merged.Textures.Select(value => value.Name),
                Is.EqualTo(new[] { "first texture", "second texture" }));
            Assert.That(merged.Resources.Resources.Select(value => value.Bytes[0]),
                Is.EqualTo(new byte[] { 1, 11, 2, 12 }));
            Assert.That(merged.ExtensionsUsed,
                Is.EqualTo(first.ExtensionsUsed));
            Assert.That(merged.Container, Is.Null);
            Assert.That(merged.Asset.Generator, Is.EqualTo("first generator"));
            Assert.That(merged.HasDefaultScene, Is.True);
            Assert.That(merged.DefaultScene, Is.EqualTo(0));
        });

        GltfIndexRemap secondRemap = result.SourceRemaps[1];
        Assert.Multiple(() =>
        {
            Assert.That(secondRemap.Accessors[0], Is.EqualTo(1));
            Assert.That(secondRemap.Animations[0], Is.EqualTo(1));
            Assert.That(secondRemap.Buffers[0], Is.EqualTo(1));
            Assert.That(secondRemap.BufferViews[0], Is.EqualTo(1));
            Assert.That(secondRemap.Cameras[0], Is.EqualTo(1));
            Assert.That(secondRemap.Images[0], Is.EqualTo(1));
            Assert.That(secondRemap.Materials[0], Is.EqualTo(1));
            Assert.That(secondRemap.Meshes[0], Is.EqualTo(1));
            Assert.That(secondRemap.Nodes[0], Is.EqualTo(2));
            Assert.That(secondRemap.Nodes[1], Is.EqualTo(3));
            Assert.That(secondRemap.Samplers[0], Is.EqualTo(1));
            Assert.That(secondRemap.Scenes[0], Is.EqualTo(1));
            Assert.That(secondRemap.Skins[0], Is.EqualTo(1));
            Assert.That(secondRemap.Textures[0], Is.EqualTo(1));
            Assert.That(secondRemap.Resources[0], Is.EqualTo(2));
            Assert.That(secondRemap.Resources[1], Is.EqualTo(3));
            Assert.That(secondRemap.PunctualLights[0], Is.EqualTo(1));
            Assert.That(secondRemap.MaterialVariants[0], Is.EqualTo(1));
            Assert.That(secondRemap.XmpJsonLdPackets[0], Is.EqualTo(1));
        });
    }

    [Test]
    public void MergeDocument_RemapsCoreAndRegisteredExtensionReferences()
    {
        GltfDocument first = CreateReferencedDocument("first", 1);
        GltfDocument second = CreateReferencedDocument("second", 2);
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfDocument merged = new GltfDocumentEditor().MergeDocument(
            [first, second],
            options).Document!;
        GltfAccessor accessor = merged.Accessors[1];
        GltfAnimation animation = merged.Animations[1];
        GltfBuffer buffer = merged.Buffers[1];
        GltfBufferView view = merged.BufferViews[1];
        GltfImage image = merged.Images[1];
        GltfMaterial material = merged.Materials[1];
        GltfMeshPrimitive primitive = merged.Meshes[1].Primitives[0];
        GltfNode node = merged.Nodes[2];
        GltfSkin skin = merged.Skins[1];
        GltfTexture texture = merged.Textures[1];

        Assert.Multiple(() =>
        {
            Assert.That(accessor.BufferView, Is.EqualTo(1));
            Assert.That(accessor.Sparse!.Indices.BufferView, Is.EqualTo(1));
            Assert.That(accessor.Sparse.Values.BufferView, Is.EqualTo(1));
            Assert.That(animation.Samplers[0].Input, Is.EqualTo(1));
            Assert.That(animation.Samplers[0].Output, Is.EqualTo(1));
            Assert.That(animation.Channels[0].Target.Node, Is.EqualTo(2));
            Assert.That(buffer.Resource, Is.EqualTo(2));
            Assert.That(view.Buffer, Is.EqualTo(1));
            Assert.That(image.BufferView, Is.EqualTo(1));
            Assert.That(image.Resource, Is.EqualTo(3));
            Assert.That(material.PbrMetallicRoughness!.BaseColorTexture!.Index,
                Is.EqualTo(1));
            Assert.That(primitive.Attributes[0].Accessor, Is.EqualTo(1));
            Assert.That(primitive.Indices, Is.EqualTo(1));
            Assert.That(primitive.Material, Is.EqualTo(1));
            Assert.That(primitive.Targets[0].Attributes[0].Accessor, Is.EqualTo(1));
            Assert.That(node.Children, Is.EqualTo(new[] { 3 }));
            Assert.That(node.Camera, Is.EqualTo(1));
            Assert.That(node.Skin, Is.EqualTo(1));
            Assert.That(node.Mesh, Is.EqualTo(1));
            Assert.That(merged.Scenes[1].Nodes, Is.EqualTo(new[] { 2 }));
            Assert.That(skin.InverseBindMatrices, Is.EqualTo(1));
            Assert.That(skin.Skeleton, Is.EqualTo(3));
            Assert.That(skin.Joints, Is.EqualTo(new[] { 3 }));
            Assert.That(texture.Sampler, Is.EqualTo(1));
            Assert.That(texture.Source, Is.EqualTo(1));
            Assert.That(texture.SelectedSource, Is.EqualTo(1));
        });

        AssertRegisteredExtensionReferences(merged);
    }

    [Test]
    public void MergeDocument_OwnsMergedDataAndRetainsCollidingResourcesDeterministically()
    {
        GltfDocument first = CreateReferencedDocument("first", 1);
        GltfDocument second = CreateReferencedDocument("second", 2);
        GltfEditOptions options = new GltfEditOptions { ValidateResult = false };

        GltfDocument merged = new GltfDocumentEditor().MergeDocument(
            [first, second],
            options).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(merged.Nodes[0], Is.Not.SameAs(first.Nodes[0]));
            Assert.That(merged.Nodes[2], Is.Not.SameAs(second.Nodes[0]));
            Assert.That(merged.Resources.Resources[0], Is.Not.SameAs(
                first.Resources.Resources[0]));
            Assert.That(merged.Resources.Resources[2], Is.Not.SameAs(
                second.Resources.Resources[0]));
            Assert.That(merged.Resources.Cache.Keys,
                Is.EquivalentTo(new[]
                {
                    "buffer", "image", "buffer#1", "image#1"
                }));
            Assert.That(merged.Resources.Cache["buffer"], Is.SameAs(
                merged.Resources.Resources[0]));
            Assert.That(merged.Resources.Cache["buffer#1"], Is.SameAs(
                merged.Resources.Resources[2]));
        });

        merged.Nodes[2].Name = "changed";
        merged.Resources.Resources[2].Bytes[0] = 99;
        GetRootLights(merged).Lights[1].Name = "changed light";

        Assert.Multiple(() =>
        {
            Assert.That(second.Nodes[0].Name, Is.EqualTo("second node"));
            Assert.That(second.Resources.Resources[0].Bytes[0], Is.EqualTo(2));
            Assert.That(GetRootLights(second).Lights[0].Name,
                Is.EqualTo("second light"));
        });
    }

    [Test]
    public void MergeDocument_ValidatesOnlyWhenRequested()
    {
        GltfDocument invalid = new GltfDocument();
        invalid.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 4 });
        GltfEditOptions uncheckedOptions = new GltfEditOptions { ValidateResult = false };

        GltfMergeResult uncheckedResult = new GltfDocumentEditor().MergeDocument(
            [invalid],
            uncheckedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(uncheckedResult.Document!.Nodes[0].Mesh, Is.EqualTo(4));
            Assert.That(
                () => new GltfDocumentEditor().MergeDocument(
                    [invalid],
                    new GltfEditOptions()),
                Throws.TypeOf<GltfValidationException>());
        });
    }

    private static void AssertRegisteredExtensionReferences(GltfDocument merged)
    {
        GltfMaterial material = merged.Materials[1];
        GltfMeshPrimitive primitive = merged.Meshes[1].Primitives[0];
        GltfNode node = merged.Nodes[2];
        GltfBufferView view = merged.BufferViews[1];
        GltfTexture texture = merged.Textures[1];
        GltfAnimationTarget target = merged.Animations[1].Channels[1].Target;
        GltfMaterialsClearcoatExtension clearcoat = material.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsClearcoat].MaterialsClearcoat!;
        GltfMaterialsVariantsPrimitiveExtension variants = primitive.PropertyJson
            .TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            .MaterialsVariantsPrimitive!;

        Assert.Multiple(() =>
        {
            Assert.That(clearcoat.ClearcoatTexture!.Index, Is.EqualTo(1));
            Assert.That(primitive.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrDracoMeshCompression]
                .DracoMeshCompression!.BufferView, Is.EqualTo(1));
            Assert.That(variants.Mappings[0].Material, Is.EqualTo(1));
            Assert.That(variants.Mappings[0].Variants, Is.EqualTo(new[] { 1 }));
            Assert.That(node.PropertyJson.TypedExtensions[
                GltfExtensionNames.ExtMeshGpuInstancing]
                .MeshGpuInstancing!.Attributes["TRANSLATION"], Is.EqualTo(1));
            Assert.That(node.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrLightsPunctual]
                .PunctualLightNode!.Light, Is.EqualTo(1));
            Assert.That(target.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrAnimationPointer]
                .AnimationPointer!.Pointer, Is.EqualTo("/nodes/2/translation"));
            Assert.That(view.PropertyJson.TypedExtensions[
                GltfExtensionNames.ExtMeshoptCompression]
                .ExtMeshoptCompressionBufferView!.Buffer, Is.EqualTo(1));
            Assert.That(texture.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureBasisu]
                .TextureBasisu!.Source, Is.EqualTo(1));
            Assert.That(texture.PropertyJson.TypedExtensions[
                GltfExtensionNames.ExtTextureWebp]
                .TextureWebp!.Source, Is.EqualTo(1));
            Assert.That(material.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrXmpJsonLd]
                .XmpJsonLdAssignment!.Packet, Is.EqualTo(1));
            Assert.That(GetRootLights(merged).Lights, Has.Count.EqualTo(2));
            Assert.That(GetRootVariants(merged).Variants, Has.Count.EqualTo(2));
            Assert.That(GetRootXmp(merged).Packets, Has.Count.EqualTo(2));
        });
    }

    private static GltfDocument CreateReferencedDocument(string prefix, byte marker)
    {
        GltfDocument result = new GltfDocument();
        result.Asset.Generator = prefix + " generator";
        result.HasDefaultScene = true;
        result.DefaultScene = 0;
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsVariants);
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrXmpJsonLd);
        result.Container = new GltfContainerData { JsonBytes = [marker] };

        GltfResource bufferResource = new GltfResource();
        bufferResource.Bytes = [marker, 0, 0, 0];
        bufferResource.CacheKey = "buffer";
        GltfResource imageResource = new GltfResource();
        imageResource.Bytes = [(byte)(marker + 10)];
        imageResource.CacheKey = "image";
        result.Resources.Resources.Add(bufferResource);
        result.Resources.Resources.Add(imageResource);
        result.Resources.Cache["buffer"] = bufferResource;
        result.Resources.Cache["image"] = imageResource;

        GltfBuffer buffer = new GltfBuffer();
        buffer.Name = prefix + " buffer";
        buffer.ByteLength = 4;
        buffer.HasResource = true;
        buffer.Resource = 0;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bufferResource.Bytes;
        result.Buffers.Add(buffer);

        GltfBufferView view = new GltfBufferView();
        view.Name = prefix + " view";
        view.Buffer = 0;
        view.ByteLength = 4;
        GltfTypedExtensionData meshopt = new GltfTypedExtensionData();
        meshopt.ExtMeshoptCompressionBufferView =
            new GltfMeshoptCompressionBufferViewExtension
            {
                Buffer = 0,
                ByteLength = 4,
                ByteStride = 4,
                Count = 1,
                Mode = "ATTRIBUTES"
            };
        view.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshoptCompression] = meshopt;
        result.BufferViews.Add(view);

        GltfAccessor accessor = new GltfAccessor();
        accessor.Name = prefix + " accessor";
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.Count = 1;
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Count = 1;
        accessor.Sparse.Indices.BufferView = 0;
        accessor.Sparse.Values.BufferView = 0;
        result.Accessors.Add(accessor);

        GltfCamera camera = new GltfCamera();
        camera.Name = prefix + " camera";
        camera.Perspective = new GltfPerspectiveCamera
        {
            VerticalFieldOfView = 0.5,
            NearPlane = 0.1
        };
        result.Cameras.Add(camera);

        GltfImage image = new GltfImage();
        image.Name = prefix + " image";
        image.HasBufferView = true;
        image.BufferView = 0;
        image.MimeType = GltfMimeTypes.Png;
        image.HasResource = true;
        image.Resource = 1;
        image.Resolved = true;
        image.ResolvedBytes = imageResource.Bytes;
        result.Images.Add(image);

        GltfSampler sampler = new GltfSampler { Name = prefix + " sampler" };
        result.Samplers.Add(sampler);
        GltfTexture texture = new GltfTexture();
        texture.Name = prefix + " texture";
        texture.HasSampler = true;
        texture.Sampler = 0;
        texture.HasSource = true;
        texture.Source = 0;
        texture.HasSelectedSource = true;
        texture.SelectedSource = 0;
        AddImageExtensions(texture);
        result.Textures.Add(texture);

        GltfMaterial material = new GltfMaterial();
        material.Name = prefix + " material";
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        material.PbrMetallicRoughness.BaseColorTexture = new GltfTextureInfo { Index = 0 };
        AddMaterialExtensions(material);
        result.Materials.Add(material);

        GltfMesh mesh = new GltfMesh { Name = prefix + " mesh" };
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(new GltfAttribute { Semantic = "POSITION", Accessor = 0 });
        primitive.HasIndices = true;
        primitive.Indices = 0;
        primitive.HasMaterial = true;
        primitive.Material = 0;
        GltfMorphTarget morph = new GltfMorphTarget();
        morph.Attributes.Add(new GltfAttribute { Semantic = "POSITION", Accessor = 0 });
        primitive.Targets.Add(morph);
        AddPrimitiveExtensions(primitive);
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);

        GltfSkin skin = new GltfSkin();
        skin.Name = prefix + " skin";
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 0;
        skin.HasSkeleton = true;
        skin.Skeleton = 1;
        skin.Joints.Add(1);
        result.Skins.Add(skin);

        GltfNode node = new GltfNode();
        node.Name = prefix + " node";
        node.Children.Add(1);
        node.HasCamera = true;
        node.Camera = 0;
        node.HasSkin = true;
        node.Skin = 0;
        node.HasMesh = true;
        node.Mesh = 0;
        AddNodeExtensions(node);
        result.Nodes.Add(node);
        result.Nodes.Add(new GltfNode { Name = prefix + " joint" });

        GltfScene scene = new GltfScene { Name = prefix + " scene" };
        scene.Nodes.Add(0);
        result.Scenes.Add(scene);
        AddAnimation(result, prefix);
        AddRootExtensions(result, prefix);
        return result;
    }

    private static void AddAnimation(GltfDocument document, string prefix)
    {
        GltfAnimation animation = new GltfAnimation { Name = prefix + " animation" };
        animation.Samplers.Add(new GltfAnimationSampler { Input = 0, Output = 0 });
        GltfAnimationChannel nodeChannel = new GltfAnimationChannel { Sampler = 0 };
        nodeChannel.Target.HasNode = true;
        nodeChannel.Target.Node = 0;
        animation.Channels.Add(nodeChannel);
        GltfAnimationChannel pointerChannel = new GltfAnimationChannel { Sampler = 0 };
        pointerChannel.Target.HasNode = false;
        pointerChannel.Target.Path = GltfAnimationPath.Pointer;
        GltfTypedExtensionData pointer = new GltfTypedExtensionData();
        pointer.AnimationPointer = new GltfAnimationPointerExtension
        {
            Pointer = "/nodes/0/translation"
        };
        pointerChannel.Target.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrAnimationPointer] = pointer;
        animation.Channels.Add(pointerChannel);
        document.Animations.Add(animation);
    }

    private static void AddMaterialExtensions(GltfMaterial material)
    {
        GltfTypedExtensionData clearcoat = new GltfTypedExtensionData();
        clearcoat.MaterialsClearcoat = new GltfMaterialsClearcoatExtension
        {
            ClearcoatTexture = new GltfTextureInfo { Index = 0 }
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsClearcoat]
            = clearcoat;
        GltfTypedExtensionData assignment = new GltfTypedExtensionData();
        assignment.XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
        {
            HasPacket = true,
            Packet = 0
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd]
            = assignment;
    }

    private static void AddImageExtensions(GltfTexture texture)
    {
        GltfTypedExtensionData basisu = new GltfTypedExtensionData();
        basisu.TextureBasisu = new GltfTextureBasisuExtension
        {
            HasSource = true,
            Source = 0
        };
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.KhrTextureBasisu] = basisu;
        GltfTypedExtensionData webp = new GltfTypedExtensionData();
        webp.TextureWebp = new GltfTextureWebpExtension
        {
            HasSource = true,
            Source = 0
        };
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.ExtTextureWebp] = webp;
    }

    private static void AddPrimitiveExtensions(GltfMeshPrimitive primitive)
    {
        GltfTypedExtensionData draco = new GltfTypedExtensionData();
        draco.DracoMeshCompression = new GltfDracoMeshCompressionExtension
        {
            BufferView = 0
        };
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = draco;
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping { Material = 0 };
        mapping.Variants.Add(0);
        GltfMaterialsVariantsPrimitiveExtension variants =
            new GltfMaterialsVariantsPrimitiveExtension();
        variants.Mappings.Add(mapping);
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariantsPrimitive = variants;
        primitive.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;
    }

    private static void AddNodeExtensions(GltfNode node)
    {
        GltfTypedExtensionData light = new GltfTypedExtensionData();
        light.PunctualLightNode = new GltfPunctualLightNodeExtension { Light = 0 };
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = light;
        GltfMeshGpuInstancingExtension instancing = new GltfMeshGpuInstancingExtension();
        instancing.Attributes["TRANSLATION"] = 0;
        GltfTypedExtensionData typedInstancing = new GltfTypedExtensionData();
        typedInstancing.MeshGpuInstancing = instancing;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshGpuInstancing]
            = typedInstancing;
    }

    private static void AddRootExtensions(GltfDocument document, string prefix)
    {
        GltfLightsPunctualExtension lights = new GltfLightsPunctualExtension();
        lights.Lights.Add(new GltfPunctualLight
        {
            Name = prefix + " light",
            Type = "point"
        });
        GltfTypedExtensionData typedLights = new GltfTypedExtensionData();
        typedLights.LightsPunctual = lights;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            = typedLights;

        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        variants.Variants.Add(new GltfMaterialVariant { Name = prefix + " variant" });
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariants = variants;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;

        GltfXmpJsonLdExtension xmp = new GltfXmpJsonLdExtension { HasPackets = true };
        GltfXmpJsonLdPacket packet = new GltfXmpJsonLdPacket { HasContext = true };
        packet.Context["dc"] = "https://purl.org/dc/elements/1.1/";
        xmp.Packets.Add(packet);
        GltfTypedExtensionData typedXmp = new GltfTypedExtensionData();
        typedXmp.XmpJsonLd = xmp;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typedXmp;
    }

    private static GltfLightsPunctualExtension GetRootLights(GltfDocument document)
    {
        return document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            .LightsPunctual!;
    }

    private static GltfMaterialsVariantsExtension GetRootVariants(GltfDocument document)
    {
        return document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            .MaterialsVariants!;
    }

    private static GltfXmpJsonLdExtension GetRootXmp(GltfDocument document)
    {
        return document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd]
            .XmpJsonLd!;
    }
}
