namespace gltf;

/// <summary>Identifies how output resources are represented.</summary>
public enum GltfResourceMode
{
    /// <summary>Preserve the existing resource representation where possible.</summary>
    Preserve = 0,

    /// <summary>Write resources as external files.</summary>
    External = 1,

    /// <summary>Embed resources in data URIs.</summary>
    DataUri = 2,

    /// <summary>Embed image resources in buffer views.</summary>
    BufferView = 3,

    /// <summary>Store binary resources in the GLB BIN chunk.</summary>
    GlbBin = 4
}
