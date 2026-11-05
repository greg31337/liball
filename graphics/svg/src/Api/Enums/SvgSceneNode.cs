namespace svg;

/// <summary>Identifies the payload stored by one normalized scene node.</summary>
public enum SvgSceneNodeKind
{
    /// <summary>A structural or compositing group.</summary>
    Group = 0,
    /// <summary>A normalized path geometry node.</summary>
    Path = 1,
    /// <summary>A normalized text node.</summary>
    Text = 2,
    /// <summary>A normalized image node.</summary>
    Image = 3,
    /// <summary>A normalized host-owned embedded-content node.</summary>
    HostContent = 4
}

/// <summary>Identifies the normalized image interpolation hint.</summary>
public enum SvgImageRendering
{
    /// <summary>Allows balanced interpolation.</summary>
    Auto = 0,
    /// <summary>Requests smooth interpolation.</summary>
    Smooth = 1,
    /// <summary>Requests higher-quality smooth interpolation.</summary>
    HighQuality = 2,
    /// <summary>Requests crisp nearest-neighbor interpolation.</summary>
    CrispEdges = 3,
    /// <summary>Requests pixel-preserving nearest-neighbor interpolation.</summary>
    Pixelated = 4,
    /// <summary>Requests faster nearest-neighbor interpolation.</summary>
    OptimizeSpeed = 5,
    /// <summary>Requests quality-oriented smooth interpolation.</summary>
    OptimizeQuality = 6
}
