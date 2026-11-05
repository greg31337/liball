namespace unicode;

/// <summary>
/// Exposes the Unicode data version and provenance compiled into the library.
/// </summary>
public static class UnicodeVersion
{
    /// <summary>
    /// The major component of the compiled Unicode release number.
    /// </summary>
    public const int Major = 17;
    /// <summary>
    /// The minor component of the compiled Unicode release number.
    /// </summary>
    public const int Minor = 0;
    /// <summary>
    /// The patch component of the compiled Unicode release number.
    /// </summary>
    public const int Patch = 0;
    /// <summary>
    /// The complete dotted Unicode release number compiled into the tables.
    /// </summary>
    public const string Version = "17.0.0";
    /// <summary>
    /// The date on which this data baseline was pinned.
    /// </summary>
    public const string BaselineDate = "2026-08-03";
    /// <summary>
    /// The Unicode release overview URL.
    /// </summary>
    public const string VersionUrl = "https://www.unicode.org/versions/Unicode17.0.0/";
    /// <summary>
    /// The base URL for the matching Unicode Character Database.
    /// </summary>
    public const string UcdUrl = "https://www.unicode.org/Public/17.0.0/ucd/";
    /// <summary>
    /// The base URL for the matching Unicode emoji data.
    /// </summary>
    public const string EmojiUrl = "https://www.unicode.org/Public/17.0.0/ucd/emoji/";
    /// <summary>
    /// The repository document that records the data provenance.
    /// </summary>
    public const string ProvenanceDocument = "todo.md";

}
