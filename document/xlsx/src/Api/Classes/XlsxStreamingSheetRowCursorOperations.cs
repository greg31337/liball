using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxStreamingSheetRowCursor"/> data.
/// </summary>
public class XlsxStreamingSheetRowCursorOperations : IXlsxStreamingSheetRowCursorOperations
{
    /// <summary>
    /// Initializes a new xlsx streaming sheet row cursor operations instance.
    /// </summary>
    public XlsxStreamingSheetRowCursorOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxStreamingSheetRowCursor"/>.Read through the Read operation.
    /// </summary>
    /// <param name="data">The xlsx streaming sheet row cursor data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool Read(XlsxStreamingSheetRowCursor data)
    {
        return data.Read();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStreamingSheetRowCursor"/>.Close through the Close operation.
    /// </summary>
    /// <param name="data">The xlsx streaming sheet row cursor data to operate on.</param>
    public void Close(XlsxStreamingSheetRowCursor data)
    {
        data.Close();
    }
}
