using System.Xml;

namespace pptx;

/// <summary>
/// Represents table cell data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTableCell
{
    /// <summary>
    /// Identifies the border left value accepted by the API.
    /// </summary>
    public const string BorderLeft = "lnL";
    /// <summary>
    /// Identifies the border right value accepted by the API.
    /// </summary>
    public const string BorderRight = "lnR";
    /// <summary>
    /// Identifies the border top value accepted by the API.
    /// </summary>
    public const string BorderTop = "lnT";
    /// <summary>
    /// Identifies the border bottom value accepted by the API.
    /// </summary>
    public const string BorderBottom = "lnB";

    internal PptxTableRow Row;
    internal XmlElement Element;
    /// <summary>
    /// Stores the row index value associated with this object.
    /// </summary>
    public int RowIndex;
    /// <summary>
    /// Stores the column index value associated with this object.
    /// </summary>
    public int ColumnIndex;

    internal PptxTableCell(PptxTableRow row, XmlElement element, int rowIndex, int columnIndex)
    {
        if (row == null)
        {
            throw new System.ArgumentNullException("row");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Row = row;
        Element = element;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }
    internal string GetText()
    {
        XmlElement? textBody = PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "txBody");
        if (textBody == null)
        {
            return "";
        }

        return PptxTextFrame.GetTextBodyText(textBody);
    }
    internal void SetText(string text)
    {
        XmlElement textBody = PptxTextFrame.GetOrCreateTextBody(Element, "a", PptxNamespaces.Drawing);
        PptxTextFrame.SetTextBodyText(textBody, PptxDiagnostic.CleanText(text));
        Row.Table.MarkDirty();
    }
    internal int GetColumnSpan()
    {
        return PptxXml.GetIntAttribute(Element, "", "gridSpan", 1);
    }
    internal int GetRowSpan()
    {
        return PptxXml.GetIntAttribute(Element, "", "rowSpan", 1);
    }
    internal bool IsHorizontalMerge()
    {
        return PptxXml.GetBoolAttribute(Element, "", "hMerge", false);
    }
    internal bool IsVerticalMerge()
    {
        return PptxXml.GetBoolAttribute(Element, "", "vMerge", false);
    }
    internal bool IsMergeContinuation()
    {
        if (IsHorizontalMerge())
        {
            return true;
        }

        if (IsVerticalMerge())
        {
            return true;
        }

        return false;
    }
    internal bool IsMergeOrigin()
    {
        if (IsMergeContinuation())
        {
            return false;
        }

        if (GetColumnSpan() > 1)
        {
            return true;
        }

        if (GetRowSpan() > 1)
        {
            return true;
        }

        return false;
    }
    internal bool IsMerged()
    {
        if (IsMergeOrigin())
        {
            return true;
        }

        if (IsMergeContinuation())
        {
            return true;
        }

        return false;
    }
    internal PptxFill GetFill()
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties == null)
        {
            return new PptxFill();
        }

        XmlElement? fillElement = PptxFill.FindFillElement(cellProperties);
        if (fillElement == null)
        {
            return new PptxFill();
        }

        return PptxFill.FromElement(fillElement);
    }
    internal void SetFill(PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        XmlElement cellProperties = GetOrCreateCellProperties();
        RemoveFillChildren(cellProperties);
        cellProperties.AppendChild(fill.ToElement(PptxTextFrame.GetOwnerDocument(cellProperties)));
        Row.Table.MarkDirty();
    }
    internal PptxLine GetBorder(string borderName)
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties == null)
        {
            return new PptxLine();
        }

        XmlElement? border = PptxXml.FindFirstChild(cellProperties, PptxNamespaces.Drawing, NormalizeBorderName(borderName));
        if (border == null)
        {
            return new PptxLine();
        }

        return PptxLine.FromElement(border);
    }
    internal void SetBorder(string borderName, PptxLine line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        XmlElement cellProperties = GetOrCreateCellProperties();
        string localName = NormalizeBorderName(borderName);
        RemoveDirectChild(cellProperties, PptxNamespaces.Drawing, localName);
        cellProperties.AppendChild(CreateBorderElement(PptxTextFrame.GetOwnerDocument(cellProperties), localName, line));
        Row.Table.MarkDirty();
    }
    internal long GetMarginLeft()
    {
        return GetMargin("left");
    }
    internal long GetMarginTop()
    {
        return GetMargin("top");
    }
    internal long GetMarginRight()
    {
        return GetMargin("right");
    }
    internal long GetMarginBottom()
    {
        return GetMargin("bottom");
    }
    internal long GetMargin(string marginName)
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties == null)
        {
            return 0L;
        }

        return PptxTransform.GetLongAttribute(cellProperties, NormalizeMarginName(marginName), 0L);
    }
    internal void SetMargins(long left, long top, long right, long bottom)
    {
        ValidateNonNegative(left, "left");
        ValidateNonNegative(top, "top");
        ValidateNonNegative(right, "right");
        ValidateNonNegative(bottom, "bottom");
        XmlElement cellProperties = GetOrCreateCellProperties();
        PptxXml.SetAttributeValue(cellProperties, "", "", "marL", left.ToString());
        PptxXml.SetAttributeValue(cellProperties, "", "", "marT", top.ToString());
        PptxXml.SetAttributeValue(cellProperties, "", "", "marR", right.ToString());
        PptxXml.SetAttributeValue(cellProperties, "", "", "marB", bottom.ToString());
        Row.Table.MarkDirty();
    }
    internal void SetMargin(string marginName, long margin)
    {
        ValidateNonNegative(margin, "margin");
        XmlElement cellProperties = GetOrCreateCellProperties();
        PptxXml.SetAttributeValue(cellProperties, "", "", NormalizeMarginName(marginName), margin.ToString());
        Row.Table.MarkDirty();
    }
    internal string GetVerticalAlignment()
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(cellProperties, "", "anchor");
    }
    internal void SetVerticalAlignment(string alignment)
    {
        XmlElement cellProperties = GetOrCreateCellProperties();
        string value = NormalizeVerticalAlignment(alignment);
        if (value.Length == 0)
        {
            cellProperties.RemoveAttribute("anchor");
        }
        else
        {
            PptxXml.SetAttributeValue(cellProperties, "", "", "anchor", value);
        }

        Row.Table.MarkDirty();
    }
    internal string GetTextDirection()
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(cellProperties, "", "vert");
    }
    internal void SetTextDirection(string direction)
    {
        XmlElement cellProperties = GetOrCreateCellProperties();
        string value = NormalizeTextDirection(direction);
        if (value.Length == 0)
        {
            cellProperties.RemoveAttribute("vert");
        }
        else
        {
            PptxXml.SetAttributeValue(cellProperties, "", "", "vert", value);
        }

        Row.Table.MarkDirty();
    }

    internal XmlElement? GetCellProperties()
    {
        return PptxXml.FindFirstChild(Element, PptxNamespaces.Drawing, "tcPr");
    }

    internal XmlElement GetOrCreateCellProperties()
    {
        XmlElement? cellProperties = GetCellProperties();
        if (cellProperties != null)
        {
            return cellProperties;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement created = document.CreateElement("a", "tcPr", PptxNamespaces.Drawing);
        Element.AppendChild(created);
        return created;
    }

    internal void RemoveFillChildren(XmlElement element)
    {
        for (int index = element.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement childElement = (XmlElement)child;
            if (childElement.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(childElement.LocalName))
            {
                element.RemoveChild(child);
            }
        }
    }

    internal void RemoveDirectChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                root.RemoveChild(child);
            }
        }
    }

    internal static XmlElement CreateBorderElement(XmlDocument document, string localName, PptxLine line)
    {
        XmlElement border = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        XmlElement source = line.ToElement(document);
        for (int index = 0; index < source.Attributes.Count; index++)
        {
            XmlAttribute attribute = source.Attributes[index];
            if (attribute != null)
            {
                XmlAttribute copy = (XmlAttribute)attribute.CloneNode(true);
                border.Attributes.Append(copy);
            }
        }

        for (int index = 0; index < source.ChildNodes.Count; index++)
        {
            XmlNode? child = source.ChildNodes[index];
            if (child != null)
            {
                border.AppendChild(child.CloneNode(true));
            }
        }

        return border;
    }

    internal static string NormalizeBorderName(string borderName)
    {
        string clean = PptxDiagnostic.CleanText(borderName).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "l" || clean == "left" || clean == "lnl")
        {
            return BorderLeft;
        }

        if (clean == "r" || clean == "right" || clean == "lnr")
        {
            return BorderRight;
        }

        if (clean == "t" || clean == "top" || clean == "lnt")
        {
            return BorderTop;
        }

        if (clean == "b" || clean == "bottom" || clean == "lnb")
        {
            return BorderBottom;
        }

        throw new System.ArgumentException("Unknown table cell border.");
    }

    internal static string NormalizeMarginName(string marginName)
    {
        string clean = PptxDiagnostic.CleanText(marginName).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "l" || clean == "left" || clean == "marl")
        {
            return "marL";
        }

        if (clean == "r" || clean == "right" || clean == "marr")
        {
            return "marR";
        }

        if (clean == "t" || clean == "top" || clean == "mart")
        {
            return "marT";
        }

        if (clean == "b" || clean == "bottom" || clean == "marb")
        {
            return "marB";
        }

        throw new System.ArgumentException("Unknown table cell margin.");
    }

    internal static string NormalizeVerticalAlignment(string alignment)
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

        if (clean == "justified" || clean == "justify" || clean == "justdist")
        {
            return "justDist";
        }

        return PptxDiagnostic.CleanText(alignment);
    }

    internal static string NormalizeTextDirection(string direction)
    {
        string clean = PptxDiagnostic.CleanText(direction).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean.Length == 0)
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

        return PptxDiagnostic.CleanText(direction);
    }

    internal static void ValidateNonNegative(long value, string parameterName)
    {
        if (value < 0L)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

