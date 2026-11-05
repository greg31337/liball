namespace docx;

/// <summary>
/// Represents compatibility setting data used by a DOCX document.
/// </summary>
public class DocxCompatibilitySetting
{
    internal string Xml = "";
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The uri value stored in the document model.
    /// </summary>
    public string Uri = "";
    /// <summary>
    /// The value value stored in the document model.
    /// </summary>
    public string Value = "";

    /// <summary>
    /// Creates a compatibility setting from XML.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCompatibilitySetting(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        LoadFromXml();
    }

    internal static DocxCompatibilitySetting Create(string name, string uri, string value)
    {
        DocxCompatibilitySetting setting = new DocxCompatibilitySetting("");
        setting.SetName(name);
        setting.SetUri(uri);
        setting.SetValue(value);
        return setting;
    }

    internal static DocxCompatibilitySetting Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("compatSetting", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:compatSetting.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxCompatibilitySetting(buffer.GetXml());
    }

    internal static DocxCompatibilitySetting ParseXml(string xml)
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
        this.Name = "";
        this.Uri = "";
        this.Value = "";

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("compatSetting", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.Name = reader.GetAttribute("name", DocxConstants.NamespaceWordprocessingMl);
        this.Uri = reader.GetAttribute("uri", DocxConstants.NamespaceWordprocessingMl);
        this.Value = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void SetName(string name)
    {
        DocxCompatibilitySettings.ValidateName(name);
        this.Name = name;
    }

    internal void SetUri(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        this.Uri = uri;
    }

    internal void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Value = value;
    }

    internal bool IsName(string name)
    {
        DocxCompatibilitySettings.ValidateName(name);
        return this.Name == name;
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

        writer.StartElement("w", "compatSetting", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "name", DocxConstants.NamespaceWordprocessingMl, this.Name);

        if (this.Uri != "")
        {
            writer.WriteAttribute("w", "uri", DocxConstants.NamespaceWordprocessingMl, this.Uri);
        }

        writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, this.Value);
        writer.EndElement();
    }
}
