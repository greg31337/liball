namespace odf;

/// <summary>
/// Describes image color adjustment, opacity, and import-filter metadata.
/// </summary>
public class OdfGraphicImageAdjustment
{
    /// <summary>The image color mode.</summary>
    public string ColorMode = "";
    /// <summary>The preserved color-inversion boolean.</summary>
    public string ColorInversionValue = "";
    /// <summary>The luminance adjustment.</summary>
    public string Luminance = "";
    /// <summary>The contrast adjustment.</summary>
    public string Contrast = "";
    /// <summary>The gamma adjustment.</summary>
    public string Gamma = "";
    /// <summary>The red-channel adjustment.</summary>
    public string Red = "";
    /// <summary>The green-channel adjustment.</summary>
    public string Green = "";
    /// <summary>The blue-channel adjustment.</summary>
    public string Blue = "";
    /// <summary>The image opacity.</summary>
    public string Opacity = "";
    /// <summary>The direct <c>draw:filter-name</c> value on an image.</summary>
    public string FilterName = "";
}
