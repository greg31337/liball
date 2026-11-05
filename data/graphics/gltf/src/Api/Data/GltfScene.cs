using System.Collections.Generic;

namespace gltf;

/// <summary>Represents a glTF scene and its root nodes.</summary>
public class GltfScene
{
    /// <summary>Indices of root nodes in the scene.</summary>
    public List<int> Nodes = new List<int>();

    /// <summary>The optional scene name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
