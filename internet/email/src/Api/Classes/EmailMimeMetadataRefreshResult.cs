using System;

namespace email;
/// <summary>
/// Reports changes and conflicts found while refreshing MIME metadata from content headers.
/// </summary>
public class EmailMimeMetadataRefreshResult : IEmailMimeMetadataRefreshResultContract
{
    MimeEntityKind previousKind;
    MimeEntityKind currentKind;
    int changedValueCount;
    bool conflicts;
    EmailIssueList issues;

    /// <summary>Creates a MIME metadata refresh result.</summary>
    /// <param name = "previousKind">The kind before refresh.</param>
    /// <param name = "currentKind">The kind after refresh.</param>
    /// <param name = "changedValueCount">The number of changed derived values.</param>
    /// <param name = "conflicts">Whether a conflict was found.</param>
    /// <param name = "issues">The refresh issues.</param>
    /// <exception cref = "ArgumentNullException">Thrown when issues is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a count or enum is invalid.</exception>
    public EmailMimeMetadataRefreshResult(MimeEntityKind previousKind, MimeEntityKind currentKind, int changedValueCount, bool conflicts, EmailIssueList issues)
    {
        if (previousKind < MimeEntityKind.Leaf || previousKind > MimeEntityKind.Encrypted || currentKind < MimeEntityKind.Leaf || currentKind > MimeEntityKind.Encrypted || changedValueCount < 0)
        {
            throw new ArgumentOutOfRangeException("previousKind");
        }

        if (issues == null) { throw new ArgumentNullException("issues"); }
        this.previousKind = previousKind;
        this.currentKind = currentKind;
        this.changedValueCount = changedValueCount;
        this.conflicts = conflicts;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>Gets the entity kind before refresh.</summary>
    /// <returns>The previous kind.</returns>
    public MimeEntityKind GetPreviousKind() { return previousKind; }
    /// <summary>Gets the entity kind after refresh.</summary>
    /// <returns>The current kind.</returns>
    public MimeEntityKind GetCurrentKind() { return currentKind; }
    /// <summary>Determines whether the kind changed.</summary>
    /// <returns><c>true</c> when the kind changed.</returns>
    public bool DidKindChange() { return previousKind != currentKind; }
    /// <summary>Gets the number of changed derived values.</summary>
    /// <returns>The changed value count.</returns>
    public int GetChangedValueCount() { return changedValueCount; }
    /// <summary>Determines whether any derived value changed.</summary>
    /// <returns><c>true</c> when a value changed.</returns>
    public bool HasChanges() { return changedValueCount > 0; }
    /// <summary>Determines whether a metadata conflict was found.</summary>
    /// <returns><c>true</c> when a conflict was found.</returns>
    public bool HasConflicts() { return conflicts; }
    /// <summary>Gets the refresh issues.</summary>
    /// <returns>A snapshot of the refresh issues.</returns>
    public EmailIssueList GetIssues() { EmailIssueList copy = new EmailIssueList(); copy.AddRange(issues); return copy; }
    /// <summary>Determines whether refresh completed without an error.</summary>
    /// <returns><c>true</c> when no error was reported.</returns>
    public bool IsSuccess() { return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error); }
}
