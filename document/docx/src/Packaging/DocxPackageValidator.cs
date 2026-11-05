using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;

namespace docx;

internal class DocxPackageValidator
{
    internal const string DuplicateZipEntryCode = "opc.duplicate-zip-entry";
    internal const string MissingContentTypesCode = "opc.missing-content-types";
    internal const string MissingPackageRelationshipsCode = "opc.missing-package-relationships";
    internal const string DuplicatePartNameCode = "opc.duplicate-part-name";
    internal const string DuplicateRelationshipSetSourceCode = "opc.duplicate-relationship-set-source";
    internal const string DuplicateRelationshipIdCode = "opc.duplicate-relationship-id";
    internal const string MissingRelationshipPartCode = "opc.missing-relationship-part";
    internal const string DuplicateContentTypeDefaultCode = "opc.duplicate-content-type-default";
    internal const string DuplicateContentTypeOverrideCode = "opc.duplicate-content-type-override";
    internal const string InvalidContentTypeCode = "opc.invalid-content-type";
    internal const string InvalidContentTypeDefaultExtensionCode = "opc.invalid-content-type-default-extension";
    internal const string InvalidContentTypeOverridePartNameCode = "opc.invalid-content-type-override-part-name";
    internal const string MissingPartContentTypeCode = "opc.missing-part-content-type";
    internal const string PartContentTypeMismatchCode = "opc.part-content-type-mismatch";
    internal const string MissingContentTypeOverridePartCode = "opc.missing-content-type-override-part";
    internal const string UnexpectedPartContentTypeCode = "opc.unexpected-part-content-type";
    internal const string InvalidXmlPartCode = "xml.invalid-part";
    internal const string MissingStyleReferenceCode = "refs.missing-style";
    internal const string MissingNumberingReferenceCode = "refs.missing-numbering";
    internal const string MissingFootnoteReferenceCode = "refs.missing-footnote";
    internal const string MissingEndnoteReferenceCode = "refs.missing-endnote";
    internal const string MissingCommentReferenceCode = "refs.missing-comment";
    internal const string MissingRelationshipReferenceCode = "refs.missing-relationship";
    internal const string UnexpectedRelationshipReferenceTypeCode = "refs.unexpected-relationship-type";
    internal const string UnmatchedBookmarkStartCode = "refs.unmatched-bookmark-start";
    internal const string UnmatchedBookmarkEndCode = "refs.unmatched-bookmark-end";
    internal const string InvalidRelationshipTargetCode = "opc.invalid-relationship-target";
    internal const string MissingRelationshipTargetPartCode = "opc.missing-relationship-target-part";
    internal const string InvalidRelationshipTargetModeCode = "opc.invalid-relationship-target-mode";
    internal const string MissingMainDocumentRelationshipCode = "wml.missing-main-document-relationship";
    internal const string MultipleMainDocumentRelationshipsCode = "wml.multiple-main-document-relationships";
    internal const string MissingMainDocumentPartCode = "wml.missing-main-document-part";

    internal DocxValidationReport Validate(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        DocxValidationReport report = new DocxValidationReport();
        AppendReport(report, ValidateDuplicateZipEntries(stream));
        ResetStream(stream);
        AppendReport(report, ValidateRequiredPackageItems(stream));
        ResetStream(stream);
        return report;
    }

    internal DocxValidationReport ValidatePackage(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateContentTypesIntoReport(package, report);
        ValidateXmlWellFormednessIntoReport(package, report);
        ValidateReferencesIntoReport(package, report);
        ValidatePackageRelationships(package, report);
        ValidatePartNames(package, report);
        ValidateRelationshipSets(package, report);
        ValidateRelationshipTargetsIntoReport(package, report);
        ValidateRelationshipPartPresence(package, report);
        AppendReport(report, ValidateWordprocessingStructure(package));
        AppendReport(report, ValidateIdentifiersAndRanges(package));
        AppendReport(report, ValidateCompatibility(package));
        AppendReport(report, ValidateNamespaceConformance(package));
        AppendReport(report, ValidateMacroPackage(package));
        return report;
    }

    internal DocxValidationReport ValidateWordprocessingRequiredParts(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateMainDocumentRelationship(package, report);
        return report;
    }

