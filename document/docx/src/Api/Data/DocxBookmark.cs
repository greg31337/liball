namespace docx;

/// <summary>
/// Represents bookmark data used by a DOCX document.
/// </summary>
public class DocxBookmark
{
    /// <summary>
    /// The kind start identifier used in DOCX package data.
    /// </summary>
    public const string KindStart = "start";
    /// <summary>
    /// The kind end identifier used in DOCX package data.
    /// </summary>
    public const string KindEnd = "end";

    internal string Xml;
    /// <summary>
    /// The kind value stored in the document model.
    /// </summary>
    public string Kind;
    /// <summary>
    /// The identifier shared by the start and end markers of a bookmark range.
    /// </summary>
    public string Id;
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name;
    /// <summary>
    /// The column first value stored in the document model.
    /// </summary>
    public string ColumnFirst;
    /// <summary>
    /// The column last value stored in the document model.
    /// </summary>
    public string ColumnLast;

    /// <summary>
    /// Creates a bookmark instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <param name="kind">The kind used by the operation.</param>
    /// <param name="id">The id used by the operation.</param>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="columnFirst">The column first used by the operation.</param>
    /// <param name="columnLast">The column last used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty, malformed, or not supported.</exception>
    public DocxBookmark(string xml, string kind, string id, string name, string columnFirst, string columnLast)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (columnFirst == null)
        {
            throw new ArgumentNullException("columnFirst");
        }

        if (columnLast == null)
        {
            throw new ArgumentNullException("columnLast");
        }

        if (kind == "")
        {
            throw new ArgumentException("Kind is required.", "kind");
        }

        this.Xml = xml;
        this.Kind = kind;
        this.Id = id;
        this.Name = name;
        this.ColumnFirst = columnFirst;
        this.ColumnLast = columnLast;
    }

    internal static DocxBookmark Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on a bookmark element.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxBookmark CreateStart(string id, string name)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (id == "")
        {
            throw new ArgumentException("Bookmark id is required.", "id");
        }

        if (name == "")
        {
            throw new ArgumentException("Bookmark name is required.", "name");
        }

        return new DocxBookmark("", KindStart, id, name, "", "");
    }

    internal static DocxBookmark CreateEnd(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            throw new ArgumentException("Bookmark id is required.", "id");
        }

        return new DocxBookmark("", KindEnd, id, "", "", "");
    }

    internal static DocxBookmark ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain a bookmark element.");
        }

        if (reader.IsElement("bookmarkStart", DocxConstants.NamespaceWordprocessingMl))
        {
            string id = ReadRequiredAttribute(reader, "id");
            string name = ReadRequiredAttribute(reader, "name");
            string columnFirst = reader.GetAttribute("colFirst", DocxConstants.NamespaceWordprocessingMl);
            string columnLast = reader.GetAttribute("colLast", DocxConstants.NamespaceWordprocessingMl);
            return new DocxBookmark(xml, KindStart, id, name, columnFirst, columnLast);
        }

        if (reader.IsElement("bookmarkEnd", DocxConstants.NamespaceWordprocessingMl))
        {
            string id = ReadRequiredAttribute(reader, "id");
            return new DocxBookmark(xml, KindEnd, id, "", "", "");
        }

        throw new InvalidOperationException("XML does not contain a bookmark element.");
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("bookmarkStart", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("bookmarkEnd", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        return false;
    }

    internal static string ReadRequiredAttribute(DocxXmlReader reader, string localName)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName == "")
        {
            throw new ArgumentException("Attribute name is required.", "localName");
        }

        string value = reader.GetAttribute(localName, DocxConstants.NamespaceWordprocessingMl);

        if (value == "")
        {
            throw new InvalidDataException("Bookmark is missing required attribute.");
        }

        return value;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool IsStart()
    {
        return this.Kind == KindStart;
    }

    internal bool IsEnd()
    {
        return this.Kind == KindEnd;
    }

    internal void SetId(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        this.Id = id;
    }

    internal void SetName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        this.Name = name;
    }

    internal void SetColumnRange(string columnFirst, string columnLast)
    {
        if (columnFirst == null)
        {
            throw new ArgumentNullException("columnFirst");
        }

        if (columnLast == null)
        {
            throw new ArgumentNullException("columnLast");
        }

        this.ColumnFirst = columnFirst;
        this.ColumnLast = columnLast;
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Kind == KindStart)
        {
            WriteStartTo(writer);
            return;
        }

        if (this.Kind == KindEnd)
        {
            WriteEndTo(writer);
            return;
        }

        throw new InvalidOperationException("Unsupported bookmark kind.");
    }

    internal void WriteStartTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Id == "")
        {
            throw new InvalidOperationException("Bookmark id is required.");
        }

        if (this.Name == "")
        {
            throw new InvalidOperationException("Bookmark name is required.");
        }

        writer.StartElement("w", "bookmarkStart", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "id", DocxConstants.NamespaceWordprocessingMl, this.Id);
        writer.WriteAttribute("w", "name", DocxConstants.NamespaceWordprocessingMl, this.Name);

        if (this.ColumnFirst != "")
        {
            writer.WriteAttribute("w", "colFirst", DocxConstants.NamespaceWordprocessingMl, this.ColumnFirst);
        }

        if (this.ColumnLast != "")
        {
            writer.WriteAttribute("w", "colLast", DocxConstants.NamespaceWordprocessingMl, this.ColumnLast);
        }

        writer.EndElement();
    }

    internal void WriteEndTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Id == "")
        {
            throw new InvalidOperationException("Bookmark id is required.");
        }

        writer.StartElement("w", "bookmarkEnd", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "id", DocxConstants.NamespaceWordprocessingMl, this.Id);
        writer.EndElement();
    }
}
