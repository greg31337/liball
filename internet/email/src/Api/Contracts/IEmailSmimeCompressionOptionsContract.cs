using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeCompressionOptions"/>.
/// </summary>
public interface IEmailSmimeCompressionOptionsContract
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
    /// Gets the compression level.
    /// </summary>
    /// <returns>The current compression level.</returns>
    CompressionLevel GetCompressionLevel();
    /// <summary>
    /// Sets the compression level used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetCompressionLevel(CompressionLevel value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailSmimeCompressionOptions Clone();
}
