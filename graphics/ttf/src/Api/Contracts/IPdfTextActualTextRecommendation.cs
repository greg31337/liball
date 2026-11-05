namespace ttf;

/// <summary>Defines an occurrence-level PDF ActualText recommendation.</summary>
public interface IPdfTextActualTextRecommendation
{
    /// <summary>Gets the resource containing the ambiguous character code.</summary>
    /// <returns>The zero-based resource index.</returns>
    int GetResourceIndex();

    /// <summary>Gets the visual run containing the occurrence.</summary>
    /// <returns>The zero-based operation run index.</returns>
    int GetRunIndex();

    /// <summary>Gets the glyph occurrence within the visual run.</summary>
    /// <returns>The zero-based glyph index.</returns>
    int GetGlyphIndex();

    /// <summary>Gets the absolute source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    int GetSourceUtf16Start();

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The source code-unit length.</returns>
    int GetSourceUtf16Length();

    /// <summary>Gets the resource-level mapping recommendation.</summary>
    /// <returns>The code, Unicode sequence, text, and ambiguity reason.</returns>
    PdfActualTextRecommendation GetRecommendation();
}
