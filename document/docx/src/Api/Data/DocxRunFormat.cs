namespace docx;

/// <summary>
/// Represents run format data used by a DOCX document.
/// </summary>
public class DocxRunFormat
{
    internal string Xml;
    /// <summary>
    /// Whether bold was explicitly supplied.
    /// </summary>
    public bool HasBold;
    /// <summary>
    /// Whether the bold behavior is enabled.
    /// </summary>
    public bool Bold;
    /// <summary>
    /// Whether italic was explicitly supplied.
    /// </summary>
    public bool HasItalic;
    /// <summary>
    /// Whether the italic behavior is enabled.
    /// </summary>
    public bool Italic;
    /// <summary>
    /// Whether strike was explicitly supplied.
    /// </summary>
    public bool HasStrike;
    /// <summary>
    /// Whether the strike behavior is enabled.
    /// </summary>
    public bool Strike;
    /// <summary>
    /// Whether double strike was explicitly supplied.
    /// </summary>
    public bool HasDoubleStrike;
    /// <summary>
    /// Whether the double strike behavior is enabled.
    /// </summary>
    public bool DoubleStrike;
    /// <summary>
    /// Whether capital letters were explicitly supplied.
    /// </summary>
    public bool HasCaps;
    /// <summary>
    /// Whether the capital letters behavior is enabled.
    /// </summary>
    public bool Caps;
    /// <summary>
    /// Whether small capital letters were explicitly supplied.
    /// </summary>
    public bool HasSmallCaps;
    /// <summary>
    /// Whether the small capital letters behavior is enabled.
    /// </summary>
    public bool SmallCaps;
    /// <summary>
    /// Whether web-hidden text was explicitly supplied.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Whether text is hidden in web layouts.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Whether vanished text was explicitly supplied.
    /// </summary>
    public bool HasVanish;
    /// <summary>
    /// Whether the vanished text behavior is enabled.
    /// </summary>
    public bool Vanish;
    /// <summary>
    /// Whether embossing was explicitly supplied.
    /// </summary>
    public bool HasEmboss;
    /// <summary>
    /// Whether the emboss behavior is enabled.
    /// </summary>
    public bool Emboss;
    /// <summary>
    /// Whether imprinting was explicitly supplied.
    /// </summary>
    public bool HasImprint;
    /// <summary>
    /// Whether the imprint behavior is enabled.
    /// </summary>
    public bool Imprint;
    /// <summary>
    /// Whether outlining was explicitly supplied.
    /// </summary>
    public bool HasOutline;
    /// <summary>
    /// Whether the outline behavior is enabled.
    /// </summary>
    public bool Outline;
    /// <summary>
    /// Whether shadowing was explicitly supplied.
    /// </summary>
    public bool HasShadow;
    /// <summary>
    /// Whether the shadow behavior is enabled.
    /// </summary>
    public bool Shadow;
    /// <summary>
    /// Whether right-to-left layout was explicitly supplied.
    /// </summary>
    public bool HasRightToLeft;
    /// <summary>
    /// Whether right-to-left layout is enabled.
    /// </summary>
    public bool RightToLeft;
    /// <summary>
    /// Whether complex-script processing was explicitly supplied.
    /// </summary>
    public bool HasComplexScript;
    /// <summary>
    /// Whether complex-script processing is enabled.
    /// </summary>
    public bool ComplexScript;
    /// <summary>
    /// Whether complex-script bold was explicitly supplied.
    /// </summary>
    public bool HasComplexScriptBold;
    /// <summary>
    /// Whether complex-script bold is enabled.
    /// </summary>
    public bool ComplexScriptBold;
    /// <summary>
    /// Whether complex-script italic was explicitly supplied.
    /// </summary>
    public bool HasComplexScriptItalic;
    /// <summary>
    /// Whether complex-script italic is enabled.
    /// </summary>
    public bool ComplexScriptItalic;
    /// <summary>
    /// The style identifier stored in the document.
    /// </summary>
    public string StyleId;
    /// <summary>
    /// The font ascii value stored in the document model.
    /// </summary>
    public string FontAscii;
    /// <summary>
    /// The font high ansi value stored in the document model.
    /// </summary>
    public string FontHighAnsi;
    /// <summary>
    /// The font east asia value stored in the document model.
    /// </summary>
    public string FontEastAsia;
    /// <summary>
    /// The font complex script value stored in the document model.
    /// </summary>
    public string FontComplexScript;
    /// <summary>
    /// The ASCII theme-font reference.
    /// </summary>
    public string FontAsciiTheme;
    /// <summary>
    /// The high-ANSI theme-font reference.
    /// </summary>
    public string FontHighAnsiTheme;
    /// <summary>
    /// The East Asian theme-font reference.
    /// </summary>
    public string FontEastAsiaTheme;
    /// <summary>
    /// The complex-script theme-font reference.
    /// </summary>
    public string FontComplexScriptTheme;
    /// <summary>
    /// The font content hint.
    /// </summary>
    public string FontHint;
    /// <summary>
    /// The underline value stored in the document model.
    /// </summary>
    public string Underline;
    /// <summary>
    /// The underline color.
    /// </summary>
    public string UnderlineColor;
    /// <summary>
    /// The underline theme-color reference.
    /// </summary>
    public string UnderlineThemeColor;
    /// <summary>
    /// The underline theme tint.
    /// </summary>
    public string UnderlineThemeTint;
    /// <summary>
    /// The underline theme shade.
    /// </summary>
    public string UnderlineThemeShade;
    /// <summary>
    /// The color value stored in the document model.
    /// </summary>
    public string Color;
    /// <summary>
    /// The text theme-color reference.
    /// </summary>
    public string ColorTheme;
    /// <summary>
    /// The text theme tint.
    /// </summary>
    public string ColorThemeTint;
    /// <summary>
    /// The text theme shade.
    /// </summary>
    public string ColorThemeShade;
    /// <summary>
    /// The highlight value stored in the document model.
    /// </summary>
    public string Highlight;
    /// <summary>
    /// The size value stored in the document model.
    /// </summary>
    public string Size;
    /// <summary>
    /// The complex-script font size in half-points.
    /// </summary>
    public string SizeComplexScript;
    /// <summary>
    /// The character spacing value in twentieths of a point.
    /// </summary>
    public string CharacterSpacing;
    /// <summary>
    /// The vertical character position in half-points.
    /// </summary>
    public string Position;
    /// <summary>
    /// The minimum font size at which kerning is applied.
    /// </summary>
    public string Kerning;
    /// <summary>
    /// The primary language tag.
    /// </summary>
    public string Language;
    /// <summary>
    /// The East Asian language tag.
    /// </summary>
    public string LanguageEastAsia;
    /// <summary>
    /// The bidirectional language tag.
    /// </summary>
    public string LanguageBidirectional;
    /// <summary>
    /// The borders value stored in the document model.
    /// </summary>
    public DocxBorders Borders;
    /// <summary>
    /// The shading value stored in the document model.
    /// </summary>
    public DocxShading Shading;

