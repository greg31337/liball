using System;

namespace email;
/// <summary>
/// Represents a maildir entry.
/// </summary>
public class EmailMaildirEntry : IEmailMaildirEntryContract
{
    int index;
    EmailMaildirFolder folder;
    EmailMaildirName name;
    string path;
    long byteCount;
    DateTimeOffset lastWriteTimeUtc;
    EmailMessage? message;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new maildir entry.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "byteCount">The number of items or bytes to process.</param>
    /// <param name = "lastWriteTimeUtc">The last write time utc used by the operation.</param>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMaildirEntry(int index, EmailMaildirFolder folder, EmailMaildirName name, string path, long byteCount, DateTimeOffset lastWriteTimeUtc, EmailMessage? message, EmailIssueList issues)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (folder < EmailMaildirFolder.New || folder > EmailMaildirFolder.Temporary)
            throw new ArgumentOutOfRangeException("folder");
        if (name == null || path == null || issues == null)
            throw new ArgumentNullException("name");
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException("byteCount");
        this.index = index;
        this.folder = folder;
        this.name = name;
        this.path = path;
        this.byteCount = byteCount;
        this.lastWriteTimeUtc = lastWriteTimeUtc;
        this.message = message;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Gets the folder.
    /// </summary>
    /// <returns>The current folder.</returns>
    public EmailMaildirFolder GetFolder()
    {
        return folder;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public EmailMaildirName GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string GetFileName()
    {
        return name.GetFileName();
    }

    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    public string GetUniqueName()
    {
        return name.GetUniqueName();
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public string GetPath()
    {
        return path;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return byteCount;
    }

    /// <summary>
    /// Gets the last write time utc.
    /// </summary>
    /// <returns>The current last write time utc.</returns>
    public DateTimeOffset GetLastWriteTimeUtc()
    {
        return lastWriteTimeUtc;
    }

    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    public EmailMaildirFlags GetFlags()
    {
        return name.GetFlags();
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return message != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
