namespace svg;

/// <summary>Identifies one semantic reason retained by a scene layer.</summary>
public enum SvgSceneLayerFeature
{
    /// <summary>The layer represents the scene root.</summary>
    Root = 0,
    /// <summary>Group opacity requires one composited result.</summary>
    Opacity = 1,
    /// <summary>A non-normal blend mode affects backdrop compositing.</summary>
    Blend = 2,
    /// <summary>Explicit isolation establishes a new stacking context.</summary>
    Isolation = 3,
    /// <summary>An overflow or clipping-path clip applies.</summary>
    Clip = 4,
    /// <summary>A mask resource applies.</summary>
    Mask = 5,
    /// <summary>A filter resource applies.</summary>
    Filter = 6
}
