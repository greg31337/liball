namespace gltf;

/// <summary>Identifies a built-in or hook-driven document transform.</summary>
public enum GltfTransformKind
{
    /// <summary>Remove objects unreachable from selected scenes.</summary>
    Prune = 0,

    /// <summary>Deduplicate equivalent objects and resources.</summary>
    Deduplicate = 1,

    /// <summary>Canonicalize object ordering and indices.</summary>
    Canonicalize = 2,

    /// <summary>Weld exactly equivalent vertices.</summary>
    Weld = 3,

    /// <summary>Expand indexed vertices into independent vertices.</summary>
    Unweld = 4,

    /// <summary>Join compatible mesh primitives.</summary>
    JoinPrimitives = 5,

    /// <summary>Split mesh primitives.</summary>
    SplitPrimitives = 6,

    /// <summary>Create GPU instances for repeated meshes.</summary>
    CreateGpuInstances = 7,

    /// <summary>Apply a matrix to selected node transforms.</summary>
    ApplyNodeTransform = 8,

    /// <summary>Bake selected node transforms into dependent data.</summary>
    BakeNodeTransforms = 9,

    /// <summary>Flatten selected node hierarchies.</summary>
    FlattenHierarchy = 10,

    /// <summary>Center scene content.</summary>
    CenterScene = 11,

    /// <summary>Move scene content onto a ground plane.</summary>
    GroundScene = 12,

    /// <summary>Reverse primitive winding and tangent handedness.</summary>
    FlipWinding = 13,

    /// <summary>Convert scene units.</summary>
    ConvertUnits = 14,

    /// <summary>Convert coordinate axes.</summary>
    ConvertAxes = 15,

    /// <summary>Generate accessor minima, maxima, and bounds.</summary>
    GenerateBounds = 16,

    /// <summary>Generate vertex normals.</summary>
    GenerateNormals = 17,

    /// <summary>Remove redundant animation keys.</summary>
    RemoveRedundantAnimationKeys = 18,

    /// <summary>Quantize numeric accessor data.</summary>
    Quantize = 19,

    /// <summary>Dequantize numeric accessor data.</summary>
    Dequantize = 20,

    /// <summary>Convert dense accessor data to sparse form.</summary>
    DenseToSparse = 21,

    /// <summary>Materialize sparse accessor data.</summary>
    SparseToDense = 22,

    /// <summary>Consolidate binary buffers.</summary>
    ConsolidateBuffers = 23,

    /// <summary>Partition binary buffers.</summary>
    PartitionBuffers = 24,

    /// <summary>Repack buffer views deterministically.</summary>
    PackBuffers = 25,

    /// <summary>Embed external resources.</summary>
    EmbedResources = 26,

    /// <summary>Extract embedded resources.</summary>
    ExtractResources = 27,

    /// <summary>Generate vertex tangents through a registered hook.</summary>
    GenerateTangents = 28,

    /// <summary>Generate texture coordinates through a registered hook.</summary>
    GenerateTextureCoordinates = 29,

    /// <summary>Reorder vertices through a registered hook.</summary>
    ReorderVertices = 30,

    /// <summary>Simplify meshes through a registered hook.</summary>
    Simplify = 31,

    /// <summary>Generate levels of detail through a registered hook.</summary>
    GenerateLod = 32,

    /// <summary>Encode Draco geometry through a registered codec.</summary>
    EncodeDraco = 33,

    /// <summary>Decode Draco geometry through a registered codec.</summary>
    DecodeDraco = 34,

    /// <summary>Encode Meshopt data through a registered codec.</summary>
    EncodeMeshopt = 35,

    /// <summary>Decode Meshopt data through a registered codec.</summary>
    DecodeMeshopt = 36,

    /// <summary>Convert encoded images through a registered codec.</summary>
    ConvertImages = 37,

    /// <summary>Create a material palette through a registered hook.</summary>
    CreatePalette = 38,

    /// <summary>Run an application-defined transform hook.</summary>
    Custom = 39
}
