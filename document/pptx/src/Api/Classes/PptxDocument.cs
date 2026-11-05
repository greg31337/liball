using System.IO;
using System.Xml;

namespace pptx;

/// <summary>
/// Provides the main entry point for creating, opening, validating, cloning, and saving PowerPoint presentations.
/// </summary>
public class PptxDocument : IPptxPresentationSource, IPptxDocument
{
    internal PptxPackage Package;

    internal PptxDocument(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        Package = package;
    }

    internal static PptxDocument Create()
    {
        return new PptxDocument(PptxPackage.Create());
    }

    internal static PptxDocument CreateMinimumPresentation()
    {
        return PptxMinimumPresentation.CreateDocument();
    }

    internal static PptxDocument CreatePresentation()
    {
        return CreateMinimumPresentation();
    }

    internal static PptxDocument OpenFile(string path)
    {
        return OpenFileWithOptions(path, PptxZipReadOptions.CreateDefault());
    }

    internal static PptxDocument OpenFileWithOptions(string path, PptxZipReadOptions options)
    {
        return new PptxDocument(PptxPackage.OpenFile(path, options));
    }

    internal static PptxDocument OpenStream(Stream stream)
    {
        return new PptxDocument(PptxPackage.Open(stream));
    }

    internal static PptxDocument OpenStreamWithOptions(Stream stream, PptxZipReadOptions options)
    {
        return new PptxDocument(PptxPackage.Open(stream, options));
    }

    internal static PptxDocument Open(Stream stream)
    {
        return OpenStream(stream);
    }

    internal static PptxDocument OpenBytes(byte[] bytes)
    {
        return new PptxDocument(PptxPackage.OpenBytes(bytes));
    }

    internal static PptxDocument OpenBytesWithOptions(byte[] bytes, PptxZipReadOptions options)
    {
        return new PptxDocument(PptxPackage.OpenBytes(bytes, options));
    }

    internal static PptxDocument OpenSpan(System.ReadOnlySpan<byte> bytes)
    {
        return new PptxDocument(PptxPackage.OpenSpan(bytes, PptxZipReadOptions.CreateDefault()));
    }

    internal static PptxDocument OpenSpanWithOptions(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options)
    {
        return new PptxDocument(PptxPackage.OpenSpan(bytes, options));
    }

    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <remarks>An existing file at the path is replaced. Saving does not close or invalidate the document.</remarks>
    /// <param name="path">The destination presentation path.</param>
    public void SaveFile(string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            SaveStream(stream);
        }
    }

    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    public void SaveStream(Stream stream)
    {
        Package.Save(stream);
    }

    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    public void Save(Stream stream)
    {
        SaveStream(stream);
    }

    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    public byte[] SaveBytes()
    {
        return Package.SaveBytes();
    }

    /// <summary>
    /// Validates the document and returns all diagnostics found during the check.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    public PptxResult Validate()
    {
        return Package.Validate();
    }

    /// <summary>
    /// Repairs recoverable package issues and returns diagnostics describing the changes.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    public PptxResult Repair()
    {
        return Package.Repair();
    }

    /// <summary>
    /// Parses the package into the editable presentation model while retaining diagnostics for missing or malformed parts.
    /// </summary>
    /// <returns>The presentation model. Check its diagnostics before editing when the source may be malformed.</returns>
    public PptxPresentation LoadPresentation()
    {
        return PptxPresentation.Load(Package);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original document.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    public PptxDocument CloneDocument()
    {
        return new PptxDocument(ClonePackage(Package));
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original document.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    public PptxDocument Clone()
    {
        return CloneDocument();
    }

    /// <summary>
    /// Returns the package without changing the document.
    /// </summary>
    /// <remarks>This is the mutable low-level package used by the document. Changes made through it affect subsequent validation and saves.</remarks>
    /// <returns>The resulting package value.</returns>
    public PptxPackage GetPackage()
    {
        return Package;
    }

    internal static PptxPackage ClonePackage(PptxPackage source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        PptxPackage clone = new PptxPackage();
        clone.ContentTypes = CloneContentTypes(source.ContentTypes);
        clone.RootRelationships = CloneRelationshipCollection(source.RootRelationships);
        clone.Diagnostics = new PptxResult();
        PptxUriCore.CopyDiagnostics(source.Diagnostics, clone.Diagnostics);
        clone.IsDirty = source.IsDirty;

        for (int index = 0; index < source.RawEntries.Count; index++)
        {
            PptxZipEntry entry = source.RawEntries[index];
            clone.RawEntries.Add(new PptxZipEntry(entry.Name, entry.PartName, PptxPart.CopyBytes(entry.Bytes), entry.Length, entry.CompressedLength));
        }

        for (int index = 0; index < source.Parts.Count; index++)
        {
            PptxPart part = ClonePart(source.Parts[index]);
            clone.Parts.Add(part);
        }

        return clone;
    }

    internal static PptxContentTypeManifest CloneContentTypes(PptxContentTypeManifest source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        PptxContentTypeManifest clone = new PptxContentTypeManifest();
        PptxUriCore.CopyDiagnostics(source.Diagnostics, clone.Diagnostics);

        for (int index = 0; index < source.Defaults.Count; index++)
        {
            PptxContentTypeDefault item = source.Defaults[index];
            clone.Defaults.Add(new PptxContentTypeDefault(item.Extension, item.ContentType));
        }

        for (int index = 0; index < source.Overrides.Count; index++)
        {
            PptxContentTypeOverride item = source.Overrides[index];
            clone.Overrides.Add(new PptxContentTypeOverride(item.PartName, item.ContentType));
        }

        return clone;
    }

    internal static PptxPart ClonePart(PptxPart source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        PptxPart clone = new PptxPart(source.PartName, source.ContentType, source.GetBytes());
        clone.Relationships = CloneRelationshipCollection(source.Relationships);
        clone.IsDirty = source.IsDirty;

        if (source.HasXmlDocument)
        {
            XmlDocument document = PptxXml.LoadBytes(PptxXml.WriteBytes(source.XmlDocument));
            clone.XmlDocument = document;
            clone.HasXmlDocument = true;
        }

        return clone;
    }

    internal static PptxRelationshipCollection CloneRelationshipCollection(PptxRelationshipCollection source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        PptxRelationshipCollection clone = new PptxRelationshipCollection(source.SourcePartName);
        PptxUriCore.CopyDiagnostics(source.Diagnostics, clone.Diagnostics);

        for (int index = 0; index < source.Relationships.Count; index++)
        {
            clone.Relationships.Add(CloneRelationship(source.Relationships[index]));
        }

        return clone;
    }

    internal static PptxRelationship CloneRelationship(PptxRelationship source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return new PptxRelationship(
            source.SourcePartName,
            source.Id,
            source.Type,
            source.Target,
            source.TargetMode,
            source.TargetPartName,
            source.TargetFragment);
    }
}
