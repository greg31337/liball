namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfTjAdjustment"/>.</summary>
public interface IPdfTjAdjustment
{
    /// <summary>Gets the visual glyph after which to place the number.</summary>
    /// <returns>The zero-based visual glyph index.</returns>
    int GetAfterGlyphIndex();

    /// <summary>Gets the suggested PDF <c>TJ</c> number.</summary>
    /// <remarks>
    /// The value is the nominal font advance minus the shaped advance in PDF
    /// 1000-unit text space. A positive horizontal value reduces the advance.
    /// </remarks>
    /// <returns>A finite nonzero adjustment.</returns>
    double GetValue();
}
