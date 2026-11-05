namespace ttf;

/// <summary>Provides one shared base name for every related PDF font object.</summary>
/// <remarks>
/// Descriptor <c>FontName</c>, composite and descendant <c>BaseFont</c>
/// entries, and the embedded program's PostScript name all use the same
/// decoded deterministic value. PDF dictionary consumers share one escaped
/// <see cref="PdfName"/> instance.
/// </remarks>
public class PdfFontResourceNames : IPdfFontResourceNames
{
    PdfName baseFontName;

    internal PdfFontResourceNames(PdfName baseFontName)
    {
        this.baseFontName = baseFontName;
    }

    /// <summary>Gets the decoded deterministic base font name.</summary>
    /// <returns>The subset-prefixed or full-font PostScript name.</returns>
    public string GetBaseFontName() { return baseFontName.GetValue(); }

    /// <summary>Gets the shared name for FontDescriptor <c>FontName</c>.</summary>
    /// <returns>The validated and escaped PDF name.</returns>
    public PdfName GetDescriptorFontName() { return baseFontName; }

    /// <summary>Gets the shared name for the Type 0 <c>BaseFont</c>.</summary>
    /// <returns>The same PDF name used by the descriptor.</returns>
    public PdfName GetCompositeBaseFontName() { return baseFontName; }

    /// <summary>Gets the shared name for the descendant <c>BaseFont</c>.</summary>
    /// <returns>The same PDF name used by the descriptor.</returns>
    public PdfName GetDescendantBaseFontName() { return baseFontName; }

    /// <summary>Gets the PostScript name written into the embedded program.</summary>
    /// <returns>The same decoded value used by every PDF dictionary.</returns>
    public string GetEmbeddedProgramName() { return baseFontName.GetValue(); }
}
