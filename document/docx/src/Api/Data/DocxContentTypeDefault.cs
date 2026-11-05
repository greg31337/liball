using System.Text;
using System.Xml;

namespace docx;

/// <summary>
/// Represents content type default data used by a DOCX document.
/// </summary>
public class DocxContentTypeDefault
{
    /// <summary>
    /// The extension value stored in the document model.
    /// </summary>
    public string Extension;
    /// <summary>
    /// The content type value stored in the document model.
    /// </summary>
    public string ContentType;

    /// <summary>
    /// Creates a content type default instance.
    /// </summary>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="contentType">The MIME content type for the part or binary data.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxContentTypeDefault(string extension, string contentType)
    {
        if (extension == null)
        {
            throw new ArgumentNullException("extension");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        this.Extension = extension;
        this.ContentType = contentType;
    }
}
