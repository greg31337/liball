using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal class ExternalCodeXlsxSheetReadState
{
    internal bool SawWorksheet;
    internal bool InSheetData;
    internal ExternalCodeXlsxRow? CurrentRow;
    internal ExternalCodeXlsxCell? CurrentCell;
    internal int CaptureDepth = -1;
    internal int CaptureKind;
    internal StringBuilder CaptureText = new StringBuilder();
    internal HashSet<int> Columns = new HashSet<int>();
    internal ExternalCodeXlsxSheet Sheet = new ExternalCodeXlsxSheet();
}
