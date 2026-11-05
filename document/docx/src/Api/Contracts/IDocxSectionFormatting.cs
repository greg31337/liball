namespace docx;

/// <summary>
/// Reads and applies direct section formatting.
/// </summary>
public interface IDocxSectionFormatting
{
    /// <summary>
    /// Returns whether a section has direct formatting.
    /// </summary>
    /// <param name="section">The section to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasFormat(DocxSection section);

    /// <summary>
    /// Returns a section's direct format model.
    /// </summary>
    /// <param name="section">The section to inspect.</param>
    /// <returns>The section format model.</returns>
    DocxSectionFormat GetFormat(DocxSection section);

    /// <summary>
    /// Replaces a section's direct format model.
    /// </summary>
    /// <param name="section">The section to edit.</param>
    /// <param name="format">The replacement format.</param>
    void SetFormat(DocxSection section, DocxSectionFormat format);

    /// <summary>
    /// Returns a section format from a body by position.
    /// </summary>
    /// <param name="body">The containing body.</param>
    /// <param name="sectionIndex">The zero-based section position.</param>
    /// <returns>The section format model.</returns>
    DocxSectionFormat GetBodyFormat(DocxBody body, int sectionIndex);

    /// <summary>
    /// Applies a section format and marks its containing body content as changed.
    /// </summary>
    /// <param name="body">The containing body.</param>
    /// <param name="sectionIndex">The zero-based section position.</param>
    /// <param name="format">The replacement format.</param>
    void SetBodyFormat(DocxBody body, int sectionIndex, DocxSectionFormat format);
}
