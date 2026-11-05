using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace csv;

/// <summary>
/// Reads CSV records forward-only from a text source using configurable policies.
/// </summary>
public class CsvReader : ICsvRowReader, ICsvReaderControl
{
    internal TextReader Reader;
    internal CsvReaderOptions Options;
    internal CsvDialect Dialect;
    internal CsvCharBuffer Buffer;
    internal CsvRow CurrentRow;
    internal CsvHeader Header;
    internal CsvDiagnosticList Diagnostics;
    internal List<CsvRow> Rejects;
    internal bool HeaderWasGenerated;
    internal long RowNumber;
    internal int CurrentFieldIndex;
    internal long PhysicalLine;
    internal int PhysicalColumn;
    internal long CharacterOffset;
    internal Encoding ByteEncoding;
    internal char[] ByteCharacters;
    internal char PendingHighSurrogate;
    internal bool HasPendingHighSurrogate;
    internal bool HasByteOffset;
    internal long ByteOffset;
    internal long CurrentRowLine;
    internal int CurrentRowColumn;
    internal long CurrentRowCharacterOffset;
    internal long CurrentRowByteOffset;
    internal long CurrentFieldLine;
    internal int CurrentFieldColumn;
    internal long CurrentFieldCharacterOffset;
    internal bool CurrentFieldWasQuoted;
    internal bool LastReadWasCarriageReturn;
    internal bool HasParseError;
    internal bool CurrentRowRejected;
    internal bool EndOfData;
    internal bool IsClosed;

