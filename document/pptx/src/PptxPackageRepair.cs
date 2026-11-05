using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxPackageRepair
{
    internal static PptxResult Repair(PptxPackage package)
    {
        PptxResult result = new PptxResult();

        if (package == null)
        {
            result.AddError("PKGR001", "Package is required.", "", "", "");
            return result;
        }

        RepairRequiredContentTypeDefaults(package, result);
        RepairMissingContentTypeOverrides(package, result);
        RepairRelationshipCollections(package, result);
        RepairRelationshipIdentifiers(package, result);
        RemoveUnusedRelationships(package, result);
        RemoveBrokenRelationships(package, result);
        RepairRootPresentationRelationship(package, result);
        return result;
    }

    internal static void RepairRequiredContentTypeDefaults(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        RepairRequiredContentTypeDefault(package, result, "rels", PptxContentTypes.Relationships);
        RepairRequiredContentTypeDefault(package, result, "xml", PptxContentTypes.Xml);
    }

    internal static void RepairRequiredContentTypeDefault(PptxPackage package, PptxResult result, string extension, string contentType)
    {
        int matchingCount = 0;
        bool correct = false;

        for (int index = 0; index < package.ContentTypes.Defaults.Count; index++)
        {
            PptxContentTypeDefault item = package.ContentTypes.Defaults[index];
            if (PptxContentTypesParser.NormalizeExtension(item.Extension) != extension)
            {
                continue;
            }

            matchingCount = matchingCount + 1;
            if (item.ContentType == contentType)
            {
                correct = true;
            }
        }

        if (matchingCount == 1 && correct)
        {
            return;
        }

        for (int index = package.ContentTypes.Defaults.Count - 1; index >= 0; index--)
        {
            if (PptxContentTypesParser.NormalizeExtension(package.ContentTypes.Defaults[index].Extension) == extension)
            {
                package.ContentTypes.Defaults.RemoveAt(index);
            }
        }

        package.ContentTypes.AddDefault(extension, contentType);
        package.MarkDirty();
        result.AddInfo("PKGR005", "Repaired required content type default.", PptxPartNames.ContentTypesEntry, extension, "");
    }

    internal static void RepairMissingContentTypeOverrides(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            string declared = package.ContentTypes.GetContentTypeForPartName(part.PartName);

            if (declared == part.ContentType && !PptxPackageValidator.PartRequiresContentTypeOverride(part, package.ContentTypes))
            {
                continue;
            }

            package.EnsureContentTypeOverride(part.PartName, part.ContentType);
            package.MarkDirty();
            result.AddInfo("PKGR010", "Added missing content type override.", part.PartName, "", "");
        }
    }

    internal static void RepairRelationshipCollections(PptxPackage package, PptxResult result)
    {
        if (package.RootRelationships.SourcePartName.Length > 0)
        {
            package.RootRelationships.SourcePartName = "";
            package.MarkDirty();
            result.AddInfo("PKGR024", "Repaired relationship collection owner metadata.", "", "", "");
        }

        RepairRelationshipCollection(package, package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (part.Relationships.SourcePartName != part.PartName)
            {
                part.Relationships.SourcePartName = part.PartName;
                part.MarkDirty();
                package.MarkDirty();
                result.AddInfo("PKGR024", "Repaired relationship collection owner metadata.", part.PartName, "", "");
            }

            RepairRelationshipCollection(package, part.Relationships, result);
        }
    }

    internal static void RepairRelationshipIdentifiers(PptxPackage package, PptxResult result)
    {
        RepairRelationshipIdentifiersInCollection(package, package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            RepairRelationshipIdentifiersInCollection(package, package.Parts[index].Relationships, result);
        }
    }

    internal static void RepairRelationshipIdentifiersInCollection(PptxPackage package, PptxRelationshipCollection relationships, PptxResult result)
    {
        Dictionary<string, string> reserved = new Dictionary<string, string>();
        Dictionary<string, string> seen = new Dictionary<string, string>();
        Dictionary<string, string> oldIdsSeen = new Dictionary<string, string>();

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            string id = relationships.Relationships[index].Id;
            PptxRelationshipCollection validation = new PptxRelationshipCollection(relationships.SourcePartName);
            if (PptxRelationshipsParser.ValidateRelationshipId(id, validation) && !reserved.ContainsKey(id))
            {
                reserved.Add(id, id);
            }
        }

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            string oldId = relationship.Id;
            PptxRelationshipCollection validation = new PptxRelationshipCollection(relationships.SourcePartName);
            bool valid = PptxRelationshipsParser.ValidateRelationshipId(oldId, validation);
            bool duplicate = oldIdsSeen.ContainsKey(oldId);
            if (!duplicate)
            {
                oldIdsSeen.Add(oldId, oldId);
            }

            if (valid && !seen.ContainsKey(oldId))
            {
                seen.Add(oldId, oldId);
                continue;
            }

            string newId = AllocateRepairRelationshipId(reserved);
            reserved.Add(newId, newId);
            seen.Add(newId, newId);
            relationship.Id = newId;

            if (!duplicate && oldId.Length > 0)
            {
                ReplaceRelationshipIdReferences(package, relationships.SourcePartName, oldId, newId);
            }

            package.MarkRelationshipsDirty(relationships.SourcePartName);
            result.AddInfo("PKGR027", "Allocated a valid unique relationship identifier.", relationships.SourcePartName, newId, oldId);
        }
    }

    internal static string AllocateRepairRelationshipId(Dictionary<string, string> reserved)
    {
        int number = 1;
        string id = "rId" + number.ToString();

        while (reserved.ContainsKey(id))
        {
            number = number + 1;
            id = "rId" + number.ToString();
        }

        return id;
    }

    internal static void RepairRelationshipCollection(PptxPackage package, PptxRelationshipCollection relationships, PptxResult result)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.SourcePartName != relationships.SourcePartName)
            {
                relationship.SourcePartName = relationships.SourcePartName;
                package.MarkRelationshipsDirty(relationships.SourcePartName);
                result.AddInfo("PKGR025", "Repaired relationship source metadata.", relationships.SourcePartName, relationship.Id, "");
            }

            if (relationship.TargetMode != PptxRelationshipTargetMode.Internal)
            {
                continue;
            }

            if (relationship.Target.Length == 0)
            {
                if (relationship.TargetPartName.Length == 0)
                {
                    continue;
                }

                PptxResult partNameResult = PptxUriCore.ValidatePartName(relationship.TargetPartName);
                if (partNameResult.Succeeded())
                {
                    string normalizedPartName = PptxUriCore.NormalizePartName(relationship.TargetPartName);
                    if (normalizedPartName != relationship.TargetPartName)
                    {
                        relationship.TargetPartName = normalizedPartName;
                        package.MarkRelationshipsDirty(relationships.SourcePartName);
                        result.AddInfo("PKGR026", "Repaired resolved relationship target metadata.", relationships.SourcePartName, relationship.Id, normalizedPartName);
                    }
                }

                continue;
            }

            PptxRelationshipCollection validation = new PptxRelationshipCollection(relationships.SourcePartName);
            if (!PptxRelationshipsParser.ValidateInternalTarget(relationships.SourcePartName, relationship.Target, validation, relationship.Id))
            {
                continue;
            }

            string normalized = PptxRelationshipsParser.NormalizeInternalTargetPartName(relationships.SourcePartName, relationship.Target);
            string fragment = PptxUriCore.ExtractFragment(relationship.Target);
            if (relationship.TargetPartName == normalized && relationship.TargetFragment == fragment)
            {
                continue;
            }

            relationship.TargetPartName = normalized;
            relationship.TargetFragment = fragment;
            package.MarkRelationshipsDirty(relationships.SourcePartName);
            result.AddInfo("PKGR026", "Repaired resolved relationship target metadata.", relationships.SourcePartName, relationship.Id, normalized);
        }
    }

    internal static void RemoveBrokenRelationships(PptxPackage package, PptxResult result)
    {
        RemoveBrokenRelationshipsFromCollection(package, package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            RemoveBrokenRelationshipsFromCollection(package, package.Parts[index].Relationships, result);
        }
    }

    internal static void RemoveBrokenRelationshipsFromCollection(PptxPackage package, PptxRelationshipCollection relationships, PptxResult result)
    {
        for (int index = relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                continue;
            }

            if (relationship.TargetPartName.Length > 0 && package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            RemoveRelationshipIdReferences(package, relationships.SourcePartName, relationship.Id);
            relationships.Relationships.RemoveAt(index);
            package.MarkRelationshipsDirty(relationships.SourcePartName);
            result.AddInfo("PKGR030", "Removed broken internal relationship.", relationships.SourcePartName, relationship.Id, relationship.TargetPartName);
        }
    }

    internal static void RepairRootPresentationRelationship(PptxPackage package, PptxResult result)
    {
        int presentationPartCount = 0;
        PptxPart? presentationPart = null;

        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (!PptxPackageValidator.IsPresentationContentType(part.ContentType))
            {
                continue;
            }

            presentationPartCount = presentationPartCount + 1;
            presentationPart = part;
        }

        if (presentationPartCount != 1 || presentationPart == null)
        {
            return;
        }

        PptxRelationship? existing = null;
        int relationshipCount = 0;
        for (int index = 0; index < package.RootRelationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = package.RootRelationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.OfficeDocument)
            {
                relationshipCount = relationshipCount + 1;
                existing = relationship;
            }
        }

        string target = PptxUriCore.ToZipEntryName(presentationPart.PartName);
        if (relationshipCount == 0)
        {
            package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, target, PptxRelationshipTargetMode.Internal);
            result.AddInfo("PKGR040", "Added missing root presentation relationship.", presentationPart.PartName, "", "");
            return;
        }

        if (relationshipCount == 1 && existing != null &&
            (existing.TargetMode != PptxRelationshipTargetMode.Internal || existing.TargetPartName != presentationPart.PartName))
        {
            PptxRelationship replacement = package.CreateRelationship("", existing.Id, existing.Type, target, PptxRelationshipTargetMode.Internal);
            int existingIndex = package.RootRelationships.IndexOfId(existing.Id);
            package.RootRelationships.Relationships[existingIndex] = replacement;
            package.MarkRelationshipsDirty("");
            result.AddInfo("PKGR041", "Retargeted root presentation relationship.", presentationPart.PartName, existing.Id, "");
        }
    }

    internal static void RemoveRelationshipIdReferences(PptxPackage package, string sourcePartName, string relationshipId)
    {
        if (sourcePartName.Length == 0 || !package.HasPart(sourcePartName))
        {
            return;
        }

        PptxPart part = package.GetPart(sourcePartName);
        if (!part.IsXmlContentType())
        {
            return;
        }

        string xml = Encoding.UTF8.GetString(part.GetBytes());
        string prefix = PptxPackageValidator.FindNamespacePrefix(xml, PptxNamespaces.Relationships);
        if (prefix.Length == 0)
        {
            return;
        }

        string updated = RemoveRelationshipReference(xml, prefix + ":id", relationshipId);
        updated = RemoveRelationshipReference(updated, prefix + ":embed", relationshipId);
        updated = RemoveRelationshipReference(updated, prefix + ":link", relationshipId);

        if (updated != xml)
        {
            part.SetBytes(Encoding.UTF8.GetBytes(updated));
            package.MarkDirty();
        }
    }

    internal static void ReplaceRelationshipIdReferences(PptxPackage package, string sourcePartName, string oldId, string newId)
    {
        if (sourcePartName.Length == 0 || !package.HasPart(sourcePartName))
        {
            return;
        }

        PptxPart part = package.GetPart(sourcePartName);
        if (!part.IsXmlContentType())
        {
            return;
        }

        string xml = Encoding.UTF8.GetString(part.GetBytes());
        string prefix = PptxPackageValidator.FindNamespacePrefix(xml, PptxNamespaces.Relationships);
        if (prefix.Length == 0)
        {
            return;
        }

        string updated = ReplaceRelationshipReference(xml, prefix + ":id", oldId, newId);
        updated = ReplaceRelationshipReference(updated, prefix + ":embed", oldId, newId);
        updated = ReplaceRelationshipReference(updated, prefix + ":link", oldId, newId);

        if (updated != xml)
        {
            part.SetBytes(Encoding.UTF8.GetBytes(updated));
            package.MarkDirty();
        }
    }

    internal static string ReplaceRelationshipReference(string xml, string attributeName, string oldId, string newId)
    {
        string updated = xml.Replace(attributeName + "=\"" + oldId + "\"", attributeName + "=\"" + newId + "\"", System.StringComparison.Ordinal);
        return updated.Replace(attributeName + "='" + oldId + "'", attributeName + "='" + newId + "'", System.StringComparison.Ordinal);
    }

    internal static string RemoveRelationshipReference(string xml, string attributeName, string relationshipId)
    {
        string updated = xml.Replace(attributeName + "=\"" + relationshipId + "\"", "", System.StringComparison.Ordinal);
        return updated.Replace(attributeName + "='" + relationshipId + "'", "", System.StringComparison.Ordinal);
    }

    internal static void RemoveUnusedRelationships(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            RemoveUnusedRelationshipsForPart(package, part, result);
        }
    }

    internal static void RemoveUnusedRelationshipsForPart(PptxPackage package, PptxPart part, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (!part.IsXmlContentType())
        {
            return;
        }

        string xml = System.Text.Encoding.UTF8.GetString(part.GetBytes());

        for (int index = part.Relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = part.Relationships.Relationships[index];
            if (!IsXmlReferencedRelationshipType(relationship.Type))
            {
                continue;
            }

            if (ShouldPreserveUnreferencedRelationship(part, relationship))
            {
                continue;
            }

            if (IsRelationshipIdReferencedInXml(xml, relationship.Id))
            {
                continue;
            }

            part.Relationships.Relationships.RemoveAt(index);
            package.MarkRelationshipsDirty(part.PartName);
            result.AddInfo("PKGR020", "Removed unused relationship.", part.PartName, relationship.Id, "");
        }
    }

    internal static bool ShouldPreserveUnreferencedRelationship(PptxPart part, PptxRelationship relationship)
    {
        if (part.ContentType == PptxContentTypes.NotesSlide && relationship.Type == PptxRelationshipTypes.Slide)
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlReferencedRelationshipType(string relationshipType)
    {
        if (relationshipType == PptxRelationshipTypes.Slide)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Image)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Hyperlink)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Chart)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Audio)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Video)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.OleObject)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.Package)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.ActiveXControl)
        {
            return true;
        }

        if (relationshipType == PptxRelationshipTypes.ActiveXControlBinary)
        {
            return true;
        }

        return false;
    }

    internal static bool IsRelationshipIdReferencedInXml(string xml, string relationshipId)
    {
        if (xml == null)
        {
            throw new System.ArgumentNullException("xml");
        }

        if (relationshipId == null)
        {
            throw new System.ArgumentNullException("relationshipId");
        }

        string prefix = PptxPackageValidator.FindNamespacePrefix(xml, PptxNamespaces.Relationships);
        if (prefix.Length == 0)
        {
            return false;
        }

        int index = 0;
        string qualifiedPrefix = prefix + ":";
        while (PptxPackageValidator.FindStartTag(xml, index) >= 0)
        {
            int start = PptxPackageValidator.FindStartTag(xml, index);
            int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
            if (end < 0)
            {
                return false;
            }

            if (PptxPackageValidator.ReadAttributeValue(xml, start + 1, end, qualifiedPrefix + "id") == relationshipId ||
                PptxPackageValidator.ReadAttributeValue(xml, start + 1, end, qualifiedPrefix + "embed") == relationshipId ||
                PptxPackageValidator.ReadAttributeValue(xml, start + 1, end, qualifiedPrefix + "link") == relationshipId)
            {
                return true;
            }

            index = end + 1;
        }

        return false;
    }
}
