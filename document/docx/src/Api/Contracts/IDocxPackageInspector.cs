namespace docx;

/// <summary>
/// Provides the read-only package information required by validation and diagnostic operations.
/// </summary>
public interface IDocxPackageInspector
{
    /// <summary>
    /// Returns the package content-type registry.
    /// </summary>
    /// <returns>The package content types.</returns>
    DocxContentTypes GetContentTypes();

    /// <summary>
    /// Returns the number of package parts.
    /// </summary>
    /// <returns>The number of package parts.</returns>
    int PartCount();

    /// <summary>
    /// Returns a package part by position.
    /// </summary>
    /// <param name="index">The zero-based index to access.</param>
    /// <returns>The requested package part.</returns>
    DocxPart GetPart(int index);

    /// <summary>
    /// Returns a package part by name, or null when it is absent.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The requested package part, or null.</returns>
    DocxPart? FindPart(string partName);

    /// <summary>
    /// Indicates whether a package part exists.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the package contains the part.</returns>
    bool ContainsPart(string partName);

    /// <summary>
    /// Returns the number of relationship sets.
    /// </summary>
    /// <returns>The number of relationship sets.</returns>
    int RelationshipSetCount();

    /// <summary>
    /// Returns a relationship set by position.
    /// </summary>
    /// <param name="index">The zero-based index to access.</param>
    /// <returns>The requested relationship set.</returns>
    DocxRelationshipSet GetRelationshipSet(int index);

    /// <summary>
    /// Returns the relationship set for a source part, or null when it is absent.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <returns>The requested relationship set, or null.</returns>
    DocxRelationshipSet? FindRelationshipSet(string sourcePartName);

    /// <summary>
    /// Returns the package-level relationship set, or null when it is absent.
    /// </summary>
    /// <returns>The package-level relationship set, or null.</returns>
    DocxRelationshipSet? GetPackageRelationshipSet();

    /// <summary>
    /// Returns the styles part, or null when it is absent.
    /// </summary>
    /// <returns>The styles part, or null.</returns>
    DocxPart? GetStylesPart();

    /// <summary>
    /// Returns the numbering part, or null when it is absent.
    /// </summary>
    /// <returns>The numbering part, or null.</returns>
    DocxPart? GetNumberingPart();

    /// <summary>
    /// Returns the footnotes part, or null when it is absent.
    /// </summary>
    /// <returns>The footnotes part, or null.</returns>
    DocxPart? GetFootnotesPart();

    /// <summary>
    /// Returns the endnotes part, or null when it is absent.
    /// </summary>
    /// <returns>The endnotes part, or null.</returns>
    DocxPart? GetEndnotesPart();

    /// <summary>
    /// Returns the comments part, or null when it is absent.
    /// </summary>
    /// <returns>The comments part, or null.</returns>
    DocxPart? GetCommentsPart();
}
