using System;
using System.Threading;

namespace html;
/// <summary>Describes one recoverable selector compilation error and its source location.</summary>
public class HtmlSelectorCompileError : IHtmlSelectorCompileError
{
    internal HtmlDiagnosticCode code;
    internal string message;
    internal HtmlSourcePosition position;
    internal HtmlSelectorCompileErrorKind kind;
    internal HtmlSelectorCompileError(HtmlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        this.code = diagnostic.GetCode();
        this.message = diagnostic.GetMessage();
        this.position = diagnostic.GetSpan().GetStart();
        this.kind = GetKind(this.code);
    }

    /// <summary>Gets the stable diagnostic code that caused selector compilation to fail.</summary>
    /// <returns>The selector syntax, unsupported-feature, or complexity diagnostic code.</returns>
    public HtmlDiagnosticCode GetCode()
    {
        return this.code;
    }

    /// <summary>Formats the error code as its stable <c>UH</c> identifier.</summary>
    /// <returns><c>UH</c> followed by the five-digit diagnostic code.</returns>
    public string GetIdentifier()
    {
        return "UH" + ((int)this.code).ToString("D5", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Gets the human-readable explanation produced by the selector compiler.</summary>
    /// <returns>Explanatory text intended for logs or display.</returns>
    public string GetMessage()
    {
        return this.message;
    }

    /// <summary>Gets where the compiler detected the selector error.</summary>
    /// <returns>The source position, or an explicitly unknown position when unavailable.</returns>
    public HtmlSourcePosition GetPosition()
    {
        return this.position;
    }

    /// <summary>Gets the broad failure category suitable for caller-facing error handling.</summary>
    /// <returns>Invalid syntax, an unsupported feature, or exceeded complexity.</returns>
    public HtmlSelectorCompileErrorKind GetKind()
    {
        return this.kind;
    }

    /// <summary>Reports whether compilation failed because the selector uses an unsupported feature.</summary>
    /// <returns><c>true</c> when an unsupported selector feature caused compilation to fail; otherwise, <c>false</c>.</returns>
    public bool IsUnsupportedFeature()
    {
        return this.kind == HtmlSelectorCompileErrorKind.UnsupportedFeature;
    }

    internal static HtmlSelectorCompileErrorKind GetKind(HtmlDiagnosticCode code)
    {
        if (code == HtmlDiagnosticCode.SelectorUnsupportedFeature)
        {
            return HtmlSelectorCompileErrorKind.UnsupportedFeature;
        }

        if (code == HtmlDiagnosticCode.SelectorComplexityExceeded)
        {
            return HtmlSelectorCompileErrorKind.ComplexityExceeded;
        }

        return HtmlSelectorCompileErrorKind.InvalidSyntax;
    }
}
