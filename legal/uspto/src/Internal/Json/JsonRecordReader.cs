namespace uspto;

internal class JsonRecord
{
    internal long RecordNumber = 0L;
    internal long StartOffset = 0L;
    internal int StartLineNumber = 0;
    internal string RawJson = string.Empty;
    internal string[] Headers = new string[0];
    internal global::System.Collections.Generic.Dictionary<string, string> Values = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal global::System.Text.Json.JsonElement Element = default(global::System.Text.Json.JsonElement);

    internal string? ReadValue(string header)
    {
        if (header == null)
        {
            throw new ValidationException("A JSON header name is required.", "header", "REQUIRED", null);
        }

        string? value;
        if (Values.TryGetValue(header, out value))
        {
            return value;
        }

        return null;
    }
}

internal class JsonRecordReader
{
    internal global::System.IO.StreamReader Reader;
    internal bool JsonLines;
    internal string? SourceName;
    internal int MaximumDepth;
    internal int MaximumStringCharacters;
    internal int MaximumBatchRecords;
    internal int MaximumRecordCharacters;
    internal global::System.Threading.CancellationToken CancellationToken;
    internal string[] Headers = new string[0];
    internal global::System.Collections.Generic.List<string> DiscoveredHeaders = new global::System.Collections.Generic.List<string>();
    internal global::System.Collections.Generic.Dictionary<string, bool> HeaderLookup = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
    internal long RecordNumber = 0L;
    internal long CharacterOffset = 0L;
    internal int LineNumber = 1;
    internal int PendingCharacter = -2;
    internal bool PreviousWasCarriageReturn = false;
    internal bool Initialized = false;
    internal bool RootArray = false;
    internal bool RootObjectRead = false;
    internal bool NeedArraySeparator = false;
    internal bool EndOfInput = false;

