using System;
using System.Collections.Generic;

namespace http;

internal static class HeaderStore
{
    internal static bool IsValidName(string name)
    {
        if (name == null || name.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character > 0x7f || !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidValue(byte[] value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character != 0x09 && character < 0x20 || character == 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void AppendField(HttpHeaders headers, HttpHeaderField field)
    {
        headers.Fields.Add(field);
    }

    internal static void ReplaceFields(HttpHeaders headers, HttpHeaderField field)
    {
        int firstMatch = -1;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField current = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(current.Name, field.Name))
            {
                if (firstMatch < 0)
                {
                    firstMatch = index;
                    headers.Fields[index] = field;
                    index = index + 1;
                }
                else
                {
                    headers.Fields.RemoveAt(index);
                }
            }
            else
            {
                index = index + 1;
            }
        }

        if (firstMatch < 0)
        {
            headers.Fields.Add(field);
        }
    }

    internal static int RemoveMatchingFields(HttpHeaders headers, string name)
    {
        int removed = 0;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(headers.Fields[index].Name, name))
            {
                headers.Fields.RemoveAt(index);
                removed = removed + 1;
            }
            else
            {
                index = index + 1;
            }
        }

        return removed;
    }

    internal static List<HttpHeaderField> FindMatchingFields(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> matches = new List<HttpHeaderField>();
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, name))
            {
                matches.Add(field);
            }

            index = index + 1;
        }

        return matches;
    }

    internal static byte[] CombineMatchingValues(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> matches = FindMatchingFields(headers, name);
        if (matches.Count == 0)
        {
            return Array.Empty<byte>();
        }

        int index = 0;
        while (index < matches.Count)
        {
            if (!IsValidValue(matches[index].Value))
            {
                throw new HttpFieldException(
                    "The field value contains a prohibited control octet.",
                    "invalid-field-value",
                    matches[index].Name);
            }

            index = index + 1;
        }

        if (matches.Count == 1)
        {
            return CopyValue(matches[0].Value);
        }

        bool cookie = Ascii.EqualsStringsIgnoreCase(name, "Cookie");
        if (!cookie && !IsCommaCombinableName(name))
        {
            throw new HttpFieldException(
                "Multiple field lines cannot be combined for this field grammar.",
                "field-not-combinable",
                name);
        }

        int totalLength = 0;
        index = 0;
        while (index < matches.Count)
        {
            byte[] value = matches[index].Value;
            if (value.Length > int.MaxValue - totalLength)
            {
                throw new HttpFieldException(
                    "The combined field value is too large.",
                    "combined-field-too-large",
                    name);
            }

            totalLength = totalLength + value.Length;
            if (index > 0)
            {
                if (totalLength > int.MaxValue - 2)
                {
                    throw new HttpFieldException(
                        "The combined field value is too large.",
                        "combined-field-too-large",
                        name);
                }

                totalLength = totalLength + 2;
            }

            index = index + 1;
        }

        byte[] combined = new byte[totalLength];
        int outputIndex = 0;
        index = 0;
        while (index < matches.Count)
        {
            if (index > 0)
            {
                if (cookie)
                {
                    combined[outputIndex] = 0x3b;
                }
                else
                {
                    combined[outputIndex] = 0x2c;
                }

                combined[outputIndex + 1] = 0x20;
                outputIndex = outputIndex + 2;
            }

            byte[] value = matches[index].Value;
            int valueIndex = 0;
            while (valueIndex < value.Length)
            {
                combined[outputIndex] = value[valueIndex];
                outputIndex = outputIndex + 1;
                valueIndex = valueIndex + 1;
            }

            index = index + 1;
        }

        return combined;
    }

    internal static bool AreValidTrailers(HttpHeaders trailers)
    {
        int index = 0;
        while (index < trailers.Fields.Count)
        {
            HttpHeaderField field = trailers.Fields[index];
            if (!IsValidName(field.Name) || !IsValidValue(field.Value) || IsForbiddenTrailerName(field.Name))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte[] CopyValue(byte[] value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = value[index];
            index = index + 1;
        }

        return result;
    }

    internal static bool IsCommaCombinableName(string name)
    {
        return Ascii.EqualsStringsIgnoreCase(name, "A-IM") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Charset") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Encoding") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Language") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Patch") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Post") ||
            Ascii.EqualsStringsIgnoreCase(name, "Accept-Ranges") ||
            Ascii.EqualsStringsIgnoreCase(name, "Allow") ||
            Ascii.EqualsStringsIgnoreCase(name, "Cache-Control") ||
            Ascii.EqualsStringsIgnoreCase(name, "Cache-Status") ||
            Ascii.EqualsStringsIgnoreCase(name, "Connection") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Encoding") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Language") ||
            Ascii.EqualsStringsIgnoreCase(name, "Forwarded") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-Match") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-None-Match") ||
            Ascii.EqualsStringsIgnoreCase(name, "Link") ||
            Ascii.EqualsStringsIgnoreCase(name, "Pragma") ||
            Ascii.EqualsStringsIgnoreCase(name, "Proxy-Authenticate") ||
            Ascii.EqualsStringsIgnoreCase(name, "TE") ||
            Ascii.EqualsStringsIgnoreCase(name, "Trailer") ||
            Ascii.EqualsStringsIgnoreCase(name, "Transfer-Encoding") ||
            Ascii.EqualsStringsIgnoreCase(name, "Upgrade") ||
            Ascii.EqualsStringsIgnoreCase(name, "Via") ||
            Ascii.EqualsStringsIgnoreCase(name, "Vary") ||
            Ascii.EqualsStringsIgnoreCase(name, "Warning") ||
            Ascii.EqualsStringsIgnoreCase(name, "WWW-Authenticate");
    }

    internal static bool IsForbiddenTrailerName(string name)
    {
        return Ascii.EqualsStringsIgnoreCase(name, "Age") ||
            Ascii.EqualsStringsIgnoreCase(name, "Authorization") ||
            Ascii.EqualsStringsIgnoreCase(name, "Cache-Control") ||
            Ascii.EqualsStringsIgnoreCase(name, "Connection") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Encoding") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Length") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Range") ||
            Ascii.EqualsStringsIgnoreCase(name, "Content-Type") ||
            Ascii.EqualsStringsIgnoreCase(name, "Date") ||
            Ascii.EqualsStringsIgnoreCase(name, "Expect") ||
            Ascii.EqualsStringsIgnoreCase(name, "Expires") ||
            Ascii.EqualsStringsIgnoreCase(name, "Host") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-Match") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-Modified-Since") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-None-Match") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-Range") ||
            Ascii.EqualsStringsIgnoreCase(name, "If-Unmodified-Since") ||
            Ascii.EqualsStringsIgnoreCase(name, "Keep-Alive") ||
            Ascii.EqualsStringsIgnoreCase(name, "Location") ||
            Ascii.EqualsStringsIgnoreCase(name, "Max-Forwards") ||
            Ascii.EqualsStringsIgnoreCase(name, "Proxy-Authenticate") ||
            Ascii.EqualsStringsIgnoreCase(name, "Proxy-Authorization") ||
            Ascii.EqualsStringsIgnoreCase(name, "Proxy-Connection") ||
            Ascii.EqualsStringsIgnoreCase(name, "Range") ||
            Ascii.EqualsStringsIgnoreCase(name, "Retry-After") ||
            Ascii.EqualsStringsIgnoreCase(name, "TE") ||
            Ascii.EqualsStringsIgnoreCase(name, "Trailer") ||
            Ascii.EqualsStringsIgnoreCase(name, "Transfer-Encoding") ||
            Ascii.EqualsStringsIgnoreCase(name, "Upgrade") ||
            Ascii.EqualsStringsIgnoreCase(name, "Vary") ||
            Ascii.EqualsStringsIgnoreCase(name, "Warning") ||
            Ascii.EqualsStringsIgnoreCase(name, "WWW-Authenticate");
    }
}
