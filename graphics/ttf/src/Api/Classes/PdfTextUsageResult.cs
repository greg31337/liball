namespace ttf;

/// <summary>Contains one fallback-aware positioned PDF text operation.</summary>
/// <remarks>
/// ActualText recommendations are refreshed when later operations on the same
/// builder introduce mapping ambiguity. They are definitive after the builder
/// finalizes its font resources.
/// </remarks>
public class PdfTextUsageResult : IPdfTextUsageResult
{
    TextShapingResult shapingResult;
    PdfTextUsageRun[] runs;
    PdfTextActualTextRecommendation[] recommendations;

    internal PdfTextUsageResult(
        TextShapingResult shapingResult,
        PdfTextUsageRun[] runs,
        PdfTextActualTextRecommendation[] recommendations)
    {
        this.shapingResult = shapingResult;
        this.runs = CopyRuns(runs);
        this.recommendations = CopyRecommendations(recommendations);
    }

    /// <summary>Gets the shaping result consumed by the operation.</summary>
    /// <returns>The immutable fallback-shaped result.</returns>
    public TextShapingResult GetShapingResult()
    {
        return shapingResult;
    }

    /// <summary>Gets the number of positioned visual runs.</summary>
    /// <returns>The run count.</returns>
    public int GetRunCount()
    {
        return runs.Length;
    }

    /// <summary>Gets one positioned visual run.</summary>
    /// <param name="index">The zero-based run index.</param>
    /// <returns>The run and its selected resource.</returns>
    public PdfTextUsageRun GetRun(int index)
    {
        ValidateIndex(index, runs.Length);
        return runs[index];
    }

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The recommendation count.</returns>
    public int GetActualTextRecommendationCount()
    {
        return recommendations.Length;
    }

    /// <summary>Gets one occurrence-level ActualText recommendation.</summary>
    /// <param name="index">The zero-based recommendation index.</param>
    /// <returns>The source range and recommended text.</returns>
    public PdfTextActualTextRecommendation GetActualTextRecommendation(
        int index)
    {
        ValidateIndex(index, recommendations.Length);
        return recommendations[index];
    }

    internal void SetRecommendations(
        PdfTextActualTextRecommendation[] values)
    {
        recommendations = CopyRecommendations(values);
    }

    static PdfTextUsageRun[] CopyRuns(PdfTextUsageRun[] values)
    {
        PdfTextUsageRun[] copy = new PdfTextUsageRun[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PdfTextActualTextRecommendation[] CopyRecommendations(
        PdfTextActualTextRecommendation[] values)
    {
        PdfTextActualTextRecommendation[] copy =
            new PdfTextActualTextRecommendation[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
