namespace ttf;

/// <summary>Contains allocated PDF codes and their original Unicode sequences.</summary>
/// <remarks>
/// The resolved CMap selects the lexicographically first sequence for each
/// code. When multiple codes prefer the same sequence, the lowest code owns
/// the CMap entry. Every displaced code-and-sequence observation is returned
/// as an occurrence-level ActualText recommendation. The separate unambiguous
/// CMap remains available for callers that want only single-candidate codes.
/// </remarks>
public class PdfToUnicodeMappingArtifact : IPdfToUnicodeMappingArtifact
{
    int codeByteCount;
    PdfToUnicodeCodeMapping[] mappings;
    int ambiguousMappingCount;
    int sharedSequenceCount;
    PdfToUnicodeCMap unambiguousCMap;
    PdfToUnicodeCMap cmap;
    PdfActualTextRecommendation[] actualTextRecommendations;

    internal PdfToUnicodeMappingArtifact(
        int codeByteCount,
        PdfToUnicodeCodeMapping[] mappings,
        int ambiguousMappingCount,
        int sharedSequenceCount,
        PdfToUnicodeCMap unambiguousCMap,
        PdfToUnicodeCMap cmap,
        PdfActualTextRecommendation[] actualTextRecommendations)
    {
        this.codeByteCount = codeByteCount;
        this.mappings = CopyMappings(mappings);
        this.ambiguousMappingCount = ambiguousMappingCount;
        this.sharedSequenceCount = sharedSequenceCount;
        this.unambiguousCMap = unambiguousCMap;
        this.cmap = cmap;
        this.actualTextRecommendations =
            CopyRecommendations(actualTextRecommendations);
    }

    /// <summary>Gets the fixed PDF character-code width.</summary>
    /// <returns>One byte for simple fonts or two bytes for Type 0 fonts.</returns>
    public int GetCodeByteCount() { return codeByteCount; }

    /// <summary>Gets the number of codes having original Unicode text.</summary>
    /// <returns>The code mapping count.</returns>
    public int GetMappingCount() { return mappings.Length; }

    /// <summary>Gets one mapping in ascending character-code order.</summary>
    /// <param name="index">The zero-based mapping index.</param>
    /// <returns>The immutable code-to-sequence mapping.</returns>
    public PdfToUnicodeCodeMapping GetMapping(int index)
    {
        if (index < 0 || index >= mappings.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return mappings[index];
    }

    /// <summary>Finds an original-text mapping by PDF character code.</summary>
    /// <param name="characterCode">The PDF character code.</param>
    /// <returns>The mapping, or null when the code has no original text.</returns>
    public PdfToUnicodeCodeMapping? FindMapping(int characterCode)
    {
        int low = 0;
        int high = mappings.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int foundCode = mappings[middle].GetCharacterCode();

            if (foundCode == characterCode)
            {
                return mappings[middle];
            }

            if (foundCode < characterCode)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    /// <summary>Gets the number of codes having multiple sequence candidates.</summary>
    /// <returns>The unresolved ambiguous mapping count.</returns>
    public int GetAmbiguousMappingCount() { return ambiguousMappingCount; }

    /// <summary>Gets the number of sequences observed for multiple codes.</summary>
    /// <returns>The distinct shared-sequence count.</returns>
    public int GetSharedSequenceCount() { return sharedSequenceCount; }

    /// <summary>Reports whether font-level mappings preserve every observation.</summary>
    /// <returns>True when no occurrence needs an ActualText recommendation.</returns>
    public bool IsComplete()
    {
        return actualTextRecommendations.Length == 0;
    }

    /// <summary>Gets a CMap containing every unambiguous code mapping.</summary>
    /// <returns>A deterministic CMap that omits ambiguous codes.</returns>
    public PdfToUnicodeCMap GetUnambiguousCMap()
    {
        return unambiguousCMap;
    }

    /// <summary>Gets the CMap produced by the stable ambiguity policy.</summary>
    /// <returns>The deterministic resolved font-level ToUnicode CMap.</returns>
    public PdfToUnicodeCMap GetCMap() { return cmap; }

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The unresolved code-and-sequence observation count.</returns>
    public int GetActualTextRecommendationCount()
    {
        return actualTextRecommendations.Length;
    }

    /// <summary>Gets one ActualText recommendation in stable code order.</summary>
    /// <param name="index">The zero-based recommendation index.</param>
    /// <returns>The immutable code, sequence, and reason.</returns>
    public PdfActualTextRecommendation GetActualTextRecommendation(
        int index)
    {
        if (index < 0 || index >= actualTextRecommendations.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return actualTextRecommendations[index];
    }

    /// <summary>Copies all ActualText recommendations.</summary>
    /// <returns>A new array in stable character-code and sequence order.</returns>
    public PdfActualTextRecommendation[] GetActualTextRecommendations()
    {
        return CopyRecommendations(actualTextRecommendations);
    }

    static PdfToUnicodeCodeMapping[] CopyMappings(
        PdfToUnicodeCodeMapping[] values)
    {
        PdfToUnicodeCodeMapping[] copy =
            new PdfToUnicodeCodeMapping[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PdfActualTextRecommendation[] CopyRecommendations(
        PdfActualTextRecommendation[] values)
    {
        PdfActualTextRecommendation[] copy =
            new PdfActualTextRecommendation[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
