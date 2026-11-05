using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailNormalizationChange"/>.
/// </summary>
public interface IEmailNormalizationChangeContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailNormalizationChangeKind GetKind();
    /// <summary>
    /// Determines whether the entity path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity path is available; otherwise, <c>false</c>.</returns>
    bool HasEntityPath();
    /// <summary>
    /// Gets the entity path.
    /// </summary>
    /// <returns>The current entity path.</returns>
    EmailPath GetEntityPath();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string GetFieldName();
    /// <summary>
    /// Determines whether the original value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original value is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalValue();
    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    string? GetOriginalValue();
    /// <summary>
    /// Determines whether the normalized value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the normalized value is available; otherwise, <c>false</c>.</returns>
    bool HasNormalizedValue();
    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    string? GetNormalizedValue();
}
