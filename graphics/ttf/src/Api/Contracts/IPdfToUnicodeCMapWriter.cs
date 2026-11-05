namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfToUnicodeCMapWriter"/>.</summary>
public interface IPdfToUnicodeCMapWriter
{
    /// <summary>Builds a complete ToUnicode CMap from explicit mappings.</summary>
    /// <param name="codeByteCount">One for simple codes or two for Type 0 codes.</param>
    /// <param name="mappings">Unique code-to-Unicode mappings in any order.</param>
    /// <returns>The immutable CMap stream artifact.</returns>
    PdfToUnicodeCMap BuildCMap(int codeByteCount, PdfToUnicodeMapping[] mappings);
}
