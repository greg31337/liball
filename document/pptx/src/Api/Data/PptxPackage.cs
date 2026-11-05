using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pptx;

/// <summary>
/// Manages the Open Packaging Convention parts and relationships stored inside a PowerPoint file.
/// </summary>
public class PptxPackage
{
    /// <summary>
    /// Contains the package parts in deterministic save order.
    /// </summary>
    /// <remarks>Prefer the package mutation methods when adding, replacing, renaming, or removing parts so content types and relationships remain consistent.</remarks>
    public List<PptxPart> Parts;
    internal List<PptxZipEntry> RawEntries;
    /// <summary>
    /// Stores the content types value associated with this object.
    /// </summary>
    public PptxContentTypeManifest ContentTypes;
    /// <summary>
    /// Stores the root relationships value associated with this object.
    /// </summary>
    public PptxRelationshipCollection RootRelationships;
    /// <summary>
    /// Stores the diagnostics value associated with this object.
    /// </summary>
    public PptxResult Diagnostics;
    internal bool IsDirty;
    internal PptxPackage()
    {
        Parts = new List<PptxPart>();
        RawEntries = new List<PptxZipEntry>();
        ContentTypes = new PptxContentTypeManifest();
        RootRelationships = new PptxRelationshipCollection("");
        Diagnostics = new PptxResult();
        IsDirty = false;
    }
    internal static PptxPackage Create()
    {
        PptxPackage package = new PptxPackage();
        AddCoreContentTypeDefaults(package);
        package.RootRelationships = new PptxRelationshipCollection("");
        package.IsDirty = true;
        return package;
    }

    internal static void AddCoreContentTypeDefaults(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (package.ContentTypes.GetDefaultContentType("rels").Length == 0)
        {
            package.ContentTypes.AddDefault("rels", PptxContentTypes.Relationships);
        }

        if (package.ContentTypes.GetDefaultContentType("xml").Length == 0)
        {
            package.ContentTypes.AddDefault("xml", PptxContentTypes.Xml);
        }
    }
    internal static PptxPackage Open(Stream stream)
    {
        return Open(stream, PptxZipReadOptions.CreateDefault());
    }
    internal static PptxPackage Open(Stream stream, PptxZipReadOptions options)
    {
        return Open(stream, options, new PptxZipReader());
    }

