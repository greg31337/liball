namespace json;

internal class JsonPrettyFormatter
{
    bool pretty;
    bool spaceAfterColon;
    string indentation;
    string newLine;
    int maximumOutputBytes;

    internal JsonPrettyFormatter(JsonWriterOptions options)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        string indentationText = normalized.Indentation
            ?? throw new global::System.InvalidOperationException(
                "Normalized indentation is required.");
        string newLineText = normalized.NewLine
            ?? throw new global::System.InvalidOperationException(
                "A normalized line ending is required.");
        maximumOutputBytes = limits.MaximumOutputBytes;
        pretty = normalized.Formatting == JsonFormatting.Pretty;
        spaceAfterColon = pretty && !normalized.OmitSpaceAfterColon;
        indentation = indentationText;
        newLine = newLineText;
    }

    internal byte[] FormatContainerStart(
        JsonWriteFrame parent,
        int depth,
        byte openingByte)
    {
        long prefixLength = GetValuePrefixLength(parent, depth);
        byte[] result = CreateOutputBuffer(prefixLength + 1);
        int offset = WriteValuePrefix(result, parent, depth);
        result[offset] = openingByte;
        return result;
    }

    internal byte[] FormatPropertyName(
        JsonWriteFrame frame,
        int depth,
        byte[] encodedName)
    {
        long separatorLength = frame.GetMemberCount() == 0 ? 0 : 1;
        long layoutLength = pretty
            ? newLine.Length + ((long)indentation.Length * depth)
            : 0;
        long suffixLength = spaceAfterColon ? 2 : 1;
        byte[] result = CreateOutputBuffer(
            separatorLength + layoutLength + encodedName.Length +
            suffixLength);
        int offset = 0;
        if (separatorLength != 0)
        {
            result[offset] = JsonSyntaxBytes.ValueSeparator;
            offset++;
        }

        if (pretty)
        {
            offset = CopyLayout(result, offset, depth);
        }

        global::System.Buffer.BlockCopy(
            encodedName,
            0,
            result,
            offset,
            encodedName.Length);
        offset += encodedName.Length;
        result[offset] = JsonSyntaxBytes.NameSeparator;
        offset++;
        if (spaceAfterColon)
        {
            result[offset] = JsonSyntaxBytes.Space;
        }

        return result;
    }

    internal byte[] FormatScalarValue(
        JsonWriteFrame frame,
        int depth,
        byte[] encodedValue)
    {
        long prefixLength = GetValuePrefixLength(frame, depth);
        byte[] result = CreateOutputBuffer(
            prefixLength + encodedValue.Length);
        int offset = WriteValuePrefix(result, frame, depth);
        global::System.Buffer.BlockCopy(
            encodedValue,
            0,
            result,
            offset,
            encodedValue.Length);
        return result;
    }

    internal byte[] FormatContainerEnd(
        JsonWriteFrame frame,
        int depth,
        byte closingByte)
    {
        bool hasContent = frame.GetContainerKind() == JsonContainerKind.Array
            ? frame.GetValueCount() != 0
            : frame.GetMemberCount() != 0;
        long layoutLength = pretty && hasContent
            ? newLine.Length + ((long)indentation.Length * (depth - 1))
            : 0;
        byte[] result = CreateOutputBuffer(layoutLength + 1);
        int offset = 0;
        if (pretty && hasContent)
        {
            offset = CopyLayout(result, offset, depth - 1);
        }

        result[offset] = closingByte;
        return result;
    }

    long GetValuePrefixLength(JsonWriteFrame frame, int depth)
    {
        if (frame.GetContainerKind() != JsonContainerKind.Array)
        {
            return 0;
        }

        long separatorLength = frame.GetValueCount() == 0 ? 0 : 1;
        if (!pretty)
        {
            return separatorLength;
        }

        return separatorLength + newLine.Length +
            ((long)indentation.Length * depth);
    }

    int WriteValuePrefix(
        byte[] destination,
        JsonWriteFrame frame,
        int depth)
    {
        if (frame.GetContainerKind() != JsonContainerKind.Array)
        {
            return 0;
        }

        int offset = 0;
        if (frame.GetValueCount() != 0)
        {
            destination[offset] = JsonSyntaxBytes.ValueSeparator;
            offset++;
        }

        if (pretty)
        {
            offset = CopyLayout(destination, offset, depth);
        }

        return offset;
    }

    int CopyLayout(byte[] destination, int offset, int depth)
    {
        int newLineIndex = 0;
        while (newLineIndex < newLine.Length)
        {
            destination[offset] = (byte)newLine[newLineIndex];
            offset++;
            newLineIndex++;
        }

        int index = 0;
        while (index < depth)
        {
            int indentationIndex = 0;
            while (indentationIndex < indentation.Length)
            {
                destination[offset] = (byte)indentation[indentationIndex];
                offset++;
                indentationIndex++;
            }

            index++;
        }

        return offset;
    }

    byte[] CreateOutputBuffer(long length)
    {
        if (length > maximumOutputBytes ||
            length > global::System.Int32.MaxValue)
        {
            throw CreateFormattingLimitFailure();
        }

        return new byte[(int)length];
    }

    JsonLimitException CreateFormattingLimitFailure()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.OutputByteLimitExceeded;
        diagnostic.Message =
            "The configured output byte limit was exceeded by formatting whitespace.";
        diagnostic.ActualToken = "Formatting";
        return new JsonLimitException(diagnostic);
    }
}
