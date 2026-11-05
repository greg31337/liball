using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodingSelectionOptions"/>.
/// </summary>
public interface IEmailEncodingSelectionOptionsContract
{
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    string GetMediaType();
    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetMediaType(string value);
    /// <summary>
    /// Gets the constraint.
    /// </summary>
    /// <returns>The current constraint.</returns>
    EmailTransferEncodingConstraint GetConstraint();
    /// <summary>
    /// Sets the constraint used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetConstraint(EmailTransferEncodingConstraint value);
    /// <summary>
    /// Gets the maximum line bytes.
    /// </summary>
    /// <returns>The current maximum line bytes.</returns>
    int GetMaximumLineBytes();
    /// <summary>
    /// Sets the maximum line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLineBytes(int value);
    /// <summary>
    /// Gets the minimum text percentage.
    /// </summary>
    /// <returns>The current minimum text percentage.</returns>
    int GetMinimumTextPercentage();
    /// <summary>
    /// Sets the minimum text percentage used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMinimumTextPercentage(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailEncodingSelectionOptions Clone();
}
