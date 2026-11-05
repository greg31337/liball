using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace http;

internal static class RepresentationSemantics
{
    internal static HttpRepresentation ExtractFromResponse(HttpResponse response)
    {
        ValidateResponse(response);
        HttpRepresentation result = new HttpRepresentation();
        result.Content = CopyBytes(response.Content);
        HttpHeaders fields = CollectRepresentationFields(response);

        byte[]? contentType = ReadSingleField(fields, HttpHeaderNames.ContentType);
        if (contentType != null)
        {
            MediaTypeParseResult parsed = FieldValueParser.ParseMediaType(
                contentType,
                contentType.Length,
                int.MaxValue);
            RequireDone(parsed.Status, HttpHeaderNames.ContentType);
            result.MediaType = parsed.Value;
            result.Charset = ReadCharset(parsed.Value);
        }

        ReadTokenList(
            fields,
            HttpHeaderNames.ContentEncoding,
            result.ContentCodings,
            false);
        ReadTokenList(
            fields,
            HttpHeaderNames.ContentLanguage,
            result.Languages,
            true);

        byte[]? contentLength = ReadSingleField(fields, HttpHeaderNames.ContentLength);
        if (contentLength != null)
        {
            IntegerParseResult parsed = IntegerParser.ParseDecimal(contentLength);
            if (!parsed.IsValid || parsed.Consumed != contentLength.Length)
            {
                throw CreateFieldError(
                    HttpHeaderNames.ContentLength,
                    "The Content-Length field is malformed.");
            }

            result.HasContentLength = true;
            result.ContentLength = parsed.Value;
        }

        byte[]? contentLocation = ReadSingleField(
            fields,
            HttpHeaderNames.ContentLocation);
        if (contentLocation != null)
        {
            FieldValueParseResult parsed = FieldValueParser.ParseContentLocation(
                contentLocation,
                contentLocation.Length);
            RequireDone(parsed.Status, HttpHeaderNames.ContentLocation);
            result.ContentLocation = Encoding.ASCII.GetString(parsed.Value);
        }

        byte[]? entityTag = ReadSingleField(fields, HttpHeaderNames.ETag);
        if (entityTag != null)
        {
            EntityTagParseResult parsed = FieldValueParser.ParseEntityTag(
                entityTag,
                entityTag.Length);
            RequireDone(parsed.Status, HttpHeaderNames.ETag);
            result.EntityTag = parsed.Value;
        }

        byte[]? lastModified = ReadSingleField(
            fields,
            HttpHeaderNames.LastModified);
        if (lastModified != null)
        {
            HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(
                lastModified,
                lastModified.Length);
            RequireDone(parsed.Status, HttpHeaderNames.LastModified);
            result.HasLastModified = true;
            result.LastModified = parsed.Value;
        }

        ReadAcceptedRanges(fields, result.AcceptedRangeUnits);
        return result;
    }

