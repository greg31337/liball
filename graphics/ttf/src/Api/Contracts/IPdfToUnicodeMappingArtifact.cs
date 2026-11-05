namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfToUnicodeMappingArtifact"/>.</summary>
public interface IPdfToUnicodeMappingArtifact
{
    /// <summary>Gets the fixed PDF character-code width.</summary>
    /// <returns>One byte for simple fonts or two bytes for Type 0 fonts.</returns>
    int GetCodeByteCount();

    /// <summary>Gets the number of codes having original Unicode text.</summary>
    /// <returns>The code mapping count.</returns>
    int GetMappingCount();

    /// <summary>Gets one mapping in ascending character-code order.</summary>
    /// <param name="index">The zero-based mapping index.</param>
    /// <returns>The immutable code-to-sequence mapping.</returns>
    PdfToUnicodeCodeMapping GetMapping(int index);

    /// <summary>Finds an original-text mapping by PDF character code.</summary>
    /// <param name="characterCode">The PDF character code.</param>
    /// <returns>The mapping, or null when the code has no original text.</returns>
    PdfToUnicodeCodeMapping? FindMapping(int characterCode);

    /// <summary>Gets the number of codes having multiple sequence candidates.</summary>
    /// <returns>The unresolved ambiguous mapping count.</returns>
    int GetAmbiguousMappingCount();

    /// <summary>Gets the number of sequences observed for multiple codes.</summary>
    /// <returns>The distinct shared-sequence count.</returns>
    int GetSharedSequenceCount();

    /// <summary>Reports whether font-level mappings preserve every observation.</summary>
    /// <returns>True when no occurrence needs an ActualText recommendation.</returns>
    bool IsComplete();

    /// <summary>Gets a CMap containing every unambiguous code mapping.</summary>
    /// <returns>A deterministic CMap that omits ambiguous codes.</returns>
    PdfToUnicodeCMap GetUnambiguousCMap();

    /// <summary>Gets the CMap produced by the stable ambiguity policy.</summary>
    /// <returns>The deterministic resolved font-level ToUnicode CMap.</returns>
    PdfToUnicodeCMap GetCMap();

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The unresolved code-and-sequence observation count.</returns>
    int GetActualTextRecommendationCount();

    /// <summary>Gets one ActualText recommendation in stable code order.</summary>
    /// <param name="index">The zero-based recommendation index.</param>
    /// <returns>The immutable code, sequence, and reason.</returns>
    PdfActualTextRecommendation GetActualTextRecommendation(int index);

    /// <summary>Copies all ActualText recommendations.</summary>
    /// <returns>A new array in stable character-code and sequence order.</returns>
    PdfActualTextRecommendation[] GetActualTextRecommendations();
}
