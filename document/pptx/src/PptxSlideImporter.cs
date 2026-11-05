using System.Collections.Generic;
using System.Xml;

namespace pptx;

internal static class PptxSlideImporter
{
    internal static PptxSlide Import(PptxSlideCollection destination, PptxPresentation sourcePresentation, int sourceIndex, int insertIndex)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (sourcePresentation == null)
        {
            throw new System.ArgumentNullException("sourcePresentation");
        }

        destination.ValidateInsertIndex(insertIndex);
        if (destination.Presentation.Part == null)
        {
            throw new System.ArgumentException("Destination presentation part is required.");
        }

        sourcePresentation.SlideCollection.ValidateIndex(sourceIndex);
        PptxSlide source = sourcePresentation.GetSlide(sourceIndex);
        string partName = PptxSlideAllocator.AllocateSlidePartName(destination.Presentation.Package);
        int slideId = PptxSlideAllocator.AllocateSlideId(destination.Presentation);
        string relationshipId = PptxSlideAllocator.AllocateSlideRelationshipId(destination.Presentation);
        byte[] bytes = sourcePresentation.Package.GetPartBytesForSave(source.Part);
        PptxPart part = destination.Presentation.Package.AddPart(partName, source.Part.ContentType, bytes);
        PptxRelationship relationship = destination.Presentation.Package.AddRelationshipWithId(
            destination.Presentation.Part.PartName,
            relationshipId,
            PptxRelationshipTypes.Slide,
            PptxSlideAllocator.GetPresentationTargetForSlidePartName(partName),
            PptxRelationshipTargetMode.Internal);

        Dictionary<string, string> copiedPartNames = new Dictionary<string, string>();
        copiedPartNames.Add(source.PartName, partName);
        CopyRelationships(sourcePresentation, destination.Presentation, source.Part, part, copiedPartNames);
        EnsureImportedSlideMasters(destination.Presentation, copiedPartNames);

