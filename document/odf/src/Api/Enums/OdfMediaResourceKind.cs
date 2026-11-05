namespace odf;

/// <summary>
/// Identifies a passive description of a media or active-content resource.
/// </summary>
public enum OdfMediaResourceKind
{
    /// <summary>The resource kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>An audio resource represented by a drawing plugin.</summary>
    Audio = 1,
    /// <summary>A video resource represented by a drawing plugin.</summary>
    Video = 2,
    /// <summary>A drawing plugin with an application-specific media type.</summary>
    Plugin = 3,
    /// <summary>A legacy Java applet description.</summary>
    Applet = 4,
    /// <summary>A floating-frame description.</summary>
    FloatingFrame = 5
}
