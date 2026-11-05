using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents number format section data used by the public workbook API.
/// </summary>
public class XlsxNumberFormatSection
{
    /// <summary>
    /// Stores the format code value used by this model.
    /// </summary>
    public string FormatCode;
    /// <summary>
    /// Stores the color value used by this model.
    /// </summary>
    public string Color;
    /// <summary>
    /// Indicates whether color is present and should be written.
    /// </summary>
    public bool HasColor;
    /// <summary>
    /// Stores the condition value used by this model.
    /// </summary>
    public string Condition;
    /// <summary>
    /// Indicates whether condition is present and should be written.
    /// </summary>
    public bool HasCondition;
    /// <summary>
    /// Contains tokens.
    /// </summary>
    public List<XlsxNumberFormatToken> Tokens;
    /// <summary>
    /// Initializes a new number format section with its default state.
    /// </summary>
    public XlsxNumberFormatSection()
    {
        FormatCode = "";
        Color = "";
        HasColor = false;
        Condition = "";
        HasCondition = false;
        Tokens = new List<XlsxNumberFormatToken>();
    }
}
