namespace odf;

/// <summary>
/// Describes an inert script or presentation event listener.
/// </summary>
public class OdfDrawingEvent
{
    /// <summary>The listener kind.</summary>
    public OdfDrawingEventKind Kind;
    /// <summary>The event name.</summary>
    public string EventName = "";
    /// <summary>The script language.</summary>
    public string Language = "";
    /// <summary>The script macro name.</summary>
    public string MacroName = "";
    /// <summary>The linked script or presentation target.</summary>
    public string Href = "";
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
    /// <summary>The link show behavior.</summary>
    public string Show = "";
    /// <summary>The link activation behavior.</summary>
    public string Actuate = "";
    /// <summary>The referenced sound, when present.</summary>
    public string SoundHref = "";
    /// <summary>The sound XLink type.</summary>
    public string SoundType = "";
    /// <summary>The sound XLink show behavior.</summary>
    public string SoundShow = "";
    /// <summary>The sound activation behavior.</summary>
    public string SoundActuate = "";
    /// <summary>The preserved play-full token.</summary>
    public string SoundPlayFullValue = "";
    /// <summary>Whether the entire event sound should play.</summary>
    public bool SoundPlayFull;
    /// <summary>Whether the play-full token is absent or valid.</summary>
    public bool IsSoundPlayFullValid;
    /// <summary>Whether the sound reference is absent or a valid IRI reference.</summary>
    public bool IsSoundReferenceValid;
    /// <summary>The resolved package path for a package-relative sound.</summary>
    public string SoundPackagePath = "";
    /// <summary>Whether the referenced package sound exists.</summary>
    public bool SoundPackageEntryExists;
    /// <summary>The underlying sound element.</summary>
    public OdfNode? SoundNode;
    /// <summary>The underlying listener element.</summary>
    public OdfNode? Node;
    internal string OriginalSoundPlayFullValue = "";
}
