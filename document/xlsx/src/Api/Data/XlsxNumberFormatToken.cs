using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents number format token data used by the public workbook API.
/// </summary>
public class XlsxNumberFormatToken
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxNumberFormatTokenKind Kind;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Initializes a new number format token with its default state.
    /// </summary>
    public XlsxNumberFormatToken()
    {
        Kind = XlsxNumberFormatTokenKind.Literal;
        Text = "";
    }
}
