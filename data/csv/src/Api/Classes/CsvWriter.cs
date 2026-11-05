using System.IO;

namespace csv;

/// <summary>
/// Writes CSV fields and records using configurable quoting and safety rules.
/// </summary>
public class CsvWriter : ICsvRowWriter, ICsvWriterControl, ICsvTableWriter
{
    internal TextWriter Writer;
    internal CsvWriterOptions Options;
    internal CsvDialect Dialect;
    internal bool IsFirstField;
    internal bool IsClosed;

    /// <summary>
    /// Creates a CSV writer over the supplied text destination.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">Operation options; null uses library defaults.</param>
    public CsvWriter(TextWriter writer, CsvWriterOptions? options)
    {
        Writer = writer;
        if (options == null)
        {
            Options = CsvWriterOptions.CreateDefault();
        }
        else
        {
            Options = options;
        }

        if (Options.Dialect == null)
        {
            Dialect = CsvDialect.CreateDefault();
        }
        else
        {
            Dialect = Options.Dialect;
        }

        IsFirstField = true;
        IsClosed = false;
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="writer">The destination CSV writer.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter Create(TextWriter writer, CsvWriterOptions? options)
    {
        CsvWriter csvWriter = new CsvWriter(writer, options);
        return csvWriter;
    }

    internal static CsvWriter CreateAppend(TextWriter writer, CsvWriterOptions options)
    {
        CsvWriter csvWriter = Create(writer, options);
        csvWriter.Options.Append = true;
        return csvWriter;
    }

    /// <summary>
    /// Writes header names as one record.
    /// </summary>
    /// <param name="header">The header to process.</param>
    public void WriteHeader(CsvHeader header)
    {
        int index = 0;
        while (index < header.Names.Length)
        {
            WriteField(header.Names[index]);
            index = index + 1;
        }

        NextRecord();
    }

    /// <summary>
    /// Encodes and writes one field, inserting a delimiter when needed.
    /// </summary>
    /// <param name="value">The field value, or null to write the configured null representation.</param>
    public void WriteField(string? value)
    {
        string encoded = CreateEncodedField(value, IsFirstField);
        if (!IsFirstField)
        {
            Writer.Write(Dialect.Delimiter);
        }

        Writer.Write(encoded);
        IsFirstField = false;
    }

    internal string CreateEncodedField(string? value, bool firstField)
    {
        if (value == null)
        {
            string text = ResolveNullFieldValue();
            text = ApplyInjectionPolicy(text);
            text = NormalizeFieldNewLines(text);
            string encoded = EncodeField(text, firstField, true);
            return encoded;
        }
        else
        {
            value = ApplyInjectionPolicy(value);
            string text = NormalizeFieldNewLines(value);
            string encoded = EncodeField(text, firstField, false);
            return encoded;
        }
    }

    /// <summary>
    /// Writes every row field and completes the record.
    /// </summary>
    /// <param name="row">The row to process.</param>
    public void WriteRow(CsvRow row)
    {
        int index = 0;
        while (index < row.Fields.Length)
        {
            WriteField(row.Fields[index]);
            index = index + 1;
        }

        NextRecord();
    }

    /// <summary>
    /// Writes the configured optional header followed by every table row.
    /// </summary>
    /// <param name="table">The table to write.</param>
    public void WriteTable(CsvTable table)
    {
        if (ShouldWriteHeader() && table.Header.Names.Length > 0)
        {
            WriteHeader(table.Header);
        }

        int index = 0;
        while (index < table.Rows.Count)
        {
            WriteRow(table.Rows[index]);
            index = index + 1;
        }
    }

    /// <summary>
    /// Indicates whether the configured output should include a header when one is available.
    /// </summary>
    /// <returns><c>true</c> when a header should be written; otherwise <c>false</c>.</returns>
    public bool ShouldWriteHeader()
    {
        return Options.WriteHeader;
    }

    /// <summary>
    /// Writes physical comment lines with the dialect marker.
    /// </summary>
    /// <param name="value">The comment text, or null to write an empty comment.</param>
    public void WriteComment(string? value)
    {
        if (!IsFirstField)
        {
            NextRecord();
        }

        string text = "";
        if (value != null)
        {
            text = value;
        }

        string line = "";
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '\r')
            {
                WriteCommentLine(line);
                line = "";
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else if (character == '\n')
            {
                WriteCommentLine(line);
                line = "";
            }
            else
            {
                line = line + character;
            }

            index = index + 1;
        }

        WriteCommentLine(line);
    }

    internal void WriteCommentLine(string value)
    {
        Writer.Write(GetCommentMarker());
        if (value.Length > 0)
        {
            if (value[0] == ' ')
            {
                Writer.Write(value);
            }
            else
            {
                Writer.Write(' ');
                Writer.Write(value);
            }
        }

        Writer.Write(GetRecordTerminator());
        IsFirstField = true;
        if (Options.FlushAfterRow)
        {
            Writer.Flush();
        }
    }

