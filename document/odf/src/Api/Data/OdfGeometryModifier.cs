namespace odf;

/// <summary>
/// Describes one positional enhanced-geometry modifier expression.
/// </summary>
public class OdfGeometryModifier
{
    /// <summary>The zero-based modifier index.</summary>
    public int Index;
    /// <summary>The preserved modifier expression.</summary>
    public string Value = "";
    internal string OriginalValue = "";
}
