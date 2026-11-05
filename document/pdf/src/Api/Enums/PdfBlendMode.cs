namespace pdf;

/// <summary>
/// Specifies the blend mode used when painting transparent PDF graphics.
/// </summary>
public enum PdfBlendMode
{
    /// <summary>
    /// Paints source colors normally.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Multiplies source and backdrop colors.
    /// </summary>
    Multiply = 1,
    /// <summary>
    /// Screens source and backdrop colors.
    /// </summary>
    Screen = 2,
    /// <summary>
    /// Overlays source colors on the backdrop.
    /// </summary>
    Overlay = 3,
    /// <summary>
    /// Selects the darker component values.
    /// </summary>
    Darken = 4,
    /// <summary>
    /// Selects the lighter component values.
    /// </summary>
    Lighten = 5,
    /// <summary>
    /// Brightens the backdrop to reflect the source.
    /// </summary>
    ColorDodge = 6,
    /// <summary>
    /// Darkens the backdrop to reflect the source.
    /// </summary>
    ColorBurn = 7,
    /// <summary>
    /// Applies hard-light blending.
    /// </summary>
    HardLight = 8,
    /// <summary>
    /// Applies soft-light blending.
    /// </summary>
    SoftLight = 9,
    /// <summary>
    /// Subtracts the darker color from the lighter color.
    /// </summary>
    Difference = 10,
    /// <summary>
    /// Applies a lower-contrast difference blend.
    /// </summary>
    Exclusion = 11,
    /// <summary>
    /// Uses the source hue with backdrop saturation and luminosity.
    /// </summary>
    Hue = 12,
    /// <summary>
    /// Uses the source saturation with backdrop hue and luminosity.
    /// </summary>
    Saturation = 13,
    /// <summary>
    /// Uses the source hue and saturation with backdrop luminosity.
    /// </summary>
    Color = 14,
    /// <summary>
    /// Uses the source luminosity with backdrop hue and saturation.
    /// </summary>
    Luminosity = 15
}
