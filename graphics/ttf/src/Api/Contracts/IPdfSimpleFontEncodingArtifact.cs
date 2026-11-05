namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfSimpleFontEncodingArtifact"/>.</summary>
public interface IPdfSimpleFontEncodingArtifact
{
    /// <summary>Gets the PDF base-encoding name.</summary>
    /// <returns><c>WinAnsiEncoding</c>.</returns>
    string GetBaseEncodingName();

    /// <summary>Gets the fixed character-code width.</summary>
    /// <returns>One byte.</returns>
    int GetCodeByteCount();

    /// <summary>Gets the number of explicitly mapped character codes.</summary>
    /// <returns>The difference count, including code zero.</returns>
    int GetDifferenceCount();

    /// <summary>Gets one character-code difference.</summary>
    /// <param name="index">The zero-based difference index.</param>
    /// <returns>The immutable code, glyph, and name mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    PdfSimpleFontEncodingDifference GetDifference(int index);

    /// <summary>Copies all character-code differences in code order.</summary>
    /// <returns>A new array containing the immutable mappings.</returns>
    PdfSimpleFontEncodingDifference[] GetDifferences();

    /// <summary>Gets codes mapped to their original Unicode sequences.</summary>
    /// <returns>The immutable one-byte ToUnicode mapping artifact.</returns>
    PdfToUnicodeMappingArtifact GetToUnicodeMappings();
}
