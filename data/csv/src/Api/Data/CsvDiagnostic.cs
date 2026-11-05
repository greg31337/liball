namespace csv;

/// <summary>
/// Describes a CSV problem together with its category and source position.
/// </summary>
public class CsvDiagnostic
{
    /// <summary>
    /// The parser category.
    /// </summary>
    public const string ParserCategory = "Parser";
    /// <summary>
    /// The converter category.
    /// </summary>
    public const string ConverterCategory = "Converter";
    /// <summary>
    /// The header category.
    /// </summary>
    public const string HeaderCategory = "Header";
    /// <summary>
    /// The schema category.
    /// </summary>
    public const string SchemaCategory = "Schema";
    /// <summary>
    /// The processing category.
    /// </summary>
    public const string ProcessingCategory = "Processing";

    /// <summary>
    /// The subsystem that produced the diagnostic.
    /// </summary>
    public string Category;
    /// <summary>
    /// A stable machine-readable diagnostic code.
    /// </summary>
    public string Code;
    /// <summary>
    /// A plain-language diagnostic explanation.
    /// </summary>
    public string Message;
    /// <summary>
    /// The one-based logical row, or zero when unavailable.
    /// </summary>
    public long Row;
    /// <summary>
    /// The one-based column, or zero when unavailable.
    /// </summary>
    public int Column;
    /// <summary>
    /// The one-based physical line, or zero when unavailable.
    /// </summary>
    public long Line;
    /// <summary>
    /// The zero-based source character offset.
    /// </summary>
    public long CharacterOffset;
    /// <summary>
    /// The zero-based source byte offset, or -1 when unavailable.
    /// </summary>
    public long ByteOffset;
    /// <summary>
    /// Raw source text associated with the diagnostic.
    /// </summary>
    public string RawContext;
    /// <summary>
    /// Whether the diagnostic represents an error.
    /// </summary>
    public bool IsError;

    /// <summary>
    /// Initializes a new CsvDiagnostic with library defaults.
    /// </summary>
    public CsvDiagnostic()
    {
        Category = "";
        Code = "";
        Message = "";
        Row = 0;
        Column = 0;
        Line = 0;
        CharacterOffset = 0;
        ByteOffset = -1;
        RawContext = "";
        IsError = true;
    }

    /// <summary>
    /// Creates an error in the parser category.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic CreateParserError(string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = Create(ParserCategory, code, message, row, column, line, characterOffset, rawContext);
        return diagnostic;
    }

    /// <summary>
    /// Creates an error in the converter category.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic CreateConverterError(string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = Create(ConverterCategory, code, message, row, column, line, characterOffset, rawContext);
        return diagnostic;
    }

    /// <summary>
    /// Creates an error in the header category.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic CreateHeaderError(string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = Create(HeaderCategory, code, message, row, column, line, characterOffset, rawContext);
        return diagnostic;
    }

    /// <summary>
    /// Creates an error in the schema category.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic CreateSchemaError(string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = Create(SchemaCategory, code, message, row, column, line, characterOffset, rawContext);
        return diagnostic;
    }

    /// <summary>
    /// Creates an error in the processing category.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic CreateProcessingError(string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = Create(ProcessingCategory, code, message, row, column, line, characterOffset, rawContext);
        return diagnostic;
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic explanation.</param>
    /// <param name="row">The row to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="line">The one-based physical line.</param>
    /// <param name="characterOffset">The zero-based source character offset.</param>
    /// <param name="rawContext">Raw text associated with the diagnostic.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnostic Create(string category, string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = category;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Row = row;
        diagnostic.Column = column;
        diagnostic.Line = line;
        diagnostic.CharacterOffset = characterOffset;
        diagnostic.RawContext = rawContext;
        diagnostic.IsError = true;
        return diagnostic;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvDiagnostic Clone()
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = Category;
        diagnostic.Code = Code;
        diagnostic.Message = Message;
        diagnostic.Row = Row;
        diagnostic.Column = Column;
        diagnostic.Line = Line;
        diagnostic.CharacterOffset = CharacterOffset;
        diagnostic.ByteOffset = ByteOffset;
        diagnostic.RawContext = RawContext;
        diagnostic.IsError = IsError;
        return diagnostic;
    }
}
