using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageAnalysisOptions"/>.
/// </summary>
public interface IEmailMessageAnalysisOptionsContract
{
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDepth(int value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    int GetMaximumHeaderCount();
    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCount(int value);
    /// <summary>
    /// Gets the maximum address count.
    /// </summary>
    /// <returns>The current maximum address count.</returns>
    int GetMaximumAddressCount();
    /// <summary>
    /// Sets the maximum address count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAddressCount(int value);
    /// <summary>
    /// Gets the maximum structured header bytes.
    /// </summary>
    /// <returns>The current maximum structured header bytes.</returns>
    int GetMaximumStructuredHeaderBytes();
    /// <summary>
    /// Sets the maximum structured header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumStructuredHeaderBytes(int value);
    /// <summary>
    /// Gets the maximum decoded content bytes.
    /// </summary>
    /// <returns>The current maximum decoded content bytes.</returns>
    long GetMaximumDecodedContentBytes();
    /// <summary>
    /// Sets the maximum decoded content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDecodedContentBytes(long value);
    /// <summary>
    /// Gets the measure repeatable decoded content.
    /// </summary>
    /// <returns><c>true</c> when the measure repeatable decoded content setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetMeasureRepeatableDecodedContent();
    /// <summary>
    /// Sets the measure repeatable decoded content used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the measure repeatable decoded content behavior.</param>
    void SetMeasureRepeatableDecodedContent(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMessageAnalysisOptions Clone();
}
