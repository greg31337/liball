namespace docx;

/// <summary>
/// Represents core properties data used by a DOCX document.
/// </summary>
public class DocxCoreProperties
{
    internal string Xml = "";
    /// <summary>
    /// The title value stored in the document model.
    /// </summary>
    public string Title = "";
    /// <summary>
    /// The subject value stored in the document model.
    /// </summary>
    public string Subject = "";
    /// <summary>
    /// The creator value stored in the document model.
    /// </summary>
    public string Creator = "";
    /// <summary>
    /// The keywords value stored in the document model.
    /// </summary>
    public string Keywords = "";
    /// <summary>
    /// The description value stored in the document model.
    /// </summary>
    public string Description = "";
    /// <summary>
    /// The last modified by value stored in the document model.
    /// </summary>
    public string LastModifiedBy = "";
    /// <summary>
    /// The revision value stored in the document model.
    /// </summary>
    public string Revision = "";
    /// <summary>
    /// The created value stored in the document model.
    /// </summary>
    public string Created = "";
    /// <summary>
    /// The modified value stored in the document model.
    /// </summary>
    public string Modified = "";
    /// <summary>
    /// The category value stored in the document model.
    /// </summary>
    public string Category = "";
    /// <summary>
    /// The content status value stored in the document model.
    /// </summary>
    public string ContentStatus = "";
    /// <summary>
    /// The version value stored in the document model.
    /// </summary>
    public string Version = "";
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates a core properties instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCoreProperties(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxCoreProperties Create()
    {
        return new DocxCoreProperties("");
    }

    internal void Initialize()
    {
        this.Title = "";
        this.Subject = "";
        this.Creator = "";
        this.Keywords = "";
        this.Description = "";
        this.LastModifiedBy = "";
        this.Revision = "";
        this.Created = "";
        this.Modified = "";
        this.Category = "";
        this.ContentStatus = "";
        this.Version = "";
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("coreProperties", DocxConstants.NamespaceCoreProperties);
    }

    internal static DocxCoreProperties Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on cp:coreProperties.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxCoreProperties(buffer.GetXml());
    }

