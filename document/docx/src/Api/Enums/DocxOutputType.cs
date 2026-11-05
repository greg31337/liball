namespace docx;

/// <summary>
/// Specifies the WordprocessingML main-part type emitted while saving.
/// </summary>
public enum DocxOutputType
{
    /// <summary>
    /// Preserves the loaded main-part type.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Emits a Word document main-part type.
    /// </summary>
    Document = 1,
    /// <summary>
    /// Emits a Word template main-part type.
    /// </summary>
    Template = 2
}
