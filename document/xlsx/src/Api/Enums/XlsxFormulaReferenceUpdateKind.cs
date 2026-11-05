using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported formula reference update kind.
/// </summary>
public enum XlsxFormulaReferenceUpdateKind
{
    /// <summary>
    /// Selects row insert for formula reference update kind operations.
    /// </summary>
    RowInsert = 1,
    /// <summary>
    /// Selects row delete for formula reference update kind operations.
    /// </summary>
    RowDelete = 2,
    /// <summary>
    /// Selects column insert for formula reference update kind operations.
    /// </summary>
    ColumnInsert = 3,
    /// <summary>
    /// Selects column delete for formula reference update kind operations.
    /// </summary>
    ColumnDelete = 4,
    /// <summary>
    /// Selects move for formula reference update kind operations.
    /// </summary>
    Move = 5,
    /// <summary>
    /// Selects copy for formula reference update kind operations.
    /// </summary>
    Copy = 6,
    /// <summary>
    /// Selects sheet rename for formula reference update kind operations.
    /// </summary>
    SheetRename = 7
}
