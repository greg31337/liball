using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFileLoadResult"/>.
/// </summary>
public interface IEmailFileLoadResultContract
{
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    string GetPath();
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string GetFileName();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    byte[] GetBytes();
    /// <summary>
    /// Gets the last write time utc.
    /// </summary>
    /// <returns>The current last write time utc.</returns>
    DateTimeOffset GetLastWriteTimeUtc();
    /// <summary>
    /// Gets the parse result.
    /// </summary>
    /// <returns>The current parse result.</returns>
    EmailParseResult GetParseResult();
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