    /// <summary>
    /// Creates a forward-only reader over the supplied text source.
    /// </summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="options">Operation options; null uses library defaults.</param>
    public CsvReader(TextReader reader, CsvReaderOptions? options)
    {
        Reader = reader;
        if (options == null)
        {
            Options = CsvReaderOptions.CreateDefault();
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

        Buffer = CsvCharBuffer.Create(reader, Options.BufferSize);
        CurrentRow = new CsvRow();
        Header = new CsvHeader();
        Diagnostics = CsvDiagnosticList.Create();
        Rejects = new List<CsvRow>();
        HeaderWasGenerated = false;
        RowNumber = 0;
        CurrentFieldIndex = 0;
        PhysicalLine = 1;
        PhysicalColumn = 1;
        CharacterOffset = 0;
        ByteEncoding = Encoding.UTF8;
        ByteCharacters = new char[2];
        PendingHighSurrogate = '\0';
        HasPendingHighSurrogate = false;
        HasByteOffset = false;
        ByteOffset = -1;
        ConfigureByteOffsets(reader);
        CurrentRowLine = 1;
        CurrentRowColumn = 1;
        CurrentRowCharacterOffset = 0;
        CurrentRowByteOffset = ByteOffset;
        CurrentFieldLine = 1;
        CurrentFieldColumn = 1;
        CurrentFieldCharacterOffset = 0;
        CurrentFieldWasQuoted = false;
        LastReadWasCarriageReturn = false;
        HasParseError = false;
        CurrentRowRejected = false;
        EndOfData = false;
        IsClosed = false;
    }

    /// <summary>
    /// Creates a forward-only reader over the supplied text source.
    /// </summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvReader Create(TextReader reader, CsvReaderOptions? options)
    {
        CsvReader csvReader = new CsvReader(reader, options);
        return csvReader;
    }

    internal void ConfigureByteOffsets(TextReader reader)
    {
        Type? readerType = reader.GetType();
        bool isStreamReader = false;
        while (readerType != null)
        {
            if (readerType.FullName == "System.IO.StreamReader")
            {
                isStreamReader = true;
                break;
            }

            readerType = readerType.BaseType;
        }

        if (!isStreamReader)
        {
            return;
        }

        StreamReader streamReader = (StreamReader)reader;
        Stream stream = streamReader.BaseStream;
        if (!stream.CanSeek || stream.Position != 0)
        {
            return;
        }

        byte[] prefix = new byte[4];
        int count = stream.Read(prefix, 0, prefix.Length);
        stream.Position = 0;
        ByteEncoding = streamReader.CurrentEncoding;
        ByteOffset = 0;

        if (count >= 4 && prefix[0] == 0 && prefix[1] == 0 && prefix[2] == 254 && prefix[3] == 255)
        {
            ByteEncoding = new UTF32Encoding(true, true);
            ByteOffset = 4;
        }
        else if (count >= 4 && prefix[0] == 255 && prefix[1] == 254 && prefix[2] == 0 && prefix[3] == 0)
        {
            ByteEncoding = new UTF32Encoding(false, true);
            ByteOffset = 4;
        }
        else if (count >= 3 && prefix[0] == 239 && prefix[1] == 187 && prefix[2] == 191)
        {
            ByteEncoding = new UTF8Encoding(true);
            ByteOffset = 3;
        }
        else if (count >= 2 && prefix[0] == 254 && prefix[1] == 255)
        {
            ByteEncoding = new UnicodeEncoding(true, true);
            ByteOffset = 2;
        }
        else if (count >= 2 && prefix[0] == 255 && prefix[1] == 254)
        {
            ByteEncoding = new UnicodeEncoding(false, true);
            ByteOffset = 2;
        }

        HasByteOffset = true;
    }

    internal void AdvanceByteOffset(char character)
    {
        if (!HasByteOffset)
        {
            return;
        }

        if (HasPendingHighSurrogate)
        {
            if (character >= '\uDC00' && character <= '\uDFFF')
            {
                ByteCharacters[0] = PendingHighSurrogate;
                ByteCharacters[1] = character;
                int pairByteCount = ByteEncoding.GetByteCount(ByteCharacters, 0, 2);
                ByteOffset = ByteOffset + pairByteCount;
                HasPendingHighSurrogate = false;
                return;
            }

            ByteCharacters[0] = PendingHighSurrogate;
            int pendingByteCount = ByteEncoding.GetByteCount(ByteCharacters, 0, 1);
            ByteOffset = ByteOffset + pendingByteCount;
            HasPendingHighSurrogate = false;
        }

        if (character >= '\uD800' && character <= '\uDBFF')
        {
            PendingHighSurrogate = character;
            HasPendingHighSurrogate = true;
            return;
        }

        ByteCharacters[0] = character;
        int byteCount = ByteEncoding.GetByteCount(ByteCharacters, 0, 1);
        ByteOffset = ByteOffset + byteCount;
    }

    /// <summary>
    /// Advances to the next accepted row, skipping ignored or rejected input.
    /// </summary>
    /// <returns><c>true</c> when a row is available; <c>false</c> at end of data or after a strict error.</returns>
    public bool Read()
    {
        while (true)
        {
            CurrentRowRejected = false;
            bool read = ReadSingleRow();
            if (read)
            {
                return true;
            }

            if (!CurrentRowRejected)
            {
                return false;
            }

            if (EndOfData)
            {
                CurrentRowRejected = false;
                return false;
            }
        }
    }

    internal bool ReadSingleRow()
    {
        if (EndOfData)
        {
            return false;
        }

        if (HasParseError)
        {
            return false;
        }

        SkipIgnoredLines();
        if (EndOfData)
        {
            return false;
        }

        List<string?> fields = new List<string?>();
        StringBuilder fieldBuilder = new StringBuilder();
        StringBuilder rawBuilder = new StringBuilder();
        CsvParserState state = CsvParserState.StartField;
        bool sawCharacter = false;
        bool recordEnded = false;
        CurrentRowLine = PhysicalLine;
        CurrentRowColumn = PhysicalColumn;
        CurrentRowCharacterOffset = CharacterOffset;
        CurrentRowByteOffset = ByteOffset;
        CurrentFieldIndex = 0;
        CurrentFieldLine = PhysicalLine;
        CurrentFieldColumn = PhysicalColumn;
        CurrentFieldCharacterOffset = CharacterOffset;
        CurrentFieldWasQuoted = false;

        while (!recordEnded)
        {
            if ((state == CsvParserState.StartField || state == CsvParserState.AfterDelimiter) && ShouldSkipInitialSpace())
            {
                SkipInitialSpaces(rawBuilder);
                if (ShouldStopCurrentRead())
                {
                    return false;
                }
            }

            int value = ReadTrackedCharacter(rawBuilder);
            if (ShouldStopCurrentRead())
            {
                return false;
            }

            if (value < 0)
            {
                EndOfData = true;
                if (!sawCharacter)
                {
                    return false;
                }

                if (state == CsvParserState.InQuotedField && ShouldFailOnMalformedQuote())
                {
                    AddParserError("UnterminatedQuote", "Quoted field is missing a closing quote.", rawBuilder);
                    return false;
                }

                AddField(fields, fieldBuilder, rawBuilder);
                if (ShouldStopCurrentRead())
                {
                    return false;
                }

                recordEnded = true;
            }
            else
            {
                char character = (char)value;
                sawCharacter = true;

                if (state == CsvParserState.StartField)
                {
                    bool isRecordTerminator = IsRecordTerminator(character);
                    ReadStartFieldCharacter(character, fields, fieldBuilder, rawBuilder);
                    if (ShouldStopCurrentRead())
                    {
                        return false;
                    }

                    if (isRecordTerminator)
                    {
                        recordEnded = true;
                    }
                    else if (character == Dialect.Delimiter)
                    {
                        state = CsvParserState.AfterDelimiter;
                    }
                    else if (character == Dialect.Quote)
                    {
                        CurrentFieldWasQuoted = true;
                        state = CsvParserState.InQuotedField;
                    }
                    else
                    {
                        state = CsvParserState.InField;
                    }
                }
                else if (state == CsvParserState.AfterDelimiter)
                {
                    bool isRecordTerminator = IsRecordTerminator(character);
                    ReadStartFieldCharacter(character, fields, fieldBuilder, rawBuilder);
                    if (ShouldStopCurrentRead())
                    {
                        return false;
                    }

                    if (isRecordTerminator)
                    {
                        recordEnded = true;
                    }
                    else if (character == Dialect.Delimiter)
                    {
                        state = CsvParserState.AfterDelimiter;
                    }
                    else if (character == Dialect.Quote)
                    {
                        CurrentFieldWasQuoted = true;
                        state = CsvParserState.InQuotedField;
                    }
                    else
                    {
                        state = CsvParserState.InField;
                    }
                }
                else if (state == CsvParserState.InField)
                {
                    if (character == Dialect.Delimiter)
                    {
                        AddField(fields, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        BeginNextField(fields);
                        state = CsvParserState.AfterDelimiter;
                    }
                    else if (ConsumeRecordTerminator(character, rawBuilder))
                    {
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        AddField(fields, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        recordEnded = true;
                    }
                    else if (character == Dialect.Quote && ShouldFailOnMalformedQuote())
                    {
                        AddParserError("MalformedQuote", "Quote character is not allowed inside an unquoted field.", rawBuilder);
                        return false;
                    }
                    else
                    {
                        AppendFieldCharacter(character, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }
                    }
                }
                else if (state == CsvParserState.InQuotedField)
                {
                    if (ShouldConsumeEscapedQuotedCharacter(character))
                    {
                        int escapedValue = ReadTrackedCharacter(rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        AppendFieldCharacter((char)escapedValue, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }
                    }
                    else if (character == Dialect.Quote)
                    {
                        if (Dialect.UseDoubleQuoteEscaping && Buffer.Peek() == Dialect.Quote)
                        {
                            ReadTrackedCharacter(rawBuilder);
                            if (ShouldStopCurrentRead())
                            {
                                return false;
                            }

                            AppendFieldCharacter(Dialect.Quote, fieldBuilder, rawBuilder);
                            if (ShouldStopCurrentRead())
                            {
                                return false;
                            }
                        }
                        else
                        {
                            state = CsvParserState.AfterQuote;
                        }
                    }
                    else
                    {
                        AppendFieldCharacter(character, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }
                    }
                }
                else if (state == CsvParserState.AfterQuote)
                {
                    if (character == Dialect.Delimiter)
                    {
                        AddField(fields, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        BeginNextField(fields);
                        state = CsvParserState.AfterDelimiter;
                    }
                    else if (ConsumeRecordTerminator(character, rawBuilder))
                    {
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        AddField(fields, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        recordEnded = true;
                    }
                    else if (ShouldFailOnMalformedQuote())
                    {
                        AddParserError("MalformedQuote", "Only a delimiter or line terminator can follow a closing quote.", rawBuilder);
                        return false;
                    }
                    else
                    {
                        AppendFieldCharacter(character, fieldBuilder, rawBuilder);
                        if (ShouldStopCurrentRead())
                        {
                            return false;
                        }

                        state = CsvParserState.InField;
                    }
                }
            }
        }

        RowNumber = RowNumber + 1;
        CurrentRow = CsvRow.Create(fields.ToArray(), RowNumber);
        CurrentRow.Line = CurrentRowLine;
        CurrentRow.Column = CurrentRowColumn;
        CurrentRow.CharacterOffset = CurrentRowCharacterOffset;
        CurrentRow.ByteOffset = CurrentRowByteOffset;
        CurrentRow.RawText = rawBuilder.ToString();
        CurrentRow.Diagnostics = Diagnostics;
        AttachHeaderToCurrentRow();

        return true;
    }

    /// <summary>
    /// Reads or generates the header before data rows are consumed.
    /// </summary>
    /// <returns><c>true</c> when a header was read or generated; otherwise <c>false</c>.</returns>
    public bool ReadHeader()
    {
        bool read = Read();
        if (!read)
        {
            return false;
        }

        Header = CsvHeader.CreateFromRow(CurrentRow, Options.HeaderOptions);
        HeaderWasGenerated = false;
        CurrentRow.Header = Header;
        ApplyDiagnosticByteOffset(Header.Diagnostics, CurrentRow.ByteOffset);
        AddDiagnostics(Header.Diagnostics);
        return true;
    }

    /// <summary>
    /// Returns a current-row field by zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The stored value, which may be null, or empty text when <c>index</c> is outside the row.</returns>
    public string? GetField(int index)
    {
        string? value = CurrentRow.GetField(index);
        return value;
    }

    /// <summary>
    /// Returns the current row field count.
    /// </summary>
    /// <returns>The current row's field count.</returns>
    public int CountFields()
    {
        int count = CurrentRow.CountFields();
        return count;
    }

    /// <summary>
    /// Checks whether the current row contains a zero-based field position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns><c>true</c> when <c>index</c> is valid; otherwise <c>false</c>.</returns>
    public bool HasField(int index)
    {
        bool hasField = CurrentRow.HasField(index);
        return hasField;
    }

    /// <summary>
    /// Returns a current-row field by header name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The stored value, which may be null, or empty text when the name is absent.</returns>
    public string? GetFieldByName(string name)
    {
        string? value = CurrentRow.GetFieldByName(name);
        return value;
    }

    /// <summary>
    /// Checks for a current-row field by header name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns><c>true</c> when the header resolves to a current-row field; otherwise <c>false</c>.</returns>
    public bool HasFieldByName(string name)
    {
        bool hasField = CurrentRow.HasFieldByName(name);
        return hasField;
    }

    /// <summary>
    /// Replaces a current-row field when the zero-based position is valid.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <param name="value">The replacement field value, which may be null.</param>
    public void SetField(int index, string? value)
    {
        CurrentRow.SetField(index, value);
    }

    /// <summary>
    /// Replaces a current-row field when the header name resolves to a valid position.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="value">The replacement field value, which may be null.</param>
    public void SetFieldByName(string name, string? value)
    {
        CurrentRow.SetFieldByName(name, value);
    }

    /// <summary>
    /// Returns a header position, or -1 when absent.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The zero-based header position, or -1 when absent.</returns>
    public int GetHeaderIndex(string name)
    {
        int index = Header.IndexOf(name);
        return index;
    }

    /// <summary>
    /// Returns the mutable row from the latest successful read.
    /// </summary>
    /// <returns>The current mutable row.</returns>
    public CsvRow GetCurrentRow()
    {
        return CurrentRow;
    }

    /// <summary>
    /// Returns the active header, including generated names.
    /// </summary>
    /// <returns>The active header.</returns>
    public CsvHeader GetHeaders()
    {
        return Header;
    }

    /// <summary>
    /// Returns accumulated reader diagnostics.
    /// </summary>
    /// <returns>The reader's mutable diagnostic collection.</returns>
    public CsvDiagnosticList GetDiagnostics()
    {
        return Diagnostics;
    }

    /// <summary>
    /// Indicates whether the configured header record should be consumed separately.
    /// </summary>
    /// <returns><c>true</c> when a header record should be read; otherwise <c>false</c>.</returns>
    public bool ShouldReadHeader()
    {
        return Options.HeaderOptions.HasHeader;
    }

    /// <summary>
    /// Returns malformed rows retained in collect-rejects mode.
    /// </summary>
    /// <returns>The reader's mutable rejected-row collection.</returns>
    public List<CsvRow> GetRejects()
    {
        return Rejects;
    }

    internal void AttachHeaderToCurrentRow()
    {
        if (Header.Names.Length > 0)
        {
            CurrentRow.Header = Header;
            if (!HeaderWasGenerated)
            {
                ApplyFieldCountPolicies();
            }

            return;
        }

        if (ShouldGenerateHeaders())
        {
            Header = CsvHeader.CreateGenerated(CurrentRow.Fields.Length, Options.HeaderOptions);
            HeaderWasGenerated = true;
            CurrentRow.Header = Header;
        }
    }

    internal bool ShouldGenerateHeaders()
    {
        if (!Options.HeaderOptions.GenerateHeaders)
        {
            return false;
        }

        if (Options.HeaderOptions.HasHeader)
        {
            return false;
        }

        return true;
    }

    internal void ApplyFieldCountPolicies()
    {
        if (Header.Names.Length == 0)
        {
            return;
        }

        if (CurrentRow.Fields.Length < Header.Names.Length)
        {
            ApplyMissingFieldPolicy();
        }

        if (CurrentRow.Fields.Length > Header.Names.Length)
        {
            ApplyExtraFieldPolicy();
        }
    }

    internal void ApplyMissingFieldPolicy()
    {
        if (Options.HeaderOptions.MissingFieldMode == CsvMissingFieldMode.Empty)
        {
            CurrentRow.Fields = ExtendFields(CurrentRow.Fields, Header.Names.Length, false);
            return;
        }

        if (Options.HeaderOptions.MissingFieldMode == CsvMissingFieldMode.Null)
        {
            CurrentRow.Fields = ExtendFields(CurrentRow.Fields, Header.Names.Length, true);
            return;
        }

        if (Options.HeaderOptions.MissingFieldMode == CsvMissingFieldMode.Error)
        {
            int index = CurrentRow.Fields.Length;
            while (index < Header.Names.Length)
            {
                AddHeaderPolicyDiagnostic("MissingField", "Field is missing for header.", index);
                index = index + 1;
            }
        }
    }

    internal void ApplyExtraFieldPolicy()
    {
        if (Options.HeaderOptions.ExtraFieldMode == CsvExtraFieldMode.Ignore)
        {
            CurrentRow.Fields = TruncateFields(CurrentRow.Fields, Header.Names.Length);
            return;
        }

        if (Options.HeaderOptions.ExtraFieldMode == CsvExtraFieldMode.Error)
        {
            int index = Header.Names.Length;
            while (index < CurrentRow.Fields.Length)
            {
                AddHeaderPolicyDiagnostic("ExtraField", "Field has no matching header.", index);
                index = index + 1;
            }
        }
    }

    internal string?[] ExtendFields(string?[] fields, int count, bool fillWithNull)
    {
        string?[] copy = new string?[count];
        int index = 0;
        while (index < count)
        {
            if (index < fields.Length)
            {
                copy[index] = fields[index];
            }
            else if (fillWithNull)
            {
                copy[index] = null;
            }
            else
            {
                copy[index] = "";
            }

            index = index + 1;
        }

        return copy;
    }

    internal string?[] TruncateFields(string?[] fields, int count)
    {
        string?[] copy = new string?[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        return copy;
    }

    internal void AddHeaderPolicyDiagnostic(string code, string message, int column)
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateHeaderError(code, message, CurrentRow.RowNumber, column, CurrentRow.Line, CurrentRow.CharacterOffset, CurrentRow.RawText);
        diagnostic.ByteOffset = CurrentRow.ByteOffset;
        Diagnostics.Add(diagnostic);
    }

    internal void ApplyDiagnosticByteOffset(CsvDiagnosticList diagnostics, long byteOffset)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            diagnostics.Get(index).ByteOffset = byteOffset;
            index = index + 1;
        }
    }

    internal void AddDiagnostics(CsvDiagnosticList diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            Diagnostics.Add(diagnostics.Get(index).Clone());
            index = index + 1;
        }
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

        IsClosed = true;
        if (!Options.LeaveOpen)
        {
            Reader.Close();
        }
    }

    internal void AddField(List<string?> fields, StringBuilder fieldBuilder, StringBuilder rawBuilder)
    {
        string? value = ApplyTrimMode(fieldBuilder.ToString(), CurrentFieldWasQuoted);
        value = ApplyNullString(value);
        fields.Add(value);
        fieldBuilder.Clear();
        CheckColumnCount(fields, rawBuilder);
    }

    internal void ReadStartFieldCharacter(char character, List<string?> fields, StringBuilder fieldBuilder, StringBuilder rawBuilder)
    {
        if (character == Dialect.Delimiter)
        {
            AddField(fields, fieldBuilder, rawBuilder);
            BeginNextField(fields);
        }
        else if (ConsumeRecordTerminator(character, rawBuilder))
        {
            AddField(fields, fieldBuilder, rawBuilder);
        }
        else if (character != Dialect.Quote)
        {
            AppendFieldCharacter(character, fieldBuilder, rawBuilder);
        }
    }

    internal void BeginNextField(List<string?> fields)
    {
        CurrentFieldIndex = fields.Count;
        CurrentFieldLine = PhysicalLine;
        CurrentFieldColumn = PhysicalColumn;
        CurrentFieldCharacterOffset = CharacterOffset;
        CurrentFieldWasQuoted = false;
    }

    internal bool IsRecordTerminator(char character)
    {
        if (Dialect.NewLineMode == CsvNewLineMode.Crlf)
        {
            if (character == '\r' && Buffer.Peek() == '\n')
            {
                return true;
            }

            return false;
        }

        if (Dialect.NewLineMode == CsvNewLineMode.Lf)
        {
            return character == '\n';
        }

        if (Dialect.NewLineMode == CsvNewLineMode.Cr)
        {
            return character == '\r';
        }

        if (character == '\r' || character == '\n')
        {
            return true;
        }

        return false;
    }

    internal bool IsPhysicalLineTerminator(char character)
    {
        if (character == '\r' || character == '\n')
        {
            return true;
        }

        return false;
    }

    internal bool ShouldConsumeEscapedQuotedCharacter(char character)
    {
        if (Dialect.UseDoubleQuoteEscaping)
        {
            return false;
        }

        if (character != Dialect.Escape)
        {
            return false;
        }

        int value = Buffer.Peek();
        if (value < 0)
        {
            return false;
        }

        char next = (char)value;
        if (next == Dialect.Quote)
        {
            return true;
        }

        if (next == Dialect.Escape)
        {
            return true;
        }

        return false;
    }

    internal bool ConsumeRecordTerminator(char character, StringBuilder rawBuilder)
    {
        if (!IsRecordTerminator(character))
        {
            return false;
        }

        if (character == '\r' && Buffer.Peek() == '\n' && Dialect.NewLineMode != CsvNewLineMode.Cr)
        {
            ReadTrackedCharacter(rawBuilder);
        }

        return true;
    }

    internal int ReadTrackedCharacter(StringBuilder rawBuilder)
    {
        int value = Buffer.Read();
        if (value < 0)
        {
            return -1;
        }

        char character = (char)value;
        rawBuilder.Append(character);
        CharacterOffset = Buffer.CharacterOffset;
        AdvanceByteOffset(character);
        AdvancePhysicalPosition(character);
        CheckRowLength(rawBuilder);
        return value;
    }

    internal int ReadIgnoredCharacter(StringBuilder rawBuilder)
    {
        int value = Buffer.Read();
        if (value < 0)
        {
            return -1;
        }

        char character = (char)value;
        rawBuilder.Append(character);
        CharacterOffset = Buffer.CharacterOffset;
        AdvanceByteOffset(character);
        AdvancePhysicalPosition(character);
        return value;
    }

    internal void AdvancePhysicalPosition(char character)
    {
        if (character == '\r')
        {
            PhysicalLine = PhysicalLine + 1;
            PhysicalColumn = 1;
            LastReadWasCarriageReturn = true;
        }
        else if (character == '\n')
        {
            if (LastReadWasCarriageReturn)
            {
                PhysicalColumn = 1;
            }
            else
            {
                PhysicalLine = PhysicalLine + 1;
                PhysicalColumn = 1;
            }

            LastReadWasCarriageReturn = false;
        }
        else
        {
            PhysicalColumn = PhysicalColumn + 1;
            LastReadWasCarriageReturn = false;
        }
    }

    internal void AppendFieldCharacter(char character, StringBuilder fieldBuilder, StringBuilder rawBuilder)
    {
        fieldBuilder.Append(character);
        CheckFieldLength(fieldBuilder, rawBuilder);
    }

    internal string ApplyTrimMode(string value, bool wasQuoted)
    {
        if (Options.TrimMode == CsvTrimMode.None)
        {
            return value;
        }

        if (Options.TrimMode == CsvTrimMode.Unquoted)
        {
            if (wasQuoted)
            {
                return value;
            }

            return value.Trim();
        }

        if (Options.TrimMode == CsvTrimMode.QuotedAndUnquoted)
        {
            return value.Trim();
        }

        if (Options.TrimMode == CsvTrimMode.Left)
        {
            return value.TrimStart();
        }

        if (Options.TrimMode == CsvTrimMode.Right)
        {
            return value.TrimEnd();
        }

        if (Options.TrimMode == CsvTrimMode.Both)
        {
            return value.Trim();
        }

        return value;
    }

    internal string? ApplyNullString(string value)
    {
        if (!Options.HasNullString)
        {
            return value;
        }

        if (value == Options.NullString)
        {
            return null;
        }

        return value;
    }

    internal void SkipIgnoredLines()
    {
        bool skipped = true;
        while (skipped)
        {
            skipped = false;
            int value = Buffer.Peek();
            if (value < 0)
            {
                EndOfData = true;
                return;
            }

            char character = (char)value;
            if (ShouldSkipBlankLines() && IsPhysicalLineTerminator(character))
            {
                StringBuilder rawBuilder = new StringBuilder();
                ReadIgnoredCharacter(rawBuilder);
                if (character == '\r' && Buffer.Peek() == '\n')
                {
                    ReadIgnoredCharacter(rawBuilder);
                }

                skipped = true;
            }
            else if (ShouldAllowComments() && character == Dialect.Comment)
            {
                StringBuilder rawBuilder = new StringBuilder();
                SkipCommentLine(rawBuilder);
                skipped = true;
            }
        }
    }

    internal void SkipCommentLine(StringBuilder rawBuilder)
    {
        while (true)
        {
            int value = ReadIgnoredCharacter(rawBuilder);
            if (value < 0)
            {
                EndOfData = true;
                return;
            }

            char character = (char)value;
            if (character == '\r')
            {
                if (Buffer.Peek() == '\n')
                {
                    ReadIgnoredCharacter(rawBuilder);
                }

                return;
            }

            if (character == '\n')
            {
                return;
            }
        }
    }

    internal void SkipInitialSpaces(StringBuilder rawBuilder)
    {
        while (Buffer.Peek() == ' ')
        {
            ReadTrackedCharacter(rawBuilder);
            if (ShouldStopCurrentRead())
            {
                return;
            }
        }
    }

    internal bool ShouldSkipBlankLines()
    {
        if (Options.SkipBlankLines)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldAllowComments()
    {
        if (!Options.AllowComments)
        {
            return false;
        }

        if (Dialect.HasComment)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldSkipInitialSpace()
    {
        if (Options.SkipInitialSpace)
        {
            return true;
        }

        if (Dialect.SkipInitialSpace)
        {
            return true;
        }

        return false;
    }

    internal void CheckFieldLength(StringBuilder fieldBuilder, StringBuilder rawBuilder)
    {
        if (!ShouldEnforceLimits())
        {
            return;
        }

        if (Options.MaxFieldLength <= 0)
        {
            return;
        }

        if (fieldBuilder.Length > Options.MaxFieldLength)
        {
            AddParserError("MaxFieldLength", "Field length exceeds the configured maximum.", rawBuilder);
        }
    }

    internal void CheckRowLength(StringBuilder rawBuilder)
    {
        if (!ShouldEnforceLimits())
        {
            return;
        }

        if (Options.MaxRowLength <= 0)
        {
            return;
        }

        if (rawBuilder.Length > Options.MaxRowLength)
        {
            AddParserError("MaxRowLength", "Row length exceeds the configured maximum.", rawBuilder);
        }
    }

    internal void CheckColumnCount(List<string?> fields, StringBuilder rawBuilder)
    {
        if (!ShouldEnforceLimits())
        {
            return;
        }

        if (Options.MaxColumnCount <= 0)
        {
            return;
        }

        if (fields.Count > Options.MaxColumnCount)
        {
            AddParserError("MaxColumnCount", "Column count exceeds the configured maximum.", rawBuilder);
        }
    }

    internal bool IsStrictMode()
    {
        if (Options.ErrorMode == CsvErrorMode.Strict)
        {
            return true;
        }

        return false;
    }

    internal bool AllowsRelaxedQuotes()
    {
        if (Options.ErrorMode == CsvErrorMode.Relaxed)
        {
            return true;
        }

        if (Dialect.AllowRelaxedQuotes)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldFailOnMalformedQuote()
    {
        if (AllowsRelaxedQuotes())
        {
            return false;
        }

        if (ShouldRejectBadRows())
        {
            return true;
        }

        if (IsStrictMode())
        {
            return true;
        }

        return false;
    }

    internal bool ShouldEnforceLimits()
    {
        if (CurrentRowRejected)
        {
            return false;
        }

        if (IsStrictMode())
        {
            return true;
        }

        if (ShouldRejectBadRows())
        {
            return true;
        }

        return false;
    }

    internal bool ShouldRejectBadRows()
    {
        if (Options.ErrorMode == CsvErrorMode.SkipBadRow)
        {
            return true;
        }

        if (Options.ErrorMode == CsvErrorMode.CollectRejects)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldCollectRejects()
    {
        if (Options.ErrorMode == CsvErrorMode.CollectRejects)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldStopCurrentRead()
    {
        if (HasParseError)
        {
            return true;
        }

        if (CurrentRowRejected)
        {
            return true;
        }

        return false;
    }

    internal void AddParserError(string code, string message, StringBuilder rawBuilder)
    {
        if (HasParseError)
        {
            return;
        }

        if (CurrentRowRejected)
        {
            return;
        }

        long rejectedRowNumber = RowNumber + 1;
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateParserError(code, message, rejectedRowNumber, CurrentFieldIndex, PhysicalLine, CharacterOffset, rawBuilder.ToString());
        diagnostic.ByteOffset = ByteOffset;
        Diagnostics.Add(diagnostic);
        if (ShouldRejectBadRows())
        {
            CurrentRowRejected = true;
            if (ShouldSkipRemainingRejectedRow(rawBuilder))
            {
                SkipRejectedRow(rawBuilder);
            }

            RowNumber = rejectedRowNumber;
            if (ShouldCollectRejects() || ShouldHandleRejects())
            {
                AddReject(rawBuilder, diagnostic, rejectedRowNumber);
            }

            return;
        }

        HasParseError = true;
        EndOfData = true;
    }

    internal bool ShouldSkipRemainingRejectedRow(StringBuilder rawBuilder)
    {
        if (EndOfData)
        {
            return false;
        }

        if (rawBuilder.Length == 0)
        {
            return true;
        }

        char last = rawBuilder[rawBuilder.Length - 1];
        if (last == '\r')
        {
            return false;
        }

        if (last == '\n')
        {
            return false;
        }

        return true;
    }

    internal void SkipRejectedRow(StringBuilder rawBuilder)
    {
        while (true)
        {
            int value = ReadTrackedCharacter(rawBuilder);
            if (value < 0)
            {
                EndOfData = true;
                return;
            }

            char character = (char)value;
            if (character == '\r')
            {
                if (Buffer.Peek() == '\n')
                {
                    ReadTrackedCharacter(rawBuilder);
                }

                return;
            }

            if (character == '\n')
            {
                return;
            }
        }
    }

    internal void AddReject(StringBuilder rawBuilder, CsvDiagnostic diagnostic, long rowNumber)
    {
        CsvRow row = new CsvRow();
        row.RowNumber = rowNumber;
        row.Line = CurrentRowLine;
        row.Column = CurrentRowColumn;
        row.CharacterOffset = CurrentRowCharacterOffset;
        row.ByteOffset = CurrentRowByteOffset;
        row.RawText = rawBuilder.ToString();
        row.Diagnostics.Add(diagnostic.Clone());
        if (Header.Names.Length > 0)
        {
            row.Header = Header;
        }

        if (ShouldCollectRejects())
        {
            Rejects.Add(row);
        }

        if (ShouldHandleRejects())
        {
            ICsvRejectHandler? rejectHandler = Options.RejectHandler;
            if (rejectHandler != null)
            {
                rejectHandler.Handle(row, diagnostic);
            }
        }
    }

    internal bool ShouldHandleRejects()
    {
        if (!Options.HasRejectHandler)
        {
            return false;
        }

        if (Options.RejectHandler == null)
        {
            return false;
        }

        return true;
    }
}
