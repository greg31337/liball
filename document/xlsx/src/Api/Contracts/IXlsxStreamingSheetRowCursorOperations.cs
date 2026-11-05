using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxStreamingSheetRowCursor"/> data.
/// </summary>
public interface IXlsxStreamingSheetRowCursorOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxStreamingSheetRowCursor"/>.Read through the Read operation.
    /// </summary>
    /// <param name="data">The xlsx streaming sheet row cursor data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    bool Read(XlsxStreamingSheetRowCursor data);
    /// <summary>
    /// Invokes <see cref="XlsxStreamingSheetRowCursor"/>.Close through the Close operation.
    /// </summary>
    /// <param name="data">The xlsx streaming sheet row cursor data to operate on.</param>
    void Close(XlsxStreamingSheetRowCursor data);
}
