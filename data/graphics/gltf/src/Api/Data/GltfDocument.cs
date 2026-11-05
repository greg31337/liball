using System.Collections.Generic;

namespace gltf;

/// <summary>Represents the root object and indexed object collections of a glTF document.</summary>
public class GltfDocument
{
    /// <summary>The required asset metadata.</summary>
    public GltfAsset Asset = new GltfAsset();

    /// <summary>The accessor collection.</summary>
    public List<GltfAccessor> Accessors = new List<GltfAccessor>();

    /// <summary>The animation collection.</summary>
    public List<GltfAnimation> Animations = new List<GltfAnimation>();

    /// <summary>The buffer collection.</summary>
    public List<GltfBuffer> Buffers = new List<GltfBuffer>();

    /// <summary>The buffer view collection.</summary>
    public List<GltfBufferView> BufferViews = new List<GltfBufferView>();

    /// <summary>The camera collection.</summary>
    public List<GltfCamera> Cameras = new List<GltfCamera>();

    /// <summary>The image collection.</summary>
    public List<GltfImage> Images = new List<GltfImage>();

    /// <summary>The material collection.</summary>
    public List<GltfMaterial> Materials = new List<GltfMaterial>();

    /// <summary>The mesh collection.</summary>
    public List<GltfMesh> Meshes = new List<GltfMesh>();

    /// <summary>The node collection.</summary>
    public List<GltfNode> Nodes = new List<GltfNode>();

    /// <summary>The sampler collection.</summary>
    public List<GltfSampler> Samplers = new List<GltfSampler>();

    /// <summary>The scene collection.</summary>
    public List<GltfScene> Scenes = new List<GltfScene>();

    /// <summary>The skin collection.</summary>
    public List<GltfSkin> Skins = new List<GltfSkin>();

    /// <summary>The texture collection.</summary>
    public List<GltfTexture> Textures = new List<GltfTexture>();

    /// <summary>Whether the root <c>scene</c> member was present.</summary>
    public bool HasDefaultScene;

    /// <summary>The default scene index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int DefaultScene = GltfConstants.AbsentIndex;

    /// <summary>Extension names declared by <c>extensionsUsed</c>.</summary>
    public List<string> ExtensionsUsed = new List<string>();

    /// <summary>Extension names declared by <c>extensionsRequired</c>.</summary>
    public List<string> ExtensionsRequired = new List<string>();

    /// <summary>Root extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();

    /// <summary>Owned resolved and generated resources associated with the document.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>Optional source container provenance and preserved chunk data.</summary>
    public GltfContainerData? Container;
}
