namespace icc;

/// <summary>
/// Identifies a normalized or generation-specific PCS number encoding.
/// </summary>
public enum IccPcsEncodingKind
{
    /// <summary>
    /// Identifies normalized floating-point PCS values.
    /// </summary>
    Normalized = 0,

    /// <summary>
    /// Identifies ICC.1 version 2 PCS encoding.
    /// </summary>
    Version2 = 1,

    /// <summary>
    /// Identifies ICC.1 version 4 PCS encoding.
    /// </summary>
    Version4 = 2,

    /// <summary>
    /// Identifies ICC.2 version 5 PCS encoding.
    /// </summary>
    Version5 = 3
}
