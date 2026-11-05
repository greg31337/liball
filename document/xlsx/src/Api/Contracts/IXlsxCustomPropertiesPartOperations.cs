using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxCustomPropertiesPart"/> data.
/// </summary>
public interface IXlsxCustomPropertiesPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxCustomPropertiesPart"/>.GetProperty through the GetProperty operation.
    /// </summary>
    /// <param name="data">The xlsx custom properties part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCustomProperty? GetProperty(XlsxCustomPropertiesPart data, string name);
}
