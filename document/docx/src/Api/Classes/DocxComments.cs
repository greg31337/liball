namespace docx;

/// <summary>
/// Represents the comments part and the comments it contains.
/// </summary>
public class DocxComments : IDocxTextContent
{
    internal string Xml;
    internal List<DocxComment> Comments;
    internal List<DocxXmlBuffer> UnknownChildren;

    /// <summary>
    /// Creates a comments instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxComments(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Comments = new List<DocxComment>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadFromXml();
    }

    internal static DocxComments Create()
    {
        return new DocxComments("");
    }

    internal static DocxComments Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("comments", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:comments.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxComments(buffer.GetXml());
    }

    internal static DocxComments ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain an element.");
        }

        return Parse(reader);
    }

    internal void LoadFromXml()
    {
        this.Comments = new List<DocxComment>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("comments", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int rootDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == rootDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == rootDepth + 1)
            {
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);

                if (IsCommentBuffer(child))
                {
                    this.Comments.Add(DocxComment.ParseXml(child.GetXml()));
                }
                else
                {
                    this.UnknownChildren.Add(child);
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal bool IsCommentBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        return reader.MoveToContent() && reader.IsElement("comment", DocxConstants.NamespaceWordprocessingMl);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "" || this.Comments.Count > 0 || this.UnknownChildren.Count > 0;
    }

    internal int CommentCount()
    {
        return this.Comments.Count;
    }

    internal DocxComment GetComment(int index)
    {
        return this.Comments[index];
    }

    internal DocxComment? FindCommentById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxComment comment in this.Comments)
        {
            if (comment.Id == id)
            {
                return comment;
            }
        }

        return null;
    }

    internal int CountCommentsByAuthor(string author)
    {
        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        int count = 0;

        foreach (DocxComment comment in this.Comments)
        {
            if (comment.Author == author)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetText()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetTextWithOptions(options);
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string GetTextWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return DocxTextExtractor.ExtractComments(this, options);
    }

    internal DocxComment CreateComment(string author, string initials, string text)
    {
        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return CreateCommentWithId(AllocateCommentId(), author, initials, "", text);
    }

    internal DocxComment CreateCommentWithId(string id, string author, string initials, string date, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (date == null)
        {
            throw new ArgumentNullException("date");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }

        DocxComment comment = DocxComment.Create(id, author, initials, date, text);
        AddComment(comment);
        return comment;
    }

    internal void AddComment(DocxComment comment)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        if (comment.Id == "")
        {
            throw new ArgumentException("Comment id is required.", "comment");
        }

        if (FindCommentById(comment.Id) != null)
        {
            throw new InvalidOperationException("Comment id already exists.");
        }

        this.Xml = "";
        this.Comments.Add(comment);
    }

    internal bool RemoveCommentById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        for (int index = 0; index < this.Comments.Count; index = index + 1)
        {
            if (this.Comments[index].Id == id)
            {
                this.Xml = "";
                this.Comments.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal string AllocateCommentId()
    {
        int highestId = 0;

        foreach (DocxComment comment in this.Comments)
        {
            if (IsNonNegativeInteger(comment.Id))
            {
                int id = ParseNonNegativeInteger(comment.Id);

                if (id > highestId)
                {
                    highestId = id;
                }
            }
        }

        if (highestId == int.MaxValue)
        {
            throw new InvalidOperationException("No comment id is available.");
        }

        return (highestId + 1).ToString();
    }

    internal static bool IsNonNegativeInteger(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return false;
        }

        for (int index = 0; index < value.Length; index = index + 1)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        int parsed = 0;
        return int.TryParse(value, out parsed);
    }

    internal static int ParseNonNegativeInteger(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!IsNonNegativeInteger(value))
        {
            throw new ArgumentException("Value must be a non-negative integer.", "value");
        }

        int parsed = 0;

        if (!int.TryParse(value, out parsed))
        {
            throw new ArgumentException("Value must be a non-negative integer.", "value");
        }

        return parsed;
    }

    internal string WriteXml()
    {
        if (this.Xml != "")
        {
            return this.Xml;
        }

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

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "comments", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxComment comment in this.Comments)
        {
            comment.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
