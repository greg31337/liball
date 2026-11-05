namespace whois;

/// <summary>
/// Builds an ordered generic line, field, and record view of decoded WHOIS text.
/// </summary>
public class WhoisResponseParser : IWhoisResponseParser
{
    /// <summary>
    /// Initializes the built-in generic response parser.
    /// </summary>
    public WhoisResponseParser()
    {
    }

    /// <summary>
    /// Parses decoded text into ordered lines, fields, continuations, and field-bearing records.
    /// </summary>
    /// <param name="response">The decoded response view.</param>
    /// <param name="limits">The parser resource limits.</param>
    /// <returns>The status-bearing generic parsed response.</returns>
    /// <exception cref="System.ArgumentNullException">The response or limits are <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">A parser resource limit is invalid.</exception>
    /// <exception cref="System.InvalidOperationException">The decoded response contains null text.</exception>
    public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
    {
        InputGuard.RequireReference(response, "response");
        InputGuard.RequireReference(limits, "limits");
        InputGuard.ValidateLimits(limits, string.Empty, WhoisQueryKind.FreeForm);
        if (response.Text == null)
        {
            throw new System.InvalidOperationException("The decoded response contains null text.");
        }

        WhoisParsedResponse result = new WhoisParsedResponse();
        result.TextResponse = response;
        if (!response.IsDecoded)
        {
            result.Error = response.Error;
            if (result.Error == null)
            {
                result.Error = new System.InvalidOperationException("The response must be decoded before parsing.");
            }
            return result;
        }

        int lineCount = LineScanner.CountLines(response.Text);
        if (lineCount > limits.MaximumLines)
        {
            result.Error = new System.InvalidOperationException("The decoded response exceeds the configured line limit.");
            return result;
        }

        string[] rawLines = LineScanner.ScanLines(response.Text);
        System.Collections.Generic.List<WhoisLine> lines = new System.Collections.Generic.List<WhoisLine>();
        System.Collections.Generic.List<WhoisField> fields = new System.Collections.Generic.List<WhoisField>();
        int continuationFieldIndex = -1;
        int lineIndex = 0;
        while (lineIndex < rawLines.Length)
        {
            string rawContent = rawLines[lineIndex];
            string normalizedContent = rawContent.Trim();
            WhoisLine line = new WhoisLine();
            line.LineNumber = lineIndex + 1;
            line.RawContent = rawContent;
            line.NormalizedContent = normalizedContent;

            if (normalizedContent.Length == 0)
            {
                line.Kind = WhoisLineKind.Blank;
                continuationFieldIndex = -1;
            }
            else if (rawContent[0] == '%' || rawContent[0] == '#')
            {
                line.Kind = WhoisLineKind.Comment;
                continuationFieldIndex = -1;
            }
            else if ((rawContent[0] == ' ' || rawContent[0] == '\t') && continuationFieldIndex >= 0)
            {
                line.Kind = WhoisLineKind.Continuation;
                line.FieldIndex = continuationFieldIndex;
                WhoisField continuedField = fields[continuationFieldIndex];
                string continuationValue = TrimSeparatorWhitespace(rawContent, 0);
                continuedField.Value = continuedField.Value + "\n" + continuationValue;
            }
            else
            {
                int separatorIndex = rawContent.IndexOf(':');
                string originalName = string.Empty;
                if (separatorIndex > 0)
                {
                    originalName = rawContent.Substring(0, separatorIndex).Trim();
                }
                if (originalName.Length > 0)
                {
                    line.Kind = WhoisLineKind.Field;
                    if (fields.Count >= limits.MaximumFields)
                    {
                        lines.Add(line);
                        result.Lines = lines.ToArray();
                        result.Fields = fields.ToArray();
                        result.Error = new System.InvalidOperationException("The decoded response exceeds the configured field limit.");
                        return result;
                    }

                    WhoisField field = new WhoisField();
                    field.Index = fields.Count;
                    field.SourceLine = line.LineNumber;
                    field.OriginalName = originalName;
                    field.CanonicalName = originalName.ToLowerInvariant();
                    field.RawValue = TrimSeparatorWhitespace(rawContent, separatorIndex + 1);
                    field.Value = field.RawValue;
                    fields.Add(field);
                    line.FieldIndex = field.Index;
                    continuationFieldIndex = field.Index;
                }
                else
                {
                    line.Kind = WhoisLineKind.Text;
                    continuationFieldIndex = -1;
                }
            }
            lines.Add(line);
            lineIndex = lineIndex + 1;
        }

        result.Lines = lines.ToArray();
        result.Fields = fields.ToArray();
        result.Records = BuildRecords(result.Lines, result.Fields);
        result.IsParsed = true;
        return result;
    }

