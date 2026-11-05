namespace svg;

/// <summary>Contains a shorthand expansion result.</summary>
public class SvgCssDeclarationExpansionResult
{
    /// <summary>Whether expansion succeeded.</summary>
    public bool Success;
    /// <summary>The stable expansion failure.</summary>
    public SvgCssDeclarationExpansionError Error;
    /// <summary>The ordered expanded longhands.</summary>
    public System.Collections.Generic.List<SvgCssExpandedDeclaration> Declarations;
    /// <summary>The ordered located diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates an expansion result and copies its collections.</summary>
    /// <param name="success">Whether expansion succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="declarations">The expanded longhands.</param>
    /// <param name="diagnostics">The located diagnostics.</param>
    public SvgCssDeclarationExpansionResult(bool success, SvgCssDeclarationExpansionError error, System.Collections.Generic.List<SvgCssExpandedDeclaration> declarations, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (error < SvgCssDeclarationExpansionError.None || error > SvgCssDeclarationExpansionError.UnsupportedShorthand) throw new System.ArgumentOutOfRangeException("error");
        if (declarations == null) throw new System.ArgumentNullException("declarations");
        if (diagnostics == null) throw new System.ArgumentNullException("diagnostics");
        Success = success;
        Error = error;
        Declarations = new System.Collections.Generic.List<SvgCssExpandedDeclaration>(declarations);
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
    }
}
