using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailParseOptions"/>.
/// </summary>
public interface IEmailParseOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailParseMode value);
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailLimits value);
    /// <summary>
    /// Gets the storage options.
    /// </summary>
    /// <returns>The current storage options.</returns>
    EmailStorageOptions GetStorageOptions();
    /// <summary>
    /// Sets the storage options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetStorageOptions(EmailStorageOptions value);
    /// <summary>
    /// Gets the compatibility options.
    /// </summary>
    /// <returns>The current compatibility options.</returns>
    EmailCompatibilityOptions GetCompatibilityOptions();
    /// <summary>
    /// Gets the body ingest options.
    /// </summary>
    /// <returns>The current body ingest options.</returns>
    EmailBodyIngestOptions GetBodyIngestOptions();
    /// <summary>
    /// Sets the body ingest options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetBodyIngestOptions(EmailBodyIngestOptions value);
    /// <summary>
    /// Sets the compatibility options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCompatibilityOptions(EmailCompatibilityOptions value);
    /// <summary>
    /// Gets the preserve source.
    /// </summary>
    /// <returns><c>true</c> when the preserve source setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetPreserveSource();
    /// <summary>
    /// Sets the preserve source used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the preserve source behavior.</param>
    void SetPreserveSource(bool value);
    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSmtpUtf8();
    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    void SetAllowSmtpUtf8(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailParseOptions Clone();
}
