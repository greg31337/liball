using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDeduplicateTransformTests
{
    [Test]
    public void Deduplicate_CollapsesEquivalentDependencyChainsAndRemapsReferences()
    {
        GltfDocument source = CreateDuplicateDocument();
        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            CreateUncheckedOptions());
        GltfDocument result = report.Document!;
        GltfTransformStepResult step = report.Steps[0];

        AssertDeduplicatedCounts(result);
        AssertRemappedReferences(result);
        Assert.Multiple(() =>
        {
            Assert.That(step.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(step.Remap.Buffers[1], Is.EqualTo(0));
            Assert.That(step.Remap.BufferViews[1], Is.EqualTo(0));
            Assert.That(step.Remap.Accessors[1], Is.EqualTo(0));
            Assert.That(step.Remap.Images[1], Is.EqualTo(0));
            Assert.That(step.Remap.Samplers[1], Is.EqualTo(0));
            Assert.That(step.Remap.Textures[1], Is.EqualTo(0));
            Assert.That(step.Remap.PunctualLights[1], Is.EqualTo(0));
            Assert.That(step.Remap.MaterialVariants[1], Is.EqualTo(0));
            Assert.That(step.Remap.XmpJsonLdPackets[1], Is.EqualTo(0));
            Assert.That(step.Remap.Materials[1], Is.EqualTo(0));
            Assert.That(step.Remap.Meshes[1], Is.EqualTo(0));
            Assert.That(step.ChangedObjects, Is.EqualTo(new[]
            {
                "buffers/1",
                "bufferViews/1",
                "accessors/1",
                "images/1",
                "samplers/1",
                "textures/1",
                "extensions/KHR_lights_punctual/lights/1",
                "extensions/KHR_materials_variants/variants/1",
                "xmpJsonLdPackets/1",
                "materials/1",
                "meshes/1"
            }));
            Assert.That(step.ChangedResources, Is.Empty);
            Assert.That(source.Buffers, Has.Count.EqualTo(2));
            Assert.That(source.Meshes, Has.Count.EqualTo(2));
            Assert.That(GetRootLights(source).Lights, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Deduplicate_PreservesObjectsWithDifferentBytesOrExtensionPayloads()
    {
        GltfDocument source = new GltfDocument();
        source.Buffers.Add(CreateBuffer(1));
        source.Buffers.Add(CreateBuffer(2));
        GltfSampler first = new GltfSampler();
        first.PropertyJson.Extensions["EXT_test"] = new JsonObject { ["value"] = 1 };
        GltfSampler second = new GltfSampler();
        second.PropertyJson.Extensions["EXT_test"] = new JsonObject { ["value"] = 2 };
        source.Samplers.Add(first);
        source.Samplers.Add(second);
        GltfTransformOptions options = CreateUncheckedOptions();
        options.RequireSafeUnknownExtensions = false;

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Document!.Buffers, Has.Count.EqualTo(2));
            Assert.That(report.Document.Samplers, Has.Count.EqualTo(2));
            Assert.That(report.Steps[0].ChangedObjects, Is.Empty);
            Assert.That(report.Steps[0].Remap.Buffers,
                Is.EqualTo(new Dictionary<int, int> { [0] = 0, [1] = 1 }));
            Assert.That(report.Steps[0].Remap.Samplers,
                Is.EqualTo(new Dictionary<int, int> { [0] = 0, [1] = 1 }));
        });
    }

    [Test]
    public void Deduplicate_RejectsUnsafeUnknownExtensionsTransactionally()
    {
        GltfDocument source = new GltfDocument();
        source.Samplers.Add(new GltfSampler());
        source.Samplers.Add(new GltfSampler());
        source.PropertyJson.Extensions["EXT_unknown"] = new JsonObject();
        GltfTransformOptions options = CreateUncheckedOptions();

        Assert.That(
            () => new GltfTransformer().TransformDocument(source, CreatePlan(), options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("cannot safely remap references"));
        Assert.That(source.Samplers, Has.Count.EqualTo(2));
    }

    [Test]
    public void Deduplicate_DefaultValidationAcceptsEquivalentSamplers()
    {
        GltfDocument source = new GltfDocument();
        source.Samplers.Add(new GltfSampler());
        source.Samplers.Add(new GltfSampler());

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            CreatePlan(),
            new GltfTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document!.Samplers, Has.Count.EqualTo(1));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "samplers/1" }));
            Assert.That(source.Samplers, Has.Count.EqualTo(2));
        });
    }

    private static void AssertDeduplicatedCounts(GltfDocument document)
    {
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Accessors, Has.Count.EqualTo(1));
            Assert.That(document.Images, Has.Count.EqualTo(1));
            Assert.That(document.Samplers, Has.Count.EqualTo(1));
            Assert.That(document.Textures, Has.Count.EqualTo(1));
            Assert.That(document.Materials, Has.Count.EqualTo(1));
            Assert.That(document.Meshes, Has.Count.EqualTo(1));
            Assert.That(GetRootLights(document).Lights, Has.Count.EqualTo(1));
            Assert.That(GetRootVariants(document).Variants, Has.Count.EqualTo(1));
            Assert.That(GetRootXmp(document).Packets, Has.Count.EqualTo(1));
        });
    }

    private static void AssertRemappedReferences(GltfDocument document)
    {
        GltfTexture texture = document.Textures[0];
        GltfMaterial material = document.Materials[0];
        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        Assert.Multiple(() =>
        {
            Assert.That(document.BufferViews[0].Buffer, Is.EqualTo(0));
            Assert.That(document.Accessors[0].BufferView, Is.EqualTo(0));
            Assert.That(document.Images[0].BufferView, Is.EqualTo(0));
            Assert.That(texture.Sampler, Is.EqualTo(0));
            Assert.That(texture.Source, Is.EqualTo(0));
            Assert.That(texture.SelectedSource, Is.EqualTo(0));
            Assert.That(texture.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureBasisu].TextureBasisu!.Source,
                Is.EqualTo(0));
            Assert.That(material.PbrMetallicRoughness!.BaseColorTexture!.Index,
                Is.EqualTo(0));
            Assert.That(material.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrXmpJsonLd].XmpJsonLdAssignment!.Packet,
                Is.EqualTo(0));
            Assert.That(primitive.Attributes[0].Accessor, Is.EqualTo(0));
            Assert.That(primitive.Material, Is.EqualTo(0));
            Assert.That(primitive.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrDracoMeshCompression]
                .DracoMeshCompression!.BufferView, Is.EqualTo(0));
            Assert.That(primitive.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsVariants]
                .MaterialsVariantsPrimitive!.Mappings[0].Material, Is.EqualTo(0));
            Assert.That(primitive.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsVariants]
                .MaterialsVariantsPrimitive!.Mappings[0].Variants,
                Is.EqualTo(new[] { 0 }));
            Assert.That(document.Nodes[0].Mesh, Is.EqualTo(0));
            Assert.That(document.Nodes[1].Mesh, Is.EqualTo(0));
            Assert.That(GetNodeLight(document.Nodes[0]).Light, Is.EqualTo(0));
            Assert.That(GetNodeLight(document.Nodes[1]).Light, Is.EqualTo(0));
            Assert.That(document.Animations[0].Samplers[0].Input, Is.EqualTo(0));
            Assert.That(document.Animations[0].Samplers[0].Output, Is.EqualTo(0));
            Assert.That(document.Skins[0].InverseBindMatrices, Is.EqualTo(0));
        });
    }

    private static GltfDocument CreateDuplicateDocument()
    {
        GltfDocument result = new GltfDocument();
        AddRootExtensions(result);
        int index = 0;
        while (index < 2)
        {
            AddDuplicateChain(result, index);
            index++;
        }

        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(new GltfAnimationSampler { Input = 1, Output = 1 });
        result.Animations.Add(animation);
        result.Skins.Add(new GltfSkin
        {
            HasInverseBindMatrices = true,
            InverseBindMatrices = 1
        });
        return result;
    }

    private static void AddDuplicateChain(GltfDocument document, int index)
    {
        document.Resources.Resources.Add(new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            Bytes = [7, 2, 3, 4]
        });
        GltfBuffer buffer = CreateBuffer(7);
        buffer.HasResource = true;
        buffer.Resource = index;
        document.Buffers.Add(buffer);
        document.BufferViews.Add(new GltfBufferView
        {
            Buffer = index,
            ByteLength = 4
        });
        document.Accessors.Add(new GltfAccessor
        {
            HasBufferView = true,
            BufferView = index,
            ComponentType = GltfComponentType.UnsignedByte,
            Count = 4,
            Type = GltfAccessorType.Scalar
        });
        document.Images.Add(new GltfImage
        {
            MimeType = GltfMimeTypes.Png,
            HasBufferView = true,
            BufferView = index,
            Resolved = true,
            ResolvedBytes = [10, 20, 30],
            EffectiveUri = "image",
            ResolvedMimeType = GltfMimeTypes.Png
        });
        document.Samplers.Add(new GltfSampler());
        GltfTexture texture = new GltfTexture
        {
            HasSampler = true,
            Sampler = index,
            HasSource = true,
            Source = index,
            HasSelectedSource = true,
            SelectedSource = index
        };
        AddBasisu(texture, index);
        document.Textures.Add(texture);
        GltfMaterial material = new GltfMaterial
        {
            PbrMetallicRoughness = new GltfPbrMetallicRoughness
            {
                BaseColorTexture = new GltfTextureInfo { Index = index }
            }
        };
        AddXmpAssignment(material, index);
        material.PropertyJson.Extras = CreateOrderedExtras(index == 0);
        material.PropertyJson.HasExtras = true;
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
        AddDraco(primitive, index);
        AddVariantMapping(primitive, index);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfNode node = new GltfNode { HasMesh = true, Mesh = index };
        AddLight(node, index);
        document.Nodes.Add(node);
    }

    private static GltfBuffer CreateBuffer(byte value)
    {
        return new GltfBuffer
        {
            ByteLength = 4,
            Resolved = true,
            ResolvedBytes = [value, 2, 3, 4]
        };
    }

    private static JsonObject CreateOrderedExtras(bool forward)
    {
        JsonObject result = new JsonObject();
        if (forward)
        {
            result["a"] = 1;
            result["b"] = 2;
        }
        else
        {
            result["b"] = 2;
            result["a"] = 1;
        }

        return result;
    }

    private static void AddBasisu(GltfTexture texture, int source)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.TextureBasisu = new GltfTextureBasisuExtension
        {
            HasSource = true,
            Source = source
        };
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.KhrTextureBasisu] = typed;
    }

    private static void AddXmpAssignment(GltfMaterial material, int packet)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
        {
            HasPacket = true,
            Packet = packet
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typed;
    }

    private static void AddDraco(GltfMeshPrimitive primitive, int bufferView)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.DracoMeshCompression = new GltfDracoMeshCompressionExtension
        {
            BufferView = bufferView
        };
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = typed;
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
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVariants] = typed;
    }

    private static void AddLight(GltfNode node, int light)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.PunctualLightNode = new GltfPunctualLightNodeExtension { Light = light };
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = typed;
    }

    private static void AddRootExtensions(GltfDocument document)
    {
        GltfLightsPunctualExtension lights = new GltfLightsPunctualExtension();
        lights.Lights.Add(new GltfPunctualLight { Type = "point" });
        lights.Lights.Add(new GltfPunctualLight { Type = "point" });
        GltfTypedExtensionData typedLights = new GltfTypedExtensionData();
        typedLights.LightsPunctual = lights;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            = typedLights;
        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        variants.Variants.Add(new GltfMaterialVariant { Name = "shared" });
        variants.Variants.Add(new GltfMaterialVariant { Name = "shared" });
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariants = variants;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;
        GltfXmpJsonLdExtension xmp = new GltfXmpJsonLdExtension { HasPackets = true };
        xmp.Packets.Add(CreateXmpPacket(true));
        xmp.Packets.Add(CreateXmpPacket(false));
        GltfTypedExtensionData typedXmp = new GltfTypedExtensionData();
        typedXmp.XmpJsonLd = xmp;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typedXmp;
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsVariants);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrXmpJsonLd);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrDracoMeshCompression);
    }

    private static GltfXmpJsonLdPacket CreateXmpPacket(bool forward)
    {
        GltfXmpJsonLdPacket result = new GltfXmpJsonLdPacket { HasContext = true };
        if (forward)
        {
            result.Context["a"] = "one";
            result.Context["b"] = "two";
        }
        else
        {
            result.Context["b"] = "two";
            result.Context["a"] = "one";
        }

        result.Properties["value"] = 3;
        return result;
    }

    private static GltfTransformPlan CreatePlan()
    {
        GltfTransformStep step = new GltfTransformStep
        {
            Kind = GltfTransformKind.Deduplicate
        };
        return new GltfTransformPlan { Steps = [step] };
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
        return document.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVariants].MaterialsVariants!;
    }

    private static GltfXmpJsonLdExtension GetRootXmp(GltfDocument document)
    {
        return document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd]
            .XmpJsonLd!;
    }

    private static GltfPunctualLightNodeExtension GetNodeLight(GltfNode node)
    {
        return node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            .PunctualLightNode!;
    }
}
