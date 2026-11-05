namespace uspto;

internal class DelimitedRow
{
    internal long RowNumber = 0L;
    internal long StartLineNumber = 0L;
    internal long EndLineNumber = 0L;
    internal long StartOffset = 0L;
    internal string[] Headers = new string[0];
    internal string[] Values = new string[0];
    internal int ExpectedColumnCount = 0;
    internal int MissingColumnCount = 0;
    internal int ExtraColumnCount = 0;
    internal bool HasMissingColumns = false;
    internal bool HasExtraColumns = false;

    internal string? ReadValue(string header)
    {
        if (header == null)
        {
            throw new ValidationException("A delimited header name is required.", "header", "REQUIRED", null);
        }

        for (int index = 0; index < Headers.Length; index += 1)
        {
            if (Headers[index].Equals(header, global::System.StringComparison.Ordinal))
            {
                if (index < Values.Length)
                {
                    return Values[index];
                }

                return null;
            }
        }

        return null;
    }
}

internal class DelimitedReader
{
    internal global::System.IO.StreamReader Reader;
    internal char Delimiter;
    internal bool HasHeaders;
    internal bool HeadersRead = false;
    internal string[] Headers = new string[0];
    internal string? SourceName;
    internal int MaximumFieldCharacters;
    internal int MaximumColumns;
    internal int MaximumBatchRows;
    internal global::System.Threading.CancellationToken CancellationToken;
    internal long RecordNumber = 0L;
    internal long PhysicalLineNumber = 1L;
    internal long CharacterOffset = 0L;
    internal bool EndOfInput = false;

    internal DelimitedReader(global::System.IO.Stream input, char delimiter, bool hasHeaders, string? sourceName, UsptoLimits limits, global::System.Threading.CancellationToken cancellationToken)
    {
        if (input == null)
        {
            throw new ValidationException("A delimited input stream is required.", "input", "REQUIRED", null);
        }

        if (!input.CanRead)
        {
            throw new ValidationException("The delimited input stream must be readable.", "input", "STREAM_NOT_READABLE", null);
        }

        if (delimiter != ',' && delimiter != '\t')
        {
            throw new ValidationException("The delimiter must be a comma or tab.", "delimiter", "UNSUPPORTED_DELIMITER", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Delimited parsing limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumDelimitedFieldCharacters <= 0)
        {
            throw new ValidationException("The maximum delimited field length must be positive.", "MaximumDelimitedFieldCharacters", "INVALID_LIMIT", null);
        }

        if (limits.MaximumDelimitedColumns <= 0)
        {
            throw new ValidationException("The maximum delimited column count must be positive.", "MaximumDelimitedColumns", "INVALID_LIMIT", null);
        }

        if (limits.MaximumResults <= 0)
        {
            throw new ValidationException("The maximum delimited batch size must be positive.", "MaximumResults", "INVALID_LIMIT", null);
        }

        Delimiter = delimiter;
        HasHeaders = hasHeaders;
        SourceName = sourceName;
        MaximumFieldCharacters = limits.MaximumDelimitedFieldCharacters;
        MaximumColumns = limits.MaximumDelimitedColumns;
        MaximumBatchRows = limits.MaximumResults;
        CancellationToken = cancellationToken;
        global::System.Text.UTF8Encoding fallbackEncoding = new global::System.Text.UTF8Encoding(false, true);
        Reader = new global::System.IO.StreamReader(input, fallbackEncoding, true, 4096, true);
    }

    internal DelimitedRow? ReadRow()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (EndOfInput)
        {
            return null;
        }

        if (HasHeaders && !HeadersRead)
        {
            DelimitedRow? headerRow = ReadNextRecord();
            HeadersRead = true;
            if (headerRow == null)
            {
                EndOfInput = true;
                return null;
            }

            Headers = new string[headerRow.Values.Length];
            global::System.Array.Copy(headerRow.Values, Headers, Headers.Length);
        }

        DelimitedRow? row = ReadNextRecord();
        if (row == null)
        {
            return null;
        }

        ApplyHeaders(row);
        return row;
    }

