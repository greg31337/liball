using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxPackagePathResult"/> data.
/// </summary>
public class XlsxPackagePathResultOperations : IXlsxPackagePathResultOperations
{
    /// <summary>
    /// Initializes a new xlsx package path result operations instance.
    /// </summary>
    public XlsxPackagePathResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxPackagePathResult"/>.IsValid through the IsValid operation.
    /// </summary>
    /// <param name="data">The xlsx package path result data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool IsValid(XlsxPackagePathResult data)
    {
        return data.IsValid();
    }
}
