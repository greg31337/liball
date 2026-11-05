using System;

namespace email;
/// <summary>
/// Describes the outcome of a file load operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailFileLoadResult : IEmailFileLoadResultContract
{
    string path;
    byte[] bytes;
    DateTimeOffset lastWriteTimeUtc;
    EmailParseResult parseResult;
    /// <summary>
    /// Creates a new file load result.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "lastWriteTimeUtc">The last write time utc used by the operation.</param>
    /// <param name = "parseResult">The parse result used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFileLoadResult(string path, byte[] bytes, DateTimeOffset lastWriteTimeUtc, EmailParseResult parseResult)
    {
        if (path == null || bytes == null || parseResult == null)
            throw new ArgumentNullException("path");
        this.path = path;
        this.bytes = new byte[bytes.Length];
        bytes.CopyTo(this.bytes, 0);
        this.lastWriteTimeUtc = lastWriteTimeUtc;
        this.parseResult = parseResult;
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
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string GetFileName()
    {
        return System.IO.Path.GetFileName(path);
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return bytes.LongLength;
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    public byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];
        bytes.CopyTo(copy, 0);
        return copy;
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
    /// Gets the parse result.
    /// </summary>
    /// <returns>The current parse result.</returns>
    public EmailParseResult GetParseResult()
    {
        return parseResult;
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return parseResult.HasMessage();
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return parseResult.GetMessage();
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return parseResult.IsSuccess();
    }
}
