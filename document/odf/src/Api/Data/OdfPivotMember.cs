namespace odf;

/// <summary>Describes one member in a pivot level.</summary>
public class OdfPivotMember
{
    /// <summary>The source member name.</summary>
    public string Name = "";
    /// <summary>Whether the member is displayed.</summary>
    public bool Display = true;
    /// <summary>Whether details for the member are shown.</summary>
    public bool ShowDetails = true;
    /// <summary>The underlying data-pilot-member element.</summary>
    public OdfNode? Node;
}
