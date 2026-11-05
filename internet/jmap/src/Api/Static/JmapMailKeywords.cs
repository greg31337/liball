namespace jmap;

/// <summary>
/// Defines standard mail keywords exposed by JMAP Mail.
/// </summary>
public static class JmapMailKeywords
{
    /// <summary>The draft keyword.</summary>
    public const string Draft = "$draft";

    /// <summary>The seen keyword.</summary>
    public const string Seen = "$seen";

    /// <summary>The flagged keyword.</summary>
    public const string Flagged = "$flagged";

    /// <summary>The answered keyword.</summary>
    public const string Answered = "$answered";

    /// <summary>The forwarded keyword.</summary>
    public const string Forwarded = "$forwarded";

    /// <summary>The phishing keyword.</summary>
    public const string Phishing = "$phishing";

    /// <summary>The junk keyword.</summary>
    public const string Junk = "$junk";

    /// <summary>The not-junk keyword.</summary>
    public const string NotJunk = "$notjunk";
}
