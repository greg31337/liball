namespace odf;

/// <summary>
/// Describes a named enhanced-geometry equation.
/// </summary>
public class OdfGeometryEquation
{
    /// <summary>The equation name referenced with a question-mark expression.</summary>
    public string Name = "";
    /// <summary>The preserved equation formula.</summary>
    public string Formula = "";
    /// <summary>The underlying <c>draw:equation</c> element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalFormula = "";
}
