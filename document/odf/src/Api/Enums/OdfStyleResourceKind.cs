namespace odf;

/// <summary>
/// Identifies a reusable or nested OpenDocument style resource.
/// </summary>
public enum OdfStyleResourceKind
{
    /// <summary>
    /// A color gradient definition.
    /// </summary>
    Gradient = 0,
    /// <summary>
    /// A hatch definition.
    /// </summary>
    Hatch = 1,
    /// <summary>
    /// A fill-image definition.
    /// </summary>
    FillImage = 2,
    /// <summary>
    /// An opacity gradient definition.
    /// </summary>
    Opacity = 3,
    /// <summary>
    /// A stroke-dash definition.
    /// </summary>
    StrokeDash = 4,
    /// <summary>
    /// A line marker definition.
    /// </summary>
    Marker = 5,
    /// <summary>
    /// A nested background image.
    /// </summary>
    BackgroundImage = 6,
    /// <summary>
    /// A nested sound reference.
    /// </summary>
    Sound = 7,
    /// <summary>
    /// A presentation page-layout definition.
    /// </summary>
    PresentationLayout = 8
}
