using System;
using System.Security.Cryptography;
using System.Text;

namespace template;

/// <summary>
/// Stores template text together with identity metadata used in diagnostics, dependency tracking, and cache keys.
/// </summary>
public class TemplateSource
{
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the path for this source.
    /// </summary>
    public string? Path;
    /// <summary>
    /// Specifies the version for this source.
    /// </summary>
    public string? Version;
    /// <summary>
    /// Specifies the content hash for this source.
    /// </summary>
    public string? ContentHash;
    /// <summary>
    /// Specifies the source timestamp used for cache identity and invalidation.
    /// </summary>
    public string? Timestamp;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the length in characters.
    /// </summary>
    public int Length;

    /// <summary>
    /// Initializes a new template source and validates any supplied values.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="path">The path used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="contentHash">The content hash used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateSource(string name, string? path, string? version, string? contentHash, string text)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Name = name;
        Path = path;
        Version = version;
        ContentHash = contentHash;
        Timestamp = null;
        Text = text;
        Length = text.Length;
    }

    internal void Validate()
    {
        if (Name == null)
        {
            throw new ArgumentNullException("Name");
        }

        if (Text == null)
        {
            throw new ArgumentNullException("Text");
        }

        if (Length != Text.Length)
        {
            throw new ArgumentException("Template source length must match its text.", "Length");
        }
    }

    /// <summary>
    /// Creates a template representation from the supplied string.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateSource FromString(string text)
    {
        return new TemplateSource("", null, null, null, text);
    }

    /// <summary>
    /// Creates a template representation from the supplied named string.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateSource FromNamedString(string name, string text)
    {
        return new TemplateSource(name, null, null, null, text);
    }

    internal static string ComputeContentHash(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Returns the display name.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        if (!string.IsNullOrEmpty(Path))
        {
            return Path;
        }

        return "(anonymous)";
    }

    /// <summary>
    /// Creates a validated span over this source using a zero-based start and character count.
    /// </summary>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of characters in the range or output.</param>
    /// <returns>The result of the operation.</returns>
    internal TemplateSourceSpan CreateSpan(int start, int length)
    {
        return new TemplateSourceSpan(this, start, length);
    }

    /// <summary>
    /// Builds a line map for converting between offsets and line-column positions in this source.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal TemplateSourceLineMap CreateLineMap()
    {
        return new TemplateSourceLineMap(this);
    }

    /// <summary>
    /// Returns the position.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal TemplateSourcePosition GetPosition(int offset)
    {
        TemplateSourceLineMap lineMap = CreateLineMap();
        return lineMap.GetPosition(offset);
    }

    /// <summary>
    /// Returns the line.
    /// </summary>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal TemplateSourceLine GetLine(int lineNumber)
    {
        TemplateSourceLineMap lineMap = CreateLineMap();
        return lineMap.GetLine(lineNumber);
    }

    /// <summary>
    /// Returns the offset.
    /// </summary>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <param name="columnNumber">The one-based column number.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal int GetOffset(int lineNumber, int columnNumber)
    {
        TemplateSourceLineMap lineMap = CreateLineMap();
        return lineMap.GetOffset(lineNumber, columnNumber);
    }
}

/// <summary>
/// Identifies a zero-based range within one template source.
/// </summary>
/// <remarks>
/// The end offset is exclusive, so an empty span has equal start and end offsets.
/// </remarks>
public class TemplateSourceSpan
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource Source;
    /// <summary>
    /// Stores the zero-based start offset.
    /// </summary>
    public int Start;
    /// <summary>
    /// Stores the length in characters.
    /// </summary>
    public int Length;
    /// <summary>
    /// Stores the exclusive end offset.
    /// </summary>
    public int End;

    /// <summary>
    /// Initializes a new template source span and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of characters in the range or output.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument lies outside the supported range.</exception>
    public TemplateSourceSpan(TemplateSource source, int start, int length)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();

        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (start > source.Length)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (length > source.Length - start)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        Source = source;
        Start = start;
        Length = length;
        End = start + length;
    }

    /// <summary>
    /// Determines whether the empty condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Length == 0;
    }

    /// <summary>
    /// Determines whether the requested item exists without returning its contents.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool Contains(int offset)
    {
        return offset >= Start && offset < End;
    }

    /// <summary>
    /// Returns the text.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal string GetText()
    {
        return Source.Text.Substring(Start, Length);
    }

    /// <summary>
    /// Returns the start position.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal TemplateSourcePosition GetStartPosition()
    {
        return Source.GetPosition(Start);
    }
}

/// <summary>
/// Describes a parser, analyzer, dependency, or security issue and its optional source location and remediation hint.
/// </summary>
public class TemplateDiagnostic
{
    /// <summary>
    /// Specifies the code for this diagnostic.
    /// </summary>
    public string Code;
    /// <summary>
    /// Specifies the severity for this diagnostic.
    /// </summary>
    public TemplateDiagnosticSeverity Severity;
    /// <summary>
    /// Specifies the message for this diagnostic.
    /// </summary>
    public string Message;
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan? Span;
    /// <summary>
    /// Specifies the hint for this diagnostic.
    /// </summary>
    public string? Hint;

    /// <summary>
    /// Initializes a new template diagnostic and validates any supplied values.
    /// </summary>
    /// <param name="code">The code used by the operation.</param>
    /// <param name="severity">The severity used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="hint">The hint used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDiagnostic(string code, TemplateDiagnosticSeverity severity, string message, TemplateSourceSpan? span, string? hint)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        Code = code;
        Severity = severity;
        Message = message;
        Span = span;
        Hint = hint;
    }

    /// <summary>
    /// Creates a diagnostic without source location information.
    /// </summary>
    /// <param name="code">The code used by the operation.</param>
    /// <param name="severity">The severity used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <returns>The new diagnostic.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateDiagnostic Create(string code, TemplateDiagnosticSeverity severity, string message)
    {
        return new TemplateDiagnostic(code, severity, message, null, null);
    }

    /// <summary>
    /// Creates a diagnostic with source location information and no remediation hint.
    /// </summary>
    /// <param name="code">The code used by the operation.</param>
    /// <param name="severity">The severity used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The new diagnostic.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateDiagnostic Create(string code, TemplateDiagnosticSeverity severity, string message, TemplateSourceSpan? span)
    {
        return new TemplateDiagnostic(code, severity, message, span, null);
    }

    /// <summary>
    /// Determines whether the error condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsError()
    {
        return Severity == TemplateDiagnosticSeverity.Error;
    }

    /// <summary>
    /// Returns the severity name.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal string GetSeverityName()
    {
        if (Severity == TemplateDiagnosticSeverity.Information)
        {
            return "information";
        }

        if (Severity == TemplateDiagnosticSeverity.Warning)
        {
            return "warning";
        }

        if (Severity == TemplateDiagnosticSeverity.Error)
        {
            return "error";
        }

        return "unknown";
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string FormatSummary()
    {
        return Code + " " + GetSeverityName() + ": " + Message;
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string Format()
    {
        TemplateDiagnosticFormatter formatter = new TemplateDiagnosticFormatter();
        return formatter.Format(this);
    }
}
