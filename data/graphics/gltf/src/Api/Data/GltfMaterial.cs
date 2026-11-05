namespace gltf;

/// <summary>Represents a glTF physically based material.</summary>
public class GltfMaterial
{
    /// <summary>The optional metallic-roughness material parameters.</summary>
    public GltfPbrMetallicRoughness? PbrMetallicRoughness;

    /// <summary>The optional normal texture parameters.</summary>
    public GltfNormalTextureInfo? NormalTexture;

    /// <summary>The optional occlusion texture parameters.</summary>
    public GltfOcclusionTextureInfo? OcclusionTexture;

    /// <summary>The optional emissive texture parameters.</summary>
    public GltfTextureInfo? EmissiveTexture;

    /// <summary>Whether the emissive factor member was explicitly present.</summary>
    public bool HasEmissiveFactor;

    /// <summary>The linear emissive factor, initialized to the glTF zero default.</summary>
    public double[] EmissiveFactor = [0.0, 0.0, 0.0];

    /// <summary>Whether the alpha mode member was explicitly present.</summary>
    public bool HasAlphaMode;

    /// <summary>The alpha mode, initialized to the glTF opaque default.</summary>
    public GltfAlphaMode AlphaMode = GltfAlphaMode.Opaque;

    /// <summary>Whether the alpha cutoff member was explicitly present.</summary>
    public bool HasAlphaCutoff;

    /// <summary>The alpha mask cutoff, initialized to the glTF default of <c>0.5</c>.</summary>
    public double AlphaCutoff = 0.5;

    /// <summary>Whether the double-sided member was explicitly present.</summary>
    public bool HasDoubleSided;

    /// <summary>Whether both sides of triangles are rendered.</summary>
    public bool DoubleSided;

    /// <summary>The optional material name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the core metallic-roughness material parameters.</summary>
public class GltfPbrMetallicRoughness
{
    /// <summary>Whether the base color factor member was explicitly present.</summary>
    public bool HasBaseColorFactor;

    /// <summary>The linear base color factor, initialized to the glTF unit default.</summary>
    public double[] BaseColorFactor = [1.0, 1.0, 1.0, 1.0];

    /// <summary>The optional base color texture parameters.</summary>
    public GltfTextureInfo? BaseColorTexture;

    /// <summary>Whether the metallic factor member was explicitly present.</summary>
    public bool HasMetallicFactor;

    /// <summary>The metallic factor, initialized to the glTF unit default.</summary>
    public double MetallicFactor = 1.0;

    /// <summary>Whether the roughness factor member was explicitly present.</summary>
    public bool HasRoughnessFactor;

    /// <summary>The roughness factor, initialized to the glTF unit default.</summary>
    public double RoughnessFactor = 1.0;

    /// <summary>The optional combined metallic-roughness texture parameters.</summary>
    public GltfTextureInfo? MetallicRoughnessTexture;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a reference to a texture and texture coordinate set.</summary>
public class GltfTextureInfo
{
    /// <summary>The required texture index.</summary>
    public int Index = GltfConstants.AbsentIndex;

    /// <summary>Whether the texture coordinate member was explicitly present.</summary>
    public bool HasTextureCoordinate;

    /// <summary>The texture coordinate set index, initialized to the glTF zero default.</summary>
    public int TextureCoordinate;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents normal texture sampling parameters.</summary>
public class GltfNormalTextureInfo
{
    /// <summary>The required texture index.</summary>
    public int Index = GltfConstants.AbsentIndex;

    /// <summary>Whether the texture coordinate member was explicitly present.</summary>
    public bool HasTextureCoordinate;

    /// <summary>The texture coordinate set index, initialized to the glTF zero default.</summary>
    public int TextureCoordinate;

    /// <summary>Whether the normal scale member was explicitly present.</summary>
    public bool HasScale;

    /// <summary>The normal scale, initialized to the glTF unit default.</summary>
    public double Scale = 1.0;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents occlusion texture sampling parameters.</summary>
public class GltfOcclusionTextureInfo
{
    /// <summary>The required texture index.</summary>
    public int Index = GltfConstants.AbsentIndex;

    /// <summary>Whether the texture coordinate member was explicitly present.</summary>
    public bool HasTextureCoordinate;

    /// <summary>The texture coordinate set index, initialized to the glTF zero default.</summary>
    public int TextureCoordinate;

    /// <summary>Whether the occlusion strength member was explicitly present.</summary>
    public bool HasStrength;

    /// <summary>The occlusion strength, initialized to the glTF unit default.</summary>
    public double Strength = 1.0;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
