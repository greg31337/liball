using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFlowedTextOptions"/>.
/// </summary>
public interface IEmailFlowedTextOptionsContract
{
    /// <summary>
    /// Gets the delete space.
    /// </summary>
    /// <returns><c>true</c> when the delete space setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetDeleteSpace();
    /// <summary>
    /// Sets the delete space used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the delete space behavior.</param>
    void SetDeleteSpace(bool value);
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
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    int GetMaximumCharacters();
    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumCharacters(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailFlowedTextOptions Clone();
}
