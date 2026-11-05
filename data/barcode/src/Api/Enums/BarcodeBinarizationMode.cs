namespace barcode;

/// <summary>
/// Selects the method used to convert luminance samples into binary modules.
/// </summary>
public enum BarcodeBinarizationMode
{
    /// <summary>
    /// Uses one global threshold.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Uses an Otsu histogram threshold.
    /// </summary>
    Otsu = 1,

    /// <summary>
    /// Uses thresholds calculated independently for image rows.
    /// </summary>
    Row = 2,

    /// <summary>
    /// Uses locally adaptive thresholds.
    /// </summary>
    Adaptive = 3,

    /// <summary>
    /// Combines global and adaptive thresholding.
    /// </summary>
    Hybrid = 4,

    /// <summary>
    /// Selects the thresholding method automatically.
    /// </summary>
    Automatic = 5
}
