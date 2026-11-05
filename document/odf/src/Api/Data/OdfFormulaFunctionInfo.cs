namespace odf;

/// <summary>
/// Describes the standardized metadata of an OpenFormula function.
/// </summary>
public class OdfFormulaFunctionInfo
{
    /// <summary>The canonical function name.</summary>
    public string Name = "";
    /// <summary>The function group name.</summary>
    public string Group = "";
    /// <summary>The minimum accepted argument count.</summary>
    public int MinimumArguments;
    /// <summary>The maximum accepted argument count.</summary>
    public int MaximumArguments;
    /// <summary>The required OpenFormula conformance level.</summary>
    public OdfFormulaConformance Conformance;
    /// <summary>The OpenDocument version that introduced the function.</summary>
    public OdfVersion IntroducedVersion;
    /// <summary>Whether the function requires caller-supplied external access.</summary>
    public bool RequiresExternalAccess;
    /// <summary>Whether recalculation can change without argument changes.</summary>
    public bool IsVolatile;
}
