using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxReaderOptions"/> data.
/// </summary>
public class XlsxReaderOptionsOperations : IXlsxReaderOptionsOperations
{
    /// <summary>
    /// Initializes a new xlsx reader options operations instance.
    /// </summary>
    public XlsxReaderOptionsOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxReaderOptions"/>.Validate through the Validate operation.
    /// </summary>
    /// <param name="data">The xlsx reader options data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxValidationResult Validate(XlsxReaderOptions data)
    {
        return data.Validate();
    }
}
