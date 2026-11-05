namespace ttf;

/// <summary>Defines the output of one fallback-aware PDF text operation.</summary>
public interface IPdfTextUsageResult
{
    /// <summary>Gets the shaping result consumed by the operation.</summary>
    /// <returns>The immutable fallback-shaped result.</returns>
    TextShapingResult GetShapingResult();

    /// <summary>Gets the number of positioned visual runs.</summary>
    /// <returns>The run count.</returns>
    int GetRunCount();

    /// <summary>Gets one positioned visual run.</summary>
    /// <param name="index">The zero-based run index.</param>
    /// <returns>The run and its selected resource.</returns>
    PdfTextUsageRun GetRun(int index);

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The recommendation count.</returns>
    int GetActualTextRecommendationCount();

    /// <summary>Gets one occurrence-level ActualText recommendation.</summary>
    /// <param name="index">The zero-based recommendation index.</param>
    /// <returns>The source range and recommended text.</returns>
    PdfTextActualTextRecommendation GetActualTextRecommendation(int index);
}
