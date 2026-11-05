namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontResourceNames"/>.</summary>
public interface IPdfFontResourceNames
{
    /// <summary>Gets the decoded deterministic base font name.</summary>
    /// <returns>The subset-prefixed or full-font PostScript name.</returns>
    string GetBaseFontName();

    /// <summary>Gets the shared name for FontDescriptor <c>FontName</c>.</summary>
    /// <returns>The validated and escaped PDF name.</returns>
    PdfName GetDescriptorFontName();

    /// <summary>Gets the shared name for the Type 0 <c>BaseFont</c>.</summary>
    /// <returns>The same PDF name used by the descriptor.</returns>
    PdfName GetCompositeBaseFontName();

    /// <summary>Gets the shared name for the descendant <c>BaseFont</c>.</summary>
    /// <returns>The same PDF name used by the descriptor.</returns>
    PdfName GetDescendantBaseFontName();

    /// <summary>Gets the PostScript name written into the embedded program.</summary>
    /// <returns>The same decoded value used by every PDF dictionary.</returns>
    string GetEmbeddedProgramName();
}
