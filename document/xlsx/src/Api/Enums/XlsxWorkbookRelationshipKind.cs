using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported workbook relationship kind.
/// </summary>
public enum XlsxWorkbookRelationshipKind
{
    /// <summary>
    /// Selects unknown for workbook relationship kind operations.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects worksheet for workbook relationship kind operations.
    /// </summary>
    Worksheet = 1,
    /// <summary>
    /// Selects chartsheet for workbook relationship kind operations.
    /// </summary>
    Chartsheet = 2,
    /// <summary>
    /// Selects dialogsheet for workbook relationship kind operations.
    /// </summary>
    Dialogsheet = 3,
    /// <summary>
    /// Selects macrosheet for workbook relationship kind operations.
    /// </summary>
    Macrosheet = 4,
    /// <summary>
    /// Selects theme for workbook relationship kind operations.
    /// </summary>
    Theme = 5,
    /// <summary>
    /// Selects styles for workbook relationship kind operations.
    /// </summary>
    Styles = 6,
    /// <summary>
    /// Selects shared strings for workbook relationship kind operations.
    /// </summary>
    SharedStrings = 7,
    /// <summary>
    /// Selects external link for workbook relationship kind operations.
    /// </summary>
    ExternalLink = 8,
    /// <summary>
    /// Selects pivot cache for workbook relationship kind operations.
    /// </summary>
    PivotCache = 9,
    /// <summary>
    /// Selects vba project for workbook relationship kind operations.
    /// </summary>
    VbaProject = 10,
    /// <summary>
    /// Selects connections for workbook relationship kind operations.
    /// </summary>
    Connections = 11
}
