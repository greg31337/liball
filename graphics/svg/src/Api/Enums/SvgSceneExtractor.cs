namespace svg;

/// <summary>Defines stable normalized-scene extraction outcomes.</summary>
public enum SvgSceneExtractionError
{
    /// <summary>No extraction failure occurred.</summary>
    None = 0,
    /// <summary>The scene, resolved document, target, or options were invalid or stale.</summary>
    InvalidArgument = 1,
    /// <summary>No scene node matched the fragment, source, or instance identity.</summary>
    TargetNotFound = 2,
    /// <summary>A node, resource, or command extraction bound was exceeded.</summary>
    LimitExceeded = 3,
    /// <summary>The cloned scene could not be bounded, planned, or enumerated.</summary>
    ProcessingFailed = 4
}
