using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Identifies how a drawing object is positioned on a worksheet.
/// </summary>
public enum XlsxDrawingAnchorKind
{
    /// <summary>The object is bounded by two worksheet cell markers.</summary>
    TwoCell = 0,
    /// <summary>The object starts at one cell marker and has an explicit extent.</summary>
    OneCell = 1,
    /// <summary>The object has an absolute position and extent in English metric units.</summary>
    Absolute = 2
}
