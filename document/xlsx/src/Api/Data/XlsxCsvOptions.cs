using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of csv operations.
/// </summary>
public class XlsxCsvOptions
{
    /// <summary>
    /// Stores the delimiter value used by this model.
    /// </summary>
    public char Delimiter;
    /// <summary>
    /// Stores the quote value used by this model.
    /// </summary>
    public char Quote;
    /// <summary>
    /// Stores the new line value used by this model.
    /// </summary>
    public string NewLine;
    /// <summary>
    /// Initializes a new csv options with its default state.
    /// </summary>
    public XlsxCsvOptions()
    {
        Delimiter = ',';
        Quote = '"';
        NewLine = "\r\n";
    }
}
