namespace odf;

/// <summary>
/// Provides typed creation, editing, resource management, and extraction for drawings.
/// </summary>
public interface IOdfDrawingEditor
{
    /// <summary>Gets all drawing objects in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of drawing objects.</returns>
    public OdfDrawingObject[] GetDrawingObjects(OdfDocument document);
    /// <summary>Finds an unambiguously named drawing object.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The drawing name.</param>
    /// <returns>The unique drawing, or <c>null</c>.</returns>
    public OdfDrawingObject? FindDrawingObject(
        OdfDocument document,
        string name);
    /// <summary>Creates a drawing object beneath a parent or document body.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="parent">The parent drawing, or <c>null</c> for the body.</param>
    /// <param name="kind">The drawing kind.</param>
    /// <param name="name">The optional drawing name.</param>
    /// <returns>The created drawing object.</returns>
    public OdfDrawingObject CreateDrawingObject(
        OdfDocument document,
        OdfDrawingObject? parent,
        OdfDrawingKind kind,
        string name);
    /// <summary>Removes a drawing subtree without collecting its package resources.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="drawing">The drawing subtree root.</param>
    public void RemoveDrawingObject(
        OdfDocument document,
        OdfDrawingObject drawing);
    /// <summary>Copies a drawing subtree within one document.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="drawing">The drawing subtree root.</param>
    /// <param name="targetParent">The destination parent.</param>
    /// <returns>The copied drawing object.</returns>
    public OdfDrawingObject CopyDrawingObject(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent);
    /// <summary>Copies a drawing and its style and package dependencies across documents.</summary>
    /// <param name="sourceDocument">The source document.</param>
    /// <param name="targetDocument">The target document.</param>
    /// <param name="drawing">The source drawing.</param>
    /// <param name="targetParent">The destination parent.</param>
    /// <returns>The imported drawing object.</returns>
    public OdfDrawingObject ImportDrawingObject(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfDrawingObject drawing,
        OdfDrawingObject targetParent);
    /// <summary>Renames a drawing identifier and updates drawing references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="drawing">The drawing to rename.</param>
    /// <param name="newIdentifier">The new identifier.</param>
    /// <returns>The refreshed drawing object.</returns>
    public OdfDrawingObject RenameDrawingIdentifier(
        OdfDocument document,
        OdfDrawingObject drawing,
        string newIdentifier);
    /// <summary>Applies changes made to drawing, geometry, and resource data fields.</summary>
    /// <param name="document">The document to update.</param>
    public void ApplyDrawingChanges(OdfDocument document);
    /// <summary>Gets all drawing image descriptions.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of images.</returns>
    public OdfImage[] GetImages(OdfDocument document);
    /// <summary>Adds a package image and a drawing image beneath a parent.</summary>
    /// <param name="document">The package document.</param>
    /// <param name="parent">The destination drawing parent.</param>
    /// <param name="path">The requested package path.</param>
    /// <param name="data">The image bytes.</param>
    /// <param name="mediaType">The declared media type.</param>
    /// <param name="name">The drawing image name.</param>
    /// <param name="deduplicate">Whether identical package bytes may be reused.</param>
    /// <returns>The created drawing image description.</returns>
    public OdfImage AddPackageImage(
        OdfDocument document,
        OdfDrawingObject parent,
        string path,
        byte[] data,
        string mediaType,
        string name,
        bool deduplicate);
    /// <summary>Replaces locally stored image bytes.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="image">The image to modify.</param>
    /// <param name="data">The replacement bytes.</param>
    /// <param name="mediaType">The declared media type.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplaceImageData(
        OdfDocument document,
        OdfImage image,
        byte[] data,
        string mediaType);
    /// <summary>Changes an image to an external reference without fetching it.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="image">The image to modify.</param>
    /// <param name="href">The absolute external IRI.</param>
    /// <param name="mediaType">The declared media type.</param>
    /// <returns>The refreshed image description.</returns>
    public OdfImage SetExternalImage(
        OdfDocument document,
        OdfImage image,
        string href,
        string mediaType);
    /// <summary>Changes an image to inline base64 content.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="image">The image to modify.</param>
    /// <param name="data">The image bytes.</param>
    /// <param name="mediaType">The declared media type.</param>
    /// <returns>The refreshed image description.</returns>
    public OdfImage SetInlineImage(
        OdfDocument document,
        OdfImage image,
        byte[] data,
        string mediaType);
    /// <summary>Extracts locally available image bytes.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="image">The image to extract.</param>
    /// <returns>An independent byte array.</returns>
    public byte[] ExtractImageData(
        OdfDocument document,
        OdfImage image);
    /// <summary>Gets passive media and active-content descriptions.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of resource descriptions.</returns>
    public OdfMediaResource[] GetMediaResources(OdfDocument document);
    /// <summary>Changes a media resource reference without loading its content.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="resource">The resource to modify.</param>
    /// <param name="href">The package-relative or external IRI.</param>
    /// <param name="mediaType">The declared media type.</param>
    /// <returns>The refreshed resource description.</returns>
    public OdfMediaResource SetMediaReference(
        OdfDocument document,
        OdfMediaResource resource,
        string href,
        string mediaType);
    /// <summary>Gets embedded objects.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of embedded objects.</returns>
    public OdfEmbeddedObject[] GetEmbeddedObjects(OdfDocument document);
    /// <summary>Replaces a single-entry package or inline embedded object.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="embeddedObject">The object to modify.</param>
    /// <param name="data">The replacement bytes.</param>
    /// <returns>The refreshed embedded object.</returns>
    public OdfEmbeddedObject ReplaceEmbeddedObjectData(
        OdfDocument document,
        OdfEmbeddedObject embeddedObject,
        byte[] data);
    /// <summary>Extracts locally available single-entry embedded-object bytes.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="embeddedObject">The object to extract.</param>
    /// <returns>An independent byte array.</returns>
    public byte[] ExtractEmbeddedObjectData(
        OdfDocument document,
        OdfEmbeddedObject embeddedObject);
    /// <summary>Gets chart descriptions.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of charts.</returns>
    public OdfChart[] GetCharts(OdfDocument document);
    /// <summary>Gets package thumbnails.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of thumbnails.</returns>
    public OdfThumbnail[] GetThumbnails(OdfDocument document);
    /// <summary>Extracts validated thumbnail bytes.</summary>
    /// <param name="thumbnail">The thumbnail to extract.</param>
    /// <returns>An independent byte array.</returns>
    public byte[] ExtractThumbnailData(OdfThumbnail thumbnail);
    /// <summary>Moves a package resource and updates document references.</summary>
    /// <param name="document">The package document.</param>
    /// <param name="sourcePath">The current package path.</param>
    /// <param name="destinationPath">The new package path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult MovePackageResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath);
    /// <summary>Finds currently unreferenced drawing package resources.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The orphaned canonical package paths.</returns>
    public string[] GetOrphanedResourcePaths(OdfDocument document);
    /// <summary>Explicitly removes currently orphaned drawing package resources.</summary>
    /// <param name="document">The package document.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult CollectDrawingGarbage(OdfDocument document);
}
