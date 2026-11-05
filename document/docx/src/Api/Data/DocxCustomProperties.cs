namespace docx;

/// <summary>
/// Represents custom properties data used by a DOCX document.
/// </summary>
public class DocxCustomProperties
{
    internal string Xml = "";
    /// <summary>
    /// The custom properties in document order.
    /// </summary>
    public List<DocxCustomProperty> Properties = new List<DocxCustomProperty>();
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates a custom properties instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCustomProperties(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxCustomProperties Create()
    {
        return new DocxCustomProperties("");
    }

    internal void Initialize()
    {
        this.Properties = new List<DocxCustomProperty>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("Properties", DocxConstants.NamespaceCustomProperties);
    }

    internal static DocxCustomProperties Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on custom properties.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxCustomProperties(buffer.GetXml());
    }

    internal static DocxCustomProperties ParseXml(string xml)
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

        if (!reader.ReadToElement("Properties", DocxConstants.NamespaceCustomProperties))
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
                if (reader.IsElement("property", DocxConstants.NamespaceCustomProperties))
                {
                    this.Properties.Add(DocxCustomProperty.Parse(reader));
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

    internal int PropertyCount()
    {
        return this.Properties.Count;
    }

    internal DocxCustomProperty GetProperty(int index)
    {
        return this.Properties[index];
    }

    internal DocxCustomProperty? FindPropertyByName(string name)
    {
        ValidatePropertyName(name);

        foreach (DocxCustomProperty property in this.Properties)
        {
            if (property.Name == name)
            {
                return property;
            }
        }

        return null;
    }

    internal DocxCustomProperty AddProperty(string name, string type, string value)
    {
        ValidatePropertyName(name);

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (FindPropertyByName(name) != null)
        {
            throw new InvalidOperationException("Custom property already exists.");
        }

        DocxCustomProperty property = DocxCustomProperty.Create(name, AllocatePropertyId(), type, value);
        this.Properties.Add(property);
        return property;
    }

    internal DocxCustomProperty SetProperty(string name, string type, string value)
    {
        ValidatePropertyName(name);

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxCustomProperty? property = FindPropertyByName(name);

        if (property == null)
        {
            return AddProperty(name, type, value);
        }

        property.SetType(type);
        property.SetValue(value);
        return property;
    }

    internal bool RemoveProperty(string name)
    {
        DocxCustomProperty? property = FindPropertyByName(name);

        if (property == null)
        {
            return false;
        }

        return this.Properties.Remove(property);
    }

    internal DocxCustomProperty AddStringProperty(string name, string value)
    {
        return AddProperty(name, "lpwstr", value);
    }

    internal DocxCustomProperty AddIntProperty(string name, string value)
    {
        return AddProperty(name, "i4", value);
    }

    internal DocxCustomProperty AddBoolProperty(string name, string value)
    {
        return AddProperty(name, "bool", value);
    }

    internal string AllocatePropertyId()
    {
        int candidate = 2;

        while (ContainsPropertyId(candidate.ToString(System.Globalization.CultureInfo.InvariantCulture)))
        {
            candidate = candidate + 1;
        }

        return candidate.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal bool ContainsPropertyId(string propertyId)
    {
        if (propertyId == null)
        {
            throw new ArgumentNullException("propertyId");
        }

        foreach (DocxCustomProperty property in this.Properties)
        {
            if (property.PropertyId == propertyId)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidatePropertyName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name == "")
        {
            throw new ArgumentException("Custom property name is required.", "name");
        }
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

        writer.StartElement("", "Properties", DocxConstants.NamespaceCustomProperties);
        writer.WriteNamespace("", DocxConstants.NamespaceCustomProperties);
        writer.WriteNamespace("vt", DocxConstants.NamespaceDocPropsVTypes);

        foreach (DocxCustomProperty property in this.Properties)
        {
            property.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
