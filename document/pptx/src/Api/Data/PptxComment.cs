using System.Xml;

namespace pptx;

/// <summary>
/// Represents comment data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxComment
{
    internal PptxSlide Slide;
    internal PptxPart Part;
    internal XmlElement Element;
    /// <summary>
    /// Stores the author id used to identify or connect related presentation data.
    /// </summary>
    public int AuthorId;
    /// <summary>
    /// Stores the index value associated with this object.
    /// </summary>
    public int Index;
    /// <summary>
    /// Stores the date time associated with this object.
    /// </summary>
    public string DateTime;
    /// <summary>
    /// Stores the text associated with this object.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the x in English Metric Units (EMUs).
    /// </summary>
    public long X;
    /// <summary>
    /// Stores the y in English Metric Units (EMUs).
    /// </summary>
    public long Y;

    internal PptxComment(PptxSlide slide, PptxPart part, XmlElement element)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Slide = slide;
        Part = part;
        Element = element;
        AuthorId = 0;
        Index = 0;
        DateTime = "";
        Text = "";
        X = 0L;
        Y = 0L;
        Load();
    }

    internal void Load()
    {
        AuthorId = PptxXml.GetIntAttribute(Element, "", "authorId", 0);
        Index = PptxXml.GetIntAttribute(Element, "", "idx", 0);
        DateTime = PptxXml.GetAttributeValue(Element, "", "dt");
        Text = "";
        X = 0L;
        Y = 0L;

        XmlElement? position = PptxXml.FindFirstChild(Element, PptxNamespaces.Presentation, "pos");
        if (position != null)
        {
            X = PptxTransform.GetLongAttribute(position, "x", 0L);
            Y = PptxTransform.GetLongAttribute(position, "y", 0L);
        }

        XmlElement? text = PptxXml.FindFirstChild(Element, PptxNamespaces.Presentation, "text");
        if (text != null)
        {
            Text = text.InnerText;
        }
    }
    internal void SetText(string text)
    {
        XmlElement textElement = GetOrCreateTextElement();
        Text = PptxRunFormatCore.NormalizeTextValue(text);
        textElement.InnerText = Text;
        MarkDirty();
    }
    internal void SetPosition(long x, long y)
    {
        XmlElement position = GetOrCreatePositionElement();
        X = x;
        Y = y;
        PptxXml.SetAttributeValue(position, "", "", "x", X.ToString());
        PptxXml.SetAttributeValue(position, "", "", "y", Y.ToString());
        MarkDirty();
    }
    internal void Delete()
    {
        XmlNode? parent = Element.ParentNode;
        if (parent != null)
        {
            parent.RemoveChild(Element);
            MarkDirty();
        }
    }

    internal XmlElement GetOrCreatePositionElement()
    {
        XmlElement? position = PptxXml.FindFirstChild(Element, PptxNamespaces.Presentation, "pos");
        if (position != null)
        {
            return position;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        position = document.CreateElement("p", "pos", PptxNamespaces.Presentation);
        Element.InsertBefore(position, Element.FirstChild);
        return position;
    }

    internal XmlElement GetOrCreateTextElement()
    {
        XmlElement? text = PptxXml.FindFirstChild(Element, PptxNamespaces.Presentation, "text");
        if (text != null)
        {
            return text;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        text = document.CreateElement("p", "text", PptxNamespaces.Presentation);
        Element.AppendChild(text);
        return text;
    }

    internal void MarkDirty()
    {
        Part.SetXmlDocument(PptxTextFrame.GetOwnerDocument(Element));
    }
}
