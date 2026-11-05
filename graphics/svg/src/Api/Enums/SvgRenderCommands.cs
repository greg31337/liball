namespace svg;

/// <summary>Identifies one backend-neutral render operation.</summary>
public enum SvgRenderCommandKind
{
    /// <summary>Saves the previous transform and installs an absolute node transform.</summary>
    PushTransform = 0,
    /// <summary>Restores the preceding transform.</summary>
    PopTransform = 1,
    /// <summary>Pushes a world-space overflow clip or referenced clipping resource.</summary>
    PushClip = 2,
    /// <summary>Restores the preceding clip state.</summary>
    PopClip = 3,
    /// <summary>Pushes a shared mask resource.</summary>
    PushMask = 4,
    /// <summary>Restores the preceding mask state.</summary>
    PopMask = 5,
    /// <summary>Begins a compositing layer with opacity, blend, isolation, and optional filter data.</summary>
    BeginLayer = 6,
    /// <summary>Composites and closes the current layer.</summary>
    EndLayer = 7,
    /// <summary>Fills normalized path geometry.</summary>
    FillPath = 8,
    /// <summary>Strokes normalized path geometry.</summary>
    StrokePath = 9,
    /// <summary>Draws referenced start, middle, and end markers.</summary>
    DrawMarkers = 10,
    /// <summary>Fills normalized text content.</summary>
    FillText = 11,
    /// <summary>Strokes normalized text content.</summary>
    StrokeText = 12,
    /// <summary>Draws a shared image resource.</summary>
    DrawImage = 13,
    /// <summary>Draws callback-supplied host-content raster output.</summary>
    DrawHostContent = 14
}

/// <summary>Defines stable render-command enumeration failures.</summary>
public enum SvgRenderCommandError
{
    /// <summary>No enumeration failure occurred.</summary>
    None = 0,
    /// <summary>The scene, layer plan, or options were invalid or stale.</summary>
    InvalidArgument = 1,
    /// <summary>The configured command bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>The generated transform, clip, mask, or layer scopes were not balanced.</summary>
    UnbalancedScopes = 3
}