        PptxSlide imported = new PptxSlide(destination.Presentation, part, relationship.Id);
        imported.SlideId = slideId;
        imported.Hidden = source.Hidden;
        imported.Layout = FindImportedLayout(destination.Presentation, part);
        destination.Slides.Insert(insertIndex, imported);
        destination.RewritePresentationSlideIdList();
        return imported;
    }

    internal static void CopyRelationships(PptxPresentation sourcePresentation, PptxPresentation destinationPresentation, PptxPart sourcePart, PptxPart targetPart, Dictionary<string, string> copiedPartNames)
    {
        for (int index = 0; index < sourcePart.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = sourcePart.Relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                destinationPresentation.Package.AddRelationshipWithId(targetPart.PartName, relationship.Id, relationship.Type, relationship.Target, relationship.TargetMode);
                continue;
            }

            string targetPartName = CopyDependentPart(sourcePresentation, destinationPresentation, relationship.TargetPartName, copiedPartNames);
            string target = PptxSlideCloner.BuildRelativeTarget(targetPart.PartName, targetPartName, relationship.TargetFragment);
            destinationPresentation.Package.AddRelationshipWithId(targetPart.PartName, relationship.Id, relationship.Type, target, PptxRelationshipTargetMode.Internal);
        }
    }

    internal static string CopyDependentPart(PptxPresentation sourcePresentation, PptxPresentation destinationPresentation, string sourcePartName, Dictionary<string, string> copiedPartNames)
    {
        string normalized = PptxUriCore.NormalizePartName(sourcePartName);
        if (copiedPartNames.ContainsKey(normalized))
        {
            return copiedPartNames[normalized];
        }

        if (!sourcePresentation.Package.HasPart(normalized))
        {
            return normalized;
        }

        PptxPart sourcePart = sourcePresentation.Package.GetPart(normalized);
        string targetPartName = PptxSlideCloner.AllocateSiblingPartName(destinationPresentation.Package, normalized);
        byte[] bytes = sourcePresentation.Package.GetPartBytesForSave(sourcePart);
        PptxPart targetPart = destinationPresentation.Package.AddPart(targetPartName, sourcePart.ContentType, bytes);
        copiedPartNames.Add(normalized, targetPartName);
        CopyRelationships(sourcePresentation, destinationPresentation, sourcePart, targetPart, copiedPartNames);
        return targetPartName;
    }

    internal static void EnsureImportedSlideMasters(PptxPresentation destinationPresentation, Dictionary<string, string> copiedPartNames)
    {
        foreach (KeyValuePair<string, string> item in copiedPartNames)
        {
            if (!destinationPresentation.Package.HasPart(item.Value))
            {
                continue;
            }

            PptxPart part = destinationPresentation.Package.GetPart(item.Value);
            if (part.ContentType != PptxContentTypes.SlideMaster)
            {
                continue;
            }

            if (!PresentationHasRelationshipToPart(destinationPresentation, item.Value, PptxRelationshipTypes.SlideMaster))
            {
                AddSlideMasterRelationship(destinationPresentation, item.Value);
            }
        }
    }

    internal static void AddSlideMasterRelationship(PptxPresentation destinationPresentation, string masterPartName)
    {
        if (destinationPresentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        string relationshipId = destinationPresentation.Package.AllocateRelationshipId(destinationPresentation.Part.Relationships);
        string target = PptxSlideCloner.BuildRelativeTargetPath(destinationPresentation.Part.PartName, masterPartName);
        PptxRelationship relationship = destinationPresentation.Package.AddRelationshipWithId(destinationPresentation.Part.PartName, relationshipId, PptxRelationshipTypes.SlideMaster, target, PptxRelationshipTargetMode.Internal);
        AppendSlideMasterId(destinationPresentation, relationship.Id);
    }

    internal static void AppendSlideMasterId(PptxPresentation destinationPresentation, string relationshipId)
    {
        XmlDocument document = destinationPresentation.SlideCollection.GetPresentationXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Presentation XML root is required.");
        }

        XmlElement list = PptxXml.GetOrCreateChild(root, "p", "sldMasterIdLst", PptxNamespaces.Presentation);
        XmlElement item = PptxXml.AppendChildElement(list, "p", "sldMasterId", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(item, "", "", "id", AllocateSlideMasterId(destinationPresentation).ToString());
        PptxXml.SetAttributeValue(item, "r", PptxNamespaces.Relationships, "id", relationshipId);
        if (destinationPresentation.Part != null)
        {
            destinationPresentation.Part.SetXmlDocument(document);
        }
    }

    internal static long AllocateSlideMasterId(PptxPresentation presentation)
    {
        if (presentation.Part == null)
        {
            return 2147483648L;
        }

        XmlDocument document = presentation.SlideCollection.GetPresentationXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return 2147483648L;
        }

        XmlElement? list = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "sldMasterIdLst");
        long maximum = 2147483647L;
        if (list == null)
        {
            return maximum + 1L;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.Presentation || element.LocalName != "sldMasterId")
            {
                continue;
            }

            long parsed = 0L;
            if (long.TryParse(PptxXml.GetAttributeValue(element, "", "id"), out parsed))
            {
                if (parsed > maximum)
                {
                    maximum = parsed;
                }
            }
        }

        return maximum + 1L;
    }

    internal static bool PresentationHasRelationshipToPart(PptxPresentation presentation, string partName, string relationshipType)
    {
        if (presentation.Part == null)
        {
            return false;
        }

        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < presentation.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = presentation.Part.Relationships.Relationships[index];
            if (relationship.Type == relationshipType && relationship.TargetPartName == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal static PptxSlideLayout? FindImportedLayout(PptxPresentation presentation, PptxPart slidePart)
    {
        for (int index = 0; index < slidePart.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slidePart.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.SlideLayout)
            {
                return presentation.FindSlideLayout(relationship.TargetPartName);
            }
        }

        return null;
    }
}
