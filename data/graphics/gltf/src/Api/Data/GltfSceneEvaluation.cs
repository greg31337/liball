using System.Collections.Generic;
using System.Numerics;

namespace gltf;

/// <summary>Controls scene traversal, pose, skin, camera, and bounds evaluation.</summary>
public class GltfSceneEvaluationOptions
{
    /// <summary>Whether a scene index was explicitly selected.</summary>
    public bool HasScene;

    /// <summary>The selected scene index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Scene = GltfConstants.AbsentIndex;

    /// <summary>Whether skinning matrices are calculated.</summary>
    public bool CalculateSkins = true;

    /// <summary>Whether camera projections are calculated.</summary>
    public bool CalculateCameras = true;

    /// <summary>Whether scene and primitive bounds are calculated.</summary>
    public bool CalculateBounds = true;

    /// <summary>Whether morph target displacement is included in bounds.</summary>
    public bool IncludeMorphTargets;
}

/// <summary>Contains evaluated node poses and optional scene bounds.</summary>
public class GltfScenePose
{
    /// <summary>The overall evaluation status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The evaluated scene index.</summary>
    public int Scene = GltfConstants.AbsentIndex;

    /// <summary>Node poses in deterministic node-index order.</summary>
    public List<GltfNodePose> Nodes = new List<GltfNodePose>();

    /// <summary>Whether scene bounds were calculated.</summary>
    public bool HasBounds;

    /// <summary>The calculated scene bounds.</summary>
    public GltfBounds Bounds = new GltfBounds();

    /// <summary>Evaluation diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

/// <summary>Contains evaluated transforms and derived matrices for one node.</summary>
public class GltfNodePose
{
    /// <summary>The node index.</summary>
    public int Node = GltfConstants.AbsentIndex;

    /// <summary>The evaluated local transform.</summary>
    public Matrix4x4 LocalTransform = Matrix4x4.Identity;

    /// <summary>The evaluated world transform.</summary>
    public Matrix4x4 WorldTransform = Matrix4x4.Identity;

    /// <summary>Whether a normal transform was calculated.</summary>
    public bool HasNormalTransform;

    /// <summary>The inverse-transpose normal transform.</summary>
    public Matrix4x4 NormalTransform = Matrix4x4.Identity;

    /// <summary>Skin palette matrices for the node's skin.</summary>
    public List<Matrix4x4> SkinMatrices = new List<Matrix4x4>();

    /// <summary>Whether a camera projection was calculated.</summary>
    public bool HasProjection;

    /// <summary>The camera projection matrix.</summary>
    public Matrix4x4 Projection = Matrix4x4.Identity;

    /// <summary>Whether the node remains visible after inherited visibility is applied.</summary>
    public bool Visible = true;

    /// <summary>Whether the node remains hoverable after inherited hoverability is applied.</summary>
    public bool Hoverable = true;

    /// <summary>Whether the node remains selectable after inherited selectability is applied.</summary>
    public bool Selectable = true;

    /// <summary>Whether the node references a punctual light.</summary>
    public bool HasPunctualLight;

    /// <summary>The referenced punctual-light index.</summary>
    public int PunctualLight = GltfConstants.AbsentIndex;

    /// <summary>The number of GPU instances, or zero when the node is not instanced.</summary>
    public int InstanceCount;

    /// <summary>GPU instance attribute accessor indices keyed by semantic.</summary>
    public Dictionary<string, int> InstanceAttributes = new Dictionary<string, int>();
}

/// <summary>Represents an axis-aligned three-dimensional bound.</summary>
public class GltfBounds
{
    /// <summary>Whether the bounds contain no points.</summary>
    public bool IsEmpty = true;

    /// <summary>The minimum coordinate.</summary>
    public Vector3 Minimum;

    /// <summary>The maximum coordinate.</summary>
    public Vector3 Maximum;
}

/// <summary>Contains a calculated camera projection matrix and diagnostics.</summary>
public class GltfProjectionResult
{
    /// <summary>The overall projection calculation status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The calculated projection matrix.</summary>
    public Matrix4x4 Projection = Matrix4x4.Identity;

    /// <summary>Projection diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
