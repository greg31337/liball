namespace fake;

/// <summary>
/// Selects Unicode normalization applied to generated text.
/// </summary>
public enum NormalizationMode
{
    /// <summary>
    /// Leaves generated text in its authored normalization form.
    /// </summary>
    None = 0,

    /// <summary>
    /// Applies canonical composition form NFC.
    /// </summary>
    Nfc = 1,

    /// <summary>
    /// Applies canonical decomposition form NFD.
    /// </summary>
    Nfd = 2,

    /// <summary>
    /// Applies compatibility composition form NFKC.
    /// </summary>
    Nfkc = 3,

    /// <summary>
    /// Applies compatibility decomposition form NFKD.
    /// </summary>
    Nfkd = 4
}
