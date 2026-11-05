using System;

namespace email;
/// <summary>
/// Describes the outcome and before/after state of an atomic Maildir move or flag update.
/// </summary>
public class EmailMaildirOperationResult : IEmailMaildirOperationResultContract
{
    EmailMaildirFolder sourceFolder;
    EmailMaildirFolder destinationFolder;
    string sourceFileName;
    string destinationFileName;
    string sourcePath;
    string destinationPath;
    EmailMaildirFlags previousFlags;
    EmailMaildirFlags flags;
    string preservedUnknownFlagText;
    int collisionCount;
    bool overwritten;
    bool moved;
    bool completed;
    EmailIssueList issues;
    /// <summary>
    /// Creates a Maildir operation result.
    /// </summary>
    /// <param name = "sourceFolder">The source folder.</param>
    /// <param name = "destinationFolder">The destination folder.</param>
    /// <param name = "sourceFileName">The source filename.</param>
    /// <param name = "destinationFileName">The destination filename.</param>
    /// <param name = "sourcePath">The source path.</param>
    /// <param name = "destinationPath">The destination path.</param>
    /// <param name = "previousFlags">The previous standard flags.</param>
    /// <param name = "flags">The resulting standard flags.</param>
    /// <param name = "preservedUnknownFlagText">The preserved unknown flag text.</param>
    /// <param name = "collisionCount">The number of collisions encountered.</param>
    /// <param name = "overwritten">Whether an existing destination was overwritten.</param>
    /// <param name = "moved">Whether a filesystem move occurred.</param>
    /// <param name = "completed">Whether the requested state was reached.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMaildirOperationResult(EmailMaildirFolder sourceFolder, EmailMaildirFolder destinationFolder, string sourceFileName, string destinationFileName, string sourcePath, string destinationPath, EmailMaildirFlags previousFlags, EmailMaildirFlags flags, string preservedUnknownFlagText, int collisionCount, bool overwritten, bool moved, bool completed, EmailIssueList issues)
    {
        ValidateFolder(sourceFolder, "sourceFolder");
        ValidateFolder(destinationFolder, "destinationFolder");
        ValidateFlags(previousFlags, "previousFlags");
        ValidateFlags(flags, "flags");
        if (sourceFileName == null || destinationFileName == null || sourcePath == null || destinationPath == null || preservedUnknownFlagText == null || issues == null)
            throw new ArgumentNullException("sourceFileName");
        if (collisionCount < 0)
            throw new ArgumentOutOfRangeException("collisionCount");
        this.sourceFolder = sourceFolder;
        this.destinationFolder = destinationFolder;
        this.sourceFileName = sourceFileName;
        this.destinationFileName = destinationFileName;
        this.sourcePath = sourcePath;
        this.destinationPath = destinationPath;
        this.previousFlags = previousFlags;
        this.flags = flags;
        this.preservedUnknownFlagText = preservedUnknownFlagText;
        this.collisionCount = collisionCount;
        this.overwritten = overwritten;
        this.moved = moved;
        this.completed = completed;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>Gets the source folder.</summary>
    /// <returns>The source folder.</returns>
    public EmailMaildirFolder GetSourceFolder() { return sourceFolder; }
    /// <summary>Gets the destination folder.</summary>
    /// <returns>The destination folder.</returns>
    public EmailMaildirFolder GetDestinationFolder() { return destinationFolder; }
    /// <summary>Gets the source filename.</summary>
    /// <returns>The source filename.</returns>
    public string GetSourceFileName() { return sourceFileName; }
    /// <summary>Gets the destination filename.</summary>
    /// <returns>The destination filename.</returns>
    public string GetDestinationFileName() { return destinationFileName; }
    /// <summary>Gets the source path.</summary>
    /// <returns>The source path.</returns>
    public string GetSourcePath() { return sourcePath; }
    /// <summary>Gets the destination path.</summary>
    /// <returns>The destination path.</returns>
    public string GetDestinationPath() { return destinationPath; }
    /// <summary>Gets the previous standard flags.</summary>
    /// <returns>The previous standard flags.</returns>
    public EmailMaildirFlags GetPreviousFlags() { return previousFlags; }
    /// <summary>Gets the resulting standard flags.</summary>
    /// <returns>The resulting standard flags.</returns>
    public EmailMaildirFlags GetFlags() { return flags; }
    /// <summary>Gets the preserved unknown flag text.</summary>
    /// <returns>The preserved unknown flag text.</returns>
    public string GetPreservedUnknownFlagText() { return preservedUnknownFlagText; }
    /// <summary>Gets the collision count.</summary>
    /// <returns>The collision count.</returns>
    public int GetCollisionCount() { return collisionCount; }
    /// <summary>Determines whether an existing destination was overwritten.</summary>
    /// <returns><c>true</c> when an existing destination was overwritten; otherwise, <c>false</c>.</returns>
    public bool WasOverwritten() { return overwritten; }
    /// <summary>Determines whether a filesystem move occurred.</summary>
    /// <returns><c>true</c> when a move occurred; otherwise, <c>false</c>.</returns>
    public bool WasMoved() { return moved; }
    /// <summary>Determines whether the operation completed, including an already-current no-op.</summary>
    /// <returns><c>true</c> when the requested state was reached; otherwise, <c>false</c>.</returns>
    public bool WasCompleted() { return completed; }
    /// <summary>Gets the operation issues.</summary>
    /// <returns>The ordered diagnostics.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }
    /// <summary>Determines whether the requested state was reached without errors.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess() { return completed && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error); }

    static void ValidateFolder(EmailMaildirFolder value, string parameterName)
    {
        if (value < EmailMaildirFolder.New || value > EmailMaildirFolder.Temporary)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    static void ValidateFlags(EmailMaildirFlags value, string parameterName)
    {
        int numeric = (int)value;
        if (numeric < 0 || (numeric & ~63) != 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
