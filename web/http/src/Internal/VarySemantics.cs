using System.Collections.Generic;
using System.Text;

namespace http;

internal static class VarySemantics
{
    internal static HttpHeaders Parse(HttpHeaders responseHeaders)
    {
        ValidateHeaders(responseHeaders, "invalid-vary-context");
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            responseHeaders,
            HttpHeaderNames.Vary);
        HttpHeaders result = new HttpHeaders();
        if (fields.Count == 0)
        {
            return result;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(
            responseHeaders,
            HttpHeaderNames.Vary);
        FieldValueParseResult parsed = FieldValueParser.ParseVary(
            combined,
            combined.Length,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateVaryError("The Vary field value is malformed.", "invalid-vary");
        }

        List<string> names = new List<string>();
        bool wildcard = false;
        int index = 0;
        while (index < parsed.Values.Count)
        {
            string name = Encoding.ASCII.GetString(parsed.Values[index]);
            if (name == "*")
            {
                wildcard = true;
            }
            else
            {
                AddUniqueName(names, ToLowercaseName(name));
            }

            index = index + 1;
        }

        if (wildcard && names.Count != 0)
        {
            throw CreateVaryError(
                "The Vary wildcard cannot be combined with field names.",
                "ambiguous-vary-wildcard");
        }

        if (wildcard)
        {
            HttpHeaderField wildcardField = new HttpHeaderField();
            wildcardField.Name = "*";
            result.Fields.Add(wildcardField);
            return result;
        }

        SortNames(names);
        index = 0;
        while (index < names.Count)
        {
            HttpHeaderField field = new HttpHeaderField();
            field.Name = names[index];
            result.Fields.Add(field);
            index = index + 1;
        }

        return result;
    }

    internal static string BuildKey(HttpRequest request, HttpHeaders varyFields)
    {
        if (request == null)
        {
            throw new HttpProtocolException(
                "The selecting request is missing.",
                "invalid-vary-context");
        }

        ValidateHeaders(request.Headers, "invalid-vary-context");
        ValidateHeaders(varyFields, "invalid-vary-context");
        List<string> names = ReadParsedNames(varyFields);
        if (ContainsWildcard(names))
        {
            return "V1;*";
        }

        SortNames(names);
        StringBuilder output = new StringBuilder();
        output.Append("V1;");
        output.Append(names.Count);
        output.Append(';');
        int index = 0;
        while (index < names.Count)
        {
            AppendNameAndValues(output, request.Headers, names[index]);
            index = index + 1;
        }

        return output.ToString();
    }

    internal static bool CompareKeys(string storedKey, string requestKey)
    {
        if (storedKey == null || requestKey == null)
        {
            throw new HttpProtocolException(
                "A normalized Vary key is missing.",
                "invalid-vary-key");
        }

        if (storedKey == "V1;*" || requestKey == "V1;*")
        {
            return false;
        }

        return storedKey == requestKey;
    }

