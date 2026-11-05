using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirOperationResult"/>.
/// </summary>
public interface IEmailMaildirOperationResultContract
{
    /// <summary>Gets the source folder.</summary>
    /// <returns>The source folder.</returns>
    EmailMaildirFolder GetSourceFolder();
    /// <summary>Gets the destination folder.</summary>
    /// <returns>The destination folder.</returns>
    EmailMaildirFolder GetDestinationFolder();
    /// <summary>Gets the source filename.</summary>
    /// <returns>The source filename.</returns>
    string GetSourceFileName();
    /// <summary>Gets the destination filename.</summary>
    /// <returns>The destination filename.</returns>
    string GetDestinationFileName();
    /// <summary>Gets the source path.</summary>
    /// <returns>The source path.</returns>
    string GetSourcePath();
    /// <summary>Gets the destination path.</summary>
    /// <returns>The destination path.</returns>
    string GetDestinationPath();
    /// <summary>Gets the previous standard flags.</summary>
    /// <returns>The previous standard flags.</returns>
    EmailMaildirFlags GetPreviousFlags();
    /// <summary>Gets the resulting standard flags.</summary>
    /// <returns>The resulting standard flags.</returns>
    EmailMaildirFlags GetFlags();
    /// <summary>Gets the preserved unknown flag text.</summary>
    /// <returns>The preserved unknown flag text.</returns>
    string GetPreservedUnknownFlagText();
    /// <summary>Gets the collision count.</summary>
    /// <returns>The collision count.</returns>
    int GetCollisionCount();
    /// <summary>Determines whether an existing destination was overwritten.</summary>
    /// <returns><c>true</c> when an existing destination was overwritten; otherwise, <c>false</c>.</returns>
    bool WasOverwritten();
    /// <summary>Determines whether a filesystem move occurred.</summary>
    /// <returns><c>true</c> when a move occurred; otherwise, <c>false</c>.</returns>
    bool WasMoved();
    /// <summary>Determines whether the operation completed, including an already-current no-op.</summary>
    /// <returns><c>true</c> when the requested state was reached; otherwise, <c>false</c>.</returns>
    bool WasCompleted();
    /// <summary>Gets the operation issues.</summary>
    /// <returns>The ordered diagnostics.</returns>
    EmailIssueList GetIssues();
    /// <summary>Determines whether the requested state was reached without errors.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
