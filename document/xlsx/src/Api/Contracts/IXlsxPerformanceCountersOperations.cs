using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxPerformanceCounters"/> data.
/// </summary>
public interface IXlsxPerformanceCountersOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxPerformanceCounters"/>.Reset through the Reset operation.
    /// </summary>
    /// <param name="data">The xlsx performance counters data to operate on.</param>
    /// <param name="operation">The operation value used by the operation.</param>
    void Reset(XlsxPerformanceCounters data, string operation);
    /// <summary>
    /// Invokes <see cref="XlsxPerformanceCounters"/>.Finish through the Finish operation.
    /// </summary>
    /// <param name="data">The xlsx performance counters data to operate on.</param>
    /// <param name="startTicks">The start ticks value used by the operation.</param>
    void Finish(XlsxPerformanceCounters data, long startTicks);
}
