using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CborDiagnosticParserCore
{
    internal string? Source;
    internal CborDiagnosticOptions? Options;
    internal CborDiagnosticLexer Lexer;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? ErrorPath;
    internal string NumberPath = "$";
    internal int IndicatorOffset = -1;
    internal CborDiagnosticEmbeddedState Embedded;
    internal Dictionary<string, int> ValueOffsets = new Dictionary<string, int>();

    internal CborDiagnosticParserCore(
        string? source,
        CborDiagnosticOptions? options)
    {
        Source = source;
        Options = options;
        Lexer = new CborDiagnosticLexer(
            source ?? "",
            options != null && options.UseExtendedNotation
        );
        Embedded = new CborDiagnosticEmbeddedState(
            options == null ? -1 : options.MaxEmbeddedDepth,
            options == null ? -1 : options.MaxEmbeddedItems,
            options == null ? -1 : options.MaxEmbeddedBytes
        );
    }

    internal CborReadResult Parse()
    {
        if (!ValidateOptions())
        {
            return CreateResult();
        }
        if (Source == null)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_TEXT_MISSING",
                "Diagnostic notation text is required.",
                -1,
                "$"
            );
            return CreateResult();
        }
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure("$");
            return CreateResult();
        }
        if (Lexer.Offset >= Lexer.Text.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_UNEXPECTED_END",
                "One diagnostic data item is required.",
                Lexer.Offset,
                "$"
            );
            return CreateResult();
        }
        CborValue? value = ParseValue(0, "$" );
        if (value == null)
        {
            return CreateResult();
        }
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure("$");
            return CreateResult();
        }
        if (Lexer.Offset != Lexer.Text.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_TRAILING_DATA",
                "Characters remain after the complete diagnostic data item.",
                Lexer.Offset,
                "$"
            );
            return CreateResult();
        }
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Done;
        result.Value = value;
        result.Consumed = Source.Length;
        result.Remaining = 0;
        result.SourceOffset = 0;
        return result;
    }

    internal List<CborReadResult> ParseSequence()
    {
        List<CborReadResult> results = new List<CborReadResult>();
        if (!ValidateOptions())
        {
            CborReadResult setup = CreateResult();
            setup.ItemIndex = 0;
            results.Add(setup);
            return results;
        }
        if (Source == null)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_TEXT_MISSING",
                "Diagnostic sequence text is required.",
                -1,
                "$[0]"
            );
            CborReadResult missing = CreateResult();
            missing.ItemIndex = 0;
            results.Add(missing);
            return results;
        }
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure("$[0]");
            CborReadResult trivia = CreateResult();
            trivia.ItemIndex = 0;
            results.Add(trivia);
            return results;
        }
        if (Lexer.Offset >= Lexer.Text.Length)
        {
            return results;
        }
        int index = 0;
        bool complete = false;
        while (!complete)
        {
            int itemOffset = Lexer.Offset;
            string path = $"$[{index}]";
            CborValue? value = ParseValue(0, path);
            if (value == null)
            {
                CborReadResult failure = CreateResult();
                failure.ItemIndex = index;
                failure.SourceOffset = itemOffset;
                failure.Remaining = Source.Length - itemOffset;
                results.Add(failure);
                return results;
            }
            int itemEnd = Lexer.Offset;
            CborReadResult item = new CborReadResult();
            item.Status = CborReadStatus.Done;
            item.Value = value;
            item.Consumed = itemEnd - itemOffset;
            item.Remaining = Source.Length - itemEnd;
            item.ItemIndex = index;
            item.SourceOffset = itemOffset;
            results.Add(item);
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                CborReadResult failure = CreateResult();
                failure.ItemIndex = index + 1;
                failure.SourceOffset = Lexer.Offset;
                failure.Remaining = Source.Length - Lexer.Offset;
                results.Add(failure);
                return results;
            }
            if (Lexer.Offset >= Lexer.Text.Length)
            {
                item.Remaining = 0;
                complete = true;
            }
            else if (Lexer.Text[Lexer.Offset] == ',')
            {
                Lexer.Offset = Lexer.Offset + 1;
                if (!Lexer.SkipTrivia())
                {
                    CopyLexerFailure($"$[{index + 1}]");
                    CborReadResult failure = CreateResult();
                    failure.ItemIndex = index + 1;
                    failure.SourceOffset = Lexer.Offset;
                    failure.Remaining = Source.Length - Lexer.Offset;
                    results.Add(failure);
                    return results;
                }
                index = index + 1;
                if (Lexer.Offset >= Lexer.Text.Length)
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_SEQUENCE_VALUE_EXPECTED",
                        "A diagnostic sequence comma must be followed by another value.",
                        Lexer.Offset,
                        $"$[{index}]"
                    );
                    CborReadResult failure = CreateResult();
                    failure.ItemIndex = index;
                    failure.SourceOffset = Lexer.Offset;
                    failure.Remaining = 0;
                    results.Add(failure);
                    return results;
                }
            }
            else
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_SEQUENCE_DELIMITER_EXPECTED",
                    "Diagnostic sequence items must be separated by commas.",
                    Lexer.Offset,
                    $"$[{index + 1}]"
                );
                CborReadResult failure = CreateResult();
                failure.ItemIndex = index + 1;
                failure.SourceOffset = Lexer.Offset;
                failure.Remaining = Source.Length - Lexer.Offset;
                results.Add(failure);
                return results;
            }
        }
        return results;
    }

    internal bool ValidateOptions()
    {
        if (Options == null)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_OPTIONS_MISSING",
                "Diagnostic parsing options are required.",
                -1,
                "$"
            );
            return false;
        }
        if (Options.MaxNestingDepth < 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_DEPTH_INVALID",
                "The diagnostic nesting limit cannot be negative.",
                -1,
                "$"
            );
            return false;
        }
        if (Options.MaxCharacters < 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_CHARACTER_LIMIT_INVALID",
                "The diagnostic character limit cannot be negative.",
                -1,
                "$"
            );
            return false;
        }
        if (Source != null && Source.Length > Options.MaxCharacters)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "The diagnostic input exceeds the configured character limit.",
                Options.MaxCharacters,
                "$"
            );
            return false;
        }
        if (!Embedded.Validate())
        {
            CopyEmbeddedFailure();
            return false;
        }
        return true;
    }

    internal CborValue? ParseValue(int depth, string path)
    {
        if (Options == null)
        {
            return null;
        }
        if (depth > Options.MaxNestingDepth)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_MAX_NESTING_DEPTH",
                "The diagnostic value exceeds the configured nesting limit.",
                Lexer.Offset,
                path
            );
            return null;
        }
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset >= Lexer.Text.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_UNEXPECTED_END",
                "The diagnostic input ended before a value was available.",
                Lexer.Offset,
                path
            );
            return null;
        }
        int valueOffset = Lexer.Offset;
        char current = Lexer.Text[Lexer.Offset];
        CborValue? value;
        if (current == '[')
        {
            value = ParseArray(depth, path);
        }
        else if (current == '{')
        {
            value = ParseMap(depth, path);
        }
        else if (current == '"')
        {
            value = ParseTextString(depth, path);
        }
        else if (Lexer.IsByteStringStart() || current == '<')
        {
            value = ParseByteString(depth, path);
        }
        else if (current == '(')
        {
            value = ParseChunkedString(depth, path);
        }
        else if (current == '-'
            || current == '+'
            || current >= '0' && current <= '9')
        {
            value = ParseNumberOrTag(depth, path);
        }
        else
        {
            value = ParseWord(depth, path);
        }
        if (value == null)
        {
            return null;
        }
        if (!ApplyTrailingIndicator(value, path))
        {
            return null;
        }
        ValueOffsets[path] = valueOffset;
        return value;
    }

    internal CborValue? ParseArray(int depth, string path)
    {
        Lexer.Offset = Lexer.Offset + 1;
        List<CborValue> items = new List<CborValue>();
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        int containerIndicator = ReadEncodingIndicator();
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset < Lexer.Text.Length && Lexer.Text[Lexer.Offset] == ']')
        {
            Lexer.Offset = Lexer.Offset + 1;
            CborValue empty = new CborValue();
            empty.Kind = CborValueKind.Array;
            empty.ArrayItems = items;
            if (!ApplyContainerIndicator(empty, containerIndicator, path))
            {
                return null;
            }
            return empty;
        }
        int index = 0;
        bool complete = false;
        while (!complete)
        {
            CborValue? item = ParseValue(
                depth + 1,
                $"{path}[{index}]"
            );
            if (item == null)
            {
                return null;
            }
            items.Add(item);
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (Lexer.Offset >= Lexer.Text.Length)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_ARRAY_UNTERMINATED",
                    "The diagnostic array has no closing bracket.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            char delimiter = Lexer.Text[Lexer.Offset];
            if (delimiter == ']')
            {
                Lexer.Offset = Lexer.Offset + 1;
                complete = true;
            }
            else if (delimiter == ',')
            {
                Lexer.Offset = Lexer.Offset + 1;
                if (!Lexer.SkipTrivia())
                {
                    CopyLexerFailure(path);
                    return null;
                }
                if (Lexer.Offset < Lexer.Text.Length
                    && Lexer.Text[Lexer.Offset] == ']')
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_VALUE_EXPECTED",
                        "A trailing array comma must be followed by a value.",
                        Lexer.Offset,
                        $"{path}[{index + 1}]"
                    );
                    return null;
                }
            }
            else
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_ARRAY_DELIMITER_EXPECTED",
                    "An array item must be followed by a comma or closing bracket.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            index = index + 1;
        }
        CborValue array = new CborValue();
        array.Kind = CborValueKind.Array;
        array.ArrayItems = items;
        if (!ApplyContainerIndicator(array, containerIndicator, path))
        {
            return null;
        }
        return array;
    }

    internal CborValue? ParseMap(int depth, string path)
    {
        Lexer.Offset = Lexer.Offset + 1;
        List<CborMapEntry> entries = new List<CborMapEntry>();
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        int containerIndicator = ReadEncodingIndicator();
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset < Lexer.Text.Length && Lexer.Text[Lexer.Offset] == '}')
        {
            Lexer.Offset = Lexer.Offset + 1;
            CborValue empty = new CborValue();
            empty.Kind = CborValueKind.Map;
            empty.MapEntries = entries;
            if (!ApplyContainerIndicator(empty, containerIndicator, path))
            {
                return null;
            }
            return empty;
        }
        int index = 0;
        bool complete = false;
        while (!complete)
        {
            CborValue? key = ParseValue(
                depth + 1,
                $"{path}[{index}].key"
            );
            if (key == null || !Lexer.SkipTrivia())
            {
                CopyLexerFailure($"{path}[{index}].key");
                return null;
            }
            if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != ':')
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_MAP_COLON_EXPECTED",
                    "A diagnostic map key must be followed by a colon.",
                    Lexer.Offset,
                    $"{path}[{index}].key"
                );
                return null;
            }
            Lexer.Offset = Lexer.Offset + 1;
            CborValue? mapValue = ParseValue(
                depth + 1,
                $"{path}[{index}].value"
            );
            if (mapValue == null)
            {
                return null;
            }
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = mapValue;
            entries.Add(entry);
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (Lexer.Offset >= Lexer.Text.Length)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_MAP_UNTERMINATED",
                    "The diagnostic map has no closing brace.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            char delimiter = Lexer.Text[Lexer.Offset];
            if (delimiter == '}')
            {
                Lexer.Offset = Lexer.Offset + 1;
                complete = true;
            }
            else if (delimiter == ',')
            {
                Lexer.Offset = Lexer.Offset + 1;
                if (!Lexer.SkipTrivia())
                {
                    CopyLexerFailure(path);
                    return null;
                }
                if (Lexer.Offset < Lexer.Text.Length
                    && Lexer.Text[Lexer.Offset] == '}')
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_VALUE_EXPECTED",
                        "A trailing map comma must be followed by an entry.",
                        Lexer.Offset,
                        $"{path}[{index + 1}].key"
                    );
                    return null;
                }
            }
            else
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_MAP_DELIMITER_EXPECTED",
                    "A map entry must be followed by a comma or closing brace.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            index = index + 1;
        }
        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.MapEntries = entries;
        if (!ApplyContainerIndicator(map, containerIndicator, path))
        {
            return null;
        }
        return map;
    }

    internal CborValue? ParseTextString(int depth, string path)
    {
        CborDiagnosticLexeme? first = Lexer.ReadQuotedText('"');
        if (first == null)
        {
            CopyLexerFailure(path);
            return null;
        }
        StringBuilder combined = new StringBuilder(first.Text ?? "");
        bool scanning = Options != null && Options.UseExtendedNotation;
        while (scanning)
        {
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (!Lexer.TriviaConsumed)
            {
                scanning = false;
            }
            else if (Lexer.Offset < Lexer.Text.Length
                && Lexer.Text[Lexer.Offset] == '"')
            {
                CborDiagnosticLexeme? textPart = Lexer.ReadQuotedText('"');
                if (textPart == null)
                {
                    CopyLexerFailure(path);
                    return null;
                }
                combined.Append(textPart.Text ?? "");
            }
            else if (IsByteSegmentStart())
            {
                CborDiagnosticLexeme? bytePart = ReadByteSegment(depth, path);
                if (bytePart == null)
                {
                    CopyLexerFailure(path);
                    return null;
                }
                CborUtf8Result decoded = new CborUtf8().ProcessChunk(
                    bytePart.Bytes ?? [],
                    true
                );
                if (decoded.Status != CborReadStatus.Done)
                {
                    SetFailure(
                        decoded.ErrorCode ?? "CBOR_DIAGNOSTIC_TEXT_BYTES_INVALID_UTF8",
                        decoded.ErrorMessage
                            ?? "A byte-string segment in concatenated text is not UTF-8.",
                        bytePart.Start,
                        path
                    );
                    return null;
                }
                combined.Append(decoded.Text);
            }
            else
            {
                scanning = false;
            }
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.TextString;
        value.TextStringValue = combined.ToString();
        return value;
    }

    internal CborValue? ParseByteString(int depth, string path)
    {
        CborDiagnosticLexeme? first = ReadByteSegment(depth, path);
        if (first == null)
        {
            CopyLexerFailure(path);
            return null;
        }
        List<byte> combined = new List<byte>(first.Bytes ?? []);
        bool scanning = Options != null && Options.UseExtendedNotation;
        while (scanning)
        {
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (!Lexer.TriviaConsumed || !IsByteSegmentStart())
            {
                scanning = false;
            }
            else
            {
                CborDiagnosticLexeme? part = ReadByteSegment(depth, path);
                if (part == null)
                {
                    CopyLexerFailure(path);
                    return null;
                }
                combined.AddRange(part.Bytes ?? []);
            }
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.ByteString;
        value.ByteStringValue = combined.ToArray();
        return value;
    }

    internal bool IsByteSegmentStart()
    {
        return Lexer.IsByteStringStart()
            || Lexer.Offset < Lexer.Text.Length
                && Lexer.Text[Lexer.Offset] == '<';
    }

    internal CborDiagnosticLexeme? ReadByteSegment(int depth, string path)
    {
        if (Lexer.Offset < Lexer.Text.Length && Lexer.Text[Lexer.Offset] == '<')
        {
            return ReadEmbeddedByteString(depth, path);
        }
        CborDiagnosticLexeme? lexeme = Lexer.ReadByteString();
        if (lexeme == null)
        {
            CopyLexerFailure(path);
        }
        return lexeme;
    }

    internal CborDiagnosticLexeme? ReadEmbeddedByteString(int depth, string path)
    {
        int start = Lexer.Offset;
        if (Options == null || !Options.UseExtendedNotation)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED",
                "Embedded CBOR byte strings require extended diagnostic notation.",
                start,
                path
            );
            return null;
        }
        if (Lexer.Offset + 1 >= Lexer.Text.Length
            || Lexer.Text[Lexer.Offset] != '<'
            || Lexer.Text[Lexer.Offset + 1] != '<')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_EMBEDDED_OPEN_INVALID",
                "An embedded CBOR byte string must start with two less-than signs.",
                start,
                path
            );
            return null;
        }
        if (!Embedded.Enter(start, path))
        {
            CopyEmbeddedFailure();
            return null;
        }
        Lexer.Offset = Lexer.Offset + 2;
        if (!Lexer.SkipTrivia())
        {
            Embedded.Leave();
            CopyLexerFailure(path);
            return null;
        }
        List<byte> bytes = new List<byte>();
        if (Lexer.Offset + 1 < Lexer.Text.Length
            && Lexer.Text[Lexer.Offset] == '>'
            && Lexer.Text[Lexer.Offset + 1] == '>')
        {
            Lexer.Offset = Lexer.Offset + 2;
            Embedded.Leave();
            CborDiagnosticLexeme empty = new CborDiagnosticLexeme();
            empty.Start = start;
            empty.Bytes = [];
            return empty;
        }
        int index = 0;
        bool complete = false;
        while (!complete)
        {
            string itemPath = $"{path}<embedded:{index}>";
            int itemOffset = Lexer.Offset;
            CborValue? item = ParseValue(depth + 1, itemPath);
            if (item == null)
            {
                Embedded.Leave();
                return null;
            }
            byte[]? encoded = EncodeEmbeddedItem(item, itemOffset, itemPath);
            if (encoded == null)
            {
                Embedded.Leave();
                return null;
            }
            bytes.AddRange(encoded);
            if (!Lexer.SkipTrivia())
            {
                Embedded.Leave();
                CopyLexerFailure(itemPath);
                return null;
            }
            if (Lexer.Offset + 1 < Lexer.Text.Length
                && Lexer.Text[Lexer.Offset] == '>'
                && Lexer.Text[Lexer.Offset + 1] == '>')
            {
                Lexer.Offset = Lexer.Offset + 2;
                complete = true;
            }
            else if (Lexer.Offset < Lexer.Text.Length
                && Lexer.Text[Lexer.Offset] == ',')
            {
                Lexer.Offset = Lexer.Offset + 1;
                if (!Lexer.SkipTrivia())
                {
                    Embedded.Leave();
                    CopyLexerFailure(itemPath);
                    return null;
                }
                index = index + 1;
                if (Lexer.Offset + 1 < Lexer.Text.Length
                    && Lexer.Text[Lexer.Offset] == '>'
                    && Lexer.Text[Lexer.Offset + 1] == '>')
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_EMBEDDED_VALUE_EXPECTED",
                        "An embedded CBOR comma must be followed by another value.",
                        Lexer.Offset,
                        $"{path}<embedded:{index}>"
                    );
                    Embedded.Leave();
                    return null;
                }
            }
            else if (Lexer.Offset >= Lexer.Text.Length)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_EMBEDDED_UNTERMINATED",
                    "The embedded CBOR byte string has no closing delimiter.",
                    Lexer.Offset,
                    path
                );
                Embedded.Leave();
                return null;
            }
            else
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_EMBEDDED_DELIMITER_EXPECTED",
                    "An embedded CBOR item must be followed by a comma or two greater-than signs.",
                    Lexer.Offset,
                    itemPath
                );
                Embedded.Leave();
                return null;
            }
        }
        Embedded.Leave();
        CborDiagnosticLexeme result = new CborDiagnosticLexeme();
        result.Start = start;
        result.Bytes = bytes.ToArray();
        return result;
    }

    internal byte[]? EncodeEmbeddedItem(
        CborValue value,
        int sourceOffset,
        string path)
    {
        if (Options == null)
        {
            return null;
        }
        CborEncodeOptions encoding = new CborEncodeOptions();
        encoding.PreserveRepresentation = true;
        encoding.MaxNestingDepth = Options.MaxNestingDepth;
        encoding.MaxTotalItems = Options.MaxEmbeddedItems - Embedded.Items;
        encoding.MaxOutputBytes = Options.MaxEmbeddedBytes - Embedded.Bytes;
        CborSizeCalculator calculator = new CborSizeCalculator(encoding);
        if (!calculator.Calculate(value))
        {
            string errorPath = CreateEmbeddedPath(path, calculator.ErrorPath);
            string errorCode = calculator.ErrorCode
                ?? "CBOR_DIAGNOSTIC_EMBEDDED_ENCODE_FAILED";
            string errorMessage = calculator.ErrorMessage
                ?? "The embedded diagnostic value cannot be encoded as CBOR.";
            if (errorCode == "CBOR_MAX_TOTAL_ITEMS")
            {
                errorCode = "CBOR_DIAGNOSTIC_MAX_EMBEDDED_ITEMS";
                errorMessage = "The diagnostic value exceeds the embedded CBOR item limit.";
            }
            else if (errorCode == "CBOR_MAX_OUTPUT_BYTES")
            {
                errorCode = "CBOR_DIAGNOSTIC_MAX_EMBEDDED_BYTES";
                errorMessage = "The diagnostic value exceeds the embedded CBOR byte limit.";
            }
            SetFailure(
                errorCode,
                errorMessage,
                FindValueOffset(errorPath, sourceOffset),
                errorPath
            );
            return null;
        }
        if (!Embedded.AddItem(
            calculator.TotalItems,
            calculator.RequiredBytes,
            sourceOffset,
            path))
        {
            CopyEmbeddedFailure();
            return null;
        }
        if (calculator.RequiredBytes > int.MaxValue)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_EMBEDDED_SIZE_OUT_OF_RANGE",
                "The embedded CBOR byte string is too large for a byte-array result.",
                sourceOffset,
                path
            );
            return null;
        }
        byte[] encoded = new byte[(int)calculator.RequiredBytes];
        CborEncodingPlanWriter writer = new CborEncodingPlanWriter(encoding);
        if (!writer.WritePlan(calculator, encoded))
        {
            string errorPath = CreateEmbeddedPath(path, writer.ErrorPath);
            SetFailure(
                writer.ErrorCode ?? "CBOR_DIAGNOSTIC_EMBEDDED_ENCODE_FAILED",
                writer.ErrorMessage
                    ?? "The embedded diagnostic value could not be encoded as CBOR.",
                FindValueOffset(errorPath, sourceOffset),
                errorPath
            );
            return null;
        }
        return encoded;
    }

    internal string CreateEmbeddedPath(string prefix, string? itemPath)
    {
        if (itemPath == null || itemPath == "$")
        {
            return prefix;
        }
        if (itemPath.Length > 0 && itemPath[0] == '$')
        {
            return prefix + itemPath.Substring(1);
        }
        return prefix;
    }

    internal int FindValueOffset(string path, int fallback)
    {
        if (ValueOffsets.ContainsKey(path))
        {
            return ValueOffsets[path];
        }
        return fallback;
    }

    internal CborValue? ParseChunkedString(int depth, string path)
    {
        int start = Lexer.Offset;
        Lexer.Offset = Lexer.Offset + 1;
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != '_')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_CHUNK_INDICATOR_EXPECTED",
                "An indefinite-length string must start with an underscore indicator.",
                Lexer.Offset,
                path
            );
            return null;
        }
        Lexer.Offset = Lexer.Offset + 1;
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] == ')')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_CHUNK_TYPE_AMBIGUOUS",
                "An indefinite-length string requires at least one typed chunk.",
                start,
                path
            );
            return null;
        }
        bool textChunks = Lexer.Text[Lexer.Offset] == '"';
        bool byteChunks = IsByteSegmentStart()
            || Lexer.Offset <= Lexer.Text.Length - 2
                && Lexer.Text[Lexer.Offset] == '\''
                && Lexer.Text[Lexer.Offset + 1] == '\'';
        if (!textChunks && !byteChunks)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_CHUNK_STRING_EXPECTED",
                "An indefinite-length string requires text or byte-string chunks.",
                Lexer.Offset,
                path
            );
            return null;
        }
        StringBuilder text = new StringBuilder();
        List<byte> bytes = new List<byte>();
        List<int> chunkLengths = new List<int>();
        bool complete = false;
        while (!complete)
        {
            if (textChunks)
            {
                if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != '"')
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_CHUNK_TYPE_MISMATCH",
                        "Every chunk must have the same string type.",
                        Lexer.Offset,
                        path
                    );
                    return null;
                }
                CborDiagnosticLexeme? chunk = Lexer.ReadQuotedText('"');
                if (chunk == null)
                {
                    CopyLexerFailure(path);
                    return null;
                }
                text.Append(chunk.Text ?? "");
                chunkLengths.Add(Encoding.UTF8.GetByteCount(chunk.Text ?? ""));
            }
            else
            {
                if (Lexer.Offset <= Lexer.Text.Length - 2
                    && Lexer.Text[Lexer.Offset] == '\''
                    && Lexer.Text[Lexer.Offset + 1] == '\''
                    && (Options == null || !Options.UseExtendedNotation))
                {
                    Lexer.Offset = Lexer.Offset + 2;
                    chunkLengths.Add(0);
                }
                else
                {
                    if (!IsByteSegmentStart())
                    {
                        SetFailure(
                            "CBOR_DIAGNOSTIC_CHUNK_TYPE_MISMATCH",
                            "Every chunk must have the same string type.",
                            Lexer.Offset,
                            path
                        );
                        return null;
                    }
                    CborDiagnosticLexeme? chunk = ReadByteSegment(depth, path);
                    if (chunk == null)
                    {
                        CopyLexerFailure(path);
                        return null;
                    }
                    bytes.AddRange(chunk.Bytes ?? []);
                    chunkLengths.Add((chunk.Bytes ?? []).Length);
                }
            }
            if (!SkipEncodingIndicators(path))
            {
                return null;
            }
            if (!Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (Lexer.Offset >= Lexer.Text.Length)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_CHUNK_STRING_UNTERMINATED",
                    "The indefinite-length string has no closing parenthesis.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            if (Lexer.Text[Lexer.Offset] == ')')
            {
                Lexer.Offset = Lexer.Offset + 1;
                complete = true;
            }
            else if (Lexer.Text[Lexer.Offset] == ',')
            {
                Lexer.Offset = Lexer.Offset + 1;
                if (!Lexer.SkipTrivia())
                {
                    CopyLexerFailure(path);
                    return null;
                }
                if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] == ')')
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_CHUNK_STRING_EXPECTED",
                        "A chunk comma must be followed by another string chunk.",
                        Lexer.Offset,
                        path
                    );
                    return null;
                }
            }
            else
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_CHUNK_DELIMITER_EXPECTED",
                    "A string chunk must be followed by a comma or closing parenthesis.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
        }
        CborValue value = new CborValue();
        if (textChunks)
        {
            value.Kind = CborValueKind.TextString;
            value.TextStringValue = text.ToString();
        }
        else
        {
            value.Kind = CborValueKind.ByteString;
            value.ByteStringValue = bytes.ToArray();
        }
        value.LengthKind = CborLengthKind.Indefinite;
        CborRepresentation representation = new CborRepresentation();
        representation.LengthKind = CborLengthKind.Indefinite;
        representation.ChunkByteLengths = chunkLengths;
        value.Representation = representation;
        return value;
    }

    internal CborValue? ParseNumberOrTag(int depth, string path)
    {
        int start = Lexer.Offset;
        string atom = Lexer.ReadAtom();
        NumberPath = path;
        CborDiagnosticNumber number = ParseNumericAtom(atom, start);
        if (!number.Success)
        {
            return null;
        }
        CborValue numeric = CreateNumericValue(number);
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (!number.IsFloat
            && number.IntegerValue >= BigInteger.Zero
            && Lexer.Offset < Lexer.Text.Length
            && Lexer.Text[Lexer.Offset] == '(')
        {
            if (number.IntegerValue > new BigInteger(ulong.MaxValue))
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_TAG_OUT_OF_RANGE",
                    "The diagnostic tag number exceeds the unsigned 64-bit range.",
                    start,
                    path
                );
                return null;
            }
            Lexer.Offset = Lexer.Offset + 1;
            CborValue? content = ParseValue(
                depth + 1,
                $"{path}<tag:{number.IntegerValue}>"
            );
            if (content == null || !Lexer.SkipTrivia())
            {
                CopyLexerFailure(path);
                return null;
            }
            if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != ')')
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_TAG_UNTERMINATED",
                    "The diagnostic tag has no closing parenthesis.",
                    Lexer.Offset,
                    path
                );
                return null;
            }
            Lexer.Offset = Lexer.Offset + 1;
            CborValue tag = new CborValue();
            tag.Kind = CborValueKind.Tag;
            tag.TagNumber = (ulong)number.IntegerValue;
            tag.TaggedValue = content;
            return tag;
        }
        return numeric;
    }

    internal CborValue? ParseWord(int depth, string path)
    {
        int start = Lexer.Offset;
        string word = Lexer.ReadAtom();
        if (word == "false")
        {
            return CreateSimple(CborSimpleValues.False);
        }
        if (word == "true")
        {
            return CreateSimple(CborSimpleValues.True);
        }
        if (word == "null")
        {
            return CreateSimple(CborSimpleValues.Null);
        }
        if (word == "undefined")
        {
            return CreateSimple(CborSimpleValues.Undefined);
        }
        if (word == "Infinity")
        {
            return CreateFloat(double.PositiveInfinity);
        }
        if (word == "NaN")
        {
            return CreateFloat(double.NaN);
        }
        if (word == "simple")
        {
            return ParseSimpleValue(depth, path);
        }
        SetFailure(
            "CBOR_DIAGNOSTIC_TOKEN_INVALID",
            "The diagnostic input contains an unrecognized value token.",
            start,
            path
        );
        return null;
    }

    internal CborValue? ParseSimpleValue(int depth, string path)
    {
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != '(')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_SIMPLE_PARENTHESIS_EXPECTED",
                "The simple value number must be enclosed in parentheses.",
                Lexer.Offset,
                path
            );
            return null;
        }
        Lexer.Offset = Lexer.Offset + 1;
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        int start = Lexer.Offset;
        string atom = Lexer.ReadAtom();
        NumberPath = path;
        CborDiagnosticNumber number = ParseNumericAtom(atom, start);
        if (!number.Success || number.IsFloat)
        {
            if (ErrorCode == null)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_SIMPLE_VALUE_INVALID",
                    "A simple value requires an integer from zero through 255.",
                    start,
                    path
                );
            }
            return null;
        }
        if (number.IntegerValue < BigInteger.Zero
            || number.IntegerValue > new BigInteger(255)
            || number.IntegerValue >= new BigInteger(CborSimpleValues.FirstReserved)
                && number.IntegerValue <= new BigInteger(CborSimpleValues.LastReserved))
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_SIMPLE_VALUE_INVALID",
                "A simple value must be valid and between zero and 255.",
                start,
                path
            );
            return null;
        }
        if (!Lexer.SkipTrivia())
        {
            CopyLexerFailure(path);
            return null;
        }
        if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != ')')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_SIMPLE_UNTERMINATED",
                "The simple value has no closing parenthesis.",
                Lexer.Offset,
                path
            );
            return null;
        }
        Lexer.Offset = Lexer.Offset + 1;
        return CreateSimple((byte)number.IntegerValue);
    }

    internal CborDiagnosticNumber ParseNumericAtom(string atom, int start)
    {
        CborDiagnosticNumber result = new CborDiagnosticNumber();
        if (atom == "-Infinity")
        {
            result.Success = true;
            result.IsFloat = true;
            result.FloatValue = double.NegativeInfinity;
            return result;
        }
        if (atom.Length == 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                "A diagnostic number was expected.",
                start,
                NumberPath
            );
            return result;
        }
        int signIndex = atom[0] == '-' || atom[0] == '+' ? 1 : 0;
        bool extended = Options != null && Options.UseExtendedNotation;
        if (signIndex == 1 && atom[0] == '+')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                "A diagnostic number cannot have a leading plus sign.",
                start,
                NumberPath
            );
            return result;
        }
        bool hasHexPrefix = atom.Length > signIndex + 2
            && atom[signIndex] == '0'
            && (atom[signIndex + 1] == 'x' || atom[signIndex + 1] == 'X');
        bool hasOctalPrefix = atom.Length > signIndex + 2
            && atom[signIndex] == '0'
            && (atom[signIndex + 1] == 'o' || atom[signIndex + 1] == 'O');
        bool hasBinaryPrefix = atom.Length > signIndex + 2
            && atom[signIndex] == '0'
            && (atom[signIndex + 1] == 'b' || atom[signIndex + 1] == 'B');
        if (hasHexPrefix || hasOctalPrefix || hasBinaryPrefix)
        {
            if (!extended)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED",
                    "Non-decimal numbers require extended diagnostic notation.",
                    start,
                    NumberPath
                );
                return result;
            }
            if (hasHexPrefix
                && (atom.IndexOf('.') >= 0
                    || atom.IndexOf('p') >= 0
                    || atom.IndexOf('P') >= 0))
            {
                return ParseHexFloat(atom, start);
            }
            int numberBase = hasHexPrefix ? 16 : hasOctalPrefix ? 8 : 2;
            return ParseBaseInteger(atom, signIndex + 2, numberBase, start);
        }
        return ParseDecimalNumber(atom, start);
    }

    internal CborDiagnosticNumber ParseDecimalNumber(string atom, int start)
    {
        CborDiagnosticNumber result = new CborDiagnosticNumber();
        int index = atom[0] == '-' || atom[0] == '+' ? 1 : 0;
        int integerStart = index;
        while (index < atom.Length && IsDecimalDigit(atom[index]))
        {
            index = index + 1;
        }
        if (index == integerStart
            || index - integerStart > 1 && atom[integerStart] == '0')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                "The decimal number has an invalid integer part.",
                start,
                NumberPath
            );
            return result;
        }
        bool floating = false;
        if (index < atom.Length && atom[index] == '.')
        {
            floating = true;
            index = index + 1;
            int fractionStart = index;
            while (index < atom.Length && IsDecimalDigit(atom[index]))
            {
                index = index + 1;
            }
            if (index == fractionStart)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                    "A decimal point must be followed by one or more digits.",
                    start,
                    NumberPath
                );
                return result;
            }
        }
        if (index < atom.Length && (atom[index] == 'e' || atom[index] == 'E'))
        {
            floating = true;
            index = index + 1;
            if (index < atom.Length && (atom[index] == '+' || atom[index] == '-'))
            {
                index = index + 1;
            }
            int exponentStart = index;
            while (index < atom.Length && IsDecimalDigit(atom[index]))
            {
                index = index + 1;
            }
            if (index == exponentStart)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                    "A decimal exponent requires one or more digits.",
                    start,
                    NumberPath
                );
                return result;
            }
        }
        if (index != atom.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                "The decimal number contains an invalid character.",
                start + index,
                NumberPath
            );
            return result;
        }
        result.IsFloat = floating;
        if (floating)
        {
            double number = double.Parse(
                atom,
                NumberStyles.Float,
                CultureInfo.InvariantCulture
            );
            if (double.IsInfinity(number))
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_NUMBER_OUT_OF_RANGE",
                    "The decimal floating-point number exceeds binary64 range.",
                    start,
                    NumberPath
                );
                return result;
            }
            result.FloatValue = number;
        }
        else
        {
            bool negative = atom[0] == '-';
            int digitIndex = atom[0] == '-' || atom[0] == '+' ? 1 : 0;
            BigInteger integer = BigInteger.Zero;
            while (digitIndex < atom.Length)
            {
                integer = integer * 10 + (atom[digitIndex] - '0');
                digitIndex = digitIndex + 1;
            }
            result.IntegerValue = negative ? -integer : integer;
        }
        result.Success = true;
        return result;
    }

    internal CborDiagnosticNumber ParseBaseInteger(
        string atom,
        int digitStart,
        int numberBase,
        int start)
    {
        CborDiagnosticNumber result = new CborDiagnosticNumber();
        BigInteger integer = BigInteger.Zero;
        int index = digitStart;
        while (index < atom.Length)
        {
            int digit = DigitValue(atom[index]);
            if (digit < 0 || digit >= numberBase)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_NUMBER_INVALID",
                    "The based integer contains a digit outside its base.",
                    start + index,
                    NumberPath
                );
                return result;
            }
            integer = integer * numberBase + digit;
            index = index + 1;
        }
        if (atom[0] == '-')
        {
            integer = -integer;
        }
        result.Success = true;
        result.IntegerValue = integer;
        return result;
    }

    internal CborDiagnosticNumber ParseHexFloat(string atom, int start)
    {
        CborDiagnosticNumber result = new CborDiagnosticNumber();
        int index = atom[0] == '-' || atom[0] == '+' ? 3 : 2;
        bool negative = atom[0] == '-';
        double mantissa = 0.0;
        int fractionDigits = 0;
        int retainedDigits = 0;
        int skippedDigits = 0;
        bool afterPoint = false;
        bool hasDigit = false;
        int integerDigits = 0;
        int pointFractionDigits = 0;
        bool nonzero = false;
        while (index < atom.Length && atom[index] != 'p' && atom[index] != 'P')
        {
            if (atom[index] == '.')
            {
                if (afterPoint)
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID",
                        "A hexadecimal float can contain only one radix point.",
                        start + index,
                        NumberPath
                    );
                    return result;
                }
                afterPoint = true;
                index = index + 1;
                continue;
            }
            int digit = DigitValue(atom[index]);
            if (digit < 0 || digit >= 16)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID",
                    "The hexadecimal float contains an invalid significand digit.",
                    start + index,
                    NumberPath
                );
                return result;
            }
            hasDigit = true;
            if (afterPoint)
            {
                fractionDigits = fractionDigits + 1;
                pointFractionDigits = pointFractionDigits + 1;
            }
            else
            {
                integerDigits = integerDigits + 1;
            }
            if (digit != 0)
            {
                nonzero = true;
            }
            if (nonzero && retainedDigits < 16)
            {
                mantissa = mantissa * 16.0 + digit;
                retainedDigits = retainedDigits + 1;
            }
            else if (nonzero)
            {
                skippedDigits = skippedDigits + 1;
            }
            index = index + 1;
        }
        if (!hasDigit
            || integerDigits == 0
            || afterPoint && pointFractionDigits == 0
            || index >= atom.Length
            || (atom[index] != 'p' && atom[index] != 'P'))
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID",
                "A hexadecimal float requires a significand and binary exponent.",
                start,
                NumberPath
            );
            return result;
        }
        index = index + 1;
        bool exponentNegative = false;
        if (index < atom.Length && (atom[index] == '+' || atom[index] == '-'))
        {
            exponentNegative = atom[index] == '-';
            index = index + 1;
        }
        int exponentStart = index;
        int exponent = 0;
        while (index < atom.Length && IsDecimalDigit(atom[index]))
        {
            if (exponent < 100000)
            {
                exponent = exponent * 10 + atom[index] - '0';
            }
            index = index + 1;
        }
        if (index == exponentStart || index != atom.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID",
                "A hexadecimal float requires a decimal binary exponent.",
                start + exponentStart,
                NumberPath
            );
            return result;
        }
        if (exponentNegative)
        {
            exponent = -exponent;
        }
        int effectiveExponent = exponent
            - fractionDigits * 4
            + skippedDigits * 4;
        double number = mantissa * System.Math.Pow(2.0, effectiveExponent);
        if (double.IsInfinity(number))
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_NUMBER_OUT_OF_RANGE",
                "The hexadecimal floating-point number exceeds binary64 range.",
                start,
                NumberPath
            );
            return result;
        }
        if (negative)
        {
            number = -number;
        }
        result.Success = true;
        result.IsFloat = true;
        result.FloatValue = number;
        return result;
    }

    internal CborValue CreateNumericValue(CborDiagnosticNumber number)
    {
        if (number.IsFloat)
        {
            return CreateFloat(number.FloatValue);
        }
        CborValue integer = new CborValue();
        integer.Kind = number.IntegerValue < BigInteger.Zero
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        integer.IntegerValue = number.IntegerValue;
        return integer;
    }

    internal CborValue CreateFloat(double number)
    {
        CborFloatValue floating = new CborFloatValue();
        floating.Value = number;
        floating.Width = CborFloatWidth.Double;
        floating.RawBits = System.BitConverter.DoubleToUInt64Bits(number);
        floating.IsNaN = double.IsNaN(number);
        CborValue value = new CborValue();
        value.Kind = CborValueKind.Float;
        value.FloatWidth = CborFloatWidth.Double;
        value.FloatValue = floating;
        return value;
    }

    internal CborValue CreateSimple(byte number)
    {
        CborValue value = new CborValue();
        value.Kind = CborValueKind.SimpleValue;
        value.SimpleValue = number;
        return value;
    }

    internal int ReadEncodingIndicator()
    {
        IndicatorOffset = -1;
        if (Lexer.Offset >= Lexer.Text.Length || Lexer.Text[Lexer.Offset] != '_')
        {
            return -1;
        }
        IndicatorOffset = Lexer.Offset;
        Lexer.Offset = Lexer.Offset + 1;
        int contentStart = Lexer.Offset;
        while (Lexer.Offset < Lexer.Text.Length)
        {
            char current = Lexer.Text[Lexer.Offset];
            bool indicator = current >= 'a' && current <= 'z'
                || current >= 'A' && current <= 'Z'
                || current >= '0' && current <= '9'
                || current == '_';
            if (!indicator)
            {
                break;
            }
            Lexer.Offset = Lexer.Offset + 1;
        }
        int length = Lexer.Offset - contentStart;
        if (length == 0)
        {
            return 7;
        }
        if (length == 1
            && Lexer.Text[contentStart] >= '0'
            && Lexer.Text[contentStart] <= '7')
        {
            return Lexer.Text[contentStart] - '0';
        }
        return -1;
    }

    internal bool SkipEncodingIndicators(string path)
    {
        ReadEncodingIndicator();
        return ErrorCode == null;
    }

    internal bool ApplyTrailingIndicator(CborValue value, string path)
    {
        int indicator = ReadEncodingIndicator();
        if (indicator < 0)
        {
            return true;
        }
        if (value.Kind == CborValueKind.Array || value.Kind == CborValueKind.Map)
        {
            return ApplyContainerIndicator(value, indicator, path);
        }
        if (value.Kind == CborValueKind.Float)
        {
            return ApplyFloatIndicator(value, indicator, path);
        }
        if (indicator >= 4 && indicator <= 6)
        {
            return FailIndicator(
                "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID",
                "The encoding indicator selects a reserved additional-information value.",
                path
            );
        }
        if (indicator == 7)
        {
            bool emptyBytes = value.Kind == CborValueKind.ByteString
                && value.ByteStringValue != null
                && value.ByteStringValue.Length == 0;
            bool emptyText = value.Kind == CborValueKind.TextString
                && value.TextStringValue != null
                && value.TextStringValue.Length == 0;
            if (!emptyBytes && !emptyText)
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID",
                    "An indefinite indicator is valid here only for an empty string.",
                    path
                );
            }
            value.LengthKind = CborLengthKind.Indefinite;
            CborRepresentation emptyRepresentation = GetRepresentation(value);
            emptyRepresentation.LengthKind = CborLengthKind.Indefinite;
            emptyRepresentation.ChunkByteLengths = new List<int>();
            return true;
        }
        int width = 1 << indicator;
        if (value.Kind == CborValueKind.SimpleValue)
        {
            if (indicator != 0 || value.SimpleValue < 32)
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH",
                    "The simple-value encoding indicator is inconsistent with the value.",
                    path
                );
            }
        }
        else
        {
            BigInteger argument;
            if (value.Kind == CborValueKind.UnsignedInteger)
            {
                argument = value.IntegerValue;
            }
            else if (value.Kind == CborValueKind.NegativeInteger)
            {
                argument = -value.IntegerValue - BigInteger.One;
            }
            else if (value.Kind == CborValueKind.Tag)
            {
                argument = new BigInteger(value.TagNumber);
            }
            else if (value.Kind == CborValueKind.ByteString)
            {
                argument = new BigInteger(
                    value.ByteStringValue == null ? 0 : value.ByteStringValue.Length
                );
                value.LengthKind = CborLengthKind.Definite;
            }
            else if (value.Kind == CborValueKind.TextString)
            {
                argument = new BigInteger(
                    Encoding.UTF8.GetByteCount(value.TextStringValue ?? "")
                );
                value.LengthKind = CborLengthKind.Definite;
            }
            else
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID",
                    "The encoding indicator is not valid for this value kind.",
                    path
                );
            }
            if (!FitsHeadWidth(argument, width))
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH",
                    "The value does not fit the indicated CBOR head width.",
                    path
                );
            }
        }
        CborRepresentation representation = GetRepresentation(value);
        representation.HeadArgumentWidth = width;
        if (value.Kind == CborValueKind.ByteString
            || value.Kind == CborValueKind.TextString)
        {
            representation.LengthKind = CborLengthKind.Definite;
        }
        return true;
    }

    internal bool ApplyContainerIndicator(
        CborValue value,
        int indicator,
        string path)
    {
        if (indicator < 0)
        {
            return true;
        }
        if (indicator >= 4 && indicator <= 6)
        {
            return FailIndicator(
                "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID",
                "The container indicator selects a reserved additional-information value.",
                path
            );
        }
        CborRepresentation representation = GetRepresentation(value);
        if (indicator == 7)
        {
            value.LengthKind = CborLengthKind.Indefinite;
            representation.LengthKind = CborLengthKind.Indefinite;
            return true;
        }
        int count = value.Kind == CborValueKind.Array
            ? value.ArrayItems == null ? 0 : value.ArrayItems.Count
            : value.MapEntries == null ? 0 : value.MapEntries.Count;
        int width = 1 << indicator;
        if (!FitsHeadWidth(new BigInteger(count), width))
        {
            return FailIndicator(
                "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH",
                "The container length does not fit the indicated CBOR head width.",
                path
            );
        }
        value.LengthKind = CborLengthKind.Definite;
        representation.LengthKind = CborLengthKind.Definite;
        representation.HeadArgumentWidth = width;
        return true;
    }

    internal bool ApplyFloatIndicator(CborValue value, int indicator, string path)
    {
        if (indicator < 1 || indicator > 3 || value.FloatValue == null)
        {
            return FailIndicator(
                "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID",
                "A floating-point indicator must select half, single, or double precision.",
                path
            );
        }
        double number = value.FloatValue.Value;
        CborFloatWidth width = indicator == 1
            ? CborFloatWidth.Half
            : indicator == 2
                ? CborFloatWidth.Single
                : CborFloatWidth.Double;
        CborFloatValue retained;
        if (double.IsNaN(number))
        {
            retained = width == CborFloatWidth.Half
                ? CborHalfPrecision.CreateHalfFloat(0x7e00)
                : width == CborFloatWidth.Single
                    ? CborHalfPrecision.CreateSingleFloat(0x7fc00000U)
                    : CborHalfPrecision.CreateDoubleFloat(0x7ff8000000000000UL);
        }
        else if (width == CborFloatWidth.Half)
        {
            if (!CborHalfPrecision.IsExactlyRepresentableAsHalf(number))
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH",
                    "The floating-point value is not exactly representable as binary16.",
                    path
                );
            }
            retained = CborHalfPrecision.CreateHalfFloat(
                CborHalfPrecision.ConvertDoubleToHalfBits(number)
            );
        }
        else if (width == CborFloatWidth.Single)
        {
            if (!CborHalfPrecision.IsExactlyRepresentableAsSingle(number))
            {
                return FailIndicator(
                    "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH",
                    "The floating-point value is not exactly representable as binary32.",
                    path
                );
            }
            retained = CborHalfPrecision.CreateSingleFloat(
                CborHalfPrecision.ConvertDoubleToSingleBits(number)
            );
        }
        else
        {
            retained = CborHalfPrecision.CreateDoubleFloat(
                CborHalfPrecision.ConvertDoubleToDoubleBits(number)
            );
        }
        value.FloatWidth = width;
        value.FloatValue = retained;
        CborRepresentation representation = GetRepresentation(value);
        representation.HeadArgumentWidth = width == CborFloatWidth.Half
            ? 2
            : width == CborFloatWidth.Single ? 4 : 8;
        representation.FloatWidth = width;
        representation.FloatBits = retained.RawBits;
        representation.HasFloatBits = true;
        return true;
    }

    internal CborRepresentation GetRepresentation(CborValue value)
    {
        if (value.Representation == null)
        {
            value.Representation = new CborRepresentation();
        }
        return value.Representation;
    }

    internal bool FitsHeadWidth(BigInteger argument, int width)
    {
        if (argument < BigInteger.Zero)
        {
            return false;
        }
        if (width == 1)
        {
            return argument <= new BigInteger(byte.MaxValue);
        }
        if (width == 2)
        {
            return argument <= new BigInteger(ushort.MaxValue);
        }
        if (width == 4)
        {
            return argument <= new BigInteger(uint.MaxValue);
        }
        return argument <= new BigInteger(ulong.MaxValue);
    }

    internal bool FailIndicator(string code, string message, string path)
    {
        SetFailure(code, message, IndicatorOffset, path);
        return false;
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal int DigitValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal void CopyLexerFailure(string path)
    {
        if (ErrorCode == null && Lexer.ErrorCode != null)
        {
            SetFailure(
                Lexer.ErrorCode,
                Lexer.ErrorMessage ?? "The diagnostic token is invalid.",
                Lexer.ErrorOffset,
                path
            );
        }
    }

    internal void CopyEmbeddedFailure()
    {
        if (ErrorCode == null && Embedded.ErrorCode != null)
        {
            SetFailure(
                Embedded.ErrorCode,
                Embedded.ErrorMessage
                    ?? "The embedded diagnostic value exceeds a configured limit.",
                Embedded.ErrorOffset,
                Embedded.ErrorPath ?? "$"
            );
        }
    }

    internal void SetFailure(
        string code,
        string message,
        int offset,
        string path)
    {
        if (ErrorCode == null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            ErrorOffset = offset;
            ErrorPath = path;
        }
    }

    internal CborReadResult CreateResult()
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = 0;
        result.Remaining = Source == null ? 0 : Source.Length;
        result.SourceOffset = Source == null ? -1 : 0;
        result.ErrorCode = ErrorCode ?? "CBOR_DIAGNOSTIC_PARSE_FAILED";
        result.ErrorMessage = ErrorMessage ?? "The diagnostic notation could not be parsed.";
        result.ErrorOffset = ErrorOffset;
        result.ErrorPath = ErrorPath ?? "$";
        return result;
    }
}
