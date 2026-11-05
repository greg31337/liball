using System.Text;

namespace pptx;

internal static class PptxCompatibilityInspectorCore
{
    internal static PptxCompatibilityInspection Inspect(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        PptxCompatibilityInspection inspection = new PptxCompatibilityInspection();
        inspection.SecurityDiagnostics = PptxSecurityScannerCore.Scan(package);
        InspectRelationships(package.RootRelationships, inspection);
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            InspectPart(part, inspection);
            InspectRelationships(part.Relationships, inspection);
        }

        return inspection;
    }

    internal static void InspectPart(PptxPart part, PptxCompatibilityInspection inspection)
    {
        PptxCompatibilityLevel level = ClassifyPart(part);
        AddFeature(inspection, PartFeatureName(part, level), level, part.PartName, "", PartDetails(level));
        if (IsXmlPart(part) && ContainsPreservedMarkup(part.GetBytes()))
        {
            AddFeature(
                inspection,
                "Extension markup",
                PptxCompatibilityLevel.PreservedOnly,
                part.PartName,
                "",
                "Alternate-content or extension XML is retained without a complete editable model.");
        }
    }

    internal static void InspectRelationships(PptxRelationshipCollection relationships, PptxCompatibilityInspection inspection)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            PptxCompatibilityLevel level = ClassifyRelationship(relationship);
            if (level == PptxCompatibilityLevel.Editable)
            {
                continue;
            }

            AddFeature(
                inspection,
                RelationshipFeatureName(relationship, level),
                level,
                relationship.SourcePartName,
                relationship.Id,
                RelationshipDetails(relationship, level));
        }
    }

    internal static PptxCompatibilityLevel ClassifyPart(PptxPart part)
    {
        if (IsUnsafePart(part))
        {
            return PptxCompatibilityLevel.Unsafe;
        }

        if (IsUnsupportedText(part.PartName) || IsUnsupportedText(part.ContentType))
        {
            return PptxCompatibilityLevel.Unsupported;
        }

        if (IsEditableContentType(part.ContentType))
        {
            return PptxCompatibilityLevel.Editable;
        }

        if (IsReadableContentType(part.ContentType))
        {
            return PptxCompatibilityLevel.Readable;
        }

        return PptxCompatibilityLevel.PreservedOnly;
    }

    internal static PptxCompatibilityLevel ClassifyRelationship(PptxRelationship relationship)
    {
        if (relationship.TargetMode == PptxRelationshipTargetMode.External || IsUnsafeRelationshipType(relationship.Type))
        {
            return PptxCompatibilityLevel.Unsafe;
        }

        if (IsUnsupportedText(relationship.Type))
        {
            return PptxCompatibilityLevel.Unsupported;
        }

        if (IsKnownRelationshipType(relationship.Type))
        {
            return PptxCompatibilityLevel.Editable;
        }

        return PptxCompatibilityLevel.PreservedOnly;
    }

    internal static bool IsEditableContentType(string contentType)
    {
        if (contentType == PptxContentTypes.Presentation || contentType == PptxContentTypes.PresentationProperties || contentType == PptxContentTypes.ViewProperties || contentType == PptxContentTypes.TableStyles)
        {
            return true;
        }

        if (contentType == PptxContentTypes.Slide || contentType == PptxContentTypes.NotesSlide || contentType == PptxContentTypes.NotesMaster || contentType == PptxContentTypes.HandoutMaster)
        {
            return true;
        }

        if (contentType == PptxContentTypes.CoreProperties || contentType == PptxContentTypes.ExtendedProperties || contentType == PptxContentTypes.CustomProperties || contentType == PptxContentTypes.Tags)
        {
            return true;
        }

        if (contentType == PptxContentTypes.Chart || contentType == PptxContentTypes.ChartStyle || contentType == PptxContentTypes.ChartColorStyle || contentType == PptxContentTypes.Comments || contentType == PptxContentTypes.ModernComments || contentType == PptxContentTypes.CommentAuthors)
        {
            return true;
        }

        if (contentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    internal static bool IsReadableContentType(string contentType)
    {
        if (contentType == PptxContentTypes.SlideMaster || contentType == PptxContentTypes.SlideLayout || contentType == PptxContentTypes.Theme)
        {
            return true;
        }

        if (contentType == PptxContentTypes.DiagramData || contentType == PptxContentTypes.DiagramLayout || contentType == PptxContentTypes.DiagramStyle || contentType == PptxContentTypes.DiagramColors)
        {
            return true;
        }

        if (contentType.StartsWith("audio/", System.StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("video/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    internal static bool IsUnsafePart(PptxPart part)
    {
        if (PptxSecurityScannerCore.IsMacroPart(part) || PptxSecurityScannerCore.IsActiveXPart(part) || PptxSecurityScannerCore.IsOleObjectPart(part) || PptxSecurityScannerCore.IsEmbeddedPackagePart(part))
        {
            return true;
        }

        return false;
    }

    internal static bool IsUnsafeRelationshipType(string relationshipType)
    {
        if (relationshipType == PptxRelationshipTypes.VbaProject || relationshipType == PptxRelationshipTypes.OleObject || relationshipType == PptxRelationshipTypes.Package)
        {
            return true;
        }

        return PptxSecurityScannerCore.IsActiveXRelationshipType(relationshipType);
    }

    internal static bool IsUnsupportedText(string value)
    {
        if (Contains(value, "model3d") || Contains(value, "webextension") || Contains(value, "customui") || Contains(value, "/ink") || Contains(value, ".ink"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsKnownRelationshipType(string type)
    {
        if (type == PptxRelationshipTypes.CoreProperties || type == PptxRelationshipTypes.Thumbnail)
        {
            return true;
        }

        if (type.StartsWith("http://schemas.openxmlformats.org/officeDocument/2006/relationships/", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (type == PptxRelationshipTypes.ChartStyle || type == PptxRelationshipTypes.ChartColorStyle || type == PptxRelationshipTypes.ModernComments || type == PptxRelationshipTypes.ActiveXControl || type == PptxRelationshipTypes.ActiveXControlBinary)
        {
            return true;
        }

        return false;
    }

    internal static bool IsXmlPart(PptxPart part)
    {
        if (part.ContentType.EndsWith("+xml", System.StringComparison.OrdinalIgnoreCase) || part.ContentType.EndsWith("/xml", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return part.PartName.EndsWith(".xml", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsPreservedMarkup(byte[] bytes)
    {
        string xml = Encoding.UTF8.GetString(bytes);
        if (Contains(xml, ":extLst") || Contains(xml, ":AlternateContent") || Contains(xml, "mc:Ignorable"))
        {
            return true;
        }

        return false;
    }

    internal static string PartFeatureName(PptxPart part, PptxCompatibilityLevel level)
    {
        if (level == PptxCompatibilityLevel.Unsafe)
        {
            return "Unsafe embedded or executable part";
        }

        if (level == PptxCompatibilityLevel.Unsupported)
        {
            return "Unsupported package feature";
        }

        if (level == PptxCompatibilityLevel.PreservedOnly)
        {
            return "Unknown package part";
        }

        if (level == PptxCompatibilityLevel.Readable)
        {
            return "Readable presentation part";
        }

        return "Editable presentation part";
    }

    internal static string PartDetails(PptxCompatibilityLevel level)
    {
        if (level == PptxCompatibilityLevel.Editable)
        {
            return "The part has public creation or mutation operations.";
        }

        if (level == PptxCompatibilityLevel.Readable)
        {
            return "The part is represented by a readable model with limited editing support.";
        }

        if (level == PptxCompatibilityLevel.PreservedOnly)
        {
            return "The unmodeled part is retained byte-for-byte when it is not edited.";
        }

        if (level == PptxCompatibilityLevel.Unsupported)
        {
            return "The recognized feature is outside the supported library scope.";
        }

        return "The part can contain executable or embedded content and requires explicit trust.";
    }

    internal static string RelationshipFeatureName(PptxRelationship relationship, PptxCompatibilityLevel level)
    {
        if (level == PptxCompatibilityLevel.Unsafe)
        {
            return "Unsafe external or executable relationship";
        }

        if (level == PptxCompatibilityLevel.Unsupported)
        {
            return "Unsupported relationship feature";
        }

        return "Unknown relationship";
    }

    internal static string RelationshipDetails(PptxRelationship relationship, PptxCompatibilityLevel level)
    {
        if (level == PptxCompatibilityLevel.Unsafe && relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            return "The relationship targets external content and requires explicit trust.";
        }

        if (level == PptxCompatibilityLevel.Unsafe)
        {
            return "The relationship targets executable or embedded content and requires explicit trust.";
        }

        if (level == PptxCompatibilityLevel.Unsupported)
        {
            return "The recognized relationship feature is outside the supported library scope.";
        }

        return "The unmodeled relationship is retained during package round-trip.";
    }

    internal static void AddFeature(PptxCompatibilityInspection inspection, string name, PptxCompatibilityLevel level, string partName, string relationshipId, string details)
    {
        PptxCompatibilityFeature feature = new PptxCompatibilityFeature();
        feature.Name = name;
        feature.Level = level;
        feature.PartName = partName;
        feature.RelationshipId = relationshipId;
        feature.Details = details;
        inspection.Add(feature);
    }

    internal static bool Contains(string text, string value)
    {
        return text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
