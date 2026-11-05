using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxContentTypesPart"/> data.
/// </summary>
public interface IXlsxContentTypesPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetDefault through the GetDefault operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="extension">The extension value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxContentTypeDefault? GetDefault(XlsxContentTypesPart data, string extension);
    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetOverride through the GetOverride operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="partName">The part name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxContentTypeOverride? GetOverride(XlsxContentTypesPart data, string partName);
    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetContentType through the GetContentType operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="partName">The part name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    string? GetContentType(XlsxContentTypesPart data, string partName);
}
