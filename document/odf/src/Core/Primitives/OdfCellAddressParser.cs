using System.Collections.Generic;

namespace odf;

internal static class OdfCellAddressParser
{
    internal static OdfQuotedSheetValue ParseQuotedSheetName(string? lexicalValue)
    {
        OdfQuotedSheetValue result = new OdfQuotedSheetValue();
        result.LexicalValue = lexicalValue ?? "";
        if (lexicalValue == null || lexicalValue.Length < 2)
        {
            return result;
        }

        int quote = 0;
        if (lexicalValue[0] == '$')
        {
            result.IsAbsolute = true;
            quote = 1;
        }
        if (quote >= lexicalValue.Length || lexicalValue[quote] != '\'' || lexicalValue[lexicalValue.Length - 1] != '\'')
        {
            return result;
        }

        global::System.Text.StringBuilder name = new global::System.Text.StringBuilder();
        int index = quote + 1;
        while (index < lexicalValue.Length - 1)
        {
            char character = lexicalValue[index];
            if (character == '\'')
            {
                if (index + 1 >= lexicalValue.Length - 1 || lexicalValue[index + 1] != '\'')
                {
                    return result;
                }
                name.Append('\'');
                index = index + 2;
            }
            else
            {
                name.Append(character);
                index = index + 1;
            }
        }
        result.SheetName = name.ToString();
        result.IsValid = true;
        return result;
    }

    internal static OdfCellAddressValue ParseCellAddress(string? lexicalValue)
    {
        OdfCellAddressValue invalid = NewAddressResult(lexicalValue);
        OdfCellRangeValue range = ParseRangeCore(lexicalValue, false);
        if (!range.IsValid || range.HasEnd || !range.Start.HasColumn || !range.Start.HasRow)
        {
            return invalid;
        }
        range.Start.LexicalValue = lexicalValue ?? "";
        return range.Start;
    }

    internal static OdfCellRangeValue ParseCellRange(string? lexicalValue)
    {
        return ParseRangeCore(lexicalValue, false);
    }

    internal static OdfCellRangeValue ParseFormulaReference(string? lexicalValue)
    {
        return ParseRangeCore(lexicalValue, true);
    }

    internal static OdfCellRangeValue ParseExternalReference(string? lexicalValue)
    {
        OdfCellRangeValue result = ParseRangeCore(lexicalValue, true);
        if (!result.IsValid || !result.Start.HasSource)
        {
            return NewRangeResult(lexicalValue);
        }
        return result;
    }

    internal static OdfCellRangeListValue ParseRangeList(string? lexicalValue)
    {
        OdfCellRangeListValue result = NewRangeListResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }

