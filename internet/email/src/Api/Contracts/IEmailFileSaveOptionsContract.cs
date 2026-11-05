using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFileSaveOptions"/>.
/// </summary>
public interface IEmailFileSaveOptionsContract
{
    /// <summary>
    /// Gets the write options.
    /// </summary>
    /// <returns>The current write options.</returns>
    EmailWriteOptions GetWriteOptions();
    /// <summary>
    /// Sets the write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetWriteOptions(EmailWriteOptions value);
    /// <summary>
    /// Gets the overwrite policy.
    /// </summary>
    /// <returns>The current overwrite policy.</returns>
    EmailFileOverwritePolicy GetOverwritePolicy();
    /// <summary>
    /// Sets the overwrite policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetOverwritePolicy(EmailFileOverwritePolicy value);
    /// <summary>
    /// Gets the maximum path characters.
    /// </summary>
    /// <returns>The current maximum path characters.</returns>
    int GetMaximumPathCharacters();
    /// <summary>
    /// Sets the maximum path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPathCharacters(int value);
    /// <summary>
    /// Gets the require eml extension.
    /// </summary>
    /// <returns><c>true</c> when the require eml extension setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireEmlExtension();
    /// <summary>
    /// Sets the require eml extension used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require eml extension behavior.</param>
    void SetRequireEmlExtension(bool value);
    /// <summary>
    /// Gets the flush to disk.
    /// </summary>
    /// <returns><c>true</c> when the flush to disk setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetFlushToDisk();
    /// <summary>
    /// Sets the flush to disk used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the flush to disk behavior.</param>
    void SetFlushToDisk(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailFileSaveOptions Clone();
}
