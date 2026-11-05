using System;
using System.Collections.Generic;

namespace http;

internal static class PreconditionParser
{
    internal static PreconditionFieldValue ParseEntityTagCondition(
        HttpHeaders headers,
        string name)
    {
        ValidateHeaders(headers);
        ValidateConditionName(name);
        PreconditionFieldValue result = new PreconditionFieldValue();
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            result.IsValid = true;
            return result;
        }

        result.IsPresent = true;
        byte[] combined = HeaderStore.CombineMatchingValues(headers, name);
        EntityTagParseResult parsed = FieldValueParser.ParseEntityTags(
            combined,
            combined.Length,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        result.IsValid = true;
        result.IsWildcard = parsed.IsWildcard;
        result.EntityTags = parsed.Values;
        return result;
    }

    internal static PreconditionFieldValue ParseDateCondition(
        HttpHeaders headers,
        string name)
    {
        ValidateHeaders(headers);
        ValidateConditionName(name);
        PreconditionFieldValue result = new PreconditionFieldValue();
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            result.IsValid = true;
            return result;
        }

        result.IsPresent = true;
        if (fields.Count != 1)
        {
            return result;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(value, value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        result.IsValid = true;
        result.IsDate = true;
        result.Date = parsed.Value;
        return result;
    }

    internal static PreconditionFieldValue ParseIfRangeCondition(HttpHeaders headers)
    {
        ValidateHeaders(headers);
        PreconditionFieldValue result = new PreconditionFieldValue();
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.IfRange);
        if (fields.Count == 0)
        {
            result.IsValid = true;
            return result;
        }

        result.IsPresent = true;
        if (fields.Count != 1)
        {
            return result;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        EntityTagParseResult entityTag = FieldValueParser.ParseEntityTag(value, value.Length);
        if (entityTag.Status == HttpOperationStatus.Done)
        {
            result.IsValid = true;
            result.IsEntityTag = true;
            result.EntityTag = entityTag.Value;
            return result;
        }

        HttpDateParseResult date = HttpDateCodec.ParseHttpDate(value, value.Length);
        if (date.Status == HttpOperationStatus.Done)
        {
            result.IsValid = true;
            result.IsDate = true;
            result.Date = date.Value;
        }

        return result;
    }

    static byte[] TrimOptionalWhitespace(byte[] value)
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

    static void ValidateHeaders(HttpHeaders headers)
    {
        if (headers == null || headers.Fields == null)
        {
            throw new HttpProtocolException(
                "The conditional field collection is missing.",
                "invalid-precondition-context");
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw new HttpProtocolException(
                    "The conditional field collection is malformed.",
                    "invalid-precondition-context");
            }

            index = index + 1;
        }
    }

    static void ValidateConditionName(string name)
    {
        if (!HeaderStore.IsValidName(name))
        {
            throw new HttpProtocolException(
                "The conditional field name is malformed.",
                "invalid-precondition-context");
        }
    }
}