    internal static PptxPackage Open(Stream stream, PptxZipReadOptions options, IPptxPackageReader reader)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The stream must be readable.");
        }

        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        PptxZipReadOptions effectiveOptions = options;
        if (effectiveOptions == null)
        {
            effectiveOptions = PptxZipReadOptions.CreateDefault();
        }
        effectiveOptions.Validate();

        PptxPackage package = new PptxPackage();
        PptxZipReadResult zipResult = reader.Read(stream, effectiveOptions);
        PptxUriCore.CopyDiagnostics(zipResult.Diagnostics, package.Diagnostics);

        for (int index = 0; index < zipResult.Entries.Count; index++)
        {
            package.RawEntries.Add(zipResult.Entries[index]);
        }

        LoadContentTypes(package);
        LoadRootRelationships(package);
        LoadParts(package);
        LoadPartRelationships(package);
        package.MarkClean();

        return package;
    }
    internal static PptxPackage OpenBytes(byte[] bytes)
    {
        return OpenBytes(bytes, PptxZipReadOptions.CreateDefault());
    }
    internal static PptxPackage OpenBytes(byte[] bytes, PptxZipReadOptions options)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return Open(stream, options);
        }
    }
    internal static PptxPackage OpenFile(string path, PptxZipReadOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            return Open(stream, options);
        }
    }
    internal static PptxPackage OpenSpan(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options)
    {
        byte[] copy = bytes.ToArray();
        return OpenBytes(copy, options);
    }
    internal byte[] SaveBytes()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            Save(stream);
            return stream.ToArray();
        }
    }
    internal void Save(Stream stream)
    {
        SaveWithWriter(stream, new PptxZipWriter());
    }

    internal void SaveWithWriter(Stream stream, IPptxPackageWriter writer)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        List<PptxZipEntry> entries = BuildZipEntries();
        PptxResult result = writer.Write(stream, entries, PptxZipWriteOptions.CreateDefault());

        if (result.Failed())
        {
            throw new System.ArgumentException(result.FormatDiagnostics());
        }

        for (int index = 0; index < Parts.Count; index++)
        {
            PptxPart part = Parts[index];
            if (part.HasXmlDocument && part.IsDirty)
            {
                part.Bytes = PptxXml.WriteBytes(part.XmlDocument);
            }
        }

        RawEntries = new List<PptxZipEntry>();
        for (int index = 0; index < entries.Count; index++)
        {
            RawEntries.Add(entries[index]);
        }

        MarkClean();
    }

    internal List<PptxZipEntry> BuildZipEntries()
    {
        EnsurePartContentTypes();

        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        byte[] contentTypesBytes = PptxContentTypesWriter.WriteBytes(ContentTypes);
        entries.Add(new PptxZipEntry(PptxPartNames.ContentTypesEntry, "", contentTypesBytes, contentTypesBytes.Length, 0));

        if (RootRelationships.Count() > 0)
        {
            byte[] rootRelationshipBytes = PptxRelationshipsWriter.WriteBytes(RootRelationships);
            entries.Add(new PptxZipEntry("_rels/.rels", "/_rels/.rels", rootRelationshipBytes, rootRelationshipBytes.Length, 0));
        }

        for (int index = 0; index < Parts.Count; index++)
        {
            PptxPart part = Parts[index];
            byte[] bytes = GetPartBytesForSave(part);
            string entryName = PptxUriCore.ToZipEntryName(part.PartName);
            entries.Add(new PptxZipEntry(entryName, part.PartName, bytes, bytes.Length, 0));

            if (part.Relationships.Count() > 0)
            {
                byte[] relationshipBytes = PptxRelationshipsWriter.WriteBytes(part.Relationships);
                string relationshipEntryName = GetRelationshipEntryNameForPartName(part.PartName);
                entries.Add(new PptxZipEntry(relationshipEntryName, PptxUriCore.NormalizePartName(relationshipEntryName), relationshipBytes, relationshipBytes.Length, 0));
            }
        }

        return entries;
    }

    internal void EnsurePartContentTypes()
    {
        for (int index = 0; index < Parts.Count; index++)
        {
            PptxPart part = Parts[index];
            string existing = ContentTypes.GetContentTypeForPartName(part.PartName);

            if (existing != part.ContentType)
            {
                EnsureContentTypeOverride(part.PartName, part.ContentType);
            }
        }
    }

    internal byte[] GetPartBytesForSave(PptxPart part)
    {
        if (part.HasXmlDocument && part.IsDirty)
        {
            return PptxXml.WriteBytes(part.XmlDocument);
        }

        return part.GetBytes();
    }

    internal static string GetRelationshipEntryNameForPartName(string partName)
    {
        string entryName = PptxUriCore.ToZipEntryName(partName);
        int slashIndex = entryName.LastIndexOf('/');

        if (slashIndex < 0)
        {
            return "_rels/" + entryName + ".rels";
        }

        string prefix = entryName.Substring(0, slashIndex);
        string fileName = entryName.Substring(slashIndex + 1);
        return prefix + "/_rels/" + fileName + ".rels";
    }
    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }
    internal bool Failed()
    {
        return Diagnostics.Failed();
    }
    internal PptxResult Validate()
    {
        return PptxPackageValidator.Validate(this);
    }
    internal PptxResult Repair()
    {
        return PptxPackageRepair.Repair(this);
    }
    internal PptxPackageCleanupResult Cleanup()
    {
        return PptxPackageCleaner.Cleanup(this);
    }
    internal PptxResult Normalize()
    {
        return PptxPackageNormalizer.Normalize(this);
    }
    internal string ToSummaryJson()
    {
        return PptxJsonSummaryExporterCore.ExportPackage(this);
    }
    internal PptxResult ScanSecurity()
    {
        return PptxSecurityScannerCore.Scan(this);
    }
    internal int PartCount()
    {
        return Parts.Count;
    }
    internal int RawEntryCount()
    {
        return RawEntries.Count;
    }
    internal bool HasPart(string partName)
    {
        return IndexOfPart(partName) >= 0;
    }
    internal int IndexOfPart(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);

        for (int index = 0; index < Parts.Count; index++)
        {
            if (Parts[index].PartName == normalized)
            {
                return index;
            }
        }

        return -1;
    }
    internal PptxPart GetPart(string partName)
    {
        int index = IndexOfPart(partName);
        if (index < 0)
        {
            throw new System.ArgumentException("Part was not found.");
        }

        return Parts[index];
    }
    internal PptxPart AddPart(string partName, string contentType, byte[] bytes)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        if (HasPart(normalized))
        {
            throw new System.ArgumentException("Part already exists.");
        }

        PptxPart part = new PptxPart(normalized, contentType, bytes);
        part.MarkDirty();
        Parts.Add(part);
        EnsureContentTypeOverride(normalized, contentType);
        MarkDirty();
        return part;
    }
    internal PptxPart ReplacePart(string partName, string contentType, byte[] bytes)
    {
        PptxPart part = GetPart(partName);
        part.SetContentType(contentType);
        part.SetBytes(bytes);
        EnsureContentTypeOverride(part.PartName, contentType);
        MarkDirty();
        return part;
    }
    internal void RemovePart(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        int index = IndexOfPart(normalized);

        if (index < 0)
        {
            throw new System.ArgumentException("Part was not found.");
        }

        Parts.RemoveAt(index);
        RemoveContentTypeOverride(normalized);
        MarkDirty();
    }
    internal PptxPart RenamePart(string oldPartName, string newPartName)
    {
        string oldNormalized = PptxUriCore.NormalizePartName(oldPartName);
        string newNormalized = PptxUriCore.NormalizePartName(newPartName);
        int index = IndexOfPart(oldNormalized);

        if (index < 0)
        {
            throw new System.ArgumentException("Part was not found.");
        }

        if (HasPart(newNormalized))
        {
            throw new System.ArgumentException("Target part already exists.");
        }

        PptxPart part = Parts[index];
        part.PartName = newNormalized;
        part.Relationships.SourcePartName = newNormalized;
        MoveContentTypeOverride(oldNormalized, newNormalized, part.ContentType);
        UpdateRelationshipTargetsForRename(oldNormalized, newNormalized);
        part.MarkDirty();
        MarkDirty();
        return part;
    }
    internal PptxRelationshipCollection GetRelationships(string partName)
    {
        string clean = PptxDiagnostic.CleanText(partName);

        if (clean.Length == 0)
        {
            return RootRelationships;
        }

        return GetPart(clean).Relationships;
    }
    internal PptxRelationship AddRelationship(string partName, string type, string target, PptxRelationshipTargetMode targetMode)
    {
        PptxRelationshipCollection relationships = GetRelationships(partName);
        string id = AllocateRelationshipId(relationships);
        return AddRelationshipWithId(partName, id, type, target, targetMode);
    }
    internal PptxRelationship AddRelationshipWithId(string partName, string id, string type, string target, PptxRelationshipTargetMode targetMode)
    {
        PptxRelationshipCollection relationships = GetRelationships(partName);
        if (relationships.ContainsId(id))
        {
            throw new System.ArgumentException("Relationship Id already exists.");
        }

        PptxRelationship relationship = CreateRelationship(relationships.SourcePartName, id, type, target, targetMode);
        relationships.Add(relationship);
        MarkRelationshipsDirty(relationships.SourcePartName);
        return relationship;
    }
    internal void RemoveRelationship(string partName, string id)
    {
        PptxRelationshipCollection relationships = GetRelationships(partName);
        int index = relationships.IndexOfId(id);

        if (index < 0)
        {
            throw new System.ArgumentException("Relationship was not found.");
        }

        relationships.Relationships.RemoveAt(index);
        MarkRelationshipsDirty(relationships.SourcePartName);
    }
    internal PptxRelationship SetRelationshipTarget(string partName, string id, string target, PptxRelationshipTargetMode targetMode)
    {
        PptxRelationshipCollection relationships = GetRelationships(partName);
        int index = relationships.IndexOfId(id);

        if (index < 0)
        {
            throw new System.ArgumentException("Relationship was not found.");
        }

        PptxRelationship existing = relationships.Relationships[index];
        PptxRelationship replacement = CreateRelationship(relationships.SourcePartName, existing.Id, existing.Type, target, targetMode);
        relationships.Relationships[index] = replacement;
        MarkRelationshipsDirty(relationships.SourcePartName);
        return replacement;
    }

    internal void EnsureContentTypeOverride(string partName, string contentType)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        PptxPart.ValidateAndNormalizeContentType(contentType);
        RemoveContentTypeOverride(normalized);
        ContentTypes.AddOverride(normalized, contentType);
    }
    internal void RemoveContentTypeOverride(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);

        for (int index = ContentTypes.Overrides.Count - 1; index >= 0; index--)
        {
            if (ContentTypes.Overrides[index].PartName == normalized)
            {
                ContentTypes.Overrides.RemoveAt(index);
            }
        }
    }

    internal void MoveContentTypeOverride(string oldPartName, string newPartName, string contentType)
    {
        RemoveContentTypeOverride(oldPartName);
        EnsureContentTypeOverride(newPartName, contentType);
    }

    internal void UpdateRelationshipTargetsForRename(string oldPartName, string newPartName)
    {
        UpdateRelationshipCollectionTargets(RootRelationships, oldPartName, newPartName);

        for (int index = 0; index < Parts.Count; index++)
        {
            UpdateRelationshipCollectionTargets(Parts[index].Relationships, oldPartName, newPartName);
        }
    }

    internal void UpdateRelationshipCollectionTargets(PptxRelationshipCollection relationships, string oldPartName, string newPartName)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];

            if (relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == oldPartName)
            {
                relationship.Target = "";
                relationship.TargetPartName = newPartName;
                MarkRelationshipsDirty(relationships.SourcePartName);
            }
        }
    }

    internal string AllocateRelationshipId(PptxRelationshipCollection relationships)
    {
        int number = 1;

        while (relationships.ContainsId("rId" + number.ToString()))
        {
            number = number + 1;
        }

        return "rId" + number.ToString();
    }

    internal PptxRelationship CreateRelationship(string sourcePartName, string id, string type, string target, PptxRelationshipTargetMode targetMode)
    {
        PptxRelationshipCollection validation = new PptxRelationshipCollection(sourcePartName);

        if (!PptxRelationshipsParser.ValidateRelationshipId(id, validation))
        {
            throw new System.ArgumentException(validation.Diagnostics.FormatDiagnostics());
        }

        if (!PptxRelationshipsParser.ValidateRelationshipType(type, validation))
        {
            throw new System.ArgumentException(validation.Diagnostics.FormatDiagnostics());
        }

        if (targetMode == PptxRelationshipTargetMode.External)
        {
            if (!PptxRelationshipsParser.ValidateExternalTarget(target, validation, id))
            {
                throw new System.ArgumentException(validation.Diagnostics.FormatDiagnostics());
            }

            return new PptxRelationship(sourcePartName, id, type, target, targetMode, "", "");
        }

        if (!PptxRelationshipsParser.ValidateInternalTarget(sourcePartName, target, validation, id))
        {
            throw new System.ArgumentException(validation.Diagnostics.FormatDiagnostics());
        }

        string targetPartName = PptxRelationshipsParser.NormalizeInternalTargetPartName(sourcePartName, target);
        string targetFragment = PptxUriCore.ExtractFragment(target);
        return new PptxRelationship(sourcePartName, id, type, target, targetMode, targetPartName, targetFragment);
    }

    internal void MarkRelationshipsDirty(string sourcePartName)
    {
        MarkDirty();

        if (sourcePartName.Length == 0)
        {
            return;
        }

        int index = IndexOfPart(sourcePartName);
        if (index >= 0)
        {
            Parts[index].MarkDirty();
        }
    }

    internal void MarkClean()
    {
        IsDirty = false;

        for (int index = 0; index < Parts.Count; index++)
        {
            Parts[index].MarkClean();
        }
    }

    internal void MarkDirty()
    {
        IsDirty = true;
    }

    internal static void LoadContentTypes(PptxPackage package)
    {
        PptxZipEntry? entry = FindRawEntry(package, PptxPartNames.ContentTypesEntry);

        if (entry == null)
        {
            package.Diagnostics.AddError("PKG001", "Package is missing [Content_Types].xml.", PptxPartNames.ContentTypesEntry, "", "");
            return;
        }

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseBytes(entry.Bytes);
        package.ContentTypes = manifest;
        PptxUriCore.CopyDiagnostics(manifest.Diagnostics, package.Diagnostics);
    }

    internal static void LoadRootRelationships(PptxPackage package)
    {
        PptxZipEntry? entry = FindRawEntry(package, "_rels/.rels");

        if (entry == null)
        {
            package.RootRelationships = new PptxRelationshipCollection("");
            return;
        }

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParseRootBytes(entry.Bytes);
        package.RootRelationships = relationships;
        PptxUriCore.CopyDiagnostics(relationships.Diagnostics, package.Diagnostics);
    }

    internal static void LoadParts(PptxPackage package)
    {
        for (int index = 0; index < package.RawEntries.Count; index++)
        {
            PptxZipEntry entry = package.RawEntries[index];

            if (entry.Name == PptxPartNames.ContentTypesEntry)
            {
                continue;
            }

            if (IsRelationshipEntryName(entry.Name))
            {
                continue;
            }

            string partName = entry.PartName;
            string contentType = package.ContentTypes.GetContentTypeForPartName(partName);

            if (contentType.Length == 0)
            {
                package.Diagnostics.AddError("PKG010", "Package part is missing a content type.", partName, "", "");
                contentType = PptxContentTypes.Binary;
            }

            package.Parts.Add(new PptxPart(partName, contentType, entry.Bytes));
        }
    }

    internal static void LoadPartRelationships(PptxPackage package)
    {
        for (int index = 0; index < package.RawEntries.Count; index++)
        {
            PptxZipEntry entry = package.RawEntries[index];

            if (!IsPartRelationshipEntryName(entry.Name))
            {
                continue;
            }

            string sourcePartName = GetSourcePartNameForRelationshipEntry(entry.Name);
            PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartBytes(sourcePartName, entry.Bytes);
            PptxUriCore.CopyDiagnostics(relationships.Diagnostics, package.Diagnostics);

            int partIndex = package.IndexOfPart(sourcePartName);
            if (partIndex < 0)
            {
                package.Diagnostics.AddError("PKG011", "Relationship part has no matching source part.", sourcePartName, "", "");
                continue;
            }

            package.Parts[partIndex].Relationships = relationships;
        }
    }

    internal static PptxZipEntry? FindRawEntry(PptxPackage package, string name)
    {
        for (int index = 0; index < package.RawEntries.Count; index++)
        {
            if (package.RawEntries[index].Name == name)
            {
                return package.RawEntries[index];
            }
        }

        return null;
    }

    internal static bool IsRelationshipEntryName(string entryName)
    {
        if (entryName == "_rels/.rels")
        {
            return true;
        }

        return IsPartRelationshipEntryName(entryName);
    }

    internal static bool IsPartRelationshipEntryName(string entryName)
    {
        if (!entryName.EndsWith(".rels", System.StringComparison.Ordinal))
        {
            return false;
        }

        if (entryName.IndexOf("/_rels/") < 0)
        {
            return false;
        }

        return true;
    }

    internal static string GetSourcePartNameForRelationshipEntry(string entryName)
    {
        int markerIndex = entryName.IndexOf("/_rels/");
        string prefix = entryName.Substring(0, markerIndex);
        string relationshipFile = entryName.Substring(markerIndex + 7);
        string sourceFile = relationshipFile.Substring(0, relationshipFile.Length - 5);
        return PptxUriCore.NormalizePartName(prefix + "/" + sourceFile);
    }
}
