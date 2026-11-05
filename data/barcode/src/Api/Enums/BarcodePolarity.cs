namespace barcode;

/// <summary>
/// Identifies the expected relationship between barcode modules and their background.
/// </summary>
public enum BarcodePolarity
{
    /// <summary>
    /// Expects dark modules on a light background.
    /// </summary>
    DarkOnLight = 0,

    /// <summary>
    /// Expects light modules on a dark background.
    /// </summary>
    LightOnDark = 1,

    /// <summary>
    /// Determines the polarity automatically.
    /// </summary>
    Automatic = 2
}
