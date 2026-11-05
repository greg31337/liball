namespace docx;

/// <summary>
/// Represents shading data used by a DOCX document.
/// </summary>
public class DocxShading
{
    internal string Xml;
    /// <summary>
    /// The val value stored in the document model.
    /// </summary>
    public string Val;
    /// <summary>
    /// The color value stored in the document model.
    /// </summary>
    public string Color;
    /// <summary>
    /// The fill value stored in the document model.
    /// </summary>
    public string Fill;
    /// <summary>
    /// The theme color value stored in the document model.
    /// </summary>
    public string ThemeColor;
    /// <summary>
    /// The theme fill value stored in the document model.
    /// </summary>
    public string ThemeFill;
    /// <summary>
    /// The theme tint value stored in the document model.
    /// </summary>
    public string ThemeTint;
    /// <summary>
    /// The theme fill tint value stored in the document model.
    /// </summary>
    public string ThemeFillTint;
    /// <summary>
    /// The theme shade value stored in the document model.
    /// </summary>
    public string ThemeShade;
    /// <summary>
    /// The theme fill shade value stored in the document model.
    /// </summary>
    public string ThemeFillShade;

    /// <summary>
    /// Creates a shading instance.
    /// </summary>
    public DocxShading()
    {
        this.Xml = "";
        this.Val = "";
        this.Color = "";
        this.Fill = "";
        this.ThemeColor = "";
        this.ThemeFill = "";
        this.ThemeTint = "";
        this.ThemeFillTint = "";
        this.ThemeShade = "";
        this.ThemeFillShade = "";
    }

    internal static DocxShading Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("shd", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:shd.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxShading ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxShading shading = new DocxShading();
        shading.Xml = xml;

        if (xml == "")
        {
            return shading;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("shd", DocxConstants.NamespaceWordprocessingMl))
        {
            return shading;
        }

        shading.ReadAttributes(reader);
        return shading;
    }

    internal void ReadAttributes(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("shd", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:shd.");
        }

        this.Val = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        this.Color = reader.GetAttribute("color", DocxConstants.NamespaceWordprocessingMl);
        this.Fill = reader.GetAttribute("fill", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeColor = reader.GetAttribute("themeColor", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeFill = reader.GetAttribute("themeFill", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeTint = reader.GetAttribute("themeTint", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeFillTint = reader.GetAttribute("themeFillTint", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeShade = reader.GetAttribute("themeShade", DocxConstants.NamespaceWordprocessingMl);
        this.ThemeFillShade = reader.GetAttribute("themeFillShade", DocxConstants.NamespaceWordprocessingMl);
    }

    internal bool HasAnyShading()
    {
        return this.Val != ""
            || this.Color != ""
            || this.Fill != ""
            || this.ThemeColor != ""
            || this.ThemeFill != ""
            || this.ThemeTint != ""
            || this.ThemeFillTint != ""
            || this.ThemeShade != ""
            || this.ThemeFillShade != "";
    }

    internal void SetShading(string val, string color, string fill)
    {
        if (val == null)
        {
            throw new ArgumentNullException("val");
        }

        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (fill == null)
        {
            throw new ArgumentNullException("fill");
        }

        this.Val = val;
        this.Color = color;
        this.Fill = fill;
    }

    internal void SetTheme(string themeColor, string themeFill, string themeTint, string themeFillTint, string themeShade, string themeFillShade)
    {
        if (themeColor == null)
        {
            throw new ArgumentNullException("themeColor");
        }

        if (themeFill == null)
        {
            throw new ArgumentNullException("themeFill");
        }

        if (themeTint == null)
        {
            throw new ArgumentNullException("themeTint");
        }

        if (themeFillTint == null)
        {
            throw new ArgumentNullException("themeFillTint");
        }

        if (themeShade == null)
        {
            throw new ArgumentNullException("themeShade");
        }

        if (themeFillShade == null)
        {
            throw new ArgumentNullException("themeFillShade");
        }

        this.ThemeColor = themeColor;
        this.ThemeFill = themeFill;
        this.ThemeTint = themeTint;
        this.ThemeFillTint = themeFillTint;
        this.ThemeShade = themeShade;
        this.ThemeFillShade = themeFillShade;
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

        writer.StartElement("w", "shd", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.Val);
        WriteOptionalAttribute(writer, "color", this.Color);
        WriteOptionalAttribute(writer, "fill", this.Fill);
        WriteOptionalAttribute(writer, "themeColor", this.ThemeColor);
        WriteOptionalAttribute(writer, "themeFill", this.ThemeFill);
        WriteOptionalAttribute(writer, "themeTint", this.ThemeTint);
        WriteOptionalAttribute(writer, "themeFillTint", this.ThemeFillTint);
        WriteOptionalAttribute(writer, "themeShade", this.ThemeShade);
        WriteOptionalAttribute(writer, "themeFillShade", this.ThemeFillShade);
        writer.EndElement();
    }

    internal void WriteOptionalAttribute(DocxXmlWriter writer, string localName, string value)
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

        if (value != "")
        {
            writer.WriteAttribute("w", localName, DocxConstants.NamespaceWordprocessingMl, value);
        }
    }
}
