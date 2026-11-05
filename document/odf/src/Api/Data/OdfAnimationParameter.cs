namespace odf;

/// <summary>Describes a parameter supplied to an animation command.</summary>
public class OdfAnimationParameter
{
    /// <summary>The parameter name.</summary>
    public string Name = "";
    /// <summary>The parameter value.</summary>
    public string Value = "";
    /// <summary>The underlying parameter element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalValue = "";
}