    internal static void ApplyToResponse(
        HttpRepresentation representation,
        HttpResponse response)
    {
        ValidateRepresentation(representation);
        ValidateResponse(response);

        byte[]? contentType = SerializeContentType(representation);
        byte[]? contentEncoding = SerializeTokenList(
            representation.ContentCodings,
            HttpHeaderNames.ContentEncoding,
            false);
        byte[]? contentLanguage = SerializeTokenList(
            representation.Languages,
            HttpHeaderNames.ContentLanguage,
            true);
        byte[]? contentLength = SerializeContentLength(representation);
        byte[]? contentLocation = SerializeContentLocation(representation.ContentLocation);
        byte[]? entityTag = SerializeEntityTag(representation.EntityTag);
        byte[]? lastModified = SerializeLastModified(representation);
        byte[]? acceptRanges = SerializeAcceptedRanges(representation.AcceptedRangeUnits);

        ReplaceOptionalField(response.Headers, HttpHeaderNames.ContentType, contentType);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.ContentEncoding, contentEncoding);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.ContentLanguage, contentLanguage);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.ContentLength, contentLength);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.ContentLocation, contentLocation);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.ETag, entityTag);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.LastModified, lastModified);
        ReplaceOptionalField(response.Headers, HttpHeaderNames.AcceptRanges, acceptRanges);
        RemoveRepresentationTrailers(response.Trailers);
        response.Content = CopyBytes(representation.Content);
    }

    static HttpHeaders CollectRepresentationFields(HttpResponse response)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < response.Headers.Fields.Count)
        {
            result.Fields.Add(response.Headers.Fields[index]);
            index = index + 1;
        }

        index = 0;
        while (index < response.Trailers.Fields.Count)
        {
            result.Fields.Add(response.Trailers.Fields[index]);
            index = index + 1;
        }

        return result;
    }

    static void RemoveRepresentationTrailers(HttpHeaders trailers)
    {
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ContentType);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ContentEncoding);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ContentLanguage);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ContentLength);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ContentLocation);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.ETag);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.LastModified);
        HeaderStore.RemoveMatchingFields(trailers, HttpHeaderNames.AcceptRanges);
    }

    internal static string? ReadCharset(HttpMediaType mediaType)
    {
        string? result = null;
        int index = 0;
        while (index < mediaType.Parameters.Count)
        {
            HttpParameter parameter = mediaType.Parameters[index];
            if (Ascii.EqualsStringsIgnoreCase(parameter.Name, "charset"))
            {
                if (result != null || !IsTokenBytes(parameter.Value))
                {
                    throw CreateFieldError(
                        HttpHeaderNames.ContentType,
                        "The Content-Type charset parameter is malformed or duplicated.");
                }

                result = Encoding.ASCII.GetString(parameter.Value);
            }

            index = index + 1;
        }

        return result;
    }

    static void ReadTokenList(
        HttpHeaders headers,
        string name,
        List<string> destination,
        bool languages)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            return;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(headers, name);
        FieldValueParseResult parsed = CommonFieldValueParser.ParseTokenList(
            combined,
            combined.Length,
            int.MaxValue);
        RequireDone(parsed.Status, name);
        if (parsed.Values.Count == 0)
        {
            throw CreateFieldError(name, "The representation list field is empty.");
        }

        int index = 0;
        while (index < parsed.Values.Count)
        {
            string value = Encoding.ASCII.GetString(parsed.Values[index]);
            if (languages && !IsLanguageTag(value))
            {
                throw CreateFieldError(
                    name,
                    "The Content-Language field contains an invalid language tag.");
            }

            destination.Add(value);
            index = index + 1;
        }
    }

    static void ReadAcceptedRanges(HttpHeaders headers, List<string> destination)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.AcceptRanges);
        if (fields.Count == 0)
        {
            return;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(
            headers,
            HttpHeaderNames.AcceptRanges);
        RangeValueParseResult parsed = FieldValueParser.ParseAcceptRanges(
            combined,
            combined.Length,
            int.MaxValue);
        RequireDone(parsed.Status, HttpHeaderNames.AcceptRanges);
        int index = 0;
        while (index < parsed.Units.Count)
        {
            string unit = parsed.Units[index];
            if (Ascii.EqualsStringsIgnoreCase(unit, "none"))
            {
                if (parsed.Units.Count != 1)
                {
                    throw CreateFieldError(
                        HttpHeaderNames.AcceptRanges,
                        "The none range value cannot be combined with another range unit.");
                }

                return;
            }

            destination.Add(unit);
            index = index + 1;
        }
    }

    internal static byte[]? SerializeContentType(HttpRepresentation representation)
    {
        if (representation.MediaType == null)
        {
            if (representation.Charset != null)
            {
                throw CreateFieldError(
                    HttpHeaderNames.ContentType,
                    "A charset cannot be applied without a media type.");
            }

            return null;
        }

        HttpMediaType mediaType = CopyMediaTypeWithCharset(
            representation.MediaType,
            representation.Charset);
        FieldValueWriteResult written = FieldValueWriter.SerializeMediaType(
            mediaType,
            int.MaxValue,
            int.MaxValue);
        RequireDone(written.Status, HttpHeaderNames.ContentType);
        return written.Bytes;
    }

    static HttpMediaType CopyMediaTypeWithCharset(
        HttpMediaType source,
        string? charset)
    {
        if (source.Parameters == null)
        {
            throw CreateFieldError(
                HttpHeaderNames.ContentType,
                "The media type parameter collection is incomplete.");
        }

        HttpMediaType result = new HttpMediaType();
        result.Type = source.Type;
        result.Subtype = source.Subtype;
        result.IsTypeWildcard = source.IsTypeWildcard;
        result.IsSubtypeWildcard = source.IsSubtypeWildcard;
        string? embeddedCharset = null;
        int index = 0;
        while (index < source.Parameters.Count)
        {
            HttpParameter parameter = source.Parameters[index];
            if (parameter == null || parameter.Name == null || parameter.Value == null)
            {
                throw CreateFieldError(
                    HttpHeaderNames.ContentType,
                    "The media type contains incomplete parameter metadata.");
            }

            if (Ascii.EqualsStringsIgnoreCase(parameter.Name, "charset"))
            {
                if (embeddedCharset != null || !IsTokenBytes(parameter.Value))
                {
                    throw CreateFieldError(
                        HttpHeaderNames.ContentType,
                        "The media type charset parameter is malformed or duplicated.");
                }

                embeddedCharset = Encoding.ASCII.GetString(parameter.Value);
            }
            else
            {
                result.Parameters.Add(CopyParameter(parameter));
            }

            index = index + 1;
        }

        string? selectedCharset = charset;
        if (selectedCharset == null)
        {
            selectedCharset = embeddedCharset;
        }

        if (selectedCharset != null)
        {
            byte[] value = Encoding.ASCII.GetBytes(selectedCharset);
            if (!IsAsciiString(selectedCharset) || !IsTokenBytes(value))
            {
                throw CreateFieldError(
                    HttpHeaderNames.ContentType,
                    "The representation charset is not a valid token.");
            }

            HttpParameter parameter = new HttpParameter();
            parameter.Name = "charset";
            parameter.HasValue = true;
            parameter.Value = value;
            result.Parameters.Add(parameter);
        }

        return result;
    }

    static byte[]? SerializeTokenList(
        List<string> values,
        string name,
        bool languages)
    {
        if (values.Count == 0)
        {
            return null;
        }

        int index = 0;
        while (index < values.Count)
        {
            if (languages && !IsLanguageTag(values[index]))
            {
                throw CreateFieldError(
                    name,
                    "The representation contains an invalid language tag.");
            }

            index = index + 1;
        }

        FieldValueWriteResult written = CommonFieldValueWriter.SerializeTokenList(
            values,
            int.MaxValue,
            int.MaxValue);
        RequireDone(written.Status, name);
        return written.Bytes;
    }

    static byte[]? SerializeContentLength(HttpRepresentation representation)
    {
        if (!representation.HasContentLength)
        {
            return null;
        }

        if (representation.ContentLength < 0)
        {
            throw CreateFieldError(
                HttpHeaderNames.ContentLength,
                "The representation content length cannot be negative.");
        }

        return Encoding.ASCII.GetBytes(
            representation.ContentLength.ToString(CultureInfo.InvariantCulture));
    }

    static byte[]? SerializeContentLocation(string? value)
    {
        if (value == null)
        {
            return null;
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeContentLocation(
            value,
            int.MaxValue);
        RequireDone(written.Status, HttpHeaderNames.ContentLocation);
        return written.Bytes;
    }

    static byte[]? SerializeEntityTag(HttpEntityTag? value)
    {
        if (value == null)
        {
            return null;
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeEntityTag(value, int.MaxValue);
        RequireDone(written.Status, HttpHeaderNames.ETag);
        return written.Bytes;
    }

    static byte[]? SerializeLastModified(HttpRepresentation representation)
    {
        if (!representation.HasLastModified)
        {
            return null;
        }

        FieldValueWriteResult written = HttpDateCodec.SerializeHttpDate(
            representation.LastModified,
            int.MaxValue);
        RequireDone(written.Status, HttpHeaderNames.LastModified);
        return written.Bytes;
    }

    static byte[]? SerializeAcceptedRanges(List<string> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        int index = 0;
        while (index < values.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(values[index], "none"))
            {
                throw CreateFieldError(
                    HttpHeaderNames.AcceptRanges,
                    "The none value is represented by an empty range-unit collection.");
            }

            index = index + 1;
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeAcceptRanges(
            values,
            int.MaxValue,
            int.MaxValue);
        RequireDone(written.Status, HttpHeaderNames.AcceptRanges);
        return written.Bytes;
    }

    static byte[]? ReadSingleField(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            return null;
        }

        if (fields.Count != 1)
        {
            throw CreateFieldError(name, "The representation field occurs more than once.");
        }

        return TrimOptionalWhitespace(fields[0].Value);
    }

    static void ReplaceOptionalField(HttpHeaders headers, string name, byte[]? value)
    {
        if (value == null)
        {
            HeaderStore.RemoveMatchingFields(headers, name);
            return;
        }

        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        HeaderStore.ReplaceFields(headers, field);
    }

    static HttpParameter CopyParameter(HttpParameter source)
    {
        HttpParameter result = new HttpParameter();
        result.Name = source.Name;
        result.HasValue = source.HasValue;
        result.Value = CopyBytes(source.Value);
        result.IsQuoted = source.IsQuoted;
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

    internal static bool IsLanguageTag(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int subtagLength = 0;
        bool firstSubtag = true;
        bool firstSubtagLetters = true;
        char firstCharacter = value[0];
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '-')
            {
                if (subtagLength == 0 || subtagLength > 8)
                {
                    return false;
                }

                if (firstSubtag && (!firstSubtagLetters ||
                    subtagLength == 1 && firstCharacter != 'i' && firstCharacter != 'I' &&
                    firstCharacter != 'x' && firstCharacter != 'X'))
                {
                    return false;
                }

                subtagLength = 0;
                firstSubtag = false;
            }
            else
            {
                if (!(character >= 'A' && character <= 'Z') &&
                    !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9'))
                {
                    return false;
                }

                subtagLength = subtagLength + 1;
                if (firstSubtag && character >= '0' && character <= '9')
                {
                    firstSubtagLetters = false;
                }
            }

            index = index + 1;
        }

        if (subtagLength == 0 || subtagLength > 8)
        {
            return false;
        }

        if (firstSubtag)
        {
            return firstSubtagLetters && subtagLength >= 2;
        }

        return true;
    }

    static bool IsAsciiString(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsTokenBytes(byte[] value)
    {
        FieldValueParseResult parsed = FieldValueParser.ParseToken(value, value.Length);
        return parsed.Status == HttpOperationStatus.Done;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    static void RequireDone(HttpOperationStatus status, string name)
    {
        if (status != HttpOperationStatus.Done)
        {
            throw CreateFieldError(name, "The representation field value is malformed.");
        }
    }

    static void ValidateRepresentation(HttpRepresentation representation)
    {
        if (representation == null || representation.ContentCodings == null ||
            representation.Languages == null || representation.AcceptedRangeUnits == null ||
            representation.Content == null)
        {
            throw CreateFieldError("", "The representation metadata is incomplete.");
        }
    }

    static void ValidateResponse(HttpResponse response)
    {
        if (response == null || response.Headers == null ||
            response.Headers.Fields == null || response.Content == null ||
            response.Trailers == null || response.Trailers.Fields == null)
        {
            throw CreateFieldError("", "The HTTP response is incomplete.");
        }

        int index = 0;
        while (index < response.Headers.Fields.Count)
        {
            HttpHeaderField field = response.Headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw CreateFieldError("", "The HTTP response contains a malformed field.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < response.Trailers.Fields.Count)
        {
            HttpHeaderField field = response.Trailers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw CreateFieldError("", "The HTTP response contains a malformed trailer.");
            }

            index = index + 1;
        }

        if (!HeaderStore.AreValidTrailers(response.Trailers))
        {
            throw CreateFieldError("", "The HTTP response contains a prohibited trailer.");
        }
    }

    static HttpFieldException CreateFieldError(string name, string message)
    {
        return new HttpFieldException(message, "invalid-representation-field", name);
    }
}
