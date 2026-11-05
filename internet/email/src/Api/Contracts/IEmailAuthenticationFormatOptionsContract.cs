using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationFormatOptions"/>.
/// </summary>
public interface IEmailAuthenticationFormatOptionsContract
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
    /// Gets the include comments.
    /// </summary>
    /// <returns><c>true</c> when the include comments setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeComments();
    /// <summary>
    /// Sets the include comments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include comments behavior.</param>
    void SetIncludeComments(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailAuthenticationFormatOptions Clone();
}
