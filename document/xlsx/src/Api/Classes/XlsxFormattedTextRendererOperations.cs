using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxFormattedTextRenderer"/> operations.
/// </summary>
public class XlsxFormattedTextRendererOperations : IXlsxFormattedTextRendererOperations
{
    /// <summary>
    /// Initializes a new XlsxFormattedTextRendererOperations instance.
    /// </summary>
    public XlsxFormattedTextRendererOperations()
    {
    }

    /// <summary>
    /// Invokes the RenderCellValue form of <see cref="XlsxFormattedTextRenderer"/>.RenderCellValue.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <param name="formatCode">The formatCode value used by the operation.</param>
    /// <param name="use1904DateSystem">The use1904DateSystem value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormattedTextRenderResult RenderCellValue(XlsxCellValue value, string formatCode, bool use1904DateSystem)
    {
        return XlsxFormattedTextRenderer.RenderCellValue(value, formatCode, use1904DateSystem);
    }
}
