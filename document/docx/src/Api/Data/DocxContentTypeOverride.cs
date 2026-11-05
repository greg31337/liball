using System.Text;
using System.Xml;

namespace docx;

/// <summary>
/// Represents content type override data used by a DOCX document.
/// </summary>
public class DocxContentTypeOverride
{
    /// <summary>
    /// The normalized absolute package part name covered by this override.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The content type value stored in the document model.
    /// </summary>
    public string ContentType;

    /// <summary>
    /// Creates a content type override instance.
    /// </summary>
    /// <param name="partName">The absolute package part name.</param>
    /// <param name="contentType">The MIME content type for the part or binary data.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxContentTypeOverride(string partName, string contentType)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        this.PartName = partName;
        this.ContentType = contentType;
    }
}
