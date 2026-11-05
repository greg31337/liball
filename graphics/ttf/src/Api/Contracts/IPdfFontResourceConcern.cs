namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontResourceConcern"/>.</summary>
public interface IPdfFontResourceConcern
{
    /// <summary>Gets the stable machine-readable concern kind.</summary>
    /// <returns>The concern identifier.</returns>
    PdfFontResourceConcernKind GetKind();

    /// <summary>Gets the number of affected codes, observations, or resources.</summary>
    /// <returns>A positive affected-item count.</returns>
    int GetAffectedCount();

    /// <summary>Gets a stable explanation of the font-level concern.</summary>
    /// <returns>The concern explanation.</returns>
    string GetMessage();

    /// <summary>Reports whether this is a PDF/A embedding concern.</summary>
    /// <returns>True for a PDF/A finding.</returns>
    bool IsPdfAConcern();

    /// <summary>Reports whether this is a PDF/UA Unicode concern.</summary>
    /// <returns>True for a PDF/UA finding.</returns>
    bool IsPdfUaConcern();
}
