using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Parses and serializes legacy HTTP field-value grammars.
/// </summary>
public class HttpFieldValueCodec : IHttpFieldValueCodec
{
    internal HttpLimits limits = new HttpLimits();

    /// <summary>Creates an HTTP field-value codec with the shared default limits.</summary>
    public HttpFieldValueCodec()
    {
    }

    /// <summary>Parses an HTTP date in any of the three RFC-recognized wire formats.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The normalized UTC timestamp.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public DateTimeOffset ParseHttpDate(byte[] input)
    {
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(
            input,
            limits.MaximumFieldValueSize);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-http-date",
            "maximum-field-value-size",
            limits.MaximumFieldValueSize);
        return parsed.Value;
    }

    /// <summary>Serializes a timestamp as IMF-fixdate in UTC.</summary>
    /// <param name="value">The timestamp to serialize.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public byte[] SerializeHttpDate(DateTimeOffset value)
    {
        FieldValueWriteResult written = HttpDateCodec.SerializeHttpDate(
            value,
            limits.MaximumFieldValueSize);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.Date,
            "maximum-field-value-size",
            limits.MaximumFieldValueSize);
    }

    /// <summary>Parses a media type or media range, including wildcard ranges.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed media value.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public HttpMediaType ParseMediaType(byte[] input)
    {
        MediaTypeParseResult parsed = FieldValueParser.ParseMediaRange(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumParameterCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-media-type",
            "maximum-parameter-count",
            limits.MaximumParameterCount);
        return parsed.Value;
    }

    /// <summary>Serializes a media type or media range in canonical form.</summary>
    /// <param name="value">The media value.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">The media value is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeMediaType(HttpMediaType value)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeMediaRange(
            value,
            limits.MaximumFieldValueSize,
            limits.MaximumParameterCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.ContentType,
            "maximum-parameter-count",
            limits.MaximumParameterCount);
    }

    /// <summary>Parses a weighted token list with thousandth-precision quality values.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered weighted values.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public List<HttpWeightedValue> ParseWeightedValues(byte[] input)
    {
        MediaTypeParseResult parsed = FieldValueParser.ParseWeightedValues(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-weighted-values",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.Values;
    }

    /// <summary>Serializes weighted token values in canonical form.</summary>
    /// <param name="values">The ordered weighted values.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">A weighted value is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeWeightedValues(List<HttpWeightedValue> values)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeWeightedValues(
            values,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        return GetSerializedBytes(
            written,
            "weighted-values",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
    }

    /// <summary>Parses RFC 4647 basic language ranges and their quality values.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered language ranges.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public List<HttpLanguageRange> ParseLanguageRanges(byte[] input)
    {
        LanguageRangeParseResult parsed = FieldValueParser.ParseLanguageRanges(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            false);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-language-ranges",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.Values;
    }

    /// <summary>Serializes RFC 4647 basic language ranges in canonical form.</summary>
    /// <param name="values">The ordered language ranges.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">A language range is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeLanguageRanges(List<HttpLanguageRange> values)
    {
        List<HttpWeightedValue> weighted = ConvertLanguageRanges(values);
        FieldValueWriteResult written = FieldValueWriter.SerializeWeightedValues(
            weighted,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        byte[] output = GetSerializedBytes(
            written,
            HttpHeaderNames.AcceptLanguage,
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        LanguageRangeParseResult validation = FieldValueParser.ParseLanguageRanges(
            output,
            output.Length,
            limits.MaximumListMemberCount,
            false);
        if (validation.Status != HttpOperationStatus.Done)
        {
            throw new HttpFieldException(
                "A language range cannot be serialized.",
                "invalid-language-ranges",
                HttpHeaderNames.AcceptLanguage);
        }

        return output;
    }

    /// <summary>Parses an entity-tag list or its wildcard form.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered entity tags.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public List<HttpEntityTag> ParseEntityTags(byte[] input)
    {
        EntityTagParseResult parsed = FieldValueParser.ParseEntityTags(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-entity-tags",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.Values;
    }

    /// <summary>Serializes an entity-tag list or its wildcard form.</summary>
    /// <param name="values">The ordered entity tags.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">An entity tag is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeEntityTags(List<HttpEntityTag> values)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeEntityTags(
            values,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.ETag,
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
    }

    /// <summary>Parses a complete Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed range request.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public HttpRangeRequest ParseRangeValue(byte[] input)
    {
        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumRangeCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-range-value",
            "maximum-range-count",
            limits.MaximumRangeCount);
        parsed.Range.Limits = limits;
        return parsed.Range;
    }

    /// <summary>Serializes a Range field value in canonical form.</summary>
    /// <param name="value">The range request.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">The range value is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeRangeValue(HttpRangeRequest value)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeRangeValue(
            value,
            limits.MaximumFieldValueSize,
            limits.MaximumRangeCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.Range,
            "maximum-range-count",
            limits.MaximumRangeCount);
    }

    /// <summary>Parses a complete Content-Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed content range.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public HttpContentRange ParseContentRange(byte[] input)
    {
        RangeValueParseResult parsed = FieldValueParser.ParseContentRange(
            input,
            limits.MaximumFieldValueSize);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-content-range",
            "maximum-field-value-size",
            limits.MaximumFieldValueSize);
        return parsed.ContentRange;
    }

    /// <summary>Serializes a Content-Range field value in canonical form.</summary>
    /// <param name="value">The content range.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">The content range is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public byte[] SerializeContentRange(HttpContentRange value)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeContentRange(
            value,
            limits.MaximumFieldValueSize);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.ContentRange,
            "maximum-field-value-size",
            limits.MaximumFieldValueSize);
    }

    /// <summary>Parses a list of authentication challenges or credentials.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered authentication values.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public List<HttpAuthenticationValue> ParseAuthenticationValues(byte[] input)
    {
        AuthenticationParseResult parsed = FieldValueParser.ParseAuthenticationValues(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-authentication-values",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.Values;
    }

    /// <summary>Serializes authentication challenges or credentials in canonical form.</summary>
    /// <param name="values">The authentication values.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">An authentication value is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeAuthenticationValues(List<HttpAuthenticationValue> values)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeAuthenticationValues(
            values,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.WWWAuthenticate,
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
    }

    /// <summary>Parses a list of Via entries.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered Via entries.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public List<HttpViaEntry> ParseViaEntries(byte[] input)
    {
        CommonFieldParseResult parsed = FieldValueParser.ParseViaEntries(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-via",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.ViaEntries;
    }

    /// <summary>Serializes Via entries in canonical form.</summary>
    /// <param name="values">The Via entries.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">A Via entry is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeViaEntries(List<HttpViaEntry> values)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeViaEntries(
            values,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.Via,
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
    }

    /// <summary>Parses Cache-Control directives.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed directives.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public HttpCacheControl ParseCacheControlValue(byte[] input)
    {
        CacheControlParseResult parsed = FieldValueParser.ParseCacheControlValue(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        ValidateParseResult(
            parsed.Status,
            parsed.IsLimitExceeded,
            parsed.ErrorPosition,
            input,
            "invalid-cache-control",
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
        return parsed.Value;
    }

    /// <summary>Serializes Cache-Control directives in deterministic canonical order.</summary>
    /// <param name="value">The parsed directives.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">The directive state is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A serialization limit is exceeded.</exception>
    public byte[] SerializeCacheControlValue(HttpCacheControl value)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeCacheControlValue(
            value,
            limits.MaximumFieldValueSize,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        return GetSerializedBytes(
            written,
            HttpHeaderNames.CacheControl,
            "maximum-list-member-count",
            limits.MaximumListMemberCount);
    }

    /// <summary>Parses an RFC 9218 Priority field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed priority.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public HttpPriority ParsePriority(byte[] input)
    {
        ValidatePriorityInput(input);
        return FieldValueParser.ParsePriority(input);
    }

    /// <summary>Serializes an RFC 9218 Priority field value canonically.</summary>
    /// <param name="value">The priority metadata.</param>
    /// <returns>The canonical field value.</returns>
    /// <exception cref="HttpFieldException">The priority state is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">The field value exceeds the size limit.</exception>
    public byte[] SerializePriority(HttpPriority value)
    {
        byte[] output = FieldValueWriter.SerializePriority(value);
        if (output.Length > limits.MaximumFieldValueSize)
        {
            throw new HttpLimitExceededException(
                "The serialized field value exceeds the configured size limit.",
                "maximum-field-value-size",
                limits.MaximumFieldValueSize,
                output.Length);
        }

        return output;
    }

    internal void ValidateParseResult(
        HttpOperationStatus status,
        bool isLimitExceeded,
        int errorPosition,
        byte[] input,
        string diagnosticCode,
        string memberLimitName,
        int memberLimit)
    {
        if (status == HttpOperationStatus.Done)
        {
            return;
        }

        if (isLimitExceeded)
        {
            string limitName = memberLimitName;
            long limit = memberLimit;
            long actual = (long)memberLimit + 1;
            if (input != null && input.Length > limits.MaximumFieldValueSize)
            {
                limitName = "maximum-field-value-size";
                limit = limits.MaximumFieldValueSize;
                actual = input.Length;
            }
            throw new HttpLimitExceededException(
                "Field-value parsing exceeded a configured limit.",
                limitName,
                limit,
                actual);
        }

        throw new HttpParseException(
            "The HTTP field value is malformed.",
            diagnosticCode,
            errorPosition);
    }

    internal byte[] GetSerializedBytes(
        FieldValueWriteResult written,
        string fieldName,
        string memberLimitName,
        int memberLimit)
    {
        if (written.Status == HttpOperationStatus.Done)
        {
            return written.Bytes;
        }

        if (written.IsLimitExceeded)
        {
            bool sizeExceeded = written.RequiredSize > limits.MaximumFieldValueSize;
            string limitName = sizeExceeded ?
                "maximum-field-value-size" : memberLimitName;
            long limit = sizeExceeded ? limits.MaximumFieldValueSize : memberLimit;
            long actual = sizeExceeded ? written.RequiredSize : (long)memberLimit + 1;
            throw new HttpLimitExceededException(
                "Field-value serialization exceeded a configured limit.",
                limitName,
                limit,
                actual);
        }

        throw new HttpFieldException(
            "The HTTP field value cannot be serialized.",
            "invalid-field-value",
            fieldName);
    }

    internal List<HttpWeightedValue> ConvertLanguageRanges(List<HttpLanguageRange> values)
    {
        if (values == null)
        {
            throw new HttpFieldException(
                "The language range list cannot be serialized.",
                "invalid-language-ranges",
                HttpHeaderNames.AcceptLanguage);
        }

        List<HttpWeightedValue> result = new List<HttpWeightedValue>();
        int index = 0;
        while (index < values.Count)
        {
            HttpLanguageRange value = values[index];
            if (value == null || value.Range == null ||
                value.IsWildcard != (value.Range == "*"))
            {
                throw new HttpFieldException(
                    "A language range cannot be serialized.",
                    "invalid-language-ranges",
                    HttpHeaderNames.AcceptLanguage);
            }

            HttpWeightedValue converted = new HttpWeightedValue();
            converted.Value = value.Range;
            converted.Quality = value.Quality;
            converted.SourceOrder = value.SourceOrder;
            converted.Specificity = value.Specificity;
            result.Add(converted);
            index = index + 1;
        }

        return result;
    }

    internal void ValidatePriorityInput(byte[] input)
    {
        if (input == null)
        {
            throw new HttpParseException(
                "The Priority field value is null.",
                "invalid-priority",
                0);
        }

        if (limits.MaximumFieldValueSize < 0 ||
            input.Length > limits.MaximumFieldValueSize)
        {
            throw new HttpLimitExceededException(
                "The Priority field value exceeds the configured size limit.",
                "maximum-field-value-size",
                limits.MaximumFieldValueSize,
                input.Length);
        }
    }
}
