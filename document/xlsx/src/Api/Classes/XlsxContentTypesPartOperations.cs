using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxContentTypesPart"/> data.
/// </summary>
public class XlsxContentTypesPartOperations : IXlsxContentTypesPartOperations
{
    /// <summary>
    /// Initializes a new xlsx content types part operations instance.
    /// </summary>
    public XlsxContentTypesPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetDefault through the GetDefault operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="extension">The extension value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxContentTypeDefault? GetDefault(XlsxContentTypesPart data, string extension)
    {
        return data.GetDefault(extension);
    }

    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetOverride through the GetOverride operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="partName">The part name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxContentTypeOverride? GetOverride(XlsxContentTypesPart data, string partName)
    {
        return data.GetOverride(partName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxContentTypesPart"/>.GetContentType through the GetContentType operation.
    /// </summary>
    /// <param name="data">The xlsx content types part data to operate on.</param>
    /// <param name="partName">The part name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string? GetContentType(XlsxContentTypesPart data, string partName)
    {
        return data.GetContentType(partName);
    }
}
