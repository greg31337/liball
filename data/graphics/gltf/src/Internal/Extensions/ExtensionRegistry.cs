using System;
using System.Collections.Generic;

namespace gltf;

internal class ExtensionRegistry
{
    internal List<GltfExtensionDescriptor> Descriptors = new List<GltfExtensionDescriptor>();

    internal ExtensionRegistry()
    {
        AddRegistryDescriptors();
        AddSchemaResources();
        AddRelationships();
        AddImplementedCapabilities();
    }

    internal List<GltfExtensionDescriptor> ListDescriptors()
    {
        List<GltfExtensionDescriptor> result = new List<GltfExtensionDescriptor>();
        int index = 0;
        while (index < Descriptors.Count)
        {
            result.Add(CopyDescriptor(Descriptors[index]));
            index++;
        }

        return result;
    }

    internal GltfExtensionDescriptor? FindDescriptor(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < Descriptors.Count)
        {
            GltfExtensionDescriptor descriptor = Descriptors[index];
            if (string.Equals(descriptor.Name, name, StringComparison.Ordinal))
            {
                return CopyDescriptor(descriptor);
            }

            index++;
        }

        return null;
    }

    internal void AddRegistryDescriptors()
    {
        AddDescriptor(GltfExtensionNames.AdobeMaterialsClearcoatSpecular, GltfExtensionStatus.Vendor, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.AdobeMaterialsClearcoatTint, GltfExtensionStatus.Vendor, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.AdobeMaterialsThinTransparency, GltfExtensionStatus.Vendor, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.AgiArticulations, GltfExtensionStatus.Vendor, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.AgiStkMetadata, GltfExtensionStatus.Vendor, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.CesiumPrimitiveOutline, GltfExtensionStatus.Vendor, "mesh.primitive", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtLightsIes, GltfExtensionStatus.MultiVendor, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtLightsImageBased, GltfExtensionStatus.MultiVendor, "glTF|scene", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtMeshGpuInstancing, GltfExtensionStatus.Ratified, "node", true, false, null);
        AddDescriptor(GltfExtensionNames.ExtMeshManifold, GltfExtensionStatus.MultiVendor, "mesh", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtMeshPrimitiveEdgeVisibility, GltfExtensionStatus.Draft, "mesh.primitive", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtMeshPrimitiveRestart, GltfExtensionStatus.MultiVendor, "mesh.primitive", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtMeshoptCompression, GltfExtensionStatus.Ratified, "buffer|bufferView", true, true, GltfExtensionNames.ExtMeshoptCompression);
        AddDescriptor(GltfExtensionNames.ExtTextureAstc, GltfExtensionStatus.Draft, "texture", false, true, GltfExtensionNames.ExtTextureAstc);
        AddDescriptor(GltfExtensionNames.ExtTextureProceduralsMx139, GltfExtensionStatus.Draft, "texture", false, true, null);
        AddDescriptor(GltfExtensionNames.ExtTextureWebp, GltfExtensionStatus.Ratified, "texture", true, true, GltfExtensionNames.ExtTextureWebp);
        AddDescriptor(GltfExtensionNames.FbGeometryMetadata, GltfExtensionStatus.Vendor, "scene", false, true, null);
        AddDescriptor(GltfExtensionNames.GodotSingleRoot, GltfExtensionStatus.Vendor, "glTF.extensionsUsed", false, true, null);
        AddDescriptor(GltfExtensionNames.GriffelBimData, GltfExtensionStatus.Vendor, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrAccessorFloat64, GltfExtensionStatus.Draft, "accessor", false, false, null);
        AddDescriptor(GltfExtensionNames.KhrAnimationPointer, GltfExtensionStatus.Ratified, "animation.channel.target", true, false, null);
        AddDescriptor(GltfExtensionNames.KhrAudioGraph, GltfExtensionStatus.Draft, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrCollisionShapes, GltfExtensionStatus.Draft, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrDracoMeshCompression, GltfExtensionStatus.Ratified, "mesh.primitive", true, true, GltfExtensionNames.KhrDracoMeshCompression);
        AddDescriptor(GltfExtensionNames.KhrGaussianSplatting, AdvancedExtensionsSnapshot.Status, "mesh.primitive", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrInteractivity, AdvancedExtensionsSnapshot.Status, "glTF", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrLightsPunctual, GltfExtensionStatus.Ratified, "glTF|node", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsAnisotropy, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsClearcoat, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsDiffuseTransmission, DiffuseTransmissionSnapshot.Status, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsDispersion, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsEmissiveStrength, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsIor, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsIridescence, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness, GltfExtensionStatus.Archived, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsSheen, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsSpecular, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsSubsurface, GltfExtensionStatus.Draft, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsTransmission, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsUnlit, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsVariants, GltfExtensionStatus.Ratified, "glTF|mesh.primitive", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMaterialsVolume, GltfExtensionStatus.Ratified, "material", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrMeshQuantization, GltfExtensionStatus.Ratified, "glTF.extensionsUsed", true, false, null);
        AddDescriptor(GltfExtensionNames.KhrMeshoptCompression, GltfExtensionStatus.ReleaseCandidate, "buffer|bufferView", true, true, GltfExtensionNames.KhrMeshoptCompression);
        AddDescriptor(GltfExtensionNames.KhrNodeHoverability, GltfExtensionStatus.ReleaseCandidate, "node", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrNodeSelectability, GltfExtensionStatus.ReleaseCandidate, "node", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrNodeVisibility, GltfExtensionStatus.Ratified, "node", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrPhysicsRigidBodies, GltfExtensionStatus.Draft, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrTechniquesWebgl, GltfExtensionStatus.Archived, "glTF|material", false, false, null);
        AddDescriptor(GltfExtensionNames.KhrTextureBasisu, GltfExtensionStatus.Ratified, "texture", true, true, GltfExtensionNames.KhrTextureBasisu);
        AddDescriptor(GltfExtensionNames.KhrTextureProcedurals, GltfExtensionStatus.Draft, "texture", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrTextureTransform, GltfExtensionStatus.Ratified, "textureInfo", true, true, null);
        AddDescriptor(GltfExtensionNames.KhrTextureVideo, GltfExtensionStatus.Draft, "texture", false, true, GltfExtensionNames.KhrTextureVideo);
        AddDescriptor(GltfExtensionNames.KhrXmp, GltfExtensionStatus.Archived, "glTF|node", false, true, null);
        AddDescriptor(GltfExtensionNames.KhrXmpJsonLd, GltfExtensionStatus.Ratified, "glTF|glTFProperty", true, true, null);
        AddDescriptor(GltfExtensionNames.MpegAccessorTimed, GltfExtensionStatus.Vendor, "accessor", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegAnimationTiming, GltfExtensionStatus.Vendor, "animation", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegAudioSpatial, GltfExtensionStatus.Vendor, "glTF|node|scene", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegBufferCircular, GltfExtensionStatus.Vendor, "bufferView", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegMedia, GltfExtensionStatus.Vendor, "glTF", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegMeshLinking, GltfExtensionStatus.Vendor, "mesh", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegSceneDynamic, GltfExtensionStatus.Vendor, "scene", false, true, null);
        AddDescriptor(GltfExtensionNames.MpegTextureVideo, GltfExtensionStatus.Vendor, "texture", false, true, GltfExtensionNames.MpegTextureVideo);
        AddDescriptor(GltfExtensionNames.MpegViewportRecommended, GltfExtensionStatus.Vendor, "scene", false, true, null);
        AddDescriptor(GltfExtensionNames.MsftLod, GltfExtensionStatus.Vendor, "node|material|mesh", false, true, null);
        AddDescriptor(GltfExtensionNames.MsftPackingNormalRoughnessMetallic, GltfExtensionStatus.Vendor, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.MsftPackingOcclusionRoughnessMetallic, GltfExtensionStatus.Vendor, "material", false, true, null);
        AddDescriptor(GltfExtensionNames.MsftTextureDds, GltfExtensionStatus.Vendor, "texture", false, true, GltfExtensionNames.MsftTextureDds);
        AddDescriptor(GltfExtensionNames.NvMaterialsMdl, GltfExtensionStatus.Vendor, "glTF|material", false, false, null);
        GetStoredDescriptor(GltfExtensionNames.KhrMaterialsDiffuseTransmission).Version =
            DiffuseTransmissionSnapshot.Version;
        GetStoredDescriptor(GltfExtensionNames.KhrGaussianSplatting).Version =
            AdvancedExtensionsSnapshot.Version;
        GetStoredDescriptor(GltfExtensionNames.KhrInteractivity).Version =
            AdvancedExtensionsSnapshot.Version;
    }

    internal void AddSchemaResources()
    {
        SetSchemas(GltfExtensionNames.AdobeMaterialsClearcoatSpecular, "glTF.ADOBE_materials_clearcoat_specular.schema.json");
        SetSchemas(GltfExtensionNames.AdobeMaterialsClearcoatTint, "material.ADOBE_materials_clearcoat_tint.schema.json");
        SetSchemas(GltfExtensionNames.AdobeMaterialsThinTransparency, "glTF.ADOBE_materials_thin_transparency.schema.json");
        SetSchemas(GltfExtensionNames.AgiArticulations, "articulation.schema.json|articulation.stage.schema.json|gltf.AGI_articulations.schema.json|node.AGI_articulations.schema.json");
        SetSchemas(GltfExtensionNames.AgiStkMetadata, "gltf.AGI_stk_metadata.schema.json|node.AGI_stk_metadata.schema.json|solarPanelGroup.schema.json");
        SetSchemas(GltfExtensionNames.CesiumPrimitiveOutline, "primitive.CESIUM_primitive_outline.schema.json");
        SetSchemas(GltfExtensionNames.ExtLightsIes, "glTF.EXT_lights_ies.schema.json|lightProfile.schema.json|node.EXT_lights_ies.schema.json");
        SetSchemas(GltfExtensionNames.ExtLightsImageBased, "glTF.EXT_lights_image_based.schema.json|light.schema.json|scene.EXT_lights_image_based.schema.json");
        SetSchemas(GltfExtensionNames.ExtMeshGpuInstancing, "glTF.EXT_mesh_gpu_instancing.schema.json");
        SetSchemas(GltfExtensionNames.ExtMeshManifold, "mesh.EXT_mesh_manifold.schema.json");
        SetSchemas(GltfExtensionNames.ExtMeshPrimitiveEdgeVisibility, "lineString.schema.json|primitive.EXT_mesh_primitive_edge_visibility.schema.json");
        SetSchemas(GltfExtensionNames.ExtMeshPrimitiveRestart, "EXT_mesh_primitive_restart.schema.json|primitiveGroup.schema.json");
        SetSchemas(GltfExtensionNames.ExtMeshoptCompression, "buffer.EXT_meshopt_compression.schema.json|bufferView.EXT_meshopt_compression.schema.json");
        SetSchemas(GltfExtensionNames.ExtTextureAstc, "texture.EXT_texture_astc.schema.json");
        SetSchemas(GltfExtensionNames.ExtTextureWebp, "glTF.EXT_texture_webp.schema.json");
        SetSchemas(GltfExtensionNames.FbGeometryMetadata, "scene.FB_geometry_metadata.schema.json|sceneBounds.schema.json");
        SetSchemas(GltfExtensionNames.GriffelBimData, "GRIFFEL_bim_data.schema.json|glTF.GRIFFEL_bim_data.schema.json|node.GRIFFEL_bim_data.schema.json");
        SetSchemas(GltfExtensionNames.KhrAnimationPointer, "animation.channel.target.KHR_animation_pointer.schema.json");
        SetSchemas(GltfExtensionNames.KhrDracoMeshCompression, "mesh.primitive.KHR_draco_mesh_compression.schema.json");
        SetSchemas(GltfExtensionNames.KhrGaussianSplatting, "mesh.primitive.KHR_gaussian_splatting.schema.json");
        SetSchemas(GltfExtensionNames.KhrInteractivity, "declaration.KHR_interactivity.schema.json|event.KHR_interactivity.schema.json|glTF.KHR_interactivity.schema.json|graph.KHR_interactivity.schema.json|nameableProperty.KHR_interactivity.schema.json|node.KHR_interactivity.schema.json|type.KHR_interactivity.schema.json|variable.KHR_interactivity.schema.json");
        SetSchemas(GltfExtensionNames.KhrLightsPunctual, "glTF.KHR_lights_punctual.schema.json|light.schema.json|light.spot.schema.json|node.KHR_lights_punctual.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsAnisotropy, "material.KHR_materials_anisotropy.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsClearcoat, "material.KHR_materials_clearcoat.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsDiffuseTransmission, "material.KHR_materials_diffuse_transmission.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsDispersion, "material.KHR_materials_dispersion.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsEmissiveStrength, "material.KHR_materials_emissive_strength.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsIor, "material.KHR_materials_ior.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsIridescence, "material.KHR_materials_iridescence.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness, "glTF.KHR_materials_pbrSpecularGlossiness.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsSheen, "material.KHR_materials_sheen.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsSpecular, "material.KHR_materials_specular.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsTransmission, "material.KHR_materials_transmission.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsUnlit, "material.KHR_materials_unlit.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsVariants, "glTF.KHR_materials_variants.schema.json|mesh.primitive.KHR_materials_variants.schema.json");
        SetSchemas(GltfExtensionNames.KhrMaterialsVolume, "material.KHR_materials_volume.schema.json");
        SetSchemas(GltfExtensionNames.KhrMeshoptCompression, "buffer.KHR_meshopt_compression.schema.json|bufferView.KHR_meshopt_compression.schema.json");
        SetSchemas(GltfExtensionNames.KhrNodeHoverability, "node.KHR_node_hoverability.schema.json");
        SetSchemas(GltfExtensionNames.KhrNodeSelectability, "node.KHR_node_selectability.schema.json");
        SetSchemas(GltfExtensionNames.KhrNodeVisibility, "node.KHR_node_visibility.schema.json");
        SetSchemas(GltfExtensionNames.KhrTechniquesWebgl, "glTF.KHR_techniques_webgl.schema.json|material.KHR_techniques_webgl.schema.json|program.schema.json|shader.schema.json|technique.attribute.schema.json|technique.schema.json|technique.uniform.schema.json|uniform.value.schema.json");
        SetSchemas(GltfExtensionNames.KhrTextureBasisu, "texture.KHR_texture_basisu.schema.json");
        SetSchemas(GltfExtensionNames.KhrTextureTransform, "textureInfo.KHR_texture_transform.schema.json");
        SetSchemas(GltfExtensionNames.KhrXmp, "glTF.KHR_xmp.schema.json|node.KHR_xmp.schema.json");
        SetSchemas(GltfExtensionNames.KhrXmpJsonLd, "KHR_xmp_json_ld.schema.json|glTF.KHR_xmp_json_ld.schema.json");
        SetSchemas(GltfExtensionNames.MpegAccessorTimed, "MPEG_accessor_timed.schema.json");
        SetSchemas(GltfExtensionNames.MpegAnimationTiming, "MPEG_animation_timing.schema.json");
        SetSchemas(GltfExtensionNames.MpegAudioSpatial, "MPEG_audio_spatial.listener.schema.json|MPEG_audio_spatial.reverb.property.schema.json|MPEG_audio_spatial.reverb.schema.json|MPEG_audio_spatial.schema.json|MPEG_audio_spatial.source.schema.json");
        SetSchemas(GltfExtensionNames.MpegBufferCircular, "MPEG_buffer_circular.schema.json");
        SetSchemas(GltfExtensionNames.MpegMedia, "MPEG_media.media.alternative.schema.json|MPEG_media.media.alternative.track.schema.json|MPEG_media.media.schema.json|MPEG_media.schema.json");
        SetSchemas(GltfExtensionNames.MpegMeshLinking, "MPEG_mesh_linking.schema.json");
        SetSchemas(GltfExtensionNames.MpegSceneDynamic, "MPEG_scene_dynamic.schema.json");
        SetSchemas(GltfExtensionNames.MpegTextureVideo, "MPEG_texture_video.schema.json");
        SetSchemas(GltfExtensionNames.MpegViewportRecommended, "MPEG_viewport_recommended.schema.json|MPEG_viewport_recommended.viewport.schema.json");
        SetSchemas(GltfExtensionNames.MsftLod, "glTF.MSFT_lod.schema.json");
        SetSchemas(GltfExtensionNames.MsftPackingNormalRoughnessMetallic, "glTF.MSFT_packing_normalRoughnessMetallic.schema.json");
        SetSchemas(GltfExtensionNames.MsftPackingOcclusionRoughnessMetallic, "glTF.MSFT_packing_occlusionRoughnessMetallic.schema.json");
        SetSchemas(GltfExtensionNames.MsftTextureDds, "glTF.MSFT_texture_dds.schema.json");
        SetSchemas(GltfExtensionNames.NvMaterialsMdl, "bsdfMeasurement.schema.json|functionCall.argument.schema.json|functionCall.schema.json|functionCall.type.schema.json|glTF.NV_materials_mdl.schema.json|material.NV_materials_mdl.schema.json|module.schema.json");
    }

    internal void AddRelationships()
    {
        AddDependency(GltfExtensionNames.AdobeMaterialsClearcoatSpecular, GltfExtensionNames.KhrMaterialsClearcoat);
        AddDependency(GltfExtensionNames.AdobeMaterialsClearcoatTint, GltfExtensionNames.KhrMaterialsClearcoat);
        AddDependency(GltfExtensionNames.KhrMaterialsDispersion, GltfExtensionNames.KhrMaterialsVolume);
        AddDependency(GltfExtensionNames.KhrMaterialsVolume, GltfExtensionNames.KhrMaterialsTransmission);
        AddDependency(GltfExtensionNames.KhrMeshQuantization, GltfExtensionNames.KhrTextureTransform);
        AddDependency(GltfExtensionNames.NvMaterialsMdl, GltfExtensionNames.ExtLightsIes);

        AddMaterialConflicts(GltfExtensionNames.AdobeMaterialsClearcoatSpecular);
        AddMaterialConflicts(GltfExtensionNames.AdobeMaterialsClearcoatTint);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsAnisotropy);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsClearcoat);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsDiffuseTransmission);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsDispersion);
        AddConflictPair(GltfExtensionNames.KhrMaterialsEmissiveStrength, GltfExtensionNames.KhrMaterialsUnlit);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsIor);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsIridescence);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsSheen);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsSpecular);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsTransmission);
        AddMaterialConflicts(GltfExtensionNames.KhrMaterialsVolume);
        AddConflictPair(GltfExtensionNames.KhrMeshoptCompression, GltfExtensionNames.ExtMeshoptCompression);
    }

    internal void AddImplementedCapabilities()
    {
        SetImplementedCapabilities(GltfExtensionNames.ExtMeshGpuInstancing, true);
        SetImplementedCapabilities(GltfExtensionNames.ExtMeshoptCompression, true);
        SetImplementedCapabilities(GltfExtensionNames.ExtTextureWebp, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrAnimationPointer, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrDracoMeshCompression, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrLightsPunctual, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsAnisotropy, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsClearcoat, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsDispersion, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsDiffuseTransmission, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsEmissiveStrength, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsIor, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsIridescence, true);
        SetImplementedCapabilities(
            GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness,
            true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsSheen, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsSpecular, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsTransmission, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsUnlit, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsVariants, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMaterialsVolume, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrGaussianSplatting, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrInteractivity, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrMeshQuantization, false);
        SetImplementedCapabilities(GltfExtensionNames.KhrMeshoptCompression, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrNodeHoverability, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrNodeSelectability, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrNodeVisibility, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrTextureTransform, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrTextureBasisu, true);
        SetImplementedCapabilities(GltfExtensionNames.KhrXmpJsonLd, true);
    }

    internal void SetImplementedCapabilities(string name, bool canRemapReferences)
    {
        GltfExtensionDescriptor descriptor = GetStoredDescriptor(name);
        descriptor.HasSemanticValidation = true;
        descriptor.CanRemapReferences = canRemapReferences;
    }

    internal void AddDescriptor(
        string name,
        GltfExtensionStatus status,
        string locations,
        bool hasTypedModel,
        bool allowsFallback,
        string? requiredCodec)
    {
        GltfExtensionDescriptor descriptor = new GltfExtensionDescriptor();
        descriptor.Name = name;
        descriptor.Status = status;
        descriptor.Version = "2.0";
        AddDelimitedValues(descriptor.ObjectLocations, locations);
        descriptor.HasTypedModel = hasTypedModel;
        descriptor.HasSemanticValidation = false;
        descriptor.CanRemapReferences = false;
        descriptor.AllowsFallback = allowsFallback;
        descriptor.RequiredCodec = requiredCodec;
        Descriptors.Add(descriptor);
    }

    internal void SetSchemas(string name, string resources)
    {
        GltfExtensionDescriptor descriptor = GetStoredDescriptor(name);
        AddDelimitedValues(descriptor.SchemaResources, resources);
    }

    internal void AddDependency(string name, string dependency)
    {
        GltfExtensionDescriptor descriptor = GetStoredDescriptor(name);
        descriptor.Dependencies.Add(dependency);
    }

    internal void AddMaterialConflicts(string name)
    {
        AddConflictPair(name, GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness);
        AddConflictPair(name, GltfExtensionNames.KhrMaterialsUnlit);
    }

    internal void AddConflictPair(string firstName, string secondName)
    {
        GltfExtensionDescriptor first = GetStoredDescriptor(firstName);
        GltfExtensionDescriptor second = GetStoredDescriptor(secondName);
        if (!ContainsString(first.Conflicts, secondName))
        {
            first.Conflicts.Add(secondName);
        }

        if (!ContainsString(second.Conflicts, firstName))
        {
            second.Conflicts.Add(firstName);
        }
    }

    internal GltfExtensionDescriptor GetStoredDescriptor(string name)
    {
        int index = 0;
        while (index < Descriptors.Count)
        {
            GltfExtensionDescriptor descriptor = Descriptors[index];
            if (string.Equals(descriptor.Name, name, StringComparison.Ordinal))
            {
                return descriptor;
            }

            index++;
        }

        throw new ArgumentException($"Extension descriptor '{name}' is not registered.", "name");
    }

    internal void AddDelimitedValues(List<string> values, string delimitedValues)
    {
        string[] items = delimitedValues.Split('|');
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Length > 0)
            {
                values.Add(items[index]);
            }

            index++;
        }
    }

    internal bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal GltfExtensionDescriptor CopyDescriptor(GltfExtensionDescriptor source)
    {
        GltfExtensionDescriptor result = new GltfExtensionDescriptor();
        result.Name = source.Name;
        result.Status = source.Status;
        result.Version = source.Version;
        CopyStrings(source.ObjectLocations, result.ObjectLocations);
        CopyStrings(source.Dependencies, result.Dependencies);
        CopyStrings(source.Conflicts, result.Conflicts);
        CopyStrings(source.SchemaResources, result.SchemaResources);
        result.HasTypedModel = source.HasTypedModel;
        result.HasSemanticValidation = source.HasSemanticValidation;
        result.CanRemapReferences = source.CanRemapReferences;
        result.AllowsFallback = source.AllowsFallback;
        result.RequiredCodec = source.RequiredCodec;
        return result;
    }

    internal void CopyStrings(List<string> source, List<string> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
