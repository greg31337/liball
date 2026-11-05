using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxMarkupCompatibilityAttributes"/> data.
/// </summary>
public class XlsxMarkupCompatibilityAttributesOperations : IXlsxMarkupCompatibilityAttributesOperations
{
    /// <summary>
    /// Initializes a new xlsx markup compatibility attributes operations instance.
    /// </summary>
    public XlsxMarkupCompatibilityAttributesOperations()
    {
    }

    /// <summary>
    /// Parses preserved markup compatibility alternate content.
    /// </summary>
    /// <param name="xml">The alternate content XML to parse.</param>
    /// <returns>The parsed alternate content and validation diagnostics.</returns>
    public XlsxAlternateContent ReadAlternateContentString(string xml)
    {
        return XlsxMarkupCompatibility.ReadAlternateContentString(xml);
    }

    /// <summary>
    /// Invokes <see cref="XlsxMarkupCompatibilityAttributes"/>.HasValues through the HasValues operation.
    /// </summary>
    /// <param name="data">The xlsx markup compatibility attributes data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool HasValues(XlsxMarkupCompatibilityAttributes data)
    {
        return data.HasValues();
    }
}
