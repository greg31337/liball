namespace odf;

/// <summary>
/// Identifies an OpenDocument event-listener vocabulary.
/// </summary>
public enum OdfEventListenerKind
{
    /// <summary>The listener kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>A script event listener.</summary>
    Script = 1,
    /// <summary>A presentation event listener.</summary>
    Presentation = 2
}
