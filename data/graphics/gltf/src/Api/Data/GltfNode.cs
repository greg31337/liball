using System.Collections.Generic;

namespace gltf;

/// <summary>Represents a node in a glTF scene hierarchy.</summary>
public class GltfNode
{
    /// <summary>Indices of child nodes.</summary>
    public List<int> Children = new List<int>();

    /// <summary>Whether the camera reference was present.</summary>
    public bool HasCamera;

    /// <summary>The camera index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Camera = GltfConstants.AbsentIndex;

    /// <summary>Whether the skin reference was present.</summary>
    public bool HasSkin;

    /// <summary>The skin index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Skin = GltfConstants.AbsentIndex;

    /// <summary>Whether the mesh reference was present.</summary>
    public bool HasMesh;

    /// <summary>The mesh index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Mesh = GltfConstants.AbsentIndex;

    /// <summary>Whether the matrix member was present.</summary>
    public bool HasMatrix;

    /// <summary>The column-major local matrix, initialized to the glTF identity default.</summary>
    public double[] Matrix =
    [
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    ];

    /// <summary>Whether the rotation member was present.</summary>
    public bool HasRotation;

    /// <summary>The local quaternion rotation, initialized to the glTF identity default.</summary>
    public double[] Rotation = [0.0, 0.0, 0.0, 1.0];

    /// <summary>Whether the scale member was present.</summary>
    public bool HasScale;

    /// <summary>The local scale, initialized to the glTF unit default.</summary>
    public double[] Scale = [1.0, 1.0, 1.0];

    /// <summary>Whether the translation member was present.</summary>
    public bool HasTranslation;

    /// <summary>The local translation, initialized to the glTF zero default.</summary>
    public double[] Translation = [0.0, 0.0, 0.0];

    /// <summary>The optional instantiated morph target weights.</summary>
    public double[]? Weights;

    /// <summary>The optional node name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
