using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls package format, compression, preservation behavior, deterministic output, and statistics collection when a workbook is written.
/// </summary>
/// <remarks>
/// Deterministic output stabilizes part ordering and XML output. Preserved unknown parts are included only when preservation remains enabled.
/// </remarks>
public class XlsxWriterOptions
{
    /// <summary>
    /// Contains preserve unknown parts.
    /// </summary>
    public bool PreserveUnknownParts;
    /// <summary>
    /// Stores the preserve unknown xml value used by this model.
    /// </summary>
    public bool PreserveUnknownXml;
    /// <summary>
    /// Stores the deterministic output value used by this model.
    /// </summary>
    public bool DeterministicOutput;
    /// <summary>
    /// Contains use shared strings.
    /// </summary>
    public bool UseSharedStrings;
    /// <summary>
    /// Stores the package variant value used by this model.
    /// </summary>
    public XlsxPackageVariant PackageVariant;
    /// <summary>
    /// Stores the compression level value used by this model.
    /// </summary>
    public CompressionLevel CompressionLevel;
    /// <summary>
    /// Contains max zip entries.
    /// </summary>
    public int MaxZipEntries;
    /// <summary>
    /// Contains max uncompressed bytes.
    /// </summary>
    public long MaxUncompressedBytes;
    /// <summary>
    /// Contains collect statistics.
    /// </summary>
    public bool CollectStatistics;
    /// <summary>
    /// Contains statistics.
    /// </summary>
    public XlsxPerformanceCounters Statistics;
    /// <summary>
    /// Initializes a new writer options with its default state.
    /// </summary>
    public XlsxWriterOptions()
    {
        PreserveUnknownParts = true;
        PreserveUnknownXml = true;
        DeterministicOutput = true;
        UseSharedStrings = true;
        PackageVariant = XlsxPackageVariant.Xlsx;
        CompressionLevel = CompressionLevel.Optimal;
        MaxZipEntries = 10000;
        MaxUncompressedBytes = 2147483648;
        CollectStatistics = false;
        Statistics = new XlsxPerformanceCounters();
    }

    /// <summary>
    /// Checks the current model or options and returns all validation diagnostics found.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult Validate()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (MaxZipEntries <= 0)
        {
            result.AddError("writer.maxZipEntries", "MaxZipEntries must be greater than zero.");
        }

        if (MaxUncompressedBytes <= 0)
        {
            result.AddError("writer.maxUncompressedBytes", "MaxUncompressedBytes must be greater than zero.");
        }

        if (!IsKnownPackageVariant(PackageVariant))
        {
            result.AddError("writer.packageVariant", "PackageVariant is not supported.");
        }

        if (!IsKnownCompressionLevel(CompressionLevel))
        {
            result.AddError("writer.compressionLevel", "CompressionLevel is not supported.");
        }

        return result;
    }

    internal bool IsKnownPackageVariant(XlsxPackageVariant packageVariant)
    {
        if (packageVariant == XlsxPackageVariant.Xlsx)
        {
            return true;
        }

        if (packageVariant == XlsxPackageVariant.Xlsm)
        {
            return true;
        }

        if (packageVariant == XlsxPackageVariant.Xltx)
        {
            return true;
        }

        if (packageVariant == XlsxPackageVariant.Xltm)
        {
            return true;
        }

        return false;
    }

    internal bool IsKnownCompressionLevel(CompressionLevel compressionLevel)
    {
        if (compressionLevel == CompressionLevel.NoCompression)
        {
            return true;
        }

        if (compressionLevel == CompressionLevel.Fastest)
        {
            return true;
        }

        if (compressionLevel == CompressionLevel.Optimal)
        {
            return true;
        }

        if (compressionLevel == CompressionLevel.SmallestSize)
        {
            return true;
        }

        return false;
    }
}
