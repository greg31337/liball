using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents text frame data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTextFrame
{
    internal PptxShape Shape;
    internal XmlElement? Element;
    /// <summary>
    /// Contains the paragraphs in package or presentation order.
    /// </summary>
    public List<PptxParagraph> Paragraphs;
    /// <summary>
    /// Indicates whether the present setting is enabled.
    /// </summary>
    public bool Present;
    internal string Xml;
    /// <summary>
    /// Stores the body properties xml associated with this object.
    /// </summary>
    public string BodyPropertiesXml;
    /// <summary>
    /// Stores the list style xml associated with this object.
    /// </summary>
    public string ListStyleXml;

    internal PptxTextFrame(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        Shape = shape;
        Element = null;
        Paragraphs = new List<PptxParagraph>();
        Present = false;
        Xml = "";
        BodyPropertiesXml = "";
        ListStyleXml = "";
        Load();
    }
    internal int ParagraphCount()
    {
        return Paragraphs.Count;
    }
    internal PptxParagraph GetParagraph(int index)
    {
        return Paragraphs[index];
    }
    internal bool HasText()
    {
        return GetText().Length > 0;
    }
    internal string GetText()
    {
        if (Element == null)
        {
            return "";
        }

        return GetTextBodyText(Element);
    }

    internal void Load()
    {
        Element = PptxXml.FindFirstChild(Shape.Element, PptxNamespaces.Presentation, "txBody");
        Paragraphs.Clear();
        Present = false;
        Xml = "";
        BodyPropertiesXml = "";
        ListStyleXml = "";
        if (Element == null)
        {
            return;
        }

        Present = true;
        Xml = Element.OuterXml;
        XmlElement? bodyProperties = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "bodyPr");
        if (bodyProperties != null)
        {
            BodyPropertiesXml = bodyProperties.OuterXml;
        }

        XmlElement? listStyle = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "lstStyle");
        if (listStyle != null)
        {
            ListStyleXml = listStyle.OuterXml;
        }

        LoadParagraphs();
    }

    internal void LoadParagraphs()
    {
        if (Element == null)
        {
            return;
        }

        for (int index = 0; index < Element.ChildNodes.Count; index++)
        {
            XmlNode? child = Element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement paragraphElement = (XmlElement)child;
            if (paragraphElement.NamespaceURI == PptxNamespaces.Drawing && paragraphElement.LocalName == "p")
            {
                Paragraphs.Add(new PptxParagraph(this, paragraphElement));
            }
        }
    }
    internal void SetText(string text)
    {
        XmlElement textBody = EnsureTextBody();
        SetTextBodyText(textBody, PptxDiagnostic.CleanText(text));
        Shape.Tree.MarkDirty();
        Load();
    }
    internal void Clear()
    {
        XmlElement textBody = EnsureTextBody();
        RemoveParagraphs(textBody);
        Shape.Tree.MarkDirty();
        Load();
    }
    internal PptxParagraph AddParagraph(string text)
    {
        XmlElement textBody = EnsureTextBody();
        XmlElement paragraph = CreateParagraphElement(GetOwnerDocument(textBody), PptxDiagnostic.CleanText(text));
        textBody.AppendChild(paragraph);
        Shape.Tree.MarkDirty();
        Load();
        return Paragraphs[Paragraphs.Count - 1];
    }
    internal int ReplaceText(string search, string replacement)
    {
        if (Element == null)
        {
            return 0;
        }

        int count = PptxTextReplacerCore.ReplaceTextBody(Element, search, replacement);
        if (count > 0)
        {
            Shape.Tree.MarkDirty();
            Load();
        }

        return count;
    }

    internal PptxTextFrameBody GetBodySettings()
    {
        PptxTextFrameBody settings = new PptxTextFrameBody();
        XmlElement? properties = GetBodyProperties();
        if (properties == null)
        {
            return settings;
        }

        settings.Margins.Left = PptxTransform.GetLongAttribute(properties, "lIns", settings.Margins.Left);
        settings.Margins.Top = PptxTransform.GetLongAttribute(properties, "tIns", settings.Margins.Top);
        settings.Margins.Right = PptxTransform.GetLongAttribute(properties, "rIns", settings.Margins.Right);
        settings.Margins.Bottom = PptxTransform.GetLongAttribute(properties, "bIns", settings.Margins.Bottom);
        settings.VerticalAlignment = PptxXml.GetAttributeValue(properties, "", "anchor");
        if (settings.VerticalAlignment.Length == 0)
        {
            settings.VerticalAlignment = "t";
        }

        settings.CenteredAnchor = PptxXml.GetBoolAttribute(properties, "", "anchorCtr", false);
        settings.Wrapping = PptxXml.GetAttributeValue(properties, "", "wrap");
        if (settings.Wrapping.Length == 0)
        {
            settings.Wrapping = "square";
        }

        settings.HorizontalOverflow = PptxXml.GetAttributeValue(properties, "", "horzOverflow");
        if (settings.HorizontalOverflow.Length == 0)
        {
            settings.HorizontalOverflow = "overflow";
        }

        settings.VerticalOverflow = PptxXml.GetAttributeValue(properties, "", "vertOverflow");
        if (settings.VerticalOverflow.Length == 0)
        {
            settings.VerticalOverflow = "overflow";
        }

        settings.VerticalText = PptxXml.GetAttributeValue(properties, "", "vert");
        if (settings.VerticalText.Length == 0)
        {
            settings.VerticalText = "horz";
        }

        settings.Rotation = PptxXml.GetIntAttribute(properties, "", "rot", 0);
        settings.Upright = PptxXml.GetBoolAttribute(properties, "", "upright", false);
        XmlElement? autofit = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "noAutofit");
        if (autofit != null)
        {
            settings.Autofit = PptxTextFrameBody.AutofitNone;
            return settings;
        }

        autofit = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "normAutofit");
        if (autofit != null)
        {
            settings.Autofit = PptxTextFrameBody.AutofitNormal;
            settings.AutofitFontScale = PptxXml.GetIntAttribute(autofit, "", "fontScale", 100000);
            settings.AutofitLineSpacingReduction = PptxXml.GetIntAttribute(autofit, "", "lnSpcReduction", 0);
            return settings;
        }

        autofit = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "spAutoFit");
        if (autofit != null)
        {
            settings.Autofit = PptxTextFrameBody.AutofitShape;
        }

        return settings;
    }

    internal void SetBodyMargins(PptxMargins margins)
    {
        if (margins == null)
        {
            throw new System.ArgumentNullException("margins");
        }

        XmlElement properties = EnsureBodyProperties();
        PptxXml.SetAttributeValue(properties, "", "", "lIns", margins.Left.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "tIns", margins.Top.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "rIns", margins.Right.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "bIns", margins.Bottom.ToString());
        MarkBodyChanged();
    }

    internal void SetBodyAnchoring(string verticalAlignment, bool centered)
    {
        string anchor = NormalizeBodyAnchor(verticalAlignment);
        XmlElement properties = EnsureBodyProperties();
        if (anchor.Length == 0)
        {
            properties.RemoveAttribute("anchor");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "anchor", anchor);
        }

        PptxXml.SetAttributeValue(properties, "", "", "anchorCtr", PptxTransform.BoolToXml(centered));
        MarkBodyChanged();
    }

    internal void SetBodyAutofit(string mode, int fontScale, int lineSpacingReduction)
    {
        string normalized = NormalizeBodyAutofit(mode);
        if (fontScale < 0 || fontScale > 100000)
        {
            throw new System.ArgumentOutOfRangeException("fontScale");
        }

        if (lineSpacingReduction < 0 || lineSpacingReduction > 100000)
        {
            throw new System.ArgumentOutOfRangeException("lineSpacingReduction");
        }

        XmlElement properties = EnsureBodyProperties();
        RemoveAutofitChildren(properties);
        XmlDocument document = GetOwnerDocument(properties);
        if (normalized == PptxTextFrameBody.AutofitNone)
        {
            InsertAutofitChild(properties, document.CreateElement("a", "noAutofit", PptxNamespaces.Drawing));
        }
        else if (normalized == PptxTextFrameBody.AutofitNormal)
        {
            XmlElement autofit = document.CreateElement("a", "normAutofit", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(autofit, "", "", "fontScale", fontScale.ToString());
            PptxXml.SetAttributeValue(autofit, "", "", "lnSpcReduction", lineSpacingReduction.ToString());
            InsertAutofitChild(properties, autofit);
        }
        else if (normalized == PptxTextFrameBody.AutofitShape)
        {
            InsertAutofitChild(properties, document.CreateElement("a", "spAutoFit", PptxNamespaces.Drawing));
        }

        MarkBodyChanged();
    }

    internal void SetBodyWrapping(string wrapping)
    {
        string normalized = NormalizeBodyWrapping(wrapping);
        XmlElement properties = EnsureBodyProperties();
        if (normalized.Length == 0)
        {
            properties.RemoveAttribute("wrap");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "wrap", normalized);
        }

        MarkBodyChanged();
    }

    internal void SetBodyOverflow(string horizontal, string vertical)
    {
        string normalizedHorizontal = NormalizeHorizontalOverflow(horizontal);
        string normalizedVertical = NormalizeVerticalOverflow(vertical);
        XmlElement properties = EnsureBodyProperties();
        if (normalizedHorizontal.Length == 0)
        {
            properties.RemoveAttribute("horzOverflow");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "horzOverflow", normalizedHorizontal);
        }

        if (normalizedVertical.Length == 0)
        {
            properties.RemoveAttribute("vertOverflow");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "vertOverflow", normalizedVertical);
        }

        MarkBodyChanged();
    }

    internal void SetBodyVerticalText(string mode)
    {
        string normalized = NormalizeBodyVerticalText(mode);
        XmlElement properties = EnsureBodyProperties();
        if (normalized.Length == 0)
        {
            properties.RemoveAttribute("vert");
        }
        else
        {
            PptxXml.SetAttributeValue(properties, "", "", "vert", normalized);
        }

        MarkBodyChanged();
    }

    internal void SetBodyOrientation(int rotation, bool upright)
    {
        XmlElement properties = EnsureBodyProperties();
        PptxXml.SetAttributeValue(properties, "", "", "rot", rotation.ToString());
        PptxXml.SetAttributeValue(properties, "", "", "upright", PptxTransform.BoolToXml(upright));
        MarkBodyChanged();
    }

    internal XmlElement? GetBodyProperties()
    {
        if (Element == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "bodyPr");
    }

    internal XmlElement EnsureBodyProperties()
    {
        XmlElement textBody = EnsureTextBody();
        XmlElement? properties = PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "bodyPr");
        if (properties == null)
        {
            throw new System.InvalidOperationException("Text body properties could not be created.");
        }

        return properties;
    }

    internal void MarkBodyChanged()
    {
        Shape.Tree.MarkDirty();
        Load();
    }

    internal void RemoveAutofitChildren(XmlElement properties)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlElement? child = properties.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            if (child.LocalName == "noAutofit" || child.LocalName == "normAutofit" || child.LocalName == "spAutoFit")
            {
                properties.RemoveChild(child);
            }
        }
    }

    internal void InsertAutofitChild(XmlElement properties, XmlElement autofit)
    {
        for (int index = 0; index < properties.ChildNodes.Count; index++)
        {
            XmlElement? child = properties.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            if (child.LocalName == "scene3d" || child.LocalName == "sp3d" || child.LocalName == "extLst")
            {
                properties.InsertBefore(autofit, child);
                return;
            }
        }

        properties.AppendChild(autofit);
    }

    internal static string NormalizeBodyAnchor(string alignment)
    {
        string clean = PptxDiagnostic.CleanText(alignment).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0)
        {
            return "";
        }

        if (clean == "top" || clean == "t")
        {
            return "t";
        }

        if (clean == "middle" || clean == "center" || clean == "centre" || clean == "ctr")
        {
            return "ctr";
        }

        if (clean == "bottom" || clean == "b")
        {
            return "b";
        }

        if (clean == "justified" || clean == "justify" || clean == "just")
        {
            return "just";
        }

        if (clean == "distributed" || clean == "dist")
        {
            return "dist";
        }

        throw new System.ArgumentException("Unknown text anchor.", "verticalAlignment");
    }

    internal static string NormalizeBodyAutofit(string mode)
    {
        string clean = PptxDiagnostic.CleanText(mode).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "none" || clean == "noautofit")
        {
            return PptxTextFrameBody.AutofitNone;
        }

        if (clean == "normal" || clean == "normautofit" || clean == "shrinktext")
        {
            return PptxTextFrameBody.AutofitNormal;
        }

        if (clean == "shape" || clean == "spautofit" || clean == "growshape")
        {
            return PptxTextFrameBody.AutofitShape;
        }

        throw new System.ArgumentException("Unknown text autofit mode.", "mode");
    }

    internal static string NormalizeBodyWrapping(string wrapping)
    {
        string clean = PptxDiagnostic.CleanText(wrapping).ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "square" || clean == "wrap")
        {
            return "square";
        }

        if (clean == "none" || clean == "nowrap")
        {
            return "none";
        }

        throw new System.ArgumentException("Unknown text wrapping mode.", "wrapping");
    }

    internal static string NormalizeHorizontalOverflow(string overflow)
    {
        string clean = PptxDiagnostic.CleanText(overflow).ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "overflow" || clean == "clip")
        {
            return clean;
        }

        throw new System.ArgumentException("Unknown horizontal overflow mode.", "horizontal");
    }

    internal static string NormalizeVerticalOverflow(string overflow)
    {
        string clean = PptxDiagnostic.CleanText(overflow).ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "overflow" || clean == "ellipsis" || clean == "clip")
        {
            return clean;
        }

        throw new System.ArgumentException("Unknown vertical overflow mode.", "vertical");
    }

    internal static string NormalizeBodyVerticalText(string mode)
    {
        string clean = PptxDiagnostic.CleanText(mode).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0 || clean == "inherit" || clean == "inherited")
        {
            return "";
        }

        if (clean == "horizontal" || clean == "horz")
        {
            return "horz";
        }

        if (clean == "vertical" || clean == "vert")
        {
            return "vert";
        }

        if (clean == "vertical270" || clean == "vert270")
        {
            return "vert270";
        }

        if (clean == "wordartvertical" || clean == "wordartvert")
        {
            return "wordArtVert";
        }

        if (clean == "wordartverticalrtl" || clean == "wordartvertrtl")
        {
            return "wordArtVertRtl";
        }

        if (clean == "eastasiavertical" || clean == "eavert")
        {
            return "eaVert";
        }

        if (clean == "mongolianvertical" || clean == "mongolianvert")
        {
            return "mongolianVert";
        }

        throw new System.ArgumentException("Unknown vertical text mode.", "mode");
    }

    internal XmlElement EnsureTextBody()
    {
        if (Element != null)
        {
            EnsureTextBodyDefaults(Element);
            return Element;
        }

        Element = GetOrCreateTextBody(Shape.Element, "p", PptxNamespaces.Presentation);
        return Element;
    }

    internal static XmlElement GetOrCreateTextBody(XmlElement parent, string prefix, string namespaceUri)
    {
        XmlElement? textBody = PptxXml.FindFirstChild(parent, namespaceUri, "txBody");
        if (textBody != null)
        {
            EnsureTextBodyDefaults(textBody);
            return textBody;
        }

        XmlDocument document = GetOwnerDocument(parent);
        XmlElement created = document.CreateElement(prefix, "txBody", namespaceUri);
        XmlElement bodyProperties = document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing);
        XmlElement listStyle = document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing);
        created.AppendChild(bodyProperties);
        created.AppendChild(listStyle);
        parent.AppendChild(created);
        return created;
    }

    internal static void EnsureTextBodyDefaults(XmlElement textBody)
    {
        XmlDocument document = GetOwnerDocument(textBody);
        if (PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "bodyPr") == null)
        {
            XmlElement bodyProperties = document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing);
            textBody.InsertBefore(bodyProperties, textBody.FirstChild);
        }

        if (PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "lstStyle") == null)
        {
            XmlElement listStyle = document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing);
            textBody.InsertBefore(listStyle, PptxXml.FindFirstChild(textBody, PptxNamespaces.Drawing, "p"));
        }
    }

    internal static void SetTextBodyText(XmlElement textBody, string text)
    {
        RemoveParagraphs(textBody);
        string normalized = PptxRunFormatCore.NormalizeTextValue(text);
        string[] paragraphs = normalized.Split(new string[] { "\n" }, System.StringSplitOptions.None);
        XmlDocument document = GetOwnerDocument(textBody);
        for (int index = 0; index < paragraphs.Length; index++)
        {
            textBody.AppendChild(CreateParagraphElement(document, paragraphs[index]));
        }

        if (paragraphs.Length == 0)
        {
            textBody.AppendChild(CreateParagraphElement(document, ""));
        }
    }

    internal static string GetTextBodyText(XmlElement textBody)
    {
        StringBuilder builder = new StringBuilder();
        int paragraphCount = 0;
        for (int index = 0; index < textBody.ChildNodes.Count; index++)
        {
            XmlNode? child = textBody.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement paragraph = (XmlElement)child;
            if (paragraph.NamespaceURI != PptxNamespaces.Drawing || paragraph.LocalName != "p")
            {
                continue;
            }

            if (paragraphCount > 0)
            {
                builder.Append("\n");
            }

            AppendParagraphText(paragraph, builder);
            paragraphCount = paragraphCount + 1;
        }

        return builder.ToString();
    }

    internal static XmlElement CreateParagraphElement(XmlDocument document, string text)
    {
        XmlElement paragraph = document.CreateElement("a", "p", PptxNamespaces.Drawing);
        AppendTextRuns(paragraph, PptxDiagnostic.CleanText(text));
        return paragraph;
    }

    internal static XmlElement CreateRunElement(XmlDocument document, string text)
    {
        XmlElement run = document.CreateElement("a", "r", PptxNamespaces.Drawing);
        run.AppendChild(document.CreateElement("a", "rPr", PptxNamespaces.Drawing));
        XmlElement value = document.CreateElement("a", "t", PptxNamespaces.Drawing);
        value.InnerText = PptxRunFormatCore.NormalizeTextValue(text);
        run.AppendChild(value);
        return run;
    }

    internal static XmlElement CreateLineBreakElement(XmlDocument document)
    {
        XmlElement lineBreak = document.CreateElement("a", "br", PptxNamespaces.Drawing);
        lineBreak.AppendChild(document.CreateElement("a", "rPr", PptxNamespaces.Drawing));
        return lineBreak;
    }

    internal static XmlElement CreateTabElement(XmlDocument document)
    {
        return document.CreateElement("a", "tab", PptxNamespaces.Drawing);
    }

    internal static void AppendTextRuns(XmlElement paragraph, string text)
    {
        XmlDocument document = GetOwnerDocument(paragraph);
        StringBuilder builder = new StringBuilder();
        string clean = PptxRunFormatCore.NormalizeTextValue(text);
        for (int index = 0; index < clean.Length; index++)
        {
            if (clean[index] == '\t')
            {
                AppendBufferedRun(paragraph, builder);
                paragraph.AppendChild(CreateTabElement(document));
            }
            else
            {
                builder.Append(clean[index]);
            }
        }

        AppendBufferedRun(paragraph, builder);
        if (paragraph.ChildNodes.Count == 0)
        {
            paragraph.AppendChild(CreateRunElement(document, ""));
        }
    }

    internal static void AppendBufferedRun(XmlElement paragraph, StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        paragraph.AppendChild(CreateRunElement(GetOwnerDocument(paragraph), builder.ToString()));
        builder.Clear();
    }

    internal static void RemoveParagraphs(XmlElement textBody)
    {
        for (int index = textBody.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = textBody.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "p")
            {
                textBody.RemoveChild(child);
            }
        }
    }

    internal static void RemoveRunChildren(XmlElement paragraph)
    {
        for (int index = paragraph.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = paragraph.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (IsTextRunElement(element))
            {
                paragraph.RemoveChild(child);
            }
        }
    }

    internal static bool IsTextRunElement(XmlElement element)
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

    internal static void AppendParagraphText(XmlElement paragraph, StringBuilder builder)
    {
        for (int index = 0; index < paragraph.ChildNodes.Count; index++)
        {
            XmlNode? child = paragraph.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            if (element.LocalName == "r" || element.LocalName == "fld")
            {
                XmlElement? text = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "t");
                if (text != null)
                {
                    builder.Append(text.InnerText);
                }
            }

            if (element.LocalName == "br")
            {
                builder.Append("\n");
            }

            if (element.LocalName == "tab")
            {
                builder.Append("\t");
            }
        }
    }

    internal static XmlDocument GetOwnerDocument(XmlElement element)
    {
        XmlDocument document = element.OwnerDocument;
        if (document == null)
        {
            throw new System.ArgumentException("Element must belong to a document.");
        }

        return document;
    }
}
