namespace ttf;

/// <summary>Contains a one-byte simple TrueType encoding and its differences.</summary>
/// <remarks>
/// Every allocated character code is included in the differences so arbitrary
/// document-local allocation does not inherit a conflicting base-encoding
/// entry. This artifact is produced only for explicitly requested legacy
/// simple-font output.
/// </remarks>
public class PdfSimpleFontEncodingArtifact : IPdfSimpleFontEncodingArtifact
{
    string baseEncodingName;
    PdfSimpleFontEncodingDifference[] differences;
    PdfToUnicodeMappingArtifact toUnicodeMappings;

    internal PdfSimpleFontEncodingArtifact(
        string baseEncodingName,
        PdfSimpleFontEncodingDifference[] differences,
        PdfToUnicodeMappingArtifact toUnicodeMappings)
    {
        this.baseEncodingName = baseEncodingName;
        this.differences = CopyDifferences(differences);
        this.toUnicodeMappings = toUnicodeMappings;
    }

    /// <summary>Gets the PDF base-encoding name.</summary>
    /// <returns><c>WinAnsiEncoding</c>.</returns>
    public string GetBaseEncodingName()
    {
        return baseEncodingName;
    }

    /// <summary>Gets the fixed character-code width.</summary>
    /// <returns>One byte.</returns>
    public int GetCodeByteCount()
    {
        return 1;
    }

    /// <summary>Gets the number of explicitly mapped character codes.</summary>
    /// <returns>The difference count, including code zero.</returns>
    public int GetDifferenceCount()
    {
        return differences.Length;
    }

    /// <summary>Gets one character-code difference.</summary>
    /// <param name="index">The zero-based difference index.</param>
    /// <returns>The immutable code, glyph, and name mapping.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    public PdfSimpleFontEncodingDifference GetDifference(int index)
    {
        ValidateIndex(index);
        return differences[index];
    }

    /// <summary>Copies all character-code differences in code order.</summary>
    /// <returns>A new array containing the immutable mappings.</returns>
    public PdfSimpleFontEncodingDifference[] GetDifferences()
    {
        return CopyDifferences(differences);
    }

    /// <summary>Gets codes mapped to their original Unicode sequences.</summary>
    /// <returns>The immutable one-byte ToUnicode mapping artifact.</returns>
    public PdfToUnicodeMappingArtifact GetToUnicodeMappings()
    {
        return toUnicodeMappings;
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= differences.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    static PdfSimpleFontEncodingDifference[] CopyDifferences(
        PdfSimpleFontEncodingDifference[] values)
    {
        PdfSimpleFontEncodingDifference[] copy =
            new PdfSimpleFontEncodingDifference[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
