namespace icc;

/// <summary>Represents a human-oriented summary and inspection of a profile.</summary>
public class IccProfileSummary
{
    /// <summary>Gets or sets the summary headline.</summary>
    public string Headline;
    /// <summary>Gets or sets the profile version description.</summary>
    public string Version;
    /// <summary>Gets or sets the profile class description.</summary>
    public string ProfileClass;
    /// <summary>Gets or sets the data colour-space description.</summary>
    public string DataColorSpace;
    /// <summary>Gets or sets the PCS description.</summary>
    public string ProfileConnectionSpace;
    /// <summary>Gets or sets the tag count.</summary>
    public int TagCount;
    /// <summary>Gets or sets the applied inspection options.</summary>
    public IccInspectionOptions Options;
    /// <summary>Gets or sets the structured inspection root.</summary>
    public IccInspectionNode Root;
    /// <summary>Gets or sets validation findings.</summary>
    public IccValidationReport Validation;
    /// <summary>Gets or sets optional profile-construction inputs associated with the profile.</summary>
    public IccProfileBuildOptions? BuildOptions;
    /// <summary>Gets or sets an optional gamut query result.</summary>
    public IccGamutResult? Gamut;
    /// <summary>Gets or sets an optional comparison report.</summary>
    public IccComparisonReport? Comparison;
    /// <summary>Gets or sets an optional embedding operation result.</summary>
    public IccEmbeddedProfileResult? Embedding;

    /// <summary>Initializes an empty profile summary.</summary>
    public IccProfileSummary()
    {
        Headline = string.Empty;
        Version = string.Empty;
        ProfileClass = string.Empty;
        DataColorSpace = string.Empty;
        ProfileConnectionSpace = string.Empty;
        TagCount = 0;
        Options = new IccInspectionOptions();
        Root = new IccInspectionNode();
        Validation = new IccValidationReport();
        BuildOptions = null;
        Gamut = null;
        Comparison = null;
        Embedding = null;
    }
}
