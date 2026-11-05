using System;

namespace email;
/// <summary>
/// Describes the outcome of a file save operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailFileSaveResult : IEmailFileSaveResultContract
{
    string path;
    EmailWriteResult writeResult;
    bool overwritten;
    bool flushedToDisk;
    bool saved;
    /// <summary>
    /// Creates a new file save result.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "writeResult">The write result used by the operation.</param>
    /// <param name = "overwritten">Whether to enable the overwritten behavior.</param>
    /// <param name = "flushedToDisk">Whether to enable the flushed to disk behavior.</param>
    /// <param name = "saved">Whether to enable the saved behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFileSaveResult(string path, EmailWriteResult writeResult, bool overwritten, bool flushedToDisk, bool saved)
    {
        if (path == null || writeResult == null)
            throw new ArgumentNullException("path");
        this.path = path;
        this.writeResult = writeResult;
        this.overwritten = overwritten;
        this.flushedToDisk = flushedToDisk;
        this.saved = saved;
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
    /// Gets the write result.
    /// </summary>
    /// <returns>The current write result.</returns>
    public EmailWriteResult GetWriteResult()
    {
        return writeResult;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return saved ? writeResult.GetByteCount() : 0;
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
    /// Determines whether the operation or source value was saved.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasSaved()
    {
        return saved;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return saved && writeResult.IsSuccess();
    }
}
