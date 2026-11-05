namespace docx;

/// <summary>
/// Describes one tracked revision and its review metadata.
/// </summary>
public class DocxRevisionInfo
{
    /// <summary>
    /// The semantic revision kind.
    /// </summary>
    public DocxRevisionKind Kind;
    /// <summary>
    /// The WordprocessingML revision element local name.
    /// </summary>
    public string LocalName;
    /// <summary>
    /// The revision identifier, or an empty string.
    /// </summary>
    public string Id;
    /// <summary>
    /// The review author, or an empty string.
    /// </summary>
    public string Author;
    /// <summary>
    /// The review timestamp text, or an empty string.
    /// </summary>
    public string Date;
    /// <summary>
    /// The normalized package part name, or an empty string for detached content.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The revision content extracted with inserted and deleted text included.
    /// </summary>
    public string Text;
    /// <summary>
    /// The preserved revision XML.
    /// </summary>
    public string Xml;

    /// <summary>
    /// Creates a tracked revision descriptor.
    /// </summary>
    /// <param name="kind">The semantic revision kind.</param>
    /// <param name="localName">The WordprocessingML local name.</param>
    /// <param name="id">The revision identifier.</param>
    /// <param name="author">The review author.</param>
    /// <param name="date">The review timestamp text.</param>
    /// <param name="partName">The normalized package part name.</param>
    /// <param name="text">The extracted revision content.</param>
    /// <param name="xml">The preserved revision XML.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required string is null.</exception>
    public DocxRevisionInfo(DocxRevisionKind kind, string localName, string id, string author, string date, string partName, string text, string xml)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (date == null)
        {
            throw new ArgumentNullException("date");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Kind = kind;
        this.LocalName = localName;
        this.Id = id;
        this.Author = author;
        this.Date = date;
        this.PartName = partName;
        this.Text = text;
        this.Xml = xml;
    }
}
