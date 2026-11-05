using System.Collections.Generic;

namespace gltf;

/// <summary>Represents a glTF skin and its joint hierarchy references.</summary>
public class GltfSkin
{
    /// <summary>Whether the inverse bind matrices accessor reference was present.</summary>
    public bool HasInverseBindMatrices;

    /// <summary>The inverse bind matrices accessor index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int InverseBindMatrices = GltfConstants.AbsentIndex;

    /// <summary>Whether the skeleton root reference was present.</summary>
    public bool HasSkeleton;

    /// <summary>The skeleton root node index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Skeleton = GltfConstants.AbsentIndex;

    /// <summary>The required joint node indices.</summary>
    public List<int> Joints = new List<int>();

    /// <summary>The optional skin name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