    static List<string> ReadParsedNames(HttpHeaders varyFields)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < varyFields.Fields.Count)
        {
            HttpHeaderField field = varyFields.Fields[index];
            if (field == null || field.Name == null ||
                (field.Name != "*" && !HeaderStore.IsValidName(field.Name)))
            {
                throw CreateVaryError(
                    "A parsed Vary field name is malformed.",
                    "invalid-vary-name");
            }

            string name = field.Name == "*" ? "*" : ToLowercaseName(field.Name);
            AddUniqueName(names, name);
            index = index + 1;
        }

        return names;
    }

    static void AppendNameAndValues(
        StringBuilder output,
        HttpHeaders requestHeaders,
        string name)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        output.Append(nameBytes.Length);
        output.Append(':');
        AppendHex(output, nameBytes);
        output.Append('=');
        List<HttpHeaderField> matches = HeaderStore.FindMatchingFields(requestHeaders, name);
        if (matches.Count == 0)
        {
            output.Append("A;");
            return;
        }

        bool combine = matches.Count > 1 &&
            (HeaderStore.IsCommaCombinableName(name) ||
            Ascii.EqualsStringsIgnoreCase(name, "Cookie"));
        bool normalizeSyntax = HeaderStore.IsCommaCombinableName(name) ||
            Ascii.EqualsStringsIgnoreCase(name, "Cookie");
        if (combine)
        {
            byte[] combined = HeaderStore.CombineMatchingValues(requestHeaders, name);
            output.Append("P1;");
            AppendNormalizedValue(output, combined, true);
            return;
        }

        output.Append('P');
        output.Append(matches.Count);
        output.Append(';');
        int index = 0;
        while (index < matches.Count)
        {
            AppendNormalizedValue(output, matches[index].Value, normalizeSyntax);
            index = index + 1;
        }
    }

    static void AppendNormalizedValue(
        StringBuilder output,
        byte[] value,
        bool normalizeSyntax)
    {
        byte[] normalized = NormalizeValue(value, normalizeSyntax);
        output.Append(normalized.Length);
        output.Append(':');
        AppendHex(output, normalized);
        output.Append(';');
    }

    static byte[] NormalizeValue(byte[] value, bool normalizeSyntax)
    {
        if (!normalizeSyntax)
        {
            return TrimOuterWhitespace(value);
        }

        List<byte> output = new List<byte>();
        bool quoted = false;
        bool escaped = false;
        bool whitespace = false;
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (quoted)
            {
                output.Add(character);
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == 0x5c)
                {
                    escaped = true;
                }
                else if (character == 0x22)
                {
                    quoted = false;
                }
            }
            else if (character == 0x20 || character == 0x09)
            {
                whitespace = true;
            }
            else
            {
                bool separator = character == 0x2c || character == 0x3b;
                if (separator)
                {
                    whitespace = false;
                }
                else if (whitespace)
                {
                    if (output.Count != 0)
                    {
                        output.Add(0x20);
                    }

                    whitespace = false;
                }

                output.Add(character);
                if (character == 0x22)
                {
                    quoted = true;
                }

                if (separator)
                {
                    whitespace = true;
                }
            }

            index = index + 1;
        }

        return output.ToArray();
    }

    static byte[] TrimOuterWhitespace(byte[] value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == 0x20 || value[start] == 0x09))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == 0x20 || value[end - 1] == 0x09))
        {
            end = end - 1;
        }

        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    static void AppendHex(StringBuilder output, byte[] value)
    {
        const string Digits = "0123456789abcdef";
        int index = 0;
        while (index < value.Length)
        {
            output.Append(Digits[value[index] >> 4]);
            output.Append(Digits[value[index] & 0x0f]);
            index = index + 1;
        }
    }

    static bool ContainsWildcard(List<string> names)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index] == "*")
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AddUniqueName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(names[index], name))
            {
                return;
            }

            index = index + 1;
        }

        names.Add(name);
    }

    static void SortNames(List<string> names)
    {
        int index = 1;
        while (index < names.Count)
        {
            string value = names[index];
            int position = index;
            while (position > 0 && CompareNames(value, names[position - 1]) < 0)
            {
                names[position] = names[position - 1];
                position = position - 1;
            }

            names[position] = value;
            index = index + 1;
        }
    }

    static int CompareNames(string left, string right)
    {
        int common = left.Length;
        if (right.Length < common)
        {
            common = right.Length;
        }

        int index = 0;
        while (index < common)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    static string ToLowercaseName(string name)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(name);
        return Encoding.ASCII.GetString(Ascii.ToLowercaseBytes(bytes));
    }

    static void ValidateHeaders(HttpHeaders headers, string code)
    {
        if (headers == null || headers.Fields == null)
        {
            throw new HttpProtocolException("A field collection is missing.", code);
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw new HttpProtocolException("A field collection is malformed.", code);
            }

            index = index + 1;
        }
    }

    static HttpFieldException CreateVaryError(string message, string code)
    {
        return new HttpFieldException(message, code, HttpHeaderNames.Vary);
    }
}
