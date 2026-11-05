using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingSheetReader"/>.
/// </summary>
public interface IXlsxStreamingSheetReader
{
    /// <summary>
    /// Opens worksheet XML bytes for forward-only row reading.
    /// </summary>
    /// <param name="bytes">The complete worksheet XML bytes.</param>
    /// <returns>The streaming cursor and validation diagnostics.</returns>
    XlsxStreamingSheetOpenResult OpenBytes(byte[] bytes);
}
