using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageIdFormatOptions"/>.
/// </summary>
public interface IEmailMessageIdFormatOptionsContract
{
    /// <summary>
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    int GetFirstLineColumn();
    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFirstLineColumn(int value);
    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    int GetMaximumLineLength();
    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLineLength(int value);
    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    int GetMaximumOutputBytes();
    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumOutputBytes(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMessageIdFormatOptions Clone();
}
