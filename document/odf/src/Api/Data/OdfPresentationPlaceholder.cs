namespace odf;

/// <summary>Describes a presentation placeholder or placeholder assignment.</summary>
public class OdfPresentationPlaceholder
{
    /// <summary>The placeholder object or presentation class token.</summary>
    public string ObjectName = "";
    /// <summary>The preserved placeholder flag.</summary>
    public string PlaceholderValue = "";
    /// <summary>Whether the element is a placeholder.</summary>
    public bool IsPlaceholder;
    /// <summary>Whether the placeholder flag is absent or valid.</summary>
    public bool IsPlaceholderValid;
    /// <summary>The preserved user-transformed flag.</summary>
    public string UserTransformedValue = "";
    /// <summary>Whether the placeholder has been transformed by a user.</summary>
    public bool IsUserTransformed;
    /// <summary>Whether the user-transformed flag is absent or valid.</summary>
    public bool IsUserTransformedValid;
    /// <summary>The horizontal position.</summary>
    public string X = "";
    /// <summary>The vertical position.</summary>
    public string Y = "";
    /// <summary>The placeholder width.</summary>
    public string Width = "";
    /// <summary>The placeholder height.</summary>
    public string Height = "";
    /// <summary>The assigned drawing object, when this placeholder belongs to drawing content.</summary>
    public OdfDrawingObject? Drawing;
    /// <summary>The presentation layout containing this placeholder.</summary>
    public OdfStyleResource? Layout;
    /// <summary>The matching placeholder in the assigned presentation layout.</summary>
    public OdfPresentationPlaceholder? LayoutPlaceholder;
    /// <summary>The underlying placeholder or drawing element.</summary>
    public OdfNode? Node;
    internal string OriginalObjectName = "";
    internal string OriginalPlaceholderValue = "";
    internal string OriginalUserTransformedValue = "";
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalWidth = "";
    internal string OriginalHeight = "";
}
