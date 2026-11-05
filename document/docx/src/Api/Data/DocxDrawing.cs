namespace docx;

/// <summary>
/// Represents drawing data used by a DOCX document.
/// </summary>
public class DocxDrawing
{
    /// <summary>
    /// The default name identifier used in DOCX package data.
    /// </summary>
    public const string DefaultName = "Picture";
    /// <summary>
    /// The kind drawing identifier used in DOCX package data.
    /// </summary>
    public const string KindDrawing = "drawing";
    /// <summary>
    /// The kind inline identifier used in DOCX package data.
    /// </summary>
    public const string KindInline = "inline";
    /// <summary>
    /// The kind floating identifier used in DOCX package data.
    /// </summary>
    public const string KindFloating = "floating";
    /// <summary>
    /// The kind chart identifier used in DOCX package data.
    /// </summary>
    public const string KindChart = "chart";
    /// <summary>
    /// The kind diagram identifier used in DOCX package data.
    /// </summary>
    public const string KindDiagram = "diagram";
    /// <summary>
    /// The kind vml identifier used in DOCX package data.
    /// </summary>
    public const string KindVml = "vml";
    /// <summary>
    /// The kind embedded object identifier used in DOCX package data.
    /// </summary>
    public const string KindEmbeddedObject = "embeddedObject";

    internal string Xml;
    /// <summary>
    /// The prefix value stored in the document model.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// The local name value stored in the document model.
    /// </summary>
    public string LocalName;
    /// <summary>
    /// The namespace uri value stored in the document model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// The kind value stored in the document model.
    /// </summary>
    public string Kind;
    /// <summary>
    /// Whether inline drawing was explicitly supplied.
    /// </summary>
    public bool HasInlineDrawing;
    /// <summary>
    /// Whether floating drawing was explicitly supplied.
    /// </summary>
    public bool HasFloatingDrawing;
    /// <summary>
    /// Whether chart was explicitly supplied.
    /// </summary>
    public bool HasChart;
    /// <summary>
    /// Whether diagram was explicitly supplied.
    /// </summary>
    public bool HasDiagram;
    /// <summary>
    /// Whether vml was explicitly supplied.
    /// </summary>
    public bool HasVml;
    /// <summary>
    /// Whether embedded object was explicitly supplied.
    /// </summary>
    public bool HasEmbeddedObject;
    /// <summary>
    /// The relationship identifier stored in the document.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name;
    /// <summary>
    /// The description value stored in the document model.
    /// </summary>
    public string Description;
    /// <summary>
    /// The width emu value stored in the document model.
    /// </summary>
    public long WidthEmu;
    /// <summary>
    /// The height emu value stored in the document model.
    /// </summary>
    public long HeightEmu;
    /// <summary>
    /// The doc property identifier stored in the document.
    /// </summary>
    public string DocPropertyId;
    /// <summary>
    /// The non visual property identifier stored in the document.
    /// </summary>
    public string NonVisualPropertyId;

    /// <summary>
    /// Creates a drawing instance.
    /// </summary>
    public DocxDrawing()
    {
        this.Xml = "";
        this.Prefix = "";
        this.LocalName = "";
        this.NamespaceUri = "";
        this.Kind = KindDrawing;
        this.HasInlineDrawing = false;
        this.HasFloatingDrawing = false;
        this.HasChart = false;
        this.HasDiagram = false;
        this.HasVml = false;
        this.HasEmbeddedObject = false;
        this.RelationshipId = "";
        this.Name = "";
        this.Description = "";
        this.WidthEmu = 0;
        this.HeightEmu = 0;
        this.DocPropertyId = "1";
        this.NonVisualPropertyId = "0";
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.Reader.NodeType != System.Xml.XmlNodeType.Element)
        {
            return false;
        }

