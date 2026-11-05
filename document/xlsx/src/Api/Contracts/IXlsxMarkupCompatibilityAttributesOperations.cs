using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxMarkupCompatibilityAttributes"/> data.
/// </summary>
public interface IXlsxMarkupCompatibilityAttributesOperations
{
    /// <summary>
    /// Parses preserved markup compatibility alternate content.
    /// </summary>
    /// <param name="xml">The alternate content XML to parse.</param>
    /// <returns>The parsed alternate content and validation diagnostics.</returns>
    XlsxAlternateContent ReadAlternateContentString(string xml);
    /// <summary>
    /// Invokes <see cref="XlsxMarkupCompatibilityAttributes"/>.HasValues through the HasValues operation.
    /// </summary>
    /// <param name="data">The xlsx markup compatibility attributes data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    bool HasValues(XlsxMarkupCompatibilityAttributes data);
}
