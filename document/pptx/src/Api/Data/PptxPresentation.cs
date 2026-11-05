using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Provides the editable presentation model, including slides, masters, themes, metadata, and presentation-wide settings.
/// </summary>
public class PptxPresentation
{
    internal PptxPackage Package;
    internal PptxPart? Part;
    /// <summary>
    /// Stores the core properties when the package contains them; otherwise, <c>null</c>.
    /// </summary>
    public PptxCoreProperties? CoreProperties;
    /// <summary>
    /// Stores the app properties when the package contains them; otherwise, <c>null</c>.
    /// </summary>
    public PptxAppProperties? AppProperties;
    /// <summary>
    /// Stores the custom properties when the package contains them; otherwise, <c>null</c>.
    /// </summary>
    public PptxCustomProperties? CustomProperties;
    /// <summary>
    /// Stores the presentation properties when the package contains them; otherwise, <c>null</c>.
    /// </summary>
    public PptxPresentationProperties? Properties;
    internal PptxPart? ViewPropertiesPart;
    internal PptxPart? TableStylesPart;
    /// <summary>
    /// Contains the slides in package or presentation order.
    /// </summary>
    public List<PptxSlide> Slides;
    /// <summary>
    /// Stores the slide collection value associated with this object.
    /// </summary>
    public PptxSlideCollection SlideCollection;
    /// <summary>
    /// Contains the slide masters in package or presentation order.
    /// </summary>
    public List<PptxSlideMaster> SlideMasters;
    /// <summary>
    /// Contains the slide layouts in package or presentation order.
    /// </summary>
    public List<PptxSlideLayout> SlideLayouts;
    /// <summary>
    /// Contains the themes in package or presentation order.
    /// </summary>
    public List<PptxTheme> Themes;
    /// <summary>
    /// Stores the primary theme when one is available; otherwise, <c>null</c>.
    /// </summary>
    public PptxTheme? Theme;
    /// <summary>
    /// Stores the notes master when one is available; otherwise, <c>null</c>.
    /// </summary>
    public PptxNotesMaster? NotesMaster;
    /// <summary>
    /// Stores the handout master when one is available; otherwise, <c>null</c>.
    /// </summary>
    public PptxHandoutMaster? HandoutMaster;
    /// <summary>
    /// Stores the slide width in English Metric Units (EMUs).
    /// </summary>
    public long SlideWidth;
    /// <summary>
    /// Stores the slide height in English Metric Units (EMUs).
    /// </summary>
    public long SlideHeight;
    /// <summary>
    /// Stores the slide size type associated with this object.
    /// </summary>
    public string SlideSizeType;
    /// <summary>
    /// Contains the sections in package or presentation order.
    /// </summary>
    public List<PptxSection> Sections;
    /// <summary>
    /// Contains the custom shows in package or presentation order.
    /// </summary>
    public List<PptxCustomShow> CustomShows;
    /// <summary>
    /// Stores the diagnostics value associated with this object.
    /// </summary>
    public PptxResult Diagnostics;

    internal PptxPresentation(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        Package = package;
        Part = null;
        CoreProperties = null;
        AppProperties = null;
        CustomProperties = null;
        Properties = null;
        ViewPropertiesPart = null;
        TableStylesPart = null;
        Slides = new List<PptxSlide>();
        SlideCollection = new PptxSlideCollection(this);
        SlideMasters = new List<PptxSlideMaster>();
        SlideLayouts = new List<PptxSlideLayout>();
        Themes = new List<PptxTheme>();
        Theme = null;
        NotesMaster = null;
        HandoutMaster = null;
        SlideWidth = 0L;
        SlideHeight = 0L;
        SlideSizeType = "";
        Sections = new List<PptxSection>();
        CustomShows = new List<PptxCustomShow>();
        Diagnostics = new PptxResult();
    }

    internal static PptxPresentation Load(PptxDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        return Load(document.GetPackage());
    }