    /// <summary>
    /// Finds fields whose canonical name case-insensitively matches the supplied name.
    /// </summary>
    /// <param name="response">The parsed response to search.</param>
    /// <param name="name">The field name to match without regard to casing.</param>
    /// <returns>All matching fields in source order, including duplicates.</returns>
    /// <exception cref="System.ArgumentNullException">The response or name is <c>null</c>.</exception>
    public WhoisField[] FindFields(WhoisParsedResponse response, string name)
    {
        InputGuard.RequireReference(response, "response");
        InputGuard.RequireReference(name, "name");
        System.Collections.Generic.List<WhoisField> matches = new System.Collections.Generic.List<WhoisField>();
        int index = 0;
        while (index < response.Fields.Length)
        {
            WhoisField field = response.Fields[index];
            if (string.Equals(field.CanonicalName, name, System.StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(field);
            }
            index = index + 1;
        }
        return matches.ToArray();
    }

    internal static string TrimSeparatorWhitespace(string text, int startIndex)
    {
        int index = startIndex;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }
        return text.Substring(index);
    }

    internal static WhoisRecord[] BuildRecords(WhoisLine[] lines, WhoisField[] fields)
    {
        System.Collections.Generic.List<WhoisRecord> records = new System.Collections.Generic.List<WhoisRecord>();
        int segmentStart = 0;
        int nextFieldIndex = 0;
        int lineIndex = 0;
        while (lineIndex <= lines.Length)
        {
            if (lineIndex == lines.Length || lines[lineIndex].Kind == WhoisLineKind.Blank)
            {
                System.Collections.Generic.List<WhoisField> recordFields = new System.Collections.Generic.List<WhoisField>();
                while (nextFieldIndex < fields.Length && fields[nextFieldIndex].SourceLine - 1 < lineIndex)
                {
                    int sourceIndex = fields[nextFieldIndex].SourceLine - 1;
                    if (sourceIndex >= segmentStart && sourceIndex < lineIndex)
                    {
                        recordFields.Add(fields[nextFieldIndex]);
                    }
                    nextFieldIndex = nextFieldIndex + 1;
                }

                if (recordFields.Count > 0)
                {
                    WhoisRecord record = new WhoisRecord();
                    record.Index = records.Count;
                    record.Fields = recordFields.ToArray();
                    System.Collections.Generic.List<WhoisLine> recordLines = new System.Collections.Generic.List<WhoisLine>();
                    int recordLineIndex = segmentStart;
                    while (recordLineIndex < lineIndex)
                    {
                        recordLines.Add(lines[recordLineIndex]);
                        recordLineIndex = recordLineIndex + 1;
                    }
                    record.Lines = recordLines.ToArray();
                    int fieldIndex = 0;
                    while (fieldIndex < record.Fields.Length)
                    {
                        record.Fields[fieldIndex].RecordIndex = record.Index;
                        fieldIndex = fieldIndex + 1;
                    }
                    records.Add(record);
                }
                segmentStart = lineIndex + 1;
            }
            lineIndex = lineIndex + 1;
        }
        return records.ToArray();
    }
}
