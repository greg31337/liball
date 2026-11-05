using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace cbor;

internal class CddlJsonControlReader
{
    internal CddlValidationState State;
    internal CddlNode Node;
    internal string Text;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? ErrorPath;

    internal CddlJsonControlReader(
        CddlValidationState state,
        CddlNode node,
        string text)
    {
        State = state;
        Node = node;
        Text = text;
    }

    internal CborValue? ParseJsonValue()
    {
        int byteCount = Encoding.UTF8.GetByteCount(Text);
        if (!State.TakeEmbeddedBytes(byteCount, Node))
        {
            return null;
        }
        CborJsonSyntaxValidator syntax = new CborJsonSyntaxValidator(
            Text,
            State.Options.MaxNestingDepth
        );
        if (!syntax.ValidateDocument())
        {
            if (syntax.ErrorCode == "CBOR_JSON_MAX_NESTING_DEPTH")
            {
                State.AddResourceLimitIssue("nesting depth", Node);
                return null;
            }
            ErrorCode = syntax.ErrorCode ?? "CDDL_JSON_SYNTAX_INVALID";
            ErrorMessage = syntax.ErrorMessage ?? "The JSON text is invalid.";
            ErrorOffset = syntax.ErrorOffset;
            ErrorPath = syntax.ErrorPath ?? "$";
            return null;
        }
        JsonDocumentOptions options = new JsonDocumentOptions();
        options.AllowTrailingCommas = false;
        options.CommentHandling = JsonCommentHandling.Disallow;
        options.MaxDepth = int.MaxValue;
        using (JsonDocument document = JsonDocument.Parse(Text, options))
        {
            return ConvertElement(document.RootElement, 0, "$" );
        }
    }

