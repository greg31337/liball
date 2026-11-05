namespace docx;

/// <summary>
/// Represents extended properties data used by a DOCX document.
/// </summary>
public class DocxExtendedProperties
{
    internal string Xml = "";
    /// <summary>
    /// The template value stored in the document model.
    /// </summary>
    public string Template = "";
    /// <summary>
    /// The manager value stored in the document model.
    /// </summary>
    public string Manager = "";
    /// <summary>
    /// The company value stored in the document model.
    /// </summary>
    public string Company = "";
    /// <summary>
    /// The pages value stored in the document model.
    /// </summary>
    public string Pages = "";
    /// <summary>
    /// The words value stored in the document model.
    /// </summary>
    public string Words = "";
    /// <summary>
    /// The characters value stored in the document model.
    /// </summary>
    public string Characters = "";
    /// <summary>
    /// The presentation format value stored in the document model.
    /// </summary>
    public string PresentationFormat = "";
    /// <summary>
    /// The lines value stored in the document model.
    /// </summary>
    public string Lines = "";
    /// <summary>
    /// The paragraphs value stored in the document model.
    /// </summary>
    public string Paragraphs = "";
    /// <summary>
    /// The slides value stored in the document model.
    /// </summary>
    public string Slides = "";
    /// <summary>
    /// The notes value stored in the document model.
    /// </summary>
    public string Notes = "";
    /// <summary>
    /// The total time value stored in the document model.
    /// </summary>
    public string TotalTime = "";
    /// <summary>
    /// The hidden slides value stored in the document model.
    /// </summary>
    public string HiddenSlides = "";
    /// <summary>
    /// The mm clips value stored in the document model.
    /// </summary>
    public string MMClips = "";
    /// <summary>
    /// The scale crop value stored in the document model.
    /// </summary>
    public string ScaleCrop = "";
    /// <summary>
    /// The preserved heading-pairs vector XML.
    /// </summary>
    public string HeadingPairsXml = "";
    /// <summary>
    /// The preserved titles-of-parts vector XML.
    /// </summary>
    public string TitlesOfPartsXml = "";
    /// <summary>
    /// The links up to date value stored in the document model.
    /// </summary>
    public string LinksUpToDate = "";
    /// <summary>
    /// The characters with spaces value stored in the document model.
    /// </summary>
    public string CharactersWithSpaces = "";
    /// <summary>
    /// The shared doc value stored in the document model.
    /// </summary>
    public string SharedDoc = "";
    /// <summary>
    /// The hyperlink base value stored in the document model.
    /// </summary>
    public string HyperlinkBase = "";
    /// <summary>
    /// The hyperlinks changed value stored in the document model.
    /// </summary>
    public string HyperlinksChanged = "";
    /// <summary>
    /// The app version value stored in the document model.
    /// </summary>
    public string AppVersion = "";
    /// <summary>
    /// The application value stored in the document model.
    /// </summary>
    public string Application = "";
    /// <summary>
    /// The doc security value stored in the document model.
    /// </summary>
    public string DocSecurity = "";
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates an extended properties instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxExtendedProperties(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxExtendedProperties Create()
    {
        return new DocxExtendedProperties("");
    }

    internal void Initialize()
    {
        this.Template = "";
        this.Manager = "";
        this.Company = "";
        this.Pages = "";
        this.Words = "";
        this.Characters = "";
        this.PresentationFormat = "";
        this.Lines = "";
        this.Paragraphs = "";
        this.Slides = "";
        this.Notes = "";
        this.TotalTime = "";
        this.HiddenSlides = "";
        this.MMClips = "";
        this.ScaleCrop = "";
        this.HeadingPairsXml = "";
        this.TitlesOfPartsXml = "";
        this.LinksUpToDate = "";
        this.CharactersWithSpaces = "";
        this.SharedDoc = "";
        this.HyperlinkBase = "";
        this.HyperlinksChanged = "";
        this.AppVersion = "";
        this.Application = "";
        this.DocSecurity = "";
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("Properties", DocxConstants.NamespaceExtendedProperties);
    }

    internal static DocxExtendedProperties Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on extended properties.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxExtendedProperties(buffer.GetXml());
    }

    internal static DocxExtendedProperties ParseXml(string xml)
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

        if (!reader.ReadToElement("Properties", DocxConstants.NamespaceExtendedProperties))
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

        if (reader.NamespaceUri() != DocxConstants.NamespaceExtendedProperties)
        {
            return false;
        }

        string localName = reader.LocalName();

        if (localName == "HeadingPairs")
        {
            DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
            this.HeadingPairsXml = buffer.GetXml();
            return true;
        }

        if (localName == "TitlesOfParts")
        {
            DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
            this.TitlesOfPartsXml = buffer.GetXml();
            return true;
        }

