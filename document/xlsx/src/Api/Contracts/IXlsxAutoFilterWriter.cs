using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxAutoFilterWriter"/>.
/// </summary>
public interface IXlsxAutoFilterWriter
{
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxAutoFilterWriteResult WriteString(XlsxAutoFilter filter);
    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxAutoFilterWriteResult WriteBytes(XlsxAutoFilter filter);
    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "filter">The filter value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxAutoFilterWriteResult WriteStream(Stream stream, XlsxAutoFilter filter);
    /// <summary>
    /// Validates and writes sort state string in the requested representation.
    /// </summary>
    /// <param name = "sortState">The sort state value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxAutoFilterWriteResult WriteSortStateString(XlsxSortState sortState);
}
