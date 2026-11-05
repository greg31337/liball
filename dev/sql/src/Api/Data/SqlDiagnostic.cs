using System;

namespace sql;

/// <summary>Describes a lexer, parser, or validation issue and its location in the source text.</summary>
public class SqlDiagnostic
{
    /// <summary>The severity associated with this value.</summary>
    public SqlDiagnosticSeverity Severity;
    /// <summary>The code associated with this value.</summary>
    public string? Code;
    /// <summary>The message associated with this value.</summary>
    public string Message;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The original source text used to produce this value.</summary>
    public SqlSourceText? SourceText;

    /// <summary>Initializes a new instance of <see cref="SqlDiagnostic"/> from the supplied values.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">message is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlDiagnostic Create(SqlDiagnosticSeverity severity, string message, SqlTextSpan span)
    {
        return new SqlDiagnostic(severity, null, message, span, null);
    }

    /// <summary>Creates a diagnostic with code and source metadata.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="sourceText">The optional source text represented by the diagnostic.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal static SqlDiagnostic Create(SqlDiagnosticSeverity severity, string? code, string message, SqlTextSpan span, SqlSourceText? sourceText)
    {
        return new SqlDiagnostic(severity, code, message, span, sourceText);
    }

    /// <summary>Initializes a new instance of <see cref="SqlDiagnostic"/> from the supplied values.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <exception cref="ArgumentNullException">message is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDiagnostic(SqlDiagnosticSeverity severity, string? code, string message, SqlTextSpan span, SqlSourceText? sourceText)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Severity = severity;
        Code = code;
        Message = message;
        Span = span;
        SourceText = sourceText;
    }

    /// <summary>Returns whether code is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasCode()
    {
        return Code != null && Code.Length > 0;
    }

    /// <summary>Returns whether source text is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSourceText()
    {
        return SourceText != null;
    }

    /// <summary>Returns the line number associated with this value.</summary>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineNumber()
    {
        if (SourceText == null)
        {
            return 0;
        }

        return SourceText.GetLineNumberForSpan(Span);
    }

    /// <summary>Returns the column number associated with this value.</summary>
    /// <returns>The requested zero-based position.</returns>
    internal int GetColumnNumber()
    {
        if (SourceText == null)
        {
            return 0;
        }

        return SourceText.GetColumnNumberForSpan(Span);
    }
}
