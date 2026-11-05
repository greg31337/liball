using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents performance counters data used by the public workbook API.
/// </summary>
public class XlsxPerformanceCounters
{
    /// <summary>
    /// Stores the enabled value used by this model.
    /// </summary>
    public bool Enabled;
    /// <summary>
    /// Stores the operation value used by this model.
    /// </summary>
    public string Operation;
    /// <summary>
    /// Contains elapsed milliseconds.
    /// </summary>
    public long ElapsedMilliseconds;
    /// <summary>
    /// Contains input bytes.
    /// </summary>
    public long InputBytes;
    /// <summary>
    /// Contains output bytes.
    /// </summary>
    public long OutputBytes;
    /// <summary>
    /// Stores the number of package entry items processed by the operation.
    /// </summary>
    public int PackageEntryCount;
    /// <summary>
    /// Contains total compressed bytes.
    /// </summary>
    public long TotalCompressedBytes;
    /// <summary>
    /// Contains total uncompressed bytes.
    /// </summary>
    public long TotalUncompressedBytes;
    /// <summary>
    /// Stores the number of worksheet items processed by the operation.
    /// </summary>
    public int WorksheetCount;
    /// <summary>
    /// Stores the number of row items processed by the operation.
    /// </summary>
    public int RowCount;
    /// <summary>
    /// Stores the number of cell items processed by the operation.
    /// </summary>
    public int CellCount;
    /// <summary>
    /// Stores the number of formula items processed by the operation.
    /// </summary>
    public int FormulaCount;
    /// <summary>
    /// Stores the number of relationship items processed by the operation.
    /// </summary>
    public int RelationshipCount;
    /// <summary>
    /// Stores the number of unknown part items processed by the operation.
    /// </summary>
    public int UnknownPartCount;
    /// <summary>
    /// Stores the number of style items processed by the operation.
    /// </summary>
    public int StyleCount;
    /// <summary>
    /// Stores the number of shared string items processed by the operation.
    /// </summary>
    public int SharedStringCount;
    /// <summary>
    /// Initializes a new performance counters with its default state.
    /// </summary>
    public XlsxPerformanceCounters()
    {
        Enabled = false;
        Operation = "";
        ElapsedMilliseconds = 0;
        InputBytes = 0;
        OutputBytes = 0;
        PackageEntryCount = 0;
        TotalCompressedBytes = 0;
        TotalUncompressedBytes = 0;
        WorksheetCount = 0;
        RowCount = 0;
        CellCount = 0;
        FormulaCount = 0;
        RelationshipCount = 0;
        UnknownPartCount = 0;
        StyleCount = 0;
        SharedStringCount = 0;
    }

    /// <summary>
    /// Performs the reset operation using the current model state.
    /// </summary>
    /// <param name = "operation">The operation value used by the operation.</param>
    internal void Reset(string operation)
    {
        Enabled = true;
        Operation = XlsxCellValueConverter.NormalizeText(operation);
        ElapsedMilliseconds = 0;
        InputBytes = 0;
        OutputBytes = 0;
        PackageEntryCount = 0;
        TotalCompressedBytes = 0;
        TotalUncompressedBytes = 0;
        WorksheetCount = 0;
        RowCount = 0;
        CellCount = 0;
        FormulaCount = 0;
        RelationshipCount = 0;
        UnknownPartCount = 0;
        StyleCount = 0;
        SharedStringCount = 0;
    }

    /// <summary>
    /// Performs the finish operation using the current model state.
    /// </summary>
    /// <param name = "startTicks">The start ticks value used by the operation.</param>
    internal void Finish(long startTicks)
    {
        ElapsedMilliseconds = System.Environment.TickCount64 - startTicks;
        if (ElapsedMilliseconds < 0)
        {
            ElapsedMilliseconds = 0;
        }
    }

    internal static XlsxPerformanceCounters Start(XlsxPerformanceCounters counters, string operation)
    {
        XlsxPerformanceCounters result = counters;
        if (result == null)
        {
            result = new XlsxPerformanceCounters();
        }

        result.Reset(operation);
        return result;
    }
}
