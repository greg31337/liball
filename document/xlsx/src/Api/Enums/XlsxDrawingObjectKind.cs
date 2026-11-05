using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Identifies the payload stored by a worksheet drawing anchor.
/// </summary>
public enum XlsxDrawingObjectKind
{
    /// <summary>The payload is not interpreted by the library.</summary>
    Unknown = 0,
    /// <summary>The payload is a text box.</summary>
    TextBox = 1,
    /// <summary>The payload is an automatic shape.</summary>
    Shape = 2,
    /// <summary>The payload is a connector.</summary>
    Connector = 3,
    /// <summary>The payload is a form control reference.</summary>
    FormControl = 4,
    /// <summary>The payload is a diagram or SmartArt graphic.</summary>
    Diagram = 5
}
