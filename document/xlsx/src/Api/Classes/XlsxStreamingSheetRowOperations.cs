using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxStreamingSheetRow"/> data.
/// </summary>
public class XlsxStreamingSheetRowOperations : IXlsxStreamingSheetRowOperations
{
    /// <summary>
    /// Initializes a new xlsx streaming sheet row operations instance.
    /// </summary>
    public XlsxStreamingSheetRowOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxStreamingSheetRow"/>.GetCell through the GetCell operation.
    /// </summary>
    /// <param name="data">The xlsx streaming sheet row data to operate on.</param>
    /// <param name="reference">The reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxStreamingSheetCell? GetCell(XlsxStreamingSheetRow data, string reference)
    {
        return data.GetCell(reference);
    }
}