        List<OdfCellRangeValue> ranges = new List<OdfCellRangeValue>();
        int index = 0;
        while (index < lexicalValue.Length)
        {
            while (index < lexicalValue.Length && IsReferenceWhitespace(lexicalValue[index]))
            {
                index = index + 1;
            }
            if (index == lexicalValue.Length)
            {
                break;
            }

            int start = index;
            bool quoted = false;
            while (index < lexicalValue.Length)
            {
                char character = lexicalValue[index];
                if (character == '\'')
                {
                    if (quoted && index + 1 < lexicalValue.Length && lexicalValue[index + 1] == '\'')
                    {
                        index = index + 2;
                        continue;
                    }
                    quoted = !quoted;
                    index = index + 1;
                    continue;
                }
                if (!quoted && IsReferenceWhitespace(character))
                {
                    break;
                }
                index = index + 1;
            }
            if (quoted)
            {
                return result;
            }
            string item = lexicalValue.Substring(start, index - start);
            OdfCellRangeValue range = ParseCellRange(item);
            if (!range.IsValid)
            {
                return result;
            }
            ranges.Add(range);
        }
        if (ranges.Count == 0)
        {
            return result;
        }
        result.Ranges = ranges.ToArray();
        result.IsValid = true;
        return result;
    }

    static OdfCellRangeValue ParseRangeCore(string? lexicalValue, bool bracketed)
    {
        OdfCellRangeValue result = NewRangeResult(lexicalValue);
        result.IsBracketed = bracketed;
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }

        string content = lexicalValue;
        if (bracketed)
        {
            if (lexicalValue.Length < 3 || lexicalValue[0] != '[' || lexicalValue[lexicalValue.Length - 1] != ']')
            {
                return result;
            }
            content = lexicalValue.Substring(1, lexicalValue.Length - 2);
        }
        else if (lexicalValue.IndexOf('[') >= 0 || lexicalValue.IndexOf(']') >= 0)
        {
            return result;
        }

        string source = "";
        bool hasSource = false;
        if (bracketed && content.Length > 0 && content[0] == '\'')
        {
            int sourceEnd = ClosingQuote(content, 0);
            if (sourceEnd >= 0 && sourceEnd + 1 < content.Length && content[sourceEnd + 1] == '#')
            {
                OdfQuotedSheetValue sourceValue = ParseQuotedSheetName(content.Substring(0, sourceEnd + 1));
                if (!sourceValue.IsValid || !OdfIriResolver.IsIriReference(sourceValue.SheetName))
                {
                    return result;
                }
                source = sourceValue.SheetName;
                hasSource = true;
                content = content.Substring(sourceEnd + 2);
            }
        }

        OdfCellAddressValue start = ParseEndpoint(content);
        if (start.IsValid && start.HasColumn && start.HasRow)
        {
            start.Source = source;
            start.HasSource = hasSource;
            result.Start = start;
            result.IsValid = true;
            return result;
        }

        int separator = FindRangeSeparator(content);
        if (separator < 0)
        {
            return result;
        }
        string startText = content.Substring(0, separator);
        string endText = content.Substring(separator + 1);
        start = ParseEndpoint(startText);
        OdfCellAddressValue end = ParseEndpoint(endText);
        if (!start.IsValid || !end.IsValid || !MatchingEndpointKinds(start, end))
        {
            return result;
        }
        start.Source = source;
        start.HasSource = hasSource;
        result.Start = start;
        result.End = end;
        result.HasEnd = true;
        result.IsValid = true;
        return result;
    }

    static OdfCellAddressValue ParseEndpoint(string value)
    {
        OdfCellAddressValue result = NewAddressResult(value);
        int dot = FindSingleOutsideQuotes(value, '.');
        if (dot < 0 || dot == value.Length - 1)
        {
            return result;
        }

        string sheetText = value.Substring(0, dot);
        string coordinate = value.Substring(dot + 1);
        if (sheetText.Length > 0)
        {
            OdfQuotedSheetValue quoted = ParseQuotedSheetName(sheetText);
            if (quoted.IsValid)
            {
                if (quoted.SheetName.Length == 0)
                {
                    return result;
                }
                result.SheetName = quoted.SheetName;
                result.SheetAbsolute = quoted.IsAbsolute;
                result.HasSheet = true;
            }
            else
            {
                int sheetStart = 0;
                if (sheetText[0] == '$')
                {
                    result.SheetAbsolute = true;
                    sheetStart = 1;
                }
                if (sheetStart == sheetText.Length || !IsUnquotedSheetName(sheetText, sheetStart))
                {
                    return result;
                }
                result.SheetName = sheetText.Substring(sheetStart);
                result.HasSheet = true;
            }
        }

        int index = 0;
        bool firstAbsolute = false;
        if (coordinate[index] == '$')
        {
            firstAbsolute = true;
            index = index + 1;
        }
        if (index == coordinate.Length)
        {
            return result;
        }

        int columnStart = index;
        while (index < coordinate.Length && coordinate[index] >= 'A' && coordinate[index] <= 'Z')
        {
            index = index + 1;
        }
        if (index > columnStart)
        {
            result.Column = coordinate.Substring(columnStart, index - columnStart);
            result.ColumnIndex = ParseColumnIndex(result.Column);
            result.ColumnAbsolute = firstAbsolute;
            result.HasColumn = true;
            if (index < coordinate.Length && coordinate[index] == '$')
            {
                result.RowAbsolute = true;
                index = index + 1;
            }
        }
        else
        {
            result.RowAbsolute = firstAbsolute;
        }

        int rowStart = index;
        while (index < coordinate.Length && coordinate[index] >= '0' && coordinate[index] <= '9')
        {
            index = index + 1;
        }
        if (index > rowStart)
        {
            if (coordinate[rowStart] == '0')
            {
                return result;
            }
            result.Row = ParsePositiveInteger(coordinate, rowStart, index);
            result.HasRow = true;
        }
        if (index != coordinate.Length || (!result.HasColumn && !result.HasRow))
        {
            return result;
        }
        result.IsValid = true;
        return result;
    }

    static bool MatchingEndpointKinds(OdfCellAddressValue start, OdfCellAddressValue end)
    {
        return start.HasColumn == end.HasColumn && start.HasRow == end.HasRow;
    }

    static int FindRangeSeparator(string value)
    {
        bool quoted = false;
        int separator = -1;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\'')
            {
                if (quoted && index + 1 < value.Length && value[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && character == ':')
            {
                OdfCellAddressValue start = ParseEndpoint(value.Substring(0, index));
                OdfCellAddressValue end = ParseEndpoint(value.Substring(index + 1));
                if (start.IsValid && end.IsValid && MatchingEndpointKinds(start, end))
                {
                    if (separator >= 0)
                    {
                        return -1;
                    }
                    separator = index;
                }
            }
            index = index + 1;
        }
        return separator;
    }

    static bool IsUnquotedSheetName(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.' || character == ' ' || character == '\'' ||
                character == '#' || character == '$' || character == '[' || character == ']' ||
                IsReferenceWhitespace(character))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static int FindSingleOutsideQuotes(string value, char separator)
    {
        bool quoted = false;
        int found = -1;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\'')
            {
                if (quoted && index + 1 < value.Length && value[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && character == separator)
            {
                if (found >= 0)
                {
                    return -2;
                }
                found = index;
            }
            index = index + 1;
        }
        if (quoted)
        {
            return -2;
        }
        return found;
    }

    static int ClosingQuote(string value, int start)
    {
        int index = start + 1;
        while (index < value.Length)
        {
            if (value[index] == '\'')
            {
                if (index + 1 < value.Length && value[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static global::System.Numerics.BigInteger ParseColumnIndex(string column)
    {
        global::System.Numerics.BigInteger result = 0;
        int index = 0;
        while (index < column.Length)
        {
            result = result * 26 + column[index] - 'A' + 1;
            index = index + 1;
        }
        return result;
    }

    static global::System.Numerics.BigInteger ParsePositiveInteger(string value, int start, int end)
    {
        global::System.Numerics.BigInteger result = 0;
        int index = start;
        while (index < end)
        {
            result = result * 10 + value[index] - '0';
            index = index + 1;
        }
        return result;
    }

    static bool IsReferenceWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static OdfCellAddressValue NewAddressResult(string? lexicalValue)
    {
        OdfCellAddressValue result = new OdfCellAddressValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfCellRangeValue NewRangeResult(string? lexicalValue)
    {
        OdfCellRangeValue result = new OdfCellRangeValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfCellRangeListValue NewRangeListResult(string? lexicalValue)
    {
        OdfCellRangeListValue result = new OdfCellRangeListValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }
}
