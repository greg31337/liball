using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported number format token kind.
/// </summary>
public enum XlsxNumberFormatTokenKind
{
    /// <summary>
    /// Selects literal for number format token kind operations.
    /// </summary>
    Literal = 0,
    /// <summary>
    /// Selects number placeholder for number format token kind operations.
    /// </summary>
    NumberPlaceholder = 1,
    /// <summary>
    /// Selects decimal point for number format token kind operations.
    /// </summary>
    DecimalPoint = 2,
    /// <summary>
    /// Selects thousands separator for number format token kind operations.
    /// </summary>
    ThousandsSeparator = 3,
    /// <summary>
    /// Selects percent for number format token kind operations.
    /// </summary>
    Percent = 4,
    /// <summary>
    /// Selects exponent for number format token kind operations.
    /// </summary>
    Exponent = 5,
    /// <summary>
    /// Selects date time code for number format token kind operations.
    /// </summary>
    DateTimeCode = 6,
    /// <summary>
    /// Selects text placeholder for number format token kind operations.
    /// </summary>
    TextPlaceholder = 7,
    /// <summary>
    /// Selects fill for number format token kind operations.
    /// </summary>
    Fill = 8,
    /// <summary>
    /// Selects spacing for number format token kind operations.
    /// </summary>
    Spacing = 9
}
