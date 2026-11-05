namespace pdf;

internal class PdfTableCellPlacement
{
    internal PdfTableCell Cell = new PdfTableCell(new PdfBlock());
    internal int RowIndex;
    internal int ColumnIndex;
    internal int RowSpan;
    internal int ColumnSpan;

    internal PdfTableCellPlacement(PdfTableCell cell, int rowIndex, int columnIndex, int rowSpan, int columnSpan)
    {
        Cell = cell;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
    }
}
