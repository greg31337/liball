using System.Collections.Generic;

namespace gltf;

/// <summary>Represents a glTF mesh.</summary>
public class GltfMesh
{
    /// <summary>The mesh primitives.</summary>
    public List<GltfMeshPrimitive> Primitives = new List<GltfMeshPrimitive>();

    /// <summary>The optional default morph target weights.</summary>
    public double[]? Weights;

    /// <summary>The optional mesh name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one renderable primitive of a glTF mesh.</summary>
public class GltfMeshPrimitive
{
    /// <summary>The vertex attributes attached to the primitive.</summary>
    public List<GltfAttribute> Attributes = new List<GltfAttribute>();

    /// <summary>Whether the indices accessor reference was present.</summary>
    public bool HasIndices;

    /// <summary>The indices accessor index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Indices = GltfConstants.AbsentIndex;

    /// <summary>Whether the material reference was present.</summary>
    public bool HasMaterial;

    /// <summary>The material index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Material = GltfConstants.AbsentIndex;

    /// <summary>Whether the primitive mode member was explicitly present.</summary>
    public bool HasMode;

    /// <summary>The primitive topology, initialized to the glTF triangles default.</summary>
    public GltfPrimitiveMode Mode = GltfPrimitiveMode.Triangles;

    /// <summary>The morph targets attached to the primitive.</summary>
    public List<GltfMorphTarget> Targets = new List<GltfMorphTarget>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Associates an attribute semantic with an accessor index.</summary>
public class GltfAttribute
{
    /// <summary>The attribute semantic.</summary>
    public string Semantic = "";

    /// <summary>The referenced accessor index.</summary>
    public int Accessor = GltfConstants.AbsentIndex;
}

/// <summary>Represents the attribute displacement accessors of a morph target.</summary>
public class GltfMorphTarget
{
    /// <summary>The morph target attributes.</summary>
    public List<GltfAttribute> Attributes = new List<GltfAttribute>();
}
