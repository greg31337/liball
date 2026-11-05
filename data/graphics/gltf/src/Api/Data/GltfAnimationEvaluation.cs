using System.Collections.Generic;

namespace gltf;

/// <summary>Controls animation sampling and time wrapping.</summary>
public class GltfAnimationEvaluationOptions
{
    /// <summary>The animation index to sample.</summary>
    public int Animation = GltfConstants.AbsentIndex;

    /// <summary>The sample time in seconds.</summary>
    public double Time;

    /// <summary>The time mapping mode outside the key range.</summary>
    public GltfAnimationWrapMode WrapMode = GltfAnimationWrapMode.Clamp;

    /// <summary>Whether sampled quaternions are normalized.</summary>
    public bool NormalizeRotations = true;
}

/// <summary>Contains independently owned animation values sampled at one time.</summary>
public class GltfAnimationPose
{
    /// <summary>The overall sampling status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The sampled animation index.</summary>
    public int Animation = GltfConstants.AbsentIndex;

    /// <summary>The mapped sample time in seconds.</summary>
    public double Time;

    /// <summary>Sampled node translations keyed by node index.</summary>
    public Dictionary<int, double[]> Translations = new Dictionary<int, double[]>();

    /// <summary>Sampled node rotations keyed by node index.</summary>
    public Dictionary<int, double[]> Rotations = new Dictionary<int, double[]>();

    /// <summary>Sampled node scales keyed by node index.</summary>
    public Dictionary<int, double[]> Scales = new Dictionary<int, double[]>();

    /// <summary>Sampled morph weights keyed by node index.</summary>
    public Dictionary<int, double[]> Weights = new Dictionary<int, double[]>();

    /// <summary>Sampled animation pointer values keyed by RFC 6901 pointer.</summary>
    public Dictionary<string, double[]> PointerValues = new Dictionary<string, double[]>();

    /// <summary>Sampling diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
