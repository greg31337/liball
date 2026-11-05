using System.IO.Compression;
using System.Text;
using System.Xml;

namespace docx;

/// <summary>
/// Represents the parts, content types, and relationships in an Open Packaging Conventions package.
/// </summary>
public class DocxPackage : IDocxPackageInspector, IDocxPackageParts, IDocxPackageContentTypes, IDocxPackageRelationships
{
    internal List<DocxPart> Parts;
    internal DocxContentTypes ContentTypes;
    internal List<DocxRelationshipSet> RelationshipSets;

    /// <summary>
    /// Creates an empty package model. Use <see cref="Create"/> when a minimal Word document is required.
    /// </summary>
    public DocxPackage()
    {
        this.Parts = new List<DocxPart>();
        this.ContentTypes = new DocxContentTypes();
        this.RelationshipSets = new List<DocxRelationshipSet>();
    }

    internal static DocxPackage Create()
    {
        DocxPackage package = new DocxPackage();
        string mainDocumentPartName = "/word/document.xml";
        DocxRelationshipSet rootRelationships = CreateRootRelationshipSet(mainDocumentPartName);

        package.ContentTypes.SetDefault("rels", DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", DocxConstants.ContentTypeXml);
        package.ContentTypes.SetOverride(mainDocumentPartName, DocxConstants.ContentTypeMainDocument);

        package.AddXmlPartWithContent(mainDocumentPartName, DocxConstants.ContentTypeMainDocument, CreateMinimalDocumentXml());
        package.AddXmlPartWithContent("_rels/.rels", DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        return package;
    }

    internal static DocxRelationshipSet CreateRootRelationshipSet(string mainDocumentPartName)
    {
        string target = DocxPartName.ToZipEntryName(mainDocumentPartName);
        DocxRelationshipSet relationships = new DocxRelationshipSet("/");
        relationships.CreateRelationshipWithId("rId1", DocxConstants.RelationshipTypeOfficeDocument, target, false);
        return relationships;
    }

    internal static string CreateMinimalDocumentXml()
    {
        return DocxDocument.CreateMinimalMainDocumentXml();
    }

    internal DocxPackage CreateDiagnosticCopy()
    {
        DocxPackage copy = new DocxPackage();
        copy.ContentTypes = this.ContentTypes.CopyMappings();

        foreach (DocxPart part in this.Parts)
        {
            copy.Parts.Add(part.CreateDiagnosticCopy());
        }

        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            copy.RelationshipSets.Add(relationshipSet.CreateDiagnosticCopy());
        }

        return copy;
    }

    internal static DocxPackage Open(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        DocxLoadOptions options = new DocxLoadOptions();
        return Open(stream, options);
    }

    internal static DocxPackage Open(Stream stream, DocxLoadOptions options)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.ValidateValues();
        ValidateReadableStream(stream);

        DocxPackage package = new DocxPackage();

        if (options.LazyLoadXmlParts || options.LazyLoadBinaryParts)
        {
            byte[] packageBytes = ReadStreamBytes(stream);

            using (MemoryStream packageStream = new MemoryStream(packageBytes))
            {
                using (ZipArchive archive = new ZipArchive(packageStream, ZipArchiveMode.Read, true))
                {
                    package.LoadContentTypes(archive);
                    package.LoadPartsWithOptions(archive, options, packageBytes);
                    package.LoadRelationshipSets();
                }
            }
        }
        else
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                package.LoadContentTypes(archive);
                package.LoadPartsWithOptions(archive, options, new byte[0]);
                package.LoadRelationshipSets();
            }
        }

        package.ApplyLoadOptions(options);
        return package;
    }

    internal static DocxPackage OpenFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        using (FileStream stream = File.OpenRead(path))
        {
            return Open(stream);
        }
    }

    internal static DocxPackage OpenFile(string path, DocxLoadOptions options)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        using (FileStream stream = File.OpenRead(path))
        {
            return Open(stream, options);
        }
    }

    internal void Save(Stream stream)
    {
        DocxSaveOptions options = new DocxSaveOptions();
        SaveWithOptions(stream, options);
    }

    internal void SaveWithOptions(Stream stream, DocxSaveOptions options)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.ValidateValues();
        ValidateWritableStream(stream);

        DocxZipWriter writer = new DocxZipWriter();
        writer.WriteEntriesWithOptions(stream, CreateZipEntriesWithOptions(options), options);
    }

    internal void SaveFile(string path)
    {
        DocxSaveOptions options = new DocxSaveOptions();
        SaveFileWithOptions(path, options);
    }

    internal void SaveFileWithOptions(string path, DocxSaveOptions options)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        using (FileStream stream = File.Create(path))
        {
            SaveWithOptions(stream, options);
        }
    }

    internal List<DocxZipWriteEntry> CreateZipEntries()
    {
        DocxSaveOptions options = new DocxSaveOptions();
        return CreateZipEntriesWithOptions(options);
    }

    internal List<DocxZipWriteEntry> CreateZipEntriesWithOptions(DocxSaveOptions options)
    {
        DocxZipWriter writer = new DocxZipWriter();
        List<DocxZipWriteEntry> entries = new List<DocxZipWriteEntry>();

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        DocxContentTypes saveContentTypes = CreateSaveContentTypes(options);
        entries.Add(writer.CreateXmlEntry("[Content_Types].xml", saveContentTypes.WriteDeterministicXml(options.UsesDeterministicOutput())));

        foreach (DocxPart part in this.Parts)
        {
            if (!options.PreserveUnknownParts && !IsKnownPart(part))
            {
                continue;
            }

            byte[] bytes = CreatePartBytesForSave(part, options);
            entries.Add(writer.CreateBinaryEntry(DocxPartName.ToZipEntryName(part.PartName), bytes));
        }

        return entries;
    }

    internal void ApplyLoadOptions(DocxLoadOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!options.PreserveUnknownRelationships)
        {
            RemoveUnknownRelationships();
        }

        if (!options.PreserveUnknownParts)
        {
            RemoveUnknownParts();
        }

        if (!options.PreserveXmlWhitespace || !options.PreserveUnknownMarkup)
        {
            TransformLoadedXmlParts(options);
        }

        ApplyLoadRepair(options);
        ApplyLoadValidation(options);
    }

    internal void ApplyLoadRepair(DocxLoadOptions options)
    {
        DocxRepairMode repairMode = options.RepairMode;

        if (options.RepairRecoverablePackageIssues && repairMode == DocxRepairMode.Disabled)
        {
            repairMode = DocxRepairMode.Safe;
        }

        if (repairMode == DocxRepairMode.Disabled)
        {
            options.RepairReport = null;
            return;
        }

        DocxRepair repair = new DocxRepair();
        DocxPackageValidator validator = new DocxPackageValidator();
        DocxValidationReport report = new DocxValidationReport();
        validator.AppendReport(report, repair.RepairMissingContentTypes(this));
        validator.AppendReport(report, repair.RepairMissingRelationshipIds(this));
        validator.AppendReport(report, repair.RepairMissingRelationships(this));

        if (repairMode == DocxRepairMode.Aggressive)
        {
            validator.AppendReport(report, repair.RepairOrphanedRelationships(this));
        }

        validator.AppendReport(report, repair.RepairEmptyBody(this));
        options.RepairReport = report;
    }

    internal void ApplyLoadValidation(DocxLoadOptions options)
    {
        if (options.ValidationMode == DocxValidationMode.None)
        {
            options.ValidationReport = null;
            return;
        }

        if (options.ValidationMode == DocxValidationMode.Full)
        {
            options.ValidationReport = new DocxValidator().ValidatePackage(this);
            return;
        }

        options.ValidationReport = new DocxPackageValidator().ValidatePackage(this);
    }

    internal DocxContentTypes CreateSaveContentTypes(DocxSaveOptions options)
    {
        DocxContentTypes contentTypes = this.ContentTypes.CopyMappings();

        foreach (DocxPart part in this.Parts)
        {
            if (!options.PreserveUnknownParts && !IsKnownPart(part))
            {
                contentTypes.RemoveOverride(part.PartName);
            }
        }

        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "" || options.OutputType == DocxOutputType.Preserve)
        {
            return contentTypes;
        }

        bool macroEnabled = IsMacroEnabled();
        string contentType = DocxConstants.ContentTypeMainDocument;

        if (options.OutputType == DocxOutputType.Template)
        {
            contentType = DocxConstants.ContentTypeTemplateMainDocument;

            if (macroEnabled)
            {
                contentType = DocxConstants.ContentTypeMacroEnabledTemplateMainDocument;
            }
        }
        else if (macroEnabled)
        {
            contentType = DocxConstants.ContentTypeMacroEnabledMainDocument;
        }

        contentTypes.SetContentTypeForPart(mainDocumentPartName, contentType);
        return contentTypes;
    }

    internal byte[] CreatePartBytesForSave(DocxPart part, DocxSaveOptions options)
    {
        if (part.IsRelationshipPart && (!options.PreserveUnknownRelationships || !options.PreserveUnknownMarkup))
        {
            DocxRelationshipSet? relationshipSet = FindRelationshipSetForPart(part.PartName);

            if (relationshipSet != null)
            {
                string relationshipsXml = CreateRelationshipXmlForSave(relationshipSet, options);
                return Encoding.UTF8.GetBytes(relationshipsXml);
            }
        }

        if (IsXmlPart(part.PartName, part.ContentType)
            && (!options.PreserveXmlWhitespace || !options.PreserveUnknownMarkup))
        {
            string xml = part.GetText();
            bool stripUnknownMarkup = !options.PreserveUnknownMarkup && IsWordprocessingPart(part);
            string transformedXml = TransformXml(xml, options.PreserveXmlWhitespace, stripUnknownMarkup);
            return Encoding.UTF8.GetBytes(transformedXml);
        }

        return part.GetBytes();
    }

    internal DocxRelationshipSet? FindRelationshipSetForPart(string relationshipPartName)
    {
        string sourcePartName = DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName(relationshipPartName);

        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            if (relationshipSet.SourcePartName == sourcePartName)
            {
                return relationshipSet;
            }
        }

        return null;
    }

    internal string CreateRelationshipXmlForSave(DocxRelationshipSet relationshipSet, DocxSaveOptions options)
    {
        DocxRelationshipSet output = new DocxRelationshipSet(relationshipSet.SourcePartName);

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (!options.PreserveUnknownRelationships && !IsKnownRelationshipType(relationship.Type))
            {
                continue;
            }

            if (options.PreserveUnknownMarkup)
            {
                output.AddRelationship(relationship);
            }
            else
            {
                output.AddRelationship(new DocxRelationship(relationship.Id, relationship.Type, relationship.Target, relationship.TargetMode));
            }
        }

        return output.WriteXml();
    }

    internal void LoadContentTypes(ZipArchive archive)
    {
        if (archive == null)
        {
            throw new ArgumentNullException("archive");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (IsDirectoryEntryName(entry.FullName))
            {
                continue;
            }

            string partName = NormalizeZipEntryName(entry.FullName);

            if (partName == "/[Content_Types].xml")
            {
                byte[] bytes = ReadZipEntryBytes(entry);
                string xml = Encoding.UTF8.GetString(bytes);
                this.ContentTypes = DocxContentTypes.Parse(xml);
                return;
            }
        }
    }

    internal void LoadParts(ZipArchive archive)
    {
        DocxLoadOptions options = new DocxLoadOptions();
        LoadPartsWithOptions(archive, options, new byte[0]);
    }

    internal void LoadPartsWithOptions(ZipArchive archive, DocxLoadOptions options, byte[] packageBytes)
    {
        if (archive == null)
        {
            throw new ArgumentNullException("archive");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (packageBytes == null)
        {
            throw new ArgumentNullException("packageBytes");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (IsDirectoryEntryName(entry.FullName))
            {
                continue;
            }

            string partName = NormalizeZipEntryName(entry.FullName);

            if (partName == "/[Content_Types].xml")
            {
                continue;
            }

            string contentType = this.ContentTypes.GetContentType(partName);
            bool isRelationshipPart = IsRelationshipPartName(partName);

            if (packageBytes.Length > 0 && ShouldLazyLoadPart(partName, contentType, isRelationshipPart, options))
            {
                this.Parts.Add(DocxPart.CreateLazy(partName, contentType, packageBytes, entry.FullName, isRelationshipPart));
            }
            else
            {
                byte[] bytes = ReadZipEntryBytes(entry);
                this.Parts.Add(new DocxPart(partName, contentType, bytes, isRelationshipPart));
            }
        }
    }

    internal void LoadRelationshipSets()
    {
        foreach (DocxPart part in this.Parts)
        {
            if (!part.IsRelationshipPart)
            {
                continue;
            }

            if (part.PartName == "/_rels/.rels")
            {
                this.RelationshipSets.Add(DocxRelationshipSet.ParsePackageRelationships(part.GetText()));
            }
            else
            {
                string sourcePartName = DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName(part.PartName);
                this.RelationshipSets.Add(DocxRelationshipSet.ParsePartRelationships(sourcePartName, part.GetText()));
            }
        }
    }

    /// <summary>
    /// Returns the content-type registry used by this package.
    /// </summary>
    /// <returns>The package content types.</returns>
    public DocxContentTypes GetContentTypes()
    {
        return this.ContentTypes;
    }

    /// <summary>
    /// Returns the default content-type declarations in catalog order.
    /// </summary>
    /// <returns>A new list containing the default declarations.</returns>
    public List<DocxContentTypeDefault> GetContentTypeDefaults()
    {
        return this.ContentTypes.GetDefaultMappings();
    }

    /// <summary>
    /// Returns the part-specific content-type declarations in catalog order.
    /// </summary>
    /// <returns>A new list containing the override declarations.</returns>
    public List<DocxContentTypeOverride> GetContentTypeOverrides()
    {
        return this.ContentTypes.GetOverrideMappings();
    }

    /// <summary>
    /// Finds a default content-type declaration by file extension.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <returns>The matching declaration, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the extension is empty or malformed.</exception>
    public DocxContentTypeDefault? FindDefaultContentType(string extension)
    {
        return this.ContentTypes.FindDefault(extension);
    }

    /// <summary>
    /// Finds a part-specific content-type declaration by package part name.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The matching declaration, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    public DocxContentTypeOverride? FindOverrideContentType(string partName)
    {
        return this.ContentTypes.FindOverride(partName);
    }

    /// <summary>
    /// Creates a default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>The created declaration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a declaration for the extension already exists.</exception>
    public DocxContentTypeDefault CreateDefaultContentType(string extension, string contentType)
    {
        DocxContentTypeDefault value = this.ContentTypes.CreateDefaultMapping(extension, contentType);
        RefreshPartContentTypes();
        return value;
    }

    /// <summary>
    /// Updates an existing default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <param name="contentType">The replacement MIME content type.</param>
    /// <returns>Whether the declaration existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    public bool UpdateDefaultContentType(string extension, string contentType)
    {
        bool updated = this.ContentTypes.UpdateDefaultMapping(extension, contentType);

        if (updated)
        {
            RefreshPartContentTypes();
        }

        return updated;
    }

    /// <summary>
    /// Removes a default content-type declaration.
    /// </summary>
    /// <param name="extension">The file extension, with or without a leading period.</param>
    /// <returns>Whether the declaration existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the extension is empty or malformed.</exception>
    public bool RemoveDefaultContentType(string extension)
    {
        bool removed = this.ContentTypes.RemoveDefault(extension);

        if (removed)
        {
            RefreshPartContentTypes();
        }

        return removed;
    }

    /// <summary>
    /// Creates a part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>The created declaration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a declaration for the part already exists.</exception>
    public DocxContentTypeOverride CreateOverrideContentType(string partName, string contentType)
    {
        DocxContentTypeOverride value = this.ContentTypes.CreateOverrideMapping(partName, contentType);
        RefreshPartContentTypes();
        return value;
    }

    /// <summary>
    /// Updates an existing part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The replacement MIME content type.</param>
    /// <returns>Whether the declaration existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    public bool UpdateOverrideContentType(string partName, string contentType)
    {
        bool updated = this.ContentTypes.UpdateOverrideMapping(partName, contentType);

        if (updated)
        {
            RefreshPartContentTypes();
        }

        return updated;
    }

    /// <summary>
    /// Removes a part-specific content-type declaration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the declaration existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    public bool RemoveOverrideContentType(string partName)
    {
        bool removed = this.ContentTypes.RemoveOverride(partName);

        if (removed)
        {
            RefreshPartContentTypes();
        }

        return removed;
    }

    /// <summary>
    /// Resolves the effective content type for a package part, preferring a part-specific declaration over an extension default.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The effective MIME content type, or an empty string when none is declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    public string ResolveContentType(string partName)
    {
        return this.ContentTypes.GetContentType(partName);
    }

    internal void RefreshPartContentTypes()
    {
        foreach (DocxPart part in this.Parts)
        {
            part.SetContentType(this.ContentTypes.GetContentType(part.PartName));
        }
    }

    /// <summary>
    /// Returns the number of part items in this package.
    /// </summary>
    /// <returns>The number of affected or available items.</returns>
    public int PartCount()
    {
        return this.Parts.Count;
    }

    /// <summary>
    /// Returns a package part by position.
    /// </summary>
    /// <param name="index">The zero-based index to access.</param>
    /// <returns>The requested package part.</returns>
    public DocxPart GetPart(int index)
    {
        return this.Parts[index];
    }

    /// <summary>
    /// Returns the package parts in catalog order.
    /// </summary>
    /// <returns>A new list containing the package parts.</returns>
    public List<DocxPart> GetParts()
    {
        List<DocxPart> parts = new List<DocxPart>();

        foreach (DocxPart part in this.Parts)
        {
            parts.Add(part);
        }

        return parts;
    }

    /// <summary>
    /// Returns the number of relationship set items in this package.
    /// </summary>
    /// <returns>The number of affected or available items.</returns>
    public int RelationshipSetCount()
    {
        return this.RelationshipSets.Count;
    }

    /// <summary>
    /// Returns a relationship set by position.
    /// </summary>
    /// <param name="index">The zero-based index to access.</param>
    /// <returns>The requested relationship set.</returns>
    public DocxRelationshipSet GetRelationshipSet(int index)
    {
        return this.RelationshipSets[index];
    }

    /// <summary>
    /// Returns package relationship set from this package.
    /// </summary>
    /// <returns>The resulting relationship set.</returns>
    public DocxRelationshipSet? GetPackageRelationshipSet()
    {
        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            if (relationshipSet.SourcePartName == "/")
            {
                return relationshipSet;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns relationships for the package root or a package part.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <returns>A new list containing the relationships in declaration order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    public List<DocxRelationship> GetRelationships(string sourcePartName)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);
        List<DocxRelationship> relationships = new List<DocxRelationship>();
        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return relationships;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            relationships.Add(relationship);
        }

        return relationships;
    }

    /// <summary>
    /// Finds a relationship by source and identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The matching relationship, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    public DocxRelationship? FindRelationship(string sourcePartName, string relationshipId)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return null;
        }

        return relationshipSet.FindById(relationshipId);
    }

    /// <summary>
    /// Resolves an internal relationship target to a normalized package part name.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The resolved part name, or an empty string when the relationship is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a source or target is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the relationship target is external.</exception>
    public string ResolveRelationshipTarget(string sourcePartName, string relationshipId)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartNameById(relationshipId);
    }

    /// <summary>
    /// Allocates the next available relationship identifier for a source.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <returns>The next available relationship identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    public string AllocateRelationshipId(string sourcePartName)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);
        ValidateRelationshipSourceExists(normalizedSourcePartName);
        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return "rId1";
        }

        return relationshipSet.AllocateRelationshipId();
    }

    /// <summary>
    /// Creates a relationship using the next available identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relative or absolute target value.</param>
    /// <param name="external">Whether the target is external to the package.</param>
    /// <returns>The created relationship.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    public DocxRelationship CreateRelationship(string sourcePartName, string type, string target, bool external)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);
        ValidateRelationshipValues(normalizedSourcePartName, type, target, external);
        DocxRelationshipSet relationshipSet = GetOrCreateRelationshipSet(normalizedSourcePartName);
        DocxRelationship relationship = relationshipSet.CreateRelationship(type, target, external);
        WriteRelationshipSetPart(relationshipSet);
        return relationship;
    }

    /// <summary>
    /// Creates a relationship with a caller-supplied identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relative or absolute target value.</param>
    /// <param name="external">Whether the target is external to the package.</param>
    /// <returns>The created relationship.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the identifier already exists for the source.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    public DocxRelationship CreateRelationshipWithId(string sourcePartName, string relationshipId, string type, string target, bool external)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (relationshipId == "")
        {
            throw new ArgumentException("Relationship id is required.", "relationshipId");
        }

        ValidateRelationshipValues(normalizedSourcePartName, type, target, external);
        DocxRelationshipSet relationshipSet = GetOrCreateRelationshipSet(normalizedSourcePartName);
        DocxRelationship relationship = relationshipSet.CreateRelationshipWithId(relationshipId, type, target, external);
        WriteRelationshipSetPart(relationshipSet);
        return relationship;
    }

    /// <summary>
    /// Updates an existing relationship.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="type">The replacement relationship type URI.</param>
    /// <param name="target">The replacement relative or absolute target value.</param>
    /// <param name="external">Whether the replacement target is external to the package.</param>
    /// <returns>Whether the relationship existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    public bool UpdateRelationship(string sourcePartName, string relationshipId, string type, string target, bool external)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        ValidateRelationshipValues(normalizedSourcePartName, type, target, external);
        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return false;
        }

        bool updated = relationshipSet.UpdateRelationship(relationshipId, type, target, external);

        if (updated)
        {
            WriteRelationshipSetPart(relationshipSet);
        }

        return updated;
    }

    /// <summary>
    /// Removes a relationship by source and identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>Whether the relationship existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    public bool RemoveRelationship(string sourcePartName, string relationshipId)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return false;
        }

        bool removed = relationshipSet.RemoveRelationship(relationshipId);

        if (removed)
        {
            WriteRelationshipSetPart(relationshipSet);
        }

        return removed;
    }

    /// <summary>
    /// Returns internal relationships whose resolved target parts are absent.
    /// </summary>
    /// <returns>A new list containing the orphan relationship descriptions.</returns>
    public List<DocxOrphanRelationship> GetOrphanRelationships()
    {
        List<DocxOrphanRelationship> orphans = new List<DocxOrphanRelationship>();

        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (!IsInternalRelationship(relationship))
                {
                    continue;
                }

                string targetPartName = relationshipSet.ResolveTargetPartName(relationship);

                if (!ContainsPart(targetPartName))
                {
                    orphans.Add(new DocxOrphanRelationship(relationshipSet.SourcePartName, relationship, targetPartName));
                }
            }
        }

        return orphans;
    }

    /// <summary>
    /// Indicates whether a relationship has a missing internal target part.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>Whether the existing relationship has a missing internal target.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a source or target is malformed.</exception>
    public bool IsOrphanRelationship(string sourcePartName, string relationshipId)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet == null)
        {
            return false;
        }

        DocxRelationship? relationship = relationshipSet.FindById(relationshipId);

        if (relationship == null || !IsInternalRelationship(relationship))
        {
            return false;
        }

        string targetPartName = relationshipSet.ResolveTargetPartName(relationship);
        return !ContainsPart(targetPartName);
    }

    internal string NormalizeRelationshipSource(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName == "/")
        {
            return "/";
        }

        return DocxPartName.Normalize(sourcePartName);
    }

    internal void ValidateRelationshipSourceExists(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName != "/" && !ContainsPart(sourcePartName))
        {
            throw new KeyNotFoundException("Relationship source part was not found.");
        }
    }

    internal void ValidateRelationshipValues(string sourcePartName, string type, string target, bool external)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (type == "")
        {
            throw new ArgumentException("Relationship type is required.", "type");
        }

        if (target == "")
        {
            throw new ArgumentException("Relationship target is required.", "target");
        }

        ValidateRelationshipSourceExists(sourcePartName);

        if (!external)
        {
            DocxRelationshipSet.ResolveTargetPartName(sourcePartName, target);
        }
    }

    internal DocxRelationshipSet GetOrCreateRelationshipSet(string sourcePartName)
    {
        ValidateRelationshipSourceExists(sourcePartName);
        DocxRelationshipSet? relationshipSet = FindRelationshipSet(sourcePartName);

        if (relationshipSet != null)
        {
            return relationshipSet;
        }

        relationshipSet = new DocxRelationshipSet(sourcePartName);
        this.RelationshipSets.Add(relationshipSet);
        return relationshipSet;
    }

    internal void WriteRelationshipSetPart(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        string relationshipPartName = "/_rels/.rels";

        if (relationshipSet.SourcePartName != "/")
        {
            relationshipPartName = DocxRelationshipSet.GetRelationshipPartName(relationshipSet.SourcePartName);
        }

        string xml = relationshipSet.WriteXml();
        DocxPart? part = FindPart(relationshipPartName);

        if (part == null)
        {
            part = new DocxPart(relationshipPartName, DocxConstants.ContentTypeRelationships, Encoding.UTF8.GetBytes(xml), true);
            this.Parts.Add(part);
        }
        else
        {
            xml = DocxXmlPreservation.PreserveRelationshipContent(part.GetText(), xml);
            part.SetContentType(DocxConstants.ContentTypeRelationships);
            part.ReplaceText(xml);
            part.IsRelationshipPart = true;
        }

        this.ContentTypes.SetContentTypeForPart(relationshipPartName, DocxConstants.ContentTypeRelationships);
    }

    internal bool IsInternalRelationship(DocxRelationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        return relationship.TargetMode == "";
    }

    internal DocxRelationship? FindMainDocumentRelationship()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeOfficeDocument && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetMainDocumentPartName()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();
        DocxRelationship? relationship = FindMainDocumentRelationship();

        if (relationshipSet == null || relationship == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetMainDocumentPart()
    {
        string partName = GetMainDocumentPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasMainDocumentPart()
    {
        return GetMainDocumentPart() != null;
    }

    internal bool IsMacroEnabled()
    {
        if (HasMacroEnabledMainDocument())
        {
            return true;
        }

        if (HasVbaProjectPart())
        {
            return true;
        }

        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (IsMacroRelationship(relationship))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal bool HasMacroEnabledMainDocument()
    {
        DocxPart? part = GetMainDocumentPart();

        if (part != null
            && (part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument))
        {
            return true;
        }

        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return false;
        }

        string contentType = this.ContentTypes.GetContentType(mainDocumentPartName);
        return contentType == DocxConstants.ContentTypeMacroEnabledMainDocument
            || contentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument;
    }

    internal bool HasVbaProjectPart()
    {
        return GetVbaProjectPart() != null;
    }

    internal DocxPart? GetVbaProjectPart()
    {
        foreach (DocxPart part in this.Parts)
        {
            if (IsMacroPart(part))
            {
                return part;
            }
        }

        return null;
    }

    internal bool IsMacroPart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (part.ContentType == DocxConstants.ContentTypeVbaProject)
        {
            return true;
        }

        string catalogContentType = this.ContentTypes.GetContentType(part.PartName);

        if (catalogContentType == DocxConstants.ContentTypeVbaProject)
        {
            return true;
        }

        return part.PartName.EndsWith("/vbaProject.bin") || part.PartName == "/word/vbaProject.bin";
    }

    internal DocxRelationship? FindVbaProjectRelationship()
    {
        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (IsMacroRelationship(relationship))
                {
                    return relationship;
                }
            }
        }

        return null;
    }

    internal bool IsMacroRelationship(DocxRelationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        return relationship.Type == DocxConstants.RelationshipTypeVbaProject;
    }

    internal DocxRelationship? FindCorePropertiesRelationship()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeCoreProperties && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetCorePropertiesPartName()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();
        DocxRelationship? relationship = FindCorePropertiesRelationship();

        if (relationshipSet == null || relationship == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetCorePropertiesPart()
    {
        string partName = GetCorePropertiesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasCorePropertiesPart()
    {
        return GetCorePropertiesPart() != null;
    }

    internal DocxRelationship? FindExtendedPropertiesRelationship()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeExtendedProperties && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetExtendedPropertiesPartName()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();
        DocxRelationship? relationship = FindExtendedPropertiesRelationship();

        if (relationshipSet == null || relationship == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetExtendedPropertiesPart()
    {
        string partName = GetExtendedPropertiesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasExtendedPropertiesPart()
    {
        return GetExtendedPropertiesPart() != null;
    }

    internal DocxRelationship? FindCustomPropertiesRelationship()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeCustomProperties && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetCustomPropertiesPartName()
    {
        DocxRelationshipSet? relationshipSet = GetPackageRelationshipSet();
        DocxRelationship? relationship = FindCustomPropertiesRelationship();

        if (relationshipSet == null || relationship == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetCustomPropertiesPart()
    {
        string partName = GetCustomPropertiesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasCustomPropertiesPart()
    {
        return GetCustomPropertiesPart() != null;
    }

    /// <summary>
    /// Returns relationship set from this package.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <returns>The resulting relationship set.</returns>
    public DocxRelationshipSet? FindRelationshipSet(string sourcePartName)
    {
        string normalizedSourcePartName = NormalizeRelationshipSource(sourcePartName);

        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            if (relationshipSet.SourcePartName == normalizedSourcePartName)
            {
                return relationshipSet;
            }
        }

        return null;
    }

    internal DocxRelationship? FindStylesRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeStyles && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetStylesPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindStylesRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    /// <summary>
    /// Returns styles part from this package.
    /// </summary>
    /// <returns>The resulting part.</returns>
    public DocxPart? GetStylesPart()
    {
        string partName = GetStylesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasStylesPart()
    {
        return GetStylesPart() != null;
    }

    internal DocxRelationship? FindNumberingRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeNumbering && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetNumberingPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindNumberingRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    /// <summary>
    /// Returns numbering part from this package.
    /// </summary>
    /// <returns>The resulting part.</returns>
    public DocxPart? GetNumberingPart()
    {
        string partName = GetNumberingPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasNumberingPart()
    {
        return GetNumberingPart() != null;
    }

    internal DocxRelationship? FindSettingsRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeSettings && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetSettingsPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindSettingsRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetSettingsPart()
    {
        string partName = GetSettingsPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasSettingsPart()
    {
        return GetSettingsPart() != null;
    }

    internal DocxRelationship? FindWebSettingsRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeWebSettings && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetWebSettingsPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindWebSettingsRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetWebSettingsPart()
    {
        string partName = GetWebSettingsPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasWebSettingsPart()
    {
        return GetWebSettingsPart() != null;
    }

    internal DocxRelationship? FindFontTableRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeFontTable && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetFontTablePartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindFontTableRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetFontTablePart()
    {
        string partName = GetFontTablePartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasFontTablePart()
    {
        return GetFontTablePart() != null;
    }

    internal DocxRelationship? FindThemeRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeTheme && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetThemePartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindThemeRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    internal DocxPart? GetThemePart()
    {
        string partName = GetThemePartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasThemePart()
    {
        return GetThemePart() != null;
    }

    internal DocxRelationship? FindFootnotesRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeFootnotes && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetFootnotesPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindFootnotesRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    /// <summary>
    /// Returns footnotes part from this package.
    /// </summary>
    /// <returns>The resulting part.</returns>
    public DocxPart? GetFootnotesPart()
    {
        string partName = GetFootnotesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasFootnotesPart()
    {
        return GetFootnotesPart() != null;
    }

    internal DocxRelationship? FindEndnotesRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeEndnotes && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetEndnotesPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindEndnotesRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    /// <summary>
    /// Returns endnotes part from this package.
    /// </summary>
    /// <returns>The resulting part.</returns>
    public DocxPart? GetEndnotesPart()
    {
        string partName = GetEndnotesPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasEndnotesPart()
    {
        return GetEndnotesPart() != null;
    }

    internal DocxRelationship? FindCommentsRelationship()
    {
        string mainDocumentPartName = GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            return null;
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return null;
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeComments && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string GetCommentsPartName()
    {
        string mainDocumentPartName = GetMainDocumentPartName();
        DocxRelationship? relationship = FindCommentsRelationship();

        if (mainDocumentPartName == "" || relationship == null)
        {
            return "";
        }

        DocxRelationshipSet? relationshipSet = FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet == null)
        {
            return "";
        }

        return relationshipSet.ResolveTargetPartName(relationship);
    }

    /// <summary>
    /// Returns comments part from this package.
    /// </summary>
    /// <returns>The resulting part.</returns>
    public DocxPart? GetCommentsPart()
    {
        string partName = GetCommentsPartName();

        if (partName == "")
        {
            return null;
        }

        return FindPart(partName);
    }

    internal bool HasCommentsPart()
    {
        return GetCommentsPart() != null;
    }

    /// <summary>
    /// Returns part from this package.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The resulting part.</returns>
    public DocxPart? FindPart(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        foreach (DocxPart part in this.Parts)
        {
            if (part.PartName == normalizedPartName)
            {
                return part;
            }
        }

        return null;
    }

    /// <summary>
    /// Indicates whether this package contains part.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the requested condition is true.</returns>
    public bool ContainsPart(string partName)
    {
        return FindPart(partName) != null;
    }

    /// <summary>
    /// Reads an XML package part as text.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>The XML text stored in the part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the requested part does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the requested part is not an XML part.</exception>
    public string GetPartXml(string partName)
    {
        DocxPart? part = FindPart(partName);

        if (part == null)
        {
            throw new KeyNotFoundException("Package part was not found.");
        }

        if (!IsXmlPart(part.PartName, part.ContentType))
        {
            throw new InvalidOperationException("Package part does not contain XML content.");
        }

        return part.GetText();
    }

    /// <summary>
    /// Reads a package part as binary data.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>A copy of the bytes stored in the part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the requested part does not exist.</exception>
    public byte[] GetPartBytes(string partName)
    {
        DocxPart? part = FindPart(partName);

        if (part == null)
        {
            throw new KeyNotFoundException("Package part was not found.");
        }

        return part.GetBytes();
    }

    internal DocxPart AddXmlPart(string partName, string contentType)
    {
        return AddXmlPartWithContent(partName, contentType, "");
    }

    /// <summary>
    /// Adds an XML package part and registers its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="xml">The XML text to store.</param>
    /// <returns>The added package part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the package part already exists.</exception>
    public DocxPart AddXmlPartWithContent(string partName, string contentType, string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return AddBinaryPart(partName, contentType, Encoding.UTF8.GetBytes(xml));
    }

    /// <summary>
    /// Adds a binary package part and registers its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="bytes">The binary data to store. The data is copied.</param>
    /// <returns>The added package part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the package part already exists.</exception>
    public DocxPart AddBinaryPart(string partName, string contentType, byte[] bytes)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        if (ContainsPart(normalizedPartName))
        {
            throw new InvalidOperationException("Package part already exists.");
        }

        bool isRelationshipPart = IsRelationshipPartName(normalizedPartName);
        DocxPart part = new DocxPart(normalizedPartName, contentType, bytes, isRelationshipPart);

        this.Parts.Add(part);
        this.ContentTypes.SetContentTypeForPart(normalizedPartName, contentType);
        SynchronizeRelationshipPart(part);

        return part;
    }

    /// <summary>
    /// Replaces an XML package part and updates its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="xml">The replacement XML text.</param>
    /// <returns>Whether the requested part existed and was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    public bool ReplaceXmlPart(string partName, string contentType, string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return ReplaceBinaryPart(partName, contentType, Encoding.UTF8.GetBytes(xml));
    }

    /// <summary>
    /// Replaces a binary package part and updates its content type.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <param name="contentType">The MIME content type for the part.</param>
    /// <param name="bytes">The replacement binary data. The data is copied.</param>
    /// <returns>Whether the requested part existed and was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    public bool ReplaceBinaryPart(string partName, string contentType, byte[] bytes)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        DocxPart? part = FindPart(normalizedPartName);

        if (part == null)
        {
            return false;
        }

        part.SetContentType(contentType);
        part.ReplaceBytes(bytes);
        part.IsRelationshipPart = IsRelationshipPartName(normalizedPartName);
        this.ContentTypes.SetContentTypeForPart(normalizedPartName, contentType);
        SynchronizeRelationshipPart(part);

        return true;
    }

    /// <summary>
    /// Removes a package part and its part-specific content-type registration.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the requested part existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    public bool RemovePart(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        for (int index = 0; index < this.Parts.Count; index = index + 1)
        {
            DocxPart part = this.Parts[index];

            if (part.PartName == normalizedPartName)
            {
                this.Parts.RemoveAt(index);
                this.ContentTypes.RemoveContentTypeForPart(normalizedPartName);

                if (part.IsRelationshipPart)
                {
                    RemoveRelationshipSetForRelationshipPart(normalizedPartName);
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns parts that cannot be reached from package-level internal relationships.
    /// </summary>
    /// <returns>A new list containing the orphaned package parts.</returns>
    public List<DocxPart> GetOrphanParts()
    {
        List<string> reachablePartNames = GetReachablePartNames();
        List<DocxPart> orphanParts = new List<DocxPart>();

        foreach (DocxPart part in this.Parts)
        {
            if (!ContainsPartName(reachablePartNames, part.PartName))
            {
                orphanParts.Add(part);
            }
        }

        return orphanParts;
    }

    /// <summary>
    /// Indicates whether an existing package part cannot be reached from package-level internal relationships.
    /// </summary>
    /// <param name="partName">The absolute or relative package part name.</param>
    /// <returns>Whether the existing part is orphaned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the part name is malformed.</exception>
    public bool IsOrphanPart(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (!ContainsPart(normalizedPartName))
        {
            return false;
        }

        List<string> reachablePartNames = GetReachablePartNames();
        return !ContainsPartName(reachablePartNames, normalizedPartName);
    }

    internal List<string> GetReachablePartNames()
    {
        List<string> reachablePartNames = new List<string>();
        bool changed = true;

        if (ContainsPart("/_rels/.rels"))
        {
            reachablePartNames.Add("/_rels/.rels");
        }

        while (changed)
        {
            changed = false;

            foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
            {
                bool sourceIsReachable = relationshipSet.SourcePartName == "/"
                    || ContainsPartName(reachablePartNames, relationshipSet.SourcePartName);

                if (!sourceIsReachable)
                {
                    continue;
                }

                if (relationshipSet.SourcePartName != "/")
                {
                    string relationshipPartName = DocxRelationshipSet.GetRelationshipPartName(relationshipSet.SourcePartName);

                    if (ContainsPart(relationshipPartName) && AddPartName(reachablePartNames, relationshipPartName))
                    {
                        changed = true;
                    }
                }

                foreach (DocxRelationship relationship in relationshipSet.Relationships)
                {
                    if (relationship.IsExternal)
                    {
                        continue;
                    }

                    string targetPartName = relationshipSet.ResolveTargetPartName(relationship);

                    if (ContainsPart(targetPartName) && AddPartName(reachablePartNames, targetPartName))
                    {
                        changed = true;
                    }
                }
            }
        }

        return reachablePartNames;
    }

    internal bool AddPartName(List<string> partNames, string partName)
    {
        if (partNames == null)
        {
            throw new ArgumentNullException("partNames");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (ContainsPartName(partNames, partName))
        {
            return false;
        }

        partNames.Add(partName);
        return true;
    }

    internal bool ContainsPartName(List<string> partNames, string partName)
    {
        if (partNames == null)
        {
            throw new ArgumentNullException("partNames");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        foreach (string existingPartName in partNames)
        {
            if (existingPartName == partName)
            {
                return true;
            }
        }

        return false;
    }

    internal void SynchronizeRelationshipPart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (!part.IsRelationshipPart)
        {
            return;
        }

        RemoveRelationshipSetForRelationshipPart(part.PartName);

        if (part.PartName == "/_rels/.rels")
        {
            this.RelationshipSets.Add(DocxRelationshipSet.ParsePackageRelationships(part.GetText()));
        }
        else
        {
            string sourcePartName = DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName(part.PartName);
            this.RelationshipSets.Add(DocxRelationshipSet.ParsePartRelationships(sourcePartName, part.GetText()));
        }
    }

    internal bool RemoveRelationshipSetForRelationshipPart(string relationshipPartName)
    {
        string sourcePartName = DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName(relationshipPartName);

        for (int index = 0; index < this.RelationshipSets.Count; index = index + 1)
        {
            DocxRelationshipSet relationshipSet = this.RelationshipSets[index];

            if (relationshipSet.SourcePartName == sourcePartName)
            {
                this.RelationshipSets.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal void RemoveUnknownRelationships()
    {
        foreach (DocxRelationshipSet relationshipSet in this.RelationshipSets)
        {
            bool changed = false;

            for (int index = relationshipSet.Relationships.Count - 1; index >= 0; index = index - 1)
            {
                DocxRelationship relationship = relationshipSet.Relationships[index];

                if (!IsKnownRelationshipType(relationship.Type))
                {
                    relationshipSet.Relationships.RemoveAt(index);
                    changed = true;
                }
            }

            if (changed)
            {
                WriteRelationshipSetPart(relationshipSet);
            }
        }
    }

    internal void RemoveUnknownParts()
    {
        for (int index = this.Parts.Count - 1; index >= 0; index = index - 1)
        {
            DocxPart part = this.Parts[index];

            if (!IsKnownPart(part))
            {
                RemovePart(part.PartName);
            }
        }
    }

    internal void TransformLoadedXmlParts(DocxLoadOptions options)
    {
        foreach (DocxPart part in this.Parts)
        {
            if (!IsXmlPart(part.PartName, part.ContentType))
            {
                continue;
            }

            bool stripUnknownMarkup = !options.PreserveUnknownMarkup
                && (part.IsRelationshipPart || IsWordprocessingPart(part));
            string transformedXml = TransformXml(part.GetText(), options.PreserveXmlWhitespace, stripUnknownMarkup);
            part.ReplaceText(transformedXml);
        }

        if (!options.PreserveUnknownMarkup)
        {
            this.RelationshipSets.Clear();
            LoadRelationshipSets();
        }
    }

    internal static string TransformXml(string xml, bool preserveWhitespace, bool stripUnknownMarkup)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = preserveWhitespace;
        document.LoadXml(xml);

        if (stripUnknownMarkup)
        {
            RemoveUnknownMarkupFromNode(document);
        }

        return document.OuterXml;
    }

    internal static void RemoveUnknownMarkupFromNode(XmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        for (int childIndex = node.ChildNodes.Count - 1; childIndex >= 0; childIndex = childIndex - 1)
        {
            XmlNode? child = node.ChildNodes[childIndex];

            if (child == null)
            {
                continue;
            }

            if (child.NodeType == XmlNodeType.Element && !IsKnownMarkupNamespace(child.NamespaceURI))
            {
                node.RemoveChild(child);
                continue;
            }

            RemoveUnknownMarkupFromNode(child);
        }

        if (node.Attributes == null)
        {
            return;
        }

        for (int attributeIndex = node.Attributes.Count - 1; attributeIndex >= 0; attributeIndex = attributeIndex - 1)
        {
            XmlAttribute attribute = node.Attributes[attributeIndex];

            if (attribute.NamespaceURI == "http://www.w3.org/2000/xmlns/"
                && !IsKnownMarkupNamespace(attribute.Value))
            {
                node.Attributes.RemoveAt(attributeIndex);
                continue;
            }

            if (attribute.NamespaceURI != ""
                && attribute.NamespaceURI != "http://www.w3.org/2000/xmlns/"
                && !IsKnownMarkupNamespace(attribute.NamespaceURI))
            {
                node.Attributes.RemoveAt(attributeIndex);
            }
        }
    }

    internal static bool IsKnownMarkupNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        return namespaceUri == ""
            || namespaceUri == DocxConstants.NamespaceContentTypes
            || namespaceUri == DocxConstants.NamespacePackageRelationships
            || namespaceUri == DocxConstants.NamespaceRelationships
            || namespaceUri == DocxConstants.NamespaceCoreProperties
            || namespaceUri == DocxConstants.NamespaceDublinCore
            || namespaceUri == DocxConstants.NamespaceDublinCoreTerms
            || namespaceUri == DocxConstants.NamespaceDublinCoreType
            || namespaceUri == DocxConstants.NamespaceXmlSchemaInstance
            || namespaceUri == DocxConstants.NamespaceExtendedProperties
            || namespaceUri == DocxConstants.NamespaceDocPropsVTypes
            || namespaceUri == DocxConstants.NamespaceCustomProperties
            || namespaceUri == DocxConstants.NamespaceWordprocessingMl
            || namespaceUri == DocxConstants.NamespaceStrictWordprocessingMl
            || namespaceUri == DocxConstants.NamespaceMarkupCompatibility
            || namespaceUri == DocxConstants.NamespaceDrawingMl
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMl
            || namespaceUri == DocxConstants.NamespaceWordprocessingDrawing
            || namespaceUri == DocxConstants.NamespaceStrictWordprocessingDrawing
            || namespaceUri == DocxConstants.NamespaceDrawingMlPicture
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlPicture
            || namespaceUri == DocxConstants.NamespaceDrawingMlChart
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlChart
            || namespaceUri == DocxConstants.NamespaceDrawingMlDiagram
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlDiagram
            || namespaceUri == DocxConstants.NamespaceVml
            || namespaceUri == DocxConstants.NamespaceOffice
            || namespaceUri == DocxConstants.NamespaceXml;
    }

    internal static bool IsKnownRelationshipType(string relationshipType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        return relationshipType == DocxConstants.RelationshipTypeCoreProperties
            || relationshipType.StartsWith(DocxConstants.RelationshipTypePrefix)
            || relationshipType.StartsWith(DocxConstants.RelationshipTypeStrictPrefix)
            || relationshipType.StartsWith("http://schemas.microsoft.com/office/2006/relationships/");
    }

    internal static bool IsKnownPart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (part.IsRelationshipPart)
        {
            return true;
        }

        string contentType = part.ContentType;

        return contentType == DocxConstants.ContentTypeXml
            || contentType.StartsWith("application/vnd.openxmlformats-")
            || contentType.StartsWith("application/vnd.ms-word")
            || contentType.StartsWith("application/vnd.ms-office")
            || contentType.StartsWith("image/");
    }

    internal static bool IsWordprocessingPart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        return part.ContentType.Contains("wordprocessingml")
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument;
    }

    internal static bool IsDirectoryEntryName(string entryName)
    {
        if (entryName == null)
        {
            throw new ArgumentNullException("entryName");
        }

        return entryName.EndsWith("/");
    }

    internal static bool IsRelationshipPartName(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (!normalizedPartName.EndsWith(".rels"))
        {
            return false;
        }

        return normalizedPartName.Contains("/_rels/");
    }

    internal static string NormalizeZipEntryName(string entryName)
    {
        if (entryName == null)
        {
            throw new ArgumentNullException("entryName");
        }

        return DocxPartName.Normalize(entryName);
    }

    internal static bool ShouldLazyLoadXmlPart(string partName, string contentType, bool isRelationshipPart, DocxLoadOptions options)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!options.LazyLoadXmlParts)
        {
            return false;
        }

        if (isRelationshipPart)
        {
            return true;
        }

        return IsXmlPart(partName, contentType);
    }

    internal static bool ShouldLazyLoadBinaryPart(string partName, string contentType, bool isRelationshipPart, DocxLoadOptions options)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!options.LazyLoadBinaryParts)
        {
            return false;
        }

        if (isRelationshipPart)
        {
            return false;
        }

        return !IsXmlPart(partName, contentType);
    }

    internal static bool ShouldLazyLoadPart(string partName, string contentType, bool isRelationshipPart, DocxLoadOptions options)
    {
        if (ShouldLazyLoadXmlPart(partName, contentType, isRelationshipPart, options))
        {
            return true;
        }

        return ShouldLazyLoadBinaryPart(partName, contentType, isRelationshipPart, options);
    }

    internal static bool IsXmlPart(string partName, string contentType)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (string.Equals(contentType, DocxConstants.ContentTypeXml, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string normalizedPartName = DocxPartName.Normalize(partName);

        if (normalizedPartName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedPartName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase);
    }

    internal static byte[] ReadStreamBytes(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        using (MemoryStream output = new MemoryStream())
        {
            stream.CopyTo(output);
            return output.ToArray();
        }
    }

    internal static void ValidateReadableStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        stream.ReadExactly(new byte[0], 0, 0);
    }

    internal static void ValidateWritableStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        stream.Write(new byte[0], 0, 0);
    }

    internal static byte[] ReadZipEntryBytes(ZipArchiveEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException("entry");
        }

        using (Stream entryStream = entry.Open())
        {
            using (MemoryStream output = new MemoryStream())
            {
                entryStream.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
