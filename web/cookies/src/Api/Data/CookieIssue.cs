namespace cookies;

/// <summary>
/// Describes a stable diagnostic produced while processing cookie data.
/// </summary>
public class CookieIssue
{
    /// <summary>
    /// The stable reason for the issue.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The severity of the issue.
    /// </summary>
    public CookieParseSeverity Severity;

    /// <summary>
    /// Indicates whether source offset and length information is available.
    /// </summary>
    public bool HasOffset;

    /// <summary>
    /// The zero-based source offset when <see cref="HasOffset"/> is true.
    /// </summary>
    public int Offset;

    /// <summary>
    /// The source length when <see cref="HasOffset"/> is true.
    /// </summary>
    public int Length;

    /// <summary>
    /// A short, safely displayable fragment associated with the issue.
    /// </summary>
    public string SafeFragment = "";
}
