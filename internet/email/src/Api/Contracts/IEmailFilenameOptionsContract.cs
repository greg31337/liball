using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFilenameOptions"/>.
/// </summary>
public interface IEmailFilenameOptionsContract
{
    /// <summary>
    /// Gets the normalization.
    /// </summary>
    /// <returns>The current normalization.</returns>
    EmailFilenameNormalization GetNormalization();
    /// <summary>
    /// Sets the normalization used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetNormalization(EmailFilenameNormalization value);
    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    /// <returns>The current maximum length.</returns>
    int GetMaximumLength();
    /// <summary>
    /// Sets the maximum length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLength(int value);
    /// <summary>
    /// Gets the fallback filename.
    /// </summary>
    /// <returns>The current fallback filename.</returns>
    string GetFallbackFilename();
    /// <summary>
    /// Sets the fallback filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetFallbackFilename(string value);
    /// <summary>
    /// Gets the replacement character.
    /// </summary>
    /// <returns>The current replacement character.</returns>
    char GetReplacementCharacter();
    /// <summary>
    /// Sets the replacement character used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    void SetReplacementCharacter(char value);
    /// <summary>
    /// Gets the case insensitive collisions.
    /// </summary>
    /// <returns><c>true</c> when the case insensitive collisions setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetCaseInsensitiveCollisions();
    /// <summary>
    /// Sets the case insensitive collisions used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case insensitive collisions behavior.</param>
    void SetCaseInsensitiveCollisions(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailFilenameOptions Clone();
}
