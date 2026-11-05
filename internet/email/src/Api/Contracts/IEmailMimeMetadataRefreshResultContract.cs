namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMimeMetadataRefreshResult"/>.
/// </summary>
public interface IEmailMimeMetadataRefreshResultContract
{
    /// <summary>Gets the entity kind before refresh.</summary>
    /// <returns>The previous kind.</returns>
    MimeEntityKind GetPreviousKind();
    /// <summary>Gets the entity kind after refresh.</summary>
    /// <returns>The current kind.</returns>
    MimeEntityKind GetCurrentKind();
    /// <summary>Determines whether the kind changed.</summary>
    /// <returns><c>true</c> when the kind changed.</returns>
    bool DidKindChange();
    /// <summary>Gets the number of changed derived values.</summary>
    /// <returns>The changed value count.</returns>
    int GetChangedValueCount();
    /// <summary>Determines whether any derived value changed.</summary>
    /// <returns><c>true</c> when a value changed.</returns>
    bool HasChanges();
    /// <summary>Determines whether a metadata conflict was found.</summary>
    /// <returns><c>true</c> when a conflict was found.</returns>
    bool HasConflicts();
    /// <summary>Gets the refresh issues.</summary>
    /// <returns>A snapshot of the refresh issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>Determines whether refresh completed without an error.</summary>
    /// <returns><c>true</c> when no error was reported.</returns>
    bool IsSuccess();
}
