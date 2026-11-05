using System.Collections.Generic;

namespace gltf;

/// <summary>Controls non-mutating document inspection.</summary>
public class GltfInspectionOptions
{
    /// <summary>Whether dependency edges are enumerated.</summary>
    public bool IncludeDependencies = true;

    /// <summary>Whether reverse dependency edges are enumerated.</summary>
    public bool IncludeReverseDependencies = true;

    /// <summary>Whether scene and primitive bounds are calculated.</summary>
    public bool CalculateBounds = true;

    /// <summary>Whether accessor data may be decoded for exact statistics.</summary>
    public bool DecodeAccessors = true;
}

/// <summary>Describes one reference edge between indexed glTF objects.</summary>
public class GltfDependency
{
    /// <summary>The source collection name.</summary>
    public string SourceCollection = "";

    /// <summary>The source object index.</summary>
    public int SourceIndex = GltfConstants.AbsentIndex;

    /// <summary>The source member name.</summary>
    public string SourceMember = "";

    /// <summary>The target collection name.</summary>
    public string TargetCollection = "";

    /// <summary>The target object index.</summary>
    public int TargetIndex = GltfConstants.AbsentIndex;
}

/// <summary>Contains statistics, dependencies, and diagnostics from document inspection.</summary>
public class GltfInspectionResult
{
    /// <summary>The overall operation status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The collected document statistics.</summary>
    public GltfDocumentStatistics Statistics = new GltfDocumentStatistics();

    /// <summary>Forward dependency edges in deterministic order.</summary>
    public List<GltfDependency> Dependencies = new List<GltfDependency>();

    /// <summary>Reverse dependency edges in deterministic order.</summary>
    public List<GltfDependency> ReverseDependencies = new List<GltfDependency>();

    /// <summary>Inspection diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