    internal static DocxCoreProperties ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);
        reader.MoveToContent();
        return Parse(reader);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadFromXml()
    {
        Initialize();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("coreProperties", DocxConstants.NamespaceCoreProperties))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1)
            {
                if (ReadKnownChild(reader))
                {
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                this.UnknownChildren.Add(child);
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal bool ReadKnownChild(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("title", DocxConstants.NamespaceDublinCore))
        {
            this.Title = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("subject", DocxConstants.NamespaceDublinCore))
        {
            this.Subject = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("creator", DocxConstants.NamespaceDublinCore))
        {
            this.Creator = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("description", DocxConstants.NamespaceDublinCore))
        {
            this.Description = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("keywords", DocxConstants.NamespaceCoreProperties))
        {
            this.Keywords = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("lastModifiedBy", DocxConstants.NamespaceCoreProperties))
        {
            this.LastModifiedBy = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("revision", DocxConstants.NamespaceCoreProperties))
        {
            this.Revision = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("category", DocxConstants.NamespaceCoreProperties))
        {
            this.Category = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("contentStatus", DocxConstants.NamespaceCoreProperties))
        {
            this.ContentStatus = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("version", DocxConstants.NamespaceCoreProperties))
        {
            this.Version = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("created", DocxConstants.NamespaceDublinCoreTerms))
        {
            this.Created = reader.Reader.ReadElementContentAsString();
            return true;
        }

        if (reader.IsElement("modified", DocxConstants.NamespaceDublinCoreTerms))
        {
            this.Modified = reader.Reader.ReadElementContentAsString();
            return true;
        }

        return false;
    }

    internal bool HasAnyProperties()
    {
        return this.Title != ""
            || this.Subject != ""
            || this.Creator != ""
            || this.Keywords != ""
            || this.Description != ""
            || this.LastModifiedBy != ""
            || this.Revision != ""
            || this.Created != ""
            || this.Modified != ""
            || this.Category != ""
            || this.ContentStatus != ""
            || this.Version != ""
            || this.UnknownChildren.Count > 0;
    }

    internal void SetTitle(string value)
    {
        this.Title = ValidateValue("value", value);
    }

    internal void SetSubject(string value)
    {
        this.Subject = ValidateValue("value", value);
    }

    internal void SetCreator(string value)
    {
        this.Creator = ValidateValue("value", value);
    }

    internal void SetKeywords(string value)
    {
        this.Keywords = ValidateValue("value", value);
    }

    internal void SetDescription(string value)
    {
        this.Description = ValidateValue("value", value);
    }

    internal void SetLastModifiedBy(string value)
    {
        this.LastModifiedBy = ValidateValue("value", value);
    }

    internal void SetRevision(string value)
    {
        this.Revision = ValidateValue("value", value);
    }

    internal void SetCreated(string value)
    {
        this.Created = ValidateValue("value", value);
    }

    internal void SetModified(string value)
    {
        this.Modified = ValidateValue("value", value);
    }

    internal void SetCategory(string value)
    {
        this.Category = ValidateValue("value", value);
    }

    internal void SetContentStatus(string value)
    {
        this.ContentStatus = ValidateValue("value", value);
    }

    internal void SetVersion(string value)
    {
        this.Version = ValidateValue("value", value);
    }

    internal static string ValidateValue(string name, string value)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }

    internal void AddUnknownChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.UnknownChildren.Add(child);
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    internal string GetUnknownChildXml(int index)
    {
        return this.UnknownChildren[index].GetXml();
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

        writer.StartElement("cp", "coreProperties", DocxConstants.NamespaceCoreProperties);
        writer.WriteNamespace("cp", DocxConstants.NamespaceCoreProperties);
        writer.WriteNamespace("dc", DocxConstants.NamespaceDublinCore);
        writer.WriteNamespace("dcterms", DocxConstants.NamespaceDublinCoreTerms);
        writer.WriteNamespace("dcmitype", DocxConstants.NamespaceDublinCoreType);
        writer.WriteNamespace("xsi", DocxConstants.NamespaceXmlSchemaInstance);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteTextElement(writer, "dc", "title", DocxConstants.NamespaceDublinCore, this.Title);
        WriteTextElement(writer, "dc", "subject", DocxConstants.NamespaceDublinCore, this.Subject);
        WriteTextElement(writer, "dc", "creator", DocxConstants.NamespaceDublinCore, this.Creator);
        WriteTextElement(writer, "cp", "keywords", DocxConstants.NamespaceCoreProperties, this.Keywords);
        WriteTextElement(writer, "dc", "description", DocxConstants.NamespaceDublinCore, this.Description);
        WriteTextElement(writer, "cp", "lastModifiedBy", DocxConstants.NamespaceCoreProperties, this.LastModifiedBy);
        WriteTextElement(writer, "cp", "revision", DocxConstants.NamespaceCoreProperties, this.Revision);
        WriteDateElement(writer, "created", this.Created);
        WriteDateElement(writer, "modified", this.Modified);
        WriteTextElement(writer, "cp", "category", DocxConstants.NamespaceCoreProperties, this.Category);
        WriteTextElement(writer, "cp", "contentStatus", DocxConstants.NamespaceCoreProperties, this.ContentStatus);
        WriteTextElement(writer, "cp", "version", DocxConstants.NamespaceCoreProperties, this.Version);

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }
    }

    internal static void WriteTextElement(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.WriteElementText(prefix, localName, namespaceUri, value);
    }

    internal static void WriteDateElement(DocxXmlWriter writer, string localName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.StartElement("dcterms", localName, DocxConstants.NamespaceDublinCoreTerms);
        writer.WriteAttribute("xsi", "type", DocxConstants.NamespaceXmlSchemaInstance, "dcterms:W3CDTF");
        writer.Writer.WriteString(value);
        writer.EndElement();
    }
}
