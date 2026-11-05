using System.Collections.Generic;

namespace pdf;

internal class PdfTableGroupLayout
{
    internal double[] RowHeights = new double[0];
    internal List<PdfTableCellPlacement> Placements = new List<PdfTableCellPlacement>();
    internal bool Fits;

    internal PdfTableGroupLayout(int rowCount)
    {
        RowHeights = new double[rowCount];
        Placements = new List<PdfTableCellPlacement>();
        Fits = true;
    }
}
