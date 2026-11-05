using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents formula token data used by the public workbook API.
/// </summary>
public class XlsxFormulaToken
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxFormulaTokenKind Kind;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the start value used by this model.
    /// </summary>
    public int Start;
    /// <summary>
    /// Stores the length value used by this model.
    /// </summary>
    public int Length;
    /// <summary>
    /// Initializes a new formula token with its default state.
    /// </summary>
    public XlsxFormulaToken()
    {
        Kind = XlsxFormulaTokenKind.Unknown;
        Text = "";
        Start = 0;
        Length = 0;
    }
}
