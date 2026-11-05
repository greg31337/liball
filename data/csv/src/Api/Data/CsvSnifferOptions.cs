namespace csv;

/// <summary>
/// Controls sample limits, candidate syntax characters, and dialect overrides.
/// </summary>
public class CsvSnifferOptions
{
    /// <summary>
    /// Maximum sample characters; zero means no limit.
    /// </summary>
    public int MaxSampleCharacters;
    /// <summary>
    /// Maximum sampled rows; zero means no row limit.
    /// </summary>
    public int MaxSampleRows;
    /// <summary>
    /// Delimiter characters considered during detection.
    /// </summary>
    public char[] CandidateDelimiters;
    /// <summary>
    /// Quote characters considered during detection.
    /// </summary>
    public char[] CandidateQuotes;
    /// <summary>
    /// Comment markers considered during detection.
    /// </summary>
    public char[] CandidateComments;
    /// <summary>
    /// Whether header detection is enabled.
    /// </summary>
    public bool DetectHeader;
    /// <summary>
    /// Whether comment detection is enabled.
    /// </summary>
    public bool DetectComments;
    /// <summary>
    /// Caller-supplied dialect used when overrides are enabled.
    /// </summary>
    public CsvDialect OverrideDialect;
    /// <summary>
    /// Whether the override dialect is active.
    /// </summary>
    public bool HasOverrideDialect;

    /// <summary>
    /// Initializes a new CsvSnifferOptions with library defaults.
    /// </summary>
    public CsvSnifferOptions()
    {
        MaxSampleCharacters = 32768;
        MaxSampleRows = 20;
        CandidateDelimiters = new char[] { ',', '\t', ';', '|' };
        CandidateQuotes = new char[] { '"', '\'' };
        CandidateComments = new char[] { '#' };
        DetectHeader = true;
        DetectComments = true;
        OverrideDialect = CsvDialect.CreateDefault();
        HasOverrideDialect = false;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvSnifferOptions CreateDefault()
    {
        CsvSnifferOptions options = new CsvSnifferOptions();
        return options;
    }
}