    /// <summary>
    /// Creates a run format instance.
    /// </summary>
    public DocxRunFormat()
    {
        this.Xml = "";
        this.HasBold = false;
        this.Bold = false;
        this.HasItalic = false;
        this.Italic = false;
        this.HasStrike = false;
        this.Strike = false;
        this.HasDoubleStrike = false;
        this.DoubleStrike = false;
        this.HasCaps = false;
        this.Caps = false;
        this.HasSmallCaps = false;
        this.SmallCaps = false;
        this.HasHidden = false;
        this.Hidden = false;
        this.HasVanish = false;
        this.Vanish = false;
        this.HasEmboss = false;
        this.Emboss = false;
        this.HasImprint = false;
        this.Imprint = false;
        this.HasOutline = false;
        this.Outline = false;
        this.HasShadow = false;
        this.Shadow = false;
        this.HasRightToLeft = false;
        this.RightToLeft = false;
        this.HasComplexScript = false;
        this.ComplexScript = false;
        this.HasComplexScriptBold = false;
        this.ComplexScriptBold = false;
        this.HasComplexScriptItalic = false;
        this.ComplexScriptItalic = false;
        this.StyleId = "";
        this.FontAscii = "";
        this.FontHighAnsi = "";
        this.FontEastAsia = "";
        this.FontComplexScript = "";
        this.FontAsciiTheme = "";
        this.FontHighAnsiTheme = "";
        this.FontEastAsiaTheme = "";
        this.FontComplexScriptTheme = "";
        this.FontHint = "";
        this.Underline = "";
        this.UnderlineColor = "";
        this.UnderlineThemeColor = "";
        this.UnderlineThemeTint = "";
        this.UnderlineThemeShade = "";
        this.Color = "";
        this.ColorTheme = "";
        this.ColorThemeTint = "";
        this.ColorThemeShade = "";
        this.Highlight = "";
        this.Size = "";
        this.SizeComplexScript = "";
        this.CharacterSpacing = "";
        this.Position = "";
        this.Kerning = "";
        this.Language = "";
        this.LanguageEastAsia = "";
        this.LanguageBidirectional = "";
        this.Borders = new DocxBorders();
        this.Shading = new DocxShading();
    }

