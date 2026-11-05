using System;

namespace pdf;

/// <summary>
/// Describes one PDF structure or conformance validation finding.
/// </summary>
public class PdfValidationIssue
{
    /// <summary>Stores the finding severity.</summary>
    public PdfValidationIssueSeverity Severity;
    /// <summary>Stores the stable finding code.</summary>
    public string Code;
    /// <summary>Stores the developer-readable finding message.</summary>
    public string Message;
    /// <summary>Stores the related indirect object number, or zero when the finding is document-wide.</summary>
    public int ObjectNumber;

    /// <summary>Initializes a validation finding.</summary>
    /// <param name="severity">The finding severity.</param>
    /// <param name="code">The stable non-empty finding code.</param>
    /// <param name="message">The developer-readable finding message.</param>
    /// <param name="objectNumber">The related non-negative indirect object number.</param>
    public PdfValidationIssue(PdfValidationIssueSeverity severity, string code, string message, int objectNumber)
    {
        if (severity != PdfValidationIssueSeverity.Error && severity != PdfValidationIssueSeverity.Warning)
        {
            throw new ArgumentException("Validation issue severity is not supported.", "severity");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (code.Length == 0)
        {
            throw new ArgumentException("Validation issue code cannot be empty.", "code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (objectNumber < 0)
        {
            throw new ArgumentOutOfRangeException("objectNumber", "Validation object number cannot be negative.");
        }

        Severity = severity;
        Code = code;
        Message = message;
        ObjectNumber = objectNumber;
    }
}
