namespace fake;

/// <summary>
/// Reports one selected edge fixture and the rule outcome associated with it.
/// </summary>
public class EdgeSelectionResult
{
    /// <summary>The fresh selected edge-case record.</summary>
    public EdgeCaseRecord Edge;

    /// <summary>The stable name of the selected edge.</summary>
    public string SelectedEdge = string.Empty;

    /// <summary>The category containing the selected edge.</summary>
    public EdgeCaseCategory Category;

    /// <summary>The generation mode used for this selection.</summary>
    public GenerationMode GenerationMode;

    /// <summary>Whether the selected value satisfies its stated input rule.</summary>
    public ValidityMode Validity;

    /// <summary>The deliberately violated rule, or an empty string for a valid-unusual selection.</summary>
    public string ViolatedRule = string.Empty;

    /// <summary>The captured safety mode.</summary>
    public SafetyMode SafetyMode;

    /// <summary>The captured locale fallback mode.</summary>
    public StrictnessMode StrictnessMode;

    /// <summary>The captured Unicode normalization mode.</summary>
    public NormalizationMode NormalizationMode;

    /// <summary>The generalized generation and validation metadata for this selection.</summary>
    public GenerationMetadata Metadata;

    internal EdgeSelectionResult()
    {
        Edge = new EdgeCaseRecord();
        SelectedEdge = string.Empty;
        Category = EdgeCaseCategory.EmptyNullWhitespace;
        GenerationMode = GenerationMode.Typical;
        Validity = ValidityMode.Unknown;
        ViolatedRule = string.Empty;
        SafetyMode = SafetyMode.Reserved;
        StrictnessMode = StrictnessMode.AllowFallback;
        NormalizationMode = NormalizationMode.None;
        Metadata = new GenerationMetadata();
    }
}
