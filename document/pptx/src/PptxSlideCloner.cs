using System.Collections.Generic;

namespace pptx;

internal static class PptxSlideCloner
{
    internal static PptxSlide Clone(PptxSlideCollection collection, int sourceIndex, int insertIndex)
    {
        if (collection == null)
        {
            throw new System.ArgumentNullException("collection");
        }

        collection.ValidateIndex(sourceIndex);
        collection.ValidateInsertIndex(insertIndex);
        if (collection.Presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        PptxSlide source = collection.Slides[sourceIndex];
        string partName = PptxSlideAllocator.AllocateSlidePartName(collection.Presentation.Package);
        int slideId = PptxSlideAllocator.AllocateSlideId(collection.Presentation);
        string relationshipId = PptxSlideAllocator.AllocateSlideRelationshipId(collection.Presentation);
        byte[] bytes = collection.Presentation.Package.GetPartBytesForSave(source.Part);
        PptxPart part = collection.Presentation.Package.AddPart(partName, source.Part.ContentType, bytes);
        PptxRelationship relationship = collection.Presentation.Package.AddRelationshipWithId(
            collection.Presentation.Part.PartName,
            relationshipId,
            PptxRelationshipTypes.Slide,
            PptxSlideAllocator.GetPresentationTargetForSlidePartName(partName),
            PptxRelationshipTargetMode.Internal);

        Dictionary<string, string> copiedPartNames = new Dictionary<string, string>();
        copiedPartNames.Add(source.PartName, partName);
        CopyRelationships(collection.Presentation, source.Part, part, copiedPartNames);

        PptxSlide clone = new PptxSlide(collection.Presentation, part, relationship.Id);
        clone.SlideId = slideId;
        clone.Layout = FindSlideLayoutForClone(collection.Presentation, part);
        collection.Slides.Insert(insertIndex, clone);
        collection.RewritePresentationSlideIdList();
        return clone;
    }

    internal static void CopyRelationships(PptxPresentation presentation, PptxPart sourcePart, PptxPart targetPart, Dictionary<string, string> copiedPartNames)
    {
        for (int index = 0; index < sourcePart.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = sourcePart.Relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                presentation.Package.AddRelationshipWithId(targetPart.PartName, relationship.Id, relationship.Type, relationship.Target, relationship.TargetMode);
                continue;
            }

            string targetPartName = relationship.TargetPartName;
            if (!ShouldShareRelationship(relationship))
            {
                targetPartName = CloneDependentPart(presentation, relationship.TargetPartName, copiedPartNames);
            }

            string target = BuildRelativeTarget(targetPart.PartName, targetPartName, relationship.TargetFragment);
            presentation.Package.AddRelationshipWithId(targetPart.PartName, relationship.Id, relationship.Type, target, PptxRelationshipTargetMode.Internal);
        }
    }

    internal static string CloneDependentPart(PptxPresentation presentation, string sourcePartName, Dictionary<string, string> copiedPartNames)
    {
        string normalized = PptxUriCore.NormalizePartName(sourcePartName);
        if (copiedPartNames.ContainsKey(normalized))
        {
            return copiedPartNames[normalized];
        }

        if (!presentation.Package.HasPart(normalized))
        {
            return normalized;
        }

        PptxPart sourcePart = presentation.Package.GetPart(normalized);
        string targetPartName = AllocateSiblingPartName(presentation.Package, normalized);
        byte[] bytes = presentation.Package.GetPartBytesForSave(sourcePart);
        PptxPart targetPart = presentation.Package.AddPart(targetPartName, sourcePart.ContentType, bytes);
        copiedPartNames.Add(normalized, targetPartName);
        CopyRelationships(presentation, sourcePart, targetPart, copiedPartNames);
        return targetPartName;
    }

    internal static bool ShouldShareRelationship(PptxRelationship relationship)
    {
        if (relationship.Type == PptxRelationshipTypes.SlideLayout)
        {
            return true;
        }

        if (relationship.Type == PptxRelationshipTypes.SlideMaster)
        {
            return true;
        }

        if (relationship.Type == PptxRelationshipTypes.Theme)
        {
            return true;
        }

        return false;
    }

    internal static PptxSlideLayout? FindSlideLayoutForClone(PptxPresentation presentation, PptxPart part)
    {
        for (int index = 0; index < part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.SlideLayout)
            {
                return presentation.FindSlideLayout(relationship.TargetPartName);
            }
        }

        return null;
    }

    internal static string AllocateSiblingPartName(PptxPackage package, string sourcePartName)
    {
        string normalized = PptxUriCore.NormalizePartName(sourcePartName);
        string folder = GetFolder(normalized);
        string fileName = GetFileName(normalized);
        string extension = GetExtension(fileName);
        string baseName = fileName.Substring(0, fileName.Length - extension.Length);
        string prefix = RemoveTrailingDigits(baseName);
        if (prefix.Length == 0)
        {
            prefix = baseName;
        }

        int number = 1;
        string candidate = folder + "/" + prefix + number.ToString() + extension;
        while (package.HasPart(candidate))
        {
            number = number + 1;
            candidate = folder + "/" + prefix + number.ToString() + extension;
        }

        return candidate;
    }

    internal static string BuildRelativeTarget(string sourcePartName, string targetPartName, string fragment)
    {
        string relative = BuildRelativeTargetPath(sourcePartName, targetPartName);
        if (fragment.Length > 0)
        {
            return relative + "#" + fragment;
        }

        return relative;
    }

    internal static string BuildRelativeTargetPath(string sourcePartName, string targetPartName)
    {
        string sourceFolder = GetFolder(PptxUriCore.NormalizePartName(sourcePartName));
        string target = PptxUriCore.NormalizePartName(targetPartName);
        string[] sourceSegments = SplitSegments(sourceFolder);
        string[] targetSegments = SplitSegments(target);
        int common = 0;

        while (common < sourceSegments.Length && common < targetSegments.Length && sourceSegments[common] == targetSegments[common])
        {
            common = common + 1;
        }

        string result = "";
        for (int index = common; index < sourceSegments.Length; index++)
        {
            result = result + "../";
        }

        for (int index = common; index < targetSegments.Length; index++)
        {
            if (index > common)
            {
                result = result + "/";
            }

            result = result + targetSegments[index];
        }

        if (result.Length == 0)
        {
            return GetFileName(target);
        }

        return result;
    }

    internal static string[] SplitSegments(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        string trimmed = normalized;
        if (trimmed.StartsWith("/", System.StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.Length == 0)
        {
            return new string[0];
        }

        return trimmed.Split('/');
    }

    internal static string GetFolder(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        int slash = normalized.LastIndexOf('/');
        if (slash <= 0)
        {
            return "";
        }

        return normalized.Substring(0, slash);
    }

    internal static string GetFileName(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        int slash = normalized.LastIndexOf('/');
        if (slash < 0)
        {
            return normalized;
        }

        return normalized.Substring(slash + 1);
    }

    internal static string GetExtension(string fileName)
    {
        int dot = fileName.LastIndexOf('.');
        if (dot < 0)
        {
            return "";
        }

        return fileName.Substring(dot);
    }

    internal static string RemoveTrailingDigits(string value)
    {
        int end = value.Length;
        while (end > 0 && value[end - 1] >= '0' && value[end - 1] <= '9')
        {
            end = end - 1;
        }

        return value.Substring(0, end);
    }
}
