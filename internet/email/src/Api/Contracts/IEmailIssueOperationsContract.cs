using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailIssue"/>.
/// </summary>
public interface IEmailIssueOperationsContract
{
    /// <summary>Creates an issue without source or recovery details.</summary>
    /// <param name = "severity">The issue severity.</param>
    /// <param name = "code">The stable issue code.</param>
    /// <param name = "description">The issue description.</param>
    /// <returns>The configured issue.</returns>
    EmailIssue CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity severity, EmailIssueCode code, string description);
    /// <summary>
    /// Creates a new issue.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <param name = "code">The code used by the operation.</param>
    /// <param name = "description">The description used by the operation.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "entityPath">The file-system or message path to use.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "recoveryDescription">The recovery description used by the operation.</param>
    /// <returns>The configured issue.</returns>
    EmailIssue CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity severity, EmailIssueCode code, string? description, EmailSourcePosition? sourcePosition, EmailPath? entityPath, string? fieldName, string? recoveryDescription);
}
