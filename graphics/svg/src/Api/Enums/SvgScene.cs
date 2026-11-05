namespace svg;

/// <summary>Identifies the payload stored by one shared scene resource.</summary>
public enum SvgSceneResourceKind
{
    /// <summary>A constant color paint.</summary>
    SolidPaint = 0,
    /// <summary>A linear or radial gradient paint.</summary>
    GradientPaint = 1,
    /// <summary>A repeating pattern paint.</summary>
    PatternPaint = 2,
    /// <summary>An encoded or host-provided image.</summary>
    Image = 3,
    /// <summary>A reusable path marker.</summary>
    Marker = 4,
    /// <summary>A reusable clipping graph.</summary>
    Clip = 5,
    /// <summary>A reusable alpha or luminance mask.</summary>
    Mask = 6,
    /// <summary>A normalized filter primitive graph.</summary>
    Filter = 7
}
