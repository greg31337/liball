namespace docx;

internal class DocxRepair
{
    internal const string RepairedContentTypeDefaultCode = DocxDiagnosticCodes.RepairedContentTypeDefault;
    internal const string RepairedPartContentTypeCode = DocxDiagnosticCodes.RepairedPartContentType;
    internal const string RepairedRelationshipSetCode = DocxDiagnosticCodes.RepairedRelationshipSet;
    internal const string RepairedRelationshipCode = DocxDiagnosticCodes.RepairedRelationship;
    internal const string RepairedRelationshipIdCode = DocxDiagnosticCodes.RepairedRelationshipId;
    internal const string RepairedRelationshipPartCode = DocxDiagnosticCodes.RepairedRelationshipPart;
    internal const string RepairedOrphanedRelationshipCode = DocxDiagnosticCodes.RepairedOrphanedRelationship;
    internal const string RepairedOrphanedRelationshipSetCode = DocxDiagnosticCodes.RepairedOrphanedRelationshipSet;
    internal const string RepairedMainDocumentPartCode = DocxDiagnosticCodes.RepairedMainDocumentPart;
    internal const string RepairedBodyCode = DocxDiagnosticCodes.RepairedBody;

    internal DocxPackageValidator Validator;

    internal DocxRepair()
    {
        this.Validator = new DocxPackageValidator();
    }

    internal DocxValidationReport RepairRecoverableDefects(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairEmptyBodyIntoReport(package, report);
        RepairMissingContentTypesIntoReport(package, report);
        RepairMissingRelationshipIdsIntoReport(package, report);
        RepairMissingRelationshipsIntoReport(package, report);
        RepairOrphanedRelationshipsIntoReport(package, report);
        return report;
    }

    internal DocxValidationReport RepairMissingRelationshipIds(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairMissingRelationshipIdsIntoReport(package, report);
        return report;
    }

