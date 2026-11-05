using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxCustomPropertiesPart"/> data.
/// </summary>
public class XlsxCustomPropertiesPartOperations : IXlsxCustomPropertiesPartOperations
{
    /// <summary>
    /// Initializes a new xlsx custom properties part operations instance.
    /// </summary>
    public XlsxCustomPropertiesPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxCustomPropertiesPart"/>.GetProperty through the GetProperty operation.
    /// </summary>
    /// <param name="data">The xlsx custom properties part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxCustomProperty? GetProperty(XlsxCustomPropertiesPart data, string name)
    {
        return data.GetProperty(name);
    }
}
