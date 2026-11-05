namespace docx;

internal class DocxThemeResolver
{
    internal Dictionary<string, string> Colors;
    internal Dictionary<string, string> Fonts;

    internal DocxThemeResolver(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Colors = new Dictionary<string, string>();
        this.Fonts = new Dictionary<string, string>();

        if (xml != "")
        {
            Load(xml);
        }
    }

    internal void Load(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(xml);
        System.Xml.XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            return;
        }

        System.Xml.XmlElement? colorScheme = FindDescendant(root, "clrScheme");

        if (colorScheme != null)
        {
            LoadColors(colorScheme);
        }

        System.Xml.XmlElement? fontScheme = FindDescendant(root, "fontScheme");

        if (fontScheme != null)
        {
            LoadFonts(fontScheme);
        }
    }

    internal void LoadColors(System.Xml.XmlElement colorScheme)
    {
        if (colorScheme == null)
        {
            throw new ArgumentNullException("colorScheme");
        }

        foreach (System.Xml.XmlNode node in colorScheme.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement colorElement = (System.Xml.XmlElement)node;
                string value = ReadColorValue(colorElement);

                if (value != "")
                {
                    this.Colors[colorElement.LocalName] = value;
                }
            }
        }
    }

    internal string ReadColorValue(System.Xml.XmlElement colorElement)
    {
        if (colorElement == null)
        {
            throw new ArgumentNullException("colorElement");
        }

        foreach (System.Xml.XmlNode node in colorElement.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement valueElement = (System.Xml.XmlElement)node;

                if (valueElement.LocalName == "srgbClr")
                {
                    return valueElement.GetAttribute("val");
                }

                if (valueElement.LocalName == "sysClr")
                {
                    string value = valueElement.GetAttribute("lastClr");

                    if (value == "")
                    {
                        value = valueElement.GetAttribute("val");
                    }

                    return value;
                }
            }
        }

        return "";
    }

    internal void LoadFonts(System.Xml.XmlElement fontScheme)
    {
        if (fontScheme == null)
        {
            throw new ArgumentNullException("fontScheme");
        }

        System.Xml.XmlElement? major = FindDirectChild(fontScheme, "majorFont");
        System.Xml.XmlElement? minor = FindDirectChild(fontScheme, "minorFont");

        if (major != null)
        {
            LoadFontSet(major, "major");
        }

        if (minor != null)
        {
            LoadFontSet(minor, "minor");
        }
    }

    internal void LoadFontSet(System.Xml.XmlElement fontSet, string prefix)
    {
        if (fontSet == null)
        {
            throw new ArgumentNullException("fontSet");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        string latin = ReadTypeface(fontSet, "latin");
        string eastAsia = ReadTypeface(fontSet, "ea");
        string complexScript = ReadTypeface(fontSet, "cs");

        this.Fonts[prefix + "Ascii"] = latin;
        this.Fonts[prefix + "HAnsi"] = latin;
        this.Fonts[prefix + "EastAsia"] = eastAsia == "" ? latin : eastAsia;
        this.Fonts[prefix + "Bidi"] = complexScript == "" ? latin : complexScript;
    }

    internal string ReadTypeface(System.Xml.XmlElement fontSet, string localName)
    {
        if (fontSet == null)
        {
            throw new ArgumentNullException("fontSet");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        System.Xml.XmlElement? element = FindDirectChild(fontSet, localName);

        if (element == null)
        {
            return "";
        }

        return element.GetAttribute("typeface");
    }

    internal string ResolveColor(string themeColor, string tint, string shade)
    {
        if (themeColor == null)
        {
            throw new ArgumentNullException("themeColor");
        }

        if (tint == null)
        {
            throw new ArgumentNullException("tint");
        }

        if (shade == null)
        {
            throw new ArgumentNullException("shade");
        }

        string key = NormalizeColorKey(themeColor);

        if (!this.Colors.ContainsKey(key))
        {
            return "";
        }

        return ApplyColorModifiers(this.Colors[key], tint, shade);
    }

    internal string ResolveFont(string themeFont)
    {
        if (themeFont == null)
        {
            throw new ArgumentNullException("themeFont");
        }

        if (this.Fonts.ContainsKey(themeFont))
        {
            return this.Fonts[themeFont];
        }

        return "";
    }

    internal void ApplyRun(DocxRunFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        format.FontAscii = ResolveFontOrFallback(format.FontAsciiTheme, format.FontAscii);
        format.FontHighAnsi = ResolveFontOrFallback(format.FontHighAnsiTheme, format.FontHighAnsi);
        format.FontEastAsia = ResolveFontOrFallback(format.FontEastAsiaTheme, format.FontEastAsia);
        format.FontComplexScript = ResolveFontOrFallback(format.FontComplexScriptTheme, format.FontComplexScript);
        format.Color = ResolveColorOrFallback(format.ColorTheme, format.ColorThemeTint, format.ColorThemeShade, format.Color);
        format.UnderlineColor = ResolveColorOrFallback(format.UnderlineThemeColor, format.UnderlineThemeTint, format.UnderlineThemeShade, format.UnderlineColor);
        ApplyShading(format.Shading);
    }

    internal void ApplyParagraph(DocxParagraphFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        ApplyShading(format.Shading);
    }

    internal void ApplyTable(DocxTableFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        ApplyShading(format.Shading);
    }

    internal void ApplyCell(DocxTableCellFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        ApplyShading(format.Shading);
    }

    internal void ApplyShading(DocxShading shading)
    {
        if (shading == null)
        {
            throw new ArgumentNullException("shading");
        }

        shading.Color = ResolveColorOrFallback(shading.ThemeColor, shading.ThemeTint, shading.ThemeShade, shading.Color);
        shading.Fill = ResolveColorOrFallback(shading.ThemeFill, shading.ThemeFillTint, shading.ThemeFillShade, shading.Fill);
    }

    internal string ResolveFontOrFallback(string themeFont, string fallback)
    {
        if (themeFont == null)
        {
            throw new ArgumentNullException("themeFont");
        }

        if (fallback == null)
        {
            throw new ArgumentNullException("fallback");
        }

        string resolved = ResolveFont(themeFont);
        return resolved == "" ? fallback : resolved;
    }

    internal string ResolveColorOrFallback(string themeColor, string tint, string shade, string fallback)
    {
        if (fallback == null)
        {
            throw new ArgumentNullException("fallback");
        }

        string resolved = ResolveColor(themeColor, tint, shade);
        return resolved == "" ? fallback : resolved;
    }

    internal static string NormalizeColorKey(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "dark1" || value == "text1")
        {
            return "dk1";
        }

        if (value == "light1" || value == "background1")
        {
            return "lt1";
        }

        if (value == "dark2" || value == "text2")
        {
            return "dk2";
        }

        if (value == "light2" || value == "background2")
        {
            return "lt2";
        }

        if (value == "hyperlink")
        {
            return "hlink";
        }

        if (value == "followedHyperlink")
        {
            return "folHlink";
        }

        return value;
    }

    internal static string ApplyColorModifiers(string color, string tint, string shade)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (tint == null)
        {
            throw new ArgumentNullException("tint");
        }

        if (shade == null)
        {
            throw new ArgumentNullException("shade");
        }

        if (color.Length != 6)
        {
            return color;
        }

        int red = ReadHexByte(color, 0);
        int green = ReadHexByte(color, 2);
        int blue = ReadHexByte(color, 4);

        if (red < 0 || green < 0 || blue < 0)
        {
            return color;
        }

        int shadeValue = ReadModifier(shade);

        if (shadeValue >= 0)
        {
            red = red * shadeValue / 255;
            green = green * shadeValue / 255;
            blue = blue * shadeValue / 255;
        }

        int tintValue = ReadModifier(tint);

        if (tintValue >= 0)
        {
            red = red + (255 - red) * tintValue / 255;
            green = green + (255 - green) * tintValue / 255;
            blue = blue + (255 - blue) * tintValue / 255;
        }

        return red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
    }

    internal static int ReadModifier(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length != 2)
        {
            return -1;
        }

        return ReadHexByte(value, 0);
    }

    internal static int ReadHexByte(string value, int index)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (index < 0 || index + 1 >= value.Length)
        {
            return -1;
        }

        int high = ReadHexDigit(value[index]);
        int low = ReadHexDigit(value[index + 1]);

        if (high < 0 || low < 0)
        {
            return -1;
        }

        return high * 16 + low;
    }

    internal static int ReadHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static System.Xml.XmlElement? FindDescendant(System.Xml.XmlElement root, string localName)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (root.LocalName == localName)
        {
            return root;
        }

        foreach (System.Xml.XmlNode node in root.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement? result = FindDescendant((System.Xml.XmlElement)node, localName);

                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    internal static System.Xml.XmlElement? FindDirectChild(System.Xml.XmlElement root, string localName)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        foreach (System.Xml.XmlNode node in root.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element && node.LocalName == localName)
            {
                return (System.Xml.XmlElement)node;
            }
        }

        return null;
    }
}
