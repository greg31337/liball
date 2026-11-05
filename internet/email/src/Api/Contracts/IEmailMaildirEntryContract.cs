using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirEntry"/>.
/// </summary>
public interface IEmailMaildirEntryContract
{
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Gets the folder.
    /// </summary>
    /// <returns>The current folder.</returns>
    EmailMaildirFolder GetFolder();
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    EmailMaildirName GetName();
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string GetFileName();
    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    string GetUniqueName();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    string GetPath();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the last write time utc.
    /// </summary>
    /// <returns>The current last write time utc.</returns>
    DateTimeOffset GetLastWriteTimeUtc();
    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    EmailMaildirFlags GetFlags();
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
