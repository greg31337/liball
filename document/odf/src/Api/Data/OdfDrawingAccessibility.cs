namespace odf;

/// <summary>
/// Describes the accessible text attached to a drawing object.
/// </summary>
public class OdfDrawingAccessibility
{
    /// <summary>The accessible title text.</summary>
    public string Title = "";
    /// <summary>The accessible description text.</summary>
    public string Description = "";
    /// <summary>The underlying <c>svg:title</c> element.</summary>
    public OdfNode? TitleNode;
    /// <summary>The underlying <c>svg:desc</c> element.</summary>
    public OdfNode? DescriptionNode;
    internal string OriginalTitle = "";
    internal string OriginalDescription = "";
}
