namespace docx;

/// <summary>
/// Specifies the save mode used by DOCX operations.
/// </summary>
public enum DocxSaveMode
{
    /// <summary>
    /// Writes a normal DOCX package without imposing deterministic entry ordering.
    /// </summary>
    Standard = 0,
    /// <summary>
    /// Uses stable package ordering and timestamps so equivalent output can be compared byte for byte in tests.
    /// </summary>
    DeterministicRoundTrip = 1
}
