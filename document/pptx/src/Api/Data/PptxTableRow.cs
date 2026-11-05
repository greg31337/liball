using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents table row data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTableRow
{
    internal PptxTable Table;
    internal XmlElement Element;
    /// <summary>
    /// Stores the row index value associated with this object.
    /// </summary>
    public int RowIndex;
    /// <summary>
    /// Contains the cells in package or presentation order.
    /// </summary>
    public List<PptxTableCell> Cells;

    internal PptxTableRow(PptxTable table, XmlElement element, int rowIndex)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Table = table;
        Element = element;
        RowIndex = rowIndex;
        Cells = new List<PptxTableCell>();
        Load();
    }
    internal int CellCount()
    {
        return Cells.Count;
    }
    internal PptxTableCell GetCell(int index)
    {
        ValidateCellIndex(index);
        return Cells[index];
    }
    internal long GetHeight()
    {
        return PptxTransform.GetLongAttribute(Element, "h", 0L);
    }
    internal void SetHeight(long height)
    {
        if (height < 0L)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        PptxXml.SetAttributeValue(Element, "", "", "h", height.ToString());
        Table.MarkDirty();
    }

    internal void Load()
    {
        Cells.Clear();
        List<XmlElement> cellElements = new List<XmlElement>();
        PptxTable.CollectDirectChildren(Element, PptxNamespaces.Drawing, "tc", cellElements);
        for (int index = 0; index < cellElements.Count; index++)
        {
            Cells.Add(new PptxTableCell(this, cellElements[index], RowIndex, index));
        }
    }

    internal void ValidateCellIndex(int index)
    {
        if (index < 0 || index >= Cells.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

