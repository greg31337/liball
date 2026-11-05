using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents style border setting data used by the public workbook API.
/// </summary>
public class XlsxStyleBorderSetting
{
    /// <summary>
    /// Stores the side value used by this model.
    /// </summary>
    public string Side;
    /// <summary>
    /// Stores the style value used by this model.
    /// </summary>
    public string Style;
    /// <summary>
    /// Stores the rgb value used by this model.
    /// </summary>
    public string Rgb;
    /// <summary>
    /// Initializes a new style border setting with its default state.
    /// </summary>
    public XlsxStyleBorderSetting()
    {
        Side = "";
        Style = "";
        Rgb = "";
    }
}
