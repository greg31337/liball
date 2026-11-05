namespace docx;

/// <summary>
/// Provides stable codes for package inspection diagnostics.
/// </summary>
public static class DocxDiagnosticCodes
{
    /// <summary>
    /// Identifies input that cannot be completely read as an OPC package.
    /// </summary>
    public const string CorruptInput = "opc.corrupt-input";

    /// <summary>
    /// Identifies a recognized package part whose XML root is invalid.
    /// </summary>
    public const string InvalidPartRoot = "wml.invalid-part-root";

    /// <summary>
    /// Identifies a main document without exactly one body element.
    /// </summary>
    public const string InvalidDocumentBody = "wml.invalid-document-body";

    /// <summary>
    /// Identifies required WordprocessingML attribute data that is missing.
    /// </summary>
    public const string MissingRequiredAttribute = "wml.missing-required-attribute";

    /// <summary>
    /// Identifies a duplicate style identifier.
    /// </summary>
    public const string DuplicateStyleId = "ids.duplicate-style";

    /// <summary>
    /// Identifies a duplicate numbering identifier.
    /// </summary>
    public const string DuplicateNumberingId = "ids.duplicate-numbering";

    /// <summary>
    /// Identifies a duplicate footnote or endnote identifier.
    /// </summary>
    public const string DuplicateNoteId = "ids.duplicate-note";

    /// <summary>
    /// Identifies a duplicate comment identifier.
    /// </summary>
    public const string DuplicateCommentId = "ids.duplicate-comment";

    /// <summary>
    /// Identifies a duplicate bookmark identifier within one story.
    /// </summary>
    public const string DuplicateBookmarkId = "ids.duplicate-bookmark";

    /// <summary>
    /// Identifies a numbering instance that refers to a missing abstract definition.
    /// </summary>
    public const string MissingAbstractNumberingReference = "refs.missing-abstract-numbering";

    /// <summary>
    /// Identifies a comment range start without a matching range end.
    /// </summary>
    public const string UnmatchedCommentRangeStart = "refs.unmatched-comment-range-start";

    /// <summary>
    /// Identifies a comment range end without a matching range start.
    /// </summary>
    public const string UnmatchedCommentRangeEnd = "refs.unmatched-comment-range-end";

    /// <summary>
    /// Identifies an unsupported or malformed compatibility mode setting.
    /// </summary>
    public const string InvalidCompatibilityMode = "compat.invalid-mode";

    /// <summary>
    /// Identifies a package that mixes Strict and Transitional namespaces.
    /// </summary>
    public const string MixedNamespaceConformance = "namespace.mixed-conformance";

    /// <summary>
    /// Identifies macro content paired with a non-macro main-document content type.
    /// </summary>
    public const string MacroMainDocumentRequired = "macro.main-document-content-type-required";

    /// <summary>
    /// Identifies a macro-enabled main document without a VBA project part.
    /// </summary>
    public const string MissingVbaProjectPart = "macro.missing-vba-project-part";

    /// <summary>
    /// Identifies a VBA project part without its package relationship.
    /// </summary>
    public const string MissingVbaProjectRelationship = "macro.missing-vba-project-relationship";

    /// <summary>
    /// Identifies a repaired content-type default declaration.
    /// </summary>
    public const string RepairedContentTypeDefault = "repair.content-type.default";

    /// <summary>
    /// Identifies repaired content-type metadata for a package part.
    /// </summary>
    public const string RepairedPartContentType = "repair.content-type.part";

    /// <summary>
    /// Identifies a relationship set created during repair.
    /// </summary>
    public const string RepairedRelationshipSet = "repair.relationship-set";

    /// <summary>
    /// Identifies a relationship created during repair.
    /// </summary>
    public const string RepairedRelationship = "repair.relationship";

    /// <summary>
    /// Identifies a relationship identifier assigned during repair.
    /// </summary>
    public const string RepairedRelationshipId = "repair.relationship-id";

    /// <summary>
    /// Identifies a relationship part created or synchronized during repair.
    /// </summary>
    public const string RepairedRelationshipPart = "repair.relationship-part";

    /// <summary>
    /// Identifies an orphaned relationship removed during repair.
    /// </summary>
    public const string RepairedOrphanedRelationship = "repair.relationship.orphaned";

    /// <summary>
    /// Identifies an orphaned relationship set removed during repair.
    /// </summary>
    public const string RepairedOrphanedRelationshipSet = "repair.relationship-set.orphaned";

    /// <summary>
    /// Identifies a missing main-document part added during repair.
    /// </summary>
    public const string RepairedMainDocumentPart = "repair.body.main-document";

    /// <summary>
    /// Identifies an empty or missing main-document body repaired with minimal content.
    /// </summary>
    public const string RepairedBody = "repair.body";
}
