namespace odf;

/// <summary>
/// Describes an inert script or presentation event listener.
/// </summary>
public class OdfEventListener
{
    /// <summary>The event-listener vocabulary.</summary>
    public OdfEventListenerKind Kind;
    /// <summary>The event name.</summary>
    public string EventName = "";
    /// <summary>The script language.</summary>
    public string Language = "";
    /// <summary>The application macro name.</summary>
    public string MacroName = "";
    /// <summary>The linked script or action target.</summary>
    public string Href = "";
    /// <summary>The XLink type token.</summary>
    public string LinkType = "";
    /// <summary>The XLink show behavior.</summary>
    public string Show = "";
    /// <summary>The XLink activation behavior.</summary>
    public string Actuate = "";
    /// <summary>The presentation action.</summary>
    public string Action = "";
    /// <summary>The presentation effect.</summary>
    public string Effect = "";
    /// <summary>The presentation effect direction.</summary>
    public string Direction = "";
    /// <summary>The presentation effect speed.</summary>
    public string Speed = "";
    /// <summary>The presentation start scale.</summary>
    public string StartScale = "";
    /// <summary>The presentation verb.</summary>
    public string Verb = "";
    /// <summary>Whether the linked target is external to the package.</summary>
    public bool IsExternal;
    /// <summary>The active-content security classification.</summary>
    public OdfActiveContentClassification Classification;
    /// <summary>Whether this listener can activate content or an action.</summary>
    public bool IsActiveContent;
    /// <summary>All qualified listener attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>All listener child nodes, including presentation sound.</summary>
    public OdfNode[] ChildNodes = [];
    /// <summary>The element that owns the office:event-listeners container.</summary>
    public OdfNode? OwnerNode;
    /// <summary>The containing office:event-listeners element.</summary>
    public OdfNode? ContainerNode;
    /// <summary>The XML part containing the listener.</summary>
    public string PartPath = "";
    /// <summary>The underlying listener element.</summary>
    public OdfNode? Node;
    internal string OriginalEventName = "";
    internal string OriginalLanguage = "";
    internal string OriginalMacroName = "";
    internal string OriginalHref = "";
    internal string OriginalLinkType = "";
    internal string OriginalShow = "";
    internal string OriginalActuate = "";
    internal string OriginalAction = "";
    internal string OriginalEffect = "";
    internal string OriginalDirection = "";
    internal string OriginalSpeed = "";
    internal string OriginalStartScale = "";
    internal string OriginalVerb = "";
}
