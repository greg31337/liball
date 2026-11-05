namespace pptx;

internal static class PptxSecurityScannerCore
{
    internal static PptxResult Scan(PptxPackage package)
    {
        PptxResult result = new PptxResult();

        if (package == null)
        {
            result.AddError("SEC000", "Package is required.", "", "", "");
            return result;
        }

        ScanRelationshipCollection(package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            ScanPart(part, result);
            ScanRelationshipCollection(part.Relationships, result);
        }

        return result;
    }

    internal static void ScanRelationshipCollection(PptxRelationshipCollection relationships, PptxResult result)
    {
        if (relationships == null)
        {
            throw new System.ArgumentNullException("relationships");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            ScanRelationship(relationships.Relationships[index], result);
        }
    }

    internal static void ScanRelationship(PptxRelationship relationship, PptxResult result)
    {
        if (relationship == null)
        {
            throw new System.ArgumentNullException("relationship");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            result.AddWarning("SEC001", "External relationship target: " + relationship.Target, relationship.SourcePartName, relationship.Id, "");
        }

        if (relationship.Type == PptxRelationshipTypes.VbaProject)
        {
            result.AddWarning("SEC010", "VBA project relationship is present.", relationship.SourcePartName, relationship.Id, "");
        }

        if (IsActiveXRelationshipType(relationship.Type))
        {
            result.AddWarning("SEC020", "ActiveX relationship is present.", relationship.SourcePartName, relationship.Id, "");
        }

        if (relationship.Type == PptxRelationshipTypes.OleObject)
        {
            result.AddWarning("SEC030", "OLE object relationship is present.", relationship.SourcePartName, relationship.Id, "");
        }

        if (relationship.Type == PptxRelationshipTypes.Package)
        {
            result.AddWarning("SEC040", "Embedded package relationship is present.", relationship.SourcePartName, relationship.Id, "");
        }

        if (relationship.Type == PptxRelationshipTypes.Audio || relationship.Type == PptxRelationshipTypes.Video)
        {
            result.AddWarning("SEC050", "Media relationship is present.", relationship.SourcePartName, relationship.Id, "");
        }
    }

    internal static void ScanPart(PptxPart part, PptxResult result)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (IsMacroPart(part))
        {
            result.AddWarning("SEC010", "Macro-capable part is present.", part.PartName, "", "");
        }

        if (IsActiveXPart(part))
        {
            result.AddWarning("SEC020", "ActiveX part is present.", part.PartName, "", "");
        }

        if (IsOleObjectPart(part))
        {
            result.AddWarning("SEC030", "OLE object part is present.", part.PartName, "", "");
        }

        if (IsEmbeddedPackagePart(part))
        {
            result.AddWarning("SEC040", "Embedded package part is present.", part.PartName, "", "");
        }

        if (IsMediaPart(part))
        {
            result.AddWarning("SEC050", "Media part is present.", part.PartName, "", "");
        }
    }

    internal static bool IsMacroPart(PptxPart part)
    {
        if (part.ContentType == PptxContentTypes.MacroEnabledPresentation)
        {
            return true;
        }

        if (ContainsText(part.PartName, "vbaProject") || ContainsText(part.ContentType, "vbaProject"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsActiveXPart(PptxPart part)
    {
        if (ContainsText(part.PartName, "/activeX/") || ContainsText(part.ContentType, "activeX"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsOleObjectPart(PptxPart part)
    {
        if (ContainsText(part.PartName, "/embeddings/oleObject") || ContainsText(part.ContentType, "oleObject"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsEmbeddedPackagePart(PptxPart part)
    {
        if (!ContainsText(part.PartName, "/embeddings/"))
        {
            return false;
        }

        if (IsOleObjectPart(part))
        {
            return false;
        }

        return true;
    }

    internal static bool IsMediaPart(PptxPart part)
    {
        if (ContainsText(part.PartName, "/media/"))
        {
            return true;
        }

        if (StartsWithText(part.ContentType, "image/") || StartsWithText(part.ContentType, "audio/") || StartsWithText(part.ContentType, "video/"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsActiveXRelationshipType(string relationshipType)
    {
        if (ContainsText(relationshipType, "activeX"))
        {
            return true;
        }

        if (ContainsText(relationshipType, "/control"))
        {
            return true;
        }

        return false;
    }

    internal static bool ContainsText(string text, string value)
    {
        if (text == null)
        {
            return false;
        }

        if (value == null)
        {
            return false;
        }

        if (text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    internal static bool StartsWithText(string text, string value)
    {
        if (text == null)
        {
            return false;
        }

        if (value == null)
        {
            return false;
        }

        return text.StartsWith(value, System.StringComparison.OrdinalIgnoreCase);
    }
}

