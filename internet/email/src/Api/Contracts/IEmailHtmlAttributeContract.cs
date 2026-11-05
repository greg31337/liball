using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlAttribute"/>.
/// </summary>
public interface IEmailHtmlAttributeContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the raw value.
    /// </summary>
    /// <returns>The current raw value.</returns>
    string GetRawValue();
    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    string GetValue();
    /// <summary>
    /// Determines whether the value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the value is available; otherwise, <c>false</c>.</returns>
    bool HasValue();
    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    int GetSourceOffset();
    /// <summary>
    /// Gets the source length.
    /// </summary>
    /// <returns>The current source length.</returns>
    int GetSourceLength();
}
