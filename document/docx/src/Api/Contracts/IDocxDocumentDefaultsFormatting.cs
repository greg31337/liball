namespace docx;

/// <summary>
/// Reads and applies document-wide paragraph and run defaults.
/// </summary>
public interface IDocxDocumentDefaultsFormatting
{
    /// <summary>
    /// Returns whether paragraph defaults are present.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>True when paragraph defaults are present; otherwise false.</returns>
    bool HasParagraphDefaults(DocxStyles styles);

    /// <summary>
    /// Returns the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>The paragraph format model.</returns>
    DocxParagraphFormat GetParagraphDefaults(DocxStyles styles);

    /// <summary>
    /// Replaces the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    /// <param name="format">The replacement paragraph format.</param>
    void SetParagraphDefaults(DocxStyles styles, DocxParagraphFormat format);

    /// <summary>
    /// Removes the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    void ClearParagraphDefaults(DocxStyles styles);

    /// <summary>
    /// Returns whether run defaults are present.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>True when run defaults are present; otherwise false.</returns>
    bool HasRunDefaults(DocxStyles styles);

    /// <summary>
    /// Returns the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>The run format model.</returns>
    DocxRunFormat GetRunDefaults(DocxStyles styles);

    /// <summary>
    /// Replaces the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    /// <param name="format">The replacement run format.</param>
    void SetRunDefaults(DocxStyles styles, DocxRunFormat format);

    /// <summary>
    /// Removes the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    void ClearRunDefaults(DocxStyles styles);
}
