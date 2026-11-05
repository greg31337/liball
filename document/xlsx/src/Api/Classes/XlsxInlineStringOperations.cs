using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxInlineString"/> data.
/// </summary>
public class XlsxInlineStringOperations : IXlsxInlineStringOperations
{
    /// <summary>
    /// Initializes a new xlsx inline string operations instance.
    /// </summary>
    public XlsxInlineStringOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxInlineString"/>.CreateText through the CreateText operation.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxInlineString CreateText(string text)
    {
        return XlsxInlineString.CreateText(text);
    }
}
