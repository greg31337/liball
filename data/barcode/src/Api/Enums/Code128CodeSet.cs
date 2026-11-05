namespace barcode;

/// <summary>
/// Selects a Code 128 code set or automatic code-set planning.
/// </summary>
public enum Code128CodeSet
{
    /// <summary>
    /// Selects code sets automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Forces Code Set A.
    /// </summary>
    A = 1,

    /// <summary>
    /// Forces Code Set B.
    /// </summary>
    B = 2,

    /// <summary>
    /// Forces Code Set C.
    /// </summary>
    C = 3
}