    internal CborValue? ConvertElement(
        JsonElement element,
        int depth,
        string path)
    {
        if (!State.TakeStep(Node))
        {
            return null;
        }
        JsonValueKind kind = element.ValueKind;
        if (kind == JsonValueKind.Object)
        {
            return ConvertObject(element, depth, path);
        }
        if (kind == JsonValueKind.Array)
        {
            return ConvertArray(element, depth, path);
        }
        if (kind == JsonValueKind.String)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = element.GetString() ?? "";
            text.LengthKind = CborLengthKind.Definite;
            return text;
        }
        if (kind == JsonValueKind.Number)
        {
            return ConvertNumber(element.GetRawText(), path);
        }
        if (kind == JsonValueKind.True)
        {
            return CreateSimple(CborSimpleValues.True);
        }
        if (kind == JsonValueKind.False)
        {
            return CreateSimple(CborSimpleValues.False);
        }
        if (kind == JsonValueKind.Null)
        {
            return CreateSimple(CborSimpleValues.Null);
        }
        SetError(
            "CDDL_JSON_VALUE_INVALID",
            "The JSON value kind is invalid.",
            -1,
            path
        );
        return null;
    }

    internal CborValue? ConvertArray(
        JsonElement element,
        int depth,
        string path)
    {
        if (!State.EnterDataNesting(Node))
        {
            return null;
        }
        CborValue array = new CborValue();
        array.Kind = CborValueKind.Array;
        array.LengthKind = CborLengthKind.Definite;
        array.ArrayItems = new List<CborValue>();
        int index = 0;
        foreach (JsonElement child in element.EnumerateArray())
        {
            CborValue? item = ConvertElement(
                child,
                depth + 1,
                path + "[" + index + "]"
            );
            if (item == null)
            {
                State.LeaveDataNesting();
                return null;
            }
            array.ArrayItems.Add(item);
            index = index + 1;
        }
        State.LeaveDataNesting();
        return array;
    }

    internal CborValue? ConvertObject(
        JsonElement element,
        int depth,
        string path)
    {
        if (!State.EnterDataNesting(Node))
        {
            return null;
        }
        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.LengthKind = CborLengthKind.Definite;
        map.MapEntries = new List<CborMapEntry>();
        int index = 0;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            CborValue? value = ConvertElement(
                property.Value,
                depth + 1,
                path + ".property[" + index + "]"
            );
            if (value == null)
            {
                State.LeaveDataNesting();
                return null;
            }
            CborValue key = new CborValue();
            key.Kind = CborValueKind.TextString;
            key.TextStringValue = property.Name;
            key.LengthKind = CborLengthKind.Definite;
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = value;
            map.MapEntries.Add(entry);
            index = index + 1;
        }
        State.LeaveDataNesting();
        return map;
    }

    internal CborValue? ConvertNumber(string raw, string path)
    {
        bool negative = raw.Length > 0 && raw[0] == '-';
        int exponentIndex = FindExponent(raw);
        int significandEnd = exponentIndex < 0
            ? raw.Length
            : exponentIndex;
        int decimalIndex = FindDecimalPoint(raw, significandEnd);
        int fractionDigits = decimalIndex < 0
            ? 0
            : significandEnd - decimalIndex - 1;
        StringBuilder coefficient = new StringBuilder();
        int index = negative ? 1 : 0;
        while (index < significandEnd)
        {
            if (raw[index] != '.')
            {
                coefficient.Append(raw[index]);
            }
            index = index + 1;
        }
        int exponent = ParseExponent(raw, exponentIndex, path);
        if (ErrorCode != null || State.Diagnostics.HasErrors)
        {
            return null;
        }
        string digits = coefficient.ToString();
        if (!State.TakePatternStates(digits.Length, Node))
        {
            return null;
        }
        if (AllZeroDigits(digits))
        {
            return CreateInteger(BigInteger.Zero);
        }
        int scale = exponent - fractionDigits;
        if (scale >= 0)
        {
            if (!State.TakePatternStates(scale, Node))
            {
                return null;
            }
            string integerText = digits + new string('0', scale);
            BigInteger integer = BigInteger.Parse(
                integerText,
                CultureInfo.InvariantCulture
            );
            return CreateInteger(negative ? -integer : integer);
        }
        int requiredZeros = -scale;
        if (!State.TakePatternStates(requiredZeros, Node))
        {
            return null;
        }
        if (CountTrailingZeros(digits) >= requiredZeros)
        {
            string integerText = digits.Substring(
                0,
                digits.Length - requiredZeros
            );
            BigInteger integer = integerText.Length == 0
                ? BigInteger.Zero
                : BigInteger.Parse(
                    integerText,
                    CultureInfo.InvariantCulture
                );
            return CreateInteger(negative ? -integer : integer);
        }
        double floating = double.Parse(
            raw,
            NumberStyles.Float,
            CultureInfo.InvariantCulture
        );
        if (double.IsNaN(floating)
            || double.IsInfinity(floating)
            || floating == 0.0 && !AllZeroDigits(digits))
        {
            SetError(
                "CDDL_JSON_NUMBER_OUT_OF_RANGE",
                "The JSON number is outside the supported binary64 range.",
                -1,
                path
            );
            return null;
        }
        CborFloatValue floatValue = new CborFloatValue();
        floatValue.Value = floating;
        floatValue.Width = CborFloatWidth.Double;
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Float;
        result.FloatWidth = CborFloatWidth.Double;
        result.FloatValue = floatValue;
        BigInteger exactCoefficient = BigInteger.Parse(
            digits,
            CultureInfo.InvariantCulture
        );
        if (negative)
        {
            exactCoefficient = -exactCoefficient;
        }
        State.AddJsonNumber(result, exactCoefficient, scale);
        return result;
    }

    internal int FindExponent(string raw)
    {
        int index = 0;
        while (index < raw.Length)
        {
            if (raw[index] == 'e' || raw[index] == 'E')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal int FindDecimalPoint(string raw, int end)
    {
        int index = 0;
        while (index < end)
        {
            if (raw[index] == '.')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal int ParseExponent(
        string raw,
        int exponentIndex,
        string path)
    {
        if (exponentIndex < 0)
        {
            return 0;
        }
        int index = exponentIndex + 1;
        bool negative = false;
        if (raw[index] == '+' || raw[index] == '-')
        {
            negative = raw[index] == '-';
            index = index + 1;
        }
        int exponent = 0;
        while (index < raw.Length)
        {
            int digit = raw[index] - '0';
            if (exponent > (1000000 - digit) / 10)
            {
                State.AddResourceLimitIssue("pattern states", Node);
                return 0;
            }
            exponent = exponent * 10 + digit;
            index = index + 1;
        }
        return negative ? -exponent : exponent;
    }

    internal bool AllZeroDigits(string digits)
    {
        int index = 0;
        while (index < digits.Length)
        {
            if (digits[index] != '0')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal int CountTrailingZeros(string digits)
    {
        int count = 0;
        int index = digits.Length - 1;
        while (index >= 0 && digits[index] == '0')
        {
            count = count + 1;
            index = index - 1;
        }
        return count;
    }

    internal CborValue CreateInteger(BigInteger integer)
    {
        CborValue result = new CborValue();
        result.Kind = integer.Sign < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        result.IntegerValue = integer;
        return result;
    }

    internal CborValue CreateSimple(byte simpleValue)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.SimpleValue;
        result.SimpleValue = simpleValue;
        return result;
    }

    internal void SetError(
        string code,
        string message,
        int offset,
        string path)
    {
        ErrorCode = code;
        ErrorMessage = message;
        ErrorOffset = offset;
        ErrorPath = path;
    }
}
