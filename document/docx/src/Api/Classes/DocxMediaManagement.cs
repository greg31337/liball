using System.IO;

namespace docx;

/// <summary>
/// Discovers, inserts, relates, replaces, and renders document images and drawing content.
/// </summary>
public class DocxMediaManagement : IDocxMediaManagement
{
    /// <summary>
    /// Creates a media management service.
    /// </summary>
    public DocxMediaManagement()
    {
    }

    /// <summary>
    /// Returns all image relationships discovered in the document package.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the images.</returns>
    public List<DocxImage> GetImages(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxImage>(document.Media.Images);
    }

    /// <summary>
    /// Finds an image by source part and relationship identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <returns>The matching image, or null.</returns>
    public DocxImage? FindImage(DocxDocument document, string sourcePartName, string relationshipId)
    {
        ValidateDocument(document);
        return document.FindImage(sourcePartName, relationshipId);
    }

    /// <summary>
    /// Returns a copy of an image's bytes.
    /// </summary>
    /// <param name="image">The image to inspect.</param>
    /// <returns>A copy of the image bytes.</returns>
    public byte[] GetImageBytes(DocxImage image)
    {
        ValidateImage(image);
        return image.GetBytes();
    }

    /// <summary>
    /// Returns an image's byte count.
    /// </summary>
    /// <param name="image">The image to inspect.</param>
    /// <returns>The byte count.</returns>
    public int GetImageByteCount(DocxImage image)
    {
        ValidateImage(image);
        return image.ByteCount();
    }

    /// <summary>
    /// Detects a supported image content type from signature bytes.
    /// </summary>
    /// <param name="bytes">The image bytes.</param>
    /// <returns>The detected content type, or an empty string.</returns>
    public string DetectImageContentType(byte[] bytes)
    {
        return DocxImage.DetectContentType(bytes);
    }

    /// <summary>
    /// Detects a supported image content type from a stream.
    /// </summary>
    /// <param name="stream">The image stream.</param>
    /// <returns>The detected content type, or an empty string.</returns>
    public string DetectImageContentTypeFromStream(Stream stream)
    {
        return DetectImageContentType(DocxMedia.ReadAllBytes(stream));
    }

    /// <summary>
    /// Adds an image and relates it from the main document part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageToMainDocument(DocxDocument document, byte[] bytes, string contentType)
    {
        ValidateDocument(document);
        return document.AddImageToMainDocument(bytes, contentType);
    }

    /// <summary>
    /// Adds an image and relates it from a specified package part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageToPart(DocxDocument document, string sourcePartName, byte[] bytes, string contentType)
    {
        ValidateDocument(document);
        return document.AddImageWithRelationship(bytes, contentType, sourcePartName);
    }

    /// <summary>
    /// Adds an image and relates it from a header part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="header">The relationship source header.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageToHeader(DocxDocument document, DocxHeader header, byte[] bytes, string contentType)
    {
        ValidateDocument(document);
        ValidateHeader(header);
        ValidateHeaderPart(document, header);
        return AddImageToPart(document, header.PartName, bytes, contentType);
    }

    /// <summary>
    /// Adds an image and relates it from a footer part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footer">The relationship source footer.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageToFooter(DocxDocument document, DocxFooter footer, byte[] bytes, string contentType)
    {
        ValidateDocument(document);
        ValidateFooter(footer);
        ValidateFooterPart(document, footer);
        return AddImageToPart(document, footer.PartName, bytes, contentType);
    }

    /// <summary>
    /// Adds an image stream and relates it from the main document part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="stream">The image stream.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageStreamToMainDocument(DocxDocument document, Stream stream, string contentType)
    {
        ValidateDocument(document);
        return AddImageToMainDocument(document, DocxMedia.ReadAllBytes(stream), contentType);
    }

    /// <summary>
    /// Adds an image stream and relates it from a specified package part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The relationship source part name.</param>
    /// <param name="stream">The image stream.</param>
    /// <param name="contentType">The content type, or empty to detect it.</param>
    /// <returns>The added image relationship model.</returns>
    public DocxImage AddImageStreamToPart(DocxDocument document, string sourcePartName, Stream stream, string contentType)
    {
        ValidateDocument(document);
        return AddImageToPart(document, sourcePartName, DocxMedia.ReadAllBytes(stream), contentType);
    }

