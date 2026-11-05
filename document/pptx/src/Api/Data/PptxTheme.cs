using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents theme data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTheme
{
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the color scheme name associated with this object.
    /// </summary>
    public string ColorSchemeName;
    /// <summary>
    /// Stores the font scheme name associated with this object.
    /// </summary>
    public string FontSchemeName;
    /// <summary>
    /// Stores the format scheme name associated with this object.
    /// </summary>
    public string FormatSchemeName;
    /// <summary>
    /// Stores the major latin font associated with this object.
    /// </summary>
    public string MajorLatinFont;
    /// <summary>
    /// Stores the minor latin font associated with this object.
    /// </summary>
    public string MinorLatinFont;
    /// <summary>
    /// Contains the colors in package or presentation order.
    /// </summary>
    public Dictionary<string, PptxColor> Colors;
    /// <summary>
    /// Contains the fill styles in package or presentation order.
    /// </summary>
    public List<PptxFill> FillStyles;
    /// <summary>
    /// Contains the line styles in package or presentation order.
    /// </summary>
    public List<PptxLine> LineStyles;
    /// <summary>
    /// Contains the effect styles in package or presentation order.
    /// </summary>
    public List<PptxEffect> EffectStyles;
    /// <summary>
    /// Contains the background fill styles in package or presentation order.
    /// </summary>
    public List<PptxFill> BackgroundFillStyles;

    internal PptxTheme(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Name = "";
        ColorSchemeName = "";
        FontSchemeName = "";
        FormatSchemeName = "";
        MajorLatinFont = "";
        MinorLatinFont = "";
        Colors = new Dictionary<string, PptxColor>();
        FillStyles = new List<PptxFill>();
        LineStyles = new List<PptxLine>();
        EffectStyles = new List<PptxEffect>();
        BackgroundFillStyles = new List<PptxFill>();
        Load();
    }

    internal void Load()
    {
        Colors.Clear();
        FillStyles.Clear();
        LineStyles.Clear();
        EffectStyles.Clear();
        BackgroundFillStyles.Clear();

        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        Name = PptxXml.GetAttributeValue(root, "", "name");
        XmlElement? themeElements = PptxXml.FindFirstChild(root, PptxNamespaces.Drawing, "themeElements");
        if (themeElements == null)
        {
            return;
        }

        LoadColorScheme(themeElements);
        LoadFontScheme(themeElements);
        LoadFormatScheme(themeElements);
    }

    internal XmlDocument GetXmlDocument()
    {
        if (Part.HasXmlDocument)
        {
            return Part.XmlDocument;
        }

        return PptxXml.LoadBytes(Part.GetBytes());
    }
    internal PptxColor GetColor(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        if (Colors.ContainsKey(clean))
        {
            return Colors[clean];
        }

        return new PptxColor();
    }
    internal string GetColorValue(string name)
    {
        return ResolveColor(PptxColor.CreateScheme(name)).Value;
    }
    internal void SetColor(string name, PptxColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        string clean = PptxDiagnostic.CleanText(name);
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.InvalidOperationException("The theme XML root is required.");
        }

        XmlElement? themeElements = PptxXml.FindFirstChild(root, PptxNamespaces.Drawing, "themeElements");
        if (themeElements == null)
        {
            throw new System.InvalidOperationException("The theme elements are required.");
        }

        XmlElement? colorScheme = PptxXml.FindFirstChild(themeElements, PptxNamespaces.Drawing, "clrScheme");
        if (colorScheme == null)
        {
            throw new System.InvalidOperationException("The theme color scheme is required.");
        }

        XmlElement? slot = PptxXml.FindFirstChild(colorScheme, PptxNamespaces.Drawing, clean);
        if (slot == null)
        {
            throw new System.ArgumentException("The requested theme color slot does not exist.", "name");
        }

        for (int index = slot.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlElement? existing = slot.ChildNodes[index] as XmlElement;
            if (existing != null && PptxColor.IsColorElement(existing))
            {
                slot.RemoveChild(existing);
            }
        }

        slot.AppendChild(color.ToElement(document));
        Part.SetXmlDocument(document);
        Load();
    }
    internal PptxColor ResolveColor(PptxColor color)
    {
        return PptxStyleResolverCore.ResolveColor(this, color);
    }
    internal string GetFont(string role)
    {
        string clean = PptxDiagnostic.CleanText(role);
        if (clean == "major")
        {
            return MajorLatinFont;
        }

        if (clean == "minor")
        {
            return MinorLatinFont;
        }

        return "";
    }
    internal int FillStyleCount()
    {
        return FillStyles.Count;
    }
    internal int LineStyleCount()
    {
        return LineStyles.Count;
    }
    internal int EffectStyleCount()
    {
        return EffectStyles.Count;
    }
    internal int BackgroundFillStyleCount()
    {
        return BackgroundFillStyles.Count;
    }
    internal PptxFill GetFillStyle(int index)
    {
        ValidateIndex(index, FillStyles.Count);
        return FillStyles[index];
    }
    internal PptxLine GetLineStyle(int index)
    {
        ValidateIndex(index, LineStyles.Count);
        return LineStyles[index];
    }
    internal PptxEffect GetEffectStyle(int index)
    {
        ValidateIndex(index, EffectStyles.Count);
        return EffectStyles[index];
    }
    internal PptxFill GetBackgroundFillStyle(int index)
    {
        ValidateIndex(index, BackgroundFillStyles.Count);
        return BackgroundFillStyles[index];
    }

    internal void LoadColorScheme(XmlElement themeElements)
    {
        XmlElement? colorScheme = PptxXml.FindFirstChild(themeElements, PptxNamespaces.Drawing, "clrScheme");
        if (colorScheme == null)
        {
            return;
        }

        ColorSchemeName = PptxXml.GetAttributeValue(colorScheme, "", "name");
        for (int index = 0; index < colorScheme.ChildNodes.Count; index++)
        {
            XmlNode? child = colorScheme.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing)
            {
                Colors[element.LocalName] = LoadThemeColor(element);
            }
        }
    }

    internal PptxColor LoadThemeColor(XmlElement colorSlot)
    {
        return PptxColor.FromParent(colorSlot);
    }

    internal void LoadFontScheme(XmlElement themeElements)
    {
        XmlElement? fontScheme = PptxXml.FindFirstChild(themeElements, PptxNamespaces.Drawing, "fontScheme");
        if (fontScheme == null)
        {
            return;
        }

        FontSchemeName = PptxXml.GetAttributeValue(fontScheme, "", "name");
        MajorLatinFont = LoadLatinFont(fontScheme, "majorFont");
        MinorLatinFont = LoadLatinFont(fontScheme, "minorFont");
    }

    internal string LoadLatinFont(XmlElement fontScheme, string localName)
    {
        XmlElement? fontGroup = PptxXml.FindFirstChild(fontScheme, PptxNamespaces.Drawing, localName);
        if (fontGroup == null)
        {
            return "";
        }

        XmlElement? latin = PptxXml.FindFirstChild(fontGroup, PptxNamespaces.Drawing, "latin");
        if (latin == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(latin, "", "typeface");
    }

    internal void LoadFormatScheme(XmlElement themeElements)
    {
        XmlElement? formatScheme = PptxXml.FindFirstChild(themeElements, PptxNamespaces.Drawing, "fmtScheme");
        if (formatScheme == null)
        {
            return;
        }

        FormatSchemeName = PptxXml.GetAttributeValue(formatScheme, "", "name");
        LoadFillStyleList(formatScheme, "fillStyleLst", FillStyles);
        LoadLineStyleList(formatScheme);
        LoadEffectStyleList(formatScheme);
        LoadFillStyleList(formatScheme, "bgFillStyleLst", BackgroundFillStyles);
    }

    internal void LoadFillStyleList(XmlElement formatScheme, string localName, List<PptxFill> fills)
    {
        XmlElement? list = PptxXml.FindFirstChild(formatScheme, PptxNamespaces.Drawing, localName);
        if (list == null)
        {
            return;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(element.LocalName))
            {
                fills.Add(PptxFill.FromElement(element));
            }
        }
    }

    internal void LoadLineStyleList(XmlElement formatScheme)
    {
        XmlElement? list = PptxXml.FindFirstChild(formatScheme, PptxNamespaces.Drawing, "lnStyleLst");
        if (list == null)
        {
            return;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "ln")
            {
                LineStyles.Add(PptxLine.FromElement(element));
            }
        }
    }

    internal void LoadEffectStyleList(XmlElement formatScheme)
    {
        XmlElement? list = PptxXml.FindFirstChild(formatScheme, PptxNamespaces.Drawing, "effectStyleLst");
        if (list == null)
        {
            return;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "effectStyle")
            {
                EffectStyles.Add(PptxEffect.FromShapeProperties(element));
            }
        }
    }

    internal void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