    internal static PptxPresentation Load(PptxPackage package)
    {
        PptxPresentation presentation = new PptxPresentation(package);
        presentation.LoadModel();
        return presentation;
    }
    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }
    internal bool Failed()
    {
        return Diagnostics.Failed();
    }
    internal int SlideCount()
    {
        return SlideCollection.Count();
    }
    internal PptxSlide GetSlide(int index)
    {
        return SlideCollection.Get(index);
    }
    internal PptxShape AddImage(int slideIndex, byte[] bytes, PptxBounds bounds)
    {
        return AddImageToSlide(GetSlide(slideIndex), bytes, bounds);
    }
    internal PptxShape AddImageToSlide(PptxSlide slide, byte[] bytes, PptxBounds bounds)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, imagePart.PartName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Image, target, PptxRelationshipTargetMode.Internal);
        PptxShapeTree tree = slide.GetShapeTree();
        return tree.AddPicture(relationship.Id, bounds);
    }
    internal PptxShape AddImageWithContentType(PptxSlide slide, byte[] bytes, string contentType, PptxBounds bounds)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        PptxImageInfo info = CreateImageInfo(bytes, contentType);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, imagePart.PartName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Image, target, PptxRelationshipTargetMode.Internal);
        return slide.GetShapeTree().AddPicture(relationship.Id, bounds);
    }
    internal PptxShape AddLinkedImage(PptxSlide slide, string targetUri, PptxBounds bounds)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        string cleanTarget = ValidateImageLink(targetUri);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Image, cleanTarget, PptxRelationshipTargetMode.External);
        PptxShape shape = slide.GetShapeTree().AddPicture(relationship.Id, bounds);
        shape.SetImageRelationshipLinked(true);
        return shape;
    }
    internal PptxChart AddChart(int slideIndex, string chartType, PptxChartData data, PptxBounds bounds)
    {
        return AddChartToSlide(GetSlide(slideIndex), chartType, data, bounds);
    }
    internal PptxChart AddChartToSlide(PptxSlide slide, string chartType, PptxChartData data, PptxBounds bounds)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        string partName = AllocateChartPartName();
        PptxPart chartPart = Package.AddPart(partName, PptxContentTypes.Chart, PptxChartBuilder.CreateChartBytes(chartType, data));
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, chartPart.PartName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Chart, target, PptxRelationshipTargetMode.Internal);
        PptxShapeTree tree = slide.GetShapeTree();
        PptxShape shape = tree.AddChart(relationship.Id, bounds);
        return shape.AsChart();
    }
    internal PptxShape AddMediaShapeToSlide(PptxSlide slide, byte[] bytes, string contentType, string mediaType, PptxBounds bounds)
    {
        ValidateMediaSlideAndBounds(slide, bounds);
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        string cleanMediaType = ValidateMediaType(mediaType);
        string cleanContentType = ValidateMediaContentType(contentType, cleanMediaType);
        string extension = GetMediaPartExtension(cleanContentType, cleanMediaType);
        string partName = AllocateMediaPartName(extension);
        PptxPart part = Package.AddPart(partName, cleanContentType, bytes);
        string relationshipType = PptxRelationshipTypes.Video;
        if (cleanMediaType == PptxMediaType.Audio)
        {
            relationshipType = PptxRelationshipTypes.Audio;
        }

        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, part.PartName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, relationshipType, target, PptxRelationshipTargetMode.Internal);
        return slide.GetShapeTree().AddMedia(relationship.Id, cleanMediaType, bounds);
    }
    internal PptxShape AddLinkedMediaShapeToSlide(PptxSlide slide, string targetUri, string mediaType, PptxBounds bounds)
    {
        ValidateMediaSlideAndBounds(slide, bounds);
        string cleanMediaType = ValidateMediaType(mediaType);
        string relationshipType = PptxRelationshipTypes.Video;
        if (cleanMediaType == PptxMediaType.Audio)
        {
            relationshipType = PptxRelationshipTypes.Audio;
        }

        string cleanTarget = ValidateMediaLink(targetUri);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, relationshipType, cleanTarget, PptxRelationshipTargetMode.External);
        return slide.GetShapeTree().AddMedia(relationship.Id, cleanMediaType, bounds);
    }

    internal static void ValidateMediaSlideAndBounds(PptxSlide slide, PptxBounds bounds)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }
    }

    internal static string ValidateMediaType(string mediaType)
    {
        string clean = PptxDiagnostic.CleanText(mediaType);
        if (clean != PptxMediaType.Audio && clean != PptxMediaType.Video)
        {
            throw new System.ArgumentException("Media type must be audio or video.");
        }

        return clean;
    }

    internal static string ValidateMediaContentType(string contentType, string mediaType)
    {
        string clean = PptxPart.ValidateAndNormalizeContentType(contentType);
        string requiredPrefix = "video/";
        if (mediaType == PptxMediaType.Audio)
        {
            requiredPrefix = "audio/";
        }

        if (!clean.StartsWith(requiredPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            throw new System.ArgumentException("Media content type does not match the audio or video type.");
        }

        return clean;
    }

    internal static string ValidateMediaLink(string targetUri)
    {
        string clean = PptxDiagnostic.CleanText(targetUri);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Media link target is required.");
        }

        return clean;
    }

    internal PptxShape AddEmbeddedObjectShapeToSlide(PptxSlide slide, byte[] bytes, string contentType, string programId, string objectName, PptxBounds bounds)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        string cleanContentType = PptxPart.ValidateAndNormalizeContentType(contentType);
        string relationshipType = PptxRelationshipTypes.Package;
        string stem = "embeddedPackage";
        if (cleanContentType == PptxContentTypes.OleObject)
        {
            relationshipType = PptxRelationshipTypes.OleObject;
            stem = "oleObject";
        }

        return AddEmbeddedObjectShapeCore(slide, bytes, cleanContentType, relationshipType, stem, programId, objectName, bounds);
    }

    internal PptxShape AddEmbeddedPackageShapeToSlide(PptxSlide slide, byte[] bytes, string contentType, string programId, string objectName, PptxBounds bounds)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        string cleanContentType = PptxPart.ValidateAndNormalizeContentType(contentType);
        return AddEmbeddedObjectShapeCore(slide, bytes, cleanContentType, PptxRelationshipTypes.Package, "embeddedPackage", programId, objectName, bounds);
    }

    internal PptxShape AddEmbeddedObjectShapeCore(PptxSlide slide, byte[] bytes, string contentType, string relationshipType, string stem, string programId, string objectName, PptxBounds bounds)
    {
        string partName = AllocateEmbeddingPartName(stem);
        PptxPart part = Package.AddPart(partName, contentType, bytes);
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, part.PartName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, relationshipType, target, PptxRelationshipTargetMode.Internal);
        return slide.GetShapeTree().AddEmbeddedObject(relationship.Id, programId, objectName, bounds);
    }

    internal string AllocateMediaPartName(string extension)
    {
        int index = 1;
        string partName = "/ppt/media/media" + index.ToString() + "." + extension;
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/media/media" + index.ToString() + "." + extension;
        }

        return partName;
    }

    internal string AllocateEmbeddingPartName(string stem)
    {
        int index = 1;
        string partName = "/ppt/embeddings/" + stem + index.ToString() + ".bin";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/embeddings/" + stem + index.ToString() + ".bin";
        }

        return partName;
    }

    internal static string GetMediaPartExtension(string contentType, string mediaType)
    {
        if (contentType == "video/mp4")
        {
            return "mp4";
        }

        if (contentType == "video/webm")
        {
            return "webm";
        }

        if (contentType == "audio/mpeg")
        {
            return "mp3";
        }

        if (contentType == "audio/wav" || contentType == "audio/x-wav")
        {
            return "wav";
        }

        if (contentType == "audio/mp4")
        {
            return "m4a";
        }

        if (mediaType == PptxMediaType.Video)
        {
            return "video";
        }

        return "audio";
    }
    internal bool IsMacroEnabled()
    {
        if (Part == null)
        {
            return false;
        }

        return Part.ContentType == PptxContentTypes.MacroEnabledPresentation;
    }
    internal bool HasMacros()
    {
        if (IsMacroEnabled())
        {
            return true;
        }

        if (GetVbaProject() != null)
        {
            return true;
        }

        for (int index = 0; index < Package.Parts.Count; index++)
        {
            if (PptxSecurityScannerCore.IsMacroPart(Package.Parts[index]))
            {
                return true;
            }
        }

        return false;
    }
    internal PptxVbaProject? GetVbaProject()
    {
        PptxRelationship? relationship = FindPresentationRelationship(PptxRelationshipTypes.VbaProject);
        if (relationship == null)
        {
            return null;
        }

        return new PptxVbaProject(this, relationship);
    }
    internal PptxVbaProject SetVbaProjectBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxVbaProject? existing = GetVbaProject();
        if (existing != null && existing.Part != null)
        {
            existing.ReplaceBytes(bytes);
            return existing;
        }

        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part was not found.");
        }

        string partName = AllocateVbaProjectPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.VbaProject, bytes);
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, part.PartName);
        PptxRelationship relationship = Package.AddRelationship(Part.PartName, PptxRelationshipTypes.VbaProject, target, PptxRelationshipTargetMode.Internal);
        EnsureMacroEnabledContentType();
        return new PptxVbaProject(this, relationship);
    }

    internal void EnsureMacroEnabledContentType()
    {
        if (Part == null)
        {
            return;
        }

        if (Part.ContentType == PptxContentTypes.MacroEnabledPresentation)
        {
            return;
        }

        Part.SetContentType(PptxContentTypes.MacroEnabledPresentation);
        Package.EnsureContentTypeOverride(Part.PartName, Part.ContentType);
    }

    internal PptxRelationship? FindPresentationRelationship(string relationshipType)
    {
        if (Part == null)
        {
            return null;
        }

        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Part.Relationships.Relationships[index];
            if (relationship.Type == relationshipType)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string AllocateVbaProjectPartName()
    {
        int index = 1;
        string partName = "/ppt/vbaProject.bin";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/vbaProject" + index.ToString() + ".bin";
        }

        return partName;
    }
    internal List<PptxEmbeddedObject> GetEmbeddedObjects()
    {
        List<PptxEmbeddedObject> objects = new List<PptxEmbeddedObject>();
        for (int index = 0; index < SlideCount(); index++)
        {
            List<PptxEmbeddedObject> slideObjects = GetSlide(index).GetEmbeddedObjects();
            for (int itemIndex = 0; itemIndex < slideObjects.Count; itemIndex++)
            {
                objects.Add(slideObjects[itemIndex]);
            }
        }

        return objects;
    }
    internal List<PptxActiveXControl> GetActiveXControls()
    {
        List<PptxActiveXControl> controls = new List<PptxActiveXControl>();
        for (int index = 0; index < Package.Parts.Count; index++)
        {
            List<PptxActiveXControl> partControls = PptxActiveXControl.FromPart(this, Package.Parts[index]);
            for (int controlIndex = 0; controlIndex < partControls.Count; controlIndex++)
            {
                controls.Add(partControls[controlIndex]);
            }
        }

        return controls;
    }
    internal List<PptxImageFile> ExtractImages(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        List<PptxImageFile> images = new List<PptxImageFile>();
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type != PptxRelationshipTypes.Image)
            {
                continue;
            }

            if (relationship.TargetMode == PptxRelationshipTargetMode.Internal && !Package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            images.Add(CreateImageFile(relationship));
        }

        return images;
    }

    internal PptxImageFile CreateImageFile(PptxRelationship relationship)
    {
        PptxImageFile image = new PptxImageFile();
        image.RelationshipId = relationship.Id;
        image.Linked = relationship.TargetMode == PptxRelationshipTargetMode.External;
        image.Target = relationship.Target;
        if (image.Linked)
        {
            return image;
        }

        PptxPart part = Package.GetPart(relationship.TargetPartName);
        image.PartName = part.PartName;
        image.Target = part.PartName;
        image.Bytes = part.GetBytes();
        image.Info = PptxImageDetectorCore.Detect(image.Bytes);
        image.Info.ContentType = part.ContentType;
        return image;
    }
    internal PptxAssetExtractionResult ExtractImagesToDirectory(string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractPresentationImages(this, directoryPath);
    }
    internal PptxAssetExtractionResult ExtractMediaToDirectory(string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractPresentationMedia(this, directoryPath);
    }
    internal List<PptxMediaFile> GetMedia()
    {
        List<PptxMediaFile> media = new List<PptxMediaFile>();
        for (int index = 0; index < SlideCount(); index++)
        {
            List<PptxMediaFile> slideMedia = GetSlideMedia(GetSlide(index));
            for (int itemIndex = 0; itemIndex < slideMedia.Count; itemIndex++)
            {
                media.Add(slideMedia[itemIndex]);
            }
        }

        return media;
    }
    internal List<PptxMediaFile> GetSlideMedia(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        List<PptxMediaFile> media = new List<PptxMediaFile>();
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (!IsMediaRelationship(relationship))
            {
                continue;
            }

            media.Add(CreateMediaFile(slide, relationship));
        }

        return media;
    }

    internal PptxMediaFile CreateMediaFile(PptxSlide slide, PptxRelationship relationship)
    {
        PptxMediaFile media = new PptxMediaFile();
        media.SlidePartName = slide.PartName;
        media.RelationshipId = relationship.Id;
        media.Type = GetMediaType(relationship);
        media.Target = relationship.Target;
        media.Linked = relationship.TargetMode == PptxRelationshipTargetMode.External;
        media.Embedded = relationship.TargetMode == PptxRelationshipTargetMode.Internal;
        if (media.Embedded)
        {
            media.PartName = relationship.TargetPartName;
            if (Package.HasPart(relationship.TargetPartName))
            {
                PptxPart part = Package.GetPart(relationship.TargetPartName);
                media.ContentType = part.ContentType;
                media.Bytes = part.GetBytes();
            }
        }

        LoadPosterFrame(slide, media);
        return media;
    }

    internal void LoadPosterFrame(PptxSlide slide, PptxMediaFile media)
    {
        string posterRelationshipId = FindPosterRelationshipIdForMedia(slide, media.RelationshipId);
        if (posterRelationshipId.Length == 0)
        {
            return;
        }

        PptxRelationship? relationship = FindSlideRelationshipById(slide, posterRelationshipId);
        if (relationship == null)
        {
            return;
        }

        if (relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            return;
        }

        if (!Package.HasPart(relationship.TargetPartName))
        {
            return;
        }

        PptxPart part = Package.GetPart(relationship.TargetPartName);
        media.HasPosterFrame = true;
        media.PosterRelationshipId = relationship.Id;
        media.PosterPartName = part.PartName;
        media.PosterBytes = part.GetBytes();
        media.PosterInfo = PptxImageDetectorCore.Detect(media.PosterBytes);
    }

    internal PptxRelationship? FindSlideRelationshipById(PptxSlide slide, string relationshipId)
    {
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Id == relationshipId)
            {
                return relationship;
            }
        }

        return null;
    }

    internal string FindPosterRelationshipIdForMedia(PptxSlide slide, string mediaRelationshipId)
    {
        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return "";
        }

        XmlElement? mediaElement = FindMediaElementForRelationship(root, mediaRelationshipId);
        if (mediaElement == null)
        {
            return "";
        }

        XmlElement? shape = FindAncestorShape(mediaElement);
        if (shape == null)
        {
            return "";
        }

        return GetBlipRelationshipId(shape);
    }

    internal XmlElement? FindMediaElementForRelationship(XmlNode node, string relationshipId)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (IsMediaFileElement(element))
            {
                string link = PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "link");
                string embed = PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "embed");
                if (link == relationshipId || embed == relationshipId)
                {
                    return element;
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                XmlElement? result = FindMediaElementForRelationship(child, relationshipId);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    internal static bool IsMediaFileElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Drawing)
        {
            return false;
        }

        if (element.LocalName == "videoFile")
        {
            return true;
        }

        if (element.LocalName == "audioFile")
        {
            return true;
        }

        return false;
    }

    internal static XmlElement? FindAncestorShape(XmlElement element)
    {
        XmlNode? node = element.ParentNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement candidate = (XmlElement)node;
                if (candidate.NamespaceURI == PptxNamespaces.Presentation)
                {
                    if (candidate.LocalName == "pic" || candidate.LocalName == "sp" || candidate.LocalName == "graphicFrame")
                    {
                        return candidate;
                    }
                }
            }

            node = node.ParentNode;
        }

        return null;
    }

    internal static string GetBlipRelationshipId(XmlElement shape)
    {
        XmlElement? blip = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Drawing, "blip");
        if (blip == null)
        {
            return "";
        }

        string embed = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "embed");
        if (embed.Length > 0)
        {
            return embed;
        }

        return PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "link");
    }

    internal static bool IsMediaRelationship(PptxRelationship relationship)
    {
        if (relationship.Type == PptxRelationshipTypes.Audio)
        {
            return true;
        }

        if (relationship.Type == PptxRelationshipTypes.Video)
        {
            return true;
        }

        return false;
    }

    internal static string GetMediaType(PptxRelationship relationship)
    {
        if (relationship.Type == PptxRelationshipTypes.Audio)
        {
            return PptxMediaType.Audio;
        }

        if (relationship.Type == PptxRelationshipTypes.Video)
        {
            return PptxMediaType.Video;
        }

        return PptxMediaType.Unknown;
    }
    internal PptxRelationship GetMediaRelationship(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        string relationshipId = shape.GetMediaRelationshipId();
        PptxRelationship? relationship = shape.Tree.FindRelationshipById(relationshipId);
        if (relationship == null || !IsMediaRelationship(relationship))
        {
            throw new System.ArgumentException("Audio or video relationship was not found.");
        }

        return relationship;
    }
    internal void ReplaceMediaEmbedded(PptxShape shape, byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxRelationship relationship = GetMediaRelationship(shape);
        string mediaType = GetMediaType(relationship);
        string cleanContentType = ValidateMediaContentType(contentType, mediaType);
        string extension = GetMediaPartExtension(cleanContentType, mediaType);
        PptxPart part = Package.AddPart(AllocateMediaPartName(extension), cleanContentType, bytes);
        string oldPartName = relationship.TargetPartName;
        string target = PptxSlideCloner.BuildRelativeTargetPath(shape.Tree.Slide.PartName, part.PartName);
        Package.SetRelationshipTarget(shape.Tree.Slide.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
        RemovePartWhenUnreferenced(oldPartName);
    }
    internal void ReplaceMediaLinked(PptxShape shape, string targetUri)
    {
        PptxRelationship relationship = GetMediaRelationship(shape);
        string oldPartName = relationship.TargetPartName;
        Package.SetRelationshipTarget(shape.Tree.Slide.PartName, relationship.Id, ValidateMediaLink(targetUri), PptxRelationshipTargetMode.External);
        RemovePartWhenUnreferenced(oldPartName);
    }
    internal void RemoveMedia(PptxShape shape)
    {
        PptxRelationship relationship = GetMediaRelationship(shape);
        PptxSlide slide = shape.Tree.Slide;
        string mediaPartName = relationship.TargetPartName;
        string posterRelationshipId = shape.GetImageRelationshipId();
        string posterPartName = "";
        PptxRelationship? posterRelationship = FindSlideRelationshipById(slide, posterRelationshipId);
        if (posterRelationship != null && posterRelationship.Type == PptxRelationshipTypes.Image)
        {
            posterPartName = posterRelationship.TargetPartName;
            Package.RemoveRelationship(slide.PartName, posterRelationship.Id);
        }

        Package.RemoveRelationship(slide.PartName, relationship.Id);
        shape.Tree.Remove(shape);
        RemovePartWhenUnreferenced(mediaPartName);
        RemovePartWhenUnreferenced(posterPartName);
    }
    internal void SetMediaPosterFrame(PptxShape shape, byte[] bytes, string contentType)
    {
        GetMediaRelationship(shape);
        PptxImageInfo info = CreateImageInfo(bytes, contentType);
        PptxPart part = FindOrAddImagePart(bytes, info);
        PptxSlide slide = shape.Tree.Slide;
        string existingId = shape.GetImageRelationshipId();
        PptxRelationship? existing = FindSlideRelationshipById(slide, existingId);
        string oldPartName = "";
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, part.PartName);
        string relationshipId = "";
        if (existing != null && existing.Type == PptxRelationshipTypes.Image)
        {
            oldPartName = existing.TargetPartName;
            Package.SetRelationshipTarget(slide.PartName, existing.Id, target, PptxRelationshipTargetMode.Internal);
            relationshipId = existing.Id;
        }
        else
        {
            PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Image, target, PptxRelationshipTargetMode.Internal);
            relationshipId = relationship.Id;
        }

        shape.SetPosterFrameRelationshipId(relationshipId);
        RemovePartWhenUnreferenced(oldPartName);
    }
    internal void RemoveMediaPosterFrame(PptxShape shape)
    {
        GetMediaRelationship(shape);
        string relationshipId = shape.GetImageRelationshipId();
        if (relationshipId.Length == 0)
        {
            return;
        }

        PptxSlide slide = shape.Tree.Slide;
        PptxRelationship? relationship = FindSlideRelationshipById(slide, relationshipId);
        string partName = "";
        if (relationship != null && relationship.Type == PptxRelationshipTypes.Image)
        {
            partName = relationship.TargetPartName;
            Package.RemoveRelationship(slide.PartName, relationship.Id);
        }

        shape.ClearPosterFrameRelationship();
        RemovePartWhenUnreferenced(partName);
    }
    internal void RemovePartWhenUnreferenced(string partName)
    {
        string clean = PptxDiagnostic.CleanText(partName);
        if (clean.Length > 0 && Package.HasPart(clean) && !IsPartReferenced(clean))
        {
            Package.RemovePart(clean);
        }
    }
    internal bool IsPartReferenced(string partName)
    {
        for (int rootIndex = 0; rootIndex < Package.RootRelationships.Relationships.Count; rootIndex++)
        {
            PptxRelationship rootRelationship = Package.RootRelationships.Relationships[rootIndex];
            if (rootRelationship.TargetMode == PptxRelationshipTargetMode.Internal && rootRelationship.TargetPartName == partName)
            {
                return true;
            }
        }

        for (int partIndex = 0; partIndex < Package.Parts.Count; partIndex++)
        {
            PptxPart source = Package.Parts[partIndex];
            for (int relationshipIndex = 0; relationshipIndex < source.Relationships.Relationships.Count; relationshipIndex++)
            {
                PptxRelationship relationship = source.Relationships.Relationships[relationshipIndex];
                if (relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == partName)
                {
                    return true;
                }
            }
        }

        return false;
    }
    internal PptxExternalMediaAuditResult AuditExternalMedia()
    {
        PptxExternalMediaAuditResult result = new PptxExternalMediaAuditResult();
        List<PptxMediaFile> media = GetMedia();
        for (int index = 0; index < media.Count; index++)
        {
            PptxMediaFile item = media[index];
            if (!item.Linked)
            {
                continue;
            }

            PptxExternalMediaLink link = new PptxExternalMediaLink();
            link.SlidePartName = item.SlidePartName;
            link.RelationshipId = item.RelationshipId;
            link.MediaType = item.Type;
            link.Target = item.Target;
            link.TargetPresent = item.Target.Length > 0;
            link.Secure = item.Target.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase);
            result.Links.Add(link);
            result.ExternalCount = result.ExternalCount + 1;
            if (!link.TargetPresent)
            {
                result.MissingTargetCount = result.MissingTargetCount + 1;
            }
            else if (!link.Secure)
            {
                result.InsecureCount = result.InsecureCount + 1;
            }
        }

        return result;
    }
    internal PptxImageFile ExtractImage(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        PptxRelationship relationship = GetImageRelationship(shape);
        return CreateImageFile(relationship);
    }
    internal void ReplaceImage(PptxShape shape, byte[] bytes)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        PptxRelationship relationship = GetImageRelationship(shape);
        string target = PptxSlideCloner.BuildRelativeTargetPath(shape.Tree.Slide.PartName, imagePart.PartName);
        Package.SetRelationshipTarget(shape.Tree.Slide.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
        shape.SetImageRelationshipLinked(false);
    }
    internal void ReplaceImageWithContentType(PptxShape shape, byte[] bytes, string contentType)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        PptxImageInfo info = CreateImageInfo(bytes, contentType);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        PptxRelationship relationship = GetImageRelationship(shape);
        string target = PptxSlideCloner.BuildRelativeTargetPath(shape.Tree.Slide.PartName, imagePart.PartName);
        Package.SetRelationshipTarget(shape.Tree.Slide.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
        shape.SetImageRelationshipLinked(false);
    }
    internal void ReplaceImageWithLink(PptxShape shape, string targetUri)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        PptxRelationship relationship = GetImageRelationship(shape);
        Package.SetRelationshipTarget(shape.Tree.Slide.PartName, relationship.Id, ValidateImageLink(targetUri), PptxRelationshipTargetMode.External);
        shape.SetImageRelationshipLinked(true);
    }
    internal void ReplaceImageRelationship(PptxSlide slide, string relationshipId, byte[] bytes, string contentType)
    {
        PptxRelationship relationship = GetSlideImageRelationship(slide, relationshipId);
        PptxImageInfo info = CreateImageInfo(bytes, contentType);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, imagePart.PartName);
        Package.SetRelationshipTarget(slide.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
        slide.GetShapeTree().SetPictureRelationshipLinked(relationship.Id, false);
    }
    internal void ReplaceImageRelationshipWithLink(PptxSlide slide, string relationshipId, string targetUri)
    {
        PptxRelationship relationship = GetSlideImageRelationship(slide, relationshipId);
        Package.SetRelationshipTarget(slide.PartName, relationship.Id, ValidateImageLink(targetUri), PptxRelationshipTargetMode.External);
        slide.GetShapeTree().SetPictureRelationshipLinked(relationship.Id, true);
    }
    internal int ReplaceAllImages(byte[] bytes, string contentType)
    {
        PptxImageInfo info = CreateImageInfo(bytes, contentType);
        PptxPart imagePart = FindOrAddImagePart(bytes, info);
        int count = 0;

        for (int slideIndex = 0; slideIndex < Slides.Count; slideIndex++)
        {
            PptxSlide slide = Slides[slideIndex];
            for (int relationshipIndex = 0; relationshipIndex < slide.Part.Relationships.Relationships.Count; relationshipIndex++)
            {
                PptxRelationship relationship = slide.Part.Relationships.Relationships[relationshipIndex];
                if (relationship.Type != PptxRelationshipTypes.Image)
                {
                    continue;
                }

                string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, imagePart.PartName);
                Package.SetRelationshipTarget(slide.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
                slide.GetShapeTree().SetPictureRelationshipLinked(relationship.Id, false);
                count = count + 1;
            }
        }

        return count;
    }
    internal int ReplaceAllImagesWithLink(string targetUri)
    {
        string cleanTarget = ValidateImageLink(targetUri);
        int count = 0;

        for (int slideIndex = 0; slideIndex < Slides.Count; slideIndex++)
        {
            PptxSlide slide = Slides[slideIndex];
            for (int relationshipIndex = 0; relationshipIndex < slide.Part.Relationships.Relationships.Count; relationshipIndex++)
            {
                PptxRelationship relationship = slide.Part.Relationships.Relationships[relationshipIndex];
                if (relationship.Type != PptxRelationshipTypes.Image)
                {
                    continue;
                }

                Package.SetRelationshipTarget(slide.PartName, relationship.Id, cleanTarget, PptxRelationshipTargetMode.External);
                slide.GetShapeTree().SetPictureRelationshipLinked(relationship.Id, true);
                count = count + 1;
            }
        }

        return count;
    }
    internal PptxRelationship GetSlideImageRelationship(PptxSlide slide, string relationshipId)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        string clean = PptxDiagnostic.CleanText(relationshipId);
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Id == clean && relationship.Type == PptxRelationshipTypes.Image)
            {
                return relationship;
            }
        }

        throw new System.ArgumentException("Picture relationship was not found.");
    }

    internal PptxRelationship GetImageRelationship(PptxShape shape)
    {
        string relationshipId = shape.GetImageRelationshipId();
        if (relationshipId.Length == 0)
        {
            throw new System.ArgumentException("Picture relationship was not found.");
        }

        for (int index = 0; index < shape.Tree.Slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = shape.Tree.Slide.Part.Relationships.Relationships[index];
            if (relationship.Id == relationshipId && relationship.Type == PptxRelationshipTypes.Image)
            {
                return relationship;
            }
        }

        throw new System.ArgumentException("Picture relationship was not found.");
    }

    internal PptxPart FindOrAddImagePart(byte[] bytes, PptxImageInfo info)
    {
        PptxPart? existing = FindImagePartByBytes(bytes, info.ContentType);
        if (existing != null)
        {
            return existing;
        }

        string extension = info.Extension;
        if (extension.Length == 0)
        {
            extension = "bin";
        }

        string contentType = info.ContentType;
        if (contentType.Length == 0)
        {
            contentType = PptxContentTypes.Binary;
        }

        string partName = AllocateImagePartName(extension);
        return Package.AddPart(partName, contentType, bytes);
    }

    internal PptxPart? FindImagePartByBytes(byte[] bytes, string contentType)
    {
        for (int index = 0; index < Package.Parts.Count; index++)
        {
            PptxPart part = Package.Parts[index];
            if (!part.PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (part.ContentType != contentType)
            {
                continue;
            }

            if (BytesEqual(part.GetBytes(), bytes))
            {
                return part;
            }
        }

        return null;
    }

    internal static PptxImageInfo CreateImageInfo(byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        string cleanContentType = PptxPart.ValidateAndNormalizeContentType(contentType);
        if (!cleanContentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new System.ArgumentException("Image content type must start with image/.");
        }

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);
        info.ContentType = cleanContentType;
        ApplyImageContentType(info, cleanContentType);
        return info;
    }
    internal static void ApplyImageContentType(PptxImageInfo info, string contentType)
    {
        if (contentType == PptxContentTypes.Png)
        {
            info.Type = PptxImageType.Png;
            info.Extension = "png";
        }
        else if (contentType == PptxContentTypes.Jpeg)
        {
            info.Type = PptxImageType.Jpeg;
            info.Extension = "jpg";
        }
        else if (contentType == PptxContentTypes.Gif)
        {
            info.Type = PptxImageType.Gif;
            info.Extension = "gif";
        }
        else if (contentType == PptxContentTypes.Bmp)
        {
            info.Type = PptxImageType.Bmp;
            info.Extension = "bmp";
        }
        else if (contentType == PptxContentTypes.Tiff)
        {
            info.Type = PptxImageType.Tiff;
            info.Extension = "tif";
        }
        else if (contentType == PptxContentTypes.Svg)
        {
            info.Type = PptxImageType.Svg;
            info.Extension = "svg";
        }
        else if (contentType == PptxContentTypes.Emf)
        {
            info.Type = PptxImageType.Emf;
            info.Extension = "emf";
        }
        else if (contentType == PptxContentTypes.Wmf)
        {
            info.Type = PptxImageType.Wmf;
            info.Extension = "wmf";
        }
    }
    internal static string ValidateImageLink(string targetUri)
    {
        string clean = PptxDiagnostic.CleanText(targetUri);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Image link target is required.");
        }

        return clean;
    }

    internal int DeduplicateImages()
    {
        int removed = 0;
        int candidateIndex = Package.Parts.Count - 1;
        while (candidateIndex >= 0)
        {
            PptxPart candidate = Package.Parts[candidateIndex];
            PptxPart? canonical = FindEarlierDuplicateImagePart(candidate, candidateIndex);
            if (canonical != null)
            {
                RetargetImageRelationships(candidate.PartName, canonical.PartName);
                Package.RemovePart(candidate.PartName);
                removed = removed + 1;
            }

            candidateIndex = candidateIndex - 1;
        }

        return removed;
    }
    internal PptxPart? FindEarlierDuplicateImagePart(PptxPart candidate, int candidateIndex)
    {
        if (!candidate.PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal) || !candidate.ContentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        byte[] candidateBytes = candidate.GetBytes();
        for (int index = 0; index < candidateIndex; index++)
        {
            PptxPart part = Package.Parts[index];
            if (part.PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal) && part.ContentType == candidate.ContentType && BytesEqual(part.GetBytes(), candidateBytes))
            {
                return part;
            }
        }

        return null;
    }
    internal void RetargetImageRelationships(string oldPartName, string newPartName)
    {
        for (int partIndex = 0; partIndex < Package.Parts.Count; partIndex++)
        {
            PptxPart source = Package.Parts[partIndex];
            for (int relationshipIndex = 0; relationshipIndex < source.Relationships.Relationships.Count; relationshipIndex++)
            {
                PptxRelationship relationship = source.Relationships.Relationships[relationshipIndex];
                if (relationship.Type == PptxRelationshipTypes.Image && relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == oldPartName)
                {
                    string target = PptxSlideCloner.BuildRelativeTargetPath(source.PartName, newPartName);
                    Package.SetRelationshipTarget(source.PartName, relationship.Id, target, PptxRelationshipTargetMode.Internal);
                }
            }
        }
    }

    internal string AllocateImagePartName(string extension)
    {
        string clean = PptxDiagnostic.CleanText(extension);
        if (clean.Length == 0)
        {
            clean = "bin";
        }

        int index = 1;
        string partName = "/ppt/media/image" + index.ToString() + "." + clean;
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/media/image" + index.ToString() + "." + clean;
        }

        return partName;
    }

    internal string AllocateChartPartName()
    {
        int index = 1;
        string partName = "/ppt/charts/chart" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/charts/chart" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
    internal void SetSlideNotesText(int slideIndex, string text)
    {
        GetSlide(slideIndex).SetNotesText(text);
    }
    internal string GetSlideNotesText(int slideIndex)
    {
        return GetSlide(slideIndex).GetNotesText();
    }
    internal string ExtractText()
    {
        return PptxTextExtractorCore.ExtractPresentationText(this);
    }
    internal string ExportText()
    {
        return PptxTextOutlineExporterCore.ExportPresentationText(this);
    }
    internal string ExportOutline()
    {
        return PptxTextOutlineExporterCore.ExportPresentationOutline(this);
    }
    internal string ExportHtml()
    {
        return PptxHtmlExporterCore.ExportPresentation(this);
    }
    internal string ExportSvg()
    {
        return PptxSvgExporterCore.ExportPresentation(this);
    }
    internal List<PptxOutlineEntry> GetOutlineEntries()
    {
        return PptxTextOutlineExporterCore.GetPresentationOutlineEntries(this);
    }
    internal string ToSummaryJson()
    {
        return PptxJsonSummaryExporterCore.ExportPresentation(this);
    }
    internal string ExtractSlideText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractSlideText(GetSlide(slideIndex));
    }
    internal string ExtractNotesText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractNotesText(this, GetSlide(slideIndex));
    }
    internal string ExtractCommentsText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractRelatedPartsText(this, GetSlide(slideIndex), PptxRelationshipTypes.Comments);
    }
    internal string ExtractModernCommentsText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractRelatedPartsText(this, GetSlide(slideIndex), PptxRelationshipTypes.ModernComments);
    }
    internal List<PptxModernComment> GetModernComments()
    {
        List<PptxModernComment> comments = new List<PptxModernComment>();
        for (int index = 0; index < SlideCount(); index++)
        {
            List<PptxModernComment> slideComments = GetSlide(index).GetModernComments();
            for (int itemIndex = 0; itemIndex < slideComments.Count; itemIndex++)
            {
                comments.Add(slideComments[itemIndex]);
            }
        }

        return comments;
    }
    internal bool HasModernComments()
    {
        return GetModernComments().Count > 0;
    }
    internal List<PptxAnimationEffect> GetAnimations()
    {
        List<PptxAnimationEffect> effects = new List<PptxAnimationEffect>();
        for (int index = 0; index < SlideCount(); index++)
        {
            List<PptxAnimationEffect> slideEffects = GetSlide(index).GetAnimations();
            for (int itemIndex = 0; itemIndex < slideEffects.Count; itemIndex++)
            {
                effects.Add(slideEffects[itemIndex]);
            }
        }

        return effects;
    }
    internal List<PptxHyperlink> GetHyperlinks()
    {
        List<PptxHyperlink> hyperlinks = new List<PptxHyperlink>();
        for (int slideIndex = 0; slideIndex < SlideCount(); slideIndex++)
        {
            List<PptxHyperlink> slideHyperlinks = GetSlide(slideIndex).GetHyperlinks();
            for (int hyperlinkIndex = 0; hyperlinkIndex < slideHyperlinks.Count; hyperlinkIndex++)
            {
                hyperlinks.Add(slideHyperlinks[hyperlinkIndex]);
            }
        }

        return hyperlinks;
    }
    internal PptxResult AuditHyperlinks()
    {
        return PptxHyperlink.AuditAll(GetHyperlinks());
    }
    internal List<PptxSlideTransition> GetTransitions()
    {
        List<PptxSlideTransition> transitions = new List<PptxSlideTransition>();
        for (int index = 0; index < SlideCount(); index++)
        {
            PptxSlideTransition transition = GetSlide(index).GetTransition();
            if (transition.HasTransition())
            {
                transitions.Add(transition);
            }
        }

        return transitions;
    }
    internal List<PptxModernComment> GetSlideModernComments(PptxSlide slide)
    {
        List<PptxModernComment> comments = new List<PptxModernComment>();
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type != PptxRelationshipTypes.ModernComments)
            {
                continue;
            }

            if (relationship.TargetMode != PptxRelationshipTargetMode.Internal)
            {
                continue;
            }

            if (!Package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            PptxPart part = Package.GetPart(relationship.TargetPartName);
            XmlDocument document = GetPartXmlDocument(part);
            XmlElement? root = document.DocumentElement;
            if (root != null)
            {
                AddModernCommentElements(slide, part, relationship, root, comments);
            }
        }

        return comments;
    }

    internal void AddModernCommentElements(PptxSlide slide, PptxPart part, PptxRelationship relationship, XmlNode node, List<PptxModernComment> comments)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.ModernComments && element.LocalName == "cm")
            {
                comments.Add(new PptxModernComment(slide, part, relationship, element));
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                AddModernCommentElements(slide, part, relationship, child, comments);
            }
        }
    }
    internal List<PptxCommentAuthor> GetCommentAuthors()
    {
        List<PptxCommentAuthor> authors = new List<PptxCommentAuthor>();
        PptxPart? part = FindCommentAuthorsPart();
        if (part == null)
        {
            return authors;
        }

        XmlDocument document = GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return authors;
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cmAuthor")
            {
                authors.Add(new PptxCommentAuthor(this, part, element));
            }
        }

        return authors;
    }
    internal PptxCommentAuthor AddCommentAuthor(string name, string initials)
    {
        PptxPart part = GetOrCreateCommentAuthorsPart();
        XmlDocument document = GetPartXmlDocument(part);
        XmlElement root = GetOrCreateCommentAuthorsRoot(document);
        int id = AllocateCommentAuthorId(root);
        XmlElement author = document.CreateElement("p", "cmAuthor", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(author, "", "", "id", id.ToString());
        PptxXml.SetAttributeValue(author, "", "", "name", PptxDiagnostic.CleanText(name));
        PptxXml.SetAttributeValue(author, "", "", "initials", PptxDiagnostic.CleanText(initials));
        PptxXml.SetAttributeValue(author, "", "", "lastIdx", "0");
        PptxXml.SetAttributeValue(author, "", "", "clrIdx", id.ToString());
        root.AppendChild(author);
        part.SetXmlDocument(document);
        return new PptxCommentAuthor(this, part, author);
    }
    internal bool RemoveCommentAuthor(PptxCommentAuthor author)
    {
        if (author == null)
        {
            throw new System.ArgumentNullException("author");
        }

        if (author.Presentation != this)
        {
            throw new System.ArgumentException("Comment author belongs to another presentation.");
        }

        for (int slideIndex = 0; slideIndex < Slides.Count; slideIndex++)
        {
            List<PptxComment> comments = GetSlideComments(Slides[slideIndex]);
            for (int commentIndex = 0; commentIndex < comments.Count; commentIndex++)
            {
                if (comments[commentIndex].AuthorId == author.Id)
                {
                    throw new System.ArgumentException("Comment author is referenced by a comment.");
                }
            }
        }

        XmlNode? parent = author.Element.ParentNode;
        if (parent == null)
        {
            return false;
        }

        parent.RemoveChild(author.Element);
        author.Part.SetXmlDocument(PptxTextFrame.GetOwnerDocument(author.Element));
        XmlElement? root = author.Part.XmlDocument.DocumentElement;
        if (root != null && HasCommentAuthorElement(root))
        {
            return true;
        }

        if (Part != null)
        {
            for (int index = Part.Relationships.Relationships.Count - 1; index >= 0; index--)
            {
                PptxRelationship relationship = Part.Relationships.Relationships[index];
                if (relationship.Type == PptxRelationshipTypes.CommentAuthors && relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == author.Part.PartName)
                {
                    Package.RemoveRelationship(Part.PartName, relationship.Id);
                }
            }
        }

        RemovePartWhenUnreferenced(author.Part.PartName);
        return true;
    }
    internal bool HasCommentAuthorElement(XmlElement root)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cmAuthor")
                {
                    return true;
                }
            }
        }

        return false;
    }
    internal List<PptxComment> GetSlideComments(PptxSlide slide)
    {
        List<PptxComment> comments = new List<PptxComment>();
        PptxPart? part = FindSlideCommentsPart(slide);
        if (part == null)
        {
            return comments;
        }

        XmlDocument document = GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return comments;
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cm")
            {
                comments.Add(new PptxComment(slide, part, element));
            }
        }

        return comments;
    }
    internal PptxComment AddSlideComment(PptxSlide slide, int authorId, string text)
    {
        return AddSlideCommentWithDetails(slide, authorId, text, CreateUtcTimestamp(), 0L, 0L);
    }
    internal PptxComment AddSlideCommentWithDetails(PptxSlide slide, int authorId, string text, string dateTime, long x, long y)
    {
        PptxPart part = GetOrCreateSlideCommentsPart(slide);
        XmlDocument document = GetPartXmlDocument(part);
        XmlElement root = GetOrCreateCommentsRoot(document);
        int commentIndex = AllocateCommentIndex(root);
        XmlElement comment = document.CreateElement("p", "cm", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(comment, "", "", "authorId", authorId.ToString());
        PptxXml.SetAttributeValue(comment, "", "", "dt", PptxDiagnostic.CleanText(dateTime));
        PptxXml.SetAttributeValue(comment, "", "", "idx", commentIndex.ToString());
        XmlElement position = document.CreateElement("p", "pos", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(position, "", "", "x", x.ToString());
        PptxXml.SetAttributeValue(position, "", "", "y", y.ToString());
        comment.AppendChild(position);
        XmlElement textElement = document.CreateElement("p", "text", PptxNamespaces.Presentation);
        textElement.InnerText = PptxRunFormatCore.NormalizeTextValue(text);
        comment.AppendChild(textElement);
        root.AppendChild(comment);
        part.SetXmlDocument(document);
        UpdateCommentAuthorLastIndex(authorId, commentIndex);
        return new PptxComment(slide, part, comment);
    }
    internal string ExtractChartsText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractRelatedPartsText(this, GetSlide(slideIndex), PptxRelationshipTypes.Chart);
    }
    internal string ExtractDiagramsText(int slideIndex)
    {
        return PptxTextExtractorCore.ExtractRelatedPartsText(this, GetSlide(slideIndex), PptxRelationshipTypes.DiagramData);
    }

    internal PptxPart? FindCommentAuthorsPart()
    {
        if (Part == null)
        {
            return null;
        }

        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.CommentAuthors && Package.HasPart(relationship.TargetPartName))
            {
                return Package.GetPart(relationship.TargetPartName);
            }
        }

        return null;
    }

    internal PptxPart GetOrCreateCommentAuthorsPart()
    {
        PptxPart? existing = FindCommentAuthorsPart();
        if (existing != null)
        {
            return existing;
        }

        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part was not found.");
        }

        string partName = AllocateCommentAuthorsPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.CommentAuthors, Encoding.UTF8.GetBytes(CreateCommentAuthorsXml()));
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, partName);
        Package.AddRelationship(Part.PartName, PptxRelationshipTypes.CommentAuthors, target, PptxRelationshipTargetMode.Internal);
        return part;
    }

    internal PptxPart? FindSlideCommentsPart(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Comments && Package.HasPart(relationship.TargetPartName))
            {
                return Package.GetPart(relationship.TargetPartName);
            }
        }

        return null;
    }

    internal PptxPart GetOrCreateSlideCommentsPart(PptxSlide slide)
    {
        PptxPart? existing = FindSlideCommentsPart(slide);
        if (existing != null)
        {
            return existing;
        }

        string partName = AllocateCommentsPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.Comments, Encoding.UTF8.GetBytes(CreateCommentsXml()));
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, partName);
        Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Comments, target, PptxRelationshipTargetMode.Internal);
        return part;
    }

    internal XmlElement GetOrCreateCommentAuthorsRoot(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", "cmAuthorLst", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "p", PptxNamespaces.Presentation);
            document.AppendChild(root);
        }

        return root;
    }

    internal XmlElement GetOrCreateCommentsRoot(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", "cmLst", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "p", PptxNamespaces.Presentation);
            document.AppendChild(root);
        }

        return root;
    }

    internal int AllocateCommentAuthorId(XmlElement root)
    {
        int id = 0;
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cmAuthor")
            {
                int existing = PptxXml.GetIntAttribute(element, "", "id", -1);
                if (existing >= id)
                {
                    id = existing + 1;
                }
            }
        }

        return id;
    }

    internal int AllocateCommentIndex(XmlElement root)
    {
        int commentIndex = 0;
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cm")
            {
                int existing = PptxXml.GetIntAttribute(element, "", "idx", -1);
                if (existing >= commentIndex)
                {
                    commentIndex = existing + 1;
                }
            }
        }

        return commentIndex;
    }

    internal void UpdateCommentAuthorLastIndex(int authorId, int commentIndex)
    {
        List<PptxCommentAuthor> authors = GetCommentAuthors();
        for (int index = 0; index < authors.Count; index++)
        {
            if (authors[index].Id == authorId && authors[index].LastIndex < commentIndex)
            {
                authors[index].SetLastIndex(commentIndex);
            }
        }
    }

    internal string AllocateCommentAuthorsPartName()
    {
        string partName = "/ppt/commentAuthors.xml";
        int index = 1;
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/commentAuthors" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal string AllocateCommentsPartName()
    {
        int index = 1;
        string partName = "/ppt/comments/comment" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/comments/comment" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal static string CreateCommentAuthorsXml()
    {
        return "<p:cmAuthorLst xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>";
    }

    internal static string CreateCommentsXml()
    {
        return "<p:cmLst xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>";
    }

    internal static string CreateUtcTimestamp()
    {
        return System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
    }
    internal List<PptxTag> GetSlideTags(PptxSlide slide)
    {
        List<PptxTag> tags = new List<PptxTag>();
        PptxPart? part = FindSlideTagsPart(slide);
        if (part == null)
        {
            return tags;
        }

        XmlDocument document = GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return tags;
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "tag")
            {
                tags.Add(new PptxTag(PptxXml.GetAttributeValue(element, "", "name"), PptxXml.GetAttributeValue(element, "", "val")));
            }
        }

        return tags;
    }
    internal string GetSlideTagValue(PptxSlide slide, string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        List<PptxTag> tags = GetSlideTags(slide);
        for (int index = 0; index < tags.Count; index++)
        {
            if (tags[index].Name == clean)
            {
                return tags[index].Value;
            }
        }

        return "";
    }
    internal void SetSlideTag(PptxSlide slide, string name, string value)
    {
        string cleanName = PptxDiagnostic.CleanText(name);
        if (cleanName.Length == 0)
        {
            throw new System.ArgumentException("Tag name is required.");
        }

        PptxPart part = GetOrCreateSlideTagsPart(slide);
        XmlDocument document = GetPartXmlDocument(part);
        XmlElement root = GetOrCreateTagRoot(document);
        XmlElement? tag = FindTagElement(root, cleanName);
        if (tag == null)
        {
            tag = document.CreateElement("p", "tag", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(tag, "", "", "name", cleanName);
            root.AppendChild(tag);
        }

        PptxXml.SetAttributeValue(tag, "", "", "val", PptxDiagnostic.CleanText(value));
        part.SetXmlDocument(document);
    }
    internal void RemoveSlideTag(PptxSlide slide, string name)
    {
        PptxPart? part = FindSlideTagsPart(slide);
        if (part == null)
        {
            return;
        }

        XmlDocument document = GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? tag = FindTagElement(root, PptxDiagnostic.CleanText(name));
        if (tag != null)
        {
            root.RemoveChild(tag);
            part.SetXmlDocument(document);
        }
    }
    internal void ClearSlideTags(PptxSlide slide)
    {
        PptxPart? part = FindSlideTagsPart(slide);
        if (part == null)
        {
            return;
        }

        XmlDocument document = GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        bool changed = false;
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "tag")
                {
                    root.RemoveChild(element);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            part.SetXmlDocument(document);
        }
    }
    internal bool RemoveSlideTagsPart(PptxSlide slide)
    {
        bool removed = false;
        List<string> partNames = new List<string>();
        for (int index = slide.Part.Relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Tags)
            {
                if (relationship.TargetMode == PptxRelationshipTargetMode.Internal)
                {
                    partNames.Add(relationship.TargetPartName);
                }

                RemoveSlideTagsRelationshipMarker(slide, relationship.Id);
                Package.RemoveRelationship(slide.PartName, relationship.Id);
                removed = true;
            }
        }

        for (int index = 0; index < partNames.Count; index++)
        {
            RemovePartWhenUnreferenced(partNames[index]);
        }

        return removed;
    }

    internal PptxPart? FindSlideTagsPart(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Tags && relationship.TargetMode == PptxRelationshipTargetMode.Internal && Package.HasPart(relationship.TargetPartName))
            {
                return Package.GetPart(relationship.TargetPartName);
            }
        }

        return null;
    }

    internal PptxPart GetOrCreateSlideTagsPart(PptxSlide slide)
    {
        PptxPart? existing = FindSlideTagsPart(slide);
        if (existing != null)
        {
            PptxRelationship? existingRelationship = FindSlideTagsRelationship(slide);
            if (existingRelationship != null)
            {
                EnsureSlideTagsRelationshipMarker(slide, existingRelationship.Id);
            }

            return existing;
        }

        string partName = AllocateTagsPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.Tags, Encoding.UTF8.GetBytes(CreateTagsXml()));
        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, partName);
        PptxRelationship relationship = Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Tags, target, PptxRelationshipTargetMode.Internal);
        EnsureSlideTagsRelationshipMarker(slide, relationship.Id);
        return part;
    }

    internal PptxRelationship? FindSlideTagsRelationship(PptxSlide slide)
    {
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Tags)
            {
                return relationship;
            }
        }

        return null;
    }

    internal void EnsureSlideTagsRelationshipMarker(PptxSlide slide, string relationshipId)
    {
        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? customDataList = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "custDataLst");
        if (customDataList == null)
        {
            customDataList = document.CreateElement("p", "custDataLst", PptxNamespaces.Presentation);
            XmlElement? colorMap = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "clrMapOvr");
            if (colorMap != null)
            {
                root.InsertBefore(customDataList, colorMap);
            }
            else
            {
                root.AppendChild(customDataList);
            }
        }

        XmlElement? tags = PptxXml.FindFirstChild(customDataList, PptxNamespaces.Presentation, "tags");
        if (tags == null)
        {
            tags = document.CreateElement("p", "tags", PptxNamespaces.Presentation);
            customDataList.AppendChild(tags);
        }

        PptxXml.SetAttributeValue(tags, "r", PptxNamespaces.Relationships, "id", PptxDiagnostic.CleanText(relationshipId));
        slide.Part.SetXmlDocument(document);
    }
    internal void RemoveSlideTagsRelationshipMarker(PptxSlide slide, string relationshipId)
    {
        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? customDataList = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "custDataLst");
        if (customDataList == null)
        {
            return;
        }

        bool changed = false;
        for (int index = customDataList.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = customDataList.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "tags" && PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "id") == relationshipId)
                {
                    customDataList.RemoveChild(element);
                    changed = true;
                }
            }
        }

        if (changed && !HasElementChild(customDataList) && customDataList.ParentNode != null)
        {
            customDataList.ParentNode.RemoveChild(customDataList);
        }

        if (changed)
        {
            slide.Part.SetXmlDocument(document);
        }
    }
    internal bool HasElementChild(XmlElement element)
    {
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                return true;
            }
        }

        return false;
    }

    internal XmlElement GetOrCreateTagRoot(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", "tagLst", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "p", PptxNamespaces.Presentation);
            document.AppendChild(root);
        }

        return root;
    }

    internal XmlElement? FindTagElement(XmlElement root, string name)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "tag" && PptxXml.GetAttributeValue(element, "", "name") == name)
            {
                return element;
            }
        }

        return null;
    }

    internal string AllocateTagsPartName()
    {
        int index = 1;
        string partName = "/ppt/tags/tag" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/tags/tag" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal static string CreateTagsXml()
    {
        return "<p:tagLst xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>";
    }
    internal List<PptxCustomXmlPart> GetCustomXmlParts()
    {
        List<PptxCustomXmlPart> parts = new List<PptxCustomXmlPart>();
        if (Part != null)
        {
            AddCustomXmlPartsFromRelationships(Part.Relationships, parts);
        }

        AddCustomXmlPartsFromRelationships(Package.RootRelationships, parts);
        for (int index = 0; index < Package.Parts.Count; index++)
        {
            AddCustomXmlPartsFromRelationships(Package.Parts[index].Relationships, parts);
        }

        return parts;
    }

    internal void AddCustomXmlPartsFromRelationships(PptxRelationshipCollection relationships, List<PptxCustomXmlPart> parts)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.Type != PptxRelationshipTypes.CustomXml)
            {
                continue;
            }

            if (relationship.TargetMode != PptxRelationshipTargetMode.Internal)
            {
                continue;
            }

            if (!Package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            if (!ContainsCustomXmlPart(parts, relationship.TargetPartName))
            {
                parts.Add(new PptxCustomXmlPart(this, Package.GetPart(relationship.TargetPartName), relationship));
            }
        }
    }

    internal bool ContainsCustomXmlPart(List<PptxCustomXmlPart> parts, string partName)
    {
        for (int index = 0; index < parts.Count; index++)
        {
            if (parts[index].PartName == partName)
            {
                return true;
            }
        }

        return false;
    }
    internal PptxCustomXmlPart AddCustomXmlPart(string xml)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part was not found.");
        }

        string partName = AllocateCustomXmlPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.Xml, Encoding.UTF8.GetBytes(PptxDiagnostic.CleanText(xml)));
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, partName);
        PptxRelationship relationship = Package.AddRelationship(Part.PartName, PptxRelationshipTypes.CustomXml, target, PptxRelationshipTargetMode.Internal);
        return new PptxCustomXmlPart(this, part, relationship);
    }
    internal bool RemoveCustomXmlPart(PptxCustomXmlPart customXmlPart)
    {
        if (customXmlPart == null)
        {
            throw new System.ArgumentNullException("customXmlPart");
        }

        if (customXmlPart.Presentation != this)
        {
            throw new System.ArgumentException("Custom XML part belongs to another presentation.");
        }

        string partName = customXmlPart.Part.PartName;
        if (!Package.HasPart(partName))
        {
            return false;
        }

        for (int index = Package.RootRelationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = Package.RootRelationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == partName)
            {
                Package.RemoveRelationship("", relationship.Id);
            }
        }

        for (int partIndex = 0; partIndex < Package.Parts.Count; partIndex++)
        {
            PptxPart source = Package.Parts[partIndex];
            if (source.PartName == partName)
            {
                continue;
            }

            for (int relationshipIndex = source.Relationships.Relationships.Count - 1; relationshipIndex >= 0; relationshipIndex--)
            {
                PptxRelationship relationship = source.Relationships.Relationships[relationshipIndex];
                if (relationship.TargetMode == PptxRelationshipTargetMode.Internal && relationship.TargetPartName == partName)
                {
                    Package.RemoveRelationship(source.PartName, relationship.Id);
                }
            }
        }

        Package.RemovePart(partName);
        return true;
    }

    internal string AllocateCustomXmlPartName()
    {
        int index = 1;
        string partName = "/customXml/item" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/customXml/item" + index.ToString() + ".xml";
        }

        return partName;
    }
    internal int ReplaceText(string search, string replacement)
    {
        return PptxTextReplacerCore.ReplacePresentationText(this, search, replacement);
    }
    internal int FillTemplate(PptxTemplateData data)
    {
        return PptxTemplateFillerCore.FillPresentation(this, data);
    }
    internal int FillTemplateFromDictionary(Dictionary<string, string> values)
    {
        return PptxTemplateFillerCore.FillPresentation(this, values);
    }
    internal int FillTemplateFromLists(List<string> keys, List<string> values)
    {
        return PptxTemplateFillerCore.FillPresentation(this, keys, values);
    }

    internal PptxPart GetOrCreateNotesSlidePart(PptxSlide slide)
    {
        PptxPart? existing = FindNotesSlidePart(slide);
        if (existing != null)
        {
            return existing;
        }

        string partName = AllocateNotesSlidePartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.NotesSlide, Encoding.UTF8.GetBytes(CreateNotesSlideXml()));
        string slideTarget = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, partName);
        Package.AddRelationship(slide.PartName, PptxRelationshipTypes.NotesSlide, slideTarget, PptxRelationshipTargetMode.Internal);
        string notesTarget = PptxSlideCloner.BuildRelativeTargetPath(partName, slide.PartName);
        Package.AddRelationship(partName, PptxRelationshipTypes.Slide, notesTarget, PptxRelationshipTargetMode.Internal);
        if (NotesMaster != null)
        {
            string masterTarget = PptxSlideCloner.BuildRelativeTargetPath(partName, NotesMaster.PartName);
            Package.AddRelationship(partName, PptxRelationshipTypes.NotesMaster, masterTarget, PptxRelationshipTargetMode.Internal);
        }

        return part;
    }

    internal PptxNotesSlide GetOrCreateNotesSlide(PptxSlide slide)
    {
        PptxNotesSlide? existing = FindNotesSlide(slide);
        if (existing != null)
        {
            return existing;
        }

        PptxPart part = GetOrCreateNotesSlidePart(slide);
        PptxRelationship? relationship = FindNotesSlideRelationship(slide);
        if (relationship == null)
        {
            throw new System.ArgumentException("Notes slide relationship was not found.");
        }

        return new PptxNotesSlide(this, slide, part, relationship.Id);
    }
    internal PptxNotesSlide? FindNotesSlide(PptxSlide slide)
    {
        PptxRelationship? relationship = FindNotesSlideRelationship(slide);
        if (relationship == null)
        {
            return null;
        }

        if (!Package.HasPart(relationship.TargetPartName))
        {
            return null;
        }

        return new PptxNotesSlide(this, slide, Package.GetPart(relationship.TargetPartName), relationship.Id);
    }

    internal PptxRelationship? FindNotesSlideRelationship(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.NotesSlide)
            {
                return relationship;
            }
        }

        return null;
    }

    internal int NotesSlideCount()
    {
        int count = 0;
        for (int index = 0; index < Slides.Count; index++)
        {
            if (FindNotesSlideRelationship(Slides[index]) != null)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal PptxNotesSlide? GetNotesSlide(int slideIndex)
    {
        return FindNotesSlide(GetSlide(slideIndex));
    }

    internal bool RemoveNotesSlide(PptxSlide slide)
    {
        ValidateSlide(slide);
        PptxRelationship? relationship = FindNotesSlideRelationship(slide);
        if (relationship == null)
        {
            return false;
        }

        string partName = relationship.TargetPartName;
        Package.RemoveRelationship(slide.PartName, relationship.Id);
        if (Package.HasPart(partName) && !NotesPartReferencedByOtherSlide(partName, slide))
        {
            Package.RemovePart(partName);
        }

        return true;
    }

    internal bool NotesPartReferencedByOtherSlide(string partName, PptxSlide excludedSlide)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < Slides.Count; index++)
        {
            PptxSlide slide = Slides[index];
            if (slide == excludedSlide)
            {
                continue;
            }

            PptxRelationship? relationship = FindNotesSlideRelationship(slide);
            if (relationship != null && relationship.TargetPartName == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal PptxNotesMaster? GetNotesMaster()
    {
        return NotesMaster;
    }

    internal PptxNotesMaster GetOrCreateNotesMaster()
    {
        if (NotesMaster != null)
        {
            return NotesMaster;
        }

        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        string partName = AllocateNotesMasterPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.NotesMaster, Encoding.UTF8.GetBytes(CreateNotesMasterXml()));
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, partName);
        PptxRelationship relationship = Package.AddRelationship(Part.PartName, PptxRelationshipTypes.NotesMaster, target, PptxRelationshipTargetMode.Internal);
        NotesMaster = new PptxNotesMaster(part, relationship.Id);
        if (Theme != null)
        {
            string themeTarget = PptxSlideCloner.BuildRelativeTargetPath(partName, Theme.PartName);
            Package.AddRelationship(partName, PptxRelationshipTypes.Theme, themeTarget, PptxRelationshipTargetMode.Internal);
            NotesMaster.Themes.Add(Theme);
        }

        for (int index = 0; index < Slides.Count; index++)
        {
            PptxPart? notesPart = FindNotesSlidePart(Slides[index]);
            if (notesPart != null && !PartHasRelationshipType(notesPart, PptxRelationshipTypes.NotesMaster))
            {
                string notesTarget = PptxSlideCloner.BuildRelativeTargetPath(notesPart.PartName, partName);
                Package.AddRelationship(notesPart.PartName, PptxRelationshipTypes.NotesMaster, notesTarget, PptxRelationshipTargetMode.Internal);
            }
        }

        return NotesMaster;
    }

    internal bool RemoveNotesMaster()
    {
        if (NotesMaster == null)
        {
            return false;
        }

        string partName = NotesMaster.PartName;
        RemoveRelationshipsByTypeAndTarget(Part, PptxRelationshipTypes.NotesMaster, partName);
        for (int index = 0; index < Slides.Count; index++)
        {
            PptxPart? notesPart = FindNotesSlidePart(Slides[index]);
            if (notesPart != null)
            {
                RemoveRelationshipsByTypeAndTarget(notesPart, PptxRelationshipTypes.NotesMaster, partName);
            }
        }

        if (Package.HasPart(partName))
        {
            Package.RemovePart(partName);
        }

        NotesMaster = null;
        return true;
    }

    internal PptxHandoutMaster? GetHandoutMaster()
    {
        return HandoutMaster;
    }

    internal PptxHandoutMaster GetOrCreateHandoutMaster()
    {
        if (HandoutMaster != null)
        {
            return HandoutMaster;
        }

        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        string partName = AllocateHandoutMasterPartName();
        PptxPart part = Package.AddPart(partName, PptxContentTypes.HandoutMaster, Encoding.UTF8.GetBytes(CreateHandoutMasterXml()));
        string target = PptxSlideCloner.BuildRelativeTargetPath(Part.PartName, partName);
        PptxRelationship relationship = Package.AddRelationship(Part.PartName, PptxRelationshipTypes.HandoutMaster, target, PptxRelationshipTargetMode.Internal);
        HandoutMaster = new PptxHandoutMaster(part, relationship.Id);
        if (Theme != null)
        {
            string themeTarget = PptxSlideCloner.BuildRelativeTargetPath(partName, Theme.PartName);
            Package.AddRelationship(partName, PptxRelationshipTypes.Theme, themeTarget, PptxRelationshipTargetMode.Internal);
            HandoutMaster.Themes.Add(Theme);
        }

        return HandoutMaster;
    }

    internal bool RemoveHandoutMaster()
    {
        if (HandoutMaster == null)
        {
            return false;
        }

        string partName = HandoutMaster.PartName;
        RemoveRelationshipsByTypeAndTarget(Part, PptxRelationshipTypes.HandoutMaster, partName);
        if (Package.HasPart(partName))
        {
            Package.RemovePart(partName);
        }

        HandoutMaster = null;
        return true;
    }

    internal bool PartHasRelationshipType(PptxPart part, string relationshipType)
    {
        for (int index = 0; index < part.Relationships.Relationships.Count; index++)
        {
            if (part.Relationships.Relationships[index].Type == relationshipType)
            {
                return true;
            }
        }

        return false;
    }

    internal void RemoveRelationshipsByTypeAndTarget(PptxPart? part, string relationshipType, string targetPartName)
    {
        if (part == null)
        {
            return;
        }

        string normalized = PptxUriCore.NormalizePartName(targetPartName);
        for (int index = part.Relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = part.Relationships.Relationships[index];
            if (relationship.Type == relationshipType && relationship.TargetPartName == normalized)
            {
                Package.RemoveRelationship(part.PartName, relationship.Id);
            }
        }
    }

    internal string AllocateNotesMasterPartName()
    {
        int index = 1;
        string partName = "/ppt/notesMasters/notesMaster" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/notesMasters/notesMaster" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal string AllocateHandoutMasterPartName()
    {
        int index = 1;
        string partName = "/ppt/handoutMasters/handoutMaster" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/handoutMasters/handoutMaster" + index.ToString() + ".xml";
        }

        return partName;
    }

    internal PptxPart? FindNotesSlidePart(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type != PptxRelationshipTypes.NotesSlide)
            {
                continue;
            }

            if (Package.HasPart(relationship.TargetPartName))
            {
                return Package.GetPart(relationship.TargetPartName);
            }
        }

        return null;
    }

    internal string AllocateNotesSlidePartName()
    {
        int index = 1;
        string partName = "/ppt/notesSlides/notesSlide" + index.ToString() + ".xml";
        while (Package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/notesSlides/notesSlide" + index.ToString() + ".xml";
        }

        return partName;
    }
    internal PptxSlide? FindSlideById(int slideId)
    {
        return SlideCollection.FindById(slideId);
    }
    internal PptxSlide AddSlide()
    {
        return SlideCollection.Add();
    }
    internal PptxSlide AddSlideWithLayout(string layoutName)
    {
        return SlideCollection.AddWithLayout(layoutName);
    }
    internal PptxSlide InsertSlide(int index)
    {
        return SlideCollection.Insert(index);
    }
    internal PptxSlide InsertSlideWithLayout(int index, string layoutName)
    {
        return SlideCollection.InsertWithLayoutName(index, layoutName);
    }
    internal PptxSlide CloneSlide(int index)
    {
        return SlideCollection.Clone(index);
    }
    internal PptxSlide CloneSlideToIndex(int index, int insertIndex)
    {
        return SlideCollection.CloneToIndex(index, insertIndex);
    }
    internal PptxSlide ImportSlide(PptxPresentation sourcePresentation, int sourceIndex)
    {
        return SlideCollection.Import(sourcePresentation, sourceIndex);
    }
    internal PptxSlide ImportSlideToIndex(PptxPresentation sourcePresentation, int sourceIndex, int insertIndex)
    {
        return SlideCollection.ImportToIndex(sourcePresentation, sourceIndex, insertIndex);
    }
    internal PptxSlide ImportDocumentSlide(PptxDocument sourceDocument, int sourceIndex)
    {
        return ImportSourceSlide((IPptxPresentationSource)sourceDocument, sourceIndex);
    }
    internal PptxSlide ImportSourceSlide(IPptxPresentationSource source, int sourceIndex)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return ImportSlide(source.LoadPresentation(), sourceIndex);
    }
    internal int Merge(PptxPresentation sourcePresentation)
    {
        return PptxMergerCore.Merge(this, sourcePresentation);
    }
    internal int MergeDocument(PptxDocument sourceDocument)
    {
        return PptxMergerCore.Merge(this, sourceDocument);
    }
    internal int MergeSource(IPptxPresentationSource source)
    {
        return PptxMergerCore.Merge(this, source);
    }
    internal List<PptxDocument> Split()
    {
        return PptxSplitterCore.Split(this);
    }
    internal PptxDocument SplitRange(int startIndex, int count)
    {
        return PptxSplitterCore.SplitRange(this, startIndex, count);
    }
    internal void RemoveSlideAt(int index)
    {
        SlideCollection.RemoveAt(index);
    }
    internal bool RemoveSlideById(int slideId)
    {
        return SlideCollection.RemoveById(slideId);
    }
    internal void MoveSlide(int fromIndex, int toIndex)
    {
        SlideCollection.Move(fromIndex, toIndex);
    }
    internal int SlideMasterCount()
    {
        return SlideMasters.Count;
    }
    internal int SlideLayoutCount()
    {
        return SlideLayouts.Count;
    }
    internal int ThemeCount()
    {
        return Themes.Count;
    }
    internal PptxSlideMaster GetSlideMaster(int index)
    {
        if (index < 0 || index >= SlideMasters.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return SlideMasters[index];
    }
    internal PptxSlideLayout GetSlideLayout(int index)
    {
        if (index < 0 || index >= SlideLayouts.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return SlideLayouts[index];
    }
    internal PptxTheme GetTheme(int index)
    {
        if (index < 0 || index >= Themes.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Themes[index];
    }
    internal PptxCoreProperties GetCoreProperties()
    {
        if (CoreProperties != null)
        {
            return CoreProperties;
        }

        PptxPart part = EnsureRootPropertyPart(PptxPartNames.CoreProperties, PptxContentTypes.CoreProperties, PptxMinimumPresentation.CorePropertiesXml(), PptxRelationshipTypes.CoreProperties, "docProps/core.xml");
        CoreProperties = new PptxCoreProperties(part, FindRootRelationshipId(PptxRelationshipTypes.CoreProperties, part.PartName));
        return CoreProperties;
    }
    internal PptxAppProperties GetAppProperties()
    {
        if (AppProperties != null)
        {
            return AppProperties;
        }

        PptxPart part = EnsureRootPropertyPart(PptxPartNames.ExtendedProperties, PptxContentTypes.ExtendedProperties, PptxMinimumPresentation.ExtendedPropertiesXml(), PptxRelationshipTypes.ExtendedProperties, "docProps/app.xml");
        AppProperties = new PptxAppProperties(part, FindRootRelationshipId(PptxRelationshipTypes.ExtendedProperties, part.PartName));
        return AppProperties;
    }
    internal PptxCustomProperties GetCustomProperties()
    {
        if (CustomProperties != null)
        {
            return CustomProperties;
        }

        PptxPart part = EnsureRootPropertyPart(PptxPartNames.CustomProperties, PptxContentTypes.CustomProperties, PptxCustomProperties.CreateEmptyXml(), PptxRelationshipTypes.CustomProperties, "docProps/custom.xml");
        CustomProperties = new PptxCustomProperties(part, FindRootRelationshipId(PptxRelationshipTypes.CustomProperties, part.PartName));
        return CustomProperties;
    }
    internal PptxPresentationProperties GetPresentationProperties()
    {
        if (Properties != null)
        {
            return Properties;
        }

        PptxPart part = PptxPresentationSettingsCore.EnsurePresentationPropertiesPart(this);
        if (Properties == null)
        {
            throw new System.ArgumentException("Presentation properties are required.");
        }

        return Properties;
    }
    internal PptxThumbnail? GetThumbnail()
    {
        PptxRelationship? relationship = FindThumbnailRelationship();
        if (relationship == null || relationship.TargetMode != PptxRelationshipTargetMode.Internal || !Package.HasPart(relationship.TargetPartName))
        {
            return null;
        }

        return CreateThumbnail(Package.GetPart(relationship.TargetPartName), relationship);
    }
    internal PptxThumbnail AddThumbnail(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);
        if (info.ContentType == PptxContentTypes.Binary)
        {
            throw new System.ArgumentException("Thumbnail image format was not recognized.");
        }

        return AddThumbnailWithContentType(bytes, info.ContentType);
    }
    internal PptxThumbnail AddThumbnailWithContentType(byte[] bytes, string contentType)
    {
        if (FindThumbnailRelationship() != null)
        {
            throw new System.ArgumentException("A document thumbnail already exists.");
        }

        return SetThumbnailValue(bytes, contentType);
    }
    internal PptxThumbnail ReplaceThumbnail(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxImageInfo info = PptxImageDetectorCore.Detect(bytes);
        if (info.ContentType == PptxContentTypes.Binary)
        {
            throw new System.ArgumentException("Thumbnail image format was not recognized.");
        }

        return ReplaceThumbnailWithContentType(bytes, info.ContentType);
    }
    internal PptxThumbnail ReplaceThumbnailWithContentType(byte[] bytes, string contentType)
    {
        if (FindThumbnailRelationship() == null)
        {
            throw new System.ArgumentException("A document thumbnail does not exist.");
        }

        return SetThumbnailValue(bytes, contentType);
    }
    internal PptxThumbnail SetThumbnailValue(byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        string cleanContentType = PptxPart.ValidateAndNormalizeContentType(contentType);
        string extension = GetThumbnailExtension(cleanContentType);
        PptxRelationship? existing = FindThumbnailRelationship();
        if (existing != null && existing.TargetMode == PptxRelationshipTargetMode.Internal && Package.HasPart(existing.TargetPartName))
        {
            PptxPart oldPart = Package.GetPart(existing.TargetPartName);
            if (oldPart.ContentType == cleanContentType)
            {
                PptxPart replaced = Package.ReplacePart(oldPart.PartName, cleanContentType, bytes);
                return CreateThumbnail(replaced, existing);
            }
        }

        if (existing != null)
        {
            Package.RemoveRelationship("", existing.Id);
            if (existing.TargetMode == PptxRelationshipTargetMode.Internal && Package.HasPart(existing.TargetPartName))
            {
                Package.RemovePart(existing.TargetPartName);
            }
        }

        string partName = AllocateThumbnailPartName(extension);
        PptxPart part = Package.AddPart(partName, cleanContentType, bytes);
        string target = PptxUriCore.ToZipEntryName(partName);
        PptxRelationship relationship = Package.AddRelationship("", PptxRelationshipTypes.Thumbnail, target, PptxRelationshipTargetMode.Internal);
        return CreateThumbnail(part, relationship);
    }
    internal bool RemoveThumbnail()
    {
        bool removed = false;
        PptxRelationship? relationship = FindThumbnailRelationship();
        while (relationship != null)
        {
            string partName = relationship.TargetPartName;
            bool removePart = relationship.TargetMode == PptxRelationshipTargetMode.Internal && Package.HasPart(partName);
            Package.RemoveRelationship("", relationship.Id);
            if (removePart)
            {
                Package.RemovePart(partName);
            }

            removed = true;
            relationship = FindThumbnailRelationship();
        }

        return removed;
    }
    internal PptxRelationship? FindThumbnailRelationship()
    {
        for (int index = 0; index < Package.RootRelationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Package.RootRelationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Thumbnail)
            {
                return relationship;
            }
        }

        return null;
    }
    internal PptxThumbnail CreateThumbnail(PptxPart part, PptxRelationship relationship)
    {
        PptxThumbnail thumbnail = new PptxThumbnail();
        thumbnail.RelationshipId = relationship.Id;
        thumbnail.PartName = part.PartName;
        thumbnail.ContentType = part.ContentType;
        thumbnail.Bytes = part.GetBytes();
        thumbnail.Info = PptxImageDetectorCore.Detect(thumbnail.Bytes);
        return thumbnail;
    }
    internal string AllocateThumbnailPartName(string extension)
    {
        string partName = "/docProps/thumbnail." + extension;
        int index = 1;
        while (Package.HasPart(partName))
        {
            partName = "/docProps/thumbnail" + index.ToString() + "." + extension;
            index = index + 1;
        }

        return partName;
    }
    internal string GetThumbnailExtension(string contentType)
    {
        if (contentType == PptxContentTypes.Png)
        {
            return "png";
        }

        if (contentType == PptxContentTypes.Jpeg)
        {
            return "jpeg";
        }

        if (contentType == PptxContentTypes.Gif)
        {
            return "gif";
        }

        if (contentType == PptxContentTypes.Bmp)
        {
            return "bmp";
        }

        if (contentType == PptxContentTypes.Tiff)
        {
            return "tiff";
        }

        if (contentType == PptxContentTypes.Svg)
        {
            return "svg";
        }

        if (contentType == PptxContentTypes.Emf)
        {
            return "emf";
        }

        if (contentType == PptxContentTypes.Wmf)
        {
            return "wmf";
        }

        throw new System.ArgumentException("Thumbnail content type must identify a supported image format.");
    }

    internal PptxPart EnsureRootPropertyPart(string partName, string contentType, string xml, string relationshipType, string target)
    {
        PptxPart part;
        if (Package.HasPart(partName))
        {
            part = Package.GetPart(partName);
        }
        else
        {
            part = Package.AddPart(partName, contentType, System.Text.Encoding.UTF8.GetBytes(xml));
        }

        if (FindRootRelationshipId(relationshipType, part.PartName).Length == 0)
        {
            Package.AddRelationship("", relationshipType, target, PptxRelationshipTargetMode.Internal);
        }

        return part;
    }

    internal string FindRootRelationshipId(string relationshipType, string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < Package.RootRelationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Package.RootRelationships.Relationships[index];
            if (relationship.Type == relationshipType && relationship.TargetPartName == normalized)
            {
                return relationship.Id;
            }
        }

        return "";
    }

    internal void LoadModel()
    {
        LoadRootRelationships();

        if (Part == null)
        {
            return;
        }

        LoadPresentationRelationships();
        LoadPresentationSlideIds();
        LoadPresentationSettings();
        LoadSlideMasterRelationships();
        LoadSlideRelationships();
        LoadSlideLayoutRelationships();
        LoadPlaceholderMetadata();
        LoadSlidePlaceholderMetadata();
        LoadNotesMasterRelationships();
        LoadHandoutMasterRelationships();
    }

    internal void LoadRootRelationships()
    {
        for (int index = 0; index < Package.RootRelationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = Package.RootRelationships.Relationships[index];

            if (relationship.Type == PptxRelationshipTypes.OfficeDocument || relationship.Type == PptxRelationshipTypes.Presentation)
            {
                LoadPresentationPart(relationship);
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.CoreProperties)
            {
                PptxPart? part = GetRelatedPart(relationship, "PPTM020", "Presentation core properties relationship target is missing.");
                if (part != null)
                {
                    CoreProperties = new PptxCoreProperties(part, relationship.Id);
                }

                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.ExtendedProperties)
            {
                PptxPart? part = GetRelatedPart(relationship, "PPTM021", "Presentation app properties relationship target is missing.");
                if (part != null)
                {
                    AppProperties = new PptxAppProperties(part, relationship.Id);
                }

                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.CustomProperties)
            {
                PptxPart? part = GetRelatedPart(relationship, "PPTM022", "Presentation custom properties relationship target is missing.");
                if (part != null)
                {
                    CustomProperties = new PptxCustomProperties(part, relationship.Id);
                }
            }
        }

        if (Part == null)
        {
            Diagnostics.AddError("PPTM010", "Package does not contain a root presentation relationship.", "", "", "");
        }
    }

    internal void LoadPresentationPart(PptxRelationship relationship)
    {
        if (Part != null)
        {
            Diagnostics.AddWarning("PPTM011", "Additional root presentation relationship was ignored.", "", relationship.Id, "");
            return;
        }

        PptxPart? part = GetRelatedPart(relationship, "PPTM012", "Root presentation relationship target is missing.");
        if (part != null)
        {
            Part = part;
        }
    }

    internal void LoadPresentationRelationships()
    {
        if (Part == null)
        {
            return;
        }

        PptxRelationshipCollection relationships = Part.Relationships;
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];

            if (relationship.Type == PptxRelationshipTypes.Slide)
            {
                AddSlideFromRelationship(relationship);
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.SlideMaster)
            {
                AddSlideMasterFromRelationship(relationship);
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.PresentationProperties)
            {
                PptxPart? part = GetRelatedPart(relationship, "PPTM023", "Presentation properties relationship target is missing.");
                if (part != null)
                {
                    Properties = new PptxPresentationProperties(part, relationship.Id);
                }

                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.ViewProperties)
            {
                ViewPropertiesPart = GetRelatedPart(relationship, "PPTM024", "View properties relationship target is missing.");
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.TableStyles)
            {
                TableStylesPart = GetRelatedPart(relationship, "PPTM025", "Table styles relationship target is missing.");
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.NotesMaster)
            {
                AddNotesMasterFromRelationship(relationship);
                continue;
            }

            if (relationship.Type == PptxRelationshipTypes.HandoutMaster)
            {
                AddHandoutMasterFromRelationship(relationship);
            }
        }
    }

    internal PptxSlide? AddSlideFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM030", "Slide relationship target is missing.");
        if (part == null)
        {
            return null;
        }

        PptxSlide? existing = FindSlide(part.PartName);
        if (existing != null)
        {
            return existing;
        }

        PptxSlide slide = new PptxSlide(this, part, relationship.Id);
        Slides.Add(slide);
        return slide;
    }

    internal void LoadPresentationSlideIds()
    {
        if (Part == null)
        {
            return;
        }

        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(Part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? list = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "sldIdLst");
        if (list == null)
        {
            return;
        }

        List<PptxSlide> ordered = new List<PptxSlide>();
        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.Presentation || element.LocalName != "sldId")
            {
                continue;
            }

            string relationshipId = PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "id");
            PptxSlide? slide = FindSlideByRelationshipId(relationshipId);
            if (slide == null)
            {
                continue;
            }

            slide.SlideId = PptxXml.GetIntAttribute(element, "", "id", 0);
            string show = PptxXml.GetAttributeValue(element, "", "show");
            if (show == "0" || show == "false")
            {
                slide.Hidden = true;
            }

            if (!ListContainsSlide(ordered, slide.PartName))
            {
                ordered.Add(slide);
            }
        }

        for (int index = 0; index < Slides.Count; index++)
        {
            if (!ListContainsSlide(ordered, Slides[index].PartName))
            {
                ordered.Add(Slides[index]);
            }
        }

        Slides.Clear();
        for (int index = 0; index < ordered.Count; index++)
        {
            Slides.Add(ordered[index]);
        }
    }

    internal PptxSlideMaster? AddSlideMasterFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM031", "Slide master relationship target is missing.");
        if (part == null)
        {
            return null;
        }

        PptxSlideMaster? existing = FindSlideMaster(part.PartName);
        if (existing != null)
        {
            return existing;
        }

        PptxSlideMaster master = new PptxSlideMaster(part, relationship.Id);
        SlideMasters.Add(master);
        return master;
    }

    internal PptxSlideLayout? AddSlideLayoutFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM032", "Slide layout relationship target is missing.");
        if (part == null)
        {
            return null;
        }

        PptxSlideLayout? existing = FindSlideLayout(part.PartName);
        if (existing != null)
        {
            return existing;
        }

        PptxSlideLayout layout = new PptxSlideLayout(part, relationship.Id);
        SlideLayouts.Add(layout);
        return layout;
    }

    internal PptxTheme? AddThemeFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM033", "Theme relationship target is missing.");
        if (part == null)
        {
            return null;
        }

        PptxTheme? existing = FindTheme(part.PartName);
        if (existing != null)
        {
            return existing;
        }

        PptxTheme theme = new PptxTheme(part, relationship.Id);
        Themes.Add(theme);
        if (Theme == null)
        {
            Theme = theme;
        }

        return theme;
    }

    internal void AddNotesMasterFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM034", "Notes master relationship target is missing.");
        if (part != null)
        {
            NotesMaster = new PptxNotesMaster(part, relationship.Id);
        }
    }

    internal void AddHandoutMasterFromRelationship(PptxRelationship relationship)
    {
        PptxPart? part = GetRelatedPart(relationship, "PPTM035", "Handout master relationship target is missing.");
        if (part != null)
        {
            HandoutMaster = new PptxHandoutMaster(part, relationship.Id);
        }
    }

    internal void LoadSlideMasterRelationships()
    {
        for (int index = 0; index < SlideMasters.Count; index++)
        {
            PptxSlideMaster master = SlideMasters[index];
            for (int relIndex = 0; relIndex < master.Part.Relationships.Relationships.Count; relIndex++)
            {
                PptxRelationship relationship = master.Part.Relationships.Relationships[relIndex];
                if (relationship.Type == PptxRelationshipTypes.Theme)
                {
                    PptxTheme? theme = AddThemeFromRelationship(relationship);
                    if (theme != null && !MasterContainsTheme(master, theme.PartName))
                    {
                        master.Themes.Add(theme);
                    }

                    continue;
                }

                if (relationship.Type == PptxRelationshipTypes.SlideLayout)
                {
                    PptxSlideLayout? layout = AddSlideLayoutFromRelationship(relationship);
                    if (layout != null)
                    {
                        layout.Master = master;
                        if (!MasterContainsLayout(master, layout.PartName))
                        {
                            master.Layouts.Add(layout);
                        }
                    }
                }
            }
        }
    }

    internal void LoadSlideRelationships()
    {
        for (int index = 0; index < Slides.Count; index++)
        {
            PptxSlide slide = Slides[index];
            for (int relIndex = 0; relIndex < slide.Part.Relationships.Relationships.Count; relIndex++)
            {
                PptxRelationship relationship = slide.Part.Relationships.Relationships[relIndex];
                if (relationship.Type == PptxRelationshipTypes.SlideLayout)
                {
                    slide.Layout = AddSlideLayoutFromRelationship(relationship);
                }
            }
        }
    }

    internal void LoadSlideLayoutRelationships()
    {
        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            PptxSlideLayout layout = SlideLayouts[index];
            for (int relIndex = 0; relIndex < layout.Part.Relationships.Relationships.Count; relIndex++)
            {
                PptxRelationship relationship = layout.Part.Relationships.Relationships[relIndex];
                if (relationship.Type == PptxRelationshipTypes.SlideMaster)
                {
                    PptxPart? part = GetRelatedPart(relationship, "PPTM036", "Slide layout master relationship target is missing.");
                    if (part != null)
                    {
                        PptxSlideMaster? master = FindSlideMaster(part.PartName);
                        if (master != null)
                        {
                            layout.Master = master;
                        }
                    }
                }
            }
        }
    }

    internal void LoadPlaceholderMetadata()
    {
        for (int index = 0; index < SlideMasters.Count; index++)
        {
            PptxSlideMaster master = SlideMasters[index];
            master.Placeholders.Clear();
            LoadSlideMasterMetadata(master);
            LoadPlaceholdersForPart(master.Part, master.Placeholders);
        }

        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            PptxSlideLayout layout = SlideLayouts[index];
            layout.Placeholders.Clear();
            layout.EffectivePlaceholders.Clear();
            LoadSlideLayoutMetadata(layout);
            LoadPlaceholdersForPart(layout.Part, layout.Placeholders);
            ApplyLayoutPlaceholderInheritance(layout);
        }
    }

    internal void LoadSlideMasterMetadata(PptxSlideMaster master)
    {
        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(master.Part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData != null)
        {
            master.Name = PptxXml.GetAttributeValue(commonSlideData, "", "name");
        }
    }

    internal void LoadSlideLayoutMetadata(PptxSlideLayout layout)
    {
        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(layout.Part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        layout.Type = PptxXml.GetAttributeValue(root, "", "type");

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData != null)
        {
            layout.Name = PptxXml.GetAttributeValue(commonSlideData, "", "name");
        }
    }

    internal void LoadPlaceholdersForPart(PptxPart part, List<PptxPlaceholder> placeholders)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (placeholders == null)
        {
            throw new System.ArgumentNullException("placeholders");
        }

        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        CollectPlaceholders(root, part.PartName, placeholders);
    }

    internal void CollectPlaceholders(XmlNode node, string partName, List<PptxPlaceholder> placeholders)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "ph")
            {
                placeholders.Add(CreatePlaceholder(element, partName));
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                CollectPlaceholders(child, partName, placeholders);
            }
        }
    }

    internal PptxPlaceholder CreatePlaceholder(XmlElement element, string partName)
    {
        PptxPlaceholder placeholder = new PptxPlaceholder();
        string cleanPartName = PptxDiagnostic.CleanText(partName);
        if (cleanPartName.Length > 0)
        {
            placeholder.SourcePartName = PptxUriCore.NormalizePartName(cleanPartName);
        }

        placeholder.Type = NormalizePlaceholderType(PptxXml.GetAttributeValue(element, "", "type"));
        placeholder.Index = PptxXml.GetIntAttribute(element, "", "idx", -1);
        placeholder.Size = PptxXml.GetAttributeValue(element, "", "sz");

        XmlElement? shape = FindPlaceholderShape(element);
        if (shape != null)
        {
            XmlElement? properties = FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
            if (properties != null)
            {
                placeholder.ShapeId = PptxXml.GetAttributeValue(properties, "", "id");
                placeholder.ShapeName = PptxXml.GetAttributeValue(properties, "", "name");
            }
        }

        return placeholder;
    }

    internal static string NormalizePlaceholderType(string type)
    {
        string clean = NormalizePlaceholderAlias(type);
        if (clean.Length == 0)
        {
            return "obj";
        }

        return clean;
    }

    internal static string NormalizePlaceholderAlias(string type)
    {
        string clean = PptxDiagnostic.CleanText(type);
        if (clean == "content")
        {
            return "obj";
        }

        if (clean == "object")
        {
            return "obj";
        }

        if (clean == "picture")
        {
            return "pic";
        }

        if (clean == "table")
        {
            return "tbl";
        }

        return clean;
    }

    internal XmlElement? FindPlaceholderShape(XmlElement placeholder)
    {
        XmlNode? node = placeholder.ParentNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (element.NamespaceURI == PptxNamespaces.Presentation)
                {
                    if (element.LocalName == "sp" || element.LocalName == "pic" || element.LocalName == "graphicFrame" || element.LocalName == "grpSp")
                    {
                        return element;
                    }
                }
            }

            node = node.ParentNode;
        }

        return null;
    }

    internal XmlElement? FindFirstDescendant(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }

            XmlElement? descendant = FindFirstDescendant(element, namespaceUri, localName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    internal void ApplyLayoutPlaceholderInheritance(PptxSlideLayout layout)
    {
        for (int index = 0; index < layout.Placeholders.Count; index++)
        {
            PptxPlaceholder placeholder = layout.Placeholders[index];
            PptxPlaceholder? masterPlaceholder = FindMatchingMasterPlaceholder(layout, placeholder);
            if (masterPlaceholder != null)
            {
                placeholder.Inherited = true;
                placeholder.InheritedFromPartName = masterPlaceholder.SourcePartName;
                placeholder.InheritedShapeId = masterPlaceholder.ShapeId;
                placeholder.InheritedShapeName = masterPlaceholder.ShapeName;
            }

            layout.EffectivePlaceholders.Add(placeholder);
        }

        if (layout.Master == null)
        {
            return;
        }

        for (int index = 0; index < layout.Master.Placeholders.Count; index++)
        {
            PptxPlaceholder masterPlaceholder = layout.Master.Placeholders[index];
            PptxPlaceholder? layoutPlaceholder = FindMatchingLayoutPlaceholder(layout, masterPlaceholder);
            if (layoutPlaceholder == null)
            {
                layout.EffectivePlaceholders.Add(CreateInheritedPlaceholder(masterPlaceholder, layout.PartName));
            }
        }
    }

    internal PptxPlaceholder CreateInheritedPlaceholder(PptxPlaceholder masterPlaceholder, string layoutPartName)
    {
        PptxPlaceholder placeholder = new PptxPlaceholder();
        placeholder.SourcePartName = PptxUriCore.NormalizePartName(layoutPartName);
        placeholder.ShapeId = masterPlaceholder.ShapeId;
        placeholder.ShapeName = masterPlaceholder.ShapeName;
        placeholder.Type = masterPlaceholder.Type;
        placeholder.Index = masterPlaceholder.Index;
        placeholder.Size = masterPlaceholder.Size;
        placeholder.Inherited = true;
        placeholder.InheritedFromPartName = masterPlaceholder.SourcePartName;
        placeholder.InheritedShapeId = masterPlaceholder.ShapeId;
        placeholder.InheritedShapeName = masterPlaceholder.ShapeName;
        return placeholder;
    }

    internal PptxPlaceholder? FindMatchingMasterPlaceholder(PptxSlideLayout layout, PptxPlaceholder placeholder)
    {
        if (layout.Master == null)
        {
            return null;
        }

        for (int index = 0; index < layout.Master.Placeholders.Count; index++)
        {
            PptxPlaceholder candidate = layout.Master.Placeholders[index];
            if (PlaceholderMatches(candidate, placeholder))
            {
                return candidate;
            }
        }

        return null;
    }

    internal PptxPlaceholder? FindMatchingLayoutPlaceholder(PptxSlideLayout layout, PptxPlaceholder masterPlaceholder)
    {
        for (int index = 0; index < layout.Placeholders.Count; index++)
        {
            PptxPlaceholder candidate = layout.Placeholders[index];
            if (PlaceholderMatches(candidate, masterPlaceholder))
            {
                return candidate;
            }
        }

        return null;
    }

    internal static bool PlaceholderMatches(PptxPlaceholder left, PptxPlaceholder right)
    {
        if (left.Type != right.Type)
        {
            return false;
        }

        if (left.Index >= 0 && right.Index >= 0)
        {
            return left.Index == right.Index;
        }

        if (left.Index < 0 && right.Index < 0)
        {
            return true;
        }

        return false;
    }

    internal void LoadSlidePlaceholderMetadata()
    {
        for (int index = 0; index < Slides.Count; index++)
        {
            PptxSlide slide = Slides[index];
            slide.ReloadPlaceholders();
            ApplySlidePlaceholderInheritance(slide);
        }
    }

    internal void ApplySlidePlaceholderInheritance(PptxSlide slide)
    {
        if (slide.Layout == null)
        {
            return;
        }

        for (int index = 0; index < slide.Placeholders.Count; index++)
        {
            PptxPlaceholder placeholder = slide.Placeholders[index];
            PptxPlaceholder? layoutPlaceholder = FindMatchingEffectiveLayoutPlaceholder(slide.Layout, placeholder);
            if (layoutPlaceholder != null)
            {
                placeholder.Inherited = true;
                placeholder.InheritedFromPartName = layoutPlaceholder.SourcePartName;
                placeholder.InheritedShapeId = layoutPlaceholder.ShapeId;
                placeholder.InheritedShapeName = layoutPlaceholder.ShapeName;
            }
        }
    }

    internal PptxPlaceholder? FindMatchingEffectiveLayoutPlaceholder(PptxSlideLayout layout, PptxPlaceholder placeholder)
    {
        for (int index = 0; index < layout.EffectivePlaceholders.Count; index++)
        {
            PptxPlaceholder candidate = layout.EffectivePlaceholders[index];
            if (PlaceholderMatches(candidate, placeholder))
            {
                return candidate;
            }
        }

        return null;
    }

    internal void LoadNotesMasterRelationships()
    {
        if (NotesMaster == null)
        {
            return;
        }

        for (int index = 0; index < NotesMaster.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = NotesMaster.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Theme)
            {
                PptxTheme? theme = AddThemeFromRelationship(relationship);
                if (theme != null)
                {
                    NotesMaster.Themes.Add(theme);
                }
            }
        }
    }

    internal void LoadHandoutMasterRelationships()
    {
        if (HandoutMaster == null)
        {
            return;
        }

        for (int index = 0; index < HandoutMaster.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = HandoutMaster.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Theme)
            {
                PptxTheme? theme = AddThemeFromRelationship(relationship);
                if (theme != null)
                {
                    HandoutMaster.Themes.Add(theme);
                }
            }
        }
    }

    internal PptxPart? GetRelatedPart(PptxRelationship relationship, string code, string message)
    {
        if (relationship.TargetMode == PptxRelationshipTargetMode.External)
        {
            Diagnostics.AddError(code, message, relationship.SourcePartName, relationship.Id, "");
            return null;
        }

        if (!Package.HasPart(relationship.TargetPartName))
        {
            Diagnostics.AddError(code, message, relationship.SourcePartName, relationship.Id, relationship.TargetPartName);
            return null;
        }

        return Package.GetPart(relationship.TargetPartName);
    }

    internal PptxSlide? FindSlide(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < Slides.Count; index++)
        {
            if (Slides[index].PartName == normalized)
            {
                return Slides[index];
            }
        }

        return null;
    }

    internal PptxSlide? FindSlideByRelationshipId(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        for (int index = 0; index < Slides.Count; index++)
        {
            if (Slides[index].RelationshipId == clean)
            {
                return Slides[index];
            }
        }

        return null;
    }

    internal bool ListContainsSlide(List<PptxSlide> slides, string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < slides.Count; index++)
        {
            if (slides[index].PartName == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal void LoadPresentationSettings()
    {
        Sections.Clear();
        CustomShows.Clear();
        if (Part == null)
        {
            return;
        }

        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(Part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? slideSize = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "sldSz");
        if (slideSize != null)
        {
            SlideWidth = GetLongAttribute(slideSize, "cx", 0L);
            SlideHeight = GetLongAttribute(slideSize, "cy", 0L);
            SlideSizeType = PptxXml.GetAttributeValue(slideSize, "", "type");
        }

        LoadSections(root);
        LoadCustomShows(root);
        ValidateLoadedSlideIdReferences();
    }
    internal string GetSlideOrientation()
    {
        if (SlideHeight > SlideWidth)
        {
            return "portrait";
        }

        return "landscape";
    }
    internal void SetSlideSize(long width, long height)
    {
        SetSlideSizeWithType(width, height, "custom");
    }
    internal void SetSlideSizeWithType(long width, long height, string type)
    {
        SlideWidth = width;
        SlideHeight = height;
        SlideSizeType = PptxDiagnostic.CleanText(type);
        SaveSlideSize();
    }
    internal void SetSlideOrientation(string orientation)
    {
        string clean = PptxDiagnostic.CleanText(orientation);
        if (clean == "portrait" && SlideWidth > SlideHeight)
        {
            long oldWidth = SlideWidth;
            SlideWidth = SlideHeight;
            SlideHeight = oldWidth;
            SaveSlideSize();
            return;
        }

        if (clean == "landscape" && SlideHeight > SlideWidth)
        {
            long oldHeight = SlideHeight;
            SlideHeight = SlideWidth;
            SlideWidth = oldHeight;
            SaveSlideSize();
        }
    }

    internal void SaveSlideSize()
    {
        XmlDocument document = GetPresentationXmlDocument();
        XmlElement root = GetPresentationRoot(document);
        XmlElement slideSize = PptxXml.GetOrCreateChild(root, "p", "sldSz", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(slideSize, "", "", "cx", SlideWidth.ToString());
        PptxXml.SetAttributeValue(slideSize, "", "", "cy", SlideHeight.ToString());
        if (SlideSizeType.Length > 0)
        {
            PptxXml.SetAttributeValue(slideSize, "", "", "type", SlideSizeType);
        }

        SetPresentationXmlDocument(document);
    }
    internal void SetSlideHidden(int index, bool hidden)
    {
        PptxSlide slide = GetSlide(index);
        slide.Hidden = hidden;
        SlideCollection.RewritePresentationSlideIdList();
    }
    internal PptxSection AddSection(string name)
    {
        PptxSection section = new PptxSection();
        section.Presentation = this;
        section.Name = PptxDiagnostic.CleanText(name);
        section.Id = AllocateSectionId();
        Sections.Add(section);
        SaveSections();
        return section;
    }
    internal void AddSlideToSection(int sectionIndex, int slideId)
    {
        if (sectionIndex < 0 || sectionIndex >= Sections.Count)
        {
            throw new System.ArgumentOutOfRangeException("sectionIndex");
        }

        AddSlideToSectionValue(Sections[sectionIndex], slideId);
    }
    internal int SectionCount()
    {
        return Sections.Count;
    }
    internal PptxSection GetSection(int index)
    {
        if (index < 0 || index >= Sections.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Sections[index];
    }
    internal PptxSection? FindSectionByName(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        for (int index = 0; index < Sections.Count; index++)
        {
            if (Sections[index].Name == clean)
            {
                return Sections[index];
            }
        }

        return null;
    }
    internal void AddSlideToSectionValue(PptxSection section, int slideId)
    {
        ValidateSection(section);
        ValidateReferencedSlideId(slideId);
        section.SlideIds.Add(slideId);
        SaveSections();
    }
    internal void RenameSection(PptxSection section, string name)
    {
        ValidateSection(section);
        section.Name = PptxDiagnostic.CleanText(name);
        SaveSections();
    }
    internal void SetSectionSlideIds(PptxSection section, List<int> slideIds)
    {
        ValidateSection(section);
        ValidateReferencedSlideIds(slideIds);
        CopySlideIds(slideIds, section.SlideIds);
        SaveSections();
    }
    internal void InsertSectionSlideId(PptxSection section, int index, int slideId)
    {
        ValidateSection(section);
        if (index < 0 || index > section.SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        ValidateReferencedSlideId(slideId);
        section.SlideIds.Insert(index, slideId);
        SaveSections();
    }
    internal void RemoveSectionSlideIdAt(PptxSection section, int index)
    {
        ValidateSection(section);
        if (index < 0 || index >= section.SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        section.SlideIds.RemoveAt(index);
        SaveSections();
    }
    internal bool RemoveSection(PptxSection section)
    {
        ValidateSection(section);
        for (int index = 0; index < Sections.Count; index++)
        {
            if (Sections[index] == section)
            {
                Sections.RemoveAt(index);
                section.Presentation = null;
                SaveSections();
                return true;
            }
        }

        return false;
    }
    internal void RemoveSectionAt(int index)
    {
        RemoveSection(GetSection(index));
    }
    internal PptxCustomShow AddCustomShow(string name)
    {
        PptxCustomShow show = new PptxCustomShow();
        show.Presentation = this;
        show.Name = PptxDiagnostic.CleanText(name);
        show.Id = AllocateCustomShowId();
        CustomShows.Add(show);
        SaveCustomShows();
        return show;
    }
    internal void AddSlideToCustomShow(int customShowIndex, int slideId)
    {
        if (customShowIndex < 0 || customShowIndex >= CustomShows.Count)
        {
            throw new System.ArgumentOutOfRangeException("customShowIndex");
        }

        AddSlideToCustomShowValue(CustomShows[customShowIndex], slideId);
    }
    internal int CustomShowCount()
    {
        return CustomShows.Count;
    }
    internal PptxCustomShow GetCustomShow(int index)
    {
        if (index < 0 || index >= CustomShows.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return CustomShows[index];
    }
    internal PptxCustomShow? FindCustomShowByName(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        for (int index = 0; index < CustomShows.Count; index++)
        {
            if (CustomShows[index].Name == clean)
            {
                return CustomShows[index];
            }
        }

        return null;
    }
    internal void AddSlideToCustomShowValue(PptxCustomShow show, int slideId)
    {
        ValidateCustomShow(show);
        ValidateReferencedSlideId(slideId);
        show.SlideIds.Add(slideId);
        SaveCustomShows();
    }
    internal void RenameCustomShow(PptxCustomShow show, string name)
    {
        ValidateCustomShow(show);
        show.Name = PptxDiagnostic.CleanText(name);
        SaveCustomShows();
    }
    internal void SetCustomShowSlideIds(PptxCustomShow show, List<int> slideIds)
    {
        ValidateCustomShow(show);
        ValidateReferencedSlideIds(slideIds);
        CopySlideIds(slideIds, show.SlideIds);
        SaveCustomShows();
    }
    internal void InsertCustomShowSlideId(PptxCustomShow show, int index, int slideId)
    {
        ValidateCustomShow(show);
        if (index < 0 || index > show.SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        ValidateReferencedSlideId(slideId);
        show.SlideIds.Insert(index, slideId);
        SaveCustomShows();
    }
    internal void RemoveCustomShowSlideIdAt(PptxCustomShow show, int index)
    {
        ValidateCustomShow(show);
        if (index < 0 || index >= show.SlideIds.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        show.SlideIds.RemoveAt(index);
        SaveCustomShows();
    }
    internal bool RemoveCustomShow(PptxCustomShow show)
    {
        ValidateCustomShow(show);
        for (int index = 0; index < CustomShows.Count; index++)
        {
            if (CustomShows[index] == show)
            {
                CustomShows.RemoveAt(index);
                show.Presentation = null;
                SaveCustomShows();
                return true;
            }
        }

        return false;
    }
    internal void RemoveCustomShowAt(int index)
    {
        RemoveCustomShow(GetCustomShow(index));
    }
    internal void ValidateSection(PptxSection section)
    {
        if (section == null)
        {
            throw new System.ArgumentNullException("section");
        }

        for (int index = 0; index < Sections.Count; index++)
        {
            if (Sections[index] == section)
            {
                return;
            }
        }

        throw new System.ArgumentException("Section does not belong to this presentation.");
    }
    internal void ValidateCustomShow(PptxCustomShow show)
    {
        if (show == null)
        {
            throw new System.ArgumentNullException("show");
        }

        for (int index = 0; index < CustomShows.Count; index++)
        {
            if (CustomShows[index] == show)
            {
                return;
            }
        }

        throw new System.ArgumentException("Custom show does not belong to this presentation.");
    }
    internal void ValidateReferencedSlideId(int slideId)
    {
        if (SlideCollection.FindById(slideId) == null)
        {
            throw new System.ArgumentException("Referenced slide id was not found.");
        }
    }
    internal void ValidateReferencedSlideIds(List<int> slideIds)
    {
        if (slideIds == null)
        {
            throw new System.ArgumentNullException("slideIds");
        }

        for (int index = 0; index < slideIds.Count; index++)
        {
            ValidateReferencedSlideId(slideIds[index]);
        }
    }
    internal static void CopySlideIds(List<int> source, List<int> destination)
    {
        List<int> copy = new List<int>();
        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        destination.Clear();
        for (int index = 0; index < copy.Count; index++)
        {
            destination.Add(copy[index]);
        }
    }
    internal void RemoveSlideIdReferences(int slideId)
    {
        bool sectionsChanged = false;
        for (int index = 0; index < Sections.Count; index++)
        {
            List<int> slideIds = Sections[index].SlideIds;
            for (int slideIndex = slideIds.Count - 1; slideIndex >= 0; slideIndex--)
            {
                if (slideIds[slideIndex] == slideId)
                {
                    slideIds.RemoveAt(slideIndex);
                    sectionsChanged = true;
                }
            }
        }

        bool showsChanged = false;
        for (int index = 0; index < CustomShows.Count; index++)
        {
            List<int> slideIds = CustomShows[index].SlideIds;
            for (int slideIndex = slideIds.Count - 1; slideIndex >= 0; slideIndex--)
            {
                if (slideIds[slideIndex] == slideId)
                {
                    slideIds.RemoveAt(slideIndex);
                    showsChanged = true;
                }
            }
        }

        if (sectionsChanged)
        {
            SaveSections();
        }

        if (showsChanged)
        {
            SaveCustomShows();
        }
    }

    internal int AllocateSectionId()
    {
        int maximum = 0;
        for (int index = 0; index < Sections.Count; index++)
        {
            if (Sections[index].Id > maximum)
            {
                maximum = Sections[index].Id;
            }
        }

        return maximum + 1;
    }

    internal int AllocateCustomShowId()
    {
        int maximum = 0;
        for (int index = 0; index < CustomShows.Count; index++)
        {
            if (CustomShows[index].Id > maximum)
            {
                maximum = CustomShows[index].Id;
            }
        }

        return maximum + 1;
    }

    internal void SaveSections()
    {
        XmlDocument document = GetPresentationXmlDocument();
        XmlElement root = GetPresentationRoot(document);
        RemoveDirectChild(root, PptxNamespaces.Presentation, "sectionLst");
        if (Sections.Count > 0)
        {
            XmlElement list = PptxXml.AppendChildElement(root, "p", "sectionLst", PptxNamespaces.Presentation);
            for (int index = 0; index < Sections.Count; index++)
            {
                PptxSection section = Sections[index];
                XmlElement item = PptxXml.AppendChildElement(list, "p", "section", PptxNamespaces.Presentation);
                PptxXml.SetAttributeValue(item, "", "", "name", section.Name);
                PptxXml.SetAttributeValue(item, "", "", "id", section.Id.ToString());
                XmlElement slideList = PptxXml.AppendChildElement(item, "p", "sldIdLst", PptxNamespaces.Presentation);
                AppendSlideIdChildren(slideList, section.SlideIds);
            }
        }

        SetPresentationXmlDocument(document);
    }

    internal void SaveCustomShows()
    {
        XmlDocument document = GetPresentationXmlDocument();
        XmlElement root = GetPresentationRoot(document);
        RemoveDirectChild(root, PptxNamespaces.Presentation, "custShowLst");
        if (CustomShows.Count > 0)
        {
            XmlElement list = PptxXml.AppendChildElement(root, "p", "custShowLst", PptxNamespaces.Presentation);
            for (int index = 0; index < CustomShows.Count; index++)
            {
                PptxCustomShow show = CustomShows[index];
                XmlElement item = PptxXml.AppendChildElement(list, "p", "custShow", PptxNamespaces.Presentation);
                PptxXml.SetAttributeValue(item, "", "", "name", show.Name);
                PptxXml.SetAttributeValue(item, "", "", "id", show.Id.ToString());
                XmlElement slideList = PptxXml.AppendChildElement(item, "p", "sldLst", PptxNamespaces.Presentation);
                AppendCustomShowSlideChildren(slideList, show.SlideIds);
            }
        }

        SetPresentationXmlDocument(document);
    }

    internal void LoadSections(XmlElement root)
    {
        XmlElement? list = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "sectionLst");
        if (list == null)
        {
            return;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "section")
            {
                PptxSection section = new PptxSection();
                section.Presentation = this;
                section.Name = PptxXml.GetAttributeValue(element, "", "name");
                section.Id = PptxXml.GetIntAttribute(element, "", "id", 0);
                LoadSlideIdChildren(PptxXml.FindFirstChild(element, PptxNamespaces.Presentation, "sldIdLst"), section.SlideIds);
                Sections.Add(section);
            }
        }
    }

    internal void LoadCustomShows(XmlElement root)
    {
        XmlElement? list = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "custShowLst");
        if (list == null)
        {
            return;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "custShow")
            {
                PptxCustomShow show = new PptxCustomShow();
                show.Presentation = this;
                show.Name = PptxXml.GetAttributeValue(element, "", "name");
                show.Id = PptxXml.GetIntAttribute(element, "", "id", 0);
                LoadCustomShowSlideChildren(PptxXml.FindFirstChild(element, PptxNamespaces.Presentation, "sldLst"), show.SlideIds);
                CustomShows.Add(show);
            }
        }
    }

    internal static void AppendSlideIdChildren(XmlElement parent, List<int> slideIds)
    {
        for (int index = 0; index < slideIds.Count; index++)
        {
            XmlElement item = PptxXml.AppendChildElement(parent, "p", "sld", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(item, "", "", "id", slideIds[index].ToString());
        }
    }

    internal void AppendCustomShowSlideChildren(XmlElement parent, List<int> slideIds)
    {
        for (int index = 0; index < slideIds.Count; index++)
        {
            ValidateReferencedSlideId(slideIds[index]);
            PptxSlide? slide = SlideCollection.FindById(slideIds[index]);
            if (slide != null)
            {
                XmlElement item = PptxXml.AppendChildElement(parent, "p", "sld", PptxNamespaces.Presentation);
                PptxXml.SetAttributeValue(item, "r", PptxNamespaces.Relationships, "id", slide.RelationshipId);
            }
        }
    }

    internal void LoadCustomShowSlideChildren(XmlElement? parent, List<int> slideIds)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.Presentation || element.LocalName != "sld")
            {
                continue;
            }

            string relationshipId = PptxXml.GetAttributeValue(element, PptxNamespaces.Relationships, "id");
            if (relationshipId.Length > 0)
            {
                PptxSlide? slide = FindSlideByRelationshipId(relationshipId);
                if (slide == null)
                {
                    string partName = "";
                    if (Part != null)
                    {
                        partName = Part.PartName;
                    }

                    Diagnostics.AddError("PPTM038", "Custom show references a slide relationship that was not found.", partName, relationshipId, "");
                }
                else
                {
                    slideIds.Add(slide.SlideId);
                }

                continue;
            }

            slideIds.Add(PptxXml.GetIntAttribute(element, "", "id", 0));
        }
    }

    internal void ValidateLoadedSlideIdReferences()
    {
        string partName = "";
        if (Part != null)
        {
            partName = Part.PartName;
        }

        for (int index = 0; index < Sections.Count; index++)
        {
            PptxSection section = Sections[index];
            for (int slideIndex = 0; slideIndex < section.SlideIds.Count; slideIndex++)
            {
                int slideId = section.SlideIds[slideIndex];
                if (SlideCollection.FindById(slideId) == null)
                {
                    Diagnostics.AddError("PPTM037", "Section references a slide id that was not found.", partName, "", slideId.ToString());
                }
            }
        }

        for (int index = 0; index < CustomShows.Count; index++)
        {
            PptxCustomShow show = CustomShows[index];
            for (int slideIndex = 0; slideIndex < show.SlideIds.Count; slideIndex++)
            {
                int slideId = show.SlideIds[slideIndex];
                if (SlideCollection.FindById(slideId) == null)
                {
                    Diagnostics.AddError("PPTM038", "Custom show references a slide id that was not found.", partName, "", slideId.ToString());
                }
            }
        }
    }

    internal static void LoadSlideIdChildren(XmlElement? parent, List<int> slideIds)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation)
            {
                slideIds.Add(PptxXml.GetIntAttribute(element, "", "id", 0));
            }
        }
    }

    internal XmlDocument GetPresentationXmlDocument()
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        if (Part.HasXmlDocument)
        {
            return Part.XmlDocument;
        }

        return PptxXml.LoadBytes(Package.GetPartBytesForSave(Part));
    }

    internal XmlDocument GetPartXmlDocument(PptxPart part)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (part.HasXmlDocument)
        {
            return part.XmlDocument;
        }

        return PptxXml.LoadBytes(Package.GetPartBytesForSave(part));
    }

    internal XmlElement GetOrCreateNotesTextBody(XmlDocument document)
    {
        XmlElement root = GetNotesRoot(document);
        XmlElement? existing = FindNotesTextBody(root);
        if (existing != null)
        {
            PptxTextFrame.EnsureTextBodyDefaults(existing);
            return existing;
        }

        XmlElement shapeTree = GetOrCreateNotesShapeTree(root);
        XmlElement shape = CreateNotesTextShape(document);
        shapeTree.AppendChild(shape);
        XmlElement? textBody = PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, "txBody");
        if (textBody == null)
        {
            throw new System.ArgumentException("Notes text body is required.");
        }

        return textBody;
    }

    internal XmlElement? FindNotesTextBody(XmlElement root)
    {
        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData != null)
        {
            XmlElement? shapeTree = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "spTree");
            if (shapeTree != null)
            {
                for (int index = 0; index < shapeTree.ChildNodes.Count; index++)
                {
                    XmlNode? child = shapeTree.ChildNodes[index];
                    if (child == null || child.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    XmlElement shape = (XmlElement)child;
                    if (!PptxShapeTree.IsShapeElement(shape))
                    {
                        continue;
                    }

                    if (IsNotesBodyPlaceholder(shape))
                    {
                        XmlElement? textBody = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "txBody");
                        if (textBody != null)
                        {
                            return textBody;
                        }
                    }
                }
            }
        }

        return PptxXml.FindFirstDescendant(root, PptxNamespaces.Presentation, "txBody");
    }

    internal bool IsNotesBodyPlaceholder(XmlElement shape)
    {
        XmlElement? placeholder = PptxXml.FindFirstDescendant(shape, PptxNamespaces.Presentation, "ph");
        if (placeholder == null)
        {
            return false;
        }

        string type = PptxXml.GetAttributeValue(placeholder, "", "type");
        if (type == "body")
        {
            return true;
        }

        return false;
    }

    internal XmlElement GetNotesRoot(XmlDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", "notes", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "a", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "r", PptxNamespaces.Relationships);
            document.AppendChild(root);
        }

        return root;
    }

    internal XmlElement GetOrCreateNotesShapeTree(XmlElement root)
    {
        XmlElement commonSlideData = PptxXml.GetOrCreateChild(root, "p", "cSld", PptxNamespaces.Presentation);
        XmlElement shapeTree = PptxXml.GetOrCreateChild(commonSlideData, "p", "spTree", PptxNamespaces.Presentation);
        if (PptxXml.FindFirstChild(shapeTree, PptxNamespaces.Presentation, "nvGrpSpPr") == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(shapeTree);
            XmlElement nonVisual = document.CreateElement("p", "nvGrpSpPr", PptxNamespaces.Presentation);
            XmlElement properties = document.CreateElement("p", "cNvPr", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(properties, "", "", "id", "1");
            PptxXml.SetAttributeValue(properties, "", "", "name", "");
            nonVisual.AppendChild(properties);
            nonVisual.AppendChild(document.CreateElement("p", "cNvGrpSpPr", PptxNamespaces.Presentation));
            nonVisual.AppendChild(document.CreateElement("p", "nvPr", PptxNamespaces.Presentation));
            shapeTree.InsertBefore(nonVisual, shapeTree.FirstChild);
        }

        if (PptxXml.FindFirstChild(shapeTree, PptxNamespaces.Presentation, "grpSpPr") == null)
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(shapeTree);
            XmlElement groupProperties = document.CreateElement("p", "grpSpPr", PptxNamespaces.Presentation);
            shapeTree.InsertBefore(groupProperties, GetFirstNotesShapeElement(shapeTree));
        }

        return shapeTree;
    }

    internal XmlElement? GetFirstNotesShapeElement(XmlElement shapeTree)
    {
        for (int index = 0; index < shapeTree.ChildNodes.Count; index++)
        {
            XmlNode? child = shapeTree.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "sp")
            {
                return element;
            }
        }

        return null;
    }

    internal XmlElement CreateNotesTextShape(XmlDocument document)
    {
        XmlDocument shapeDocument = PptxXml.LoadString(CreateNotesTextShapeXml());
        XmlElement? root = shapeDocument.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Notes text shape XML is required.");
        }

        XmlNode imported = document.ImportNode(root, true);
        return (XmlElement)imported;
    }

    internal static string CreateNotesSlideXml()
    {
        return
            "<p:notes xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateNotesTextShapeXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:notes>";
    }

    internal static string CreateNotesMasterXml()
    {
        return
            "<p:notesMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:hf/><p:notesStyle/>" +
            "</p:notesMaster>";
    }

    internal static string CreateHandoutMasterXml()
    {
        return
            "<p:handoutMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:hf/>" +
            "</p:handoutMaster>";
    }

    internal static string CreateNotesTextShapeXml()
    {
        return
            "<p:sp xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:nvSpPr>" +
            "<p:cNvPr id=\"2\" name=\"Notes Placeholder 1\"/>" +
            "<p:cNvSpPr/>" +
            "<p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr>" +
            "</p:nvSpPr>" +
            "<p:spPr/>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }

    internal XmlElement GetPresentationRoot(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Presentation XML root is required.");
        }

        return root;
    }

    internal void SetPresentationXmlDocument(XmlDocument document)
    {
        if (Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        Part.SetXmlDocument(document);
    }

    internal static long GetLongAttribute(XmlElement element, string localName, long fallback)
    {
        string value = PptxXml.GetAttributeValue(element, "", localName);
        long parsed = 0L;
        if (long.TryParse(value, out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    internal static void RemoveDirectChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                root.RemoveChild(child);
            }
        }
    }

    internal PptxSlideMaster? FindSlideMaster(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < SlideMasters.Count; index++)
        {
            if (SlideMasters[index].PartName == normalized)
            {
                return SlideMasters[index];
            }
        }

        return null;
    }

    internal PptxSlideLayout? FindSlideLayout(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            if (SlideLayouts[index].PartName == normalized)
            {
                return SlideLayouts[index];
            }
        }

        return null;
    }
    internal PptxSlideLayout? FindSlideLayoutByName(string layoutName)
    {
        string clean = PptxDiagnostic.CleanText(layoutName);
        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            PptxSlideLayout layout = SlideLayouts[index];
            if (layout.Name == clean)
            {
                return layout;
            }

            if (layout.Type == clean)
            {
                return layout;
            }

            if (layout.PartName == clean)
            {
                return layout;
            }
        }

        return null;
    }

    internal PptxSlideMaster? FindSlideMasterByName(string masterName)
    {
        string clean = PptxDiagnostic.CleanText(masterName);
        for (int index = 0; index < SlideMasters.Count; index++)
        {
            PptxSlideMaster master = SlideMasters[index];
            if (master.Name == clean || master.PartName == clean)
            {
                return master;
            }
        }

        return null;
    }

    internal PptxSlideLayout? FindSlideLayoutByType(string layoutType)
    {
        string clean = PptxDiagnostic.CleanText(layoutType);
        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            if (SlideLayouts[index].Type == clean)
            {
                return SlideLayouts[index];
            }
        }

        return null;
    }

    internal PptxTheme? FindThemeByName(string themeName)
    {
        string clean = PptxDiagnostic.CleanText(themeName);
        for (int index = 0; index < Themes.Count; index++)
        {
            PptxTheme theme = Themes[index];
            if (theme.Name == clean || theme.PartName == clean)
            {
                return theme;
            }
        }

        return null;
    }

    internal PptxTheme? GetThemeForMaster(PptxSlideMaster master)
    {
        ValidateSlideMaster(master);
        if (master.Themes.Count == 0)
        {
            return null;
        }

        return master.Themes[0];
    }

    internal void BindTheme(PptxSlideMaster master, PptxTheme theme)
    {
        ValidateSlideMaster(master);
        ValidateTheme(theme);
        string relationshipId = "";
        for (int index = 0; index < master.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = master.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Theme)
            {
                relationshipId = relationship.Id;
                break;
            }
        }

        for (int index = master.Part.Relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = master.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.Theme && relationship.Id != relationshipId)
            {
                Package.RemoveRelationship(master.PartName, relationship.Id);
            }
        }

        string target = PptxSlideCloner.BuildRelativeTargetPath(master.PartName, theme.PartName);
        if (relationshipId.Length == 0)
        {
            Package.AddRelationship(master.PartName, PptxRelationshipTypes.Theme, target, PptxRelationshipTargetMode.Internal);
        }
        else
        {
            Package.SetRelationshipTarget(master.PartName, relationshipId, target, PptxRelationshipTargetMode.Internal);
        }

        master.Themes.Clear();
        master.Themes.Add(theme);
    }

    internal void ApplySlideLayout(PptxSlide slide, PptxSlideLayout layout)
    {
        ValidateSlide(slide);
        ValidateSlideLayout(layout);
        string relationshipId = "";
        for (int index = 0; index < slide.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.SlideLayout)
            {
                relationshipId = relationship.Id;
                break;
            }
        }

        for (int index = slide.Part.Relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = slide.Part.Relationships.Relationships[index];
            if (relationship.Type == PptxRelationshipTypes.SlideLayout && relationship.Id != relationshipId)
            {
                Package.RemoveRelationship(slide.PartName, relationship.Id);
            }
        }

        string target = PptxSlideCloner.BuildRelativeTargetPath(slide.PartName, layout.PartName);
        if (relationshipId.Length == 0)
        {
            Package.AddRelationship(slide.PartName, PptxRelationshipTypes.SlideLayout, target, PptxRelationshipTargetMode.Internal);
        }
        else
        {
            Package.SetRelationshipTarget(slide.PartName, relationshipId, target, PptxRelationshipTargetMode.Internal);
        }

        slide.Layout = layout;
        slide.ReloadPlaceholders();
        ApplySlidePlaceholderInheritance(slide);
    }

    internal int ReplaceSlideLayout(PptxSlideLayout currentLayout, PptxSlideLayout replacementLayout)
    {
        ValidateSlideLayout(currentLayout);
        ValidateSlideLayout(replacementLayout);
        int count = 0;
        for (int index = 0; index < Slides.Count; index++)
        {
            PptxSlide slide = Slides[index];
            if (slide.Layout == currentLayout)
            {
                ApplySlideLayout(slide, replacementLayout);
                count = count + 1;
            }
        }

        return count;
    }

    internal PptxPlaceholderInheritance InspectPlaceholderInheritance(PptxSlide slide, string placeholderType, int placeholderIndex)
    {
        ValidateSlide(slide);
        PptxPlaceholderInheritance result = new PptxPlaceholderInheritance();
        PptxPlaceholder selector = new PptxPlaceholder();
        selector.Type = NormalizePlaceholderType(placeholderType);
        selector.Index = placeholderIndex;
        result.SlidePlaceholder = FindMatchingPlaceholder(slide.Placeholders, selector);
        if (slide.Layout != null)
        {
            result.LayoutPlaceholder = FindMatchingPlaceholder(slide.Layout.Placeholders, selector);
            if (slide.Layout.Master != null)
            {
                result.MasterPlaceholder = FindMatchingPlaceholder(slide.Layout.Master.Placeholders, selector);
            }
        }

        return result;
    }

    internal PptxPlaceholder? FindMatchingPlaceholder(List<PptxPlaceholder> placeholders, PptxPlaceholder selector)
    {
        for (int index = 0; index < placeholders.Count; index++)
        {
            if (PlaceholderMatches(placeholders[index], selector))
            {
                return placeholders[index];
            }
        }

        return null;
    }

    internal PptxInheritedBackground GetEffectiveBackground(PptxSlide slide)
    {
        ValidateSlide(slide);
        PptxInheritedBackground result = CreateBackgroundResult(slide.Part, "slide", false);
        if (result.Fill.Present)
        {
            return result;
        }

        if (slide.Layout != null)
        {
            result = CreateBackgroundResult(slide.Layout.Part, "layout", true);
            if (result.Fill.Present)
            {
                return result;
            }

            if (slide.Layout.Master != null)
            {
                result = CreateBackgroundResult(slide.Layout.Master.Part, "master", true);
                if (result.Fill.Present)
                {
                    return result;
                }
            }
        }

        return new PptxInheritedBackground();
    }

    internal PptxInheritedBackground CreateBackgroundResult(PptxPart part, string sourceLevel, bool inherited)
    {
        PptxInheritedBackground result = new PptxInheritedBackground();
        result.Fill = GetBackgroundFillForPart(part);
        if (result.Fill.Present)
        {
            result.SourcePartName = part.PartName;
            result.SourceLevel = sourceLevel;
            result.Inherited = inherited;
        }

        return result;
    }

    internal PptxFill GetBackgroundFillForPart(PptxPart part)
    {
        XmlDocument document = PptxXml.LoadBytes(Package.GetPartBytesForSave(part));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return new PptxFill();
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData == null)
        {
            return new PptxFill();
        }

        XmlElement? background = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "bg");
        if (background == null)
        {
            return new PptxFill();
        }

        XmlElement? properties = PptxXml.FindFirstChild(background, PptxNamespaces.Presentation, "bgPr");
        return PptxFill.FromShapeProperties(properties);
    }

    internal void ValidateSlide(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        for (int index = 0; index < Slides.Count; index++)
        {
            if (Slides[index] == slide)
            {
                return;
            }
        }

        throw new System.ArgumentException("Slide does not belong to this presentation.");
    }

    internal void ValidateSlideMaster(PptxSlideMaster master)
    {
        if (master == null)
        {
            throw new System.ArgumentNullException("master");
        }

        for (int index = 0; index < SlideMasters.Count; index++)
        {
            if (SlideMasters[index] == master)
            {
                return;
            }
        }

        throw new System.ArgumentException("Slide master does not belong to this presentation.");
    }

    internal void ValidateSlideLayout(PptxSlideLayout layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        for (int index = 0; index < SlideLayouts.Count; index++)
        {
            if (SlideLayouts[index] == layout)
            {
                return;
            }
        }

        throw new System.ArgumentException("Slide layout does not belong to this presentation.");
    }

    internal void ValidateTheme(PptxTheme theme)
    {
        if (theme == null)
        {
            throw new System.ArgumentNullException("theme");
        }

        for (int index = 0; index < Themes.Count; index++)
        {
            if (Themes[index] == theme)
            {
                return;
            }
        }

        throw new System.ArgumentException("Theme does not belong to this presentation.");
    }

    internal PptxTheme? FindTheme(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < Themes.Count; index++)
        {
            if (Themes[index].PartName == normalized)
            {
                return Themes[index];
            }
        }

        return null;
    }

    internal bool MasterContainsLayout(PptxSlideMaster master, string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < master.Layouts.Count; index++)
        {
            if (master.Layouts[index].PartName == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal bool MasterContainsTheme(PptxSlideMaster master, string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < master.Themes.Count; index++)
        {
            if (master.Themes[index].PartName == normalized)
            {
                return true;
            }
        }

        return false;
    }
}
