namespace icc;

/// <summary>
/// Identifies a supported colour-difference formula.
/// </summary>
public enum IccColorDifferenceFormula
{
    /// <summary>
    /// Identifies the CIE 1976 colour-difference formula.
    /// </summary>
    Cie76 = 0,

    /// <summary>
    /// Identifies the CIE94 graphic-arts formula.
    /// </summary>
    Cie94GraphicArts = 1,

    /// <summary>
    /// Identifies the CIE94 textiles formula.
    /// </summary>
    Cie94Textiles = 2,

    /// <summary>
    /// Identifies the CIEDE2000 formula.
    /// </summary>
    Ciede2000 = 3
}
