namespace barcode;

/// <summary>
/// Selects the strategy used to locate barcode candidates in an image.
/// </summary>
public enum BarcodeLocalizationMode
{
    /// <summary>
    /// Scans the image directly without a separate localization pass.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Locates candidates from connected image blocks.
    /// </summary>
    ConnectedBlocks = 1,

    /// <summary>
    /// Locates candidates from coherent line evidence.
    /// </summary>
    Lines = 2,

    /// <summary>
    /// Locates candidates from symbology finder patterns.
    /// </summary>
    FinderPatterns = 3,

    /// <summary>
    /// Selects localization strategies automatically.
    /// </summary>
    Automatic = 4
}
