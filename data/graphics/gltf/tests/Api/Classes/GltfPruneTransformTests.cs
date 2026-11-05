using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfPruneTransformTests
{
    [Test]
    public void Prune_KeepsSelectedSceneClosureAndReportsExactRemovals()
    {
        GltfDocument source = CreateTwoSceneDocument();
        GltfTransformStep step = CreatePruneStep(1);
        step.Settings["keepAnimations"] = false;
        step.Settings["keepSkins"] = false;
        step.Settings["keepExtras"] = false;
        GltfTransformOptions options = CreateUncheckedOptions();

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan { Steps = [step] },
            options);
        GltfDocument pruned = report.Document!;
        GltfTransformStepResult result = report.Steps[0];

        AssertSingleReachableChain(pruned);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Remap.Accessors[1], Is.EqualTo(0));
            Assert.That(result.Remap.Buffers[1], Is.EqualTo(0));
            Assert.That(result.Remap.BufferViews[1], Is.EqualTo(0));
            Assert.That(result.Remap.Images[1], Is.EqualTo(0));
            Assert.That(result.Remap.Materials[1], Is.EqualTo(0));
            Assert.That(result.Remap.Meshes[1], Is.EqualTo(0));
            Assert.That(result.Remap.Nodes[1], Is.EqualTo(0));
            Assert.That(result.Remap.Resources[1], Is.EqualTo(0));
            Assert.That(result.Remap.Samplers[1], Is.EqualTo(0));
            Assert.That(result.Remap.Scenes[1], Is.EqualTo(0));
            Assert.That(result.Remap.Skins[1], Is.EqualTo(0));
            Assert.That(result.Remap.Textures[1], Is.EqualTo(0));
            Assert.That(result.Remap.PunctualLights[1], Is.EqualTo(0));
            Assert.That(result.Remap.MaterialVariants[1], Is.EqualTo(0));
            Assert.That(result.Remap.XmpJsonLdPackets[1], Is.EqualTo(0));
            Assert.That(result.ChangedObjects, Does.Contain("accessors/0"));
            Assert.That(result.ChangedObjects, Does.Contain("animations/0"));
            Assert.That(result.ChangedObjects, Does.Contain("animations/1"));
            Assert.That(result.ChangedObjects, Does.Contain("nodes/0"));
            Assert.That(result.ChangedObjects, Does.Contain("scenes/0"));
            Assert.That(result.ChangedObjects,
                Does.Contain("extensions/KHR_lights_punctual/lights/0"));
            Assert.That(result.ChangedObjects,
                Does.Contain("extensions/KHR_materials_variants/variants/0"));
            Assert.That(result.ChangedObjects, Does.Contain("xmpJsonLdPackets/0"));
            Assert.That(result.ChangedObjects, Does.Contain("extras"));
            Assert.That(result.ChangedResources,
                Is.EqualTo(new[] { "resources/0", "cache/resource-0" }));
            Assert.That(pruned.PropertyJson.HasExtras, Is.False);
            Assert.That(pruned.Nodes[0].PropertyJson.HasExtras, Is.False);
            Assert.That(source.Scenes, Has.Count.EqualTo(2));
            Assert.That(source.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(source.Nodes[1].PropertyJson.HasExtras, Is.True);
        });
    }

    [Test]
    public void Prune_RetentionFlagsControlAnimationSkinExtensionAndExtrasReachability()
    {
        GltfDocument source = CreateRetentionDocument();
        GltfTransformStep retainedStep = CreatePruneStep(0);
        GltfTransformOptions options = CreateUncheckedOptions();
        options.RequireSafeUnknownExtensions = false;

        GltfDocument retained = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan { Steps = [retainedStep] },
            options).Document!;

        GltfTransformStep removedStep = CreatePruneStep(0);
        removedStep.Settings["keepAnimations"] = false;
        removedStep.Settings["keepSkins"] = false;
        removedStep.Settings["keepExtensions"] = false;
        removedStep.Settings["keepExtras"] = false;
        GltfTransformOptions unsafeOptions = CreateUncheckedOptions();
        unsafeOptions.RequireSafeUnknownExtensions = false;
        GltfDocument removed = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan { Steps = [removedStep] },
            unsafeOptions).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(retained.Animations, Has.Count.EqualTo(1));
            Assert.That(retained.Skins, Has.Count.EqualTo(1));
            Assert.That(retained.Nodes, Has.Count.EqualTo(2));
            Assert.That(retained.Accessors, Has.Count.EqualTo(1));
            Assert.That(retained.Buffers, Has.Count.EqualTo(1));
            Assert.That(retained.BufferViews, Has.Count.EqualTo(1));
            Assert.That(retained.Meshes[0].Primitives[0].PropertyJson.TypedExtensions,
                Does.ContainKey(GltfExtensionNames.KhrDracoMeshCompression));
            Assert.That(retained.Nodes[0].PropertyJson.TypedExtensions, Is.Not.Empty);
            Assert.That(retained.Nodes[0].PropertyJson.HasExtras, Is.True);
            Assert.That(removed.Animations, Is.Empty);
            Assert.That(removed.Skins, Is.Empty);
            Assert.That(removed.Nodes, Has.Count.EqualTo(1));
            Assert.That(removed.Accessors, Is.Empty);
            Assert.That(removed.Buffers, Is.Empty);
            Assert.That(removed.BufferViews, Is.Empty);
            Assert.That(removed.Meshes[0].Primitives[0].PropertyJson.TypedExtensions,
                Is.Empty);
            Assert.That(removed.Nodes[0].PropertyJson.TypedExtensions, Is.Empty);
            Assert.That(removed.Nodes[0].PropertyJson.Extensions, Is.Empty);
            Assert.That(removed.Nodes[0].PropertyJson.HasExtras, Is.False);
            Assert.That(removed.ExtensionsUsed, Is.Empty);
            Assert.That(source.Animations, Has.Count.EqualTo(1));
            Assert.That(source.Nodes[0].PropertyJson.TypedExtensions, Is.Not.Empty);
        });
    }

    [Test]
    public void Prune_DefaultValidationAcceptsASelectedValidScene()
    {
        GltfDocument source = new GltfDocument();
        source.Scenes.Add(new GltfScene { Name = "remove" });
        source.Scenes.Add(new GltfScene { Name = "keep" });
        source.HasDefaultScene = true;
        source.DefaultScene = 1;
        GltfTransformStep step = CreatePruneStep(1);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            new GltfTransformPlan { Steps = [step] },
            new GltfTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.Document!.Scenes, Has.Count.EqualTo(1));
            Assert.That(report.Document.Scenes[0].Name, Is.EqualTo("keep"));
            Assert.That(report.Document.DefaultScene, Is.EqualTo(0));
            Assert.That(source.Scenes, Has.Count.EqualTo(2));
            Assert.That(source.DefaultScene, Is.EqualTo(1));
        });
    }

    [Test]
    public void Prune_InvalidSceneSelectionFailsBeforeChangingSource()
    {
        GltfDocument source = new GltfDocument();
        source.Scenes.Add(new GltfScene { Name = "only" });
        GltfTransformStep step = CreatePruneStep(2);
        GltfTransformOptions options = CreateUncheckedOptions();

        Assert.That(
            () => new GltfTransformer().TransformDocument(
                source,
                new GltfTransformPlan { Steps = [step] },
                options),
            Throws.TypeOf<ArgumentException>()
                .With.Message.Contains("does not resolve to a scene"));
        Assert.That(source.Scenes, Has.Count.EqualTo(1));
        Assert.That(source.Scenes[0].Name, Is.EqualTo("only"));
    }

    private static void AssertSingleReachableChain(GltfDocument document)
    {
        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        GltfPunctualLightNodeExtension light = document.Nodes[0].PropertyJson
            .TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            .PunctualLightNode!;
        GltfMaterialsVariantsPrimitiveExtension variants = primitive.PropertyJson
            .TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            .MaterialsVariantsPrimitive!;
        GltfXmpJsonLdAssignmentExtension xmp = document.Materials[0].PropertyJson
            .TypedExtensions[GltfExtensionNames.KhrXmpJsonLd]
            .XmpJsonLdAssignment!;

        Assert.Multiple(() =>
        {
            Assert.That(document.Accessors, Has.Count.EqualTo(1));
            Assert.That(document.Animations, Is.Empty);
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Images, Has.Count.EqualTo(1));
            Assert.That(document.Materials, Has.Count.EqualTo(1));
            Assert.That(document.Meshes, Has.Count.EqualTo(1));
            Assert.That(document.Nodes, Has.Count.EqualTo(1));
            Assert.That(document.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(document.Samplers, Has.Count.EqualTo(1));
            Assert.That(document.Scenes, Has.Count.EqualTo(1));
            Assert.That(document.Skins, Has.Count.EqualTo(1));
            Assert.That(document.Textures, Has.Count.EqualTo(1));
            Assert.That(document.DefaultScene, Is.EqualTo(0));
            Assert.That(document.Accessors[0].BufferView, Is.EqualTo(0));
            Assert.That(document.BufferViews[0].Buffer, Is.EqualTo(0));
            Assert.That(document.Buffers[0].Resource, Is.EqualTo(0));
            Assert.That(document.Images[0].BufferView, Is.EqualTo(0));
            Assert.That(document.Textures[0].Source, Is.EqualTo(0));
            Assert.That(document.Textures[0].Sampler, Is.EqualTo(0));
            Assert.That(document.Materials[0].PbrMetallicRoughness!
                .BaseColorTexture!.Index, Is.EqualTo(0));
            Assert.That(primitive.Attributes[0].Accessor, Is.EqualTo(0));
            Assert.That(primitive.Material, Is.EqualTo(0));
            Assert.That(document.Nodes[0].Mesh, Is.EqualTo(0));
            Assert.That(document.Nodes[0].Skin, Is.EqualTo(0));
            Assert.That(document.Skins[0].Joints, Is.EqualTo(new[] { 0 }));
            Assert.That(document.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
            Assert.That(light.Light, Is.EqualTo(0));
            Assert.That(variants.Mappings[0].Material, Is.EqualTo(0));
            Assert.That(variants.Mappings[0].Variants, Is.EqualTo(new[] { 0 }));
            Assert.That(xmp.Packet, Is.EqualTo(0));
            Assert.That(GetRootLights(document).Lights[0].Name, Is.EqualTo("light-1"));
            Assert.That(GetRootVariants(document).Variants[0].Name,
                Is.EqualTo("variant-1"));
            Assert.That(GetRootXmp(document).Packets, Has.Count.EqualTo(1));
            Assert.That(document.Resources.Cache.Keys, Is.EqualTo(new[] { "resource-1" }));
        });
    }

    private static GltfDocument CreateTwoSceneDocument()
    {
        GltfDocument result = new GltfDocument();
        AddRootExtensions(result);
        AddSceneChain(result, 0);
        AddSceneChain(result, 1);
        result.HasDefaultScene = true;
        result.DefaultScene = 1;
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsVariants);
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrXmpJsonLd);
        return result;
    }

    private static void AddSceneChain(GltfDocument document, int index)
    {
        GltfResource resource = new GltfResource
        {
            Bytes = new byte[36],
            CacheKey = "resource-" + index
        };
        document.Resources.Resources.Add(resource);
        document.Resources.Cache[resource.CacheKey] = resource;
        document.Buffers.Add(new GltfBuffer
        {
            HasResource = true,
            Resource = index,
            ByteLength = 36,
            Resolved = true,
            ResolvedBytes = resource.Bytes
        });
        document.BufferViews.Add(new GltfBufferView
        {
            Buffer = index,
            ByteLength = 36
        });
        document.Accessors.Add(new GltfAccessor
        {
            HasBufferView = true,
            BufferView = index,
            ComponentType = GltfComponentType.Float,
            Count = 3,
            Type = GltfAccessorType.Vector3
        });
        document.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = index,
            MimeType = GltfMimeTypes.Png
        });
        document.Samplers.Add(new GltfSampler());
        document.Textures.Add(new GltfTexture
        {
            HasSampler = true,
            Sampler = index,
            HasSource = true,
            Source = index
        });
        GltfMaterial material = new GltfMaterial
        {
            PbrMetallicRoughness = new GltfPbrMetallicRoughness
            {
                BaseColorTexture = new GltfTextureInfo { Index = index }
            }
        };
        AddXmpAssignment(material, index);
        document.Materials.Add(material);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasMaterial = true,
            Material = index
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = index
        });
        AddVariantMapping(primitive, index);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(index);
        document.Skins.Add(skin);
        GltfNode node = new GltfNode
        {
            HasMesh = true,
            Mesh = index,
            HasSkin = true,
            Skin = index
        };
        node.PropertyJson.HasExtras = true;
        node.PropertyJson.Extras = new JsonObject { ["index"] = index };
        AddLightReference(node, index);
        document.Nodes.Add(node);
        GltfScene scene = new GltfScene { Name = "scene-" + index };
        scene.Nodes.Add(index);
        document.Scenes.Add(scene);
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(new GltfAnimationSampler
        {
            Input = index,
            Output = index
        });
        GltfAnimationChannel channel = new GltfAnimationChannel { Sampler = 0 };
        channel.Target.HasNode = true;
        channel.Target.Node = index;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
    }

    private static void AddRootExtensions(GltfDocument document)
    {
        GltfLightsPunctualExtension lights = new GltfLightsPunctualExtension();
        lights.Lights.Add(new GltfPunctualLight { Name = "light-0", Type = "point" });
        lights.Lights.Add(new GltfPunctualLight { Name = "light-1", Type = "point" });
        GltfTypedExtensionData typedLights = new GltfTypedExtensionData();
        typedLights.LightsPunctual = lights;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            = typedLights;
        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        variants.Variants.Add(new GltfMaterialVariant { Name = "variant-0" });
        variants.Variants.Add(new GltfMaterialVariant { Name = "variant-1" });
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariants = variants;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;
        GltfXmpJsonLdExtension xmp = new GltfXmpJsonLdExtension { HasPackets = true };
        xmp.Packets.Add(new GltfXmpJsonLdPacket());
        xmp.Packets.Add(new GltfXmpJsonLdPacket());
        GltfTypedExtensionData typedXmp = new GltfTypedExtensionData();
        typedXmp.XmpJsonLd = xmp;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typedXmp;
    }

    private static void AddLightReference(GltfNode node, int index)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.PunctualLightNode = new GltfPunctualLightNodeExtension { Light = index };
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = typed;
    }

    private static void AddVariantMapping(GltfMeshPrimitive primitive, int index)
    {
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping
        {
            Material = index
        };
        mapping.Variants.Add(index);
        GltfMaterialsVariantsPrimitiveExtension variants =
            new GltfMaterialsVariantsPrimitiveExtension();
        variants.Mappings.Add(mapping);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsVariantsPrimitive = variants;
        primitive.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typed;
    }

    private static void AddXmpAssignment(GltfMaterial material, int index)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
        {
            HasPacket = true,
            Packet = index
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typed;
    }

    private static GltfDocument CreateRetentionDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Accessors.Add(new GltfAccessor());
        GltfNode selected = new GltfNode();
        selected.PropertyJson.HasExtras = true;
        selected.PropertyJson.Extras = new JsonObject { ["keep"] = true };
        GltfMeshGpuInstancingExtension instancing = new GltfMeshGpuInstancingExtension();
        instancing.Attributes["TRANSLATION"] = 0;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MeshGpuInstancing = instancing;
        selected.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshGpuInstancing] = typed;
        selected.PropertyJson.Extensions["EXT_unknown"] = new JsonObject { ["value"] = 1 };
        result.ExtensionsUsed.Add(GltfExtensionNames.ExtMeshGpuInstancing);
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrDracoMeshCompression);
        result.ExtensionsUsed.Add("EXT_unknown");
        result.Buffers.Add(new GltfBuffer { ByteLength = 16 });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 16
        });
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfTypedExtensionData draco = new GltfTypedExtensionData();
        draco.DracoMeshCompression = new GltfDracoMeshCompressionExtension
        {
            BufferView = 0
        };
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = draco;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        selected.HasMesh = true;
        selected.Mesh = 0;
        result.Nodes.Add(selected);
        result.Nodes.Add(new GltfNode());
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        result.Scenes.Add(scene);
        GltfAnimation animation = new GltfAnimation();
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Target.HasNode = true;
        channel.Target.Node = 1;
        animation.Channels.Add(channel);
        result.Animations.Add(animation);
        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(1);
        result.Skins.Add(skin);
        return result;
    }

    private static GltfTransformStep CreatePruneStep(int scene)
    {
        GltfTransformStep result = new GltfTransformStep { Kind = GltfTransformKind.Prune };
        result.Settings["scenes"] = new JsonArray(scene);
        return result;
    }

    private static GltfTransformOptions CreateUncheckedOptions()
    {
        return new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
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
