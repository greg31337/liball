using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxPackageOperations"/>.
/// </summary>
public interface IPptxPackageOperations
{
    /// <summary>
    /// Creates an empty package with the standard XML and relationships content-type defaults.
    /// </summary>
    /// <remarks>The package does not contain a presentation part until the caller adds one. Use <see cref="PptxDocument.CreatePresentation"/> for a valid starter deck.</remarks>
    /// <returns>The resulting package value.</returns>
    PptxPackage Create();
    /// <summary>
    /// Opens and parses a PowerPoint package from a file.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenFile(string path);
    /// <summary>
    /// Opens and parses a PowerPoint package from a file using configurable ZIP safety limits.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenFileWithOptions(string path, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage Open(Stream stream);
    /// <summary>
    /// Opens and parses a PowerPoint package from a stream using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenWithOptions(Stream stream, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenBytes(byte[] bytes);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenBytesWithOptions(byte[] bytes, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenSpan(System.ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    PptxPackage OpenSpanWithOptions(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options);
    /// <summary>
    /// Serializes the current package and writes it to the requested destination.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    byte[] SaveBytes(PptxPackage target);
    /// <summary>
    /// Serializes the current package and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    /// <param name="target">The data value to operate on.</param>
    void Save(PptxPackage target, Stream stream);
    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Succeeded(PptxPackage target);
    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Failed(PptxPackage target);
    /// <summary>
    /// Validates the package and returns all diagnostics found during the check.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxResult Validate(PptxPackage target);
    /// <summary>
    /// Repairs recoverable package issues and returns diagnostics describing the changes.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxResult Repair(PptxPackage target);
    /// <summary>
    /// Removes redundant or unreachable package data and reports what changed.
    /// </summary>
    /// <returns>The resulting package cleanup result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPackageCleanupResult Cleanup(PptxPackage target);
    /// <summary>
    /// Normalizes package ordering and retained serialized entries without removing package content.
    /// </summary>
    /// <param name="target">The package to normalize.</param>
    /// <returns>Diagnostics describing whether normalization changed the package.</returns>
    PptxResult Normalize(PptxPackage target);
    /// <summary>
    /// Creates a fully detached deep copy of the package and its mutable parts, relationships, diagnostics, and retained entries.
    /// </summary>
    /// <param name="target">The package to clone.</param>
    /// <returns>The detached package copy.</returns>
    PptxPackage ClonePackage(PptxPackage target);
    /// <summary>
    /// Inspects the effective serialized package entries without changing the supplied package.
    /// </summary>
    /// <param name="target">The package to inspect.</param>
    /// <returns>The package manifest.</returns>
    PptxPackageManifest InspectManifest(PptxPackage target);
    /// <summary>
    /// Classifies package features by editing, reading, preservation, support, and safety level.
    /// </summary>
    /// <param name="target">The package to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    PptxCompatibilityInspection InspectCompatibility(PptxPackage target);
    /// <summary>
    /// Compares effective serialized package entry names, metadata, and bytes.
    /// </summary>
    /// <param name="left">The left package to compare.</param>
    /// <param name="right">The right package to compare.</param>
    /// <returns>The package manifest comparison.</returns>
    PptxPackageManifestComparison CompareManifests(PptxPackage left, PptxPackage right);
    /// <summary>
    /// Builds aggregate counts for content represented by the editable presentation model.
    /// </summary>
    /// <param name="target">The package to summarize.</param>
    /// <returns>The editable-model summary.</returns>
    PptxEditableModelSummary SummarizeEditableModel(PptxPackage target);
    /// <summary>
    /// Compares aggregate editable-model summaries for two packages.
    /// </summary>
    /// <param name="left">The left package to compare.</param>
    /// <param name="right">The right package to compare.</param>
    /// <returns>The editable-model comparison.</returns>
    PptxEditableModelComparison CompareEditableModels(PptxPackage left, PptxPackage right);
    /// <summary>
    /// Returns a JSON summary of package parts, relationships, and diagnostics.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ToSummaryJson(PptxPackage target);
    /// <summary>
    /// Scans the package for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxResult ScanSecurity(PptxPackage target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int PartCount(PptxPackage target);
    /// <summary>
    /// Returns the number of original ZIP entries retained by the package.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int RawEntryCount(PptxPackage target);
    /// <summary>
    /// Reports whether the package contains or enables part.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasPart(PptxPackage target, string partName);
    /// <summary>
    /// Returns the zero-based index of a normalized part name, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    int IndexOfPart(PptxPackage target, string partName);
    /// <summary>
    /// Returns the part without changing the package.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart GetPart(PptxPackage target, string partName);
    /// <summary>
    /// Adds the part and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart AddPart(PptxPackage target, string partName, string contentType, byte[] bytes);
    /// <summary>
    /// Replaces part while preserving unrelated presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart ReplacePart(PptxPackage target, string partName, string contentType, byte[] bytes);
    /// <summary>
    /// Removes the part and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemovePart(PptxPackage target, string partName);
    /// <summary>
    /// Renames a package part and updates content types and internal relationships that target it.
    /// </summary>
    /// <param name="oldPartName">The old part name value to use.</param>
    /// <param name="newPartName">The new part name value to use.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart RenamePart(PptxPackage target, string oldPartName, string newPartName);
    /// <summary>
    /// Returns the relationships without changing the package.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting relationship collection value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRelationshipCollection GetRelationships(PptxPackage target, string partName);
    /// <summary>
    /// Adds the relationship and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <param name="targetMode">Whether the relationship target is internal or external.</param>
    /// <returns>The resulting relationship value.</returns>
    /// <param name="package">The package value to operate on.</param>
    PptxRelationship AddRelationship(PptxPackage package, string partName, string type, string target, PptxRelationshipTargetMode targetMode);
    /// <summary>
    /// Adds the relationship with id and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="id">The id value to use.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <param name="targetMode">Whether the relationship target is internal or external.</param>
    /// <returns>The resulting relationship value.</returns>
    /// <param name="package">The package value to operate on.</param>
    PptxRelationship AddRelationshipWithId(PptxPackage package, string partName, string id, string type, string target, PptxRelationshipTargetMode targetMode);
    /// <summary>
    /// Removes the relationship and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="id">The id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveRelationship(PptxPackage target, string partName, string id);
    /// <summary>
    /// Updates the relationship target and the underlying presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="id">The id value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <param name="targetMode">Whether the relationship target is internal or external.</param>
    /// <returns>The resulting relationship value.</returns>
    /// <param name="package">The package value to operate on.</param>
    PptxRelationship SetRelationshipTarget(PptxPackage package, string partName, string id, string target, PptxRelationshipTargetMode targetMode);
    /// <summary>
    /// Removes the content type override and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveContentTypeOverride(PptxPackage target, string partName);
}
