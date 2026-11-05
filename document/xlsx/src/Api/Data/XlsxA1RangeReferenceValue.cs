using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents a1 range reference value data used by the public workbook API.
/// </summary>
public class XlsxA1RangeReferenceValue
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the first value used by this model.
    /// </summary>
    public XlsxCellReferenceValue First;
    /// <summary>
    /// Stores the last value used by this model.
    /// </summary>
    public XlsxCellReferenceValue Last;
    /// <summary>
    /// Indicates whether last is present and should be written.
    /// </summary>
    public bool HasLast;
    /// <summary>
    /// Initializes a new a1 range reference value with its default state.
    /// </summary>
    public XlsxA1RangeReferenceValue()
    {
        Text = "";
        First = new XlsxCellReferenceValue();
        Last = new XlsxCellReferenceValue();
        HasLast = false;
    }
}
