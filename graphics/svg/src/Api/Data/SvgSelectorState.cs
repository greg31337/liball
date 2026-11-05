namespace svg;

/// <summary>Reports one dynamic pseudo-class match and any safe non-matching diagnostics.</summary>
public class SvgDynamicSelectorMatchResult
{
    /// <summary>Whether the node matches the dynamic pseudo-class.</summary>
    public bool Matches;
    /// <summary>Whether the dynamic pseudo-class is supported by the matcher.</summary>
    public bool Supported;
    /// <summary>Diagnostics produced while evaluating the pseudo-class.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgDynamicSelectorMatchResult(bool matches, bool supported, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        Matches = matches;
        Supported = supported;
        Diagnostics = diagnostics;
    }
}
