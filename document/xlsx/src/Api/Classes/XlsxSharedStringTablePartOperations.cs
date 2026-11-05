using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxSharedStringTablePart"/> data.
/// </summary>
public class XlsxSharedStringTablePartOperations : IXlsxSharedStringTablePartOperations
{
    /// <summary>
    /// Initializes a new xlsx shared string table part operations instance.
    /// </summary>
    public XlsxSharedStringTablePartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetItem through the GetItem operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxSharedStringItem? GetItem(XlsxSharedStringTablePart data, int index)
    {
        return data.GetItem(index);
    }

    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetText through the GetText operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string GetText(XlsxSharedStringTablePart data, int index)
    {
        return data.GetText(index);
    }

    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.GetOrAddText through the GetOrAddText operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int GetOrAddText(XlsxSharedStringTablePart data, string text)
    {
        return data.GetOrAddText(text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.AddTextReference through the AddTextReference operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int AddTextReference(XlsxSharedStringTablePart data, string text)
    {
        return data.AddTextReference(text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxSharedStringTablePart"/>.RecalculateCountsFromReferences through the RecalculateCountsFromReferences operation.
    /// </summary>
    /// <param name="data">The xlsx shared string table part data to operate on.</param>
    public void RecalculateCountsFromReferences(XlsxSharedStringTablePart data)
    {
        data.RecalculateCountsFromReferences();
    }
}
