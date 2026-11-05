using System.Globalization;

namespace odf;

internal static class OdfCellAddressFormatter
{
    internal static string FormatQuotedSheetName(OdfQuotedSheetValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        string absolute = value.IsAbsolute ? "$" : "";
        return absolute + Quote(value.SheetName);
    }

    internal static string FormatCellAddress(OdfCellAddressValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid || !value.HasColumn || !value.HasRow)
        {
            return value.LexicalValue;
        }
        return FormatEndpoint(value);
    }

    internal static string FormatCellRange(OdfCellRangeValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        return FormatRangeCore(value, value.IsBracketed);
    }

    internal static string FormatFormulaReference(OdfCellRangeValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        return FormatRangeCore(value, true);
    }

    internal static string FormatRangeList(OdfCellRangeListValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        string[] ranges = new string[value.Ranges.Length];
        int index = 0;
        while (index < value.Ranges.Length)
        {
            ranges[index] = FormatRangeCore(value.Ranges[index], false);
            index = index + 1;
        }
        return string.Join(" ", ranges);
    }

    static string FormatRangeCore(OdfCellRangeValue value, bool bracketed)
    {
        string result = "";
        if (value.Start.HasSource)
        {
            result = Quote(value.Start.Source) + "#";
        }
        result = result + FormatEndpoint(value.Start);
        if (value.HasEnd)
        {
            result = result + ":" + FormatEndpoint(value.End);
        }
        if (bracketed)
        {
            result = "[" + result + "]";
        }
        return result;
    }

    static string FormatEndpoint(OdfCellAddressValue value)
    {
        string result = "";
        if (value.HasSheet)
        {
            if (value.SheetAbsolute)
            {
                result = result + "$";
            }
            result = result + FormatSheet(value.SheetName);
        }
        result = result + ".";
        if (value.HasColumn)
        {
            if (value.ColumnAbsolute)
            {
                result = result + "$";
            }
            result = result + ColumnText(value.ColumnIndex);
        }
        if (value.HasRow)
        {
            if (value.RowAbsolute)
            {
                result = result + "$";
            }
            result = result + value.Row.ToString(CultureInfo.InvariantCulture);
        }
        return result;
    }

    static string FormatSheet(string value)
    {
        if (CanUseUnquotedSheet(value))
        {
            return value;
        }
        return Quote(value);
    }

    static bool CanUseUnquotedSheet(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.' || character == ' ' || character == '\'' ||
                character == '#' || character == '$' || character == '[' || character == ']' ||
                character == '\t' || character == '\r' || character == '\n')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string Quote(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        result.Append('\'');
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\'')
            {
                result.Append("''");
            }
            else
            {
                result.Append(value[index]);
            }
            index = index + 1;
        }
        result.Append('\'');
        return result.ToString();
    }

    static string ColumnText(global::System.Numerics.BigInteger columnIndex)
    {
        if (columnIndex <= 0)
        {
            return "";
        }
        string result = "";
        global::System.Numerics.BigInteger value = columnIndex;
        while (value > 0)
        {
            global::System.Numerics.BigInteger zeroBased = value - 1;
            int digit = (int)(zeroBased % 26);
            result = (char)('A' + digit) + result;
            value = zeroBased / 26;
        }
        return result;
    }
}
