namespace ttf;

/// <summary>Recommends original text for one ambiguous PDF code occurrence.</summary>
public class PdfActualTextRecommendation : IPdfActualTextRecommendation
{
    int characterCode;
    int sourceGlyphId;
    int[] unicodeSequence;
    string text;
    PdfActualTextReason reason;

    internal PdfActualTextRecommendation(
        int characterCode,
        int sourceGlyphId,
        int[] unicodeSequence,
        PdfActualTextReason reason)
    {
        this.characterCode = characterCode;
        this.sourceGlyphId = sourceGlyphId;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
        text = CreateText(unicodeSequence);
        this.reason = reason;
    }

    /// <summary>Gets the PDF code whose occurrence needs original text.</summary>
    /// <returns>The allocated character code.</returns>
    public int GetCharacterCode() { return characterCode; }

    /// <summary>Gets the source glyph selected by the character code.</summary>
    /// <returns>The source-font glyph identifier.</returns>
    public int GetSourceGlyphId() { return sourceGlyphId; }

    /// <summary>Gets the ambiguity that requires original content text.</summary>
    /// <returns>The stable recommendation reason.</returns>
    public PdfActualTextReason GetReason() { return reason; }

    /// <summary>Gets the number of original Unicode scalars.</summary>
    /// <returns>One or more scalars.</returns>
    public int GetUnicodeScalarCount() { return unicodeSequence.Length; }

    /// <summary>Gets one original Unicode scalar.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar.</returns>
    public int GetUnicodeScalar(int index)
    {
        if (index < 0 || index >= unicodeSequence.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return unicodeSequence[index];
    }

    /// <summary>Copies the original Unicode sequence.</summary>
    /// <returns>A new array containing the recommended text scalars.</returns>
    public int[] GetUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    /// <summary>Gets the original sequence as a .NET string.</summary>
    /// <returns>The text to use for an occurrence-level ActualText value.</returns>
    public string GetText() { return text; }

    static string CreateText(int[] unicodeSequence)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        int index = 0;

        while (index < unicodeSequence.Length)
        {
            builder.Append(System.Char.ConvertFromUtf32(
                unicodeSequence[index]));
            index += 1;
        }

        return builder.ToString();
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
