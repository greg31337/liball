using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxDataValidationWriter"/>.
/// </summary>
public interface IXlsxDataValidationWriter
{
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxDataValidationWriteResult WriteString(XlsxDataValidations validations);
    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxDataValidationWriteResult WriteBytes(XlsxDataValidations validations);
    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "validations">The validations value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxDataValidationWriteResult WriteStream(Stream stream, XlsxDataValidations validations);
}
