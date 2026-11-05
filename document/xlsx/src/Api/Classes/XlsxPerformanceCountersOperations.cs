using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxPerformanceCounters"/> data.
/// </summary>
public class XlsxPerformanceCountersOperations : IXlsxPerformanceCountersOperations
{
    /// <summary>
    /// Initializes a new xlsx performance counters operations instance.
    /// </summary>
    public XlsxPerformanceCountersOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxPerformanceCounters"/>.Reset through the Reset operation.
    /// </summary>
    /// <param name="data">The xlsx performance counters data to operate on.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    public void Reset(XlsxPerformanceCounters data, string operation)
    {
        data.Reset(operation);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPerformanceCounters"/>.Finish through the Finish operation.
    /// </summary>
    /// <param name="data">The xlsx performance counters data to operate on.</param>
    /// <param name="startTicks">The start ticks value used by the operation.</param>
    public void Finish(XlsxPerformanceCounters data, long startTicks)
    {
        data.Finish(startTicks);
    }
}
