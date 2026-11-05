namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfActualTextRecommendation"/>.</summary>
public interface IPdfActualTextRecommendation
{
    /// <summary>Gets the PDF code whose occurrence needs original text.</summary>
    /// <returns>The allocated character code.</returns>
    int GetCharacterCode();

    /// <summary>Gets the source glyph selected by the character code.</summary>
    /// <returns>The source-font glyph identifier.</returns>
    int GetSourceGlyphId();

    /// <summary>Gets the ambiguity that requires original content text.</summary>
    /// <returns>The stable recommendation reason.</returns>
    PdfActualTextReason GetReason();

    /// <summary>Gets the number of original Unicode scalars.</summary>
    /// <returns>One or more scalars.</returns>
    int GetUnicodeScalarCount();

    /// <summary>Gets one original Unicode scalar.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar.</returns>
    int GetUnicodeScalar(int index);

    /// <summary>Copies the original Unicode sequence.</summary>
    /// <returns>A new array containing the recommended text scalars.</returns>
    int[] GetUnicodeSequence();

    /// <summary>Gets the original sequence as a .NET string.</summary>
    /// <returns>The text to use for an occurrence-level ActualText value.</returns>
    string GetText();
}
