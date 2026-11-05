namespace ttf;

/// <summary>Represents font processing that exceeded a configured resource limit.</summary>
public class FontLimitException : System.Exception
{
    FontDiagnosticCode diagnosticCode;

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the exceeded limit.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public FontLimitException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
        diagnosticCode = FontDiagnosticCode.None;
    }

    /// <summary>Gets the machine-readable diagnostic associated with this failure.</summary>
    /// <returns>The diagnostic code, or none for an unclassified limit failure.</returns>
    public FontDiagnosticCode GetDiagnosticCode() { return diagnosticCode; }

    /// <summary>Creates an exception with a deterministic default message.</summary>
    /// <returns>A new limit exception.</returns>
    internal static FontLimitException Create()
    {
        return Create("Font processing exceeded a configured resource limit.");
    }

    /// <summary>Creates an exception with a caller-supplied message.</summary>
    /// <param name="message">The message that describes the exceeded limit.</param>
    /// <returns>A new limit exception.</returns>
    internal static FontLimitException Create(string message)
    {
        return CreateDiagnostic(
            message,
            FontDiagnosticCode.LimitExceeded);
    }

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the exceeded limit.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    /// <returns>A new limit exception.</returns>
    internal static FontLimitException Create(
        string message,
        System.Exception innerException)
    {
        return new FontLimitException(message, innerException);
    }

    internal static FontLimitException CreateDiagnostic(
        string message,
        FontDiagnosticCode diagnosticCode)
    {
        FontLimitException exception = new FontLimitException(message, null);
        exception.diagnosticCode = diagnosticCode;
        return exception;
    }
}