        string localName = reader.LocalName();
        string namespaceUri = reader.NamespaceUri();

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl && localName == "drawing")
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl && localName == "pict")
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl && localName == "object")
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceVml)
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceDrawingMlChart)
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceDrawingMlDiagram)
        {
            return true;
        }

        if (namespaceUri == DocxConstants.NamespaceOffice && localName == "OLEObject")
        {
            return true;
        }

        return false;
    }

    internal static DocxDrawing Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on a supported drawing or object element.");
        }

        string prefix = reader.Reader.Prefix;
        string localName = reader.LocalName();
        string namespaceUri = reader.NamespaceUri();
        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        DocxDrawing drawing = new DocxDrawing();
        drawing.SetPreservedXml(buffer.GetXml(), prefix, localName, namespaceUri);
        return drawing;
    }

    internal static DocxDrawing ParseXml(string xml)
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

    internal static DocxDrawing CreateInlineImage(DocxImage image, long widthEmu, long heightEmu)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        return CreateInlineImage(image.RelationshipId, BuildImageName(image), "", widthEmu, heightEmu);
    }

    internal static DocxDrawing CreateInlineImage(string relationshipId, string name, string description, long widthEmu, long heightEmu)
    {
        DocxDrawing drawing = new DocxDrawing();
        drawing.SetInlineImage(relationshipId, name, description, widthEmu, heightEmu);
        return drawing;
    }

    internal static string BuildImageName(DocxImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (image.PartName == "")
        {
            return DefaultName;
        }

        int index = image.PartName.LastIndexOf("/");

        if (index < 0)
        {
            return image.PartName;
        }

        if (index + 1 >= image.PartName.Length)
        {
            return DefaultName;
        }

        return image.PartName.Substring(index + 1);
    }

    internal void SetInlineImage(string relationshipId, string name, string description, long widthEmu, long heightEmu)
    {
        ValidateInlineImage(relationshipId, name, description, widthEmu, heightEmu);

        this.Xml = "";
        this.Prefix = "";
        this.LocalName = "";
        this.NamespaceUri = "";
        this.Kind = KindInline;
        this.HasInlineDrawing = true;
        this.HasFloatingDrawing = false;
        this.HasChart = false;
        this.HasDiagram = false;
        this.HasVml = false;
        this.HasEmbeddedObject = false;
        this.RelationshipId = relationshipId;
        this.Name = name;
        this.Description = description;
        this.WidthEmu = widthEmu;
        this.HeightEmu = heightEmu;
    }

    internal void SetPreservedXml(string xml, string prefix, string localName, string namespaceUri)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == "")
        {
            throw new ArgumentException("Element local name is required.", "localName");
        }

        this.Xml = xml;
        this.Prefix = prefix;
        this.LocalName = localName;
        this.NamespaceUri = namespaceUri;
        AnalyzePreservedXml();
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool IsKind(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (kind == "")
        {
            throw new ArgumentException("Drawing kind is required.", "kind");
        }

        if (kind == KindInline)
        {
            return IsInline();
        }

        if (kind == KindFloating)
        {
            return IsFloating();
        }

        if (kind == KindChart)
        {
            return ContainsChart();
        }

        if (kind == KindDiagram)
        {
            return ContainsDiagram();
        }

        if (kind == KindVml)
        {
            return ContainsVml();
        }

        if (kind == KindEmbeddedObject)
        {
            return ContainsEmbeddedObject();
        }

        return this.Kind == kind;
    }

    internal bool IsInline()
    {
        return this.HasInlineDrawing;
    }

    internal bool IsFloating()
    {
        return this.HasFloatingDrawing;
    }

    internal bool ContainsChart()
    {
        return this.HasChart;
    }

    internal bool ContainsDiagram()
    {
        return this.HasDiagram;
    }

    internal bool ContainsVml()
    {
        return this.HasVml;
    }

    internal bool ContainsEmbeddedObject()
    {
        return this.HasEmbeddedObject;
    }

    internal void AnalyzePreservedXml()
    {
        this.Kind = KindDrawing;
        this.HasInlineDrawing = false;
        this.HasFloatingDrawing = false;
        this.HasChart = false;
        this.HasDiagram = false;
        this.HasVml = false;
        this.HasEmbeddedObject = false;
        this.RelationshipId = "";
        this.Name = "";
        this.Description = "";
        this.WidthEmu = 0;
        this.HeightEmu = 0;
        this.DocPropertyId = "1";
        this.NonVisualPropertyId = "0";

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);
        bool hasNode = reader.MoveToContent();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                AnalyzeElement(reader.LocalName(), reader.NamespaceUri());
                ReadPreservedMetadata(reader);
            }

            hasNode = reader.Read();
        }

        SetKindFromFlags();
    }

    internal void ReadPreservedMetadata(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string localName = reader.LocalName();
        string namespaceUri = reader.NamespaceUri();

        if (namespaceUri == DocxConstants.NamespaceWordprocessingDrawing && localName == "extent")
        {
            long width = 0;
            long height = 0;
            string widthValue = reader.Reader.GetAttribute("cx") ?? "";
            string heightValue = reader.Reader.GetAttribute("cy") ?? "";

            if (long.TryParse(widthValue, out width))
            {
                this.WidthEmu = width;
            }

            if (long.TryParse(heightValue, out height))
            {
                this.HeightEmu = height;
            }
        }
        else if (namespaceUri == DocxConstants.NamespaceWordprocessingDrawing && localName == "docPr")
        {
            this.DocPropertyId = reader.Reader.GetAttribute("id") ?? this.DocPropertyId;
            this.Name = reader.Reader.GetAttribute("name") ?? this.Name;
            this.Description = reader.Reader.GetAttribute("descr") ?? this.Description;
        }
        else if (namespaceUri == DocxConstants.NamespaceDrawingMlPicture && localName == "cNvPr")
        {
            this.NonVisualPropertyId = reader.Reader.GetAttribute("id") ?? this.NonVisualPropertyId;

            if (this.Name == "")
            {
                this.Name = reader.Reader.GetAttribute("name") ?? this.Name;
            }

            if (this.Description == "")
            {
                this.Description = reader.Reader.GetAttribute("descr") ?? this.Description;
            }
        }
        else if (namespaceUri == DocxConstants.NamespaceDrawingMl && localName == "blip")
        {
            this.RelationshipId = reader.GetAttribute("embed", DocxConstants.NamespaceRelationships);
        }
    }

    internal void AnalyzeElement(string localName, string namespaceUri)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingDrawing && localName == "inline")
        {
            this.HasInlineDrawing = true;
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingDrawing && localName == "anchor")
        {
            this.HasFloatingDrawing = true;
        }

        if (namespaceUri == DocxConstants.NamespaceDrawingMlChart)
        {
            this.HasChart = true;
        }

        if (namespaceUri == DocxConstants.NamespaceDrawingMlDiagram)
        {
            this.HasDiagram = true;
        }

        if (namespaceUri == DocxConstants.NamespaceVml)
        {
            this.HasVml = true;
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl && localName == "pict")
        {
            this.HasVml = true;
        }

        if (namespaceUri == DocxConstants.NamespaceWordprocessingMl && localName == "object")
        {
            this.HasEmbeddedObject = true;
        }

        if (namespaceUri == DocxConstants.NamespaceOffice && localName == "OLEObject")
        {
            this.HasEmbeddedObject = true;
        }
    }

    internal void SetKindFromFlags()
    {
        if (this.HasEmbeddedObject)
        {
            this.Kind = KindEmbeddedObject;
            return;
        }

        if (this.HasVml)
        {
            this.Kind = KindVml;
            return;
        }

        if (this.HasFloatingDrawing)
        {
            this.Kind = KindFloating;
            return;
        }

        if (this.HasChart)
        {
            this.Kind = KindChart;
            return;
        }

        if (this.HasDiagram)
        {
            this.Kind = KindDiagram;
            return;
        }

        if (this.HasInlineDrawing)
        {
            this.Kind = KindInline;
            return;
        }

        this.Kind = KindDrawing;
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

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        ValidateInlineImage(this.RelationshipId, this.Name, this.Description, this.WidthEmu, this.HeightEmu);

        writer.StartElement("w", "r", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "drawing", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("wp", "inline", DocxConstants.NamespaceWordprocessingDrawing);

        WriteExtentTo(writer);
        WriteDocPropertiesTo(writer);
        WriteNonVisualGraphicFramePropertiesTo(writer);
        WriteGraphicTo(writer);

        writer.EndElement();
        writer.EndElement();
        writer.EndElement();
    }

    internal void WriteExtentTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("wp", "extent", DocxConstants.NamespaceWordprocessingDrawing);
        writer.WriteAttributeWithoutPrefix("cx", this.WidthEmu.ToString());
        writer.WriteAttributeWithoutPrefix("cy", this.HeightEmu.ToString());
        writer.EndElement();
    }

    internal void WriteDocPropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("wp", "docPr", DocxConstants.NamespaceWordprocessingDrawing);
        writer.WriteAttributeWithoutPrefix("id", this.DocPropertyId);
        writer.WriteAttributeWithoutPrefix("name", this.Name);
        writer.WriteAttributeWithoutPrefix("descr", this.Description);
        writer.EndElement();
    }

    internal void WriteNonVisualGraphicFramePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("wp", "cNvGraphicFramePr", DocxConstants.NamespaceWordprocessingDrawing);
        writer.StartElement("a", "graphicFrameLocks", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttributeWithoutPrefix("noChangeAspect", "1");
        writer.EndElement();
        writer.EndElement();
    }

    internal void WriteGraphicTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("a", "graphic", DocxConstants.NamespaceDrawingMl);
        writer.StartElement("a", "graphicData", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttributeWithoutPrefix("uri", DocxConstants.NamespaceDrawingMlPicture);
        WritePictureTo(writer);
        writer.EndElement();
        writer.EndElement();
    }

    internal void WritePictureTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("pic", "pic", DocxConstants.NamespaceDrawingMlPicture);
        WriteNonVisualPicturePropertiesTo(writer);
        WriteBlipFillTo(writer);
        WriteShapePropertiesTo(writer);
        writer.EndElement();
    }

    internal void WriteNonVisualPicturePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("pic", "nvPicPr", DocxConstants.NamespaceDrawingMlPicture);
        writer.StartElement("pic", "cNvPr", DocxConstants.NamespaceDrawingMlPicture);
        writer.WriteAttributeWithoutPrefix("id", this.NonVisualPropertyId);
        writer.WriteAttributeWithoutPrefix("name", this.Name);
        writer.WriteAttributeWithoutPrefix("descr", this.Description);
        writer.EndElement();
        writer.StartElement("pic", "cNvPicPr", DocxConstants.NamespaceDrawingMlPicture);
        writer.EndElement();
        writer.EndElement();
    }

    internal void WriteBlipFillTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("pic", "blipFill", DocxConstants.NamespaceDrawingMlPicture);
        writer.StartElement("a", "blip", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttribute("r", "embed", DocxConstants.NamespaceRelationships, this.RelationshipId);
        writer.EndElement();
        writer.StartElement("a", "stretch", DocxConstants.NamespaceDrawingMl);
        writer.StartElement("a", "fillRect", DocxConstants.NamespaceDrawingMl);
        writer.EndElement();
        writer.EndElement();
        writer.EndElement();
    }

    internal void WriteShapePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("pic", "spPr", DocxConstants.NamespaceDrawingMlPicture);
        writer.StartElement("a", "xfrm", DocxConstants.NamespaceDrawingMl);
        writer.StartElement("a", "off", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttributeWithoutPrefix("x", "0");
        writer.WriteAttributeWithoutPrefix("y", "0");
        writer.EndElement();
        writer.StartElement("a", "ext", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttributeWithoutPrefix("cx", this.WidthEmu.ToString());
        writer.WriteAttributeWithoutPrefix("cy", this.HeightEmu.ToString());
        writer.EndElement();
        writer.EndElement();
        writer.StartElement("a", "prstGeom", DocxConstants.NamespaceDrawingMl);
        writer.WriteAttributeWithoutPrefix("prst", "rect");
        writer.StartElement("a", "avLst", DocxConstants.NamespaceDrawingMl);
        writer.EndElement();
        writer.EndElement();
        writer.EndElement();
    }

    internal static void ValidateInlineImage(string relationshipId, string name, string description, long widthEmu, long heightEmu)
    {
        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        if (relationshipId == "")
        {
            throw new ArgumentException("Relationship id is required.", "relationshipId");
        }

        if (name == "")
        {
            throw new ArgumentException("Image name is required.", "name");
        }

        if (widthEmu <= 0)
        {
            throw new ArgumentOutOfRangeException("widthEmu");
        }

        if (heightEmu <= 0)
        {
            throw new ArgumentOutOfRangeException("heightEmu");
        }
    }
}
