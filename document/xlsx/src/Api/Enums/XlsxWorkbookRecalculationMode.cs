using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported workbook recalculation mode.
/// </summary>
public enum XlsxWorkbookRecalculationMode
{
    /// <summary>
    /// Selects preserve cached values for workbook recalculation mode operations.
    /// </summary>
    PreserveCachedValues = 1,
    /// <summary>
    /// Selects recalculate supported formulas for workbook recalculation mode operations.
    /// </summary>
    RecalculateSupportedFormulas = 2,
    /// <summary>
    /// Selects recalculate changed formulas for workbook recalculation mode operations.
    /// </summary>
    RecalculateChangedFormulas = 3,
    /// <summary>
    /// Selects mark for excel recalculation for workbook recalculation mode operations.
    /// </summary>
    MarkForExcelRecalculation = 4
}
