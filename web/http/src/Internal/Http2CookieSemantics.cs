using System.Collections.Generic;

namespace http;

internal static class Http2CookieSemantics
{
    internal static void CombineDecodedCookies(HttpHeaders headers)
    {
        int cookieCount = 0;
        int firstCookieIndex = -1;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(
                headers.Fields[index].Name, HttpHeaderNames.Cookie))
            {
                if (firstCookieIndex < 0)
                {
                    firstCookieIndex = index;
                }

                cookieCount = cookieCount + 1;
            }

            index = index + 1;
        }

        if (cookieCount < 2)
        {
            return;
        }

        HttpHeaderField combined = CreateCombinedCookie(headers);
        List<HttpHeaderField> fields = new List<HttpHeaderField>();
        index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            bool cookie = Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.Cookie);
            if (index == firstCookieIndex)
            {
                fields.Add(combined);
            }
            else if (!cookie)
            {
                fields.Add(field);
            }

            index = index + 1;
        }

        headers.Fields = fields;
    }

    internal static HttpHeaders CreateEncodingCookieFields(
        HttpHeaders headers,
        bool splitCookies)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (splitCookies && Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.Cookie) &&
                ContainsSemicolon(field.Value))
            {
                AddSplitCookieFields(result, field);
            }
            else
            {
                result.Fields.Add(CopyCookieField(field));
            }

            index = index + 1;
        }

        return result;
    }

    internal static HttpHeaderField CreateCombinedCookie(HttpHeaders headers)
    {
        byte[] value = HeaderStore.CombineMatchingValues(
            headers, HttpHeaderNames.Cookie);
        HttpHeaderField combined = new HttpHeaderField();
        combined.Name = "cookie";
        combined.Value = value;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.Cookie))
            {
                combined.IsSensitive = combined.IsSensitive ||
                    field.IsSensitive;
                combined.NeverIndex = combined.NeverIndex ||
                    field.NeverIndex;
            }

            index = index + 1;
        }

        return combined;
    }

    internal static bool ContainsSemicolon(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == (byte)';')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddSplitCookieFields(
        HttpHeaders result,
        HttpHeaderField source)
    {
        int start = 0;
        int index = 0;
        while (index <= source.Value.Length)
        {
            if (index == source.Value.Length ||
                source.Value[index] == (byte)';')
            {
                int end = index;
                while (start < end &&
                    (source.Value[start] == 0x20 ||
                        source.Value[start] == 0x09))
                {
                    start = start + 1;
                }

                while (end > start &&
                    (source.Value[end - 1] == 0x20 ||
                        source.Value[end - 1] == 0x09))
                {
                    end = end - 1;
                }

                HttpHeaderField field = new HttpHeaderField();
                field.Name = source.Name;
                field.Value = CopyCookieSegment(source.Value, start, end);
                field.IsSensitive = source.IsSensitive;
                field.NeverIndex = source.NeverIndex;
                result.Fields.Add(field);
                start = index + 1;
            }

            index = index + 1;
        }
    }

    internal static byte[] CopyCookieSegment(
        byte[] value,
        int start,
        int end)
    {
        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    internal static HttpHeaderField CopyCookieField(HttpHeaderField source)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = source.Name;
        field.Value = Http2RequestSemantics.CopyBytes(source.Value);
        field.IsSensitive = source.IsSensitive;
        field.NeverIndex = source.NeverIndex;
        return field;
    }
}