        string value = reader.Reader.ReadElementContentAsString();
        SetKnownValue(localName, value);
        return true;
    }

    internal void SetKnownValue(string localName, string value)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (localName == "Template")
        {
            this.Template = value;
        }
        else if (localName == "Manager")
        {
            this.Manager = value;
        }
        else if (localName == "Company")
        {
            this.Company = value;
        }
        else if (localName == "Pages")
        {
            this.Pages = value;
        }
        else if (localName == "Words")
        {
            this.Words = value;
        }
        else if (localName == "Characters")
        {
            this.Characters = value;
        }
        else if (localName == "PresentationFormat")
        {
            this.PresentationFormat = value;
        }
        else if (localName == "Lines")
        {
            this.Lines = value;
        }
        else if (localName == "Paragraphs")
        {
            this.Paragraphs = value;
        }
        else if (localName == "Slides")
        {
            this.Slides = value;
        }
        else if (localName == "Notes")
        {
            this.Notes = value;
        }
        else if (localName == "TotalTime")
        {
            this.TotalTime = value;
        }
        else if (localName == "HiddenSlides")
        {
            this.HiddenSlides = value;
        }
        else if (localName == "MMClips")
        {
            this.MMClips = value;
        }
        else if (localName == "ScaleCrop")
        {
            this.ScaleCrop = value;
        }
        else if (localName == "LinksUpToDate")
        {
            this.LinksUpToDate = value;
        }
        else if (localName == "CharactersWithSpaces")
        {
            this.CharactersWithSpaces = value;
        }
        else if (localName == "SharedDoc")
        {
            this.SharedDoc = value;
        }
        else if (localName == "HyperlinkBase")
        {
            this.HyperlinkBase = value;
        }
        else if (localName == "HyperlinksChanged")
        {
            this.HyperlinksChanged = value;
        }
        else if (localName == "AppVersion")
        {
            this.AppVersion = value;
        }
        else if (localName == "Application")
        {
            this.Application = value;
        }
        else if (localName == "DocSecurity")
        {
            this.DocSecurity = value;
        }
        else
        {
            this.UnknownChildren.Add(new DocxXmlBuffer(CreateExtendedPropertyXml(localName, value)));
        }
    }

    internal static string CreateExtendedPropertyXml(string localName, string value)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.WriteElementText("", localName, DocxConstants.NamespaceExtendedProperties, value);
        return writer.GetXml();
    }

    internal void SetApplication(string value)
    {
        this.Application = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetAppVersion(string value)
    {
        this.AppVersion = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetCompany(string value)
    {
        this.Company = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetManager(string value)
    {
        this.Manager = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetTemplate(string value)
    {
        this.Template = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetPages(string value)
    {
        this.Pages = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetWords(string value)
    {
        this.Words = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetCharacters(string value)
    {
        this.Characters = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetCharactersWithSpaces(string value)
    {
        this.CharactersWithSpaces = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetPresentationFormat(string value)
    {
        this.PresentationFormat = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetLines(string value)
    {
        this.Lines = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetParagraphs(string value)
    {
        this.Paragraphs = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetSlides(string value)
    {
        this.Slides = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetNotes(string value)
    {
        this.Notes = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetTotalTime(string value)
    {
        this.TotalTime = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetHiddenSlides(string value)
    {
        this.HiddenSlides = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetMMClips(string value)
    {
        this.MMClips = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetDocSecurity(string value)
    {
        this.DocSecurity = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetScaleCrop(string value)
    {
        this.ScaleCrop = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetLinksUpToDate(string value)
    {
        this.LinksUpToDate = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetSharedDoc(string value)
    {
        this.SharedDoc = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetHyperlinksChanged(string value)
    {
        this.HyperlinksChanged = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetHyperlinkBase(string value)
    {
        this.HyperlinkBase = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetHeadingPairsXml(string xml)
    {
        this.HeadingPairsXml = DocxCoreProperties.ValidateValue("xml", xml);
    }

    internal void SetTitlesOfPartsXml(string xml)
    {
        this.TitlesOfPartsXml = DocxCoreProperties.ValidateValue("xml", xml);
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

        writer.StartElement("", "Properties", DocxConstants.NamespaceExtendedProperties);
        writer.WriteNamespace("", DocxConstants.NamespaceExtendedProperties);
        writer.WriteNamespace("vt", DocxConstants.NamespaceDocPropsVTypes);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteTextElement(writer, "Template", this.Template);
        WriteTextElement(writer, "Manager", this.Manager);
        WriteTextElement(writer, "Company", this.Company);
        WriteTextElement(writer, "Pages", this.Pages);
        WriteTextElement(writer, "Words", this.Words);
        WriteTextElement(writer, "Characters", this.Characters);
        WriteTextElement(writer, "PresentationFormat", this.PresentationFormat);
        WriteTextElement(writer, "Lines", this.Lines);
        WriteTextElement(writer, "Paragraphs", this.Paragraphs);
        WriteTextElement(writer, "Slides", this.Slides);
        WriteTextElement(writer, "Notes", this.Notes);
        WriteTextElement(writer, "TotalTime", this.TotalTime);
        WriteTextElement(writer, "HiddenSlides", this.HiddenSlides);
        WriteTextElement(writer, "MMClips", this.MMClips);
        WriteTextElement(writer, "ScaleCrop", this.ScaleCrop);
        WriteRawChild(writer, this.HeadingPairsXml);
        WriteRawChild(writer, this.TitlesOfPartsXml);
        WriteTextElement(writer, "LinksUpToDate", this.LinksUpToDate);
        WriteTextElement(writer, "CharactersWithSpaces", this.CharactersWithSpaces);
        WriteTextElement(writer, "SharedDoc", this.SharedDoc);
        WriteTextElement(writer, "HyperlinkBase", this.HyperlinkBase);
        WriteTextElement(writer, "HyperlinksChanged", this.HyperlinksChanged);
        WriteTextElement(writer, "AppVersion", this.AppVersion);
        WriteTextElement(writer, "Application", this.Application);
        WriteTextElement(writer, "DocSecurity", this.DocSecurity);

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }
    }

    internal static void WriteTextElement(DocxXmlWriter writer, string localName, string value)
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

        writer.WriteElementText("", localName, DocxConstants.NamespaceExtendedProperties, value);
    }

    internal static void WriteRawChild(DocxXmlWriter writer, string xml)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return;
        }

        DocxXmlBuffer buffer = new DocxXmlBuffer(xml);
        buffer.WriteTo(writer);
    }
}
