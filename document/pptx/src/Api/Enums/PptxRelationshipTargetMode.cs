namespace pptx;

/// <summary>
/// Specifies whether a package relationship targets another package part or an external resource.
/// </summary>
public enum PptxRelationshipTargetMode
{
    /// <summary>
    /// Targets another part stored in the same package.
    /// </summary>
    Internal = 0,
    /// <summary>
    /// Targets a resource outside the package, such as a web address or linked file.
    /// </summary>
    External = 1
}

