namespace ftp;

/// <summary>
/// Represents one normalized or vendor-specific MLSx fact.
/// </summary>
public class FtpFact
{
    /// <summary>
    /// The normalized case-insensitive fact name.
    /// </summary>
    public string Name;

    /// <summary>
    /// The decoded fact value.
    /// </summary>
    public string Value;

    /// <summary>
    /// The original fact text without its trailing semicolon.
    /// </summary>
    public string RawText;

    /// <summary>
    /// Indicates that the fact is defined by the supported standards.
    /// </summary>
    public bool Standard;

    /// <summary>
    /// Creates an empty, nonstandard fact.
    /// </summary>
    public FtpFact()
    {
        Name = "";
        Value = "";
        RawText = "";
        Standard = false;
    }
}

