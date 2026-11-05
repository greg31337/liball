using System.Globalization;
using System.Text;

namespace http;

internal static class Http1FramingNormalizer
{
    internal static string? FindUnsafeConnectionOption(HttpHeaders headers)
    {
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Connection))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseConnection(
                    field.Value,
                    int.MaxValue,
                    int.MaxValue);
                int optionIndex = 0;
                while (optionIndex < parsed.Values.Count)
                {
                    string option = Encoding.ASCII.GetString(parsed.Values[optionIndex]);
                    if (Ascii.EqualsStringsIgnoreCase(
                        option,
                        HttpHeaderNames.ContentLength) ||
                        Ascii.EqualsStringsIgnoreCase(
                            option,
                            HttpHeaderNames.TransferEncoding))
                    {
                        return option;
                    }

                    optionIndex = optionIndex + 1;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return null;
    }

    internal static void NormalizeSelectedFraming(
        HttpHeaders headers,
        Http1BodyFraming framing,
        int contentLength)
    {
        HeaderStore.RemoveMatchingFields(headers, HttpHeaderNames.ContentLength);
        HeaderStore.RemoveMatchingFields(headers, HttpHeaderNames.TransferEncoding);
        if (framing != Http1BodyFraming.Chunked)
        {
            HeaderStore.RemoveMatchingFields(headers, HttpHeaderNames.Trailer);
        }

        if (framing == Http1BodyFraming.ContentLength)
        {
            AddNormalizedFramingField(
                headers,
                HttpHeaderNames.ContentLength,
                contentLength.ToString(CultureInfo.InvariantCulture));
        }
        else if (framing == Http1BodyFraming.Chunked)
        {
            AddNormalizedFramingField(
                headers,
                HttpHeaderNames.TransferEncoding,
                HttpTransferCodings.Chunked);
        }
    }

    static void AddNormalizedFramingField(
        HttpHeaders headers,
        string name,
        string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }
}
