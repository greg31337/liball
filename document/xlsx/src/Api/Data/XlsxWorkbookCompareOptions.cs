using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of workbook compare operations.
/// </summary>
public class XlsxWorkbookCompareOptions
{
    /// <summary>
    /// Stores the compare package variant value used by this model.
    /// </summary>
    public bool ComparePackageVariant;
    /// <summary>
    /// Stores the compare workbook structure value used by this model.
    /// </summary>
    public bool CompareWorkbookStructure;
    /// <summary>
    /// Contains compare document properties.
    /// </summary>
    public bool CompareDocumentProperties;
    /// <summary>
    /// Contains compare relationships.
    /// </summary>
    public bool CompareRelationships;
    /// <summary>
    /// Contains compare worksheet cells.
    /// </summary>
    public bool CompareWorksheetCells;
    /// <summary>
    /// Contains compare unknown parts.
    /// </summary>
    public bool CompareUnknownParts;
    /// <summary>
    /// Initializes a new workbook compare options with its default state.
    /// </summary>
    public XlsxWorkbookCompareOptions()
    {
        ComparePackageVariant = true;
        CompareWorkbookStructure = true;
        CompareDocumentProperties = true;
        CompareRelationships = true;
        CompareWorksheetCells = true;
        CompareUnknownParts = true;
    }
}
