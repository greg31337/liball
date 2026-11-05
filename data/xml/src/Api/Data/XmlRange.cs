namespace xml;

/// <summary>
/// Carries boundary points within an XML tree.
/// </summary>
public class XmlRange
{
    /// <summary>
    /// The document containing the range.
    /// </summary>
    public XmlDocument? Document;
    /// <summary>
    /// The start boundary container.
    /// </summary>
    public XmlNode? StartContainer;
    /// <summary>
    /// The start boundary offset.
    /// </summary>
    public int StartOffset;
    /// <summary>
    /// The end boundary container.
    /// </summary>
    public XmlNode? EndContainer;
    /// <summary>
    /// The end boundary offset.
    /// </summary>
    public int EndOffset;
    /// <summary>
    /// The optional frozen node selection for the range.
    /// </summary>
    public XmlNodeSelection? Snapshot;
    /// <summary>
    /// The document mutation version at range creation.
    /// </summary>
    public long MutationVersion;
    /// <summary>
    /// Whether the range remains valid.
    /// </summary>
    public bool IsValid;
}

