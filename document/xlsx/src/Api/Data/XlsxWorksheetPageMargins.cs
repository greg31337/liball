using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet page margins data used by the public workbook API.
/// </summary>
public class XlsxWorksheetPageMargins
{
    /// <summary>
    /// Stores the left value used by this model.
    /// </summary>
    public double Left;
    /// <summary>
    /// Stores the right value used by this model.
    /// </summary>
    public double Right;
    /// <summary>
    /// Stores the top value used by this model.
    /// </summary>
    public double Top;
    /// <summary>
    /// Stores the bottom value used by this model.
    /// </summary>
    public double Bottom;
    /// <summary>
    /// Stores the header value used by this model.
    /// </summary>
    public double Header;
    /// <summary>
    /// Stores the footer value used by this model.
    /// </summary>
    public double Footer;
    /// <summary>
    /// Initializes a new worksheet page margins with its default state.
    /// </summary>
    public XlsxWorksheetPageMargins()
    {
        Left = 0;
        Right = 0;
        Top = 0;
        Bottom = 0;
        Header = 0;
        Footer = 0;
    }
}
