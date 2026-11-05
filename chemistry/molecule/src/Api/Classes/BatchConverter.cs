using System.IO;

namespace molecule;


/// <summary>
/// Provides batch converter data and operations for molecular graphs.
/// </summary>
public class BatchConverter : IBatchConverter
{
    internal IMoleculeFormatService Formats;

    /// <summary>
    /// Initializes a streaming molecule format converter.
    /// </summary>
    public BatchConverter()
    {
        Formats = new MoleculeFormats();
    }

    /// <summary>
    /// Configures the format service used by subsequent conversions.
    /// </summary>
    /// <param name="formats">The format service used by the input and output streams.</param>
    public void ConfigureFormats(IMoleculeFormatService? formats)
    {
        if (formats == null)
        {
            Formats = new MoleculeFormats();
            return;
        }

        Formats = formats;
    }

    /// <summary>
    /// Creates a streaming converter with an explicit format service.
    /// </summary>
    /// <param name="formats">The format service used by the input and output streams.</param>
    /// <returns>A converter that uses the supplied format service.</returns>
    internal static BatchConverter Create(IMoleculeFormatService? formats)
    {
        BatchConverter converter = new BatchConverter();
        converter.ConfigureFormats(formats);
        return converter;
    }

    /// <summary>
    /// Reads molecules from one text stream, writes them in another format, and returns conversion totals and diagnostics.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="inputOptions">The options used to read the input.</param>
    /// <param name="outputOptions">The options used to write the output.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public ConversionReport Convert(TextReader reader, TextWriter writer, FormatOptions inputOptions, FormatOptions outputOptions)
    {
        return ConvertWithOptions(reader, writer, inputOptions, outputOptions, null);
    }

    /// <summary>
    /// Reads, optionally filters, and writes molecule records while reporting deterministic progress snapshots.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="inputOptions">The options used to read the input.</param>
    /// <param name="outputOptions">The options used to write the output.</param>
    /// <param name="conversionOptions">The optional filter and progress contracts; <c>null</c> disables both.</param>
    /// <returns>Deterministic read, written, skipped, warning, and failure totals and all read diagnostics.</returns>
    public ConversionReport ConvertWithOptions(TextReader reader, TextWriter writer, FormatOptions inputOptions, FormatOptions outputOptions, BatchConversionOptions? conversionOptions)
    {
        ConversionReport report = new ConversionReport();
        MoleculeStreamReader streamReader = MoleculeStreamReader.CreateWithFormats(reader, inputOptions, Formats);
        MoleculeStreamWriter streamWriter = MoleculeStreamWriter.CreateWithFormats(writer, outputOptions, Formats);
        BatchConversionOptions safeOptions = NormalizeConversionOptions(conversionOptions);
        bool done = false;
        while (!done)
        {
            MoleculeStreamReadResult readResult = streamReader.ReadNext();
            if (readResult.EndOfStream)
            {
                done = true;
            }
            else
            {
                report.ReadCount = report.ReadCount + 1;
                report.Merge(readResult.Report);
                if (readResult.Report.GetErrorCount() > 0)
                {
                    report.FailureCount = report.FailureCount + 1;
                }
                else
                {
                    bool include = ShouldInclude(safeOptions.Filter, readResult, report.ReadCount - 1);
                    if (include)
                    {
                        streamWriter.Write(readResult.Molecule);
                        report.WrittenCount = report.WrittenCount + 1;
                    }
                    else
                    {
                        report.SkippedCount = report.SkippedCount + 1;
                    }
                }

                ReportProgress(safeOptions.Progress, report, false);
            }
        }

        ReportProgress(safeOptions.Progress, report, true);
        return report;
    }

    internal BatchConversionOptions NormalizeConversionOptions(BatchConversionOptions? options)
    {
        if (options == null)
        {
            return new BatchConversionOptions();
        }

        return options;
    }

    internal bool ShouldInclude(IBatchConversionFilter? filter, MoleculeStreamReadResult readResult, int index)
    {
        if (filter == null)
        {
            return true;
        }

        BatchConversionRecord record = new BatchConversionRecord();
        record.Index = index;
        record.Molecule = readResult.Molecule;
        record.Report = readResult.Report;
        return filter.Include(record);
    }

    internal void ReportProgress(IBatchConversionProgress? progress, ConversionReport report, bool completed)
    {
        if (progress == null)
        {
            return;
        }

        BatchConversionProgress snapshot = new BatchConversionProgress();
        snapshot.ReadCount = report.ReadCount;
        snapshot.WrittenCount = report.WrittenCount;
        snapshot.SkippedCount = report.SkippedCount;
        snapshot.WarningCount = report.WarningCount;
        snapshot.FailureCount = report.FailureCount;
        snapshot.Completed = completed;
        progress.Report(snapshot);
    }
}
