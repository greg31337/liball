using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported formula calculation value kind.
/// </summary>
public enum XlsxFormulaCalculationValueKind
{
    /// <summary>
    /// Selects blank for formula calculation value kind operations.
    /// </summary>
    Blank = 0,
    /// <summary>
    /// Selects number for formula calculation value kind operations.
    /// </summary>
    Number = 1,
    /// <summary>
    /// Selects text for formula calculation value kind operations.
    /// </summary>
    Text = 2,
    /// <summary>
    /// Selects boolean for formula calculation value kind operations.
    /// </summary>
    Boolean = 3,
    /// <summary>
    /// Selects error for formula calculation value kind operations.
    /// </summary>
    Error = 4,
    /// <summary>
    /// Selects range for formula calculation value kind operations.
    /// </summary>
    Range = 5,
    /// <summary>
    /// Selects lambda for formula calculation value kind operations.
    /// </summary>
    Lambda = 6
}
