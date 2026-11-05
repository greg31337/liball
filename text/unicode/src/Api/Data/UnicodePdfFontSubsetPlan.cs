namespace unicode;

/// <summary>
/// Contains a grapheme-safe font coverage and subset plan for PDF text.
/// </summary>
public class UnicodePdfFontSubsetPlan
{
    /// <summary>Whether every grapheme cluster is covered.</summary>
    public bool HasFullCoverage;
    /// <summary>All distinct scalars required by the text.</summary>
    public int[] RequiredScalars;
    /// <summary>Scalars belonging only to fully covered clusters.</summary>
    public int[] CoveredScalars;
    /// <summary>Scalars belonging to clusters that require fallback.</summary>
    public int[] MissingScalars;
    /// <summary>Subset requirements grouped by resolved script.</summary>
    public UnicodePdfScriptSubsetPlan[] ScriptPlans;
    /// <summary>Coverage decisions for complete grapheme clusters.</summary>
    public UnicodePdfClusterSubsetPlan[] Clusters;

    /// <summary>
    /// Initializes a PDF font-subset plan.
    /// </summary>
    /// <param name="requiredScalars">All distinct required scalars.</param>
    /// <param name="coveredScalars">Scalars in fully covered clusters.</param>
    /// <param name="missingScalars">Scalars in fallback clusters.</param>
    /// <param name="scriptPlans">Per-script subset plans.</param>
    /// <param name="clusters">Cluster coverage decisions.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an array is <c>null</c> or contains a null item.</exception>
    public UnicodePdfFontSubsetPlan(
        int[] requiredScalars,
        int[] coveredScalars,
        int[] missingScalars,
        UnicodePdfScriptSubsetPlan[] scriptPlans,
        UnicodePdfClusterSubsetPlan[] clusters)
    {
        UnicodePdfFontSubset.ValidateScalars(requiredScalars);
        UnicodePdfFontSubset.ValidateScalars(coveredScalars);
        UnicodePdfFontSubset.ValidateScalars(missingScalars);
        System.ArgumentNullException.ThrowIfNull(scriptPlans);
        System.ArgumentNullException.ThrowIfNull(clusters);

        ScriptPlans = new UnicodePdfScriptSubsetPlan[scriptPlans.Length];
        int index = 0;
        while (index < scriptPlans.Length)
        {
            if (scriptPlans[index] == null)
            {
                throw new System.ArgumentNullException("scriptPlans");
            }

            ScriptPlans[index] = scriptPlans[index];
            index = index + 1;
        }

        Clusters = new UnicodePdfClusterSubsetPlan[clusters.Length];
        index = 0;
        while (index < clusters.Length)
        {
            if (clusters[index] == null)
            {
                throw new System.ArgumentNullException("clusters");
            }

            Clusters[index] = clusters[index];
            index = index + 1;
        }

        RequiredScalars = UnicodePdfFontSubset.CopyScalars(requiredScalars);
        CoveredScalars = UnicodePdfFontSubset.CopyScalars(coveredScalars);
        MissingScalars = UnicodePdfFontSubset.CopyScalars(missingScalars);
        HasFullCoverage = missingScalars.Length == 0;
    }
}
