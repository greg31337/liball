using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents paragraph data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxParagraph
{
    /// <summary>
    /// Stores the text frame value associated with this object.
    /// </summary>
    public PptxTextFrame TextFrame;
    internal XmlElement Element;
    /// <summary>
    /// Contains the runs in package or presentation order.
    /// </summary>
    public List<PptxRun> Runs;
    /// <summary>
    /// Stores the alignment associated with this object.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// Stores the level value associated with this object.
    /// </summary>
    public int Level;
    /// <summary>
    /// Indicates whether the has bullet setting is enabled.
    /// </summary>
    public bool HasBullet;
    /// <summary>
    /// Stores the bullet type associated with this object.
    /// </summary>
    public string BulletType;
    /// <summary>
    /// Stores the bullet character associated with this object.
    /// </summary>
    public string BulletCharacter;
    /// <summary>
    /// Stores the numbering type associated with this object.
    /// </summary>
    public string NumberingType;
    /// <summary>
    /// Stores the numbering start at value associated with this object.
    /// </summary>
    public int NumberingStartAt;
    internal string Xml;
    internal string PropertiesXml;
    internal PptxParagraph(PptxTextFrame textFrame, XmlElement element)
    {
        if (textFrame == null)
        {
            throw new System.ArgumentNullException("textFrame");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        TextFrame = textFrame;
        Element = element;
        Runs = new List<PptxRun>();
        Alignment = "";
        Level = 0;
        HasBullet = false;
        BulletType = PptxBullet.None;
        BulletCharacter = "";
        NumberingType = "";
        NumberingStartAt = 0;
        Xml = element.OuterXml;
        PropertiesXml = "";
        Load();
    }
    internal int RunCount()
    {
        return Runs.Count;
    }
    internal PptxRun GetRun(int index)
    {
        return Runs[index];
    }
    internal string GetText()
    {
        return PptxTextFrame.GetTextBodyText(CreateTemporaryTextBody());
    }
    internal void SetText(string text)
    {
        PptxTextFrame.RemoveRunChildren(Element);
        PptxTextFrame.AppendTextRuns(Element, PptxDiagnostic.CleanText(text));
        TextFrame.Shape.Tree.MarkDirty();
        TextFrame.Load();
    }
    internal PptxRun AddRun(string text)
    {
        Element.AppendChild(PptxTextFrame.CreateRunElement(PptxTextFrame.GetOwnerDocument(Element), PptxDiagnostic.CleanText(text)));
        TextFrame.Shape.Tree.MarkDirty();
        TextFrame.Load();
        return FindReloadedLastRun();
    }
    internal PptxRun AddLineBreak()
    {
        Element.AppendChild(PptxTextFrame.CreateLineBreakElement(PptxTextFrame.GetOwnerDocument(Element)));
        TextFrame.Shape.Tree.MarkDirty();
        TextFrame.Load();
        return FindReloadedLastRun();
    }
    internal PptxRun AddTab()
    {
        Element.AppendChild(PptxTextFrame.CreateTabElement(PptxTextFrame.GetOwnerDocument(Element)));
        TextFrame.Shape.Tree.MarkDirty();
        TextFrame.Load();
        return FindReloadedLastRun();
    }
    internal PptxRun AddField(string fieldId, string fieldType, string text)
    {
        string cleanId = PptxDiagnostic.CleanText(fieldId);
        string cleanType = PptxDiagnostic.CleanText(fieldType);
        if (cleanId.Length == 0)
        {
            throw new System.ArgumentException("A field identifier is required.", "fieldId");
        }

        if (cleanType.Length == 0)
        {
            throw new System.ArgumentException("A field type is required.", "fieldType");
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement field = document.CreateElement("a", "fld", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(field, "", "", "id", cleanId);
        PptxXml.SetAttributeValue(field, "", "", "type", cleanType);
        XmlElement value = document.CreateElement("a", "t", PptxNamespaces.Drawing);
        value.InnerText = PptxRunFormatCore.NormalizeTextValue(text);
        field.AppendChild(value);
        Element.AppendChild(field);
        TextFrame.Shape.Tree.MarkDirty();
        TextFrame.Load();
        return FindReloadedLastRun();
    }
    internal PptxRun AddSymbol(string symbol, string typeface)
    {
        string cleanSymbol = PptxRunFormatCore.NormalizeTextValue(symbol);
        string cleanTypeface = PptxDiagnostic.CleanText(typeface);
        if (cleanSymbol.Length == 0)
        {
            throw new System.ArgumentException("A symbol is required.", "symbol");
        }

        if (cleanTypeface.Length == 0)
        {
            throw new System.ArgumentException("A symbol typeface is required.", "typeface");
        }

        PptxRun run = AddRun(cleanSymbol);
        PptxRunFormatCore.SetSymbol(run, cleanTypeface);
        return run;
    }
    internal int ReplaceText(string search, string replacement)
    {
        int count = PptxTextReplacerCore.ReplaceParagraphText(Element, search, replacement);
        if (count > 0)
        {
            TextFrame.Shape.Tree.MarkDirty();
            TextFrame.Load();
        }

        return count;
    }
    internal PptxParagraphFormat GetFormat()
    {
        return PptxParagraphFormatCore.GetFormat(this);
    }
    internal void SetAlignment(string alignment)
    {
        PptxParagraphFormatCore.SetAlignment(this, alignment);
    }
    internal void SetIndentation(int level, long marginLeft, long marginRight, long indent)
    {
        PptxParagraphFormatCore.SetIndentation(this, level, marginLeft, marginRight, indent);
    }
    internal void SetSpacing(PptxTextSpacing line, PptxTextSpacing before, PptxTextSpacing after)
    {
        PptxParagraphFormatCore.SetSpacing(this, line, before, after);
    }
    internal void SetTabs(List<PptxTextTab> tabs)
    {
        PptxParagraphFormatCore.SetTabs(this, tabs);
    }
    internal void SetDefaultTabSize(long size)
    {
        PptxParagraphFormatCore.SetDefaultTabSize(this, size);
    }
    internal void SetDefaultRunProperties(PptxFont font)
    {
        PptxParagraphFormatCore.SetDefaultRunProperties(this, font);
    }
    internal void SetCharacterBullet(string character)
    {
        PptxParagraphFormatCore.SetCharacterBullet(this, character);
    }
    internal void SetAutomaticNumbering(string numberingType, int startAt)
    {
        PptxParagraphFormatCore.SetAutomaticNumbering(this, numberingType, startAt);
    }
    internal void ClearBullet()
    {
        PptxParagraphFormatCore.ClearBullet(this);
    }

    internal XmlElement CreateTemporaryTextBody()
    {
        XmlDocument document = PptxXml.CreateDocument();
        XmlElement textBody = document.CreateElement("p", "txBody", PptxNamespaces.Presentation);
        XmlNode imported = document.ImportNode(Element, true);
        textBody.AppendChild(imported);
        return textBody;
    }

    internal PptxRun FindReloadedLastRun()
    {
        for (int index = 0; index < TextFrame.Paragraphs.Count; index++)
        {
            PptxParagraph paragraph = TextFrame.Paragraphs[index];
            if (paragraph.Element == Element && paragraph.RunCount() > 0)
            {
                return paragraph.GetRun(paragraph.RunCount() - 1);
            }
        }

        throw new System.ArgumentException("Paragraph run was not found.");
    }

    internal void Load()
    {
        Alignment = "";
        Level = 0;
        HasBullet = false;
        BulletType = PptxBullet.None;
        BulletCharacter = "";
        NumberingType = "";
        NumberingStartAt = 0;
        PropertiesXml = "";
        XmlElement? properties = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "pPr");
        LoadProperties(properties);
        LoadRuns();
    }

    internal void LoadProperties(XmlElement? properties)
    {
        if (properties == null)
        {
            return;
        }

        PropertiesXml = properties.OuterXml;
        Alignment = PptxXml.GetAttributeValue(properties, "", "algn");
        Level = PptxXml.GetIntAttribute(properties, "", "lvl", 0);
        LoadBullet(properties);
    }

    internal void LoadBullet(XmlElement properties)
    {
        XmlElement? none = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buNone");
        if (none != null)
        {
            HasBullet = false;
            BulletType = PptxBullet.None;
            return;
        }

        XmlElement? character = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buChar");
        if (character != null)
        {
            HasBullet = true;
            BulletType = PptxBullet.Character;
            BulletCharacter = PptxXml.GetAttributeValue(character, "", "char");
            return;
        }

        XmlElement? automaticNumbering = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buAutoNum");
        if (automaticNumbering != null)
        {
            HasBullet = true;
            BulletType = PptxBullet.Number;
            NumberingType = PptxXml.GetAttributeValue(automaticNumbering, "", "type");
            NumberingStartAt = PptxXml.GetIntAttribute(automaticNumbering, "", "startAt", 1);
            return;
        }

        XmlElement? picture = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "buBlip");
        if (picture != null)
        {
            HasBullet = true;
            BulletType = PptxBullet.Picture;
        }
    }

    internal void LoadRuns()
    {
        Runs.Clear();
        for (int index = 0; index < Element.ChildNodes.Count; index++)
        {
            XmlNode? child = Element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement runElement = (XmlElement)child;
            if (IsRunElement(runElement))
            {
                Runs.Add(new PptxRun(this, runElement));
            }
        }
    }

    internal bool IsRunElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Drawing)
        {
            return false;
        }

        if (element.LocalName == "r")
        {
            return true;
        }

        if (element.LocalName == "fld")
        {
            return true;
        }

        if (element.LocalName == "br")
        {
            return true;
        }

        if (element.LocalName == "tab")
        {
            return true;
        }

        return false;
    }
}
