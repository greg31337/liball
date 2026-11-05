using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedIndexOptions"/>.
/// </summary>
public interface IEmailRelatedIndexOptionsContract
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
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the maximum HTML bytes.
    /// </summary>
    /// <returns>The current maximum HTML bytes.</returns>
    int GetMaximumHtmlBytes();
    /// <summary>
    /// Sets the maximum HTML bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumHtmlBytes(int value);
    /// <summary>
    /// Gets the maximum references.
    /// </summary>
    /// <returns>The current maximum references.</returns>
    int GetMaximumReferences();
    /// <summary>
    /// Sets the maximum references used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumReferences(int value);
    /// <summary>
    /// Gets the maximum url characters.
    /// </summary>
    /// <returns>The current maximum url characters.</returns>
    int GetMaximumUrlCharacters();
    /// <summary>
    /// Sets the maximum url characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumUrlCharacters(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailRelatedIndexOptions Clone();
}