    internal void RepairMissingRelationshipIdsIntoReport(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        bool repaired = false;

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (relationship.Id == "")
                {
                    relationship.Id = relationshipSet.AllocateRelationshipId();
                    repaired = true;
                    report.Add(
                        DocxErrorSeverity.Information,
                        RepairedRelationshipIdCode,
                        "Assigned missing relationship id.",
                        relationshipSet.SourcePartName + "#" + relationship.Id);
                }
            }
        }

        if (repaired)
        {
            RepairRelationshipParts(package, report);
        }
    }

    internal DocxValidationReport RepairMissingContentTypes(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairMissingContentTypesIntoReport(package, report);
        return report;
    }

    internal void RepairMissingContentTypesIntoReport(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        RepairDefaultContentType(package.ContentTypes, "rels", DocxConstants.ContentTypeRelationships, report);
        RepairDefaultContentType(package.ContentTypes, "xml", DocxConstants.ContentTypeXml, report);
        RepairRelationshipTargetContentTypes(package, report);

        foreach (DocxPart part in package.Parts)
        {
            RepairMissingPartContentType(package, part, report);
        }
    }

    internal DocxValidationReport RepairMissingRelationships(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairMissingRelationshipsIntoReport(package, report);
        return report;
    }

    internal void RepairMissingRelationshipsIntoReport(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        RepairMissingPackageRelationships(package, report);
        RepairKnownPartRelationships(package, report);
        RepairRelationshipParts(package, report);
    }

    internal DocxValidationReport RepairOrphanedRelationships(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairOrphanedRelationshipsIntoReport(package, report);
        return report;
    }

    internal void RepairOrphanedRelationshipsIntoReport(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        RepairOrphanedRelationshipSets(package, report);

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            RepairOrphanedRelationshipsInSet(package, relationshipSet, report);
        }

        RepairRelationshipParts(package, report);
    }

    internal DocxValidationReport RepairEmptyBody(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = new DocxValidationReport();
        RepairEmptyBodyIntoReport(package, report);
        return report;
    }

    internal void RepairEmptyBodyIntoReport(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string mainDocumentPartName = FindMainDocumentPartName(package);

        if (mainDocumentPartName == "")
        {
            mainDocumentPartName = "/word/document.xml";
        }

        DocxPart? part = package.FindPart(mainDocumentPartName);

        if (part == null)
        {
            package.AddXmlPartWithContent(mainDocumentPartName, DocxConstants.ContentTypeMainDocument, CreateRepairedMainDocumentXml());
            report.Add(
                DocxErrorSeverity.Information,
                RepairedMainDocumentPartCode,
                "Added missing main document part.",
                mainDocumentPartName);
            return;
        }

        RepairMainDocumentBody(part, report);
    }

    internal void RepairMainDocumentBody(DocxPart part, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string repairedXml = RepairMainDocumentBodyXml(part.GetText());

        if (repairedXml == part.GetText())
        {
            return;
        }

        part.ReplaceText(repairedXml);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedBodyCode,
            "Repaired empty or missing main document body.",
            part.PartName);
    }

    internal string RepairMainDocumentBodyXml(string documentXml)
    {
        if (documentXml == null)
        {
            throw new ArgumentNullException("documentXml");
        }

        if (documentXml == "")
        {
            return CreateRepairedMainDocumentXml();
        }

        if (this.Validator.GetXmlWellFormednessError(documentXml) != "")
        {
            return documentXml;
        }

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(documentXml);

        if (document.DocumentElement == null)
        {
            return CreateRepairedMainDocumentXml();
        }

        if (document.DocumentElement.LocalName != "document"
            || (document.DocumentElement.NamespaceURI != DocxConstants.NamespaceWordprocessingMl
                && document.DocumentElement.NamespaceURI != DocxConstants.NamespaceStrictWordprocessingMl))
        {
            return CreateRepairedMainDocumentXml();
        }

        System.Xml.XmlElement? body = FindMainDocumentBodyElement(document);

        if (body == null)
        {
            body = CreateBodyElement(document);
            document.DocumentElement.AppendChild(body);
            return document.OuterXml;
        }

        if (!BodyHasContent(body))
        {
            InsertMinimalParagraph(body);
            return document.OuterXml;
        }

        return documentXml;
    }

    internal System.Xml.XmlElement? FindMainDocumentBodyElement(System.Xml.XmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (document.DocumentElement == null)
        {
            return null;
        }

        System.Xml.XmlNodeList bodies = document.GetElementsByTagName("body", document.DocumentElement.NamespaceURI);

        if (bodies.Count == 0)
        {
            return null;
        }

        return bodies[0] as System.Xml.XmlElement;
    }

    internal bool BodyHasContent(System.Xml.XmlElement body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        foreach (System.Xml.XmlNode child in body.ChildNodes)
        {
            if (child.NodeType != System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            if (child.LocalName == "sectPr" && child.NamespaceURI == body.NamespaceURI)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    internal System.Xml.XmlElement CreateBodyElement(System.Xml.XmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        string namespaceUri = DocxConstants.NamespaceWordprocessingMl;

        if (document.DocumentElement != null)
        {
            namespaceUri = document.DocumentElement.NamespaceURI;
        }

        System.Xml.XmlElement body = document.CreateElement("w", "body", namespaceUri);
        body.AppendChild(CreateMinimalParagraphElement(document));
        return body;
    }

    internal void InsertMinimalParagraph(System.Xml.XmlElement body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        if (body.OwnerDocument == null)
        {
            return;
        }

        System.Xml.XmlElement paragraph = CreateMinimalParagraphElement(body.OwnerDocument);
        System.Xml.XmlNode? sectionProperties = FindFirstBodySectionProperties(body);

        if (sectionProperties == null)
        {
            body.AppendChild(paragraph);
            return;
        }

        body.InsertBefore(paragraph, sectionProperties);
    }

    internal System.Xml.XmlNode? FindFirstBodySectionProperties(System.Xml.XmlElement body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        foreach (System.Xml.XmlNode child in body.ChildNodes)
        {
            if (child.NodeType == System.Xml.XmlNodeType.Element
                && child.LocalName == "sectPr"
                && child.NamespaceURI == body.NamespaceURI)
            {
                return child;
            }
        }

        return null;
    }

    internal System.Xml.XmlElement CreateMinimalParagraphElement(System.Xml.XmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        string namespaceUri = DocxConstants.NamespaceWordprocessingMl;

        if (document.DocumentElement != null)
        {
            namespaceUri = document.DocumentElement.NamespaceURI;
        }

        return document.CreateElement("w", "p", namespaceUri);
    }

    internal string CreateRepairedMainDocumentXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "document", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "body", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "p", DocxConstants.NamespaceWordprocessingMl);
        writer.EndElement();
        writer.EndElement();
        writer.EndElement();
        return writer.GetXml();
    }

    internal void RepairOrphanedRelationshipSets(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int index = package.RelationshipSets.Count - 1; index >= 0; index = index - 1)
        {
            DocxRelationshipSet relationshipSet = package.RelationshipSets[index];

            if (relationshipSet.SourcePartName == "/")
            {
                continue;
            }

            if (package.ContainsPart(relationshipSet.SourcePartName))
            {
                continue;
            }

            string relationshipPartName = this.Validator.GetRelationshipPartName(relationshipSet.SourcePartName);
            package.RelationshipSets.RemoveAt(index);
            RemoveRelationshipPart(package, relationshipPartName);
            report.Add(
                DocxErrorSeverity.Information,
                RepairedOrphanedRelationshipSetCode,
                "Removed orphaned relationship set.",
                relationshipSet.SourcePartName);
        }
    }

    internal void RepairOrphanedRelationshipsInSet(DocxPackage package, DocxRelationshipSet relationshipSet, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        for (int index = relationshipSet.Relationships.Count - 1; index >= 0; index = index - 1)
        {
            DocxRelationship relationship = relationshipSet.Relationships[index];

            if (IsOrphanedRelationship(package, relationshipSet, relationship))
            {
                relationshipSet.Relationships.RemoveAt(index);
                report.Add(
                    DocxErrorSeverity.Information,
                    RepairedOrphanedRelationshipCode,
                    "Removed orphaned relationship.",
                    relationshipSet.SourcePartName + "#" + relationship.Id);
            }
        }
    }

    internal bool IsOrphanedRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, DocxRelationship relationship)
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

        if (!this.Validator.IsValidRelationshipTargetMode(relationship.TargetMode))
        {
            return false;
        }

        if (this.Validator.IsExternalRelationshipTargetMode(relationship.TargetMode))
        {
            return false;
        }

        string targetPartName = this.Validator.ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

        if (targetPartName == "")
        {
            return false;
        }

        return !package.ContainsPart(targetPartName);
    }

    internal bool RemoveRelationshipPart(DocxPackage package, string relationshipPartName)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipPartName == null)
        {
            throw new ArgumentNullException("relationshipPartName");
        }

        string normalizedRelationshipPartName = DocxPartName.Normalize(relationshipPartName);

        for (int index = 0; index < package.Parts.Count; index = index + 1)
        {
            DocxPart part = package.Parts[index];

            if (part.PartName == normalizedRelationshipPartName)
            {
                package.Parts.RemoveAt(index);
                package.ContentTypes.RemoveContentTypeForPart(normalizedRelationshipPartName);
                return true;
            }
        }

        return false;
    }

    internal void RepairMissingPackageRelationships(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        DocxRelationshipSet relationshipSet = EnsureRelationshipSet(package, "/", report);
        string mainDocumentPartName = FindMainDocumentPartName(package);

        if (mainDocumentPartName == "")
        {
            return;
        }

        EnsureRelationship(package, relationshipSet, DocxConstants.RelationshipTypeOfficeDocument, mainDocumentPartName, report);
    }

    internal void RepairKnownPartRelationships(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string mainDocumentPartName = FindMainDocumentPartName(package);

        if (mainDocumentPartName == "")
        {
            return;
        }

        if (HasKnownDocumentRelationshipParts(package))
        {
            DocxRelationshipSet documentRelationships = EnsureRelationshipSet(package, mainDocumentPartName, report);

            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeStyles, "/word/styles.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeNumbering, "/word/numbering.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeSettings, "/word/settings.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeWebSettings, "/word/webSettings.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeFontTable, "/word/fontTable.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeTheme, "/word/theme/theme1.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeFootnotes, "/word/footnotes.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeEndnotes, "/word/endnotes.xml", report);
            EnsureKnownRelationship(package, documentRelationships, DocxConstants.RelationshipTypeComments, "/word/comments.xml", report);

            foreach (DocxPart part in package.Parts)
            {
                EnsureKnownHeaderFooterRelationship(package, documentRelationships, part, report);
            }
        }

        EnsureKnownPackageRelationship(package, DocxConstants.RelationshipTypeCoreProperties, "/docProps/core.xml", report);
        EnsureKnownPackageRelationship(package, DocxConstants.RelationshipTypeExtendedProperties, "/docProps/app.xml", report);
        EnsureKnownPackageRelationship(package, DocxConstants.RelationshipTypeCustomProperties, "/docProps/custom.xml", report);
    }

    internal bool HasKnownDocumentRelationshipParts(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (package.ContainsPart("/word/styles.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/numbering.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/settings.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/webSettings.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/fontTable.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/theme/theme1.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/footnotes.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/endnotes.xml"))
        {
            return true;
        }

        if (package.ContainsPart("/word/comments.xml"))
        {
            return true;
        }

        foreach (DocxPart part in package.Parts)
        {
            if (part.PartName.StartsWith("/word/header") && part.PartName.EndsWith(".xml"))
            {
                return true;
            }

            if (part.PartName.StartsWith("/word/footer") && part.PartName.EndsWith(".xml"))
            {
                return true;
            }
        }

        return false;
    }

    internal void EnsureKnownHeaderFooterRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, DocxPart part, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (part.PartName.StartsWith("/word/header") && part.PartName.EndsWith(".xml"))
        {
            EnsureRelationship(package, relationshipSet, DocxConstants.RelationshipTypeHeader, part.PartName, report);
        }

        if (part.PartName.StartsWith("/word/footer") && part.PartName.EndsWith(".xml"))
        {
            EnsureRelationship(package, relationshipSet, DocxConstants.RelationshipTypeFooter, part.PartName, report);
        }
    }

    internal void EnsureKnownRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, string relationshipType, string targetPartName, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (!package.ContainsPart(targetPartName))
        {
            return;
        }

        EnsureRelationship(package, relationshipSet, relationshipType, targetPartName, report);
    }

    internal void EnsureKnownPackageRelationship(DocxPackage package, string relationshipType, string targetPartName, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (!package.ContainsPart(targetPartName))
        {
            return;
        }

        DocxRelationshipSet relationshipSet = EnsureRelationshipSet(package, "/", report);
        EnsureRelationship(package, relationshipSet, relationshipType, targetPartName, report);
    }

    internal DocxRelationshipSet EnsureRelationshipSet(DocxPackage package, string sourcePartName, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        DocxRelationshipSet? existing = null;

        if (sourcePartName == "/")
        {
            existing = package.GetPackageRelationshipSet();
        }
        else
        {
            existing = package.FindRelationshipSet(sourcePartName);
        }

        if (existing != null)
        {
            return existing;
        }

        string normalizedSourcePartName = sourcePartName;

        if (normalizedSourcePartName != "/")
        {
            normalizedSourcePartName = DocxPartName.Normalize(normalizedSourcePartName);
        }

        DocxRelationshipSet relationshipSet = new DocxRelationshipSet(normalizedSourcePartName);
        package.RelationshipSets.Add(relationshipSet);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedRelationshipSetCode,
            "Added missing relationship set.",
            relationshipSet.SourcePartName);
        return relationshipSet;
    }

    internal void EnsureRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, string relationshipType, string targetPartName, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string normalizedTargetPartName = DocxPartName.Normalize(targetPartName);

        if (ContainsInternalRelationship(package, relationshipSet, relationshipType, normalizedTargetPartName))
        {
            return;
        }

        string target = CreateRelationshipTarget(relationshipSet.SourcePartName, normalizedTargetPartName);
        relationshipSet.CreateRelationship(relationshipType, target, false);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedRelationshipCode,
            "Added missing relationship.",
            relationshipSet.SourcePartName + "->" + normalizedTargetPartName);
    }

    internal bool ContainsInternalRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, string relationshipType, string targetPartName)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationshipType == null)
        {
            throw new ArgumentNullException("relationshipType");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        string normalizedTargetPartName = DocxPartName.Normalize(targetPartName);

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.IsExternal)
            {
                continue;
            }

            if (relationship.Type != relationshipType)
            {
                continue;
            }

            string relationshipTargetPartName = this.Validator.ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

            if (relationshipTargetPartName == normalizedTargetPartName)
            {
                return true;
            }
        }

        return false;
    }

    internal void RepairRelationshipParts(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            RepairRelationshipPart(package, relationshipSet, report);
        }
    }

    internal void RepairRelationshipPart(DocxPackage package, DocxRelationshipSet relationshipSet, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string relationshipPartName = this.Validator.GetRelationshipPartName(relationshipSet.SourcePartName);
        string xml = relationshipSet.WriteXml();
        DocxPart? part = package.FindPart(relationshipPartName);

        if (part == null)
        {
            package.Parts.Add(new DocxPart(relationshipPartName, DocxConstants.ContentTypeRelationships, System.Text.Encoding.UTF8.GetBytes(xml), true));
            package.ContentTypes.SetContentTypeForPart(relationshipPartName, DocxConstants.ContentTypeRelationships);
            report.Add(
                DocxErrorSeverity.Information,
                RepairedRelationshipPartCode,
                "Added missing relationship part.",
                relationshipPartName);
            return;
        }

        if (part.GetText() == xml && part.ContentType == DocxConstants.ContentTypeRelationships && part.IsRelationshipPart)
        {
            return;
        }

        part.SetContentType(DocxConstants.ContentTypeRelationships);
        part.ReplaceText(xml);
        part.IsRelationshipPart = true;
        package.ContentTypes.SetContentTypeForPart(relationshipPartName, DocxConstants.ContentTypeRelationships);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedRelationshipPartCode,
            "Updated relationship part.",
            relationshipPartName);
    }

    internal string FindMainDocumentPartName(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        string relatedPartName = package.GetMainDocumentPartName();

        if (relatedPartName != "" && package.ContainsPart(relatedPartName))
        {
            return relatedPartName;
        }

        foreach (DocxPart part in package.Parts)
        {
            if (part.ContentType == DocxConstants.ContentTypeMainDocument
                || part.ContentType == DocxConstants.ContentTypeTemplateMainDocument
                || part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument)
            {
                return part.PartName;
            }
        }

        if (package.ContainsPart("/word/document.xml"))
        {
            return "/word/document.xml";
        }

        foreach (DocxPart part in package.Parts)
        {
            if (part.PartName.StartsWith("/word/") && part.PartName.EndsWith(".xml") && !part.IsRelationshipPart)
            {
                return part.PartName;
            }
        }

        return "";
    }

    internal string CreateRelationshipTarget(string sourcePartName, string targetPartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        string normalizedTargetPartName = DocxPartName.Normalize(targetPartName);

        if (sourcePartName == "/")
        {
            return DocxPartName.ToZipEntryName(normalizedTargetPartName);
        }

        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);
        string sourceBaseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(normalizedSourcePartName);

        if (normalizedTargetPartName.StartsWith(sourceBaseDirectory))
        {
            return normalizedTargetPartName.Substring(sourceBaseDirectory.Length);
        }

        return DocxPartName.ToZipEntryName(normalizedTargetPartName);
    }

    internal void RepairDefaultContentType(DocxContentTypes contentTypes, string extension, string contentType, DocxValidationReport report)
    {
        if (contentTypes == null)
        {
            throw new ArgumentNullException("contentTypes");
        }

        if (extension == null)
        {
            throw new ArgumentNullException("extension");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (contentTypes.GetDefaultContentType(extension) != "")
        {
            return;
        }

        contentTypes.SetDefault(extension, contentType);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedContentTypeDefaultCode,
            "Added missing content type default.",
            extension);
    }

    internal void RepairRelationshipTargetContentTypes(DocxPackage package, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                RepairRelationshipTargetContentType(package, relationshipSet, relationship, report);
            }
        }
    }

    internal void RepairRelationshipTargetContentType(DocxPackage package, DocxRelationshipSet relationshipSet, DocxRelationship relationship, DocxValidationReport report)
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

        if (this.Validator.IsExternalRelationshipTargetMode(relationship.TargetMode))
        {
            return;
        }

        string targetPartName = this.Validator.ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

        if (targetPartName == "")
        {
            return;
        }

        DocxPart? part = package.FindPart(targetPartName);

        if (part == null)
        {
            return;
        }

        string contentType = InferRelationshipTargetContentType(relationship, part);

        if (contentType == "")
        {
            return;
        }

        RepairMissingPartContentTypeWithKnownType(package, part, contentType, report);
    }

    internal void RepairMissingPartContentType(DocxPackage package, DocxPart part, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        string contentType = InferContentType(package, part);

        if (contentType == "")
        {
            return;
        }

        RepairMissingPartContentTypeWithKnownType(package, part, contentType, report);
    }

    internal void RepairMissingPartContentTypeWithKnownType(DocxPackage package, DocxPart part, string contentType, DocxValidationReport report)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (!this.Validator.IsValidContentType(contentType))
        {
            return;
        }

        string overrideContentType = package.ContentTypes.GetOverrideContentType(part.PartName);
        string declaredContentType = package.ContentTypes.GetContentType(part.PartName);

        if (overrideContentType != "")
        {
            return;
        }

        if (declaredContentType != "" && declaredContentType == contentType)
        {
            RepairPartContentTypeMetadata(part, contentType, report);
            return;
        }

        if (declaredContentType != "" && declaredContentType != contentType)
        {
            part.SetContentType(contentType);
            package.ContentTypes.SetContentTypeForPart(part.PartName, contentType);
            report.Add(
                DocxErrorSeverity.Information,
                RepairedPartContentTypeCode,
                "Added missing part content type override.",
                part.PartName);
            return;
        }

        part.SetContentType(contentType);
        package.ContentTypes.SetContentTypeForPart(part.PartName, contentType);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedPartContentTypeCode,
            "Added missing part content type.",
            part.PartName);
    }

    internal void RepairPartContentTypeMetadata(DocxPart part, string contentType, DocxValidationReport report)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (part.ContentType == contentType)
        {
            return;
        }

        part.SetContentType(contentType);
        report.Add(
            DocxErrorSeverity.Information,
            RepairedPartContentTypeCode,
            "Updated part content type metadata.",
            part.PartName);
    }

    internal string InferContentType(DocxPackage package, DocxPart part)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        string relationshipContentType = InferContentTypeFromRelationships(package, part);

        if (relationshipContentType != "")
        {
            return relationshipContentType;
        }

        string partNameContentType = InferContentTypeFromPartName(part.PartName);

        if (partNameContentType != "")
        {
            return partNameContentType;
        }

        string byteContentType = InferContentTypeFromBytes(part);

        if (byteContentType != "")
        {
            return byteContentType;
        }

        if (part.ContentType != null && this.Validator.IsValidContentType(part.ContentType))
        {
            return part.ContentType;
        }

        string extension = DocxContentTypes.GetExtensionFromPartName(part.PartName);
        return InferContentTypeFromExtension(extension);
    }

    internal string InferContentTypeFromRelationships(DocxPackage package, DocxPart part)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (this.Validator.IsExternalRelationshipTargetMode(relationship.TargetMode))
                {
                    continue;
                }

                string targetPartName = this.Validator.ResolveInternalRelationshipTargetPartName(relationshipSet, relationship);

                if (targetPartName == part.PartName)
                {
                    string contentType = InferRelationshipTargetContentType(relationship, part);

                    if (contentType != "")
                    {
                        return contentType;
                    }
                }
            }
        }

        return "";
    }

    internal string InferRelationshipTargetContentType(DocxRelationship relationship, DocxPart part)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (relationship.Type == DocxConstants.RelationshipTypeOfficeDocument)
        {
            if (part.ContentType != null && this.Validator.IsExpectedContentTypeForRelationship(relationship.Type, part.ContentType))
            {
                return part.ContentType;
            }

            return DocxConstants.ContentTypeMainDocument;
        }

        if (relationship.Type == DocxConstants.RelationshipTypeImage)
        {
            return InferImageContentType(part);
        }

        if (relationship.Type == DocxConstants.RelationshipTypeCustomXml)
        {
            return DocxConstants.ContentTypeXml;
        }

        return this.Validator.GetExpectedContentTypeForRelationship(relationship.Type);
    }

    internal string InferContentTypeFromPartName(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (DocxPackage.IsRelationshipPartName(normalizedPartName))
        {
            return DocxConstants.ContentTypeRelationships;
        }

        if (normalizedPartName == "/word/document.xml")
        {
            return DocxConstants.ContentTypeMainDocument;
        }

        if (normalizedPartName == "/word/styles.xml")
        {
            return DocxConstants.ContentTypeStyles;
        }

        if (normalizedPartName == "/word/numbering.xml")
        {
            return DocxConstants.ContentTypeNumbering;
        }

        if (normalizedPartName == "/word/settings.xml")
        {
            return DocxConstants.ContentTypeSettings;
        }

        if (normalizedPartName == "/word/webSettings.xml")
        {
            return DocxConstants.ContentTypeWebSettings;
        }

        if (normalizedPartName == "/word/fontTable.xml")
        {
            return DocxConstants.ContentTypeFontTable;
        }

        if (normalizedPartName == "/word/theme/theme1.xml")
        {
            return DocxConstants.ContentTypeTheme;
        }

        if (normalizedPartName.StartsWith("/word/header") && normalizedPartName.EndsWith(".xml"))
        {
            return DocxConstants.ContentTypeHeader;
        }

        if (normalizedPartName.StartsWith("/word/footer") && normalizedPartName.EndsWith(".xml"))
        {
            return DocxConstants.ContentTypeFooter;
        }

        if (normalizedPartName == "/word/footnotes.xml")
        {
            return DocxConstants.ContentTypeFootnotes;
        }

        if (normalizedPartName == "/word/endnotes.xml")
        {
            return DocxConstants.ContentTypeEndnotes;
        }

        if (normalizedPartName == "/word/comments.xml")
        {
            return DocxConstants.ContentTypeComments;
        }

        if (normalizedPartName == "/docProps/core.xml")
        {
            return DocxConstants.ContentTypeCoreProperties;
        }

        if (normalizedPartName == "/docProps/app.xml")
        {
            return DocxConstants.ContentTypeExtendedProperties;
        }

        if (normalizedPartName == "/docProps/custom.xml")
        {
            return DocxConstants.ContentTypeCustomProperties;
        }

        if (normalizedPartName.StartsWith("/customXml/") && normalizedPartName.EndsWith(".xml"))
        {
            return DocxConstants.ContentTypeXml;
        }

        string extension = DocxContentTypes.GetExtensionFromPartName(normalizedPartName);
        return InferContentTypeFromExtension(extension);
    }

    internal string InferContentTypeFromExtension(string extension)
    {
        if (extension == null)
        {
            throw new ArgumentNullException("extension");
        }

        if (string.Equals(extension, "rels", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypeRelationships;
        }

        if (string.Equals(extension, "xml", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypeXml;
        }

        if (string.Equals(extension, "png", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypePng;
        }

        if (string.Equals(extension, "jpg", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypeJpeg;
        }

        if (string.Equals(extension, "jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypeJpeg;
        }

        if (string.Equals(extension, "gif", StringComparison.OrdinalIgnoreCase))
        {
            return DocxConstants.ContentTypeGif;
        }

        return "";
    }

    internal string InferContentTypeFromBytes(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        return InferImageContentType(part);
    }

    internal string InferImageContentType(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        string detectedContentType = DocxImage.DetectContentType(part.GetBytes());

        if (detectedContentType != "")
        {
            return detectedContentType;
        }

        string extension = DocxContentTypes.GetExtensionFromPartName(part.PartName);

        if (string.Equals(extension, "png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, "jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, "jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, "gif", StringComparison.OrdinalIgnoreCase))
        {
            return InferContentTypeFromExtension(extension);
        }

        return "";
    }
}
