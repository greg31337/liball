namespace docx;

/// <summary>
/// Describes one validation, parsing, or repair issue.
/// </summary>
public class DocxError
{
    /// <summary>
    /// The severity used to decide whether the issue is informational, a warning, or an error.
    /// </summary>
    public DocxErrorSeverity Severity;
    /// <summary>
    /// The stable machine-readable code that identifies the kind of issue.
    /// </summary>
    public string Code;
    /// <summary>
    /// The human-readable explanation of the issue.
    /// </summary>
    public string Message;
    /// <summary>
    /// The package part, relationship, or XML location associated with the issue, or an empty string when it is package-wide.
    /// </summary>
    public string Location;

    /// <summary>
    /// Creates an error instance.
    /// </summary>
    /// <param name="severity">The severity used by the operation.</param>
    /// <param name="code">The code used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <param name="location">The location used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxError(DocxErrorSeverity severity, string code, string message, string location)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (location == null)
        {
            throw new ArgumentNullException("location");
        }

        this.Severity = severity;
        this.Code = code;
        this.Message = message;
        this.Location = location;
    }

    internal bool IsError()
    {
        return this.Severity == DocxErrorSeverity.Error;
    }

    internal bool IsWarning()
    {
        return this.Severity == DocxErrorSeverity.Warning;
    }
}
