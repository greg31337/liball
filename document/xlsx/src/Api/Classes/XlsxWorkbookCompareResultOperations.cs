using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbookCompareResult"/> data.
/// </summary>
public class XlsxWorkbookCompareResultOperations : IXlsxWorkbookCompareResultOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook compare result operations instance.
    /// </summary>
    public XlsxWorkbookCompareResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookCompareResult"/>.AddDifference through the AddDifference operation.
    /// </summary>
    /// <param name="data">The xlsx workbook compare result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="path">The path value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <param name="leftValue">The left value value used by the operation.</param>
    /// <param name="rightValue">The right value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookDifference AddDifference(XlsxWorkbookCompareResult data, string code, string path, string message, string leftValue, string rightValue)
    {
        return data.AddDifference(code, path, message, leftValue, rightValue);
    }
}
