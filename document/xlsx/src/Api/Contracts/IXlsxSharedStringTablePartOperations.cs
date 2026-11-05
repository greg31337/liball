using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxSharedStringTablePart"/> data.
/// </summary>
public interface IXlsxSharedStringTablePartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetItem through the GetItem operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxSharedStringItem? GetItem(XlsxSharedStringTablePart data, int index);
    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetText through the GetText operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    string GetText(XlsxSharedStringTablePart data, int index);
    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetOrAddText through the GetOrAddText operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    int GetOrAddText(XlsxSharedStringTablePart data, string text);
    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.AddTextReference through the AddTextReference operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    int AddTextReference(XlsxSharedStringTablePart data, string text);
    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.RecalculateCountsFromReferences through the RecalculateCountsFromReferences operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    void RecalculateCountsFromReferences(XlsxSharedStringTablePart data);
}
