namespace svg;

/// <summary>Configures bounded scene transform and drawing-bounds precomputation.</summary>
public class SvgSceneBoundsOptions
{
    /// <summary>Whether computed stroke geometry contributes.</summary>
    public bool IncludeStroke;
    /// <summary>Whether referenced start, middle, and end markers contribute.</summary>
    public bool IncludeMarkers;
    /// <summary>Whether filter, clip, and mask regions affect final world bounds.</summary>
    public bool ApplyEffects;
    /// <summary>The maximum registered scene nodes processed.</summary>
    public int MaximumNodes;
    /// <summary>The maximum marker vertices processed for one path.</summary>
    public int MaximumMarkerVertices;

    /// <summary>Creates deterministic bounded scene-bounds defaults.</summary>
    public SvgSceneBoundsOptions()
    {
        IncludeStroke = true;
        IncludeMarkers = true;
        ApplyEffects = true;
        MaximumNodes = 100000;
        MaximumMarkerVertices = 100000;
    }
}

/// <summary>Contains a scene transform and drawing-bounds precomputation outcome.</summary>
public class SvgSceneBoundsResult
{
    /// <summary>Whether every registered node was processed.</summary>
    public bool Success;
    /// <summary>The stable drawing-bounds failure.</summary>
    public SvgDrawingBoundsError Error;
    /// <summary>The aggregate world-space scene bounds.</summary>
    public SvgRect? Bounds;
    /// <summary>The registered scene node count processed.</summary>
    public int ProcessedNodeCount;

    /// <summary>Creates one scene-bounds result.</summary>
    /// <param name="success">Whether processing succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="bounds">The aggregate world-space bounds.</param>
    /// <param name="processedNodeCount">The processed node count.</param>
    public SvgSceneBoundsResult(bool success, SvgDrawingBoundsError error, SvgRect? bounds, int processedNodeCount)
    {
        Success = success;
        Error = error;
        Bounds = bounds;
        ProcessedNodeCount = processedNodeCount;
    }
}
