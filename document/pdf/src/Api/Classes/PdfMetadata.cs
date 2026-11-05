using System;

namespace pdf;

/// <summary>
/// Represents the metadata used by the PDF generation API.
/// </summary>
public class PdfMetadata : IPdfMetadata
{
    /// <summary>
    /// Stores the optional document title, or null when no title is set.
    /// </summary>
    internal string? Title;
    /// <summary>
    /// Stores the optional document author, or null when no author is set.
    /// </summary>
    internal string? Author;
    /// <summary>
    /// Stores the optional document subject, or null when no subject is set.
    /// </summary>
    internal string? Subject;
    /// <summary>
    /// Stores the optional document keywords, or null when no keywords are set.
    /// </summary>
    internal string? Keywords;
    /// <summary>
    /// Stores the optional document creator, or null when no creator is set.
    /// </summary>
    internal string? Creator;
    /// <summary>
    /// Stores the optional document producer, or null when no producer is set.
    /// </summary>
    internal string? Producer;
    /// <summary>
    /// Indicates whether a creation date has been set.
    /// </summary>
    internal bool HasCreationDateValue;
    /// <summary>
    /// Stores the document creation date when one has been set.
    /// </summary>
    internal DateTime CreationDate;
    /// <summary>
    /// Indicates whether a modification date has been set.
    /// </summary>
    internal bool HasModificationDateValue;
    /// <summary>
    /// Stores the document modification date when one has been set.
    /// </summary>
    internal DateTime ModificationDate;
    /// <summary>
    /// Stores the optional XMP packet, or null when no packet is set.
    /// </summary>
    internal string? XmpMetadata;

    /// <summary>
    /// Initializes a new metadata.
    /// </summary>
    public PdfMetadata()
    {
        Title = null;
        Author = null;
        Subject = null;
        Keywords = null;
        Creator = null;
        Producer = null;
        HasCreationDateValue = false;
        CreationDate = default(DateTime);
        HasModificationDateValue = false;
        ModificationDate = default(DateTime);
        XmpMetadata = null;
    }

    /// <summary>
    /// Sets the document title.
    /// </summary>
    /// <param name="title">The title stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when title is null.</exception>
    public void SetTitle(string title)
    {
        ValidateText(title, "title");
        Title = title;
    }

    /// <summary>
    /// Sets the document author.
    /// </summary>
    /// <param name="author">The author stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when author is null.</exception>
    public void SetAuthor(string author)
    {
        ValidateText(author, "author");
        Author = author;
    }

    /// <summary>
    /// Sets the document subject.
    /// </summary>
    /// <param name="subject">The subject stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when subject is null.</exception>
    public void SetSubject(string subject)
    {
        ValidateText(subject, "subject");
        Subject = subject;
    }

    /// <summary>
    /// Sets the document keywords.
    /// </summary>
    /// <param name="keywords">The keywords stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when keywords is null.</exception>
    public void SetKeywords(string keywords)
    {
        ValidateText(keywords, "keywords");
        Keywords = keywords;
    }

    /// <summary>
    /// Sets the application or component that created the source document.
    /// </summary>
    /// <param name="creator">The creator stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when creator is null.</exception>
    public void SetCreator(string creator)
    {
        ValidateText(creator, "creator");
        Creator = creator;
    }

    /// <summary>
    /// Sets the application or component that produced the PDF.
    /// </summary>
    /// <param name="producer">The producer stored in the document metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when producer is null.</exception>
    public void SetProducer(string producer)
    {
        ValidateText(producer, "producer");
        Producer = producer;
    }

    /// <summary>
    /// Sets the document creation date.
    /// </summary>
    /// <param name="creationDate">The creation date stored in the document metadata.</param>
    public void SetCreationDate(DateTime creationDate)
    {
        HasCreationDateValue = true;
        CreationDate = creationDate;
    }

    /// <summary>
    /// Sets the document modification date.
    /// </summary>
    /// <param name="modificationDate">The modification date stored in the document metadata.</param>
    public void SetModificationDate(DateTime modificationDate)
    {
        HasModificationDateValue = true;
        ModificationDate = modificationDate;
    }

    /// <summary>
    /// Sets the complete XMP packet written to the document metadata stream as UTF-8.
    /// </summary>
    /// <param name="xmpMetadata">The XMP XML packet, or an empty string to omit the metadata stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when XMP metadata is null.</exception>
    public void SetXmpMetadata(string xmpMetadata)
    {
        ValidateText(xmpMetadata, "xmpMetadata");
        XmpMetadata = xmpMetadata;
    }

    /// <summary>
    /// Determines whether the current object has values.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasValues()
    {
        if (!string.IsNullOrEmpty(Title))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Author))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Subject))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Keywords))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Creator))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Producer))
        {
            return true;
        }

        if (HasCreationDateValue)
        {
            return true;
        }

        if (HasModificationDateValue)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(XmpMetadata))
        {
            return true;
        }

        return false;
    }

    internal bool HasInfoValues()
    {
        if (!string.IsNullOrEmpty(Title))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Author))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Subject))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Keywords))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Creator))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(Producer))
        {
            return true;
        }

        return HasCreationDateValue || HasModificationDateValue;
    }

    internal bool HasXmpMetadata()
    {
        return !string.IsNullOrEmpty(XmpMetadata);
    }

    internal static void ValidateText(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
