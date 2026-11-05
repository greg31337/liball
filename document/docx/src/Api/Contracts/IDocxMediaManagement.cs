using System.IO;

namespace docx;

/// <summary>
/// Discovers, inserts, relates, replaces, and renders document images and drawing content.
/// </summary>
public interface IDocxMediaManagement
{
    /// <summary>
    /// Returns all image relationships discovered in the document package.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the images.</returns>
    List<DocxImage> GetImages(DocxDocument document);

    /// <summary>
    /// Finds an image by source part and relationship identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <returns>The matching image, or null.</returns>
    DocxImage? FindImage(DocxDocument document, string sourcePartName, string relationshipId);

    /// <summary>
    /// Returns a copy of an image's bytes.
    /// </summary>
    /// <param name="image">The image to inspect.</param>
    /// <returns>A copy of the image bytes.</returns>
    byte[] GetImageBytes(DocxImage image);

    /// <summary>
    /// Returns an image's byte count.
    /// </summary>
    /// <param name="image">The image to inspect.</param>
    /// <returns>The byte count.</returns>
    int GetImageByteCount(DocxImage image);

    /// <summary>
    /// Detects a supported image content type from signature bytes.
    /// </summary>
    /// <param name="bytes">The image bytes.</param>
    /// <returns>The detected content type, or an empty string.</returns>
    string DetectImageContentType(byte[] bytes);

    /// <summary>
    /// Detects a supported image content type from a stream.
    /// </summary>
    /// <param name="stream">The image stream.</param>
    /// <returns>The detected content type, or an empty string.</returns>
    string DetectImageContentTypeFromStream(Stream stream);

    /// <summary>
    /// Adds an image and relates it from the main document part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageToMainDocument(DocxDocument document, byte[] bytes, string contentType);

    /// <summary>
    /// Adds an image and relates it from a specified package part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageToPart(DocxDocument document, string sourcePartName, byte[] bytes, string contentType);

    /// <summary>
    /// Adds an image and relates it from a header part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="header">The relationship source header.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageToHeader(DocxDocument document, DocxHeader header, byte[] bytes, string contentType);

    /// <summary>
    /// Adds an image and relates it from a footer part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footer">The relationship source footer.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageToFooter(DocxDocument document, DocxFooter footer, byte[] bytes, string contentType);

    /// <summary>
    /// Adds an image stream and relates it from the main document part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="stream">The image stream.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageStreamToMainDocument(DocxDocument document, Stream stream, string contentType);

    /// <summary>
    /// Adds an image stream and relates it from a specified package part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="stream">The image stream.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    DocxImage AddImageStreamToPart(DocxDocument document, string sourcePartName, Stream stream, string contentType);

    /// <summary>
    /// Creates or reuses a relationship from another source part to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="sourcePartName">The target relationship source part.</param>
    /// <returns>An image model for the source-part relationship.</returns>
    DocxImage RelateImageToPart(DocxDocument document, DocxImage image, string sourcePartName);

    /// <summary>
    /// Creates or reuses a relationship from a header to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="header">The target relationship source header.</param>
    /// <returns>An image model for the header relationship.</returns>
    DocxImage RelateImageToHeader(DocxDocument document, DocxImage image, DocxHeader header);

    /// <summary>
    /// Creates or reuses a relationship from a footer to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="footer">The target relationship source footer.</param>
    /// <returns>An image model for the footer relationship.</returns>
    DocxImage RelateImageToFooter(DocxDocument document, DocxImage image, DocxFooter footer);

    /// <summary>
    /// Replaces image bytes and content type in the package.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The image to replace.</param>
    /// <param name="bytes">The replacement bytes.</param>
    /// <param name="contentType">The replacement content type, or empty to detect it.</param>
    /// <returns>True when the image part existed and was replaced.</returns>
    bool ReplaceImage(DocxDocument document, DocxImage image, byte[] bytes, string contentType);

    /// <summary>
    /// Replaces image content from a stream.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The image to replace.</param>
    /// <param name="stream">The replacement stream.</param>
    /// <param name="contentType">The replacement content type, or empty to detect it.</param>
    /// <returns>True when the image part existed and was replaced.</returns>
    bool ReplaceImageFromStream(DocxDocument document, DocxImage image, Stream stream, string contentType);

    /// <summary>
    /// Converts a pixel count at a specified dots-per-inch value to English Metric Units.
    /// </summary>
    /// <param name="pixels">The pixel count.</param>
    /// <param name="dotsPerInch">The dots-per-inch value.</param>
    /// <returns>The corresponding EMU count.</returns>
    long PixelsToEmu(int pixels, int dotsPerInch);

    /// <summary>
    /// Creates an inline image drawing with explicit dimensions and alternative text.
    /// </summary>
    /// <param name="image">The related image.</param>
    /// <param name="name">The non-visual image name.</param>
    /// <param name="alternativeText">The accessibility description.</param>
    /// <param name="widthEmu">The width in EMUs.</param>
    /// <param name="heightEmu">The height in EMUs.</param>
    /// <returns>The created drawing.</returns>
    DocxDrawing CreateInlineDrawing(DocxImage image, string name, string alternativeText, long widthEmu, long heightEmu);

    /// <summary>
    /// Appends an inline or preserved drawing run to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="drawing">The drawing to append.</param>
    void AppendDrawingToParagraph(DocxParagraph paragraph, DocxDrawing drawing);

    /// <summary>
    /// Adds, relates, and appends an inline image to a main-document paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <param name="name">The non-visual image name.</param>
    /// <param name="alternativeText">The accessibility description.</param>
    /// <param name="widthEmu">The width in EMUs.</param>
    /// <param name="heightEmu">The height in EMUs.</param>
    /// <returns>The created drawing.</returns>
    DocxDrawing AddInlineImageToParagraph(DocxDocument document, DocxParagraph paragraph, byte[] bytes, string contentType, string name, string alternativeText, long widthEmu, long heightEmu);

    /// <summary>
    /// Returns serialized drawing markup, preserving unsupported drawing or object forms.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>The drawing markup.</returns>
    string GetDrawingXml(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains inline DrawingML.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when inline DrawingML is present.</returns>
    bool IsInlineDrawing(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains floating DrawingML.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when floating DrawingML is present.</returns>
    bool IsFloatingDrawing(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains chart markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when chart markup is present.</returns>
    bool DrawingContainsChart(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains diagram markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when diagram markup is present.</returns>
    bool DrawingContainsDiagram(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains VML markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when VML markup is present.</returns>
    bool DrawingContainsVml(DocxDrawing drawing);

    /// <summary>
    /// Returns whether a drawing contains an embedded or OLE object.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when embedded-object markup is present.</returns>
    bool DrawingContainsEmbeddedObject(DocxDrawing drawing);
}
