namespace gltf;

/// <summary>Contains one location-specific typed extension payload.</summary>
public class GltfTypedExtensionData
{
    /// <summary>Typed <c>KHR_materials_anisotropy</c> data.</summary>
    public GltfMaterialsAnisotropyExtension? MaterialsAnisotropy;

    /// <summary>Typed <c>KHR_materials_clearcoat</c> data.</summary>
    public GltfMaterialsClearcoatExtension? MaterialsClearcoat;

    /// <summary>Typed <c>KHR_materials_dispersion</c> data.</summary>
    public GltfMaterialsDispersionExtension? MaterialsDispersion;

    /// <summary>Typed <c>KHR_materials_emissive_strength</c> data.</summary>
    public GltfMaterialsEmissiveStrengthExtension? MaterialsEmissiveStrength;

    /// <summary>Typed <c>KHR_materials_ior</c> data.</summary>
    public GltfMaterialsIorExtension? MaterialsIor;

    /// <summary>Typed <c>KHR_materials_iridescence</c> data.</summary>
    public GltfMaterialsIridescenceExtension? MaterialsIridescence;

    /// <summary>Typed <c>KHR_materials_sheen</c> data.</summary>
    public GltfMaterialsSheenExtension? MaterialsSheen;

    /// <summary>Typed <c>KHR_materials_specular</c> data.</summary>
    public GltfMaterialsSpecularExtension? MaterialsSpecular;

    /// <summary>Typed <c>KHR_materials_transmission</c> data.</summary>
    public GltfMaterialsTransmissionExtension? MaterialsTransmission;

    /// <summary>Typed <c>KHR_materials_unlit</c> data.</summary>
    public GltfMaterialsUnlitExtension? MaterialsUnlit;

    /// <summary>Typed <c>KHR_materials_volume</c> data.</summary>
    public GltfMaterialsVolumeExtension? MaterialsVolume;

    /// <summary>Typed snapshot <c>KHR_materials_diffuse_transmission</c> data.</summary>
    public GltfMaterialsDiffuseTransmissionExtension? MaterialsDiffuseTransmission;

    /// <summary>Typed archived <c>KHR_materials_pbrSpecularGlossiness</c> data.</summary>
    public GltfMaterialsPbrSpecularGlossinessExtension? MaterialsPbrSpecularGlossiness;

    /// <summary>Typed <c>KHR_texture_transform</c> data.</summary>
    public GltfTextureTransformExtension? TextureTransform;

    /// <summary>Typed root <c>KHR_lights_punctual</c> data.</summary>
    public GltfLightsPunctualExtension? LightsPunctual;

    /// <summary>Typed node <c>KHR_lights_punctual</c> data.</summary>
    public GltfPunctualLightNodeExtension? PunctualLightNode;

    /// <summary>Typed <c>KHR_node_visibility</c> data.</summary>
    public GltfNodeVisibilityExtension? NodeVisibility;

    /// <summary>Typed snapshot <c>KHR_node_hoverability</c> data.</summary>
    public GltfNodeHoverabilityExtension? NodeHoverability;

    /// <summary>Typed snapshot <c>KHR_node_selectability</c> data.</summary>
    public GltfNodeSelectabilityExtension? NodeSelectability;

    /// <summary>Typed <c>EXT_mesh_gpu_instancing</c> data.</summary>
    public GltfMeshGpuInstancingExtension? MeshGpuInstancing;

    /// <summary>Typed <c>KHR_animation_pointer</c> data.</summary>
    public GltfAnimationPointerExtension? AnimationPointer;

    /// <summary>Typed root <c>KHR_materials_variants</c> data.</summary>
    public GltfMaterialsVariantsExtension? MaterialsVariants;

    /// <summary>Typed primitive <c>KHR_materials_variants</c> data.</summary>
    public GltfMaterialsVariantsPrimitiveExtension? MaterialsVariantsPrimitive;

    /// <summary>Typed snapshot <c>KHR_gaussian_splatting</c> data.</summary>
    public GltfGaussianSplattingExtension? GaussianSplatting;

    /// <summary>Typed snapshot <c>KHR_interactivity</c> data.</summary>
    public GltfInteractivityExtension? Interactivity;

    /// <summary>Typed <c>KHR_draco_mesh_compression</c> data.</summary>
    public GltfDracoMeshCompressionExtension? DracoMeshCompression;

    /// <summary>Typed marker data for <c>KHR_mesh_quantization</c>.</summary>
    public GltfMeshQuantizationExtension? MeshQuantization;

    /// <summary>Typed KHR Meshopt buffer data.</summary>
    public GltfMeshoptCompressionBufferExtension? KhrMeshoptCompressionBuffer;

    /// <summary>Typed KHR Meshopt buffer-view data.</summary>
    public GltfMeshoptCompressionBufferViewExtension? KhrMeshoptCompressionBufferView;

    /// <summary>Typed EXT Meshopt buffer data.</summary>
    public GltfMeshoptCompressionBufferExtension? ExtMeshoptCompressionBuffer;

    /// <summary>Typed EXT Meshopt buffer-view data.</summary>
    public GltfMeshoptCompressionBufferViewExtension? ExtMeshoptCompressionBufferView;

    /// <summary>Typed <c>KHR_texture_basisu</c> data.</summary>
    public GltfTextureBasisuExtension? TextureBasisu;

    /// <summary>Typed <c>EXT_texture_webp</c> data.</summary>
    public GltfTextureWebpExtension? TextureWebp;

    /// <summary>Typed root <c>KHR_xmp_json_ld</c> data.</summary>
    public GltfXmpJsonLdExtension? XmpJsonLd;

    /// <summary>Typed object assignment for <c>KHR_xmp_json_ld</c>.</summary>
    public GltfXmpJsonLdAssignmentExtension? XmpJsonLdAssignment;
}
