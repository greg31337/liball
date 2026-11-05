using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of streaming sheet writer operations.
/// </summary>
public class XlsxStreamingSheetWriterOptions
{
    /// <summary>
    /// Contains use shared strings.
    /// </summary>
    public bool UseSharedStrings;
    /// <summary>
    /// Initializes a new streaming sheet writer options with its default state.
    /// </summary>
    public XlsxStreamingSheetWriterOptions()
    {
        UseSharedStrings = false;
    }
}
