using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Writes repeatable compact JSON and recursively orders free-form JSON objects.</summary>
/// <remarks>The deterministic glTF policy normalizes negative floating-point zero to the JSON number <c>0</c>.</remarks>
internal class DeterministicJsonWriter
{
    internal bool SortObjects;
    internal bool Indented;
    internal int Depth;

    internal JsonNode? OrderValue(JsonNode? source)
    {
        if (source == null)
        {
            return null;
        }

        JsonValueKind kind = source.GetValueKind();
        if (kind == JsonValueKind.Object)
        {
            JsonObject sourceObject = source.AsObject();
            JsonObject resultObject = new JsonObject();
            List<string> names = GetObjectNames(sourceObject);
            names.Sort(StringComparer.Ordinal);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                resultObject[name] = OrderChildValue(sourceObject[name]);
                index++;
            }

            return resultObject;
        }

        if (kind == JsonValueKind.Array)
        {
            JsonArray sourceArray = source.AsArray();
            JsonArray resultArray = new JsonArray();
            int index = 0;
            while (index < sourceArray.Count)
            {
                resultArray.Add(OrderChildValue(sourceArray[index]));
                index++;
            }

            return resultArray;
        }

        return JsonValueCloner.CloneNode(source);
    }

    internal JsonNode? OrderChildValue(JsonNode? source)
    {
        return OrderValue(source);
    }

    internal string WriteCanonicalValue(JsonNode? source)
    {
        SortObjects = true;
        Indented = false;
        Depth = 0;
        StringBuilder result = new StringBuilder();
        AppendValue(result, source);
        return result.ToString();
    }

    internal string WriteOrderedValue(JsonNode? source)
    {
        SortObjects = false;
        Indented = false;
        Depth = 0;
        StringBuilder result = new StringBuilder();
        AppendValue(result, source);
        return result.ToString();
    }

    internal string WriteFormattedValue(JsonNode? source, bool indented, bool sortObjects)
    {
        SortObjects = sortObjects;
        Indented = indented;
        Depth = 0;
        StringBuilder result = new StringBuilder();
        AppendValue(result, source);
        return result.ToString();
    }

    internal void AppendValue(StringBuilder result, JsonNode? source)
    {
        if (source == null)
        {
            result.Append("null");
            return;
        }

        JsonValueKind kind = source.GetValueKind();
        if (kind == JsonValueKind.Object)
        {
            AppendObject(result, source.AsObject());
            return;
        }

        if (kind == JsonValueKind.Array)
        {
            AppendArray(result, source.AsArray());
            return;
        }

        if (kind == JsonValueKind.String)
        {
            AppendString(result, source.GetValue<string>());
            return;
        }

        if (kind == JsonValueKind.Number)
        {
            result.Append(FormatNumber(source));
            return;
        }

        if (kind == JsonValueKind.True)
        {
            result.Append("true");
            return;
        }

        if (kind == JsonValueKind.False)
        {
            result.Append("false");
            return;
        }

        if (kind == JsonValueKind.Null)
        {
            result.Append("null");
            return;
        }

        throw new GltfWriteException($"Unsupported JSON value kind {(int)kind}.");
    }

    internal void AppendObject(StringBuilder result, JsonObject source)
    {
        List<string> names = GetObjectNames(source);
        if (SortObjects)
        {
            names.Sort(StringComparer.Ordinal);
        }

        result.Append('{');
        if (names.Count > 0 && Indented)
        {
            Depth++;
        }

        int index = 0;
        while (index < names.Count)
        {
            if (index > 0)
            {
                result.Append(',');
            }

            if (Indented)
            {
                AppendLineIndent(result);
            }

            string name = names[index];
            AppendString(result, name);
            if (Indented)
            {
                result.Append(": ");
            }
            else
            {
                result.Append(':');
            }

            AppendValue(result, source[name]);
            index++;
        }

        if (names.Count > 0 && Indented)
        {
            Depth--;
            AppendLineIndent(result);
        }

        result.Append('}');
    }

    internal void AppendArray(StringBuilder result, JsonArray source)
    {
        result.Append('[');
        if (source.Count > 0 && Indented)
        {
            Depth++;
        }

        int index = 0;
        while (index < source.Count)
        {
            if (index > 0)
            {
                result.Append(',');
            }

            if (Indented)
            {
                AppendLineIndent(result);
            }

            AppendValue(result, source[index]);
            index++;
        }

        if (source.Count > 0 && Indented)
        {
            Depth--;
            AppendLineIndent(result);
        }

        result.Append(']');
    }

    internal void AppendLineIndent(StringBuilder result)
    {
        result.Append('\n');
        int spaceCount = Depth * 2;
        int index = 0;
        while (index < spaceCount)
        {
            result.Append(' ');
            index++;
        }
    }

    internal List<string> GetObjectNames(JsonObject source)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            result.Add(member.Key);
        }

        return result;
    }

    internal void AppendString(StringBuilder result, string source)
    {
        result.Append('"');
        int index = 0;
        while (index < source.Length)
        {
            char value = source[index];
            if (value == '"')
            {
                result.Append("\\\"");
            }
            else if (value == '\\')
            {
                result.Append("\\\\");
            }
            else if (value == '\b')
            {
                result.Append("\\b");
            }
            else if (value == '\t')
            {
                result.Append("\\t");
            }
            else if (value == '\n')
            {
                result.Append("\\n");
            }
            else if (value == '\f')
            {
                result.Append("\\f");
            }
            else if (value == '\r')
            {
                result.Append("\\r");
            }
            else if (value < 0x20)
            {
                AppendControlEscape(result, value);
            }
            else if (char.IsHighSurrogate(value))
            {
                if (index + 1 >= source.Length || !char.IsLowSurrogate(source[index + 1]))
                {
                    throw new GltfWriteException("JSON strings cannot contain lone UTF-16 surrogates.");
                }

                result.Append(value);
                index++;
                result.Append(source[index]);
            }
            else if (char.IsLowSurrogate(value))
            {
                throw new GltfWriteException("JSON strings cannot contain lone UTF-16 surrogates.");
            }
            else
            {
                result.Append(value);
            }

            index++;
        }

        result.Append('"');
    }

    internal void AppendControlEscape(StringBuilder result, char value)
    {
        const string hexadecimal = "0123456789abcdef";
        int numericValue = value;
        result.Append("\\u00");
        result.Append(hexadecimal[(numericValue >> 4) & 15]);
        result.Append(hexadecimal[numericValue & 15]);
    }

    internal string FormatNumber(JsonNode source)
    {
        object boxed = source.GetValue<object>();
        if (boxed.GetType() == 0.0.GetType())
        {
            double boxedDouble = (double)boxed;
            if (!FiniteNumber.IsFiniteDouble(boxedDouble))
            {
                throw new GltfWriteException("JSON numbers must be finite.");
            }
        }

        if (boxed.GetType() == 0.0f.GetType())
        {
            float boxedSingle = (float)boxed;
            if (!FiniteNumber.IsFiniteSingle(boxedSingle))
            {
                throw new GltfWriteException("JSON numbers must be finite.");
            }
        }

        string serialized = source.ToJsonString();
        double value = double.Parse(serialized, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (!FiniteNumber.IsFiniteDouble(value))
        {
            throw new GltfWriteException("JSON numbers must be finite IEEE 754 double-precision values.");
        }

        return FormatFiniteDouble(value);
    }

    internal string FormatFiniteDouble(double value)
    {
        if (value == 0.0)
        {
            return "0";
        }

        string roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
        bool negative = roundTrip[0] == '-';
        int startIndex = 0;
        if (negative)
        {
            startIndex = 1;
        }

        int exponentIndex = roundTrip.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = roundTrip.IndexOf('e');
        }

        int mantissaEnd = roundTrip.Length;
        int explicitExponent = 0;
        if (exponentIndex >= 0)
        {
            mantissaEnd = exponentIndex;
            explicitExponent = int.Parse(
                roundTrip.Substring(exponentIndex + 1),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture);
        }

        int decimalIndex = roundTrip.IndexOf('.');
        int digitsBeforeDecimal = mantissaEnd - startIndex;
        if (decimalIndex >= 0 && decimalIndex < mantissaEnd)
        {
            digitsBeforeDecimal = decimalIndex - startIndex;
        }

        StringBuilder allDigits = new StringBuilder();
        int index = startIndex;
        while (index < mantissaEnd)
        {
            if (roundTrip[index] != '.')
            {
                allDigits.Append(roundTrip[index]);
            }

            index++;
        }

        int firstDigit = 0;
        while (firstDigit < allDigits.Length && allDigits[firstDigit] == '0')
        {
            firstDigit++;
        }

        int lastDigit = allDigits.Length - 1;
        while (lastDigit > firstDigit && allDigits[lastDigit] == '0')
        {
            lastDigit--;
        }

        string significantDigits = allDigits.ToString(firstDigit, lastDigit - firstDigit + 1);
        int normalizedExponent = explicitExponent + digitsBeforeDecimal - firstDigit - 1;
        return RenderNumber(negative, significantDigits, normalizedExponent);
    }

    internal string RenderNumber(bool negative, string digits, int exponent)
    {
        StringBuilder result = new StringBuilder();
        if (negative)
        {
            result.Append('-');
        }

        if (exponent >= 21 || exponent <= -7)
        {
            result.Append(digits[0]);
            if (digits.Length > 1)
            {
                result.Append('.');
                result.Append(digits.Substring(1));
            }

            result.Append('e');
            if (exponent >= 0)
            {
                result.Append('+');
            }

            result.Append(exponent.ToString(CultureInfo.InvariantCulture));
            return result.ToString();
        }

        int decimalPosition = exponent + 1;
        if (decimalPosition <= 0)
        {
            result.Append("0.");
            int zeroCount = -decimalPosition;
            int index = 0;
            while (index < zeroCount)
            {
                result.Append('0');
                index++;
            }

            result.Append(digits);
            return result.ToString();
        }

        if (decimalPosition >= digits.Length)
        {
            result.Append(digits);
            int zeroCount = decimalPosition - digits.Length;
            int index = 0;
            while (index < zeroCount)
            {
                result.Append('0');
                index++;
            }

            return result.ToString();
        }

        result.Append(digits.Substring(0, decimalPosition));
        result.Append('.');
        result.Append(digits.Substring(decimalPosition));
        return result.ToString();
    }
}