    internal DelimitedRow[] ReadBatch(int maximumRows)
    {
        if (maximumRows <= 0 || maximumRows > MaximumBatchRows)
        {
            throw new ValidationException("The delimited batch size is outside the configured range.", "maximumRows", "BATCH_SIZE_OUT_OF_RANGE", null);
        }

        global::System.Collections.Generic.List<DelimitedRow> rows = new global::System.Collections.Generic.List<DelimitedRow>();
        while (rows.Count < maximumRows)
        {
            DelimitedRow? row = ReadRow();
            if (row == null)
            {
                break;
            }

            rows.Add(row);
        }

        DelimitedRow[] result = new DelimitedRow[rows.Count];
        rows.CopyTo(result, 0);
        return result;
    }

    internal void Close()
    {
        Reader.Close();
    }

    internal DelimitedRow? ReadNextRecord()
    {
        if (EndOfInput)
        {
            return null;
        }

        long nextRecordNumber = RecordNumber + 1L;
        long startLineNumber = PhysicalLineNumber;
        long startOffset = CharacterOffset;
        global::System.Collections.Generic.List<string> fields = new global::System.Collections.Generic.List<string>();
        global::System.Text.StringBuilder field = new global::System.Text.StringBuilder();
        bool inQuotes = false;
        bool afterClosingQuote = false;
        bool fieldStarted = false;
        bool sawCharacter = false;

        while (true)
        {
            CancellationToken.ThrowIfCancellationRequested();
            long currentOffset = CharacterOffset;
            int read = Reader.Read();
            if (read < 0)
            {
                EndOfInput = true;
                if (inQuotes)
                {
                    ThrowMalformed("The delimited input ended inside a quoted field.", nextRecordNumber, fields.Count + 1, CharacterOffset);
                }

                if (!sawCharacter && fields.Count == 0 && field.Length == 0)
                {
                    return null;
                }

                AddField(fields, field, nextRecordNumber, currentOffset);
                return CreateRow(fields, nextRecordNumber, startLineNumber, PhysicalLineNumber, startOffset);
            }

            CharacterOffset += 1L;
            sawCharacter = true;
            char character = (char)read;
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (Reader.Peek() == '"')
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        Reader.Read();
                        CharacterOffset += 1L;
                        AppendCharacter(field, '"', nextRecordNumber, fields.Count + 1, currentOffset);
                    }
                    else
                    {
                        inQuotes = false;
                        afterClosingQuote = true;
                    }
                }
                else if (character == '\r')
                {
                    AppendCharacter(field, '\r', nextRecordNumber, fields.Count + 1, currentOffset);
                    if (Reader.Peek() == '\n')
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        Reader.Read();
                        CharacterOffset += 1L;
                        AppendCharacter(field, '\n', nextRecordNumber, fields.Count + 1, currentOffset + 1L);
                    }

