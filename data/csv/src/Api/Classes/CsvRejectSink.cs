using System.Globalization;
using System.IO;

namespace csv;

/// <summary>
/// Writes rejected rows and diagnostics as a self-contained CSV stream.
/// </summary>
/// <remarks>The output columns are category, code, message, row, column, line, character offset, and raw input.</remarks>
public class CsvRejectSink : ICsvRejectHandler, ICsvRejectSink
{
    internal CsvWriter Writer;
    internal CsvHeader Header;
    internal bool HeaderWasConsidered;

    /// <summary>
    /// Initializes a reject sink over a text destination.
    /// </summary>
    /// <param name="writer">The reject-stream destination.</param>
    /// <param name="options">CSV output options; null uses defaults and enables the reject header.</param>
    public CsvRejectSink(TextWriter writer, CsvWriterOptions? options)
    {
        CsvWriterOptions activeOptions = ResolveOptions(options);
        Writer = new CsvWriter(writer, activeOptions);
        Header = CreateHeader();
        HeaderWasConsidered = false;
    }

    /// <summary>Writes one rejected row and its diagnostic.</summary>
    /// <param name="row">The rejected row.</param>
    /// <param name="diagnostic">The rejection diagnostic.</param>
    public void Handle(CsvRow row, CsvDiagnostic diagnostic)
    {
        WriteReject(row, diagnostic);
    }

    /// <summary>Writes one rejected row and its diagnostic.</summary>
    /// <param name="row">The rejected row.</param>
    /// <param name="diagnostic">The rejection diagnostic.</param>
    public void WriteReject(CsvRow row, CsvDiagnostic diagnostic)
    {
        WriteHeaderWhenConfigured();
        CsvRow outputRow = new CsvRow();
        outputRow.Fields = new string?[]
        {
            diagnostic.Category,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Row.ToString(CultureInfo.InvariantCulture),
            diagnostic.Column.ToString(CultureInfo.InvariantCulture),
            diagnostic.Line.ToString(CultureInfo.InvariantCulture),
            diagnostic.CharacterOffset.ToString(CultureInfo.InvariantCulture),
            ResolveRawInput(row, diagnostic)
        };
        Writer.WriteRow(outputRow);
    }

    /// <summary>Flushes buffered reject output.</summary>
    public void Flush()
    {
        Writer.Flush();
    }

    /// <summary>Closes the reject output according to its ownership settings.</summary>
    public void Close()
    {
        Writer.Close();
    }

    internal static CsvWriterOptions ResolveOptions(CsvWriterOptions? options)
    {
        if (options != null)
        {
            return options;
        }

        CsvWriterOptions defaultOptions = CsvWriterOptions.CreateDefault();
        defaultOptions.WriteHeader = true;
        return defaultOptions;
    }

    internal static CsvHeader CreateHeader()
    {
        CsvHeader header = new CsvHeader();
        header.Names = new string[] { "Category", "Code", "Message", "Row", "Column", "Line", "CharacterOffset", "RawInput" };
        return header;
    }

    internal void WriteHeaderWhenConfigured()
    {
        if (HeaderWasConsidered)
        {
            return;
        }

        HeaderWasConsidered = true;
        if (Writer.ShouldWriteHeader())
        {
            Writer.WriteHeader(Header);
        }
    }

    internal string ResolveRawInput(CsvRow row, CsvDiagnostic diagnostic)
    {
        string rawInput = row.RawText;
        if (rawInput.Length == 0)
        {
            rawInput = diagnostic.RawContext;
        }

        return rawInput;
    }
}
