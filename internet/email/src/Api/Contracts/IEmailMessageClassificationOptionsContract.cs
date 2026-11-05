using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageClassificationOptions"/>.
/// </summary>
public interface IEmailMessageClassificationOptionsContract
{
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
    /// Gets the maximum header value bytes.
    /// </summary>
    /// <returns>The current maximum header value bytes.</returns>
    int GetMaximumHeaderValueBytes();
    /// <summary>
    /// Sets the maximum header value bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderValueBytes(int value);
    /// <summary>
    /// Gets the maximum subject characters.
    /// </summary>
    /// <returns>The current maximum subject characters.</returns>
    int GetMaximumSubjectCharacters();
    /// <summary>
    /// Sets the maximum subject characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSubjectCharacters(int value);
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
    /// Gets the report parse options.
    /// </summary>
    /// <returns>The current report parse options.</returns>
    EmailReportParseOptions GetReportParseOptions();
    /// <summary>
    /// Sets the report parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetReportParseOptions(EmailReportParseOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMessageClassificationOptions Clone();
}
