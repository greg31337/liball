using System;

namespace pdf;

/// <summary>
/// Represents an attachment used by the PDF generation API.
/// </summary>
public class PdfAttachment
{
    /// <summary>
    /// Stores the file name used by this object.
    /// </summary>
    public string FileName;
    /// <summary>
    /// Exposes the data currently stored by this object.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Stores the mime type used by this object.
    /// </summary>
    public string MimeType;
    /// <summary>
    /// Stores the description used by this object.
    /// </summary>
    public string Description;
    /// <summary>
    /// Indicates whether the file is associated with the document catalog for archival output.
    /// </summary>
    public bool IsAssociatedFile;
    /// <summary>
    /// Stores the semantic relationship between an associated file and the PDF.
    /// </summary>
    public PdfAssociatedFileRelationship Relationship;

    /// <summary>
    /// Initializes a new attachment.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <param name="mimeType">The mime type to use for this operation.</param>
    /// <param name="description">The description to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfAttachment(string fileName, byte[] data, string mimeType, string description)
    {
        if (fileName == null)
        {
            throw new ArgumentNullException("fileName");
        }

        if (fileName.Length == 0)
        {
            throw new ArgumentException("Attachment file name cannot be empty.", "fileName");
        }

        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (mimeType == null)
        {
            throw new ArgumentNullException("mimeType");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        FileName = fileName;
        Data = new byte[data.Length];
        Array.Copy(data, Data, data.Length);
        MimeType = mimeType;
        Description = description;
        IsAssociatedFile = false;
        Relationship = PdfAssociatedFileRelationship.Unspecified;
    }
}
