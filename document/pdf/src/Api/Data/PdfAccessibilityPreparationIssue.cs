using System;

namespace pdf;

/// <summary>
/// Describes one accessibility preparation found while checking a document.
/// </summary>
public class PdfAccessibilityPreparationIssue
{
    /// <summary>
    /// Stores the severity used by this object.
    /// </summary>
    public PdfAccessibilityPreparationIssueSeverity Severity;
    /// <summary>
    /// Stores the code used by this object.
    /// </summary>
    public string Code;
    /// <summary>
    /// Stores the message used by this object.
    /// </summary>
    public string Message;

    /// <summary>
    /// Initializes a new accessibility preparation issue.
    /// </summary>
    /// <param name="severity">The severity to use for this operation.</param>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity severity, string code, string message)
    {
        if (severity != PdfAccessibilityPreparationIssueSeverity.Error && severity != PdfAccessibilityPreparationIssueSeverity.Warning)
        {
            throw new ArgumentException("Accessibility preparation issue severity is not supported.", "severity");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (code.Length == 0)
        {
            throw new ArgumentException("Issue code cannot be empty.", "code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        Severity = severity;
        Code = code;
        Message = message;
    }
}
