using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailIssue"/>.
/// </summary>
public interface IEmailIssueContract
{
    /// <summary>
    /// Gets the severity.
    /// </summary>
    /// <returns>The current severity.</returns>
    EmailIssueSeverity GetSeverity();
    /// <summary>
    /// Gets the code.
    /// </summary>
    /// <returns>The current code.</returns>
    EmailIssueCode GetCode();
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    string? GetDescription();
    /// <summary>
    /// Determines whether the source position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source position is available; otherwise, <c>false</c>.</returns>
    bool HasSourcePosition();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition? GetSourcePosition();
    /// <summary>
    /// Determines whether the entity path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity path is available; otherwise, <c>false</c>.</returns>
    bool HasEntityPath();
    /// <summary>
    /// Gets the entity path.
    /// </summary>
    /// <returns>The current entity path.</returns>
    EmailPath? GetEntityPath();
    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    bool HasFieldName();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string? GetFieldName();
    /// <summary>
    /// Determines whether the recovery description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the recovery description is available; otherwise, <c>false</c>.</returns>
    bool HasRecoveryDescription();
    /// <summary>
    /// Gets the recovery description.
    /// </summary>
    /// <returns>The current recovery description.</returns>
    string? GetRecoveryDescription();
}
