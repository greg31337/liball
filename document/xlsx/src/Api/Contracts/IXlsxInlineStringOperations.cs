using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxInlineString"/> data.
/// </summary>
public interface IXlsxInlineStringOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxInlineString"/>.CreateText through the CreateText operation.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxInlineString CreateText(string text);
}
