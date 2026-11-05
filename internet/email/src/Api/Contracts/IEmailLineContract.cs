using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailLine"/>.
/// </summary>
public interface IEmailLineContract
{
    /// <summary>
    /// Gets the content length.
    /// </summary>
    /// <returns>The current content length.</returns>
    int GetContentLength();
    /// <summary>
    /// Gets the content bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content bytes.</returns>
    byte[] GetContentBytes();
    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    EmailLineEnding GetLineEnding();
    /// <summary>
    /// Determines whether the line ending is present or available.
    /// </summary>
    /// <returns><c>true</c> when the line ending is available; otherwise, <c>false</c>.</returns>
    bool HasLineEnding();
    /// <summary>
    /// Gets the line ending byte count.
    /// </summary>
    /// <returns>The current line ending byte count.</returns>
    int GetLineEndingByteCount();
    /// <summary>
    /// Gets the total byte count.
    /// </summary>
    /// <returns>The current total byte count.</returns>
    long GetTotalByteCount();
}