    /// <summary>
    /// Creates or reuses a relationship from another source part to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="sourcePartName">The target relationship source part.</param>
    /// <returns>An image model for the source-part relationship.</returns>
    public DocxImage RelateImageToPart(DocxDocument document, DocxImage image, string sourcePartName)
    {
        ValidateDocument(document);
        ValidateImage(image);
        ValidateDocumentImage(document, image);

        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (!document.Package.ContainsPart(image.PartName))
        {
            throw new InvalidOperationException("Image part is missing from the package.");
        }

        DocxRelationshipSet relationshipSet = document.Media.GetOrCreateRelationshipSet(sourcePartName);
        DocxRelationship? relationship = document.Media.FindImageRelationship(relationshipSet, image);

        if (relationship == null)
        {
            string target = DocxMedia.GetRelationshipTarget(relationshipSet.SourcePartName, image.PartName);
            relationship = relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeImage, target, false);
            document.Media.WriteRelationshipSetPart(relationshipSet);
        }

        DocxImage? relatedImage = document.Media.FindImage(relationshipSet.SourcePartName, relationship.Id);

        if (relatedImage != null)
        {
            return relatedImage;
        }

        relatedImage = DocxImage.Create(
            relationshipSet.SourcePartName,
            image.PartName,
            relationship.Id,
            image.ContentType,
            image.GetBytes());
        document.Media.Images.Add(relatedImage);
        return relatedImage;
    }

    /// <summary>
    /// Creates or reuses a relationship from a header to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="header">The target relationship source header.</param>
    /// <returns>An image model for the header relationship.</returns>
    public DocxImage RelateImageToHeader(DocxDocument document, DocxImage image, DocxHeader header)
    {
        ValidateDocument(document);
        ValidateImage(image);
        ValidateHeader(header);
        ValidateHeaderPart(document, header);
        return RelateImageToPart(document, image, header.PartName);
    }

    /// <summary>
    /// Creates or reuses a relationship from a footer to an existing image part.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The existing image part.</param>
    /// <param name="footer">The target relationship source footer.</param>
    /// <returns>An image model for the footer relationship.</returns>
    public DocxImage RelateImageToFooter(DocxDocument document, DocxImage image, DocxFooter footer)
    {
        ValidateDocument(document);
        ValidateImage(image);
        ValidateFooter(footer);
        ValidateFooterPart(document, footer);
        return RelateImageToPart(document, image, footer.PartName);
    }

    /// <summary>
    /// Replaces image bytes and content type in the package.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The image to replace.</param>
    /// <param name="bytes">The replacement bytes.</param>
    /// <param name="contentType">The replacement content type, or empty to detect it.</param>
    /// <returns>True when the image part existed and was replaced.</returns>
    public bool ReplaceImage(DocxDocument document, DocxImage image, byte[] bytes, string contentType)
    {
        ValidateDocument(document);
        ValidateImage(image);
        ValidateDocumentImage(document, image);
        bool replaced = document.ReplaceImage(image, bytes, contentType);

        if (!replaced)
        {
            return false;
        }

        foreach (DocxImage relatedImage in document.Media.Images)
        {
            if (relatedImage.PartName == image.PartName)
            {
                relatedImage.ContentType = image.ContentType;
                relatedImage.ReplaceBytes(bytes);
            }
        }

        return true;
    }

    /// <summary>
    /// Replaces image content from a stream.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="image">The image to replace.</param>
    /// <param name="stream">The replacement stream.</param>
    /// <param name="contentType">The replacement content type, or empty to detect it.</param>
    /// <returns>True when the image part existed and was replaced.</returns>
    public bool ReplaceImageFromStream(DocxDocument document, DocxImage image, Stream stream, string contentType)
    {
        ValidateDocument(document);
        ValidateImage(image);
        return ReplaceImage(document, image, DocxMedia.ReadAllBytes(stream), contentType);
    }

    /// <summary>
    /// Converts a pixel count at a specified dots-per-inch value to English Metric Units.
    /// </summary>
    /// <param name="pixels">The pixel count.</param>
    /// <param name="dotsPerInch">The dots-per-inch value.</param>
    /// <returns>The corresponding EMU count.</returns>
    public long PixelsToEmu(int pixels, int dotsPerInch)
    {
        if (pixels < 0)
        {
            throw new ArgumentOutOfRangeException("pixels");
        }

        if (dotsPerInch <= 0)
        {
            throw new ArgumentOutOfRangeException("dotsPerInch");
        }

        return ((long)pixels * 914400L) / dotsPerInch;
    }

    /// <summary>
    /// Creates an inline image drawing with explicit dimensions and alternative text.
    /// </summary>
    /// <param name="image">The related image.</param>
    /// <param name="name">The non-visual image name.</param>
    /// <param name="alternativeText">The accessibility description.</param>
    /// <param name="widthEmu">The width in EMUs.</param>
    /// <param name="heightEmu">The height in EMUs.</param>
    /// <returns>The created drawing.</returns>
    public DocxDrawing CreateInlineDrawing(DocxImage image, string name, string alternativeText, long widthEmu, long heightEmu)
    {
        ValidateImage(image);
        return DocxDrawing.CreateInlineImage(image.RelationshipId, name, alternativeText, widthEmu, heightEmu);
    }

    /// <summary>
    /// Appends an inline or preserved drawing run to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="drawing">The drawing to append.</param>
    public void AppendDrawingToParagraph(DocxParagraph paragraph, DocxDrawing drawing)
    {
        ValidateParagraph(paragraph);
        ValidateDrawing(drawing);
        DocxRun run;

        if (drawing.GetXml() == "")
        {
            run = new DocxRun(drawing.WriteXml());
        }
        else
        {
            run = new DocxRun("");
            run.InsertDrawing(0, drawing);
        }

        paragraph.InsertRun(paragraph.ChildCount(), run);
    }

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
    public DocxDrawing AddInlineImageToParagraph(DocxDocument document, DocxParagraph paragraph, byte[] bytes, string contentType, string name, string alternativeText, long widthEmu, long heightEmu)
    {
        ValidateDocument(document);
        ValidateParagraph(paragraph);
        DocxImage image = AddImageToMainDocument(document, bytes, contentType);
        DocxDrawing drawing = CreateInlineDrawing(image, name, alternativeText, widthEmu, heightEmu);
        AppendDrawingToParagraph(paragraph, drawing);
        return drawing;
    }

    /// <summary>
    /// Returns serialized drawing markup, preserving unsupported drawing or object forms.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>The drawing markup.</returns>
    public string GetDrawingXml(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.WriteXml();
    }

    /// <summary>
    /// Returns whether a drawing contains inline DrawingML.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when inline DrawingML is present.</returns>
    public bool IsInlineDrawing(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.IsInline();
    }

    /// <summary>
    /// Returns whether a drawing contains floating DrawingML.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when floating DrawingML is present.</returns>
    public bool IsFloatingDrawing(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.IsFloating();
    }

    /// <summary>
    /// Returns whether a drawing contains chart markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when chart markup is present.</returns>
    public bool DrawingContainsChart(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.ContainsChart();
    }

    /// <summary>
    /// Returns whether a drawing contains diagram markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when diagram markup is present.</returns>
    public bool DrawingContainsDiagram(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.ContainsDiagram();
    }

    /// <summary>
    /// Returns whether a drawing contains VML markup.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when VML markup is present.</returns>
    public bool DrawingContainsVml(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.ContainsVml();
    }

    /// <summary>
    /// Returns whether a drawing contains an embedded or OLE object.
    /// </summary>
    /// <param name="drawing">The drawing to inspect.</param>
    /// <returns>True when embedded-object markup is present.</returns>
    public bool DrawingContainsEmbeddedObject(DocxDrawing drawing)
    {
        ValidateDrawing(drawing);
        return drawing.ContainsEmbeddedObject();
    }

    internal static void ValidateHeaderPart(DocxDocument document, DocxHeader header)
    {
        bool found = false;

        foreach (DocxHeader candidate in document.Headers)
        {
            if (candidate == header)
            {
                found = true;
            }
        }

        if (!found || header.PartName == "" || !document.Package.ContainsPart(header.PartName))
        {
            throw new ArgumentException("The header does not belong to the document.", "header");
        }
    }

    internal static void ValidateDocumentImage(DocxDocument document, DocxImage image)
    {
        foreach (DocxImage candidate in document.Media.Images)
        {
            if (candidate == image)
            {
                return;
            }
        }

        throw new ArgumentException("The image does not belong to the document.", "image");
    }

    internal static void ValidateFooterPart(DocxDocument document, DocxFooter footer)
    {
        bool found = false;

        foreach (DocxFooter candidate in document.Footers)
        {
            if (candidate == footer)
            {
                found = true;
            }
        }

        if (!found || footer.PartName == "" || !document.Package.ContainsPart(footer.PartName))
        {
            throw new ArgumentException("The footer does not belong to the document.", "footer");
        }
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateImage(DocxImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }
    }

    internal static void ValidateHeader(DocxHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }
    }

    internal static void ValidateFooter(DocxFooter footer)
    {
        if (footer == null)
        {
            throw new ArgumentNullException("footer");
        }
    }

    internal static void ValidateParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }
    }

    internal static void ValidateDrawing(DocxDrawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException("drawing");
        }
    }
}
