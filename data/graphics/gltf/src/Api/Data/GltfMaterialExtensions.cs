namespace gltf;

/// <summary>Represents the <c>KHR_materials_anisotropy</c> material payload.</summary>
public class GltfMaterialsAnisotropyExtension
{
    /// <summary>Whether anisotropy strength was explicitly provided.</summary>
    public bool HasAnisotropyStrength;

    /// <summary>The anisotropy strength.</summary>
    public double AnisotropyStrength;

    /// <summary>Whether anisotropy rotation was explicitly provided.</summary>
    public bool HasAnisotropyRotation;

    /// <summary>The counter-clockwise anisotropy rotation in radians.</summary>
    public double AnisotropyRotation;

    /// <summary>The optional anisotropy direction and strength texture.</summary>
    public GltfTextureInfo? AnisotropyTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_clearcoat</c> material payload.</summary>
public class GltfMaterialsClearcoatExtension
{
    /// <summary>Whether the clearcoat factor was explicitly provided.</summary>
    public bool HasClearcoatFactor;

    /// <summary>The clearcoat layer intensity.</summary>
    public double ClearcoatFactor;

    /// <summary>The optional clearcoat intensity texture.</summary>
    public GltfTextureInfo? ClearcoatTexture;

    /// <summary>Whether the clearcoat roughness factor was explicitly provided.</summary>
    public bool HasClearcoatRoughnessFactor;

    /// <summary>The clearcoat layer roughness.</summary>
    public double ClearcoatRoughnessFactor;

    /// <summary>The optional clearcoat roughness texture.</summary>
    public GltfTextureInfo? ClearcoatRoughnessTexture;

    /// <summary>The optional clearcoat normal texture.</summary>
    public GltfNormalTextureInfo? ClearcoatNormalTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_dispersion</c> material payload.</summary>
public class GltfMaterialsDispersionExtension
{
    /// <summary>Whether dispersion was explicitly provided.</summary>
    public bool HasDispersion;

    /// <summary>The dispersion value in the 20/Abbe formulation.</summary>
    public double Dispersion;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_emissive_strength</c> material payload.</summary>
public class GltfMaterialsEmissiveStrengthExtension
{
    /// <summary>Whether emissive strength was explicitly provided.</summary>
    public bool HasEmissiveStrength;

    /// <summary>The multiplier applied to the core emissive value.</summary>
    public double EmissiveStrength = 1.0;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_ior</c> material payload.</summary>
public class GltfMaterialsIorExtension
{
    /// <summary>Whether the index of refraction was explicitly provided.</summary>
    public bool HasIor;

    /// <summary>The material index of refraction.</summary>
    public double Ior = 1.5;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_iridescence</c> material payload.</summary>
public class GltfMaterialsIridescenceExtension
{
    /// <summary>Whether the iridescence factor was explicitly provided.</summary>
    public bool HasIridescenceFactor;

    /// <summary>The iridescence intensity factor.</summary>
    public double IridescenceFactor;

    /// <summary>The optional iridescence intensity texture.</summary>
    public GltfTextureInfo? IridescenceTexture;

    /// <summary>Whether the thin-film index of refraction was explicitly provided.</summary>
    public bool HasIridescenceIor;

    /// <summary>The thin-film index of refraction.</summary>
    public double IridescenceIor = 1.3;

    /// <summary>Whether minimum thin-film thickness was explicitly provided.</summary>
    public bool HasIridescenceThicknessMinimum;

    /// <summary>The minimum thin-film thickness in nanometers.</summary>
    public double IridescenceThicknessMinimum = 100.0;

    /// <summary>Whether maximum thin-film thickness was explicitly provided.</summary>
    public bool HasIridescenceThicknessMaximum;

    /// <summary>The maximum thin-film thickness in nanometers.</summary>
    public double IridescenceThicknessMaximum = 400.0;

    /// <summary>The optional thin-film thickness texture.</summary>
    public GltfTextureInfo? IridescenceThicknessTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_sheen</c> material payload.</summary>
public class GltfMaterialsSheenExtension
{
    /// <summary>Whether the sheen color factor was explicitly provided.</summary>
    public bool HasSheenColorFactor;

    /// <summary>The linear RGB sheen color factor.</summary>
    public double[] SheenColorFactor = [0.0, 0.0, 0.0];

    /// <summary>The optional sheen color texture.</summary>
    public GltfTextureInfo? SheenColorTexture;

    /// <summary>Whether the sheen roughness factor was explicitly provided.</summary>
    public bool HasSheenRoughnessFactor;

