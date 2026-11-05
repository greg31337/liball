namespace csv;

/// <summary>
/// Controls syntax, quoting, nulls, safety, flushing, ownership, and append behavior.
/// </summary>
public class CsvWriterOptions
{
    /// <summary>
    /// Syntax rules used to read or write.
    /// </summary>
    public CsvDialect Dialect;
    /// <summary>
    /// Policy deciding which output fields are quoted.
    /// </summary>
    public CsvQuoteMode QuoteMode;
    /// <summary>
    /// The text token representing a null field; null writes an empty field.
    /// </summary>
    public string? NullString;
    /// <summary>
    /// Whether null-token matching is active.
    /// </summary>
    public bool HasNullString;
    /// <summary>
    /// Policy for spreadsheet formula-like values.
    /// </summary>
    public CsvInjectionMode InjectionMode;
    /// <summary>
    /// Text written between records; null or empty text uses the dialect default.
    /// </summary>
    public string? LineTerminator;
    /// <summary>
    /// Whether field newlines are rewritten to the output terminator.
    /// </summary>
    public bool NormalizeNewLines;
    /// <summary>
    /// Whether high-level helpers write an available header.
    /// </summary>
    public bool WriteHeader;
    /// <summary>
    /// Whether each completed record is flushed.
    /// </summary>
    public bool FlushAfterRow;
    /// <summary>
    /// Whether closing leaves the underlying text object open.
    /// </summary>
    public bool LeaveOpen;
    /// <summary>
    /// Whether default file output appends instead of replacing.
    /// </summary>
    public bool Append;

    /// <summary>
    /// Initializes a new CsvWriterOptions with library defaults.
    /// </summary>
    public CsvWriterOptions()
    {
        Dialect = CsvDialect.CreateDefault();
        QuoteMode = CsvQuoteMode.Minimal;
        NullString = "";
        HasNullString = false;
        InjectionMode = CsvInjectionMode.None;
        LineTerminator = "\r\n";
        NormalizeNewLines = false;
        WriteHeader = false;
        FlushAfterRow = false;
        LeaveOpen = false;
        Append = false;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvWriterOptions CreateDefault()
    {
        CsvWriterOptions options = new CsvWriterOptions();
        return options;
    }
}
