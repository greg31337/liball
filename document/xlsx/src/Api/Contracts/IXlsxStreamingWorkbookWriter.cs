using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingWorkbookWriter"/>.
/// </summary>
public interface IXlsxStreamingWorkbookWriter
{
    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult BeginStream(Stream stream);
    /// <summary>
    /// Performs the begin stream operation using the current model state.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult BeginStreamWithOptions(Stream stream, XlsxWriterOptions options);
}
