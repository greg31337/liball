namespace csv;

/// <summary>
/// Controls syntax, headers, whitespace, nulls, limits, errors, and ownership while reading.
/// </summary>
public class CsvReaderOptions
{
    /// <summary>
    /// Syntax rules used to read or write.
    /// </summary>
    public CsvDialect Dialect;
    /// <summary>
    /// Rules used to read and validate headers.
    /// </summary>
    public CsvHeaderOptions HeaderOptions;
    /// <summary>
    /// Whitespace trimming applied to fields.
    /// </summary>
    public CsvTrimMode TrimMode;
    /// <summary>
    /// Whether spaces after a delimiter are ignored.
    /// </summary>
    public bool SkipInitialSpace;
    /// <summary>
    /// Whether physically blank records are ignored.
    /// </summary>
    public bool SkipBlankLines;
    /// <summary>
    /// Whether configured comment lines are ignored.
    /// </summary>
    public bool AllowComments;
    /// <summary>
    /// The text token representing a null field; null matches no parsed text.
    /// </summary>
    public string? NullString;
    /// <summary>
    /// Whether null-token matching is active.
    /// </summary>
    public bool HasNullString;
    /// <summary>
    /// Characters read into the internal buffer at a time.
    /// </summary>
    public int BufferSize;
    /// <summary>
    /// Maximum field length in strict modes; zero means no limit.
    /// </summary>
    public int MaxFieldLength;
    /// <summary>
    /// Maximum raw record length in strict modes; zero means no limit.
    /// </summary>
    public int MaxRowLength;
    /// <summary>
    /// Maximum fields per record in strict modes; zero means no limit.
    /// </summary>
    public int MaxColumnCount;
    /// <summary>
    /// Malformed-input and parser-limit policy.
    /// </summary>
    public CsvErrorMode ErrorMode;
    /// <summary>
    /// Handler receiving malformed parser rows, or null when explicitly disabled.
    /// </summary>
    public ICsvRejectHandler? RejectHandler;
    /// <summary>
    /// Whether malformed parser rows are sent to the configured reject handler.
    /// </summary>
    public bool HasRejectHandler;
    /// <summary>
    /// Whether closing leaves the underlying text object open.
    /// </summary>
    public bool LeaveOpen;

    /// <summary>
    /// Initializes a new CsvReaderOptions with library defaults.
    /// </summary>
    public CsvReaderOptions()
    {
        Dialect = CsvDialect.CreateDefault();
        HeaderOptions = CsvHeaderOptions.CreateDefault();
        TrimMode = CsvTrimMode.None;
        SkipInitialSpace = false;
        SkipBlankLines = false;
        AllowComments = false;
        NullString = "";
        HasNullString = false;
        BufferSize = 4096;
        MaxFieldLength = 0;
        MaxRowLength = 0;
        MaxColumnCount = 0;
        ErrorMode = CsvErrorMode.Strict;
        RejectHandler = null;
        HasRejectHandler = false;
        LeaveOpen = false;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvReaderOptions CreateDefault()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        return options;
    }
}
