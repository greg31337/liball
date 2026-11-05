namespace docx;

/// <summary>
/// Represents compatibility settings data used by a DOCX document.
/// </summary>
public class DocxCompatibilitySettings
{
    internal string Xml = "";
    /// <summary>
    /// The compatibility settings in document order.
    /// </summary>
    public List<DocxCompatibilitySetting> Settings = new List<DocxCompatibilitySetting>();
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates a compatibility settings instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCompatibilitySettings(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxCompatibilitySettings Create()
    {
        return new DocxCompatibilitySettings("");
    }

    internal void Initialize()
    {
        this.Settings = new List<DocxCompatibilitySetting>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("compat", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static DocxCompatibilitySettings Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on w:compat.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxCompatibilitySettings(buffer.GetXml());
    }

    internal static DocxCompatibilitySettings ParseXml(string xml)
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

        if (!reader.ReadToElement("compat", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int compatDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == compatDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == compatDepth + 1)
            {
                if (reader.IsElement("compatSetting", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Settings.Add(DocxCompatibilitySetting.Parse(reader));
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

    internal string GetXml()
    {
        return this.Xml;
    }

    internal int SettingCount()
    {
        return this.Settings.Count;
    }

    internal DocxCompatibilitySetting GetSetting(int index)
    {
        return this.Settings[index];
    }

    internal DocxCompatibilitySetting? FindSettingByName(string name)
    {
        ValidateName(name);

        foreach (DocxCompatibilitySetting setting in this.Settings)
        {
            if (setting.Name == name)
            {
                return setting;
            }
        }

        return null;
    }

    internal DocxCompatibilitySetting AddSetting(string name, string uri, string value)
    {
        DocxCompatibilitySetting setting = DocxCompatibilitySetting.Create(name, uri, value);
        this.Settings.Add(setting);
        return setting;
    }

    internal DocxCompatibilitySetting SetSetting(string name, string uri, string value)
    {
        DocxCompatibilitySetting? setting = FindSettingByName(name);

        if (setting == null)
        {
            return AddSetting(name, uri, value);
        }

        setting.SetUri(uri);
        setting.SetValue(value);
        return setting;
    }

    internal bool RemoveSetting(string name)
    {
        DocxCompatibilitySetting? setting = FindSettingByName(name);

        if (setting == null)
        {
            return false;
        }

        return this.Settings.Remove(setting);
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name == "")
        {
            throw new ArgumentException("Compatibility setting name is required.", "name");
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

        writer.StartElement("w", "compat", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxCompatibilitySetting setting in this.Settings)
        {
            setting.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
