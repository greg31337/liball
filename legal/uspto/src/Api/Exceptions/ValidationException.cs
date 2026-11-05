namespace uspto;

/// <summary>
/// Reports invalid caller input or an invalid option combination.
/// </summary>
public class ValidationException : global::System.Exception
{
    /// <summary>The public field or request element associated with the failure.</summary>
    public string? FieldName;

    /// <summary>A stable, credential-free validation error code.</summary>
    public string? ErrorCode;

    /// <summary>Initializes a validation exception and its input context.</summary>
    /// <param name="message">The validation failure message.</param>
    /// <param name="fieldName">The public field or request element associated with the failure.</param>
    /// <param name="errorCode">A stable validation error code.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public ValidationException(string message, string? fieldName, string? errorCode, global::System.Exception? innerException)
        : base(message, innerException)
    {
        FieldName = fieldName;
        ErrorCode = errorCode;
    }
}
