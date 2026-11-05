namespace odf;

/// <summary>
/// Identifies the value presentation described by an OpenDocument data style.
/// </summary>
public enum OdfDataStyleKind
{
    /// <summary>
    /// The data-style kind is not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A decimal number style.
    /// </summary>
    Number = 1,
    /// <summary>
    /// A scientific-number style.
    /// </summary>
    Scientific = 2,
    /// <summary>
    /// A fraction style.
    /// </summary>
    Fraction = 3,
    /// <summary>
    /// A percentage style.
    /// </summary>
    Percentage = 4,
    /// <summary>
    /// A currency style.
    /// </summary>
    Currency = 5,
    /// <summary>
    /// A date style.
    /// </summary>
    Date = 6,
    /// <summary>
    /// A time style.
    /// </summary>
    Time = 7,
    /// <summary>
    /// A boolean style.
    /// </summary>
    Boolean = 8,
    /// <summary>
    /// A text style.
    /// </summary>
    Text = 9
}
