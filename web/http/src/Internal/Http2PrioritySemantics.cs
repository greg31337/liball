using System;

namespace http;

internal static class Http2PrioritySemantics
{
    internal static HttpPriority? ReadPrioritySignal(HttpHeaders headers)
    {
        byte[] combined = Array.Empty<byte>();
        bool found = false;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field != null && field.Name != null && field.Value != null &&
                Ascii.EqualsStringsIgnoreCase(
                    field.Name, HttpHeaderNames.Priority))
            {
                combined = AppendPriorityValue(combined, field.Value, found);
                found = true;
            }

            index = index + 1;
        }

        if (!found)
        {
            return null;
        }

        return PriorityCodec.ParseValidated(combined);
    }

    static byte[] AppendPriorityValue(
        byte[] existing,
        byte[] value,
        bool includeSeparator)
    {
        int separatorLength = includeSeparator ? 2 : 0;
        byte[] result = new byte[
            existing.Length + separatorLength + value.Length];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }

        if (includeSeparator)
        {
            result[index] = (byte)',';
            result[index + 1] = (byte)' ';
            index = index + 2;
        }

        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            result[index + valueIndex] = value[valueIndex];
            valueIndex = valueIndex + 1;
        }

        return result;
    }
}
