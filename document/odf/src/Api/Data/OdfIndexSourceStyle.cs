namespace odf;

/// <summary>Describes paragraph styles included at one index outline level.</summary>
public class OdfIndexSourceStyle
{
    /// <summary>The included outline level.</summary>
    public string OutlineLevel = "";
    /// <summary>The included paragraph-style names.</summary>
    public string[] StyleNames = [];
    /// <summary>The underlying source-styles element.</summary>
    public OdfNode? Node;
}
