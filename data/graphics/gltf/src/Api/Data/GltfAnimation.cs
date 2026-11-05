using System.Collections.Generic;

namespace gltf;

/// <summary>Represents a glTF animation clip.</summary>
public class GltfAnimation
{
    /// <summary>The required animation channels.</summary>
    public List<GltfAnimationChannel> Channels = new List<GltfAnimationChannel>();

    /// <summary>The required animation samplers.</summary>
    public List<GltfAnimationSampler> Samplers = new List<GltfAnimationSampler>();

    /// <summary>The optional animation name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the input, output, and interpolation of an animation sampler.</summary>
public class GltfAnimationSampler
{
    /// <summary>The required input time accessor index.</summary>
    public int Input = GltfConstants.AbsentIndex;

    /// <summary>Whether the interpolation member was explicitly present.</summary>
    public bool HasInterpolation;

    /// <summary>The interpolation algorithm, initialized to the glTF linear default.</summary>
    public GltfAnimationInterpolation Interpolation = GltfAnimationInterpolation.Linear;

    /// <summary>The required output value accessor index.</summary>
    public int Output = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Connects an animation sampler to a target property.</summary>
public class GltfAnimationChannel
{
    /// <summary>The required animation-local sampler index.</summary>
    public int Sampler = GltfConstants.AbsentIndex;

    /// <summary>The required animation target.</summary>
    public GltfAnimationTarget Target = new GltfAnimationTarget();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Identifies the node property or extension pointer targeted by an animation channel.</summary>
public class GltfAnimationTarget
{
    /// <summary>Whether the target node reference was present.</summary>
    public bool HasNode;

    /// <summary>The target node index, or <see cref="GltfConstants.AbsentIndex"/> when absent.</summary>
    public int Node = GltfConstants.AbsentIndex;

    /// <summary>The required animation target path.</summary>
    public GltfAnimationPath Path = GltfAnimationPath.Translation;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