                    PhysicalLineNumber += 1L;
                }
                else if (character == '\n')
                {
                    AppendCharacter(field, '\n', nextRecordNumber, fields.Count + 1, currentOffset);
                    PhysicalLineNumber += 1L;
                }
                else
                {
                    AppendCharacter(field, character, nextRecordNumber, fields.Count + 1, currentOffset);
                }

                continue;
            }

            if (afterClosingQuote)
            {
                if (character == Delimiter)
                {
                    AddField(fields, field, nextRecordNumber, currentOffset);
                    field = new global::System.Text.StringBuilder();
                    fieldStarted = false;
                    afterClosingQuote = false;
                    continue;
                }

                if (character == '\r' || character == '\n')
                {
                    AddField(fields, field, nextRecordNumber, currentOffset);
                    long endLine = PhysicalLineNumber;
                    ConsumeRecordNewline(character);
                    return CreateRow(fields, nextRecordNumber, startLineNumber, endLine, startOffset);
                }

                ThrowMalformed("Only a delimiter or line ending may follow a closing quote.", nextRecordNumber, fields.Count + 1, currentOffset);
            }

            if (character == Delimiter)
            {
                AddField(fields, field, nextRecordNumber, currentOffset);
                field = new global::System.Text.StringBuilder();
                fieldStarted = false;
            }
            else if (character == '\r' || character == '\n')
            {
                AddField(fields, field, nextRecordNumber, currentOffset);
                long endLine = PhysicalLineNumber;
                ConsumeRecordNewline(character);
                return CreateRow(fields, nextRecordNumber, startLineNumber, endLine, startOffset);
            }
            else if (character == '"')
            {
                if (fieldStarted || field.Length != 0)
                {
                    ThrowMalformed("A quote is only valid at the beginning of a delimited field.", nextRecordNumber, fields.Count + 1, currentOffset);
                }

                inQuotes = true;
                fieldStarted = true;
            }
            else
            {
                AppendCharacter(field, character, nextRecordNumber, fields.Count + 1, currentOffset);
                fieldStarted = true;
            }
        }
    }

    internal void AppendCharacter(global::System.Text.StringBuilder field, char value, long rowNumber, int columnNumber, long offset)
    {
        if (field.Length >= MaximumFieldCharacters)
        {
            ThrowFieldLimit(rowNumber, columnNumber, offset);
        }

        field.Append(value);
    }

    internal void AddField(global::System.Collections.Generic.List<string> fields, global::System.Text.StringBuilder field, long rowNumber, long offset)
    {
        if (fields.Count >= MaximumColumns)
        {
            ThrowColumnLimit(rowNumber, fields.Count + 1, offset);
        }

        fields.Add(field.ToString());
    }

    internal void ConsumeRecordNewline(char character)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (character == '\r' && Reader.Peek() == '\n')
        {
            Reader.Read();
            CharacterOffset += 1L;
        }

        PhysicalLineNumber += 1L;
    }

    internal DelimitedRow CreateRow(global::System.Collections.Generic.List<string> fields, long rowNumber, long startLineNumber, long endLineNumber, long startOffset)
    {
        RecordNumber = rowNumber;
        DelimitedRow row = new DelimitedRow();
        row.RowNumber = rowNumber;
        row.StartLineNumber = startLineNumber;
        row.EndLineNumber = endLineNumber;
        row.StartOffset = startOffset;
        row.Values = new string[fields.Count];
        fields.CopyTo(row.Values, 0);
        return row;
    }

    internal void ApplyHeaders(DelimitedRow row)
    {
        if (!HasHeaders)
        {
            return;
        }

        row.Headers = new string[Headers.Length];
        global::System.Array.Copy(Headers, row.Headers, Headers.Length);
        row.ExpectedColumnCount = Headers.Length;
        if (row.Values.Length < Headers.Length)
        {
            row.MissingColumnCount = Headers.Length - row.Values.Length;
            row.HasMissingColumns = true;
        }
        else if (row.Values.Length > Headers.Length)
        {
            row.ExtraColumnCount = row.Values.Length - Headers.Length;
            row.HasExtraColumns = true;
        }
    }

    internal string CreateFieldPath(long rowNumber, int columnNumber)
    {
        return "row[" + rowNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].column[" + columnNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
    }

    internal void ThrowMalformed(string message, long rowNumber, int columnNumber, long offset)
    {
        ThrowParseFailure(message, rowNumber, columnNumber, offset);
    }

    internal void ThrowFieldLimit(long rowNumber, int columnNumber, long offset)
    {
        ThrowParseFailure("A delimited field exceeds the configured character limit.", rowNumber, columnNumber, offset);
    }

    internal void ThrowColumnLimit(long rowNumber, int columnNumber, long offset)
    {
        ThrowParseFailure("A delimited row exceeds the configured column limit.", rowNumber, columnNumber, offset);
    }

    internal void ThrowParseFailure(string message, long rowNumber, int columnNumber, long offset)
    {
        int lineNumber = 0;
        bool hasLineNumber = PhysicalLineNumber <= global::System.Int32.MaxValue;
        if (hasLineNumber)
        {
            lineNumber = (int)PhysicalLineNumber;
        }

        throw new ParseException(message, DocumentFormat.Csv, SourceName, CreateFieldPath(rowNumber, columnNumber), offset, true, lineNumber, hasLineNumber, null);
    }
}
