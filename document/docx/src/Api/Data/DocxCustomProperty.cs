namespace docx;

/// <summary>
/// Represents custom property data used by a DOCX document.
/// </summary>
public class DocxCustomProperty
{
    /// <summary>
    /// The default format id identifier used in DOCX package data.
    /// </summary>
    public const string DefaultFormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

    internal string Xml = "";
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The property identifier stored in the document.
    /// </summary>
    public string PropertyId = "";
    /// <summary>
    /// The format identifier stored in the document.
    /// </summary>
    public string FormatId = "";
    /// <summary>
    /// The link target value stored in the document model.
    /// </summary>
    public string LinkTarget = "";
    /// <summary>
    /// The type value stored in the document model.
    /// </summary>
    public string Type = "";
    /// <summary>
    /// The value value stored in the document model.
    /// </summary>
    public string Value = "";
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates a custom property from XML.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCustomProperty(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxCustomProperty Create(string name, string propertyId, string type, string value)
    {
        DocxCustomProperty property = new DocxCustomProperty("");
        property.SetName(name);
        property.SetPropertyId(propertyId);
        property.FormatId = DefaultFormatId;
        property.SetType(type);
        property.SetValue(value);
        return property;
    }

    internal void Initialize()
    {
        this.Name = "";
        this.PropertyId = "";
        this.FormatId = "";
        this.LinkTarget = "";
        this.Type = "";
        this.Value = "";
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static DocxCustomProperty Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("property", DocxConstants.NamespaceCustomProperties))
        {
            throw new InvalidOperationException("Reader is not positioned on a custom property.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxCustomProperty(buffer.GetXml());
    }

    internal static DocxCustomProperty ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);
        reader.MoveToContent();
        return Parse(reader);
    }

    internal void LoadFromXml()
    {
        Initialize();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("property", DocxConstants.NamespaceCustomProperties))
        {
            return;
        }

        this.FormatId = ReadRequiredAttribute(reader, "fmtid");
        this.PropertyId = ReadRequiredAttribute(reader, "pid");
        this.Name = ReadRequiredAttribute(reader, "name");
        this.LinkTarget = reader.GetAttribute("linkTarget", "");

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int propertyDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertyDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertyDepth + 1)
            {
                if (reader.NamespaceUri() == DocxConstants.NamespaceDocPropsVTypes)
                {
                    this.Type = reader.LocalName();
                    this.Value = reader.Reader.ReadElementContentAsString();
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

        string value = reader.GetAttribute(localName, "");

        if (value == "")
        {
            throw new System.IO.InvalidDataException("Custom property is missing required attribute.");
        }

        return value;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal void SetName(string name)
    {
        DocxCustomProperties.ValidatePropertyName(name);
        this.Name = name;
    }

    internal void SetPropertyId(string propertyId)
    {
        if (propertyId == null)
        {
            throw new ArgumentNullException("propertyId");
        }

        if (propertyId == "")
        {
            throw new ArgumentException("Custom property id is required.", "propertyId");
        }

        this.PropertyId = propertyId;
    }

    internal void SetFormatId(string formatId)
    {
        if (formatId == null)
        {
            throw new ArgumentNullException("formatId");
        }

        if (formatId == "")
        {
            throw new ArgumentException("Custom property format id is required.", "formatId");
        }

        this.FormatId = formatId;
    }

    internal void SetLinkTarget(string linkTarget)
    {
        if (linkTarget == null)
        {
            throw new ArgumentNullException("linkTarget");
        }

        this.LinkTarget = linkTarget;
    }

    internal void SetType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (type == "")
        {
            throw new ArgumentException("Custom property type is required.", "type");
        }

        this.Type = type;
    }

    internal void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Value = value;
    }

    internal bool IsType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (type == "")
        {
            throw new ArgumentException("Custom property type is required.", "type");
        }

        return this.Type == type;
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

        writer.StartElement("", "property", DocxConstants.NamespaceCustomProperties);
        writer.WriteAttributeWithoutPrefix("fmtid", this.FormatId);
        writer.WriteAttributeWithoutPrefix("pid", this.PropertyId);
        writer.WriteAttributeWithoutPrefix("name", this.Name);

        if (this.LinkTarget != "")
        {
            writer.WriteAttributeWithoutPrefix("linkTarget", this.LinkTarget);
        }

        WriteValueTo(writer);

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }

    internal void WriteValueTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        string type = this.Type;

        if (type == "")
        {
            type = "lpwstr";
        }

        writer.WriteElementText("vt", type, DocxConstants.NamespaceDocPropsVTypes, this.Value);
    }
}
