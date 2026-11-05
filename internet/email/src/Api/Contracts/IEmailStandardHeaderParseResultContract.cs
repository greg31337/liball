namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailStandardHeaderParseResult"/>.
/// </summary>
public interface IEmailStandardHeaderParseResultContract
{
    /// <summary>Gets the parsed value.</summary>
    /// <returns>The parsed value, or null.</returns>
    EmailStandardHeaderValue? GetValue();
    /// <summary>Gets the issues.</summary>
    /// <returns>A snapshot of the issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>Gets the consumed byte count.</summary>
    /// <returns>The consumed byte count.</returns>
    int GetConsumedByteCount();
    /// <summary>Gets the source byte count.</summary>
    /// <returns>The source byte count.</returns>
    int GetSourceByteCount();
    /// <summary>Determines whether parsing succeeded.</summary>
    /// <returns><c>true</c> when parsing succeeded.</returns>
    bool IsSuccess();
    /// <summary>Determines whether a fatal error occurred.</summary>
    /// <returns><c>true</c> when a fatal error occurred.</returns>
    bool HasFatalError();
}