    internal static DocxRunFormat Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:rPr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxRunFormat ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxRunFormat format = new DocxRunFormat();
        format.Xml = xml;

        if (xml == "")
        {
            return format;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("rPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return format;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return format;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return format;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1)
            {
                if (reader.IsElement("bdr", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.Borders = DocxBorders.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("shd", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.Shading = DocxShading.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                format.ReadProperty(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }

        return format;
    }

    internal void ReadProperty(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("rStyle", DocxConstants.NamespaceWordprocessingMl))
        {
            this.StyleId = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("rFonts", DocxConstants.NamespaceWordprocessingMl))
        {
            this.FontAscii = reader.GetAttribute("ascii", DocxConstants.NamespaceWordprocessingMl);
            this.FontHighAnsi = reader.GetAttribute("hAnsi", DocxConstants.NamespaceWordprocessingMl);
            this.FontEastAsia = reader.GetAttribute("eastAsia", DocxConstants.NamespaceWordprocessingMl);
            this.FontComplexScript = reader.GetAttribute("cs", DocxConstants.NamespaceWordprocessingMl);
            this.FontAsciiTheme = reader.GetAttribute("asciiTheme", DocxConstants.NamespaceWordprocessingMl);
            this.FontHighAnsiTheme = reader.GetAttribute("hAnsiTheme", DocxConstants.NamespaceWordprocessingMl);
            this.FontEastAsiaTheme = reader.GetAttribute("eastAsiaTheme", DocxConstants.NamespaceWordprocessingMl);
            this.FontComplexScriptTheme = reader.GetAttribute("cstheme", DocxConstants.NamespaceWordprocessingMl);
            this.FontHint = reader.GetAttribute("hint", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("b", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasBold = true;
            this.Bold = ReadToggleValue(reader);
        }
        else if (reader.IsElement("i", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasItalic = true;
            this.Italic = ReadToggleValue(reader);
        }
        else if (reader.IsElement("strike", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasStrike = true;
            this.Strike = ReadToggleValue(reader);
        }
        else if (reader.IsElement("dstrike", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasDoubleStrike = true;
            this.DoubleStrike = ReadToggleValue(reader);
        }
        else if (reader.IsElement("caps", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasCaps = true;
            this.Caps = ReadToggleValue(reader);
        }
        else if (reader.IsElement("smallCaps", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasSmallCaps = true;
            this.SmallCaps = ReadToggleValue(reader);
        }
        else if (reader.IsElement("webHidden", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasHidden = true;
            this.Hidden = ReadToggleValue(reader);
        }
        else if (reader.IsElement("vanish", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasVanish = true;
            this.Vanish = ReadToggleValue(reader);
        }
        else if (reader.IsElement("emboss", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasEmboss = true;
            this.Emboss = ReadToggleValue(reader);
        }
        else if (reader.IsElement("imprint", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasImprint = true;
            this.Imprint = ReadToggleValue(reader);
        }
        else if (reader.IsElement("outline", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasOutline = true;
            this.Outline = ReadToggleValue(reader);
        }
        else if (reader.IsElement("shadow", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasShadow = true;
            this.Shadow = ReadToggleValue(reader);
        }
        else if (reader.IsElement("rtl", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasRightToLeft = true;
            this.RightToLeft = ReadToggleValue(reader);
        }
        else if (reader.IsElement("cs", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasComplexScript = true;
            this.ComplexScript = ReadToggleValue(reader);
        }
        else if (reader.IsElement("bCs", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasComplexScriptBold = true;
            this.ComplexScriptBold = ReadToggleValue(reader);
        }
        else if (reader.IsElement("iCs", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasComplexScriptItalic = true;
            this.ComplexScriptItalic = ReadToggleValue(reader);
        }
        else if (reader.IsElement("u", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Underline = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.UnderlineColor = reader.GetAttribute("color", DocxConstants.NamespaceWordprocessingMl);
            this.UnderlineThemeColor = reader.GetAttribute("themeColor", DocxConstants.NamespaceWordprocessingMl);
            this.UnderlineThemeTint = reader.GetAttribute("themeTint", DocxConstants.NamespaceWordprocessingMl);
            this.UnderlineThemeShade = reader.GetAttribute("themeShade", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("color", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Color = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.ColorTheme = reader.GetAttribute("themeColor", DocxConstants.NamespaceWordprocessingMl);
            this.ColorThemeTint = reader.GetAttribute("themeTint", DocxConstants.NamespaceWordprocessingMl);
            this.ColorThemeShade = reader.GetAttribute("themeShade", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("highlight", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Highlight = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("sz", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Size = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("szCs", DocxConstants.NamespaceWordprocessingMl))
        {
            this.SizeComplexScript = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("spacing", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CharacterSpacing = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("position", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Position = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("kern", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kerning = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("lang", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Language = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.LanguageEastAsia = reader.GetAttribute("eastAsia", DocxConstants.NamespaceWordprocessingMl);
            this.LanguageBidirectional = reader.GetAttribute("bidi", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal static bool ReadToggleValue(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);

        if (value == "")
        {
            return true;
        }

        return DocxXmlAttributes.ParseBool(value);
    }

    internal bool HasAnyFormatting()
    {
        return this.HasBold
            || this.HasItalic
            || this.HasStrike
            || this.HasDoubleStrike
            || this.HasCaps
            || this.HasSmallCaps
            || this.HasHidden
            || this.HasVanish
            || this.HasEmboss
            || this.HasImprint
            || this.HasOutline
            || this.HasShadow
            || this.HasRightToLeft
            || this.HasComplexScript
            || this.HasComplexScriptBold
            || this.HasComplexScriptItalic
            || this.StyleId != ""
            || this.FontAscii != ""
            || this.FontHighAnsi != ""
            || this.FontEastAsia != ""
            || this.FontComplexScript != ""
            || this.FontAsciiTheme != ""
            || this.FontHighAnsiTheme != ""
            || this.FontEastAsiaTheme != ""
            || this.FontComplexScriptTheme != ""
            || this.FontHint != ""
            || this.Underline != ""
            || this.UnderlineColor != ""
            || this.UnderlineThemeColor != ""
            || this.UnderlineThemeTint != ""
            || this.UnderlineThemeShade != ""
            || this.Color != ""
            || this.ColorTheme != ""
            || this.ColorThemeTint != ""
            || this.ColorThemeShade != ""
            || this.Highlight != ""
            || this.Size != ""
            || this.SizeComplexScript != ""
            || this.CharacterSpacing != ""
            || this.Position != ""
            || this.Kerning != ""
            || this.Language != ""
            || this.LanguageEastAsia != ""
            || this.LanguageBidirectional != ""
            || this.Borders.HasAnyBorder()
            || this.Shading.HasAnyShading();
    }

    internal void SetBold(bool value)
    {
        this.HasBold = true;
        this.Bold = value;
    }

    internal void ClearBold()
    {
        this.HasBold = false;
        this.Bold = false;
    }

    internal void SetItalic(bool value)
    {
        this.HasItalic = true;
        this.Italic = value;
    }

    internal void ClearItalic()
    {
        this.HasItalic = false;
        this.Italic = false;
    }

    internal void SetStrike(bool value)
    {
        this.HasStrike = true;
        this.Strike = value;
    }

    internal void ClearStrike()
    {
        this.HasStrike = false;
        this.Strike = false;
    }

    internal void SetStyleId(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.StyleId = value;
    }

    internal void SetFonts(string ascii, string highAnsi, string eastAsia, string complexScript)
    {
        if (ascii == null)
        {
            throw new ArgumentNullException("ascii");
        }

        if (highAnsi == null)
        {
            throw new ArgumentNullException("highAnsi");
        }

        if (eastAsia == null)
        {
            throw new ArgumentNullException("eastAsia");
        }

        if (complexScript == null)
        {
            throw new ArgumentNullException("complexScript");
        }

        this.FontAscii = ascii;
        this.FontHighAnsi = highAnsi;
        this.FontEastAsia = eastAsia;
        this.FontComplexScript = complexScript;
    }

    internal void SetUnderline(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Underline = value;
    }

    internal void SetColor(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            this.Color = "";
            return;
        }

        this.Color = DocxXmlAttributes.NormalizeColor(value);
    }

    internal void SetHighlight(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Highlight = value;
    }

    internal void SetSize(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Size = value;
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

        writer.StartElement("w", "rPr", DocxConstants.NamespaceWordprocessingMl);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteStringProperty(writer, "rStyle", "val", this.StyleId);
        WriteFontsTo(writer);
        WriteToggleProperty(writer, "b", this.HasBold, this.Bold);
        WriteToggleProperty(writer, "bCs", this.HasComplexScriptBold, this.ComplexScriptBold);
        WriteToggleProperty(writer, "i", this.HasItalic, this.Italic);
        WriteToggleProperty(writer, "iCs", this.HasComplexScriptItalic, this.ComplexScriptItalic);
        WriteToggleProperty(writer, "caps", this.HasCaps, this.Caps);
        WriteToggleProperty(writer, "smallCaps", this.HasSmallCaps, this.SmallCaps);
        WriteToggleProperty(writer, "strike", this.HasStrike, this.Strike);
        WriteToggleProperty(writer, "dstrike", this.HasDoubleStrike, this.DoubleStrike);
        WriteToggleProperty(writer, "outline", this.HasOutline, this.Outline);
        WriteToggleProperty(writer, "shadow", this.HasShadow, this.Shadow);
        WriteToggleProperty(writer, "emboss", this.HasEmboss, this.Emboss);
        WriteToggleProperty(writer, "imprint", this.HasImprint, this.Imprint);
        WriteToggleProperty(writer, "vanish", this.HasVanish, this.Vanish);
        WriteToggleProperty(writer, "webHidden", this.HasHidden, this.Hidden);
        WriteColorTo(writer);
        WriteStringProperty(writer, "spacing", "val", this.CharacterSpacing);
        WriteStringProperty(writer, "kern", "val", this.Kerning);
        WriteStringProperty(writer, "position", "val", this.Position);
        WriteStringProperty(writer, "sz", "val", this.Size);
        WriteStringProperty(writer, "szCs", "val", this.SizeComplexScript);
        WriteStringProperty(writer, "highlight", "val", this.Highlight);
        WriteUnderlineTo(writer);
        WriteBordersTo(writer);
        WriteShadingTo(writer);
        WriteToggleProperty(writer, "rtl", this.HasRightToLeft, this.RightToLeft);
        WriteToggleProperty(writer, "cs", this.HasComplexScript, this.ComplexScript);
        WriteLanguageTo(writer);
    }

    internal void WriteStringProperty(DocxXmlWriter writer, string elementName, string attributeName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", attributeName, DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal void WriteToggleProperty(DocxXmlWriter writer, string elementName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (!hasValue)
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);

        if (!value)
        {
            writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, "0");
        }

        writer.EndElement();
    }

    internal void WriteFontsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.FontAscii == ""
            && this.FontHighAnsi == ""
            && this.FontEastAsia == ""
            && this.FontComplexScript == ""
            && this.FontAsciiTheme == ""
            && this.FontHighAnsiTheme == ""
            && this.FontEastAsiaTheme == ""
            && this.FontComplexScriptTheme == ""
            && this.FontHint == "")
        {
            return;
        }

        writer.StartElement("w", "rFonts", DocxConstants.NamespaceWordprocessingMl);

        if (this.FontAscii != "")
        {
            writer.WriteAttribute("w", "ascii", DocxConstants.NamespaceWordprocessingMl, this.FontAscii);
        }

        if (this.FontHighAnsi != "")
        {
            writer.WriteAttribute("w", "hAnsi", DocxConstants.NamespaceWordprocessingMl, this.FontHighAnsi);
        }

        if (this.FontEastAsia != "")
        {
            writer.WriteAttribute("w", "eastAsia", DocxConstants.NamespaceWordprocessingMl, this.FontEastAsia);
        }

        if (this.FontComplexScript != "")
        {
            writer.WriteAttribute("w", "cs", DocxConstants.NamespaceWordprocessingMl, this.FontComplexScript);
        }

        if (this.FontAsciiTheme != "")
        {
            writer.WriteAttribute("w", "asciiTheme", DocxConstants.NamespaceWordprocessingMl, this.FontAsciiTheme);
        }

        if (this.FontHighAnsiTheme != "")
        {
            writer.WriteAttribute("w", "hAnsiTheme", DocxConstants.NamespaceWordprocessingMl, this.FontHighAnsiTheme);
        }

        if (this.FontEastAsiaTheme != "")
        {
            writer.WriteAttribute("w", "eastAsiaTheme", DocxConstants.NamespaceWordprocessingMl, this.FontEastAsiaTheme);
        }

        if (this.FontComplexScriptTheme != "")
        {
            writer.WriteAttribute("w", "cstheme", DocxConstants.NamespaceWordprocessingMl, this.FontComplexScriptTheme);
        }

        if (this.FontHint != "")
        {
            writer.WriteAttribute("w", "hint", DocxConstants.NamespaceWordprocessingMl, this.FontHint);
        }

        writer.EndElement();
    }

    internal void WriteColorTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Color == "" && this.ColorTheme == "" && this.ColorThemeTint == "" && this.ColorThemeShade == "")
        {
            return;
        }

        writer.StartElement("w", "color", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.Color);
        WriteOptionalAttribute(writer, "themeColor", this.ColorTheme);
        WriteOptionalAttribute(writer, "themeTint", this.ColorThemeTint);
        WriteOptionalAttribute(writer, "themeShade", this.ColorThemeShade);
        writer.EndElement();
    }

    internal void WriteUnderlineTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Underline == ""
            && this.UnderlineColor == ""
            && this.UnderlineThemeColor == ""
            && this.UnderlineThemeTint == ""
            && this.UnderlineThemeShade == "")
        {
            return;
        }

        writer.StartElement("w", "u", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.Underline);
        WriteOptionalAttribute(writer, "color", this.UnderlineColor);
        WriteOptionalAttribute(writer, "themeColor", this.UnderlineThemeColor);
        WriteOptionalAttribute(writer, "themeTint", this.UnderlineThemeTint);
        WriteOptionalAttribute(writer, "themeShade", this.UnderlineThemeShade);
        writer.EndElement();
    }

    internal void WriteLanguageTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Language == "" && this.LanguageEastAsia == "" && this.LanguageBidirectional == "")
        {
            return;
        }

        writer.StartElement("w", "lang", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.Language);
        WriteOptionalAttribute(writer, "eastAsia", this.LanguageEastAsia);
        WriteOptionalAttribute(writer, "bidi", this.LanguageBidirectional);
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

    internal void WriteBordersTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Borders.HasAnyBorder())
        {
            this.Borders.WriteTo(writer, "bdr");
        }
    }

    internal void WriteShadingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Shading.HasAnyShading())
        {
            this.Shading.WriteTo(writer);
        }
    }
}
