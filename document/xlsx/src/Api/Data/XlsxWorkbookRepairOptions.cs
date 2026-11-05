using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of workbook repair operations.
/// </summary>
public class XlsxWorkbookRepairOptions
{
    /// <summary>
    /// Stores the repair package value used by this model.
    /// </summary>
    public bool RepairPackage;
    /// <summary>
    /// Contains repair relationships.
    /// </summary>
    public bool RepairRelationships;
    /// <summary>
    /// Contains repair dimensions.
    /// </summary>
    public bool RepairDimensions;
    /// <summary>
    /// Contains repair shared strings.
    /// </summary>
    public bool RepairSharedStrings;
    /// <summary>
    /// Contains repair styles.
    /// </summary>
    public bool RepairStyles;
    /// <summary>
    /// Contains repair worksheets.
    /// </summary>
    public bool RepairWorksheets;
    /// <summary>
    /// Contains reader options.
    /// </summary>
    public XlsxReaderOptions ReaderOptions;
    /// <summary>
    /// Contains writer options.
    /// </summary>
    public XlsxWriterOptions WriterOptions;
    /// <summary>
    /// Initializes a new workbook repair options with its default state.
    /// </summary>
    public XlsxWorkbookRepairOptions()
    {
        RepairPackage = true;
        RepairRelationships = true;
        RepairDimensions = true;
        RepairSharedStrings = true;
        RepairStyles = true;
        RepairWorksheets = true;
        ReaderOptions = new XlsxReaderOptions();
        WriterOptions = new XlsxWriterOptions();
    }
}
