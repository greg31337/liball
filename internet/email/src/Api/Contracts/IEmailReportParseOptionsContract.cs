using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportParseOptions"/>.
/// </summary>
public interface IEmailReportParseOptionsContract
{
    /// <summary>
    /// Gets the maximum report bytes.
    /// </summary>
    /// <returns>The current maximum report bytes.</returns>
    int GetMaximumReportBytes();
    /// <summary>
    /// Sets the maximum report bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumReportBytes(int value);
    /// <summary>
    /// Gets the maximum part count.
    /// </summary>
    /// <returns>The current maximum part count.</returns>
    int GetMaximumPartCount();
    /// <summary>
    /// Sets the maximum part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPartCount(int value);
    /// <summary>
    /// Gets the maximum field block count.
    /// </summary>
    /// <returns>The current maximum field block count.</returns>
    int GetMaximumFieldBlockCount();
    /// <summary>
    /// Sets the maximum field block count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFieldBlockCount(int value);
    /// <summary>
    /// Gets the maximum field count.
    /// </summary>
    /// <returns>The current maximum field count.</returns>
    int GetMaximumFieldCount();
    /// <summary>
    /// Sets the maximum field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFieldCount(int value);
    /// <summary>
    /// Gets the maximum fields per block.
    /// </summary>
    /// <returns>The current maximum fields per block.</returns>
    int GetMaximumFieldsPerBlock();
    /// <summary>
    /// Sets the maximum fields per block used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFieldsPerBlock(int value);
    /// <summary>
    /// Gets the maximum field block bytes.
    /// </summary>
    /// <returns>The current maximum field block bytes.</returns>
    int GetMaximumFieldBlockBytes();
    /// <summary>
    /// Sets the maximum field block bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFieldBlockBytes(int value);
    /// <summary>
    /// Gets the maximum line bytes.
    /// </summary>
    /// <returns>The current maximum line bytes.</returns>
    int GetMaximumLineBytes();
    /// <summary>
    /// Sets the maximum line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumLineBytes(int value);
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailStructuredValueParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailStructuredValueParseMode value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailReportParseOptions Clone();
}
