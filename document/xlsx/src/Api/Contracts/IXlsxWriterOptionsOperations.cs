using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWriterOptions"/> data.
/// </summary>
public interface IXlsxWriterOptionsOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWriterOptions"/>.Validate through the Validate operation.
    /// </summary>
    /// <param name="data">The xlsx writer options data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxValidationResult Validate(XlsxWriterOptions data);
}
