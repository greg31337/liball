namespace unicode;

/// <summary>
/// Describes the Unicode release and source locations used to generate the runtime tables.
/// </summary>
public class UnicodeDataProvenance
{
    /// <summary>
    /// The Unicode release number used by the generated tables.
    /// </summary>
    public string UnicodeVersion;
    /// <summary>
    /// The date on which this data baseline was pinned.
    /// </summary>
    public string BaselineDate;
    /// <summary>
    /// The Unicode release overview URL.
    /// </summary>
    public string VersionUrl;
    /// <summary>
    /// The base URL for the matching Unicode Character Database.
    /// </summary>
    public string UcdUrl;
    /// <summary>
    /// The base URL for the matching Unicode emoji data.
    /// </summary>
    public string EmojiUrl;
    /// <summary>
    /// The repository document that records the data provenance.
    /// </summary>
    public string ProvenanceDocument;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="unicodeVersion">The Unicode release number.</param>
    /// <param name="baselineDate">The date on which the data was pinned.</param>
    /// <param name="versionUrl">The Unicode release overview URL.</param>
    /// <param name="ucdUrl">The Unicode Character Database base URL.</param>
    /// <param name="emojiUrl">The Unicode emoji data base URL.</param>
    /// <param name="provenanceDocument">The repository provenance document.</param>
    public UnicodeDataProvenance(
        string unicodeVersion,
        string baselineDate,
        string versionUrl,
        string ucdUrl,
        string emojiUrl,
        string provenanceDocument)
    {
        UnicodeVersion = unicodeVersion;
        BaselineDate = baselineDate;
        VersionUrl = versionUrl;
        UcdUrl = ucdUrl;
        EmojiUrl = emojiUrl;
        ProvenanceDocument = provenanceDocument;
    }
}
