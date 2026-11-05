using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported formula token kind.
/// </summary>
public enum XlsxFormulaTokenKind
{
    /// <summary>
    /// Selects unknown for formula token kind operations.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects whitespace for formula token kind operations.
    /// </summary>
    Whitespace = 1,
    /// <summary>
    /// Selects number for formula token kind operations.
    /// </summary>
    Number = 2,
    /// <summary>
    /// Selects string for formula token kind operations.
    /// </summary>
    String = 3,
    /// <summary>
    /// Selects boolean for formula token kind operations.
    /// </summary>
    Boolean = 4,
    /// <summary>
    /// Selects error for formula token kind operations.
    /// </summary>
    Error = 5,
    /// <summary>
    /// Selects identifier for formula token kind operations.
    /// </summary>
    Identifier = 6,
    /// <summary>
    /// Selects cell reference for formula token kind operations.
    /// </summary>
    CellReference = 7,
    /// <summary>
    /// Selects sheet name for formula token kind operations.
    /// </summary>
    SheetName = 8,
    /// <summary>
    /// Selects structured reference for formula token kind operations.
    /// </summary>
    StructuredReference = 9,
    /// <summary>
    /// Selects operator for formula token kind operations.
    /// </summary>
    Operator = 10,
    /// <summary>
    /// Selects range operator for formula token kind operations.
    /// </summary>
    RangeOperator = 11,
    /// <summary>
    /// Selects union operator for formula token kind operations.
    /// </summary>
    UnionOperator = 12,
    /// <summary>
    /// Selects open paren for formula token kind operations.
    /// </summary>
    OpenParen = 13,
    /// <summary>
    /// Selects close paren for formula token kind operations.
    /// </summary>
    CloseParen = 14,
    /// <summary>
    /// Selects array open for formula token kind operations.
    /// </summary>
    ArrayOpen = 15,
    /// <summary>
    /// Selects array close for formula token kind operations.
    /// </summary>
    ArrayClose = 16,
    /// <summary>
    /// Selects argument separator for formula token kind operations.
    /// </summary>
    ArgumentSeparator = 17,
    /// <summary>
    /// Selects bang for formula token kind operations.
    /// </summary>
    Bang = 18,
    /// <summary>
    /// Selects percent for formula token kind operations.
    /// </summary>
    Percent = 19
}
