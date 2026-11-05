namespace docx;

/// <summary>
/// Reads and applies direct table, row, and cell formatting.
/// </summary>
public class DocxTableFormatting : IDocxTableFormatting
{
    /// <summary>
    /// Creates a table-formatting service.
    /// </summary>
    public DocxTableFormatting()
    {
    }

    /// <summary>
    /// Returns whether a table has direct formatting.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasTableFormat(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        return table.HasFormat();
    }

    /// <summary>
    /// Returns a table's direct format model.
    /// </summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The table format model.</returns>
    public DocxTableFormat GetTableFormat(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        return table.Format;
    }

    /// <summary>
    /// Replaces a table's direct format model.
    /// </summary>
    /// <param name="table">The table to edit.</param>
    /// <param name="format">The replacement format.</param>
    public void SetTableFormat(DocxTable table, DocxTableFormat format)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        table.SetFormat(format);
    }

    /// <summary>
    /// Returns whether a row has direct formatting.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasRowFormat(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        return row.HasAnyFormatting();
    }

    /// <summary>
    /// Returns a row's direct format model.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns>The row format model.</returns>
    public DocxTableRowFormat GetRowFormat(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        DocxTableRowFormat format = new DocxTableRowFormat();
        format.Height = row.Height;
        format.HeightRule = row.HeightRule;
        format.HasCantSplit = row.HasCantSplit;
        format.CantSplit = row.CantSplit;
        format.HasRepeatHeader = row.HasTableHeader;
        format.RepeatHeader = row.TableHeader;
        return format;
    }

    /// <summary>
    /// Replaces a row's direct format model.
    /// </summary>
    /// <param name="row">The row to edit.</param>
    /// <param name="format">The replacement format.</param>
    public void SetRowFormat(DocxTableRow row, DocxTableRowFormat format)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        row.Height = format.Height;
        row.HeightRule = format.HeightRule;
        row.HasCantSplit = format.HasCantSplit;
        row.CantSplit = format.CantSplit;
        row.HasTableHeader = format.HasRepeatHeader;
        row.TableHeader = format.RepeatHeader;
        row.ClearRawProperties();
    }

    /// <summary>
    /// Returns whether a cell has direct formatting.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasCellFormat(DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        return cell.HasAnyFormatting();
    }

    /// <summary>
    /// Returns a cell's direct format model.
    /// </summary>
    /// <param name="cell">The cell to inspect.</param>
    /// <returns>The cell format model.</returns>
    public DocxTableCellFormat GetCellFormat(DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        return cell.CreateFormat();
    }

    /// <summary>
    /// Replaces a cell's direct format model.
    /// </summary>
    /// <param name="cell">The cell to edit.</param>
    /// <param name="format">The replacement format.</param>
    public void SetCellFormat(DocxTableCell cell, DocxTableCellFormat format)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        cell.ApplyFormat(format);
    }

    /// <summary>
    /// Returns a row format from a table by position.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <returns>The row format model.</returns>
    public DocxTableRowFormat GetTableRowFormat(DocxTable table, int rowIndex)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        return GetRowFormat(table.GetRow(rowIndex));
    }

    /// <summary>
    /// Applies a row format and marks its containing table as changed.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="format">The replacement format.</param>
    public void SetTableRowFormat(DocxTable table, int rowIndex, DocxTableRowFormat format)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        SetRowFormat(table.GetRow(rowIndex), format);
        table.Xml = "";
    }

    /// <summary>
    /// Returns a cell format from a table by position.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The cell format model.</returns>
    public DocxTableCellFormat GetTableCellFormat(DocxTable table, int rowIndex, int cellIndex)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        return GetCellFormat(table.GetRow(rowIndex).GetCell(cellIndex));
    }

    /// <summary>
    /// Applies a cell format and marks its containing row and table as changed.
    /// </summary>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <param name="format">The replacement format.</param>
    public void SetTableCellFormat(DocxTable table, int rowIndex, int cellIndex, DocxTableCellFormat format)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        DocxTableRow row = table.GetRow(rowIndex);
        SetCellFormat(row.GetCell(cellIndex), format);
        row.Xml = "";
        table.Xml = "";
    }
}
