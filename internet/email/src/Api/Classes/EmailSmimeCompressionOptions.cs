using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Controls how S/MIME compression operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeCompressionOptions : IEmailSmimeCompressionOptionsContract
{
    EmailCryptographicLimits limits;
    CompressionLevel compressionLevel;
    /// <summary>
    /// Creates a new S/MIME compression options object.
    /// </summary>
    public EmailSmimeCompressionOptions()
    {
        limits = new EmailCryptographicLimits();
        compressionLevel = CompressionLevel.Optimal;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the compression level.
    /// </summary>
    /// <returns>The current compression level.</returns>
    public CompressionLevel GetCompressionLevel()
    {
        return compressionLevel;
    }

    /// <summary>
    /// Sets the compression level used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetCompressionLevel(CompressionLevel value)
    {
        if (value < CompressionLevel.Optimal || value > CompressionLevel.SmallestSize)
            throw new ArgumentOutOfRangeException("value");
        compressionLevel = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeCompressionOptions Clone()
    {
        EmailSmimeCompressionOptions copy = new EmailSmimeCompressionOptions();
        copy.SetLimits(limits);
        copy.SetCompressionLevel(compressionLevel);
        return copy;
    }
}
