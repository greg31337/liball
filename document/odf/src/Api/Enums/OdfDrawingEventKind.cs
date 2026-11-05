namespace odf;

/// <summary>
/// Identifies a drawing event-listener vocabulary.
/// </summary>
public enum OdfDrawingEventKind
{
    /// <summary>The listener kind is unrecognized.</summary>
    Unknown = 0,
    /// <summary>An ODF script event listener.</summary>
    Script = 1,
    /// <summary>An ODF presentation event listener.</summary>
    Presentation = 2
}
