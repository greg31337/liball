using System.Collections.Generic;

namespace iso20022;

internal class ExternalCodeXlsxRow
{
    internal int RowNumber;
    internal List<ExternalCodeXlsxCell> Cells = new List<ExternalCodeXlsxCell>();
}
