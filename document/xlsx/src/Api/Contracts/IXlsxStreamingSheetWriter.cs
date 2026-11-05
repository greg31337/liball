using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingSheetWriter"/>.
/// </summary>
public interface IXlsxStreamingSheetWriter
{
    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingSheetWriteResult BeginStream(Stream stream);
    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingSheetWriteResult BeginStreamWithOptions(Stream stream, XlsxStreamingSheetWriterOptions options);
}
