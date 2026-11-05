using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailLimits"/>.
/// </summary>
public interface IEmailLimitsContract
{
    /// <summary>
    /// Gets the parsing limits.
    /// </summary>
    /// <returns>The current parsing limits.</returns>
    EmailParsingLimits GetParsingLimits();
    /// <summary>
    /// Sets the parsing limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetParsingLimits(EmailParsingLimits value);
    /// <summary>
    /// Gets the decoding limits.
    /// </summary>
    /// <returns>The current decoding limits.</returns>
    EmailDecodingLimits GetDecodingLimits();
    /// <summary>
    /// Sets the decoding limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetDecodingLimits(EmailDecodingLimits value);
    /// <summary>
    /// Gets the HTML limits.
    /// </summary>
    /// <returns>The current HTML limits.</returns>
    EmailHtmlLimits GetHtmlLimits();
    /// <summary>
    /// Sets the HTML limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHtmlLimits(EmailHtmlLimits value);
    /// <summary>
    /// Gets the archive limits.
    /// </summary>
    /// <returns>The current archive limits.</returns>
    EmailArchiveLimits GetArchiveLimits();
    /// <summary>
    /// Sets the archive limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetArchiveLimits(EmailArchiveLimits value);
    /// <summary>
    /// Gets the temporary storage limits.
    /// </summary>
    /// <returns>The current temporary storage limits.</returns>
    EmailTemporaryStorageLimits GetTemporaryStorageLimits();
    /// <summary>
    /// Sets the temporary storage limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetTemporaryStorageLimits(EmailTemporaryStorageLimits value);
    /// <summary>
    /// Gets the cryptographic limits.
    /// </summary>
    /// <returns>The current cryptographic limits.</returns>
    EmailCryptographicLimits GetCryptographicLimits();
    /// <summary>
    /// Sets the cryptographic limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCryptographicLimits(EmailCryptographicLimits value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailLimits Clone();
}
