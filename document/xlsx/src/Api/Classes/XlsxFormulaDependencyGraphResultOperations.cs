using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxFormulaDependencyGraphResult"/> data.
/// </summary>
public class XlsxFormulaDependencyGraphResultOperations : IXlsxFormulaDependencyGraphResultOperations
{
    /// <summary>
    /// Initializes a new xlsx formula dependency graph result operations instance.
    /// </summary>
    public XlsxFormulaDependencyGraphResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaDependencyGraphResult"/>.GetNode through the GetNode operation.
    /// </summary>
    /// <param name="data">The xlsx formula dependency graph result data to operate on.</param>
    /// <param name="cellReference">The cell reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaDependencyNode? GetNode(XlsxFormulaDependencyGraphResult data, string cellReference)
    {
        return data.GetNode(cellReference);
    }
}
