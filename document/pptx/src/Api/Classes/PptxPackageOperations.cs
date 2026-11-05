using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxPackage values.
/// </summary>
public class PptxPackageOperations : IPptxPackageOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxPackageOperations()
    {
    }

    /// <summary>
    /// Creates an empty package with the standard XML and relationships content-type defaults.
    /// </summary>
    /// <remarks>The package does not contain a presentation part until the caller adds one. Use <see cref="PptxDocument.CreatePresentation"/> for a valid starter deck.</remarks>
    /// <returns>The resulting package value.</returns>
    public PptxPackage Create()
    {
        return PptxPackage.Create();
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a file.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenFile(string path)
    {
        return PptxPackage.OpenFile(path, PptxZipReadOptions.CreateDefault());
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a file using configurable ZIP safety limits.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenFileWithOptions(string path, PptxZipReadOptions options)
    {
        return PptxPackage.OpenFile(path, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage Open(Stream stream)
    {
        return PptxPackage.Open(stream);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a stream using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenWithOptions(Stream stream, PptxZipReadOptions options)
    {
        return PptxPackage.Open(stream, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenBytes(byte[] bytes)
    {
        return PptxPackage.OpenBytes(bytes);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenBytesWithOptions(byte[] bytes, PptxZipReadOptions options)
    {
        return PptxPackage.OpenBytes(bytes, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenSpan(System.ReadOnlySpan<byte> bytes)
    {
        return PptxPackage.OpenSpan(bytes, PptxZipReadOptions.CreateDefault());
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting package value.</returns>
    public PptxPackage OpenSpanWithOptions(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options)
    {
        return PptxPackage.OpenSpan(bytes, options);
    }

    /// <summary>
    /// Serializes the current package and writes it to the requested destination.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    public byte[] SaveBytes(PptxPackage target)
    {
        return RequirePackage(target).SaveBytes();
    }

    /// <summary>
    /// Serializes the current package and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Save(PptxPackage target, Stream stream)
    {
        RequirePackage(target).Save(stream);
    }

    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Succeeded(PptxPackage target)
    {
        return RequirePackage(target).Succeeded();
    }

    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Failed(PptxPackage target)
    {
        return RequirePackage(target).Failed();
    }

    /// <summary>
    /// Validates the package and returns all diagnostics found during the check.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxResult Validate(PptxPackage target)
    {
        return RequirePackage(target).Validate();
    }

    /// <summary>
    /// Repairs recoverable package issues and returns diagnostics describing the changes.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxResult Repair(PptxPackage target)
    {
        return RequirePackage(target).Repair();
    }

    /// <summary>
    /// Removes redundant or unreachable package data and reports what changed.
    /// </summary>
    /// <returns>The resulting package cleanup result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPackageCleanupResult Cleanup(PptxPackage target)
    {
        return RequirePackage(target).Cleanup();
    }

    /// <summary>
    /// Normalizes package ordering and retained serialized entries without removing package content.
    /// </summary>
    /// <param name="target">The package to normalize.</param>
    /// <returns>Diagnostics describing whether normalization changed the package.</returns>
    public PptxResult Normalize(PptxPackage target)
    {
        return RequirePackage(target).Normalize();
    }

    /// <summary>
    /// Creates a fully detached deep copy of the package and its mutable parts, relationships, diagnostics, and retained entries.
    /// </summary>
    /// <param name="target">The package to clone.</param>
    /// <returns>The detached package copy.</returns>
    public PptxPackage ClonePackage(PptxPackage target)
    {
        return PptxDocument.ClonePackage(RequirePackage(target));
    }

    /// <summary>
    /// Inspects the effective serialized package entries without changing the supplied package.
    /// </summary>
    /// <param name="target">The package to inspect.</param>
    /// <returns>The package manifest.</returns>
    public PptxPackageManifest InspectManifest(PptxPackage target)
    {
        return PptxPackageInspectionCore.InspectManifest(RequirePackage(target));
    }

    /// <summary>
    /// Classifies package features by editing, reading, preservation, support, and safety level.
    /// </summary>
    /// <param name="target">The package to inspect.</param>
    /// <returns>The grouped compatibility inspection.</returns>
    public PptxCompatibilityInspection InspectCompatibility(PptxPackage target)
    {
        return PptxCompatibilityInspectorCore.Inspect(RequirePackage(target));
    }

    /// <summary>
    /// Compares effective serialized package entry names, metadata, and bytes.
    /// </summary>
    /// <param name="left">The left package to compare.</param>
    /// <param name="right">The right package to compare.</param>
    /// <returns>The package manifest comparison.</returns>
    public PptxPackageManifestComparison CompareManifests(PptxPackage left, PptxPackage right)
    {
        return PptxPackageInspectionCore.CompareManifests(RequirePackage(left), RequirePackage(right));
    }

    /// <summary>
    /// Builds aggregate counts for content represented by the editable presentation model.
    /// </summary>
    /// <param name="target">The package to summarize.</param>
    /// <returns>The editable-model summary.</returns>
    public PptxEditableModelSummary SummarizeEditableModel(PptxPackage target)
    {
        return PptxPackageInspectionCore.SummarizeEditableModel(RequirePackage(target));
    }

    /// <summary>
    /// Compares aggregate editable-model summaries for two packages.
    /// </summary>
    /// <param name="left">The left package to compare.</param>
    /// <param name="right">The right package to compare.</param>
    /// <returns>The editable-model comparison.</returns>
    public PptxEditableModelComparison CompareEditableModels(PptxPackage left, PptxPackage right)
    {
        return PptxPackageInspectionCore.CompareEditableModels(RequirePackage(left), RequirePackage(right));
    }

    /// <summary>
    /// Returns a JSON summary of package parts, relationships, and diagnostics.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ToSummaryJson(PptxPackage target)
    {
        return RequirePackage(target).ToSummaryJson();
    }

    /// <summary>
    /// Scans the package for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxResult ScanSecurity(PptxPackage target)
    {
        return RequirePackage(target).ScanSecurity();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int PartCount(PptxPackage target)
    {
        return RequirePackage(target).PartCount();
    }

    /// <summary>
    /// Returns the number of original ZIP entries retained by the package.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int RawEntryCount(PptxPackage target)
    {
        return RequirePackage(target).RawEntryCount();
    }

    /// <summary>
    /// Reports whether the package contains or enables part.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasPart(PptxPackage target, string partName)
    {
        return RequirePackage(target).HasPart(partName);
    }

    /// <summary>
    /// Returns the zero-based index of a normalized part name, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int IndexOfPart(PptxPackage target, string partName)
    {
        return RequirePackage(target).IndexOfPart(partName);
    }

    /// <summary>
    /// Returns the part without changing the package.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart GetPart(PptxPackage target, string partName)
    {
        return RequirePackage(target).GetPart(partName);
    }

    /// <summary>
    /// Adds the part and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart AddPart(PptxPackage target, string partName, string contentType, byte[] bytes)
    {
        return RequirePackage(target).AddPart(partName, contentType, bytes);
    }

    /// <summary>
    /// Replaces part while preserving unrelated presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart ReplacePart(PptxPackage target, string partName, string contentType, byte[] bytes)
    {
        return RequirePackage(target).ReplacePart(partName, contentType, bytes);
    }

    /// <summary>
    /// Removes the part and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemovePart(PptxPackage target, string partName)
    {
        RequirePackage(target).RemovePart(partName);
    }

    /// <summary>
    /// Renames a package part and updates content types and internal relationships that target it.
    /// </summary>
    /// <param name="oldPartName">The old part name value to use.</param>
    /// <param name="newPartName">The new part name value to use.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart RenamePart(PptxPackage target, string oldPartName, string newPartName)
    {
        return RequirePackage(target).RenamePart(oldPartName, newPartName);
    }

    /// <summary>
    /// Returns the relationships without changing the package.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting relationship collection value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRelationshipCollection GetRelationships(PptxPackage target, string partName)
    {
        return RequirePackage(target).GetRelationships(partName);
    }

    /// <summary>
    /// Adds the relationship and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <param name="targetMode">Whether the relationship target is internal or external.</param>
    /// <returns>The resulting relationship value.</returns>
    /// <param name="package">The package value to operate on.</param>
    public PptxRelationship AddRelationship(PptxPackage package, string partName, string type, string target, PptxRelationshipTargetMode targetMode)
    {
        return RequirePackage(package).AddRelationship(partName, type, target, targetMode);
    }

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
    public PptxRelationship AddRelationshipWithId(PptxPackage package, string partName, string id, string type, string target, PptxRelationshipTargetMode targetMode)
    {
        return RequirePackage(package).AddRelationshipWithId(partName, id, type, target, targetMode);
    }

    /// <summary>
    /// Removes the relationship and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="id">The id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveRelationship(PptxPackage target, string partName, string id)
    {
        RequirePackage(target).RemoveRelationship(partName, id);
    }

    /// <summary>
    /// Updates the relationship target and the underlying presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="id">The id value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <param name="targetMode">Whether the relationship target is internal or external.</param>
    /// <returns>The resulting relationship value.</returns>
    /// <param name="package">The package value to operate on.</param>
    public PptxRelationship SetRelationshipTarget(PptxPackage package, string partName, string id, string target, PptxRelationshipTargetMode targetMode)
    {
        return RequirePackage(package).SetRelationshipTarget(partName, id, target, targetMode);
    }

    /// <summary>
    /// Removes the content type override and updates dependent presentation data.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveContentTypeOverride(PptxPackage target, string partName)
    {
        RequirePackage(target).RemoveContentTypeOverride(partName);
    }

    internal static PptxPackage RequirePackage(PptxPackage target)
    {
        if (target == null)
        {
            throw new System.ArgumentNullException("target");
        }

        return target;
    }
}