    internal DocxValidationReport ValidateDuplicateZipEntries(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        DocxValidationReport report = new DocxValidationReport();
        List<string> seenEntries = new List<string>();
        List<string> reportedEntries = new List<string>();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (DocxPackage.IsDirectoryEntryName(entry.FullName))
                {
                    continue;
                }

                string partName = DocxPackage.NormalizeZipEntryName(entry.FullName);

                if (ContainsName(seenEntries, partName))
                {
                    if (!ContainsName(reportedEntries, partName))
                    {
                        report.Add(
                            DocxErrorSeverity.Error,
                            DuplicateZipEntryCode,
                            "Duplicate ZIP entry.",
                            partName);

                        reportedEntries.Add(partName);
                    }
                }
                else
                {
                    seenEntries.Add(partName);
                }
            }
        }

        return report;
    }

    internal DocxValidationReport ValidateRequiredPackageItems(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        bool hasContentTypes = false;
        bool hasPackageRelationships = false;
        DocxValidationReport report = new DocxValidationReport();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (DocxPackage.IsDirectoryEntryName(entry.FullName))
                {
                    continue;
                }

                string partName = DocxPackage.NormalizeZipEntryName(entry.FullName);

                if (partName == "/[Content_Types].xml")
                {
                    hasContentTypes = true;
                }

                if (partName == "/_rels/.rels")
                {
                    hasPackageRelationships = true;
                }
            }
        }

        if (!hasContentTypes)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingContentTypesCode,
                "Missing [Content_Types].xml.",
                "/[Content_Types].xml");
        }

        if (!hasPackageRelationships)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingPackageRelationshipsCode,
                "Missing package relationships part.",
                "/_rels/.rels");
        }

        return report;
    }

    internal bool ContainsName(List<string> names, string name)
    {
        if (names == null)
        {
            throw new ArgumentNullException("names");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (string value in names)
        {
            if (value == name)
            {
                return true;
            }
        }

        return false;
    }

    internal void AppendReport(DocxValidationReport target, DocxValidationReport source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        foreach (DocxError error in source.Errors)
        {
            target.AddError(error);
        }
    }

    internal void ResetStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    internal DocxValidationReport ValidateContentTypes(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateContentTypesIntoReport(package, report);
        return report;
    }

    internal void ValidateContentTypesIntoReport(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (package.GetContentTypes().DefaultCount() == 0 && package.GetContentTypes().OverrideCount() == 0)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingContentTypesCode,
                "Missing content type declarations.",
                "/[Content_Types].xml");
            return;
        }

        ValidateContentTypeDefaults(package.GetContentTypes(), report);
        ValidateContentTypeOverrides(package, report);
        ValidatePartContentTypes(package, report);
        ValidateRelationshipTargetContentTypes(package, report);
    }

    internal void ValidateContentTypeDefaults(DocxContentTypes contentTypes, DocxValidationReport report)
    {
        if (contentTypes == null)
        {
            throw new ArgumentNullException("contentTypes");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> seenExtensions = new List<string>();
        List<string> reportedExtensions = new List<string>();

        foreach (DocxContentTypeDefault value in contentTypes.Defaults)
        {
            if (!IsValidContentTypeDefaultExtension(value.Extension))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidContentTypeDefaultExtensionCode,
                    "Invalid content type default extension.",
                    value.Extension);
            }
            else if (ContainsNameIgnoreCase(seenExtensions, value.Extension))
            {
                if (!ContainsNameIgnoreCase(reportedExtensions, value.Extension))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        DuplicateContentTypeDefaultCode,
                        "Duplicate content type default.",
                        value.Extension);
                    reportedExtensions.Add(value.Extension);
                }
            }
            else
            {
                seenExtensions.Add(value.Extension);
            }

            if (!IsValidContentType(value.ContentType))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidContentTypeCode,
                    "Invalid default content type.",
                    value.Extension);
            }
        }
    }

    internal void ValidateContentTypeOverrides(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> seenPartNames = new List<string>();
        List<string> reportedPartNames = new List<string>();

        foreach (DocxContentTypeOverride value in package.GetContentTypes().Overrides)
        {
            if (!DocxPartName.IsValid(value.PartName))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidContentTypeOverridePartNameCode,
                    "Invalid content type override part name.",
                    value.PartName);
            }
            else if (ContainsName(seenPartNames, value.PartName))
            {
                if (!ContainsName(reportedPartNames, value.PartName))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        DuplicateContentTypeOverrideCode,
                        "Duplicate content type override.",
                        value.PartName);
                    reportedPartNames.Add(value.PartName);
                }
            }
            else
            {
                seenPartNames.Add(value.PartName);

                if (!package.ContainsPart(value.PartName))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        MissingContentTypeOverridePartCode,
                        "Content type override targets a missing part.",
                        value.PartName);
                }
            }

            if (!IsValidContentType(value.ContentType))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidContentTypeCode,
                    "Invalid override content type.",
                    value.PartName);
            }
        }
    }

    internal void ValidatePartContentTypes(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (!IsValidContentType(part.ContentType))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidContentTypeCode,
                    "Invalid package part content type.",
                    part.PartName);
            }

            string declaredContentType = package.GetContentTypes().GetContentType(part.PartName);

            if (declaredContentType == "")
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    MissingPartContentTypeCode,
                    "Package part is missing a content type declaration.",
                    part.PartName);
            }
            else if (declaredContentType != part.ContentType)
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    PartContentTypeMismatchCode,
                    "Package part content type does not match the content type catalog.",
                    part.PartName);
            }
        }
    }

    internal void ValidateRelationshipTargetContentTypes(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int relationshipSetIndex = 0; relationshipSetIndex < package.RelationshipSetCount(); relationshipSetIndex = relationshipSetIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(relationshipSetIndex);

            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                ValidateRelationshipTargetContentType(package, relationshipSet, relationship, report);
            }
        }
    }

    internal void ValidateRelationshipTargetContentType(IDocxPackageInspector package, DocxRelationshipSet relationshipSet, DocxRelationship relationship, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (IsExternalRelationshipTargetMode(relationship.TargetMode))
        {
            return;
        }

        string targetPartName = ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

        if (targetPartName == "")
        {
            return;
        }

        if (!package.ContainsPart(targetPartName))
        {
            return;
        }

        string contentType = package.GetContentTypes().GetContentType(targetPartName);

        if (contentType == "")
        {
            return;
        }

        if (!IsExpectedContentTypeForRelationship(relationship.Type, contentType))
        {
            report.Add(
                DocxErrorSeverity.Error,
                UnexpectedPartContentTypeCode,
                "Relationship target has an unexpected content type.",
                targetPartName);
        }
    }

    internal bool IsExpectedContentTypeForRelationship(string relationshipType, string contentType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        relationshipType = GetTransitionalRelationshipType(relationshipType);

        if (relationshipType == DocxConstants.RelationshipTypeOfficeDocument)
        {
            return contentType == DocxConstants.ContentTypeMainDocument
                || contentType == DocxConstants.ContentTypeTemplateMainDocument
                || contentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                || contentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument;
        }

        string expectedContentType = GetExpectedContentTypeForRelationship(relationshipType);

        if (expectedContentType == "")
        {
            return true;
        }

        return contentType == expectedContentType;
    }

    internal string GetExpectedContentTypeForRelationship(string relationshipType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        relationshipType = GetTransitionalRelationshipType(relationshipType);

        if (relationshipType == DocxConstants.RelationshipTypeStyles)
        {
            return DocxConstants.ContentTypeStyles;
        }

        if (relationshipType == DocxConstants.RelationshipTypeNumbering)
        {
            return DocxConstants.ContentTypeNumbering;
        }

        if (relationshipType == DocxConstants.RelationshipTypeSettings)
        {
            return DocxConstants.ContentTypeSettings;
        }

        if (relationshipType == DocxConstants.RelationshipTypeWebSettings)
        {
            return DocxConstants.ContentTypeWebSettings;
        }

        if (relationshipType == DocxConstants.RelationshipTypeFontTable)
        {
            return DocxConstants.ContentTypeFontTable;
        }

        if (relationshipType == DocxConstants.RelationshipTypeTheme)
        {
            return DocxConstants.ContentTypeTheme;
        }

        if (relationshipType == DocxConstants.RelationshipTypeHeader)
        {
            return DocxConstants.ContentTypeHeader;
        }

        if (relationshipType == DocxConstants.RelationshipTypeFooter)
        {
            return DocxConstants.ContentTypeFooter;
        }

        if (relationshipType == DocxConstants.RelationshipTypeFootnotes)
        {
            return DocxConstants.ContentTypeFootnotes;
        }

        if (relationshipType == DocxConstants.RelationshipTypeEndnotes)
        {
            return DocxConstants.ContentTypeEndnotes;
        }

        if (relationshipType == DocxConstants.RelationshipTypeComments)
        {
            return DocxConstants.ContentTypeComments;
        }

        if (relationshipType == DocxConstants.RelationshipTypeCoreProperties)
        {
            return DocxConstants.ContentTypeCoreProperties;
        }

        if (relationshipType == DocxConstants.RelationshipTypeExtendedProperties)
        {
            return DocxConstants.ContentTypeExtendedProperties;
        }

        if (relationshipType == DocxConstants.RelationshipTypeCustomProperties)
        {
            return DocxConstants.ContentTypeCustomProperties;
        }

        return "";
    }

    internal bool IsValidContentType(string contentType)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            return false;
        }

        if (ContainsWhitespaceOrControl(contentType))
        {
            return false;
        }

        int slashIndex = contentType.IndexOf('/');

        if (slashIndex <= 0)
        {
            return false;
        }

        if (slashIndex == contentType.Length - 1)
        {
            return false;
        }

        if (slashIndex != contentType.LastIndexOf('/'))
        {
            return false;
        }

        return true;
    }

    internal bool IsValidContentTypeDefaultExtension(string extension)
    {
        if (extension == null)
        {
            throw new ArgumentNullException("extension");
        }

        if (extension == "")
        {
            return false;
        }

        if (extension.StartsWith("."))
        {
            return false;
        }

        if (extension.Contains("/") || extension.Contains("\\"))
        {
            return false;
        }

        if (ContainsWhitespaceOrControl(extension))
        {
            return false;
        }

        return true;
    }

    internal bool ContainsNameIgnoreCase(List<string> names, string name)
    {
        if (names == null)
        {
            throw new ArgumentNullException("names");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (string value in names)
        {
            if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal DocxValidationReport ValidateXmlWellFormedness(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateXmlWellFormednessIntoReport(package, report);
        return report;
    }

    internal void ValidateXmlWellFormednessIntoReport(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);
            ValidateXmlPartWellFormedness(part, report);
        }
    }

    internal void ValidateXmlPartWellFormedness(DocxPart part, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (!IsXmlPart(part))
        {
            return;
        }

        string error = GetXmlWellFormednessError(part.GetText());

        if (error != "")
        {
            report.Add(
                DocxErrorSeverity.Error,
                InvalidXmlPartCode,
                "XML part is not well formed. " + error,
                part.PartName);
        }
    }

    internal bool IsXmlPart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (part.IsRelationshipPart)
        {
            return true;
        }

        if (part.ContentType != null && IsXmlContentType(part.ContentType))
        {
            return true;
        }

        string extension = DocxContentTypes.GetExtensionFromPartName(part.PartName);

        return string.Equals(extension, "xml", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, "rels", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsXmlContentType(string contentType)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        return string.Equals(contentType, DocxConstants.ContentTypeXml, StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, DocxConstants.ContentTypeRelationships, StringComparison.OrdinalIgnoreCase)
            || contentType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
    }

    internal string GetXmlWellFormednessError(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        Task task = Task.Run(() => ReadXmlToEnd(xml));

        while (!task.IsCompleted)
        {
        }

        if (!task.IsFaulted)
        {
            return "";
        }

        if (task.Exception == null)
        {
            return "Invalid XML.";
        }

        Exception exception = task.Exception.GetBaseException();
        return exception.Message;
    }

    internal void ReadXmlToEnd(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                }
            }
        }
    }

    internal DocxValidationReport ValidateReferences(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateReferencesIntoReport(package, report);
        return report;
    }

    internal void ValidateReferencesIntoReport(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> styleIds = CollectStyleIds(package);
        List<string> numberingIds = CollectNumberingIds(package);
        List<string> footnoteIds = CollectFootnoteIds(package);
        List<string> endnoteIds = CollectEndnoteIds(package);
        List<string> commentIds = CollectCommentIds(package);

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);
            ValidatePartReferences(package, part, styleIds, numberingIds, footnoteIds, endnoteIds, commentIds, report);
        }
    }

    internal void ValidatePartReferences(IDocxPackageInspector package, DocxPart part, List<string> styleIds, List<string> numberingIds, List<string> footnoteIds, List<string> endnoteIds, List<string> commentIds, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (styleIds == null)
        {
            throw new ArgumentNullException("styleIds");
        }

        if (numberingIds == null)
        {
            throw new ArgumentNullException("numberingIds");
        }

        if (footnoteIds == null)
        {
            throw new ArgumentNullException("footnoteIds");
        }

        if (endnoteIds == null)
        {
            throw new ArgumentNullException("endnoteIds");
        }

        if (commentIds == null)
        {
            throw new ArgumentNullException("commentIds");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (part.IsRelationshipPart || !IsXmlPart(part))
        {
            return;
        }

        if (GetXmlWellFormednessError(part.GetText()) != "")
        {
            return;
        }

        List<string> bookmarkStartIds = new List<string>();
        List<string> bookmarkEndIds = new List<string>();

        using (StringReader stringReader = new StringReader(part.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        ValidateElementReferences(package, part, reader, styleIds, numberingIds, footnoteIds, endnoteIds, commentIds, bookmarkStartIds, bookmarkEndIds, report);
                    }
                }
            }
        }

        ValidateBookmarkPairs(part, bookmarkStartIds, bookmarkEndIds, report);
    }

    internal void ValidateElementReferences(IDocxPackageInspector package, DocxPart part, XmlReader reader, List<string> styleIds, List<string> numberingIds, List<string> footnoteIds, List<string> endnoteIds, List<string> commentIds, List<string> bookmarkStartIds, List<string> bookmarkEndIds, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (styleIds == null)
        {
            throw new ArgumentNullException("styleIds");
        }

        if (numberingIds == null)
        {
            throw new ArgumentNullException("numberingIds");
        }

        if (footnoteIds == null)
        {
            throw new ArgumentNullException("footnoteIds");
        }

        if (endnoteIds == null)
        {
            throw new ArgumentNullException("endnoteIds");
        }

        if (commentIds == null)
        {
            throw new ArgumentNullException("commentIds");
        }

        if (bookmarkStartIds == null)
        {
            throw new ArgumentNullException("bookmarkStartIds");
        }

        if (bookmarkEndIds == null)
        {
            throw new ArgumentNullException("bookmarkEndIds");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string localName = reader.LocalName;
        string namespaceUri = reader.NamespaceURI;

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl)
        {
            ValidateWordprocessingElementReferences(package, part, reader, localName, styleIds, numberingIds, footnoteIds, endnoteIds, commentIds, bookmarkStartIds, bookmarkEndIds, report);
        }

        ValidateMediaRelationshipAttributes(package, part, reader, report);
    }

    internal void ValidateWordprocessingElementReferences(IDocxPackageInspector package, DocxPart part, XmlReader reader, string localName, List<string> styleIds, List<string> numberingIds, List<string> footnoteIds, List<string> endnoteIds, List<string> commentIds, List<string> bookmarkStartIds, List<string> bookmarkEndIds, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (styleIds == null)
        {
            throw new ArgumentNullException("styleIds");
        }

        if (numberingIds == null)
        {
            throw new ArgumentNullException("numberingIds");
        }

        if (footnoteIds == null)
        {
            throw new ArgumentNullException("footnoteIds");
        }

        if (endnoteIds == null)
        {
            throw new ArgumentNullException("endnoteIds");
        }

        if (commentIds == null)
        {
            throw new ArgumentNullException("commentIds");
        }

        if (bookmarkStartIds == null)
        {
            throw new ArgumentNullException("bookmarkStartIds");
        }

        if (bookmarkEndIds == null)
        {
            throw new ArgumentNullException("bookmarkEndIds");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (IsStyleReferenceElement(localName))
        {
            ValidateValueReference(part, reader, "val", styleIds, MissingStyleReferenceCode, "Style reference points to a missing style.", report);
        }

        if (localName == "numId")
        {
            ValidateValueReference(part, reader, "val", numberingIds, MissingNumberingReferenceCode, "Numbering reference points to a missing numbering instance.", report);
        }

        if (localName == "footnoteReference")
        {
            ValidateValueReference(part, reader, "id", footnoteIds, MissingFootnoteReferenceCode, "Footnote reference points to a missing footnote.", report);
        }

        if (localName == "endnoteReference")
        {
            ValidateValueReference(part, reader, "id", endnoteIds, MissingEndnoteReferenceCode, "Endnote reference points to a missing endnote.", report);
        }

        if (localName == "commentReference" || localName == "commentRangeStart" || localName == "commentRangeEnd")
        {
            ValidateValueReference(part, reader, "id", commentIds, MissingCommentReferenceCode, "Comment reference points to a missing comment.", report);
        }

        if (localName == "headerReference")
        {
            ValidateRelationshipReference(package, part, ReadAttribute(reader, "id", DocxConstants.NamespaceRelationships), DocxConstants.RelationshipTypeHeader, report);
        }

        if (localName == "footerReference")
        {
            ValidateRelationshipReference(package, part, ReadAttribute(reader, "id", DocxConstants.NamespaceRelationships), DocxConstants.RelationshipTypeFooter, report);
        }

        if (localName == "bookmarkStart")
        {
            AddNameIfMissing(bookmarkStartIds, ReadAttribute(reader, "id", DocxConstants.NamespaceWordprocessingMl));
        }

        if (localName == "bookmarkEnd")
        {
            AddNameIfMissing(bookmarkEndIds, ReadAttribute(reader, "id", DocxConstants.NamespaceWordprocessingMl));
        }
    }

    internal void ValidateMediaRelationshipAttributes(IDocxPackageInspector package, DocxPart part, XmlReader reader, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string embedId = ReadAttribute(reader, "embed", DocxConstants.NamespaceRelationships);
        string linkId = ReadAttribute(reader, "link", DocxConstants.NamespaceRelationships);

        ValidateRelationshipReference(package, part, embedId, DocxConstants.RelationshipTypeImage, report);
        ValidateRelationshipReference(package, part, linkId, DocxConstants.RelationshipTypeImage, report);
    }

    internal void ValidateValueReference(DocxPart part, XmlReader reader, string attributeLocalName, List<string> definedIds, string errorCode, string message, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (attributeLocalName == null)
        {
            throw new ArgumentNullException("attributeLocalName");
        }

        if (definedIds == null)
        {
            throw new ArgumentNullException("definedIds");
        }

        if (errorCode == null)
        {
            throw new ArgumentNullException("errorCode");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string value = ReadAttribute(reader, attributeLocalName, DocxConstants.NamespaceWordprocessingMl);

        if (value == "")
        {
            return;
        }

        if (!ContainsName(definedIds, value))
        {
            report.Add(
                DocxErrorSeverity.Error,
                errorCode,
                message,
                part.PartName + "#" + value);
        }
    }

    internal void ValidateRelationshipReference(IDocxPackageInspector package, DocxPart part, string relationshipId, string expectedRelationshipType, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (expectedRelationshipType == null)
        {
            throw new ArgumentNullException("expectedRelationshipType");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (relationshipId == "")
        {
            return;
        }

        DocxRelationshipSet? relationshipSet = package.FindRelationshipSet(part.PartName);

        if (relationshipSet == null)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingRelationshipReferenceCode,
                "Relationship reference has no relationship set.",
                part.PartName + "#" + relationshipId);
            return;
        }

        DocxRelationship? relationship = relationshipSet.FindById(relationshipId);

        if (relationship == null)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingRelationshipReferenceCode,
                "Relationship reference points to a missing relationship.",
                part.PartName + "#" + relationshipId);
            return;
        }

        if (expectedRelationshipType != "" && !AreEquivalentRelationshipTypes(relationship.Type, expectedRelationshipType))
        {
            report.Add(
                DocxErrorSeverity.Error,
                UnexpectedRelationshipReferenceTypeCode,
                "Relationship reference points to an unexpected relationship type.",
                part.PartName + "#" + relationshipId);
        }
    }

    internal void ValidateBookmarkPairs(DocxPart part, List<string> bookmarkStartIds, List<string> bookmarkEndIds, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (bookmarkStartIds == null)
        {
            throw new ArgumentNullException("bookmarkStartIds");
        }

        if (bookmarkEndIds == null)
        {
            throw new ArgumentNullException("bookmarkEndIds");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        foreach (string id in bookmarkStartIds)
        {
            if (!ContainsName(bookmarkEndIds, id))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    UnmatchedBookmarkStartCode,
                    "Bookmark start is missing a matching bookmark end.",
                    part.PartName + "#" + id);
            }
        }

        foreach (string id in bookmarkEndIds)
        {
            if (!ContainsName(bookmarkStartIds, id))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    UnmatchedBookmarkEndCode,
                    "Bookmark end is missing a matching bookmark start.",
                    part.PartName + "#" + id);
            }
        }
    }

    internal List<string> CollectStyleIds(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        List<string> ids = new List<string>();
        DocxPart? part = package.GetStylesPart();

        if (part != null)
        {
            CollectElementAttributeValues(part, "style", DocxConstants.NamespaceWordprocessingMl, "styleId", DocxConstants.NamespaceWordprocessingMl, ids);
        }

        return ids;
    }

    internal List<string> CollectNumberingIds(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        List<string> ids = new List<string>();
        DocxPart? part = package.GetNumberingPart();

        if (part != null)
        {
            CollectElementAttributeValues(part, "num", DocxConstants.NamespaceWordprocessingMl, "numId", DocxConstants.NamespaceWordprocessingMl, ids);
        }

        return ids;
    }

    internal List<string> CollectFootnoteIds(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        List<string> ids = new List<string>();
        DocxPart? part = package.GetFootnotesPart();

        if (part != null)
        {
            CollectElementAttributeValues(part, "footnote", DocxConstants.NamespaceWordprocessingMl, "id", DocxConstants.NamespaceWordprocessingMl, ids);
        }

        return ids;
    }

    internal List<string> CollectEndnoteIds(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        List<string> ids = new List<string>();
        DocxPart? part = package.GetEndnotesPart();

        if (part != null)
        {
            CollectElementAttributeValues(part, "endnote", DocxConstants.NamespaceWordprocessingMl, "id", DocxConstants.NamespaceWordprocessingMl, ids);
        }

        return ids;
    }

    internal List<string> CollectCommentIds(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        List<string> ids = new List<string>();
        DocxPart? part = package.GetCommentsPart();

        if (part != null)
        {
            CollectElementAttributeValues(part, "comment", DocxConstants.NamespaceWordprocessingMl, "id", DocxConstants.NamespaceWordprocessingMl, ids);
        }

        return ids;
    }

    internal void CollectElementAttributeValues(DocxPart part, string elementLocalName, string elementNamespaceUri, string attributeLocalName, string attributeNamespaceUri, List<string> values)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (elementLocalName == null)
        {
            throw new ArgumentNullException("elementLocalName");
        }

        if (elementNamespaceUri == null)
        {
            throw new ArgumentNullException("elementNamespaceUri");
        }

        if (attributeLocalName == null)
        {
            throw new ArgumentNullException("attributeLocalName");
        }

        if (attributeNamespaceUri == null)
        {
            throw new ArgumentNullException("attributeNamespaceUri");
        }

        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (GetXmlWellFormednessError(part.GetText()) != "")
        {
            return;
        }

        using (StringReader stringReader = new StringReader(part.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == elementLocalName
                        && reader.NamespaceURI == elementNamespaceUri)
                    {
                        AddNameIfMissing(values, ReadAttribute(reader, attributeLocalName, attributeNamespaceUri));
                    }
                }
            }
        }
    }

    internal bool IsStyleReferenceElement(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "pStyle"
            || localName == "rStyle"
            || localName == "tblStyle"
            || localName == "basedOn"
            || localName == "next"
            || localName == "link";
    }

    internal void AddNameIfMissing(List<string> names, string name)
    {
        if (names == null)
        {
            throw new ArgumentNullException("names");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name == "")
        {
            return;
        }

        if (!ContainsName(names, name))
        {
            names.Add(name);
        }
    }

    internal string ReadAttribute(XmlReader reader, string localName, string namespaceUri)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        string? value = reader.GetAttribute(localName, namespaceUri);

        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal void ValidatePackageRelationships(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (package.GetPackageRelationshipSet() == null)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingPackageRelationshipsCode,
                "Missing package relationships set.",
                "/_rels/.rels");
        }
    }

    internal void ValidateMainDocumentRelationship(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        DocxRelationshipSet? relationshipSet = package.GetPackageRelationshipSet();

        if (relationshipSet == null)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingMainDocumentRelationshipCode,
                "Missing WordprocessingML main document relationship.",
                "/_rels/.rels");
            return;
        }

        int mainDocumentRelationshipCount = CountMainDocumentRelationships(relationshipSet);

        if (mainDocumentRelationshipCount == 0)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingMainDocumentRelationshipCode,
                "Missing WordprocessingML main document relationship.",
                "/_rels/.rels");
            return;
        }

        if (mainDocumentRelationshipCount > 1)
        {
            report.Add(
                DocxErrorSeverity.Error,
                MultipleMainDocumentRelationshipsCode,
                "Multiple WordprocessingML main document relationships.",
                "/_rels/.rels");
        }

        DocxRelationship? relationship = FindFirstMainDocumentRelationship(relationshipSet);

        if (relationship == null)
        {
            return;
        }

        string mainDocumentPartName = ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

        if (mainDocumentPartName == "")
        {
            return;
        }

        if (!package.ContainsPart(mainDocumentPartName))
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingMainDocumentPartCode,
                "Missing WordprocessingML main document part.",
                mainDocumentPartName);
        }
    }

    internal int CountMainDocumentRelationships(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        int count = 0;

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (AreEquivalentRelationshipTypes(relationship.Type, DocxConstants.RelationshipTypeOfficeDocument) && !relationship.IsExternal)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal DocxRelationship? FindFirstMainDocumentRelationship(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (AreEquivalentRelationshipTypes(relationship.Type, DocxConstants.RelationshipTypeOfficeDocument) && !relationship.IsExternal)
            {
                return relationship;
            }
        }

        return null;
    }

    internal DocxValidationReport ValidateRelationshipTargets(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        ValidateRelationshipTargetsIntoReport(package, report);
        return report;
    }

    internal void ValidateRelationshipTargetsIntoReport(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int relationshipSetIndex = 0; relationshipSetIndex < package.RelationshipSetCount(); relationshipSetIndex = relationshipSetIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(relationshipSetIndex);

            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                ValidateRelationshipTarget(package, relationshipSet, relationship, report);
            }
        }
    }

    internal void ValidateRelationshipTarget(IDocxPackageInspector package, DocxRelationshipSet relationshipSet, DocxRelationship relationship, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (!IsValidRelationshipTargetMode(relationship.TargetMode))
        {
            report.Add(
                DocxErrorSeverity.Error,
                InvalidRelationshipTargetModeCode,
                "Invalid relationship target mode.",
                GetRelationshipLocation(relationshipSet, relationship));
            return;
        }

        if (IsExternalRelationshipTargetMode(relationship.TargetMode))
        {
            if (!IsValidExternalRelationshipTarget(relationship.Target))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    InvalidRelationshipTargetCode,
                    "Invalid external relationship target.",
                    GetRelationshipLocation(relationshipSet, relationship));
            }

            return;
        }

        string targetPartName = ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

        if (targetPartName == "")
        {
            report.Add(
                DocxErrorSeverity.Error,
                InvalidRelationshipTargetCode,
                "Invalid internal relationship target.",
                GetRelationshipLocation(relationshipSet, relationship));
            return;
        }

        if (!package.ContainsPart(targetPartName))
        {
            report.Add(
                DocxErrorSeverity.Error,
                MissingRelationshipTargetPartCode,
                "Relationship target part is missing.",
                targetPartName);
        }
    }

    internal bool IsValidRelationshipTargetMode(string targetMode)
    {
        if (targetMode == null)
        {
            throw new ArgumentNullException("targetMode");
        }

        return targetMode == ""
            || string.Equals(targetMode, "Internal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetMode, "External", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsExternalRelationshipTargetMode(string targetMode)
    {
        if (targetMode == null)
        {
            throw new ArgumentNullException("targetMode");
        }

        return string.Equals(targetMode, "External", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsValidExternalRelationshipTarget(string target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (target == "")
        {
            return false;
        }

        if (ContainsWhitespaceOrControl(target))
        {
            return false;
        }

        int separatorIndex = GetUriSchemeSeparatorIndex(target);
        return separatorIndex > 0 && separatorIndex < target.Length - 1;
    }

    internal string ResolveInternalRelationshipTargetPartName(DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (relationship.Target == null)
        {
            return "";
        }

        if (relationship.Target == "")
        {
            return "";
        }

        if (IsAbsoluteUriTarget(relationship.Target))
        {
            return "";
        }

        string sourceBaseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(relationshipSet.SourcePartName);
        string combinedTarget = relationship.Target;

        if (!combinedTarget.StartsWith("/"))
        {
            combinedTarget = sourceBaseDirectory + combinedTarget;
        }

        string resolvedTarget = ResolveRelationshipTargetPathSegments(combinedTarget);

        if (resolvedTarget == "")
        {
            return "";
        }

        if (!DocxPartName.IsValid(resolvedTarget))
        {
            return "";
        }

        return DocxPartName.Normalize(resolvedTarget);
    }

    internal string ResolveRelationshipTargetPathSegments(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        string normalizedPath = path.Replace('\\', '/');
        List<string> segments = new List<string>();
        int segmentStart = 0;

        if (normalizedPath.StartsWith("/"))
        {
            segmentStart = 1;
        }

        for (int index = segmentStart; index <= normalizedPath.Length; index = index + 1)
        {
            if (index == normalizedPath.Length || normalizedPath[index] == '/')
            {
                int segmentLength = index - segmentStart;
                string segment = normalizedPath.Substring(segmentStart, segmentLength);

                if (segment == "" || segment == ".")
                {
                }
                else if (segment == "..")
                {
                    if (segments.Count == 0)
                    {
                        return "";
                    }

                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(segment);
                }

                segmentStart = index + 1;
            }
        }

        string resolvedPath = "";

        foreach (string segment in segments)
        {
            resolvedPath = resolvedPath + "/" + segment;
        }

        if (resolvedPath == "")
        {
            return "/";
        }

        return resolvedPath;
    }

    internal bool IsAbsoluteUriTarget(string target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        return GetUriSchemeSeparatorIndex(target) > 0;
    }

    internal int GetUriSchemeSeparatorIndex(string target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        int separatorIndex = target.IndexOf(':');

        if (separatorIndex <= 0)
        {
            return -1;
        }

        for (int index = 0; index < separatorIndex; index = index + 1)
        {
            char value = target[index];

            if (index == 0 && !IsAsciiLetter(value))
            {
                return -1;
            }

            if (index > 0 && !IsUriSchemeCharacter(value))
            {
                return -1;
            }
        }

        return separatorIndex;
    }

    internal bool IsUriSchemeCharacter(char value)
    {
        return IsAsciiLetter(value)
            || IsAsciiDigit(value)
            || value == '+'
            || value == '-'
            || value == '.';
    }

    internal bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z');
    }

    internal bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal bool ContainsWhitespaceOrControl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        for (int index = 0; index < value.Length; index = index + 1)
        {
            if (value[index] <= ' ')
            {
                return true;
            }
        }

        return false;
    }

    internal string GetRelationshipLocation(DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        return relationshipSet.SourcePartName + "#" + relationship.Id;
    }

    internal void ValidatePartNames(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> seenPartNames = new List<string>();
        List<string> reportedPartNames = new List<string>();

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (ContainsName(seenPartNames, part.PartName))
            {
                if (!ContainsName(reportedPartNames, part.PartName))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        DuplicatePartNameCode,
                        "Duplicate package part name.",
                        part.PartName);
                    reportedPartNames.Add(part.PartName);
                }
            }
            else
            {
                seenPartNames.Add(part.PartName);
            }
        }
    }

    internal void ValidateRelationshipSets(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> seenSources = new List<string>();
        List<string> reportedSources = new List<string>();

        for (int relationshipSetIndex = 0; relationshipSetIndex < package.RelationshipSetCount(); relationshipSetIndex = relationshipSetIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(relationshipSetIndex);

            if (ContainsName(seenSources, relationshipSet.SourcePartName))
            {
                if (!ContainsName(reportedSources, relationshipSet.SourcePartName))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        DuplicateRelationshipSetSourceCode,
                        "Duplicate relationship set source.",
                        relationshipSet.SourcePartName);
                    reportedSources.Add(relationshipSet.SourcePartName);
                }
            }
            else
            {
                seenSources.Add(relationshipSet.SourcePartName);
            }

            ValidateRelationshipIds(relationshipSet, report);
        }
    }

    internal void ValidateRelationshipIds(DocxRelationshipSet relationshipSet, DocxValidationReport report)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> seenIds = new List<string>();
        List<string> reportedIds = new List<string>();

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (ContainsName(seenIds, relationship.Id))
            {
                if (!ContainsName(reportedIds, relationship.Id))
                {
                    report.Add(
                        DocxErrorSeverity.Error,
                        DuplicateRelationshipIdCode,
                        "Duplicate relationship id.",
                        relationshipSet.SourcePartName + "#" + relationship.Id);
                    reportedIds.Add(relationship.Id);
                }
            }
            else
            {
                seenIds.Add(relationship.Id);
            }
        }
    }

    internal void ValidateRelationshipPartPresence(IDocxPackageInspector package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        List<string> reportedPartNames = new List<string>();

        for (int relationshipSetIndex = 0; relationshipSetIndex < package.RelationshipSetCount(); relationshipSetIndex = relationshipSetIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(relationshipSetIndex);
            string partName = GetRelationshipPartName(relationshipSet.SourcePartName);

            if (!package.ContainsPart(partName) && !ContainsName(reportedPartNames, partName))
            {
                report.Add(
                    DocxErrorSeverity.Error,
                    MissingRelationshipPartCode,
                    "Relationship set is missing its package part.",
                    partName);
                reportedPartNames.Add(partName);
            }
        }
    }

    internal string GetRelationshipPartName(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName == "/")
        {
            return "/_rels/.rels";
        }

        return DocxRelationshipSet.GetRelationshipPartName(sourcePartName);
    }

    internal DocxValidationReport ValidateWordprocessingStructure(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            ValidateWordprocessingPartStructure(package.GetPart(partIndex), report);
        }

        return report;
    }

    internal void ValidateWordprocessingPartStructure(DocxPart part, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string expectedRoot = GetExpectedRootLocalName(part);

        if (expectedRoot == "" || GetXmlWellFormednessError(part.GetText()) != "")
        {
            return;
        }

        int bodyCount = 0;

        using (StringReader stringReader = new StringReader(part.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                if (reader.MoveToContent() == XmlNodeType.None)
                {
                    AddInvalidPartRoot(part, expectedRoot, report);
                    return;
                }

                if (reader.LocalName != expectedRoot || !IsExpectedRootNamespace(part, reader.NamespaceURI))
                {
                    AddInvalidPartRoot(part, expectedRoot, report);
                    return;
                }

                bool hasNode = true;

                while (hasNode)
                {
                    if (reader.NodeType == XmlNodeType.Element && IsWordprocessingNamespace(reader.NamespaceURI))
                    {
                        if (part.ContentType == DocxConstants.ContentTypeMainDocument
                            || part.ContentType == DocxConstants.ContentTypeTemplateMainDocument
                            || part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                            || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument)
                        {
                            if (reader.LocalName == "body")
                            {
                                bodyCount = bodyCount + 1;
                            }
                        }

                        ValidateRequiredWordprocessingAttributes(part, reader, report);
                    }

                    hasNode = reader.Read();
                }
            }
        }

        if ((expectedRoot == "document") && bodyCount != 1)
        {
            report.Add(
                DocxErrorSeverity.Error,
                DocxDiagnosticCodes.InvalidDocumentBody,
                "The main document must contain exactly one body element.",
                part.PartName);
        }
    }

    internal string GetExpectedRootLocalName(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (part.IsRelationshipPart)
        {
            return "Relationships";
        }

        if (part.ContentType == DocxConstants.ContentTypeMainDocument
            || part.ContentType == DocxConstants.ContentTypeTemplateMainDocument
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument)
        {
            return "document";
        }

        if (part.ContentType == DocxConstants.ContentTypeStyles)
        {
            return "styles";
        }

        if (part.ContentType == DocxConstants.ContentTypeNumbering)
        {
            return "numbering";
        }

        if (part.ContentType == DocxConstants.ContentTypeSettings)
        {
            return "settings";
        }

        if (part.ContentType == DocxConstants.ContentTypeHeader)
        {
            return "hdr";
        }

        if (part.ContentType == DocxConstants.ContentTypeFooter)
        {
            return "ftr";
        }

        if (part.ContentType == DocxConstants.ContentTypeFootnotes)
        {
            return "footnotes";
        }

        if (part.ContentType == DocxConstants.ContentTypeEndnotes)
        {
            return "endnotes";
        }

        if (part.ContentType == DocxConstants.ContentTypeComments)
        {
            return "comments";
        }

        return "";
    }

    internal bool IsExpectedRootNamespace(DocxPart part, string namespaceUri)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (part.IsRelationshipPart)
        {
            return namespaceUri == DocxConstants.NamespacePackageRelationships;
        }

        return IsWordprocessingNamespace(namespaceUri);
    }

    internal bool IsWordprocessingNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        return namespaceUri == DocxConstants.NamespaceWordprocessingMl
            || namespaceUri == DocxConstants.NamespaceStrictWordprocessingMl;
    }

    internal void AddInvalidPartRoot(DocxPart part, string expectedRoot, DocxValidationReport report)
    {
        report.Add(
            DocxErrorSeverity.Error,
            DocxDiagnosticCodes.InvalidPartRoot,
            "Expected root element " + expectedRoot + ".",
            part.PartName);
    }

    internal void ValidateRequiredWordprocessingAttributes(DocxPart part, XmlReader reader, DocxValidationReport report)
    {
        string localName = reader.LocalName;

        if (localName == "style")
        {
            ValidateRequiredAttribute(part, reader, "type", reader.NamespaceURI, report);
            ValidateRequiredAttribute(part, reader, "styleId", reader.NamespaceURI, report);
        }

        if (localName == "abstractNum")
        {
            ValidateRequiredAttribute(part, reader, "abstractNumId", reader.NamespaceURI, report);
        }

        if (localName == "num")
        {
            ValidateRequiredAttribute(part, reader, "numId", reader.NamespaceURI, report);
        }

        if (localName == "footnote" || localName == "endnote" || localName == "comment"
            || localName == "bookmarkStart" || localName == "bookmarkEnd"
            || localName == "commentRangeStart" || localName == "commentRangeEnd"
            || localName == "commentReference")
        {
            ValidateRequiredAttribute(part, reader, "id", reader.NamespaceURI, report);
        }

        if (localName == "compatSetting")
        {
            ValidateRequiredAttribute(part, reader, "name", reader.NamespaceURI, report);
            ValidateRequiredAttribute(part, reader, "uri", reader.NamespaceURI, report);
            ValidateRequiredAttribute(part, reader, "val", reader.NamespaceURI, report);
        }
    }

    internal void ValidateRequiredAttribute(DocxPart part, XmlReader reader, string localName, string namespaceUri, DocxValidationReport report)
    {
        string? value = reader.GetAttribute(localName, namespaceUri);

        if (value == null || value == "")
        {
            report.Add(
                DocxErrorSeverity.Error,
                DocxDiagnosticCodes.MissingRequiredAttribute,
                "Missing required attribute " + localName + ".",
                part.PartName + "#" + reader.LocalName);
        }
    }

    internal DocxValidationReport ValidateIdentifiersAndRanges(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        List<string> styleIds = new List<string>();
        List<string> abstractNumberingIds = new List<string>();
        List<string> numberingIds = new List<string>();
        List<string> footnoteIds = new List<string>();
        List<string> endnoteIds = new List<string>();
        List<string> commentIds = new List<string>();

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            ValidatePartIdentifiersAndRanges(
                package.GetPart(partIndex),
                styleIds,
                abstractNumberingIds,
                numberingIds,
                footnoteIds,
                endnoteIds,
                commentIds,
                report);
        }

        ValidateAbstractNumberingReferences(package, abstractNumberingIds, report);
        return report;
    }

    internal void ValidatePartIdentifiersAndRanges(DocxPart part, List<string> styleIds, List<string> abstractNumberingIds, List<string> numberingIds, List<string> footnoteIds, List<string> endnoteIds, List<string> commentIds, DocxValidationReport report)
    {
        if (!IsXmlPart(part) || GetXmlWellFormednessError(part.GetText()) != "")
        {
            return;
        }

        List<string> bookmarkIds = new List<string>();
        List<string> activeCommentIds = new List<string>();

        using (StringReader stringReader = new StringReader(part.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || !IsWordprocessingNamespace(reader.NamespaceURI))
                    {
                        continue;
                    }

                    string id = "";
                    string code = "";
                    List<string>? ids = null;

                    if (reader.LocalName == "style")
                    {
                        id = ReadAttribute(reader, "styleId", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateStyleId;
                        ids = styleIds;
                    }
                    else if (reader.LocalName == "abstractNum")
                    {
                        id = ReadAttribute(reader, "abstractNumId", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateNumberingId;
                        ids = abstractNumberingIds;
                    }
                    else if (reader.LocalName == "num")
                    {
                        id = ReadAttribute(reader, "numId", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateNumberingId;
                        ids = numberingIds;
                    }
                    else if (reader.LocalName == "footnote")
                    {
                        id = ReadAttribute(reader, "id", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateNoteId;
                        ids = footnoteIds;
                    }
                    else if (reader.LocalName == "endnote")
                    {
                        id = ReadAttribute(reader, "id", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateNoteId;
                        ids = endnoteIds;
                    }
                    else if (reader.LocalName == "comment")
                    {
                        id = ReadAttribute(reader, "id", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateCommentId;
                        ids = commentIds;
                    }
                    else if (reader.LocalName == "bookmarkStart")
                    {
                        id = ReadAttribute(reader, "id", reader.NamespaceURI);
                        code = DocxDiagnosticCodes.DuplicateBookmarkId;
                        ids = bookmarkIds;
                    }

                    if (ids != null)
                    {
                        AddUniqueIdentifier(ids, id, code, part.PartName, report);
                    }

                    ValidateCommentRangeElement(part, reader, activeCommentIds, report);
                }
            }
        }

        foreach (string id in activeCommentIds)
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.UnmatchedCommentRangeStart, "Comment range start is missing a matching end.", part.PartName + "#" + id);
        }
    }

    internal void AddUniqueIdentifier(List<string> ids, string id, string code, string partName, DocxValidationReport report)
    {
        if (id == "")
        {
            return;
        }

        if (ContainsName(ids, id))
        {
            report.Add(DocxErrorSeverity.Error, code, "Duplicate identifier.", partName + "#" + id);
            return;
        }

        ids.Add(id);
    }

    internal void ValidateCommentRangeElement(DocxPart part, XmlReader reader, List<string> activeIds, DocxValidationReport report)
    {
        if (reader.LocalName != "commentRangeStart" && reader.LocalName != "commentRangeEnd")
        {
            return;
        }

        string id = ReadAttribute(reader, "id", reader.NamespaceURI);

        if (id == "")
        {
            return;
        }

        if (reader.LocalName == "commentRangeStart")
        {
            if (!ContainsName(activeIds, id))
            {
                activeIds.Add(id);
            }

            return;
        }

        if (!ContainsName(activeIds, id))
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.UnmatchedCommentRangeEnd, "Comment range end is missing a matching start.", part.PartName + "#" + id);
            return;
        }

        activeIds.Remove(id);
    }

    internal void ValidateAbstractNumberingReferences(IDocxPackageInspector package, List<string> abstractNumberingIds, DocxValidationReport report)
    {
        DocxPart? numberingPart = package.GetNumberingPart();

        if (numberingPart == null)
        {
            for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
            {
                DocxPart candidate = package.GetPart(partIndex);

                if (candidate.ContentType == DocxConstants.ContentTypeNumbering)
                {
                    numberingPart = candidate;
                    break;
                }
            }
        }

        if (numberingPart == null || GetXmlWellFormednessError(numberingPart.GetText()) != "")
        {
            return;
        }

        using (StringReader stringReader = new StringReader(numberingPart.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "abstractNumId"
                        && IsWordprocessingNamespace(reader.NamespaceURI))
                    {
                        string id = ReadAttribute(reader, "val", reader.NamespaceURI);

                        if (id != "" && !ContainsName(abstractNumberingIds, id))
                        {
                            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.MissingAbstractNumberingReference, "Numbering instance refers to a missing abstract numbering definition.", numberingPart.PartName + "#" + id);
                        }
                    }
                }
            }
        }
    }

    internal DocxValidationReport ValidateCompatibility(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (part.ContentType != DocxConstants.ContentTypeSettings || GetXmlWellFormednessError(part.GetText()) != "")
            {
                continue;
            }

            ValidateCompatibilityPart(part, report);
        }

        return report;
    }

    internal void ValidateCompatibilityPart(DocxPart part, DocxValidationReport report)
    {
        using (StringReader stringReader = new StringReader(part.GetText()))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "compatSetting" && IsWordprocessingNamespace(reader.NamespaceURI))
                    {
                        string name = ReadAttribute(reader, "name", reader.NamespaceURI);
                        string value = ReadAttribute(reader, "val", reader.NamespaceURI);

                        if (name == "compatibilityMode" && value != "12" && value != "13" && value != "14" && value != "15")
                        {
                            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.InvalidCompatibilityMode, "Compatibility mode must be 12, 13, 14, or 15.", part.PartName + "#compatibilityMode");
                        }
                    }
                }
            }
        }
    }

    internal DocxValidationReport ValidateNamespaceConformance(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();

        if (DetectNamespaceConformance(package) == DocxNamespaceConformance.Mixed)
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.MixedNamespaceConformance, "The package mixes Strict and Transitional namespace families.", "/");
        }

        return report;
    }

    internal DocxValidationReport ValidateMacroPackage(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        DocxPart? mainPart = null;
        DocxRelationshipSet? packageRelationships = package.GetPackageRelationshipSet();

        if (packageRelationships != null)
        {
            DocxRelationship? mainRelationship = FindFirstMainDocumentRelationship(packageRelationships);

            if (mainRelationship != null)
            {
                string mainPartName = ResolveInternalRelationshipTargetPartName(packageRelationships, mainRelationship);
                mainPart = package.FindPart(mainPartName);
            }
        }
        bool macroMain = false;
        bool hasVbaPart = false;
        bool hasVbaRelationship = false;

        if (mainPart != null)
        {
            macroMain = mainPart.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                || mainPart.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument;
        }

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (part.ContentType == DocxConstants.ContentTypeVbaProject || part.PartName.EndsWith("/vbaProject.bin"))
            {
                hasVbaPart = true;
            }
        }

        for (int setIndex = 0; setIndex < package.RelationshipSetCount(); setIndex = setIndex + 1)
        {
            foreach (DocxRelationship relationship in package.GetRelationshipSet(setIndex).Relationships)
            {
                if (relationship.Type == DocxConstants.RelationshipTypeVbaProject)
                {
                    hasVbaRelationship = true;
                }
            }
        }

        if ((hasVbaPart || hasVbaRelationship) && !macroMain)
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.MacroMainDocumentRequired, "VBA content requires a macro-enabled main-document content type.", mainPart == null ? "/" : mainPart.PartName);
        }

        if (macroMain && !hasVbaPart)
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.MissingVbaProjectPart, "Macro-enabled main document is missing a VBA project part.", "/word/vbaProject.bin");
        }

        if (hasVbaPart && !hasVbaRelationship)
        {
            report.Add(DocxErrorSeverity.Error, DocxDiagnosticCodes.MissingVbaProjectRelationship, "VBA project part is missing its relationship.", "/word/vbaProject.bin");
        }

        return report;
    }

    internal DocxNamespaceConformance DetectNamespaceConformance(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxNamespaceConformance result = DocxNamespaceConformance.Unknown;

        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (IsXmlPart(part))
            {
                result = CombineNamespaceConformance(result, DetectPartNamespaceConformance(part));
            }
        }

        for (int relationshipSetIndex = 0; relationshipSetIndex < package.RelationshipSetCount(); relationshipSetIndex = relationshipSetIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(relationshipSetIndex);
            result = CombineNamespaceConformance(result, DetectRelationshipSetNamespaceConformance(relationshipSet));
        }

        return result;
    }

    internal DocxNamespaceConformance DetectPartNamespaceConformance(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        return DetectXmlNamespaceConformance(part.GetText());
    }

    internal DocxNamespaceConformance DetectXmlNamespaceConformance(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return DocxNamespaceConformance.Unknown;
        }

        if (GetXmlWellFormednessError(xml) != "")
        {
            return DocxNamespaceConformance.Unknown;
        }

        DocxNamespaceConformance result = DocxNamespaceConformance.Unknown;

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    result = CombineNamespaceConformance(result, DetectNamespaceValue(reader.NamespaceURI));

                    if (reader.NodeType == XmlNodeType.Element && reader.HasAttributes)
                    {
                        while (reader.MoveToNextAttribute())
                        {
                            result = CombineNamespaceConformance(result, DetectNamespaceValue(reader.NamespaceURI));
                            result = CombineNamespaceConformance(result, DetectNamespaceValue(reader.Value));
                        }

                        reader.MoveToElement();
                    }
                }
            }
        }

        return result;
    }

    internal DocxNamespaceConformance DetectRelationshipSetNamespaceConformance(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        DocxNamespaceConformance result = DocxNamespaceConformance.Unknown;

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            result = CombineNamespaceConformance(result, DetectNamespaceValue(relationship.Type));
        }

        return result;
    }

    internal DocxNamespaceConformance DetectNamespaceValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (IsStrictNamespace(value) || IsStrictRelationshipType(value))
        {
            return DocxNamespaceConformance.Strict;
        }

        if (IsTransitionalNamespace(value) || IsTransitionalRelationshipType(value))
        {
            return DocxNamespaceConformance.Transitional;
        }

        return DocxNamespaceConformance.Unknown;
    }

    internal DocxNamespaceConformance CombineNamespaceConformance(DocxNamespaceConformance current, DocxNamespaceConformance next)
    {
        if (current == DocxNamespaceConformance.Unknown)
        {
            return next;
        }

        if (next == DocxNamespaceConformance.Unknown)
        {
            return current;
        }

        if (current == next)
        {
            return current;
        }

        return DocxNamespaceConformance.Mixed;
    }

    internal bool IsTransitionalNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        return namespaceUri == DocxConstants.NamespaceRelationships
            || namespaceUri == DocxConstants.NamespaceExtendedProperties
            || namespaceUri == DocxConstants.NamespaceDocPropsVTypes
            || namespaceUri == DocxConstants.NamespaceCustomProperties
            || namespaceUri == DocxConstants.NamespaceWordprocessingMl
            || namespaceUri == DocxConstants.NamespaceDrawingMl
            || namespaceUri == DocxConstants.NamespaceWordprocessingDrawing
            || namespaceUri == DocxConstants.NamespaceDrawingMlPicture
            || namespaceUri == DocxConstants.NamespaceDrawingMlChart
            || namespaceUri == DocxConstants.NamespaceDrawingMlDiagram
            || namespaceUri == DocxConstants.NamespaceVml
            || namespaceUri == DocxConstants.NamespaceOffice;
    }

    internal bool IsStrictNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        return namespaceUri == DocxConstants.NamespaceStrictWordprocessingMl
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMl
            || namespaceUri == DocxConstants.NamespaceStrictWordprocessingDrawing
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlPicture
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlChart
            || namespaceUri == DocxConstants.NamespaceStrictDrawingMlDiagram;
    }

    internal bool IsTransitionalRelationshipType(string relationshipType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        return relationshipType.StartsWith(DocxConstants.RelationshipTypePrefix);
    }

    internal bool AreEquivalentRelationshipTypes(string first, string second)
    {
        if (first == null)
        {
            throw new ArgumentNullException("first");
        }

        if (second == null)
        {
            throw new ArgumentNullException("second");
        }

        return GetTransitionalRelationshipType(first) == GetTransitionalRelationshipType(second);
    }

    internal string GetTransitionalRelationshipType(string relationshipType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (relationshipType.StartsWith(DocxConstants.RelationshipTypeStrictPrefix))
        {
            return DocxConstants.RelationshipTypePrefix
                + relationshipType.Substring(DocxConstants.RelationshipTypeStrictPrefix.Length);
        }

        return relationshipType;
    }

    internal bool IsStrictRelationshipType(string relationshipType)
    {
        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        return relationshipType.StartsWith(DocxConstants.RelationshipTypeStrictPrefix);
    }
}
