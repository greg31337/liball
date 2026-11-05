using System;
using System.Globalization;

namespace html;
/// <summary>Describes a bounded parser or processing problem without requiring an exception.</summary>
public class HtmlDiagnostic : IHtmlDiagnostic
{
    internal HtmlDiagnosticCode code;
    internal HtmlDiagnosticSeverity severity;
    internal string message;
    internal HtmlSourceSpan span;
    internal HtmlDiagnostic(HtmlDiagnosticCode code, HtmlDiagnosticSeverity severity, string message, HtmlSourceSpan span)
    {
        this.code = code;
        this.severity = severity;
        this.message = message;
        this.span = span;
    }

    internal static HtmlDiagnostic Create(HtmlDiagnosticCode code, HtmlDiagnosticSeverity severity, string message, HtmlSourceSpan span)
    {
        if ((int)code <= 0)
        {
            throw new ArgumentOutOfRangeException("code", (int)code, "A diagnostic must have a stable nonzero code.");
        }

        if ((int)severity < 0 || (int)severity > 3)
        {
            throw new ArgumentOutOfRangeException("severity", (int)severity, "Unknown HTML diagnostic severity.");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return new HtmlDiagnostic(code, severity, message, span);
    }

    /// <summary>Gets the stable machine-readable category for this diagnostic.</summary>
    /// <returns>The diagnostic code applications should use for programmatic decisions.</returns>
    public HtmlDiagnosticCode GetCode()
    {
        return this.code;
    }

    /// <summary>Formats the diagnostic code as its stable <c>UH</c> identifier.</summary>
    /// <returns><c>UH</c> followed by the five-digit numeric diagnostic code.</returns>
    public string GetIdentifier()
    {
        return "UH" + ((int)this.code).ToString("D5", CultureInfo.InvariantCulture);
    }

    /// <summary>Gets how strongly this diagnostic affects the requested operation.</summary>
    /// <returns>The information, warning, recoverable error, or fatal severity.</returns>
    public HtmlDiagnosticSeverity GetSeverity()
    {
        return this.severity;
    }

    /// <summary>Gets the deterministic human-readable explanation of the occurrence.</summary>
    /// <returns>Explanatory text intended for logs or display, not programmatic comparison.</returns>
    public string GetMessage()
    {
        return this.message;
    }

    /// <summary>Gets the source range associated with the occurrence.</summary>
    /// <returns>The half-open source span, or an explicitly unknown span when no honest location exists.</returns>
    public HtmlSourceSpan GetSpan()
    {
        return this.span;
    }
}
