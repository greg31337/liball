using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook difference data used by the public workbook API.
/// </summary>
public class XlsxWorkbookDifference
{
    /// <summary>
    /// Stores the code value used by this model.
    /// </summary>
    public string Code;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string Path;
    /// <summary>
    /// Stores the message value used by this model.
    /// </summary>
    public string Message;
    /// <summary>
    /// Stores the left value value used by this model.
    /// </summary>
    public string LeftValue;
    /// <summary>
    /// Stores the right value value used by this model.
    /// </summary>
    public string RightValue;
    /// <summary>
    /// Initializes a new workbook difference with its default state.
    /// </summary>
    public XlsxWorkbookDifference()
    {
        Code = "";
        Path = "";
        Message = "";
        LeftValue = "";
        RightValue = "";
    }
}
