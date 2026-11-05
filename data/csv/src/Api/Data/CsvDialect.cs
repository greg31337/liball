namespace csv;

/// <summary>
/// Describes delimiter, quoting, escaping, line ending, spacing, and comment rules.
/// </summary>
public class CsvDialect
{
    /// <summary>
    /// The character separating fields.
    /// </summary>
    public char Delimiter;
    /// <summary>
    /// The character surrounding quoted fields.
    /// </summary>
    public char Quote;
    /// <summary>
    /// The explicit escape character.
    /// </summary>
    public char Escape;
    /// <summary>
    /// Whether doubled quotes represent one quote inside quoted fields.
    /// </summary>
    public bool UseDoubleQuoteEscaping;
    /// <summary>
    /// Text written between records; empty text uses the dialect default.
    /// </summary>
    public string LineTerminator;
    /// <summary>
    /// Physical line endings recognized as record boundaries.
    /// </summary>
    public CsvNewLineMode NewLineMode;
    /// <summary>
    /// Whether spaces after a delimiter are ignored.
    /// </summary>
    public bool SkipInitialSpace;
    /// <summary>
    /// Whether deterministic non-standard quote placement is accepted.
    /// </summary>
    public bool AllowRelaxedQuotes;
    /// <summary>
    /// Whether a comment marker is configured.
    /// </summary>
    public bool HasComment;
    /// <summary>
    /// The character beginning a comment line.
    /// </summary>
    public char Comment;

    /// <summary>
    /// Initializes a new CsvDialect with library defaults.
    /// </summary>
    public CsvDialect()
    {
        Delimiter = ',';
        Quote = '"';
        Escape = '"';
        UseDoubleQuoteEscaping = true;
        LineTerminator = "\r\n";
        NewLineMode = CsvNewLineMode.Auto;
        SkipInitialSpace = false;
        AllowRelaxedQuotes = false;
        HasComment = false;
        Comment = '\0';
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvDialect CreateDefault()
    {
        CsvDialect dialect = CreateRfc4180();
        return dialect;
    }

    /// <summary>
    /// Creates a comma dialect with CRLF output and doubled quote escaping.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvDialect CreateRfc4180()
    {
        CsvDialect dialect = new CsvDialect();
        dialect.Delimiter = ',';
        dialect.Quote = '"';
        dialect.Escape = '"';
        dialect.UseDoubleQuoteEscaping = true;
        dialect.LineTerminator = "\r\n";
        dialect.NewLineMode = CsvNewLineMode.Auto;
        dialect.SkipInitialSpace = false;
        dialect.AllowRelaxedQuotes = false;
        dialect.HasComment = false;
        dialect.Comment = '\0';
        return dialect;
    }

    /// <summary>
    /// Creates a tab-delimited dialect.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvDialect CreateTsv()
    {
        CsvDialect dialect = CreateRfc4180();
        dialect.Delimiter = '\t';
        dialect.LineTerminator = "\n";
        return dialect;
    }

    /// <summary>
    /// Creates a comma dialect accepting common relaxed spreadsheet quoting.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvDialect CreateExcel()
    {
        CsvDialect dialect = CreateRfc4180();
        dialect.AllowRelaxedQuotes = true;
        return dialect;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvDialect Clone()
    {
        CsvDialect dialect = new CsvDialect();
        dialect.Delimiter = Delimiter;
        dialect.Quote = Quote;
        dialect.Escape = Escape;
        dialect.UseDoubleQuoteEscaping = UseDoubleQuoteEscaping;
        dialect.LineTerminator = LineTerminator;
        dialect.NewLineMode = NewLineMode;
        dialect.SkipInitialSpace = SkipInitialSpace;
        dialect.AllowRelaxedQuotes = AllowRelaxedQuotes;
        dialect.HasComment = HasComment;
        dialect.Comment = Comment;
        return dialect;
    }
}
