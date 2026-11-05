namespace odf;

/// <summary>
/// Selects package parts and event kinds for synchronous streaming operations.
/// </summary>
public class OdfStreamSelection
{
    /// <summary>
    /// Package part paths to include, or an empty array to include every part.
    /// </summary>
    public string[] PartPaths = [];
    /// <summary>
    /// Element names to include, or an empty array to include every element.
    /// </summary>
    public OdfQualifiedName[] ElementNames = [];
    /// <summary>
    /// Whether package-entry boundary events are included.
    /// </summary>
    public bool IncludePackageEntryEvents = true;
    /// <summary>
    /// Whether element boundary events are included.
    /// </summary>
    public bool IncludeElementEvents = true;
    /// <summary>
    /// Whether text events are included.
    /// </summary>
    public bool IncludeTextEvents = true;
    /// <summary>
    /// Whether comment events are included.
    /// </summary>
    public bool IncludeCommentEvents = true;
    /// <summary>
    /// Whether processing-instruction events are included.
    /// </summary>
    public bool IncludeProcessingInstructionEvents = true;
    /// <summary>
    /// Whether binary entry content is included rather than represented only by boundaries.
    /// </summary>
    public bool IncludeBinaryContent;
    /// <summary>
    /// Whether unselected XML subtrees should be skipped as early as possible.
    /// </summary>
    public bool SkipUnselectedSubtrees = true;
}
