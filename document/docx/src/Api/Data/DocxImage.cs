namespace docx;

/// <summary>
/// Represents image bytes and the relationship that addresses them from a document part.
/// </summary>
public class DocxImage
{
    /// <summary>
    /// The absolute package part name for the source.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// The normalized absolute name of the image package part.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The relationship identifier stored in the document.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// The content type value stored in the document model.
    /// </summary>
    public string ContentType;
    internal byte[] Bytes;

    /// <summary>
    /// Creates an image instance.
    /// </summary>
    public DocxImage()
    {
        this.SourcePartName = "";
        this.PartName = "";
        this.RelationshipId = "";
        this.ContentType = "";
        this.Bytes = new byte[0];
    }

    internal static DocxImage Create(string sourcePartName, string partName, string relationshipId, string contentType, byte[] bytes)
    {
        DocxImage image = new DocxImage();
        image.SetPartInfo(sourcePartName, partName, relationshipId, contentType);
        image.SetBytes(bytes);
        return image;
    }

    internal void SetPartInfo(string sourcePartName, string partName, string relationshipId, string contentType)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        this.SourcePartName = NormalizeSourcePartName(sourcePartName);
        this.PartName = DocxPartName.Normalize(partName);
        this.RelationshipId = relationshipId;
        this.ContentType = contentType;
    }

    internal void SetBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        this.Bytes = DocxPart.CopyBytes(bytes);
    }

    internal void ReplaceBytes(byte[] bytes)
    {
        SetBytes(bytes);
    }

    internal byte[] GetBytes()
    {
        return DocxPart.CopyBytes(this.Bytes);
    }

    internal int ByteCount()
    {
        return this.Bytes.Length;
    }

    internal string DetectContentType()
    {
        return DetectContentType(this.Bytes);
    }

    internal void SetDetectedContentType()
    {
        string contentType = DetectContentType(this.Bytes);

        if (contentType != "")
        {
            this.ContentType = contentType;
        }
    }

    internal static string DetectContentType(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (HasPngHeader(bytes))
        {
            return DocxConstants.ContentTypePng;
        }

        if (HasJpegHeader(bytes))
        {
            return DocxConstants.ContentTypeJpeg;
        }

        if (HasGifHeader(bytes))
        {
            return DocxConstants.ContentTypeGif;
        }

        return "";
    }

    internal static bool HasPngHeader(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return bytes.Length >= 8
            && bytes[0] == 137
            && bytes[1] == 80
            && bytes[2] == 78
            && bytes[3] == 71
            && bytes[4] == 13
            && bytes[5] == 10
            && bytes[6] == 26
            && bytes[7] == 10;
    }

    internal static bool HasJpegHeader(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return bytes.Length >= 3
            && bytes[0] == 255
            && bytes[1] == 216
            && bytes[2] == 255;
    }

    internal static bool HasGifHeader(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return bytes.Length >= 6
            && bytes[0] == 71
            && bytes[1] == 73
            && bytes[2] == 70
            && bytes[3] == 56
            && (bytes[4] == 55 || bytes[4] == 57)
            && bytes[5] == 97;
    }

    internal static string NormalizeSourcePartName(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName == "" || sourcePartName == "/")
        {
            return sourcePartName;
        }

        return DocxPartName.Normalize(sourcePartName);
    }
}
