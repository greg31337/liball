using System;

namespace email;
/// <summary>
/// Describes one ordered diagnostic produced while parsing, validating, transforming, or writing email data.
/// </summary>
public class EmailIssue : IEmailIssueContract
{
    EmailIssueSeverity severity;
    EmailIssueCode code;
    string? description;
    EmailSourcePosition? sourcePosition;
    EmailPath? entityPath;
    string? fieldName;
    string? recoveryDescription;
    /// <summary>
    /// Creates an issue without source or recovery details.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <param name = "code">The code used by the operation.</param>
    /// <param name = "description">The description used by the operation.</param>
    public EmailIssue(EmailIssueSeverity severity, EmailIssueCode code, string description)
    {
        Initialize(severity, code, description, null, null, null, null);
    }

    internal static EmailIssue CreateImplementationWithSeverityAndCodeAndDescription(EmailIssueSeverity severity, EmailIssueCode code, string description)
    {
        return new EmailIssue(severity, code, description);
    }

    internal static EmailIssue CreateImplementationWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity severity, EmailIssueCode code, string? description, EmailSourcePosition? sourcePosition, EmailPath? entityPath, string? fieldName, string? recoveryDescription)
    {
        EmailIssue value = new EmailIssue(severity, code, description ?? throw new ArgumentNullException("description"));
        value.Initialize(severity, code, description, sourcePosition, entityPath, fieldName, recoveryDescription);
        return value;
    }

    void Initialize(EmailIssueSeverity severity, EmailIssueCode code, string? description, EmailSourcePosition? sourcePosition, EmailPath? entityPath, string? fieldName, string? recoveryDescription)
    {
        if (severity < EmailIssueSeverity.Information || severity > EmailIssueSeverity.Fatal)
        {
            throw new ArgumentOutOfRangeException("severity");
        }

        if (code == EmailIssueCode.Unknown || !new EmailIssueCodeRangesOperations().IsLibraryCode((int)code))
        {
            throw new ArgumentOutOfRangeException("code");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        if (description.Length == 0)
        {
            throw new ArgumentException("Issue descriptions cannot be empty.", "description");
        }

        if (fieldName != null && fieldName.Length == 0)
        {
            throw new ArgumentException("Field names cannot be empty when supplied.", "fieldName");
        }

        if (recoveryDescription != null && recoveryDescription.Length == 0)
        {
            throw new ArgumentException("Recovery descriptions cannot be empty when supplied.", "recoveryDescription");
        }

        this.severity = severity;
        this.code = code;
        this.description = description;
        this.sourcePosition = sourcePosition;
        this.entityPath = entityPath;
        this.fieldName = fieldName;
        this.recoveryDescription = recoveryDescription;
    }

    /// <summary>
    /// Gets the severity.
    /// </summary>
    /// <returns>The current severity.</returns>
    public EmailIssueSeverity GetSeverity()
    {
        return severity;
    }

    /// <summary>
    /// Gets the code.
    /// </summary>
    /// <returns>The current code.</returns>
    public EmailIssueCode GetCode()
    {
        return code;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    public string? GetDescription()
    {
        return description;
    }

    /// <summary>
    /// Determines whether the source position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source position is available; otherwise, <c>false</c>.</returns>
    public bool HasSourcePosition()
    {
        return sourcePosition != null;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition? GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Determines whether the entity path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity path is available; otherwise, <c>false</c>.</returns>
    public bool HasEntityPath()
    {
        return entityPath != null;
    }

    /// <summary>
    /// Gets the entity path.
    /// </summary>
    /// <returns>The current entity path.</returns>
    public EmailPath? GetEntityPath()
    {
        return entityPath;
    }

    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    public bool HasFieldName()
    {
        return fieldName != null;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    public string? GetFieldName()
    {
        return fieldName;
    }

    /// <summary>
    /// Determines whether the recovery description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the recovery description is available; otherwise, <c>false</c>.</returns>
    public bool HasRecoveryDescription()
    {
        return recoveryDescription != null;
    }

    /// <summary>
    /// Gets the recovery description.
    /// </summary>
    /// <returns>The current recovery description.</returns>
    public string? GetRecoveryDescription()
    {
        return recoveryDescription;
    }
}
