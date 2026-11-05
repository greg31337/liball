using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecompressionOptions"/>.
/// </summary>
public interface IEmailSmimeDecompressionOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailCryptographicLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailCryptographicLimits value);
    /// <summary>
    /// Gets the read buffer bytes.
    /// </summary>
    /// <returns>The current read buffer bytes.</returns>
    int GetReadBufferBytes();
    /// <summary>
    /// Sets the read buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetReadBufferBytes(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeDecompressionOptions Clone();
}
