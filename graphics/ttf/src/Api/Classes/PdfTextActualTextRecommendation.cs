namespace ttf;

/// <summary>Identifies one glyph occurrence that should carry ActualText.</summary>
public class PdfTextActualTextRecommendation :
    IPdfTextActualTextRecommendation
{
    int resourceIndex;
    int runIndex;
    int glyphIndex;
    int sourceUtf16Start;
    int sourceUtf16Length;
    PdfActualTextRecommendation recommendation;

    internal PdfTextActualTextRecommendation(
        int resourceIndex,
        int runIndex,
        int glyphIndex,
        int sourceUtf16Start,
        int sourceUtf16Length,
        PdfActualTextRecommendation recommendation)
    {
        this.resourceIndex = resourceIndex;
        this.runIndex = runIndex;
        this.glyphIndex = glyphIndex;
        this.sourceUtf16Start = sourceUtf16Start;
        this.sourceUtf16Length = sourceUtf16Length;
        this.recommendation = recommendation;
    }

    /// <summary>Gets the resource containing the ambiguous character code.</summary>
    /// <returns>The zero-based resource index.</returns>
    public int GetResourceIndex() { return resourceIndex; }

    /// <summary>Gets the visual run containing the occurrence.</summary>
    /// <returns>The zero-based operation run index.</returns>
    public int GetRunIndex() { return runIndex; }

    /// <summary>Gets the glyph occurrence within the visual run.</summary>
    /// <returns>The zero-based glyph index.</returns>
    public int GetGlyphIndex() { return glyphIndex; }

    /// <summary>Gets the absolute source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    public int GetSourceUtf16Start() { return sourceUtf16Start; }

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The source code-unit length.</returns>
    public int GetSourceUtf16Length() { return sourceUtf16Length; }

    /// <summary>Gets the resource-level mapping recommendation.</summary>
    /// <returns>The code, Unicode sequence, text, and ambiguity reason.</returns>
    public PdfActualTextRecommendation GetRecommendation()
    {
        return recommendation;
    }
}