    /// <summary>The sheen layer roughness.</summary>
    public double SheenRoughnessFactor;

    /// <summary>The optional sheen roughness texture.</summary>
    public GltfTextureInfo? SheenRoughnessTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_specular</c> material payload.</summary>
public class GltfMaterialsSpecularExtension
{
    /// <summary>Whether the specular factor was explicitly provided.</summary>
    public bool HasSpecularFactor;

    /// <summary>The strength of non-metallic specular reflection.</summary>
    public double SpecularFactor = 1.0;

    /// <summary>The optional specular strength texture.</summary>
    public GltfTextureInfo? SpecularTexture;

    /// <summary>Whether the specular color factor was explicitly provided.</summary>
    public bool HasSpecularColorFactor;

    /// <summary>The linear RGB specular color factor.</summary>
    public double[] SpecularColorFactor = [1.0, 1.0, 1.0];

    /// <summary>The optional specular color texture.</summary>
    public GltfTextureInfo? SpecularColorTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_transmission</c> material payload.</summary>
public class GltfMaterialsTransmissionExtension
{
    /// <summary>Whether the transmission factor was explicitly provided.</summary>
    public bool HasTransmissionFactor;

    /// <summary>The fraction of light transmitted through the surface.</summary>
    public double TransmissionFactor;

    /// <summary>The optional transmission texture.</summary>
    public GltfTextureInfo? TransmissionTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the marker payload of <c>KHR_materials_unlit</c>.</summary>
public class GltfMaterialsUnlitExtension
{
    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_materials_volume</c> material payload.</summary>
public class GltfMaterialsVolumeExtension
{
    /// <summary>Whether the volume thickness factor was explicitly provided.</summary>
    public bool HasThicknessFactor;

    /// <summary>The volume thickness in mesh coordinate space.</summary>
    public double ThicknessFactor;

    /// <summary>The optional volume thickness texture.</summary>
    public GltfTextureInfo? ThicknessTexture;

    /// <summary>Whether attenuation distance was explicitly provided.</summary>
    public bool HasAttenuationDistance;

    /// <summary>The average attenuation distance in world space.</summary>
    public double AttenuationDistance;

    /// <summary>Whether attenuation color was explicitly provided.</summary>
    public bool HasAttenuationColor;

    /// <summary>The linear RGB attenuation color.</summary>
    public double[] AttenuationColor = [1.0, 1.0, 1.0];

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the snapshot payload of <c>KHR_materials_diffuse_transmission</c>.</summary>
public class GltfMaterialsDiffuseTransmissionExtension
{
    /// <summary>Whether diffuse transmission strength was explicitly provided.</summary>
    public bool HasDiffuseTransmissionFactor;

    /// <summary>The fraction of light diffusely transmitted through the surface.</summary>
    public double DiffuseTransmissionFactor;

    /// <summary>The optional diffuse transmission strength texture.</summary>
    public GltfTextureInfo? DiffuseTransmissionTexture;

    /// <summary>Whether diffuse transmission color was explicitly provided.</summary>
    public bool HasDiffuseTransmissionColorFactor;

    /// <summary>The linear RGB color of diffusely transmitted light.</summary>
    public double[] DiffuseTransmissionColorFactor = [1.0, 1.0, 1.0];

    /// <summary>The optional diffuse transmission color texture.</summary>
    public GltfTextureInfo? DiffuseTransmissionColorTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents archived <c>KHR_materials_pbrSpecularGlossiness</c> material data.</summary>
public class GltfMaterialsPbrSpecularGlossinessExtension
{
    /// <summary>Whether the diffuse factor was explicitly provided.</summary>
    public bool HasDiffuseFactor;

    /// <summary>The linear RGBA diffuse factor.</summary>
    public double[] DiffuseFactor = [1.0, 1.0, 1.0, 1.0];

    /// <summary>The optional diffuse texture.</summary>
    public GltfTextureInfo? DiffuseTexture;

    /// <summary>Whether the specular factor was explicitly provided.</summary>
    public bool HasSpecularFactor;

    /// <summary>The linear RGB specular factor.</summary>
    public double[] SpecularFactor = [1.0, 1.0, 1.0];

    /// <summary>Whether the glossiness factor was explicitly provided.</summary>
    public bool HasGlossinessFactor;

    /// <summary>The material glossiness factor.</summary>
    public double GlossinessFactor = 1.0;

    /// <summary>The optional combined specular-glossiness texture.</summary>
    public GltfTextureInfo? SpecularGlossinessTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
