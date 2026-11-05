namespace svg;

/// <summary>Contains a stylesheet or declaration-list parse result.</summary>
public class SvgCssParseResult
{
    /// <summary>Whether parsing completed without a fatal limit or argument failure.</summary>
    public bool Success;
    /// <summary>The parser entry-point mode.</summary>
    public SvgCssParseMode Mode;
    /// <summary>The fatal parser failure.</summary>
    public SvgCssParseError Error;
    /// <summary>The parsed stylesheet, or <c>null</c> for declaration modes or fatal failure.</summary>
    public SvgStyleSheet? StyleSheet;
    /// <summary>The ordered parsed declarations for declaration modes.</summary>
    public System.Collections.Generic.List<SvgCssDeclaration> Declarations;
    /// <summary>The ordered retained syntax discarded by standards recovery.</summary>
    public System.Collections.Generic.List<SvgCssUnknownSyntax> UnknownSyntax;
    /// <summary>The ordered recoverable and fatal diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The exact fatal failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The stable fatal failure message.</summary>
    public string Message;

    /// <summary>Creates a CSS parse result and copies its collections.</summary>
    /// <param name="success">Whether parsing completed.</param>
    /// <param name="mode">The entry-point mode.</param>
    /// <param name="error">The fatal failure.</param>
    /// <param name="styleSheet">The parsed stylesheet, or <c>null</c>.</param>
    /// <param name="declarations">The parsed declarations.</param>
    /// <param name="unknownSyntax">The retained recovered syntax.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    /// <param name="errorOffset">The fatal failure offset.</param>
    /// <param name="message">The fatal failure message.</param>
    public SvgCssParseResult(bool success, SvgCssParseMode mode, SvgCssParseError error, SvgStyleSheet? styleSheet, System.Collections.Generic.List<SvgCssDeclaration> declarations, System.Collections.Generic.List<SvgCssUnknownSyntax> unknownSyntax, System.Collections.Generic.List<SvgDiagnostic> diagnostics, int errorOffset, string message)
    {
        Success = success;
        Mode = mode;
        Error = error;
        StyleSheet = styleSheet;
        Declarations = new System.Collections.Generic.List<SvgCssDeclaration>(declarations);
        UnknownSyntax = new System.Collections.Generic.List<SvgCssUnknownSyntax>(unknownSyntax);
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        ErrorOffset = errorOffset;
        Message = message;
    }
}
