using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFileSaveResult"/>.
/// </summary>
public interface IEmailFileSaveResultContract
{
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    string GetPath();
    /// <summary>
    /// Gets the write result.
    /// </summary>
    /// <returns>The current write result.</returns>
    EmailWriteResult GetWriteResult();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
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
    /// Determines whether the operation or source value was saved.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasSaved();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
