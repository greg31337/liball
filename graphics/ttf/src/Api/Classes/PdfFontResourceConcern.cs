namespace ttf;

/// <summary>Describes one font-level concern for a requested PDF profile.</summary>
public class PdfFontResourceConcern : IPdfFontResourceConcern
{
    PdfFontResourceConcernKind kind;
    int affectedCount;
    string message;

    internal PdfFontResourceConcern(
        PdfFontResourceConcernKind kind,
        int affectedCount,
        string message)
    {
        this.kind = kind;
        this.affectedCount = affectedCount;
        this.message = message;
    }

    /// <summary>Gets the stable machine-readable concern kind.</summary>
    /// <returns>The concern identifier.</returns>
    public PdfFontResourceConcernKind GetKind()
    {
        return kind;
    }

    /// <summary>Gets the number of affected codes, observations, or resources.</summary>
    /// <returns>A positive affected-item count.</returns>
    public int GetAffectedCount()
    {
        return affectedCount;
    }

    /// <summary>Gets a stable explanation of the font-level concern.</summary>
    /// <returns>The concern explanation.</returns>
    public string GetMessage()
    {
        return message;
    }

    /// <summary>Reports whether this is a PDF/A embedding concern.</summary>
    /// <returns>True for a PDF/A finding.</returns>
    public bool IsPdfAConcern()
    {
        return kind ==
                PdfFontResourceConcernKind.PdfAEmbeddingRightsConflict ||
            kind == PdfFontResourceConcernKind.PdfA1OpenTypeFontProgram;
    }

    /// <summary>Reports whether this is a PDF/UA Unicode concern.</summary>
    /// <returns>True for a PDF/UA finding.</returns>
    public bool IsPdfUaConcern()
    {
        return kind ==
                PdfFontResourceConcernKind.PdfUaMissingUnicodeMappings ||
            kind == PdfFontResourceConcernKind.PdfUaActualTextRequired;
    }
}
