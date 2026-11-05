using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxWorkbookComparer"/> operations.
/// </summary>
public class XlsxWorkbookComparerOperations : IXlsxWorkbookComparerOperations
{
    /// <summary>
    /// Initializes a new XlsxWorkbookComparerOperations instance.
    /// </summary>
    public XlsxWorkbookComparerOperations()
    {
    }

    /// <summary>
    /// Invokes the Compare form of <see cref="XlsxWorkbookComparer"/>.Compare.
    /// </summary>
    /// <param name="left">The left value used by the operation.</param>
    /// <param name="right">The right value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookCompareResult Compare(XlsxWorkbook? left, XlsxWorkbook? right)
    {
        return XlsxWorkbookComparer.Compare(left, right);
    }

    /// <summary>
    /// Invokes the CompareWithOptions form of <see cref="XlsxWorkbookComparer"/>.Compare.
    /// </summary>
    /// <param name="left">The left value used by the operation.</param>
    /// <param name="right">The right value used by the operation.</param>
    /// <param name="options">The options value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorkbookCompareResult CompareWithOptions(XlsxWorkbook? left, XlsxWorkbook? right, XlsxWorkbookCompareOptions? options)
    {
        return XlsxWorkbookComparer.Compare(left, right, options);
    }
}
