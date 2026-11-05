using System;

namespace odf;

/// <summary>
/// Provides typed creation, editing, resource management, and extraction for drawings.
/// </summary>
public class OdfDrawingEditor : IOdfDrawingEditor
{
    /// <summary>Initializes a drawing editor.</summary>
    public OdfDrawingEditor()
    {
    }

    /// <summary>Gets all drawing objects in document order.</summary>
    public OdfDrawingObject[] GetDrawingObjects(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfDrawingObject[])document.DrawingObjects.Clone();
    }

    /// <summary>Finds an unambiguously named drawing object.</summary>
    public OdfDrawingObject? FindDrawingObject(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        OdfDrawingObject? result = null;
        int count = 0;
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index].Name == name)
            {
                result = document.DrawingObjects[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    /// <summary>Creates a drawing object beneath a parent or document body.</summary>
    public OdfDrawingObject CreateDrawingObject(
        OdfDocument document,
        OdfDrawingObject? parent,
        OdfDrawingKind kind,
        string name)
    {
        return DrawingEditingModel.CreateDrawing(
            document,
            parent,
            kind,
            name);
    }

    /// <summary>Removes a drawing subtree without collecting its package resources.</summary>
    public void RemoveDrawingObject(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        DrawingEditingModel.RemoveDrawing(document, drawing);
    }

    /// <summary>Copies a drawing subtree within one document.</summary>
    public OdfDrawingObject CopyDrawingObject(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent)
    {
        return DrawingEditingModel.CopyDrawing(
            document,
            drawing,
            targetParent);
    }

    /// <summary>Copies a drawing and its style and package dependencies across documents.</summary>
    public OdfDrawingObject ImportDrawingObject(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent)
    {
        return DrawingEditingModel.ImportDrawing(
            sourceDocument,
            targetDocument,
            drawing,
            targetParent);
    }

    /// <summary>Renames a drawing identifier and updates drawing references.</summary>
    public OdfDrawingObject RenameDrawingIdentifier(
        OdfDocument document,
        OdfDrawingObject drawing,
        string newIdentifier)
    {
        return DrawingEditingModel.RenameIdentifier(
            document,
            drawing,
            newIdentifier);
    }

    /// <summary>Applies changes made to drawing, geometry, and resource data fields.</summary>
    public void ApplyDrawingChanges(OdfDocument document)
    {
        DrawingEditingModel.ApplyChanges(document);
    }

    /// <summary>Gets all drawing image descriptions.</summary>
    public OdfImage[] GetImages(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfImage[])document.Images.Clone();
    }

    /// <summary>Adds a package image and a drawing image beneath a parent.</summary>
    public OdfImage AddPackageImage(
        OdfDocument document,
        OdfDrawingObject parent,
        string path,
        byte[] data,
        string mediaType,
        string name,
        bool deduplicate)
    {
        OdfImage packageImage = ImageResourceManager.AddPackageImage(
            document,
            path,
            data,
            mediaType,
            deduplicate);
        OdfDrawingObject drawing = DrawingEditingModel.CreateDrawing(
            document,
            parent,
            OdfDrawingKind.Image,
            name);
        OdfNode node = drawing.Node ?? new OdfNode();
        OdfImage image = FindImageByNode(document, node);
        image.Href = packageImage.PackagePath;
        image.XLinkType = "simple";
        image.DeclaredMediaType = packageImage.DeclaredMediaType;
        ImageResourceManager.SynchronizeImages(document);
        DrawingResourceManager.RefreshOrphans(document);
        return FindImageByNode(document, node);
    }

    /// <summary>Replaces locally stored image bytes.</summary>
    public OdfEditResult ReplaceImageData(
        OdfDocument document,
        OdfImage image,
        byte[] data,
        string mediaType)
    {
        OdfEditResult result = ImageResourceManager.ReplaceImageData(
            document,
            image,
            data,
            mediaType);
        if (result.Status == OdfEditStatus.Success
            || result.Status == OdfEditStatus.NoChange)
        {
            ImageResourceManager.SynchronizeImages(document);
            DrawingResourceManager.RefreshOrphans(document);
        }
        return result;
    }

    /// <summary>Changes an image to an external reference without fetching it.</summary>
    public OdfImage SetExternalImage(
        OdfDocument document,
        OdfImage image,
        string href,
        string mediaType)
    {
        RequireImageNode(document, image);
        OdfNode node = image.Node ?? new OdfNode();
        ImageResourceManager.SetExternalImage(image, href, mediaType);
        ImageResourceManager.SynchronizeImages(document);
        DrawingResourceManager.RefreshOrphans(document);
        return FindImageByNode(document, node);
    }

    /// <summary>Changes an image to inline base64 content.</summary>
    public OdfImage SetInlineImage(
        OdfDocument document,
        OdfImage image,
        byte[] data,
        string mediaType)
    {
        RequireImageNode(document, image);
        OdfNode node = image.Node ?? new OdfNode();
        ImageResourceManager.SetBase64Image(image, data, mediaType);
        ImageResourceManager.SynchronizeImages(document);
        DrawingResourceManager.RefreshOrphans(document);
        return FindImageByNode(document, node);
    }

    /// <summary>Extracts locally available image bytes.</summary>
    public byte[] ExtractImageData(
        OdfDocument document,
        OdfImage image)
    {
        return ImageResourceManager.ExtractImageData(document, image);
    }

    /// <summary>Gets passive media and active-content descriptions.</summary>
    public OdfMediaResource[] GetMediaResources(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfMediaResource[])document.MediaResources.Clone();
    }

    /// <summary>Changes a media resource reference without loading its content.</summary>
    public OdfMediaResource SetMediaReference(
        OdfDocument document,
        OdfMediaResource resource,
        string href,
        string mediaType)
    {
        RequireMediaNode(document, resource);
        if (!OdfIriResolver.IsIriReference(href))
        {
            throw new OdfEditException(
                "A media resource requires a valid IRI reference.");
        }
        OdfNode node = resource.Node ?? new OdfNode();
        resource.Href = href;
        resource.MediaType = mediaType;
        if (resource.XLinkType.Length == 0)
        {
            resource.XLinkType = "simple";
        }
        MediaResourceManager.SynchronizeMediaResources(document);
        DrawingResourceManager.RefreshOrphans(document);
        return FindMediaByNode(document, node);
    }

    /// <summary>Gets embedded objects.</summary>
    public OdfEmbeddedObject[] GetEmbeddedObjects(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfEmbeddedObject[])document.EmbeddedObjects.Clone();
    }

    /// <summary>Replaces a single-entry package or inline embedded object.</summary>
    public OdfEmbeddedObject ReplaceEmbeddedObjectData(
        OdfDocument document,
        OdfEmbeddedObject embeddedObject,
        byte[] data)
    {
        RequireEmbeddedObject(document, embeddedObject);
        if (embeddedObject.SourceKind == OdfResourceSourceKind.External)
        {
            throw new OdfEditException(
                "External embedded-object content cannot be replaced.");
        }
        if (embeddedObject.IsDirectoryPackage)
        {
            throw new OdfEditException(
                "Directory-form embedded objects require package-entry editing.");
        }
        OdfNode? node = embeddedObject.Node;
        embeddedObject.Data = CopyBytes(data);
        EmbeddedContentManager.SynchronizeEmbeddedContent(document);
        DrawingResourceManager.RefreshOrphans(document);
        return node == null
            ? FindEmbeddedByPath(document, embeddedObject.PackagePath)
            : FindEmbeddedByNode(document, node);
    }

    /// <summary>Extracts locally available single-entry embedded-object bytes.</summary>
    public byte[] ExtractEmbeddedObjectData(
        OdfDocument document,
        OdfEmbeddedObject embeddedObject)
    {
        return EmbeddedContentManager.ExtractEmbeddedObjectData(
            document,
            embeddedObject);
    }

    /// <summary>Gets chart descriptions.</summary>
    public OdfChart[] GetCharts(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfChart[])document.Charts.Clone();
    }

    /// <summary>Gets package thumbnails.</summary>
    public OdfThumbnail[] GetThumbnails(OdfDocument document)
    {
        RequireDocument(document);
        return (OdfThumbnail[])document.Thumbnails.Clone();
    }

    /// <summary>Extracts validated thumbnail bytes.</summary>
    public byte[] ExtractThumbnailData(OdfThumbnail thumbnail)
    {
        return EmbeddedContentManager.ExtractThumbnailData(thumbnail);
    }

    /// <summary>Moves a package resource and updates document references.</summary>
    public OdfEditResult MovePackageResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath)
    {
        return DrawingResourceManager.MovePackageResource(
            document,
            sourcePath,
            destinationPath);
    }

    /// <summary>Finds currently unreferenced drawing package resources.</summary>
    public string[] GetOrphanedResourcePaths(OdfDocument document)
    {
        string[] paths = DrawingResourceManager.FindOrphanedResources(
            document);
        document.OrphanedResourcePaths = (string[])paths.Clone();
        return paths;
    }

    /// <summary>Explicitly removes currently orphaned drawing package resources.</summary>
    public OdfEditResult CollectDrawingGarbage(OdfDocument document)
    {
        return DrawingResourceManager.CollectGarbage(document);
    }

    static OdfImage FindImageByNode(OdfDocument document, OdfNode node)
    {
        int index = 0;
        while (index < document.Images.Length)
        {
            if (document.Images[index].Node == node)
            {
                return document.Images[index];
            }
            index = index + 1;
        }
        throw new OdfEditException("The drawing image could not be refreshed.");
    }

    static OdfMediaResource FindMediaByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.MediaResources.Length)
        {
            if (document.MediaResources[index].Node == node)
            {
                return document.MediaResources[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The media resource could not be refreshed.");
    }

    static OdfEmbeddedObject FindEmbeddedByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            if (document.EmbeddedObjects[index].Node == node)
            {
                return document.EmbeddedObjects[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The embedded object could not be refreshed.");
    }

    static OdfEmbeddedObject FindEmbeddedByPath(
        OdfDocument document,
        string path)
    {
        int index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            if (document.EmbeddedObjects[index].PackagePath == path)
            {
                return document.EmbeddedObjects[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The embedded package object could not be refreshed.");
    }

    static void RequireImageNode(OdfDocument document, OdfImage image)
    {
        RequireDocument(document);
        if (image == null || image.Node == null
            || FindOptionalImage(document, image.Node) != image)
        {
            throw new OdfEditException(
                "A drawing image belonging to the document is required.");
        }
    }

    static OdfImage? FindOptionalImage(OdfDocument document, OdfNode node)
    {
        int index = 0;
        while (index < document.Images.Length)
        {
            if (document.Images[index].Node == node)
            {
                return document.Images[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void RequireMediaNode(
        OdfDocument document,
        OdfMediaResource resource)
    {
        RequireDocument(document);
        if (resource == null || resource.Node == null)
        {
            throw new OdfEditException(
                "A materialized media resource is required.");
        }
        int index = 0;
        while (index < document.MediaResources.Length)
        {
            if (document.MediaResources[index] == resource)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The media resource does not belong to the document.");
    }

    static void RequireEmbeddedObject(
        OdfDocument document,
        OdfEmbeddedObject embeddedObject)
    {
        RequireDocument(document);
        int index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            if (document.EmbeddedObjects[index] == embeddedObject)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The embedded object does not belong to the document.");
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for drawing editing.");
        }
    }

    static byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        Array.Copy(data, result, data.Length);
        return result;
    }
}
