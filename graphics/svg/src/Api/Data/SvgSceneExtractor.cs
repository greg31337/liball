namespace svg;

/// <summary>Configures bounded immutable scene extraction.</summary>
public class SvgSceneExtractionOptions
{
    /// <summary>The maximum cloned ancestor and descendant node count.</summary>
    public int MaximumNodes;
    /// <summary>The maximum transitively retained shared resource count.</summary>
    public int MaximumResources;
    /// <summary>The maximum generated render command count.</summary>
    public int MaximumCommands;

    /// <summary>Creates deterministic bounded extraction defaults.</summary>
    public SvgSceneExtractionOptions()
    {
        MaximumNodes = 100000;
        MaximumResources = 10000;
        MaximumCommands = 1000000;
    }
}

/// <summary>Contains one independently rooted extracted scene and target identity.</summary>
public class SvgSceneExtractionResult
{
    /// <summary>The extracted scene, or <c>null</c> after failure.</summary>
    public SvgScene? Scene;
    /// <summary>The cloned requested target within the extracted scene, or <c>null</c>.</summary>
    public SvgSceneNode? Target;
    /// <summary>The exact requested node in the full scene, or <c>null</c>.</summary>
    public SvgSceneNode? SourceTarget;
    /// <summary>The number of retained target ancestors.</summary>
    public int AncestorCount;
    /// <summary>Whether extraction and scene processing completed.</summary>
    public bool Success;
    /// <summary>The stable extraction outcome.</summary>
    public SvgSceneExtractionError Error;

    internal SvgSceneExtractionResult()
    {
        Scene = null;
        Target = null;
        SourceTarget = null;
        AncestorCount = 0;
        Success = false;
        Error = SvgSceneExtractionError.None;
    }
}
