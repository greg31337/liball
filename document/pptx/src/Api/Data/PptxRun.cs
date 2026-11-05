using System.Xml;

namespace pptx;

/// <summary>
/// Represents run data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxRun
{
    /// <summary>
    /// Identifies the text run value accepted by the API.
    /// </summary>
    public const string TextRun = "text";
    /// <summary>
    /// Identifies the field value accepted by the API.
    /// </summary>
    public const string Field = "field";
    /// <summary>
    /// Identifies the line break value accepted by the API.
    /// </summary>
    public const string LineBreak = "lineBreak";
    /// <summary>
    /// Identifies the tab value accepted by the API.
    /// </summary>
    public const string Tab = "tab";

    internal PptxParagraph Paragraph;
    internal XmlElement Element;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the text associated with this object.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the field id used to identify or connect related presentation data.
    /// </summary>
    public string FieldId;
    /// <summary>
    /// Stores the field type associated with this object.
    /// </summary>
    public string FieldType;
    /// <summary>
    /// Stores the hyperlink relationship id used to identify or connect related presentation data.
    /// </summary>
    public string HyperlinkRelationshipId;
    /// <summary>
    /// Stores the hyperlink target associated with this object.
    /// </summary>
    public string HyperlinkTarget;
    /// <summary>
    /// Stores the font value associated with this object.
    /// </summary>
    public PptxFont Font;
    internal string Xml;
    internal string PropertiesXml;
    internal PptxRun(PptxParagraph paragraph, XmlElement element)
    {
        if (paragraph == null)
        {
            throw new System.ArgumentNullException("paragraph");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Paragraph = paragraph;
        Element = element;
        Type = TextRun;
        Text = "";
        FieldId = "";
        FieldType = "";
        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        Font = new PptxFont();
        Xml = element.OuterXml;
        PropertiesXml = "";
        Load();
    }
    internal bool IsText()
    {
        return Type == TextRun;
    }
    internal bool IsField()
    {
        return Type == Field;
    }
    internal bool IsLineBreak()
    {
        return Type == LineBreak;
    }
    internal bool IsTab()
    {
        return Type == Tab;
    }

    internal void Load()
    {
        Type = TextRun;
        Text = "";
        FieldId = "";
        FieldType = "";
        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        Font = new PptxFont();
        Xml = Element.OuterXml;
        PropertiesXml = "";
        if (Element.NamespaceURI != PptxNamespaces.Drawing)
        {
            return;
        }

        if (Element.LocalName == "r")
        {
            Type = TextRun;
            Text = ReadTextElement(Element);
            LoadRunProperties(PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "rPr"));
            return;
        }

        if (Element.LocalName == "fld")
        {
            Type = Field;
            FieldId = PptxXml.GetAttributeValue(Element, "", "id");
            FieldType = PptxXml.GetAttributeValue(Element, "", "type");
            Text = ReadTextElement(Element);
            LoadRunProperties(PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "rPr"));
            return;
        }

        if (Element.LocalName == "br")
        {
            Type = LineBreak;
            Text = "\n";
            LoadRunProperties(PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "rPr"));
            return;
        }

        if (Element.LocalName == "tab")
        {
            Type = Tab;
            Text = "\t";
        }
    }

    internal string ReadTextElement(XmlElement parent)
    {
        XmlElement? text = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "t");
        if (text == null)
        {
            return "";
        }

        return text.InnerText;
    }
    internal void SetText(string text)
    {
        if (Type == LineBreak || Type == Tab)
        {
            throw new System.ArgumentException("Line breaks and tabs do not contain editable text.");
        }

        XmlElement value = GetOrCreateTextElement();
        value.InnerText = PptxRunFormatCore.NormalizeTextValue(text);
        Paragraph.TextFrame.Shape.Tree.MarkDirty();
        Paragraph.TextFrame.Load();
    }
    internal int ReplaceText(string search, string replacement)
    {
        if (Type == LineBreak || Type == Tab)
        {
            return 0;
        }

        XmlElement value = GetOrCreateTextElement();
        int count = PptxTextReplacerCore.ReplacePlainTextElement(value, PptxDiagnostic.CleanText(search), PptxDiagnostic.CleanText(replacement));
        if (count > 0)
        {
            Paragraph.TextFrame.Shape.Tree.MarkDirty();
            Paragraph.TextFrame.Load();
        }

        return count;
    }

    internal XmlElement GetOrCreateTextElement()
    {
        XmlElement? value = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "t");
        if (value != null)
        {
            return value;
        }

        XmlElement created = PptxTextFrame.GetOwnerDocument(Element).CreateElement("a", "t", PptxNamespaces.Drawing);
        Element.AppendChild(created);
        return created;
    }

    internal void LoadRunProperties(XmlElement? properties)
    {
        if (properties == null)
        {
            return;
        }

        PropertiesXml = properties.OuterXml;
        Font.Language = PptxXml.GetAttributeValue(properties, "", "lang");
        Font.AlternativeLanguage = PptxXml.GetAttributeValue(properties, "", "altLang");
        int size = PptxXml.GetIntAttribute(properties, "", "sz", 0);
        if (size > 0)
        {
            Font.SizePoints = (double)size / 100.0;
        }

        Font.Bold = PptxXml.GetBoolAttribute(properties, "", "b", false);
        Font.Italic = PptxXml.GetBoolAttribute(properties, "", "i", false);
        Font.UnderlineStyle = PptxXml.GetAttributeValue(properties, "", "u");
        Font.StrikeStyle = PptxXml.GetAttributeValue(properties, "", "strike");
        Font.Underline = IsEnabledTextDecoration(Font.UnderlineStyle, "none");
        Font.Strike = IsEnabledTextDecoration(Font.StrikeStyle, "noStrike");
        Font.Baseline = PptxXml.GetIntAttribute(properties, "", "baseline", 0);
        Font.Capitalization = PptxXml.GetAttributeValue(properties, "", "cap");
        Font.Kerning = PptxXml.GetIntAttribute(properties, "", "kern", 0);
        Font.Spacing = PptxXml.GetIntAttribute(properties, "", "spc", 0);
        LoadTypeface(properties);
        LoadFillColor(properties);
        LoadHighlight(properties);
        LoadHyperlink(properties);
    }

    internal bool IsEnabledTextDecoration(string value, string disabledValue)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value == disabledValue)
        {
            return false;
        }

        return true;
    }

    internal void LoadTypeface(XmlElement properties)
    {
        XmlElement? latin = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "latin");
        if (latin != null)
        {
            Font.Family = PptxXml.GetAttributeValue(latin, "", "typeface");
        }

        XmlElement? eastAsian = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "ea");
        if (eastAsian != null)
        {
            Font.EastAsianFamily = PptxXml.GetAttributeValue(eastAsian, "", "typeface");
        }

        XmlElement? complexScript = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "cs");
        if (complexScript != null)
        {
            Font.ComplexScriptFamily = PptxXml.GetAttributeValue(complexScript, "", "typeface");
        }

        XmlElement? symbol = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "sym");
        if (symbol != null)
        {
            Font.SymbolFamily = PptxXml.GetAttributeValue(symbol, "", "typeface");
        }
    }

    internal void LoadFillColor(XmlElement properties)
    {
        Font.Fill = PptxFill.FromShapeProperties(properties);
        if (Font.Fill.Type == PptxFill.Solid)
        {
            Font.FillColor = Font.Fill.Color;
        }
    }

    internal void LoadHighlight(XmlElement properties)
    {
        XmlElement? highlight = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "highlight");
        if (highlight != null)
        {
            Font.HighlightColor = PptxColor.FromParent(highlight);
        }
    }

    internal void LoadHyperlink(XmlElement properties)
    {
        XmlElement? hyperlink = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
        if (hyperlink == null)
        {
            return;
        }

        HyperlinkRelationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        PptxRelationship? relationship = Paragraph.TextFrame.Shape.Tree.FindRelationshipById(HyperlinkRelationshipId);
        if (relationship != null)
        {
            HyperlinkTarget = relationship.Target;
        }
    }
    internal PptxHyperlink GetHyperlink()
    {
        return new PptxHyperlink(null, this);
    }
    internal XmlElement? GetHyperlinkClickElement()
    {
        XmlElement? properties = PptxRunFormatCore.GetProperties(this);
        if (properties == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
    }
}