    /// <summary>
    /// Writes the record terminator and begins a new record.
    /// </summary>
    public void NextRecord()
    {
        Writer.Write(GetRecordTerminator());
        IsFirstField = true;
        if (Options.FlushAfterRow)
        {
            Writer.Flush();
        }
    }

    /// <summary>
    /// Flushes buffered text to the destination.
    /// </summary>
    public void Flush()
    {
        Writer.Flush();
    }

    /// <summary>
    /// Closes the underlying text object unless leave-open is enabled.
    /// </summary>
    public void Close()
    {
        if (IsClosed)
        {
            return;
        }

        if (Options.LeaveOpen)
        {
            Writer.Flush();
        }
        else
        {
            Writer.Close();
        }

        IsClosed = true;
    }

    internal string ResolveFieldValue(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal string ResolveNullFieldValue()
    {
        if (Options.HasNullString)
        {
            if (Options.NullString != null)
            {
                return Options.NullString;
            }
        }

        return "";
    }

    internal string ApplyInjectionPolicy(string value)
    {
        string guarded = CsvInjectionGuard.Apply(value, Options.InjectionMode);
        return guarded;
    }

    internal string GetRecordTerminator()
    {
        if (Options.LineTerminator != null && Options.LineTerminator.Length > 0)
        {
            return Options.LineTerminator;
        }

        if (Dialect.LineTerminator != null && Dialect.LineTerminator.Length > 0)
        {
            return Dialect.LineTerminator;
        }

        return "\r\n";
    }

    internal char GetCommentMarker()
    {
        if (Dialect.HasComment)
        {
            return Dialect.Comment;
        }

        return '#';
    }

    internal string NormalizeFieldNewLines(string value)
    {
        if (!Options.NormalizeNewLines)
        {
            return value;
        }

        string terminator = GetRecordTerminator();
        string normalized = "";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r')
            {
                normalized = normalized + terminator;
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else if (character == '\n')
            {
                normalized = normalized + terminator;
            }
            else
            {
                normalized = normalized + character;
            }

            index = index + 1;
        }

        return normalized;
    }

    internal string EncodeField(string value, bool firstField, bool isNull)
    {
        if (Options.QuoteMode == CsvQuoteMode.None)
        {
            string escaped = EscapeUnquotedField(value);
            return escaped;
        }

        if (Options.QuoteMode == CsvQuoteMode.All)
        {
            string quoted = QuoteField(value);
            return quoted;
        }

        if (Options.QuoteMode == CsvQuoteMode.NonNull)
        {
            if (!isNull)
            {
                string quoted = QuoteField(value);
                return quoted;
            }

            return value;
        }

        bool quote = ShouldQuote(value, firstField);
        if (!quote)
        {
            return value;
        }

        string minimal = QuoteField(value);
        return minimal;
    }

    internal string QuoteField(string value)
    {
        string escaped = EscapeQuotes(value);
        string quoted = Dialect.Quote.ToString() + escaped + Dialect.Quote.ToString();
        return quoted;
    }

    internal bool ShouldQuote(string value, bool firstField)
    {
        if (ContainsSpecialCharacter(value))
        {
            return true;
        }

        if (HasOuterSpace(value))
        {
            return true;
        }

        if (firstField && StartsWithCommentMarker(value))
        {
            return true;
        }

        return false;
    }

    internal bool ContainsSpecialCharacter(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == Dialect.Delimiter || character == Dialect.Quote || character == '\r' || character == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasOuterSpace(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == ' ' || value[value.Length - 1] == ' ')
        {
            return true;
        }

        return false;
    }

    internal bool StartsWithCommentMarker(string value)
    {
        if (!Dialect.HasComment)
        {
            return false;
        }

        string trimmed = value.TrimStart();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed[0] == Dialect.Comment)
        {
            return true;
        }

        return false;
    }

    internal string EscapeQuotes(string value)
    {
        string escaped = "";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == Dialect.Quote)
            {
                if (Dialect.UseDoubleQuoteEscaping)
                {
                    escaped = escaped + Dialect.Quote + Dialect.Quote;
                }
                else
                {
                    escaped = escaped + Dialect.Escape + Dialect.Quote;
                }
            }
            else
            {
                escaped = escaped + character;
            }

            index = index + 1;
        }

        return escaped;
    }

    internal string EscapeUnquotedField(string value)
    {
        string escaped = "";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == Dialect.Delimiter || character == Dialect.Quote || character == Dialect.Escape)
            {
                escaped = escaped + Dialect.Escape + character;
            }
            else if (character == '\r')
            {
                escaped = escaped + Dialect.Escape + "r";
            }
            else if (character == '\n')
            {
                escaped = escaped + Dialect.Escape + "n";
            }
            else
            {
                escaped = escaped + character;
            }

            index = index + 1;
        }

        return escaped;
    }
}
