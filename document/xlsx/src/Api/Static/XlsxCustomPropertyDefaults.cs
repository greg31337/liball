using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides the default OOXML values used by custom document properties.
/// </summary>
public static class XlsxCustomPropertyDefaults
{
    /// <summary>
    /// Identifies the default format id OOXML value.
    /// </summary>
    public const string DefaultFormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";
}
