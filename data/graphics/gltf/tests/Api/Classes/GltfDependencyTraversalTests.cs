namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDependencyTraversalTests
{
    [Test]
    public void InspectDocument_TraversesCoreDerivedAndRegisteredExtensionReferences()
    {
        GltfDocument document = CreateExtensionReferenceDocument();
        GltfInspectionOptions options = new GltfInspectionOptions
        {
            CalculateBounds = false,
            DecodeAccessors = false
        };

        GltfInspectionResult result = new GltfInspector().InspectDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceCollection == "textures"
                && edge.SourceMember == "selectedSource"
                && edge.TargetCollection == "images"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.EndsWith("KHR_materials_clearcoat/clearcoatTexture/index")
                && edge.TargetCollection == "textures"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.EndsWith("KHR_draco_mesh_compression/bufferView")
                && edge.TargetCollection == "bufferViews"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_materials_variants")
                && edge.TargetCollection == "materials"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_materials_variants")
                && edge.TargetCollection
                    == "extensions/KHR_materials_variants/variants"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("EXT_mesh_gpu_instancing")
                && edge.TargetCollection == "accessors"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_lights_punctual")
                && edge.TargetCollection
                    == "extensions/KHR_lights_punctual/lights"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_texture_basisu")
                && edge.TargetCollection == "images"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_xmp_json_ld")
                && edge.TargetCollection == "xmpJsonLdPackets"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.Dependencies.Any(edge =>
                edge.SourceMember.Contains("KHR_animation_pointer")
                && edge.TargetCollection == "nodes"
                && edge.TargetIndex == 1), Is.True);
            Assert.That(result.ReverseDependencies.Any(edge =>
                edge.SourceCollection == "xmpJsonLdPackets"
                && edge.SourceIndex == 1
                && edge.TargetCollection == "materials"
                && edge.TargetIndex == 0), Is.True);
        });
    }

    private static GltfDocument CreateExtensionReferenceDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Accessors.Add(new GltfAccessor());
        result.Accessors.Add(new GltfAccessor());
        result.BufferViews.Add(new GltfBufferView());
        result.BufferViews.Add(new GltfBufferView());
        result.Images.Add(new GltfImage());
        result.Images.Add(new GltfImage());
        result.Textures.Add(new GltfTexture());
        result.Textures.Add(new GltfTexture());
        result.Materials.Add(new GltfMaterial());
        result.Materials.Add(new GltfMaterial());
        result.Nodes.Add(new GltfNode());
        result.Nodes.Add(new GltfNode());
        AddRootExtensionCollections(result);
        AddMaterialReferences(result.Materials[0]);
        AddPrimitiveReferences(result);
        AddNodeReferences(result.Nodes[0]);
        AddTextureReferences(result.Textures[0]);
        AddAnimationPointerReference(result);
        return result;
    }

    private static void AddRootExtensionCollections(GltfDocument document)
    {
        GltfLightsPunctualExtension lights = new GltfLightsPunctualExtension();
        lights.Lights.Add(new GltfPunctualLight { Type = "point" });
        lights.Lights.Add(new GltfPunctualLight { Type = "point" });
        GltfTypedExtensionData typedLights = new GltfTypedExtensionData();
        typedLights.LightsPunctual = lights;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            = typedLights;

        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        variants.Variants.Add(new GltfMaterialVariant { Name = "first" });
        variants.Variants.Add(new GltfMaterialVariant { Name = "second" });
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

    private static void AddMaterialReferences(GltfMaterial material)
    {
        GltfTypedExtensionData clearcoat = new GltfTypedExtensionData();
        clearcoat.MaterialsClearcoat = new GltfMaterialsClearcoatExtension
        {
            ClearcoatTexture = new GltfTextureInfo { Index = 1 }
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsClearcoat]
            = clearcoat;
        GltfTypedExtensionData xmp = new GltfTypedExtensionData();
        xmp.XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
        {
            HasPacket = true,
            Packet = 1
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = xmp;
    }

    private static void AddPrimitiveReferences(GltfDocument document)
    {
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfTypedExtensionData draco = new GltfTypedExtensionData();
        draco.DracoMeshCompression = new GltfDracoMeshCompressionExtension
        {
            BufferView = 1
        };
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = draco;
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping
        {
            Material = 1
        };
        mapping.Variants.Add(1);
        GltfMaterialsVariantsPrimitiveExtension variants =
            new GltfMaterialsVariantsPrimitiveExtension();
        variants.Mappings.Add(mapping);
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariantsPrimitive = variants;
        primitive.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
    }

    private static void AddNodeReferences(GltfNode node)
    {
        GltfTypedExtensionData light = new GltfTypedExtensionData();
        light.PunctualLightNode = new GltfPunctualLightNodeExtension { Light = 1 };
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = light;
        GltfMeshGpuInstancingExtension instancing = new GltfMeshGpuInstancingExtension();
        instancing.Attributes["TRANSLATION"] = 1;
        GltfTypedExtensionData typedInstancing = new GltfTypedExtensionData();
        typedInstancing.MeshGpuInstancing = instancing;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.ExtMeshGpuInstancing]
            = typedInstancing;
    }

    private static void AddTextureReferences(GltfTexture texture)
    {
        texture.HasSelectedSource = true;
        texture.SelectedSource = 1;
        GltfTypedExtensionData basisu = new GltfTypedExtensionData();
        basisu.TextureBasisu = new GltfTextureBasisuExtension
        {
            HasSource = true,
            Source = 1
        };
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.KhrTextureBasisu] = basisu;
    }

    private static void AddAnimationPointerReference(GltfDocument document)
    {
        GltfAnimation animation = new GltfAnimation();
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Target.Path = GltfAnimationPath.Pointer;
        GltfTypedExtensionData pointer = new GltfTypedExtensionData();
        pointer.AnimationPointer = new GltfAnimationPointerExtension
        {
            Pointer = "/nodes/1/translation"
        };
        channel.Target.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrAnimationPointer] = pointer;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
    }
}
