using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirWriterOptions"/>.
/// </summary>
public interface IEmailMaildirWriterOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailArchiveLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailArchiveLimits value);
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
    /// Gets the collision policy.
    /// </summary>
    /// <returns>The current collision policy.</returns>
    EmailMaildirCollisionPolicy GetCollisionPolicy();
    /// <summary>
    /// Sets the collision policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetCollisionPolicy(EmailMaildirCollisionPolicy value);
    /// <summary>
    /// Gets the maximum collision attempts.
    /// </summary>
    /// <returns>The current maximum collision attempts.</returns>
    int GetMaximumCollisionAttempts();
    /// <summary>
    /// Sets the maximum collision attempts used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCollisionAttempts(int value);
    /// <summary>
    /// Gets the host name.
    /// </summary>
    /// <returns>The current host name.</returns>
    string GetHostName();
    /// <summary>
    /// Sets the host name used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetHostName(string value);
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
    EmailMaildirWriterOptions Clone();
}
