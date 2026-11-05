namespace docx;

/// <summary>
/// Reads and applies direct section formatting.
/// </summary>
public class DocxSectionFormatting : IDocxSectionFormatting
{
    /// <summary>
    /// Creates a section-formatting service.
    /// </summary>
    public DocxSectionFormatting()
    {
    }

    /// <summary>
    /// Returns whether a section has direct formatting.
    /// </summary>
    /// <param name="section">The section to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasFormat(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return section.HasFormat();
    }

    /// <summary>
    /// Returns a section's direct format model.
    /// </summary>
    /// <param name="section">The section to inspect.</param>
    /// <returns>The section format model.</returns>
    public DocxSectionFormat GetFormat(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return section.Format;
    }

    /// <summary>
    /// Replaces a section's direct format model.
    /// </summary>
    /// <param name="section">The section to edit.</param>
    /// <param name="format">The replacement format.</param>
    public void SetFormat(DocxSection section, DocxSectionFormat format)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        section.SetFormat(format);
    }

    /// <summary>
    /// Returns a section format from a body by position.
    /// </summary>
    /// <param name="body">The containing body.</param>
    /// <param name="sectionIndex">The zero-based section position.</param>
    /// <returns>The section format model.</returns>
    public DocxSectionFormat GetBodyFormat(DocxBody body, int sectionIndex)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        return body.GetSection(sectionIndex).Format;
    }

    /// <summary>
    /// Applies a section format and marks its containing body content as changed.
    /// </summary>
    /// <param name="body">The containing body.</param>
    /// <param name="sectionIndex">The zero-based section position.</param>
    /// <param name="format">The replacement format.</param>
    public void SetBodyFormat(DocxBody body, int sectionIndex, DocxSectionFormat format)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        DocxSection section = body.GetSection(sectionIndex);
        section.SetFormat(format);
        body.ClearSectionOwnerProperties(sectionIndex);
    }
}
