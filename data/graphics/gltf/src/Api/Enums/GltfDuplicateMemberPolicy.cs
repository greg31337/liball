namespace gltf;

/// <summary>Controls how repeated JSON object member names are handled while reading.</summary>
public enum GltfDuplicateMemberPolicy
{
    /// <summary>Reject the JSON input when an object repeats a member name.</summary>
    Reject = 0,

    /// <summary>Retain the first value associated with a repeated member name.</summary>
    KeepFirst = 1,

    /// <summary>Retain the last value associated with a repeated member name.</summary>
    KeepLast = 2
}
