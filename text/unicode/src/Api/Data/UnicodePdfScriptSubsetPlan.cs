namespace unicode;

/// <summary>
/// Groups required, covered, and fallback scalars for one resolved script.
/// </summary>
public class UnicodePdfScriptSubsetPlan
{
    /// <summary>The resolved script.</summary>
    public UnicodeScript Script;
    /// <summary>All distinct scalars required for the script.</summary>
    public int[] RequiredScalars;
    /// <summary>Scalars belonging only to fully covered clusters.</summary>
    public int[] CoveredScalars;
    /// <summary>Scalars belonging to clusters that require fallback.</summary>
    public int[] MissingScalars;
    /// <summary>Whether every cluster assigned to the script is covered.</summary>
    public bool HasFullCoverage;

    /// <summary>
    /// Initializes a per-script subset plan.
    /// </summary>
    /// <param name="script">The resolved script.</param>
    /// <param name="requiredScalars">All required scalars.</param>
    /// <param name="coveredScalars">Scalars in fully covered clusters.</param>
    /// <param name="missingScalars">Scalars in fallback clusters.</param>
    public UnicodePdfScriptSubsetPlan(
        UnicodeScript script,
        int[] requiredScalars,
        int[] coveredScalars,
        int[] missingScalars)
    {
        UnicodePdfFontSubset.ValidateScalars(requiredScalars);
        UnicodePdfFontSubset.ValidateScalars(coveredScalars);
        UnicodePdfFontSubset.ValidateScalars(missingScalars);
        Script = script;
        RequiredScalars = UnicodePdfFontSubset.CopyScalars(requiredScalars);
        CoveredScalars = UnicodePdfFontSubset.CopyScalars(coveredScalars);
        MissingScalars = UnicodePdfFontSubset.CopyScalars(missingScalars);
        HasFullCoverage = missingScalars.Length == 0;
    }
}
