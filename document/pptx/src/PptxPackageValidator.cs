using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxPackageValidator
{
    internal static PptxResult Validate(PptxPackage package)
    {
        PptxResult result = new PptxResult();

        if (package == null)
        {
            result.AddError("PKGV001", "Package is required.", "", "", "");
            return result;
        }

        PptxUriCore.CopyDiagnostics(package.Diagnostics, result);
        ValidateContentTypes(package, result);
        ValidateRootPresentationRelationship(package, result);
        ValidateRelationshipMetadata(package, result);
        ValidateInternalRelationshipTargets(package, result);
        ValidateXmlParts(package, result);
        ValidateMinimumPartStructures(package, result);
        ValidateXmlIdentifiersAndReferences(package, result);
        ValidateRequiredPresentationRelationships(package, result);
        return result;
    }

    internal static void ValidateXmlParts(PptxPackage package, PptxResult result)
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
            PptxXmlValidator.ValidatePart(package.Parts[index], result);
        }
    }

    internal static void ValidateContentTypes(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        PptxResult manifestResult = PptxContentTypesWriter.ValidateManifest(package.ContentTypes);
        PptxUriCore.CopyDiagnostics(manifestResult, result);

        ValidateRequiredContentTypeDefault(package.ContentTypes, result, "rels", PptxContentTypes.Relationships);
        ValidateRequiredContentTypeDefault(package.ContentTypes, result, "xml", PptxContentTypes.Xml);
        ValidatePartContentTypeDeclarations(package, result);
        ValidateContentTypeOverridesTargetExistingParts(package, result);
    }

    internal static void ValidateRequiredContentTypeDefault(PptxContentTypeManifest manifest, PptxResult result, string extension, string contentType)
    {
        if (manifest == null)
        {
            throw new System.ArgumentNullException("manifest");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        string existing = manifest.GetDefaultContentType(extension);
        if (existing.Length == 0)
        {
            result.AddError("PKGV030", "Package is missing a required content type default.", PptxPartNames.ContentTypesEntry, extension, "");
            return;
        }

        if (existing != contentType)
        {
            result.AddError("PKGV031", "Required content type default has the wrong content type.", PptxPartNames.ContentTypesEntry, extension, "");
        }
    }

    internal static void ValidatePartContentTypeDeclarations(PptxPackage package, PptxResult result)
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

            if (declared.Length == 0)
            {
                result.AddError("PKGV032", "Part is missing a content type declaration.", part.PartName, "", "");
                continue;
            }

            if (declared != part.ContentType)
            {
                result.AddError("PKGV033", "Part content type declaration does not match the loaded part content type.", part.PartName, "", "");
            }

            if (PartRequiresContentTypeOverride(part, package.ContentTypes))
            {
                result.AddError("PKGV034", "Part requires a content type override.", part.PartName, "", "");
            }
        }
    }

    internal static void ValidateContentTypeOverridesTargetExistingParts(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        for (int index = 0; index < package.ContentTypes.Overrides.Count; index++)
        {
            PptxContentTypeOverride item = package.ContentTypes.Overrides[index];
            if (!package.HasPart(item.PartName))
            {
                result.AddError("PKGV035", "Content type override points to a missing part.", item.PartName, "", "");
            }
        }
    }

    internal static bool PartRequiresContentTypeOverride(PptxPart part, PptxContentTypeManifest manifest)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (manifest == null)
        {
            throw new System.ArgumentNullException("manifest");
        }

        if (manifest.GetOverrideContentType(part.PartName).Length > 0)
        {
            return false;
        }

        string extension = PptxContentTypesParser.GetPartNameExtension(part.PartName);
        if (extension.Length == 0)
        {
            return false;
        }

        string defaultContentType = manifest.GetDefaultContentType(extension);
        if (defaultContentType.Length == 0)
        {
            return false;
        }

        if (defaultContentType == part.ContentType)
        {
            return false;
        }

        return true;
    }

    internal static void ValidateRootPresentationRelationship(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        PptxRelationship? presentationRelationship = null;
        int presentationRelationshipCount = 0;

        for (int index = 0; index < package.RootRelationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = package.RootRelationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.OfficeDocument)
            {
                presentationRelationshipCount = presentationRelationshipCount + 1;
                if (presentationRelationship == null)
                {
                    presentationRelationship = relationship;
                }
            }
        }

        if (presentationRelationshipCount == 0)
        {
            result.AddError("PKGV010", "Package is missing a root presentation relationship.", "", "", "");
            return;
        }

        if (presentationRelationshipCount > 1)
        {
            result.AddError("PKGV014", "Package has multiple root presentation relationships.", "", "", "");
        }

        if (presentationRelationship == null)
        {
            return;
        }

        if (presentationRelationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            result.AddError("PKGV011", "Root presentation relationship must target an internal part.", "", presentationRelationship.Id, "");
            return;
        }

        if (presentationRelationship.TargetPartName.Length == 0 || !package.HasPart(presentationRelationship.TargetPartName))
        {
            result.AddError("PKGV012", "Root presentation relationship target part is missing.", presentationRelationship.TargetPartName, presentationRelationship.Id, "");
            return;
        }

        PptxPart part = package.GetPart(presentationRelationship.TargetPartName);
        if (!IsPresentationContentType(part.ContentType))
        {
            result.AddError("PKGV013", "Root presentation relationship target must be a presentation part.", part.PartName, presentationRelationship.Id, "");
        }
    }

    internal static void ValidateRelationshipMetadata(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        ValidateRelationshipCollectionMetadata(package, package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            ValidateRelationshipCollectionMetadata(package, package.Parts[index].Relationships, result);
        }
    }

    internal static void ValidateRelationshipCollectionMetadata(PptxPackage package, PptxRelationshipCollection relationships, PptxResult result)
    {
        Dictionary<string, string> seenIds = new Dictionary<string, string>();

        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            PptxRelationshipCollection validation = new PptxRelationshipCollection(relationships.SourcePartName);

            if (relationship.SourcePartName != relationships.SourcePartName)
            {
                result.AddError("PKGV022", "Relationship source does not match its owning relationship collection.", relationships.SourcePartName, relationship.Id, "");
            }

            bool validId = PptxRelationshipsParser.ValidateRelationshipId(relationship.Id, validation);
            if (validId)
            {
                if (seenIds.ContainsKey(relationship.Id))
                {
                    result.AddError("PKGV060", "Relationship identifier is duplicated within its owning part.", relationships.SourcePartName, relationship.Id, "");
                }
                else
                {
                    seenIds.Add(relationship.Id, relationship.Id);
                }
            }

            PptxRelationshipsParser.ValidateRelationshipType(relationship.Type, validation);

            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                ValidateExternalRelationshipMetadata(relationship, validation, result);
            }
            else
            {
                ValidateInternalRelationshipMetadata(relationship, relationships.SourcePartName, validation, result);
            }

            PptxUriCore.CopyDiagnostics(validation.Diagnostics, result);
            ValidateKnownRelationshipTarget(package, relationship, result);
        }
    }

    internal static void ValidateExternalRelationshipMetadata(PptxRelationship relationship, PptxRelationshipCollection validation, PptxResult result)
    {
        if (string.IsNullOrWhiteSpace(relationship.Target))
        {
            validation.Diagnostics.AddError("REL014", "Relationship Target is required.", relationship.SourcePartName, relationship.Id, "");
        }
        else
        {
            PptxRelationshipsParser.ValidateExternalTarget(relationship.Target, validation, relationship.Id);
        }

        if (RelationshipTypeRequiresInternalTarget(relationship.Type))
        {
            result.AddError("PKGV070", "Relationship type requires an internal package target.", relationship.SourcePartName, relationship.Id, "");
        }

        if (relationship.TargetPartName.Length > 0)
        {
            result.AddError("PKGV023", "External relationship must not retain a resolved package part name.", relationship.SourcePartName, relationship.Id, "");
        }
    }

    internal static void ValidateInternalRelationshipMetadata(PptxRelationship relationship, string sourcePartName, PptxRelationshipCollection validation, PptxResult result)
    {
        if (!PptxRelationshipsWriter.ValidateInternalRelationshipForWrite(sourcePartName, relationship, validation))
        {
            return;
        }

        if (relationship.Target.Length == 0)
        {
            return;
        }

        string normalized = PptxRelationshipsParser.NormalizeInternalTargetPartName(sourcePartName, relationship.Target);
        if (normalized != relationship.TargetPartName)
        {
            result.AddError("PKGV023", "Resolved relationship target does not match the stored target.", sourcePartName, relationship.Id, "");
        }
    }

    internal static void ValidateKnownRelationshipTarget(PptxPackage package, PptxRelationship relationship, PptxResult result)
    {
        if (relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            return;
        }

        if (relationship.TargetPartName.Length == 0 || !package.HasPart(relationship.TargetPartName))
        {
            return;
        }

        PptxPart targetPart = package.GetPart(relationship.TargetPartName);
        if (!RelationshipTargetMatchesContentType(relationship.Type, targetPart.ContentType))
        {
            result.AddError("PKGV071", "Relationship target content type is not valid for the relationship type.", relationship.SourcePartName, relationship.Id, "");
        }
    }

    internal static bool RelationshipTypeRequiresInternalTarget(string relationshipType)
    {
        if (relationshipType == PptxRelationshipTypes.Hyperlink ||
            relationshipType == PptxRelationshipTypes.Image ||
            relationshipType == PptxRelationshipTypes.Audio ||
            relationshipType == PptxRelationshipTypes.Video ||
            relationshipType == PptxRelationshipTypes.OleObject ||
            relationshipType == PptxRelationshipTypes.Package)
        {
            return false;
        }

        if (IsKnownRelationshipType(relationshipType))
        {
            return true;
        }

        return false;
    }

    internal static bool IsKnownRelationshipType(string relationshipType)
    {
        if (relationshipType == PptxRelationshipTypes.OfficeDocument ||
            relationshipType == PptxRelationshipTypes.CoreProperties ||
            relationshipType == PptxRelationshipTypes.ExtendedProperties ||
            relationshipType == PptxRelationshipTypes.CustomProperties ||
            relationshipType == PptxRelationshipTypes.Thumbnail ||
            relationshipType == PptxRelationshipTypes.PresentationProperties ||
            relationshipType == PptxRelationshipTypes.ViewProperties ||
            relationshipType == PptxRelationshipTypes.TableStyles ||
            relationshipType == PptxRelationshipTypes.Slide ||
            relationshipType == PptxRelationshipTypes.SlideMaster ||
            relationshipType == PptxRelationshipTypes.SlideLayout ||
            relationshipType == PptxRelationshipTypes.Theme ||
            relationshipType == PptxRelationshipTypes.NotesSlide ||
            relationshipType == PptxRelationshipTypes.NotesMaster ||
            relationshipType == PptxRelationshipTypes.HandoutMaster ||
            relationshipType == PptxRelationshipTypes.Tags ||
            relationshipType == PptxRelationshipTypes.CustomXml ||
            relationshipType == PptxRelationshipTypes.Image ||
            relationshipType == PptxRelationshipTypes.Hyperlink ||
            relationshipType == PptxRelationshipTypes.Chart ||
            relationshipType == PptxRelationshipTypes.ChartStyle ||
            relationshipType == PptxRelationshipTypes.ChartColorStyle ||
            relationshipType == PptxRelationshipTypes.DiagramData ||
            relationshipType == PptxRelationshipTypes.DiagramLayout ||
            relationshipType == PptxRelationshipTypes.DiagramQuickStyle ||
            relationshipType == PptxRelationshipTypes.DiagramColors ||
            relationshipType == PptxRelationshipTypes.Comments ||
            relationshipType == PptxRelationshipTypes.ModernComments ||
            relationshipType == PptxRelationshipTypes.CommentAuthors ||
            relationshipType == PptxRelationshipTypes.VbaProject ||
            relationshipType == PptxRelationshipTypes.Audio ||
            relationshipType == PptxRelationshipTypes.Video ||
            relationshipType == PptxRelationshipTypes.OleObject ||
            relationshipType == PptxRelationshipTypes.Package ||
            relationshipType == PptxRelationshipTypes.ActiveXControl ||
            relationshipType == PptxRelationshipTypes.ActiveXControlBinary)
        {
            return true;
        }

        return false;
    }

    internal static bool RelationshipTargetMatchesContentType(string relationshipType, string contentType)
    {
        if (relationshipType == PptxRelationshipTypes.OfficeDocument)
        {
            return IsPresentationContentType(contentType);
        }

        if (relationshipType == PptxRelationshipTypes.CoreProperties)
        {
            return contentType == PptxContentTypes.CoreProperties;
        }

        if (relationshipType == PptxRelationshipTypes.ExtendedProperties)
        {
            return contentType == PptxContentTypes.ExtendedProperties;
        }

        if (relationshipType == PptxRelationshipTypes.CustomProperties)
        {
            return contentType == PptxContentTypes.CustomProperties;
        }

        if (relationshipType == PptxRelationshipTypes.Thumbnail || relationshipType == PptxRelationshipTypes.Image)
        {
            return contentType.StartsWith("image/", System.StringComparison.Ordinal);
        }

        if (relationshipType == PptxRelationshipTypes.CustomXml)
        {
            return contentType == PptxContentTypes.Xml || contentType.EndsWith("+xml", System.StringComparison.Ordinal);
        }

        if (relationshipType == PptxRelationshipTypes.PresentationProperties)
        {
            return contentType == PptxContentTypes.PresentationProperties;
        }

        if (relationshipType == PptxRelationshipTypes.ViewProperties)
        {
            return contentType == PptxContentTypes.ViewProperties;
        }

        if (relationshipType == PptxRelationshipTypes.TableStyles)
        {
            return contentType == PptxContentTypes.TableStyles;
        }

        if (relationshipType == PptxRelationshipTypes.Slide)
        {
            return contentType == PptxContentTypes.Slide;
        }

        if (relationshipType == PptxRelationshipTypes.SlideMaster)
        {
            return contentType == PptxContentTypes.SlideMaster;
        }

        if (relationshipType == PptxRelationshipTypes.SlideLayout)
        {
            return contentType == PptxContentTypes.SlideLayout;
        }

        if (relationshipType == PptxRelationshipTypes.Theme)
        {
            return contentType == PptxContentTypes.Theme;
        }

        if (relationshipType == PptxRelationshipTypes.NotesSlide)
        {
            return contentType == PptxContentTypes.NotesSlide;
        }

        if (relationshipType == PptxRelationshipTypes.NotesMaster)
        {
            return contentType == PptxContentTypes.NotesMaster;
        }

        if (relationshipType == PptxRelationshipTypes.HandoutMaster)
        {
            return contentType == PptxContentTypes.HandoutMaster;
        }

        if (relationshipType == PptxRelationshipTypes.Tags)
        {
            return contentType == PptxContentTypes.Tags;
        }

        if (relationshipType == PptxRelationshipTypes.Chart)
        {
            return contentType == PptxContentTypes.Chart;
        }

        if (relationshipType == PptxRelationshipTypes.ChartStyle)
        {
            return contentType == PptxContentTypes.ChartStyle;
        }

        if (relationshipType == PptxRelationshipTypes.ChartColorStyle)
        {
            return contentType == PptxContentTypes.ChartColorStyle;
        }

        if (relationshipType == PptxRelationshipTypes.DiagramData)
        {
            return contentType == PptxContentTypes.DiagramData;
        }

        if (relationshipType == PptxRelationshipTypes.DiagramLayout)
        {
            return contentType == PptxContentTypes.DiagramLayout;
        }

        if (relationshipType == PptxRelationshipTypes.DiagramQuickStyle)
        {
            return contentType == PptxContentTypes.DiagramStyle;
        }

        if (relationshipType == PptxRelationshipTypes.DiagramColors)
        {
            return contentType == PptxContentTypes.DiagramColors;
        }

        if (relationshipType == PptxRelationshipTypes.Comments)
        {
            return contentType == PptxContentTypes.Comments;
        }

        if (relationshipType == PptxRelationshipTypes.ModernComments)
        {
            return contentType == PptxContentTypes.ModernComments;
        }

        if (relationshipType == PptxRelationshipTypes.CommentAuthors)
        {
            return contentType == PptxContentTypes.CommentAuthors;
        }

        if (relationshipType == PptxRelationshipTypes.VbaProject)
        {
            return contentType == PptxContentTypes.VbaProject;
        }

        if (relationshipType == PptxRelationshipTypes.Audio)
        {
            return contentType.StartsWith("audio/", System.StringComparison.Ordinal);
        }

        if (relationshipType == PptxRelationshipTypes.Video)
        {
            return contentType.StartsWith("video/", System.StringComparison.Ordinal);
        }

        if (relationshipType == PptxRelationshipTypes.OleObject)
        {
            return contentType == PptxContentTypes.OleObject;
        }

        if (relationshipType == PptxRelationshipTypes.ActiveXControl)
        {
            return contentType == PptxContentTypes.ActiveX;
        }

        if (relationshipType == PptxRelationshipTypes.ActiveXControlBinary)
        {
            return contentType == PptxContentTypes.ActiveXBinary;
        }

        return true;
    }

    internal static void ValidateInternalRelationshipTargets(PptxPackage package, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        ValidateRelationshipCollectionTargets(package, package.RootRelationships, result);

        for (int index = 0; index < package.Parts.Count; index++)
        {
            ValidateRelationshipCollectionTargets(package, package.Parts[index].Relationships, result);
        }
    }

    internal static void ValidateRelationshipCollectionTargets(PptxPackage package, PptxRelationshipCollection relationships, PptxResult result)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

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
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                continue;
            }

            if (relationship.TargetPartName.Length == 0)
            {
                result.AddError("PKGV021", "Internal relationship target part name is missing.", relationships.SourcePartName, relationship.Id, "");
                continue;
            }

            if (!package.HasPart(relationship.TargetPartName))
            {
                result.AddError("PKGV020", "Internal relationship target part is missing.", relationships.SourcePartName, relationship.Id, "");
            }
        }
    }

    internal static void ValidateMinimumPartStructures(PptxPackage package, PptxResult result)
    {
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (!part.IsXmlContentType())
            {
                continue;
            }

            PptxResult syntax = new PptxResult();
            PptxXmlValidator.ValidatePart(part, syntax);
            if (syntax.Failed())
            {
                continue;
            }

            ValidateMinimumPartStructure(part, result);
        }
    }

    internal static void ValidateMinimumPartStructure(PptxPart part, PptxResult result)
    {
        string xml = Encoding.UTF8.GetString(part.GetBytes());
        string expectedRoot = ExpectedRootElement(part.ContentType);
        if (expectedRoot.Length == 0)
        {
            return;
        }

        string actualRoot = ReadRootElementLocalName(xml);
        if (actualRoot != expectedRoot)
        {
            result.AddError("PKGV050", "Part does not have the required document element for its content type.", part.PartName, "", "/" + actualRoot);
            return;
        }

        string expectedNamespace = ExpectedRootNamespace(part.ContentType);
        string actualNamespace = ReadRootElementNamespace(xml);
        if (expectedNamespace.Length > 0 && actualNamespace != expectedNamespace)
        {
            result.AddError("PKGV053", "Part document element has the wrong namespace for its content type.", part.PartName, "", "/" + actualRoot);
        }

        if (IsPresentationContentType(part.ContentType))
        {
            RequireElement(xml, part.PartName, "sldMasterIdLst", result);
            RequireElement(xml, part.PartName, "sldSz", result);
            RequireElement(xml, part.PartName, "notesSz", result);
            return;
        }

        if (part.ContentType == PptxContentTypes.Slide)
        {
            RequireCommonSlideData(xml, part.PartName, result);
            return;
        }

        if (part.ContentType == PptxContentTypes.SlideMaster)
        {
            RequireCommonSlideData(xml, part.PartName, result);
            RequireElement(xml, part.PartName, "clrMap", result);
            RequireElement(xml, part.PartName, "sldLayoutIdLst", result);
            return;
        }

        if (part.ContentType == PptxContentTypes.SlideLayout)
        {
            RequireCommonSlideData(xml, part.PartName, result);
            return;
        }

        if (part.ContentType == PptxContentTypes.NotesSlide)
        {
            RequireCommonSlideData(xml, part.PartName, result);
            return;
        }

        if (part.ContentType == PptxContentTypes.NotesMaster || part.ContentType == PptxContentTypes.HandoutMaster)
        {
            RequireCommonSlideData(xml, part.PartName, result);
            RequireElement(xml, part.PartName, "clrMap", result);
            return;
        }

        if (part.ContentType == PptxContentTypes.Theme)
        {
            RequireElement(xml, part.PartName, "themeElements", result);
            RequireElement(xml, part.PartName, "clrScheme", result);
            RequireElement(xml, part.PartName, "fontScheme", result);
            RequireElement(xml, part.PartName, "fmtScheme", result);
            return;
        }

        if (part.ContentType == PptxContentTypes.Chart)
        {
            RequireElement(xml, part.PartName, "chart", result);
        }
    }

    internal static string ExpectedRootElement(string contentType)
    {
        if (IsPresentationContentType(contentType))
        {
            return "presentation";
        }

        if (contentType == PptxContentTypes.Slide)
        {
            return "sld";
        }

        if (contentType == PptxContentTypes.SlideMaster)
        {
            return "sldMaster";
        }

        if (contentType == PptxContentTypes.SlideLayout)
        {
            return "sldLayout";
        }

        if (contentType == PptxContentTypes.NotesSlide)
        {
            return "notes";
        }

        if (contentType == PptxContentTypes.NotesMaster)
        {
            return "notesMaster";
        }

        if (contentType == PptxContentTypes.HandoutMaster)
        {
            return "handoutMaster";
        }

        if (contentType == PptxContentTypes.Theme)
        {
            return "theme";
        }

        if (contentType == PptxContentTypes.Chart)
        {
            return "chartSpace";
        }

        if (contentType == PptxContentTypes.PresentationProperties)
        {
            return "presentationPr";
        }

        if (contentType == PptxContentTypes.ViewProperties)
        {
            return "viewPr";
        }

        if (contentType == PptxContentTypes.TableStyles)
        {
            return "tblStyleLst";
        }

        if (contentType == PptxContentTypes.CoreProperties)
        {
            return "coreProperties";
        }

        if (contentType == PptxContentTypes.ExtendedProperties || contentType == PptxContentTypes.CustomProperties)
        {
            return "Properties";
        }

        return "";
    }

    internal static string ExpectedRootNamespace(string contentType)
    {
        if (IsPresentationContentType(contentType) ||
            contentType == PptxContentTypes.Slide ||
            contentType == PptxContentTypes.SlideMaster ||
            contentType == PptxContentTypes.SlideLayout ||
            contentType == PptxContentTypes.NotesSlide ||
            contentType == PptxContentTypes.NotesMaster ||
            contentType == PptxContentTypes.HandoutMaster ||
            contentType == PptxContentTypes.PresentationProperties ||
            contentType == PptxContentTypes.ViewProperties)
        {
            return PptxNamespaces.Presentation;
        }

        if (contentType == PptxContentTypes.Theme || contentType == PptxContentTypes.TableStyles)
        {
            return PptxNamespaces.Drawing;
        }

        if (contentType == PptxContentTypes.Chart)
        {
            return PptxNamespaces.Chart;
        }

        if (contentType == PptxContentTypes.CoreProperties)
        {
            return PptxNamespaces.CoreProperties;
        }

        if (contentType == PptxContentTypes.ExtendedProperties)
        {
            return PptxNamespaces.ExtendedProperties;
        }

        if (contentType == PptxContentTypes.CustomProperties)
        {
            return PptxNamespaces.CustomProperties;
        }

        return "";
    }

    internal static void RequireCommonSlideData(string xml, string partName, PptxResult result)
    {
        RequireElement(xml, partName, "cSld", result);
        RequireElement(xml, partName, "spTree", result);
    }

    internal static void RequireElement(string xml, string partName, string localName, PptxResult result)
    {
        if (!ContainsStartElement(xml, localName))
        {
            result.AddError("PKGV051", "Part is missing an element required by its minimum structure.", partName, "", "/" + localName);
        }
    }

    internal static void ValidateXmlIdentifiersAndReferences(PptxPackage package, PptxResult result)
    {
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (!part.IsXmlContentType())
            {
                continue;
            }

            PptxResult syntax = new PptxResult();
            PptxXmlValidator.ValidatePart(part, syntax);
            if (syntax.Failed())
            {
                continue;
            }

            string xml = Encoding.UTF8.GetString(part.GetBytes());
            ValidateUniqueElementAttribute(xml, part.PartName, "sldId", "id", result);
            ValidateUniqueElementAttribute(xml, part.PartName, "sldMasterId", "id", result);
            ValidateUniqueElementAttribute(xml, part.PartName, "sldLayoutId", "id", result);
            ValidateUniqueElementAttribute(xml, part.PartName, "cNvPr", "id", result);
            ValidateUniqueElementAttribute(xml, part.PartName, "cTn", "id", result);
            ValidateUniqueElementAttribute(xml, part.PartName, "cmAuthor", "id", result);
            ValidateUniqueCommentIndexes(xml, part.PartName, result);
            ValidateRelationshipReferences(xml, part, result);
        }
    }

    internal static void ValidateUniqueElementAttribute(string xml, string partName, string elementLocalName, string attributeName, PptxResult result)
    {
        Dictionary<string, string> seen = new Dictionary<string, string>();
        int index = 0;

        while (FindStartTag(xml, index) >= 0)
        {
            int start = FindStartTag(xml, index);
            int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
            if (end < 0)
            {
                return;
            }

            string elementName = PptxXmlValidator.ReadTagName(xml, start + 1, end);
            if (LocalName(elementName) == elementLocalName)
            {
                string value = ReadAttributeValue(xml, start + 1, end, attributeName);
                if (value.Length > 0)
                {
                    if (seen.ContainsKey(value))
                    {
                        result.AddError("PKGV060", "XML identifier is duplicated within its owning part.", partName, "", "/" + elementLocalName + "[@" + attributeName + "='" + value + "']");
                    }
                    else
                    {
                        seen.Add(value, value);
                    }
                }
            }

            index = end + 1;
        }
    }

    internal static void ValidateUniqueCommentIndexes(string xml, string partName, PptxResult result)
    {
        Dictionary<string, string> seen = new Dictionary<string, string>();
        int index = 0;

        while (FindStartTag(xml, index) >= 0)
        {
            int start = FindStartTag(xml, index);
            int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
            if (end < 0)
            {
                return;
            }

            string elementName = PptxXmlValidator.ReadTagName(xml, start + 1, end);
            if (LocalName(elementName) == "cm")
            {
                string authorId = ReadAttributeValue(xml, start + 1, end, "authorId");
                string commentIndex = ReadAttributeValue(xml, start + 1, end, "idx");
                if (authorId.Length > 0 && commentIndex.Length > 0)
                {
                    string key = authorId + ":" + commentIndex;
                    if (seen.ContainsKey(key))
                    {
                        result.AddError("PKGV060", "Comment identifier is duplicated within its owning part.", partName, "", "/cm[@authorId='" + authorId + "'][@idx='" + commentIndex + "']");
                    }
                    else
                    {
                        seen.Add(key, key);
                    }
                }
            }

            index = end + 1;
        }
    }

    internal static void ValidateRelationshipReferences(string xml, PptxPart part, PptxResult result)
    {
        string relationshipPrefix = FindNamespacePrefix(xml, PptxNamespaces.Relationships);
        if (relationshipPrefix.Length == 0)
        {
            return;
        }

        string qualifiedPrefix = relationshipPrefix + ":";
        int index = 0;

        while (FindStartTag(xml, index) >= 0)
        {
            int start = FindStartTag(xml, index);
            int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
            if (end < 0)
            {
                return;
            }

            ValidateRelationshipReferenceAttribute(xml, start + 1, end, qualifiedPrefix + "id", part, result);
            ValidateRelationshipReferenceAttribute(xml, start + 1, end, qualifiedPrefix + "embed", part, result);
            ValidateRelationshipReferenceAttribute(xml, start + 1, end, qualifiedPrefix + "link", part, result);
            index = end + 1;
        }
    }

    internal static void ValidateRelationshipReferenceAttribute(string xml, int startIndex, int endIndex, string attributeName, PptxPart part, PptxResult result)
    {
        string relationshipId = ReadAttributeValue(xml, startIndex, endIndex, attributeName);
        if (relationshipId.Length > 0 && !part.Relationships.ContainsId(relationshipId))
        {
            result.AddError("PKGV061", "XML relationship reference does not resolve in the owning part.", part.PartName, relationshipId, "");
        }
    }

    internal static void ValidateRequiredPresentationRelationships(PptxPackage package, PptxResult result)
    {
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];

            if (IsPresentationContentType(part.ContentType))
            {
                RequireRelationship(part, PptxRelationshipTypes.SlideMaster, result);
                RequireRelationship(part, PptxRelationshipTypes.PresentationProperties, result);
                continue;
            }

            if (part.ContentType == PptxContentTypes.SlideMaster)
            {
                RequireRelationship(part, PptxRelationshipTypes.Theme, result);
                RequireRelationship(part, PptxRelationshipTypes.SlideLayout, result);
                continue;
            }

            if (part.ContentType == PptxContentTypes.SlideLayout)
            {
                RequireRelationship(part, PptxRelationshipTypes.SlideMaster, result);
                continue;
            }

            if (part.ContentType == PptxContentTypes.Slide)
            {
                RequireRelationship(part, PptxRelationshipTypes.SlideLayout, result);
                continue;
            }

            if (part.ContentType == PptxContentTypes.NotesSlide)
            {
                RequireRelationship(part, PptxRelationshipTypes.Slide, result);
                continue;
            }

            if (part.ContentType == PptxContentTypes.NotesMaster || part.ContentType == PptxContentTypes.HandoutMaster)
            {
                RequireRelationship(part, PptxRelationshipTypes.Theme, result);
            }
        }
    }

    internal static void RequireRelationship(PptxPart part, string relationshipType, PptxResult result)
    {
        for (int index = 0; index < part.Relationships.Relationships.Count; index++)
        {
            if (part.Relationships.Relationships[index].Type == relationshipType)
            {
                return;
            }
        }

        result.AddError("PKGV052", "Part is missing a relationship required by its role in the presentation.", part.PartName, "", relationshipType);
    }

    internal static string ReadRootElementLocalName(string xml)
    {
        int start = FindStartTag(xml, 0);
        if (start < 0)
        {
            return "";
        }

        int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
        if (end < 0)
        {
            return "";
        }

        return LocalName(PptxXmlValidator.ReadTagName(xml, start + 1, end));
    }

    internal static string ReadRootElementNamespace(string xml)
    {
        int start = FindStartTag(xml, 0);
        if (start < 0)
        {
            return "";
        }

        int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
        if (end < 0)
        {
            return "";
        }

        string qualifiedName = PptxXmlValidator.ReadTagName(xml, start + 1, end);
        int colonIndex = qualifiedName.IndexOf(':');
        if (colonIndex < 0)
        {
            return ReadAttributeValue(xml, start + 1, end, "xmlns");
        }

        string prefix = qualifiedName.Substring(0, colonIndex);
        return ReadAttributeValue(xml, start + 1, end, "xmlns:" + prefix);
    }

    internal static bool ContainsStartElement(string xml, string localName)
    {
        int index = 0;

        while (FindStartTag(xml, index) >= 0)
        {
            int start = FindStartTag(xml, index);
            int end = PptxXmlValidator.FindTagEnd(xml, start + 1);
            if (end < 0)
            {
                return false;
            }

            string elementName = PptxXmlValidator.ReadTagName(xml, start + 1, end);
            if (LocalName(elementName) == localName)
            {
                return true;
            }

            index = end + 1;
        }

        return false;
    }

    internal static int FindStartTag(string xml, int startIndex)
    {
        int index = startIndex;

        while (index < xml.Length)
        {
            int tagStart = xml.IndexOf('<', index);
            if (tagStart < 0 || tagStart + 1 >= xml.Length)
            {
                return -1;
            }

            char marker = xml[tagStart + 1];
            if (marker != '/' && marker != '!' && marker != '?')
            {
                return tagStart;
            }

            int tagEnd = PptxXmlValidator.FindTagEnd(xml, tagStart + 1);
            if (tagEnd < 0)
            {
                return -1;
            }

            index = tagEnd + 1;
        }

        return -1;
    }

    internal static string ReadAttributeValue(string xml, int startIndex, int endIndex, string requestedName)
    {
        int nameEnd = PptxXmlValidator.ReadTagNameEnd(xml, startIndex, endIndex);
        if (nameEnd < 0)
        {
            return "";
        }

        int index = nameEnd;
        while (index < endIndex)
        {
            index = PptxXmlValidator.SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex || xml[index] == '/')
            {
                return "";
            }

            int attributeStart = index;
            index = index + 1;
            while (index < endIndex && PptxXmlValidator.IsXmlNameCharacter(xml[index]))
            {
                index = index + 1;
            }

            string attributeName = xml.Substring(attributeStart, index - attributeStart);
            index = PptxXmlValidator.SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex || xml[index] != '=')
            {
                return "";
            }

            index = index + 1;
            index = PptxXmlValidator.SkipWhitespace(xml, index, endIndex);
            if (index >= endIndex || (xml[index] != '"' && xml[index] != '\''))
            {
                return "";
            }

            char quote = xml[index];
            int valueStart = index + 1;
            int valueEnd = xml.IndexOf(quote, valueStart);
            if (valueEnd < 0 || valueEnd > endIndex)
            {
                return "";
            }

            if (attributeName == requestedName)
            {
                return xml.Substring(valueStart, valueEnd - valueStart);
            }

            index = valueEnd + 1;
        }

        return "";
    }

    internal static string LocalName(string qualifiedName)
    {
        int colonIndex = qualifiedName.IndexOf(':');
        if (colonIndex < 0)
        {
            return qualifiedName;
        }

        return qualifiedName.Substring(colonIndex + 1);
    }

    internal static string FindNamespacePrefix(string xml, string namespaceUri)
    {
        int index = 0;

        while (index < xml.Length)
        {
            int declarationStart = xml.IndexOf("xmlns:", index, System.StringComparison.Ordinal);
            if (declarationStart < 0)
            {
                return "";
            }

            int prefixStart = declarationStart + 6;
            int prefixEnd = prefixStart;
            while (prefixEnd < xml.Length && PptxXmlValidator.IsXmlNameCharacter(xml[prefixEnd]))
            {
                prefixEnd = prefixEnd + 1;
            }

            int equalsIndex = PptxXmlValidator.SkipWhitespace(xml, prefixEnd, xml.Length);
            if (equalsIndex < xml.Length && xml[equalsIndex] == '=')
            {
                int quoteIndex = PptxXmlValidator.SkipWhitespace(xml, equalsIndex + 1, xml.Length);
                if (quoteIndex < xml.Length && (xml[quoteIndex] == '"' || xml[quoteIndex] == '\''))
                {
                    char quote = xml[quoteIndex];
                    int valueEnd = xml.IndexOf(quote, quoteIndex + 1);
                    if (valueEnd > quoteIndex)
                    {
                        string value = xml.Substring(quoteIndex + 1, valueEnd - quoteIndex - 1);
                        if (value == namespaceUri)
                        {
                            return xml.Substring(prefixStart, prefixEnd - prefixStart);
                        }

                        index = valueEnd + 1;
                        continue;
                    }
                }
            }

            index = prefixEnd;
        }

        return "";
    }

    internal static bool IsPresentationContentType(string contentType)
    {
        if (contentType == PptxContentTypes.Presentation)
        {
            return true;
        }

        if (contentType == PptxContentTypes.MacroEnabledPresentation)
        {
            return true;
        }

        return false;
    }
}
