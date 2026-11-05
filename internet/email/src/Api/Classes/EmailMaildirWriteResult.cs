using System;

namespace email;
/// <summary>
/// Describes the outcome of a maildir write operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailMaildirWriteResult : IEmailMaildirWriteResultContract
{
    EmailMaildirFolder folder;
    EmailMaildirFlags flags;
    string uniqueName;
    string? fileName;
    string? path;
    EmailStreamWriteResult messageWriteResult;
    long byteCount;
    int collisionCount;
    bool overwritten;
    bool flushedToDisk;
    bool published;
    /// <summary>
    /// Creates a new maildir write result.
    /// </summary>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "messageWriteResult">The message write result used by the operation.</param>
    /// <param name = "byteCount">The number of items or bytes to process.</param>
    /// <param name = "collisionCount">The number of items or bytes to process.</param>
    /// <param name = "overwritten">Whether to enable the overwritten behavior.</param>
    /// <param name = "flushedToDisk">Whether to enable the flushed to disk behavior.</param>
    /// <param name = "published">Whether to enable the published behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMaildirWriteResult(EmailMaildirFolder folder, EmailMaildirFlags flags, string uniqueName, string? fileName, string? path, EmailStreamWriteResult messageWriteResult, long byteCount, int collisionCount, bool overwritten, bool flushedToDisk, bool published)
    {
        if (folder < EmailMaildirFolder.New || folder > EmailMaildirFolder.Current)
            throw new ArgumentOutOfRangeException("folder");
        if (uniqueName == null || messageWriteResult == null)
            throw new ArgumentNullException("uniqueName");
        if (!new EmailMaildirNameOperations().IsSafeUniqueName(uniqueName))
            throw new ArgumentException("A Maildir write result requires a safe unique name.", "uniqueName");
        int numericFlags = (int)flags;
        if (numericFlags < 0 || (numericFlags & ~63) != 0)
            throw new ArgumentOutOfRangeException("flags");
        if (folder == EmailMaildirFolder.New && flags != EmailMaildirFlags.None)
            throw new ArgumentException("A result in new cannot carry current-message flags.", "flags");
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException("byteCount");
        if (collisionCount < 0)
            throw new ArgumentOutOfRangeException("collisionCount");
        if (published && (fileName == null || path == null))
            throw new ArgumentException("Published Maildir results require a filename and path.", "fileName");
        if (published && !new EmailMaildirNameOperations().IsSafeFileName(fileName, folder))
            throw new ArgumentException("A published Maildir result requires a safe filename.", "fileName");
        this.folder = folder;
        this.flags = flags;
        this.uniqueName = uniqueName;
        this.fileName = fileName;
        this.path = path;
        this.messageWriteResult = messageWriteResult;
        this.byteCount = byteCount;
        this.collisionCount = collisionCount;
        this.overwritten = overwritten;
        this.flushedToDisk = flushedToDisk;
        this.published = published;
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
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    public EmailMaildirFlags GetFlags()
    {
        return flags;
    }

    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    public string GetUniqueName()
    {
        return uniqueName;
    }

    /// <summary>
    /// Determines whether the file name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file name is available; otherwise, <c>false</c>.</returns>
    public bool HasFileName()
    {
        return fileName != null;
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string? GetFileName()
    {
        return fileName;
    }

    /// <summary>
    /// Determines whether the path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the path is available; otherwise, <c>false</c>.</returns>
    public bool HasPath()
    {
        return path != null;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public string? GetPath()
    {
        return path;
    }

    /// <summary>
    /// Gets the message write result.
    /// </summary>
    /// <returns>The current message write result.</returns>
    public EmailStreamWriteResult GetMessageWriteResult()
    {
        return messageWriteResult;
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
    /// Gets the collision count.
    /// </summary>
    /// <returns>The current collision count.</returns>
    public int GetCollisionCount()
    {
        return collisionCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was overwritten.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasOverwritten()
    {
        return overwritten;
    }

    /// <summary>
    /// Determines whether the operation or source value was flushed to disk.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasFlushedToDisk()
    {
        return flushedToDisk;
    }

    /// <summary>
    /// Determines whether the operation or source value was published.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasPublished()
    {
        return published;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return published && messageWriteResult.IsSuccess();
    }
}
