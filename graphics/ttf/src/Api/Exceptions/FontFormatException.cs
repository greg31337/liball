namespace ttf;

/// <summary>Represents malformed or structurally invalid font data.</summary>
public class FontFormatException : System.Exception
{
    FontDiagnosticCode diagnosticCode;

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the format failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public FontFormatException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
        diagnosticCode = FontDiagnosticCode.None;
    }

    /// <summary>Gets the machine-readable diagnostic associated with this failure.</summary>
    /// <returns>The diagnostic code, or none for an unclassified format failure.</returns>
    public FontDiagnosticCode GetDiagnosticCode() { return diagnosticCode; }

    /// <summary>Creates an exception with a deterministic default message.</summary>
    /// <returns>A new format exception.</returns>
    internal static FontFormatException Create()
    {
        return Create("The font data is malformed or structurally invalid.");
    }

    /// <summary>Creates an exception with a caller-supplied message.</summary>
    /// <param name="message">The message that describes the format failure.</param>
    /// <returns>A new format exception.</returns>
    internal static FontFormatException Create(string message)
    {
        return new FontFormatException(message, null);
    }

    /// <summary>Creates an exception with a message and underlying exception.</summary>
    /// <param name="message">The message that describes the format failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    /// <returns>A new format exception.</returns>
    internal static FontFormatException Create(
        string message,
        System.Exception innerException)
    {
        return new FontFormatException(message, innerException);
    }

    internal static FontFormatException CreateDiagnostic(
        string message,
        FontDiagnosticCode diagnosticCode)
    {
        FontFormatException exception = new FontFormatException(message, null);
        exception.diagnosticCode = diagnosticCode;
        return exception;
    }
}
