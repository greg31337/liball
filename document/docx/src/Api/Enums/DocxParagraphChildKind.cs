namespace docx;

/// <summary>
/// Identifies the kind of a direct paragraph child.
/// </summary>
public enum DocxParagraphChildKind
{
    /// <summary>
    /// A run child.
    /// </summary>
    Run = 0,
    /// <summary>
    /// A hyperlink child.
    /// </summary>
    Hyperlink = 1,
    /// <summary>
    /// A bookmark start or end marker.
    /// </summary>
    Bookmark = 2,
    /// <summary>
    /// A simple field child.
    /// </summary>
    Field = 3,
    /// <summary>
    /// A content-control child.
    /// </summary>
    ContentControl = 4,
    /// <summary>
    /// A proofing-error marker.
    /// </summary>
    ProofingMarker = 5,
    /// <summary>
    /// A permission-range marker.
    /// </summary>
    PermissionRange = 6,
    /// <summary>
    /// A smart-tag child.
    /// </summary>
    SmartTag = 7,
    /// <summary>
    /// A custom XML markup child.
    /// </summary>
    CustomXml = 8,
    /// <summary>
    /// A tracked-revision child.
    /// </summary>
    Revision = 9,
    /// <summary>
    /// A preserved child not represented by another kind.
    /// </summary>
    PreservedUnknown = 10
}
