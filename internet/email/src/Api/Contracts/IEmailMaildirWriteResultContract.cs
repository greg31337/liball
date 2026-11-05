using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirWriteResult"/>.
/// </summary>
public interface IEmailMaildirWriteResultContract
{
    /// <summary>
    /// Gets the folder.
    /// </summary>
    /// <returns>The current folder.</returns>
    EmailMaildirFolder GetFolder();
    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    EmailMaildirFlags GetFlags();
    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    string GetUniqueName();
    /// <summary>
    /// Determines whether the file name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file name is available; otherwise, <c>false</c>.</returns>
    bool HasFileName();
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string? GetFileName();
    /// <summary>
    /// Determines whether the path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the path is available; otherwise, <c>false</c>.</returns>
    bool HasPath();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    string? GetPath();
    /// <summary>
    /// Gets the message write result.
    /// </summary>
    /// <returns>The current message write result.</returns>
    EmailStreamWriteResult GetMessageWriteResult();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the collision count.
    /// </summary>
    /// <returns>The current collision count.</returns>
    int GetCollisionCount();
    /// <summary>
    /// Determines whether the operation or source value was overwritten.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasOverwritten();
    /// <summary>
    /// Determines whether the operation or source value was flushed to disk.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasFlushedToDisk();
    /// <summary>
    /// Determines whether the operation or source value was published.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasPublished();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
