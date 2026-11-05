namespace barcode;

/// <summary>
/// Controls whether normal or mirrored barcode orientations are considered.
/// </summary>
public enum BarcodeMirrorMode
{
    /// <summary>
    /// Considers only the normal orientation.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Considers only the mirrored orientation.
    /// </summary>
    Mirrored = 1,

    /// <summary>
    /// Considers both normal and mirrored orientations.
    /// </summary>
    Both = 2
}