    internal JsonRecordReader(global::System.IO.Stream input, bool jsonLines, string? sourceName, UsptoLimits limits, global::System.Threading.CancellationToken cancellationToken)
    {
        if (input == null)
        {
            throw new ValidationException("A JSON input stream is required.", "input", "REQUIRED", null);
        }

        if (!input.CanRead)
        {
            throw new ValidationException("The JSON input stream must be readable.", "input", "STREAM_NOT_READABLE", null);
        }

        if (limits == null)
        {
            throw new ValidationException("JSON parsing limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumJsonDepth <= 0)
        {
            throw new ValidationException("The maximum JSON depth must be positive.", "MaximumJsonDepth", "INVALID_LIMIT", null);
        }

        if (limits.MaximumStringCharacters <= 0)
        {
            throw new ValidationException("The maximum JSON string length must be positive.", "MaximumStringCharacters", "INVALID_LIMIT", null);
        }

        if (limits.MaximumResults <= 0)
        {
            throw new ValidationException("The maximum JSON batch size must be positive.", "MaximumResults", "INVALID_LIMIT", null);
        }

        if (limits.MaximumRawResponseBytes <= 0L)
        {
            throw new ValidationException("The maximum JSON record length must be positive.", "MaximumRawResponseBytes", "INVALID_LIMIT", null);
        }

        JsonLines = jsonLines;
        SourceName = sourceName;
        MaximumDepth = limits.MaximumJsonDepth;
        MaximumStringCharacters = limits.MaximumStringCharacters;
        MaximumBatchRecords = limits.MaximumResults;
        MaximumRecordCharacters = global::System.Int32.MaxValue;
        if (limits.MaximumRawResponseBytes < global::System.Int32.MaxValue)
        {
            MaximumRecordCharacters = (int)limits.MaximumRawResponseBytes;
        }

        CancellationToken = cancellationToken;
        global::System.Text.UTF8Encoding fallbackEncoding = new global::System.Text.UTF8Encoding(false, true);
        Reader = new global::System.IO.StreamReader(input, fallbackEncoding, true, 4096, true);
    }

    internal JsonRecord? ReadRecord()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (EndOfInput)
        {
            return null;
        }

        if (JsonLines)
        {
            return ReadJsonLineRecord();
        }

        if (!Initialized)
        {
            Initialized = true;
            int first = ReadSignificantCharacter();
            if (first < 0)
            {
                EndOfInput = true;
                return null;
            }

            if (first == '[')
            {
                RootArray = true;
                return ReadArrayRecord();
            }

            if (first == '{')
            {
                long startOffset = CharacterOffset - 1L;
                int startLine = LineNumber;
                string rawJson = ReadRawValue(first, startOffset, startLine);
                RootObjectRead = true;
                return CreateRecord(rawJson, startOffset, startLine);
            }

            ThrowParseFailure("A JSON document must have an object or array root.", "$", CharacterOffset - 1L, LineNumber);
        }

        if (RootArray)
        {
            return ReadArrayRecord();
        }

        if (RootObjectRead)
        {
            int trailing = ReadSignificantCharacter();
            if (trailing >= 0)
            {
                ThrowParseFailure("Unexpected content follows the root JSON object.", "$", CharacterOffset - 1L, LineNumber);
            }

            EndOfInput = true;
        }

        return null;
    }

    internal JsonRecord[] ReadBatch(int maximumRecords)
    {
        if (maximumRecords <= 0 || maximumRecords > MaximumBatchRecords)
        {
            throw new ValidationException("The JSON batch size is outside the configured range.", "maximumRecords", "BATCH_SIZE_OUT_OF_RANGE", null);
        }

        global::System.Collections.Generic.List<JsonRecord> records = new global::System.Collections.Generic.List<JsonRecord>();
        while (records.Count < maximumRecords)
        {
            JsonRecord? record = ReadRecord();
            if (record == null)
            {
                break;
            }

            records.Add(record);
        }

        JsonRecord[] result = new JsonRecord[records.Count];
        records.CopyTo(result, 0);
        return result;
    }

    internal void Close()
    {
        Reader.Close();
    }

    internal JsonRecord? ReadArrayRecord()
    {
        int first;
        if (NeedArraySeparator)
        {
            int separator = ReadSignificantCharacter();
            if (separator == ']')
            {
                CompleteDocument();
                return null;
            }

            if (separator != ',')
            {
                ThrowParseFailure("A comma or closing bracket is required after a JSON array record.", CreateRecordPath(RecordNumber + 1L), CharacterOffset - 1L, LineNumber);
            }

            first = ReadSignificantCharacter();
            if (first < 0 || first == ']')
            {
                ThrowParseFailure("A JSON array comma must be followed by a record.", CreateRecordPath(RecordNumber + 1L), CharacterOffset, LineNumber);
            }
        }
        else
        {
            first = ReadSignificantCharacter();
            if (first == ']')
            {
                CompleteDocument();
                return null;
            }

            if (first < 0)
            {
                ThrowParseFailure("The JSON array ended before its closing bracket.", "$", CharacterOffset, LineNumber);
            }
        }

        long startOffset = CharacterOffset - 1L;
        int startLine = LineNumber;
        string rawJson = ReadRawValue(first, startOffset, startLine);
        NeedArraySeparator = true;
        return CreateRecord(rawJson, startOffset, startLine);
    }

    internal JsonRecord? ReadJsonLineRecord()
    {
        while (true)
        {
            long lineStartOffset = CharacterOffset;
            int lineStartNumber = LineNumber;
            global::System.Text.StringBuilder line = new global::System.Text.StringBuilder();
            bool sawCharacter = false;
            while (true)
            {
                int read = ReadCharacter();
                if (read < 0)
                {
                    EndOfInput = true;
                    break;
                }

                char character = (char)read;
                if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && Reader.Peek() == '\n')
                    {
                        ReadCharacter();
                    }

                    break;
                }

                sawCharacter = true;
                AppendRawCharacter(line, character, lineStartOffset, lineStartNumber);
            }

            if (!sawCharacter && EndOfInput)
            {
                return null;
            }

            string rawLine = line.ToString();
            int startIndex = 0;
            while (startIndex < rawLine.Length && global::System.Char.IsWhiteSpace(rawLine[startIndex]))
            {
                startIndex += 1;
            }

            int endIndex = rawLine.Length - 1;
            while (endIndex >= startIndex && global::System.Char.IsWhiteSpace(rawLine[endIndex]))
            {
                endIndex -= 1;
            }

            if (endIndex >= startIndex)
            {
                string rawJson = rawLine.Substring(startIndex, endIndex - startIndex + 1);
                return CreateRecord(rawJson, lineStartOffset + startIndex, lineStartNumber);
            }

            if (EndOfInput)
            {
                return null;
            }
        }
    }

    internal string ReadRawValue(int first, long startOffset, int startLine)
    {
        global::System.Text.StringBuilder raw = new global::System.Text.StringBuilder();
        AppendRawCharacter(raw, (char)first, startOffset, startLine);
        if (first == '{' || first == '[')
        {
            global::System.Collections.Generic.List<char> containers = new global::System.Collections.Generic.List<char>();
            containers.Add((char)first);
            bool inString = false;
            bool escaped = false;
            while (containers.Count != 0)
            {
                int read = ReadCharacter();
                if (read < 0)
                {
                    ThrowParseFailure("The JSON record ended before its container was closed.", CreateRecordPath(RecordNumber + 1L), CharacterOffset, LineNumber);
                }

                char character = (char)read;
                AppendRawCharacter(raw, character, startOffset, startLine);
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == '{' || character == '[')
                {
                    containers.Add(character);
                }
                else if (character == '}' || character == ']')
                {
                    char expected = '{';
                    if (character == ']')
                    {
                        expected = '[';
                    }

                    if (containers[containers.Count - 1] != expected)
                    {
                        ThrowParseFailure("A JSON container has a mismatched closing delimiter.", CreateRecordPath(RecordNumber + 1L), CharacterOffset - 1L, LineNumber);
                    }

                    containers.RemoveAt(containers.Count - 1);
                }
            }

            return raw.ToString();
        }

        if (first == '"')
        {
            bool escaped = false;
            while (true)
            {
                int read = ReadCharacter();
                if (read < 0)
                {
                    ThrowParseFailure("The JSON record ended inside a string.", CreateRecordPath(RecordNumber + 1L), CharacterOffset, LineNumber);
                }

                char character = (char)read;
                AppendRawCharacter(raw, character, startOffset, startLine);
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    return raw.ToString();
                }
            }
        }

        while (true)
        {
            int read = ReadCharacter();
            if (read < 0)
            {
                return raw.ToString();
            }

            char character = (char)read;
            if (global::System.Char.IsWhiteSpace(character))
            {
                return raw.ToString();
            }

            if (character == ',' || character == ']')
            {
                PendingCharacter = read;
                return raw.ToString();
            }

            AppendRawCharacter(raw, character, startOffset, startLine);
        }
    }

    internal JsonRecord CreateRecord(string rawJson, long startOffset, int startLine)
    {
        global::System.Text.Json.JsonDocumentOptions options = new global::System.Text.Json.JsonDocumentOptions();
        options.MaxDepth = MaximumDepth;
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(rawJson, options))
        {
            global::System.Text.Json.JsonElement root = document.RootElement;
            ValidateStrings(root, CreateRecordPath(RecordNumber + 1L), startOffset, startLine);
            JsonRecord record = new JsonRecord();
            RecordNumber += 1L;
            record.RecordNumber = RecordNumber;
            record.StartOffset = startOffset;
            record.StartLineNumber = startLine;
            record.RawJson = rawJson;
            record.Element = root.Clone();
            if (root.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                global::System.Collections.Generic.List<string> recordHeaders = new global::System.Collections.Generic.List<string>();
                foreach (global::System.Text.Json.JsonProperty property in root.EnumerateObject())
                {
                    if (!record.Values.ContainsKey(property.Name))
                    {
                        recordHeaders.Add(property.Name);
                    }

                    record.Values[property.Name] = property.Value.GetRawText();
                    DiscoverHeader(property.Name);
                }

                record.Headers = new string[recordHeaders.Count];
                recordHeaders.CopyTo(record.Headers, 0);
            }

            return record;
        }
    }

    internal void ValidateStrings(global::System.Text.Json.JsonElement element, string path, long offset, int lineNumber)
    {
        if (element.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            string? value = element.GetString();
            if (value != null && value.Length > MaximumStringCharacters)
            {
                ThrowParseFailure("A JSON string exceeds the configured character limit.", path, offset, lineNumber);
            }

            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                string propertyPath = path + "[\"" + property.Name + "\"]";
                if (property.Name.Length > MaximumStringCharacters)
                {
                    ThrowParseFailure("A JSON property name exceeds the configured character limit.", propertyPath, offset, lineNumber);
                }

                ValidateStrings(property.Value, propertyPath, offset, lineNumber);
            }
        }
        else if (element.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int index = 0;
            foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
            {
                ValidateStrings(item, path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", offset, lineNumber);
                index += 1;
            }
        }
    }

    internal void DiscoverHeader(string header)
    {
        if (HeaderLookup.ContainsKey(header))
        {
            return;
        }

        HeaderLookup.Add(header, true);
        DiscoveredHeaders.Add(header);
        Headers = new string[DiscoveredHeaders.Count];
        DiscoveredHeaders.CopyTo(Headers, 0);
    }

    internal void AppendRawCharacter(global::System.Text.StringBuilder raw, char character, long startOffset, int startLine)
    {
        if (raw.Length >= MaximumRecordCharacters)
        {
            ThrowParseFailure("A JSON record exceeds the configured raw character limit.", CreateRecordPath(RecordNumber + 1L), startOffset, startLine);
        }

        raw.Append(character);
    }

    internal int ReadSignificantCharacter()
    {
        while (true)
        {
            int read = ReadCharacter();
            if (read < 0 || !global::System.Char.IsWhiteSpace((char)read))
            {
                return read;
            }
        }
    }

    internal int ReadCharacter()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (PendingCharacter != -2)
        {
            int pending = PendingCharacter;
            PendingCharacter = -2;
            return pending;
        }

        int read = Reader.Read();
        if (read < 0)
        {
            return -1;
        }

        CharacterOffset += 1L;
        char character = (char)read;
        if (character == '\r')
        {
            LineNumber += 1;
            PreviousWasCarriageReturn = true;
        }
        else if (character == '\n')
        {
            if (!PreviousWasCarriageReturn)
            {
                LineNumber += 1;
            }

            PreviousWasCarriageReturn = false;
        }
        else
        {
            PreviousWasCarriageReturn = false;
        }

        return read;
    }

    internal void CompleteDocument()
    {
        int trailing = ReadSignificantCharacter();
        if (trailing >= 0)
        {
            ThrowParseFailure("Unexpected content follows the root JSON array.", "$", CharacterOffset - 1L, LineNumber);
        }

        EndOfInput = true;
    }

    internal string CreateRecordPath(long recordNumber)
    {
        return "$[" + recordNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
    }

    internal void ThrowParseFailure(string message, string path, long offset, int lineNumber)
    {
        throw new ParseException(message, DocumentFormat.Json, SourceName, path, offset, true, lineNumber, true, null);
    }
}
