using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefReaderOptions"/>.
/// </summary>
public interface IEmailTnefReaderOptionsContract
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
    /// Gets the require valid checksums.
    /// </summary>
    /// <returns><c>true</c> when the require valid checksums setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireValidChecksums();
    /// <summary>
    /// Sets the require valid checksums used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require valid checksums behavior.</param>
    void SetRequireValidChecksums(bool value);
    /// <summary>
    /// Gets the maximum property count.
    /// </summary>
    /// <returns>The current maximum property count.</returns>
    int GetMaximumPropertyCount();
    /// <summary>
    /// Sets the maximum property count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPropertyCount(int value);
    /// <summary>
    /// Gets the maximum property value count.
    /// </summary>
    /// <returns>The current maximum property value count.</returns>
    int GetMaximumPropertyValueCount();
    /// <summary>
    /// Sets the maximum property value count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPropertyValueCount(int value);
    /// <summary>
    /// Gets the maximum recipient count.
    /// </summary>
    /// <returns>The current maximum recipient count.</returns>
    int GetMaximumRecipientCount();
    /// <summary>
    /// Sets the maximum recipient count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumRecipientCount(int value);
    /// <summary>
    /// Gets the maximum embedded depth.
    /// </summary>
    /// <returns>The current maximum embedded depth.</returns>
    int GetMaximumEmbeddedDepth();
    /// <summary>
    /// Sets the maximum embedded depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEmbeddedDepth(int value);
    /// <summary>
    /// Gets the maximum named property characters.
    /// </summary>
    /// <returns>The current maximum named property characters.</returns>
    int GetMaximumNamedPropertyCharacters();
    /// <summary>
    /// Sets the maximum named property characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumNamedPropertyCharacters(int value);
    /// <summary>
    /// Gets the maximum decoded rtf bytes.
    /// </summary>
    /// <returns>The current maximum decoded rtf bytes.</returns>
    long GetMaximumDecodedRtfBytes();
    /// <summary>
    /// Sets the maximum decoded rtf bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumDecodedRtfBytes(long value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTnefReaderOptions Clone();
}
