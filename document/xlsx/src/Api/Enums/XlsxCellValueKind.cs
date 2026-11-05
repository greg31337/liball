using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported cell value kind.
/// </summary>
public enum XlsxCellValueKind
{
    /// <summary>
    /// Selects blank for cell value kind operations.
    /// </summary>
    Blank = 0,
    /// <summary>
    /// Selects number for cell value kind operations.
    /// </summary>
    Number = 1,
    /// <summary>
    /// Selects text for cell value kind operations.
    /// </summary>
    Text = 2,
    /// <summary>
    /// Selects shared string for cell value kind operations.
    /// </summary>
    SharedString = 3,
    /// <summary>
    /// Selects inline string for cell value kind operations.
    /// </summary>
    InlineString = 4,
    /// <summary>
    /// Selects boolean for cell value kind operations.
    /// </summary>
    Boolean = 5,
    /// <summary>
    /// Selects error for cell value kind operations.
    /// </summary>
    Error = 6,
    /// <summary>
    /// Selects date time for cell value kind operations.
    /// </summary>
    DateTime = 7,
    /// <summary>
    /// Selects formula cached value for cell value kind operations.
    /// </summary>
    FormulaCachedValue = 8,
    /// <summary>
    /// Selects rich text for cell value kind operations.
    /// </summary>
    RichText = 9,
    /// <summary>
    /// Selects unknown raw value for cell value kind operations.
    /// </summary>
    UnknownRawValue = 10,
    /// <summary>
    /// Selects an exact decimal number for cell value operations.
    /// </summary>
    Decimal = 11,
    /// <summary>
    /// Selects an elapsed duration stored as an Excel day fraction.
    /// </summary>
    Duration = 12
}
