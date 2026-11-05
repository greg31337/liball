namespace odf;

/// <summary>
/// Describes an inert parameter attached to a plugin or applet resource.
/// </summary>
public class OdfMediaParameter
{
    /// <summary>The parameter name.</summary>
    public string Name = "";
    /// <summary>The preserved parameter value.</summary>
    public string Value = "";
    /// <summary>The underlying parameter element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalValue = "";
}
