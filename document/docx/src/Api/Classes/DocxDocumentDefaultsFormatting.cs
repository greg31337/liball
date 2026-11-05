namespace docx;

/// <summary>
/// Reads and applies document-wide paragraph and run defaults.
/// </summary>
public class DocxDocumentDefaultsFormatting : IDocxDocumentDefaultsFormatting
{
    /// <summary>
    /// Creates a document-default formatting service.
    /// </summary>
    public DocxDocumentDefaultsFormatting()
    {
    }

    /// <summary>
    /// Returns whether paragraph defaults are present.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>True when paragraph defaults are present; otherwise false.</returns>
    public bool HasParagraphDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        return styles.DocumentDefaults.HasParagraphFormat();
    }

    /// <summary>
    /// Returns the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>The paragraph format model.</returns>
    public DocxParagraphFormat GetParagraphDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        return styles.DocumentDefaults.ParagraphFormat;
    }

    /// <summary>
    /// Replaces the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    /// <param name="format">The replacement paragraph format.</param>
    public void SetParagraphDefaults(DocxStyles styles, DocxParagraphFormat format)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        styles.SetParagraphDefaults(format);
    }

    /// <summary>
    /// Removes the document-wide paragraph defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    public void ClearParagraphDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        styles.ClearParagraphDefaults();
    }

    /// <summary>
    /// Returns whether run defaults are present.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>True when run defaults are present; otherwise false.</returns>
    public bool HasRunDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        return styles.DocumentDefaults.HasRunFormat();
    }

    /// <summary>
    /// Returns the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>The run format model.</returns>
    public DocxRunFormat GetRunDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        return styles.DocumentDefaults.RunFormat;
    }

    /// <summary>
    /// Replaces the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    /// <param name="format">The replacement run format.</param>
    public void SetRunDefaults(DocxStyles styles, DocxRunFormat format)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        styles.SetRunDefaults(format);
    }

    /// <summary>
    /// Removes the document-wide run defaults.
    /// </summary>
    /// <param name="styles">The styles model to edit.</param>
    public void ClearRunDefaults(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        styles.ClearRunDefaults();
    }
}
