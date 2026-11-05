using System.IO;

namespace docx;

/// <summary>
/// Provides DOCX package validation and safe repair operations.
/// </summary>
public interface IDocxValidator
{
    /// <summary>
    /// Inspects package bytes without changing the supplied stream data.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <returns>The package diagnostics.</returns>
    DocxPackageDiagnostics GetStreamDiagnostics(Stream stream);

    /// <summary>
    /// Inspects a loaded package without changing its parts, relationships, or content types.
    /// </summary>
    /// <param name="package">The package to inspect.</param>
    /// <returns>The package diagnostics.</returns>
    DocxPackageDiagnostics GetPackageDiagnostics(DocxPackage package);

    /// <summary>
    /// Checks package and WordprocessingML consistency without changing the supplied data.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport Validate(Stream stream);

    /// <summary>
    /// Checks package and WordprocessingML consistency without changing the supplied data.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidatePackage(IDocxPackageInspector package);

    /// <summary>
    /// Checks that the package has one reachable main-document part.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateWordprocessingRequiredParts(IDocxPackageInspector package);

    /// <summary>
    /// Checks package content-type declarations.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateContentTypes(IDocxPackageInspector package);

    /// <summary>
    /// Checks the well-formedness of XML package parts.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateXmlWellFormedness(IDocxPackageInspector package);

    /// <summary>
    /// Checks supported references between WordprocessingML content and package resources.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateReferences(IDocxPackageInspector package);

    /// <summary>
    /// Checks relationship targets and target modes.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateRelationshipTargets(IDocxPackageInspector package);

    /// <summary>
    /// Checks recognized WordprocessingML part roots, required body structure, and required attributes.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateWordprocessingStructure(IDocxPackageInspector package);

    /// <summary>
    /// Checks unique identifiers, numbering-definition links, and bookmark and comment ranges.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateIdentifiersAndRanges(IDocxPackageInspector package);

    /// <summary>
    /// Checks compatibility-mode settings supported by WordprocessingML consumers.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateCompatibility(IDocxPackageInspector package);

    /// <summary>
    /// Checks that Strict and Transitional namespace families are not mixed.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateNamespaceConformance(IDocxPackageInspector package);

    /// <summary>
    /// Checks macro-enabled main-document, VBA part, and relationship consistency.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport ValidateMacroPackage(IDocxPackageInspector package);

    /// <summary>
    /// Detects the namespace conformance used by a package.
    /// </summary>
    /// <param name="package">The package information to inspect.</param>
    /// <returns>The resulting namespace conformance.</returns>
    DocxNamespaceConformance DetectNamespaceConformance(IDocxPackageInspector package);

    /// <summary>
    /// Applies all unambiguous package repairs and returns one ordered diagnostic report.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting repair report.</returns>
    DocxValidationReport RepairRecoverableDefects(DocxPackage package);

    /// <summary>
    /// Assigns unique identifiers to relationships whose identifiers are missing.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting repair report.</returns>
    DocxValidationReport RepairMissingRelationshipIds(DocxPackage package);

    /// <summary>
    /// Repairs missing content-type declarations.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport RepairMissingContentTypes(DocxPackage package);

    /// <summary>
    /// Repairs missing relationship declarations.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport RepairMissingRelationships(DocxPackage package);

    /// <summary>
    /// Removes orphaned relationship declarations.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport RepairOrphanedRelationships(DocxPackage package);

    /// <summary>
    /// Repairs an empty main-document body.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting validation report.</returns>
    DocxValidationReport RepairEmptyBody(DocxPackage package);
}
