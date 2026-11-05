using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace http;

internal static class MultipartRangeCodec
{
    internal static HttpMultipartRange Build(
        HttpRepresentation representation,
        List<HttpByteRange> ranges,
        string boundary)
    {
        ValidateBuildContext(representation, ranges, boundary);
        string selectedBoundary = boundary;
        if (selectedBoundary.Length == 0)
        {
            selectedBoundary = GenerateBoundary(representation, ranges);
        }

        if (!IsValidBoundary(selectedBoundary))
        {
            throw CreateProtocolError(
                "The multipart boundary is invalid.",
                "invalid-multipart-boundary");
        }

        if (HasBoundaryCollision(representation.Content, ranges, selectedBoundary))
        {
            throw CreateProtocolError(
                "The multipart boundary occurs within selected representation data.",
                "multipart-boundary-collision");
        }

        byte[]? mediaType = RepresentationSemantics.SerializeContentType(representation);
        byte[] boundaryBytes = Encoding.ASCII.GetBytes(selectedBoundary);
        long completeLength = representation.Content.LongLength;
        long requiredLength = boundaryBytes.Length + 6;
        int index = 0;
        while (index < ranges.Count)
        {
            HttpContentRange contentRange = RangeSemantics.BuildContentRange(
                ranges[index],
                completeLength);
            FieldValueWriteResult serializedRange = FieldValueWriter.SerializeContentRange(
                contentRange,
                int.MaxValue);
            if (serializedRange.Status != HttpOperationStatus.Done)
            {
                throw CreateProtocolError(
                    "Content-Range metadata cannot be serialized.",
                    "invalid-content-range");
            }

            byte[] header = BuildPartHeader(mediaType, serializedRange.Bytes);
            long contentLength = CalculateRangeLength(ranges[index]);
            requiredLength = AddCheckedLength(requiredLength, boundaryBytes.Length + 4);
            requiredLength = AddCheckedLength(requiredLength, header.Length);
            requiredLength = AddCheckedLength(requiredLength, contentLength);
            requiredLength = AddCheckedLength(requiredLength, 2);
            index = index + 1;
        }

        if (requiredLength > int.MaxValue)
        {
            throw CreateLimitError(
                "multipart-content-size",
                int.MaxValue,
                requiredLength);
        }

        byte[] output = new byte[(int)requiredLength];
        int outputIndex = 0;
        HttpMultipartRange result = new HttpMultipartRange();
        result.Boundary = selectedBoundary;
        result.ContentType = BuildMultipartContentType(selectedBoundary);
        index = 0;
        while (index < ranges.Count)
        {
            HttpByteRange range = ranges[index];
            HttpContentRange contentRange = RangeSemantics.BuildContentRange(
                range,
                completeLength);
            FieldValueWriteResult serializedRange = FieldValueWriter.SerializeContentRange(
                contentRange,
                int.MaxValue);
            byte[] header = BuildPartHeader(mediaType, serializedRange.Bytes);

            output[outputIndex] = 0x2d;
            output[outputIndex + 1] = 0x2d;
            outputIndex = CopyBytesAt(boundaryBytes, output, outputIndex + 2);
            output[outputIndex] = 0x0d;
            output[outputIndex + 1] = 0x0a;
            outputIndex = outputIndex + 2;
            outputIndex = CopyBytesAt(header, output, outputIndex);
            int rangeLength = (int)CalculateRangeLength(range);
            Array.Copy(
                representation.Content,
                (int)range.Start,
                output,
                outputIndex,
                rangeLength);
            outputIndex = outputIndex + rangeLength;
            output[outputIndex] = 0x0d;
            output[outputIndex + 1] = 0x0a;
            outputIndex = outputIndex + 2;
            result.Parts.Add(CreateBuiltPart(
                representation,
                mediaType,
                contentRange,
                range));
            index = index + 1;
        }

        output[outputIndex] = 0x2d;
        output[outputIndex + 1] = 0x2d;
        outputIndex = CopyBytesAt(boundaryBytes, output, outputIndex + 2);
        output[outputIndex] = 0x2d;
        output[outputIndex + 1] = 0x2d;
        output[outputIndex + 2] = 0x0d;
        output[outputIndex + 3] = 0x0a;
        result.EncodedLength = requiredLength;
        result.EncodedBody = output;
        return result;
    }

    internal static HttpMultipartRange Parse(
        byte[] content,
        string boundary,
        HttpLimits limits)
    {
        ValidateParseContext(content, boundary, limits);
        byte[] boundaryBytes = Encoding.ASCII.GetBytes(boundary);
        MultipartBoundaryMatch first = FindFirstBoundary(content, boundaryBytes);
        if (!first.IsMatch || first.IsClosing)
        {
            throw CreateParseError(
                "The multipart body has no initial part boundary.",
                "missing-multipart-boundary",
                0);
        }

        HttpMultipartRange result = new HttpMultipartRange();
        result.Boundary = boundary;
        result.ContentType = BuildMultipartContentType(boundary);
        result.EncodedLength = content.LongLength;
        result.EncodedBody = CopyBytes(content);
        bool hasCompleteLength = false;
        long completeLength = 0;
        int position = first.NextPosition;
        bool complete = false;
        while (!complete)
        {
            if (result.Parts.Count >= limits.MaximumRangeCount)
            {
                throw CreateLimitError(
                    "maximum-range-count",
                    limits.MaximumRangeCount,
                    result.Parts.Count + 1);
            }

            MultipartHeaderResult headers = ParsePartHeaders(content, position, limits);
            MultipartBoundaryMatch next = FindFollowingBoundary(
                content,
                headers.NextPosition,
                boundaryBytes);
            if (!next.IsMatch)
            {
                throw CreateParseError(
                    "The multipart body is missing its closing boundary.",
                    "truncated-multipart-body",
                    headers.NextPosition);
            }

            int bodyEnd = next.BoundaryPosition - 2;
            HttpContentRange? contentRange = headers.ContentRange;
            if (contentRange == null)
            {
                throw CreateParseError(
                    "A byte-range part is missing Content-Range metadata.",
                    "missing-content-range",
                    position);
            }

            ValidateParsedContentRange(contentRange, bodyEnd - headers.NextPosition);
            if (contentRange.HasCompleteLength)
            {
                if (hasCompleteLength && completeLength != contentRange.CompleteLength)
                {
                    throw CreateParseError(
                        "Multipart byte ranges have conflicting complete lengths.",
                        "conflicting-complete-length",
                        position);
                }

                hasCompleteLength = true;
                completeLength = contentRange.CompleteLength;
            }

            result.Parts.Add(CreateParsedPart(
                content,
                headers.NextPosition,
                bodyEnd,
                headers.MediaType,
                contentRange));
            complete = next.IsClosing;
            position = next.NextPosition;
        }

        if (result.Parts.Count == 0)
        {
            throw CreateParseError(
                "A multipart byte-range body must contain at least one part.",
                "empty-multipart-body",
                first.BoundaryPosition);
        }

        return result;
    }

    static void ValidateBuildContext(
        HttpRepresentation representation,
        List<HttpByteRange> ranges,
        string boundary)
    {
        if (representation == null || representation.Content == null ||
            ranges == null || boundary == null)
        {
            throw CreateProtocolError(
                "The multipart construction context is incomplete.",
                "invalid-multipart-context");
        }

        if (ranges.Count < 2)
        {
            throw CreateProtocolError(
                "A generated multipart response requires at least two ranges.",
                "insufficient-multipart-ranges");
        }

        if (representation.HasContentLength &&
            representation.ContentLength != representation.Content.LongLength)
        {
            throw CreateProtocolError(
                "The representation length does not match its owned content.",
                "inconsistent-representation-length");
        }

        int index = 0;
        while (index < ranges.Count)
        {
            RangeSemantics.BuildContentRange(
                ranges[index],
                representation.Content.LongLength);
            index = index + 1;
        }
    }

    static void ValidateParseContext(
        byte[] content,
        string boundary,
        HttpLimits limits)
    {
        if (content == null || limits == null || !IsValidBoundary(boundary))
        {
            throw CreateProtocolError(
                "The multipart parsing context is invalid.",
                "invalid-multipart-context");
        }

        if (limits.MaximumContentSize < 0 || limits.MaximumFieldSectionSize < 0 ||
            limits.MaximumFieldCount < 0 || limits.MaximumFieldLineSize < 0 ||
            limits.MaximumFieldValueSize < 0 || limits.MaximumRangeCount < 0 ||
            limits.MaximumParameterCount < 0)
        {
            throw CreateProtocolError(
                "Multipart parsing limits cannot be negative.",
                "invalid-multipart-limits");
        }

        if (content.LongLength > limits.MaximumContentSize)
        {
            throw CreateLimitError(
                "maximum-content-size",
                limits.MaximumContentSize,
                content.LongLength);
        }
    }

    static string GenerateBoundary(
        HttpRepresentation representation,
        List<HttpByteRange> ranges)
    {
        const string Hex = "0123456789abcdef";
        int attempt = 0;
        while (attempt < 16)
        {
            byte[] random = new byte[18];
            RandomNumberGenerator.Fill(random);
            StringBuilder builder = new StringBuilder("http-range-");
            int index = 0;
            while (index < random.Length)
            {
                builder.Append(Hex[random[index] >> 4]);
                builder.Append(Hex[random[index] & 0x0f]);
                index = index + 1;
            }

            string boundary = builder.ToString();
            if (!HasBoundaryCollision(representation.Content, ranges, boundary))
            {
                return boundary;
            }

            attempt = attempt + 1;
        }

        throw CreateProtocolError(
            "A collision-free multipart boundary could not be generated.",
            "multipart-boundary-generation-failed");
    }

    static bool IsValidBoundary(string boundary)
    {
        if (boundary == null || boundary.Length == 0 || boundary.Length > 70 ||
            boundary[boundary.Length - 1] == ' ')
        {
            return false;
        }

        int index = 0;
        while (index < boundary.Length)
        {
            char character = boundary[index];
            bool alphaNumeric = character >= '0' && character <= '9' ||
                character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z';
            bool punctuation = character == '\'' || character == '(' ||
                character == ')' || character == '+' || character == '_' ||
                character == ',' || character == '-' || character == '.' ||
                character == '/' || character == ':' || character == '=' ||
                character == '?';
            if (!alphaNumeric && !punctuation && character != ' ')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool HasBoundaryCollision(
        byte[] content,
        List<HttpByteRange> ranges,
        string boundary)
    {
        byte[] prefix = Encoding.ASCII.GetBytes("--" + boundary);
        int rangeIndex = 0;
        while (rangeIndex < ranges.Count)
        {
            HttpByteRange range = ranges[rangeIndex];
            int start = (int)range.Start;
            int end = (int)range.End + 1;
            int index = start;
            while (index + prefix.Length <= end)
            {
                bool lineStart = index == start || index >= start + 2 &&
                    content[index - 2] == 0x0d && content[index - 1] == 0x0a;
                if (lineStart && MatchesBytesAt(content, index, prefix))
                {
                    return true;
                }

                index = index + 1;
            }

            rangeIndex = rangeIndex + 1;
        }

        return false;
    }

    static string BuildMultipartContentType(string boundary)
    {
        if (IsHttpToken(boundary))
        {
            return "multipart/byteranges; boundary=" + boundary;
        }

        return "multipart/byteranges; boundary=\"" + boundary + "\"";
    }

    static bool IsHttpToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character > 0x7f || !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return value.Length != 0;
    }

    static byte[] BuildPartHeader(byte[]? mediaType, byte[] contentRange)
    {
        byte[] contentTypeName = Encoding.ASCII.GetBytes("Content-Type: ");
        byte[] contentRangeName = Encoding.ASCII.GetBytes("Content-Range: ");
        int length = contentRangeName.Length + contentRange.Length + 4;
        if (mediaType != null)
        {
            length = length + contentTypeName.Length + mediaType.Length + 2;
        }

        byte[] result = new byte[length];
        int index = 0;
        if (mediaType != null)
        {
            index = CopyBytesAt(contentTypeName, result, index);
            index = CopyBytesAt(mediaType, result, index);
            result[index] = 0x0d;
            result[index + 1] = 0x0a;
            index = index + 2;
        }

        index = CopyBytesAt(contentRangeName, result, index);
        index = CopyBytesAt(contentRange, result, index);
        result[index] = 0x0d;
        result[index + 1] = 0x0a;
        result[index + 2] = 0x0d;
        result[index + 3] = 0x0a;
        return result;
    }

    static long CalculateRangeLength(HttpByteRange range)
    {
        return range.End - range.Start + 1;
    }

    static long AddCheckedLength(long current, long additional)
    {
        if (additional < 0 || current > long.MaxValue - additional)
        {
            throw CreateLimitError(
                "multipart-content-size",
                long.MaxValue,
                long.MaxValue);
        }

        return current + additional;
    }

    static HttpRepresentation CreateBuiltPart(
        HttpRepresentation source,
        byte[]? mediaType,
        HttpContentRange contentRange,
        HttpByteRange range)
    {
        HttpRepresentation result = new HttpRepresentation();
        if (mediaType != null)
        {
            MediaTypeParseResult parsed = FieldValueParser.ParseMediaType(
                mediaType,
                mediaType.Length,
                int.MaxValue);
            result.MediaType = parsed.Value;
            result.Charset = RepresentationSemantics.ReadCharset(parsed.Value);
        }

        int length = (int)CalculateRangeLength(range);
        result.HasContentLength = true;
        result.ContentLength = length;
        result.ContentRange = contentRange;
        result.Content = new byte[length];
        Array.Copy(source.Content, (int)range.Start, result.Content, 0, length);
        return result;
    }

    static MultipartBoundaryMatch FindFirstBoundary(
        byte[] content,
        byte[] boundary)
    {
        int index = 0;
        while (index < content.Length)
        {
            bool lineStart = index == 0 || index >= 2 &&
                content[index - 2] == 0x0d && content[index - 1] == 0x0a;
            if (lineStart)
            {
                MultipartBoundaryMatch match = MatchBoundaryLine(
                    content,
                    index,
                    boundary);
                if (match.IsMatch)
                {
                    return match;
                }
            }

            index = index + 1;
        }

        return new MultipartBoundaryMatch();
    }

    static MultipartBoundaryMatch FindFollowingBoundary(
        byte[] content,
        int start,
        byte[] boundary)
    {
        int index = start;
        while (index + 2 < content.Length)
        {
            if (content[index] == 0x0d && content[index + 1] == 0x0a)
            {
                MultipartBoundaryMatch match = MatchBoundaryLine(
                    content,
                    index + 2,
                    boundary);
                if (match.IsMatch)
                {
                    return match;
                }
            }

            index = index + 1;
        }

        return new MultipartBoundaryMatch();
    }

    static MultipartBoundaryMatch MatchBoundaryLine(
        byte[] content,
        int position,
        byte[] boundary)
    {
        MultipartBoundaryMatch result = new MultipartBoundaryMatch();
        int required = boundary.Length + 2;
        if (position < 0 || position > content.Length - required ||
            content[position] != 0x2d || content[position + 1] != 0x2d ||
            !MatchesBytesAt(content, position + 2, boundary))
        {
            return result;
        }

        int index = position + required;
        bool closing = index + 1 < content.Length &&
            content[index] == 0x2d && content[index + 1] == 0x2d;
        if (closing)
        {
            index = index + 2;
        }

        while (index < content.Length &&
            (content[index] == 0x20 || content[index] == 0x09))
        {
            index = index + 1;
        }

        if (index == content.Length)
        {
            if (!closing)
            {
                return result;
            }
        }
        else
        {
            if (index + 1 >= content.Length || content[index] != 0x0d ||
                content[index + 1] != 0x0a)
            {
                return result;
            }

            index = index + 2;
        }

        result.IsMatch = true;
        result.IsClosing = closing;
        result.BoundaryPosition = position;
        result.NextPosition = index;
        return result;
    }

    static MultipartHeaderResult ParsePartHeaders(
        byte[] content,
        int position,
        HttpLimits limits)
    {
        MultipartHeaderResult result = new MultipartHeaderResult();
        int sectionStart = position;
        int fieldCount = 0;
        bool complete = false;
        while (!complete)
        {
            int lineEnd = FindCrlf(content, position);
            if (lineEnd < 0)
            {
                throw CreateParseError(
                    "A multipart part has a truncated header section.",
                    "truncated-part-headers",
                    position);
            }

            int lineLength = lineEnd - position;
            if (lineLength > limits.MaximumFieldLineSize)
            {
                throw CreateLimitError(
                    "maximum-field-line-size",
                    limits.MaximumFieldLineSize,
                    lineLength);
            }

            int sectionLength = lineEnd + 2 - sectionStart;
            if (sectionLength > limits.MaximumFieldSectionSize)
            {
                throw CreateLimitError(
                    "maximum-field-section-size",
                    limits.MaximumFieldSectionSize,
                    sectionLength);
            }

            if (lineLength == 0)
            {
                result.NextPosition = lineEnd + 2;
                complete = true;
            }
            else
            {
                if (fieldCount >= limits.MaximumFieldCount)
                {
                    throw CreateLimitError(
                        "maximum-field-count",
                        limits.MaximumFieldCount,
                        fieldCount + 1);
                }

                ParsePartHeaderLine(content, position, lineEnd, limits, result);
                fieldCount = fieldCount + 1;
                position = lineEnd + 2;
            }
        }

        if (!result.HasContentRange)
        {
            throw CreateParseError(
                "A byte-range part requires a Content-Range field.",
                "missing-content-range",
                sectionStart);
        }

        return result;
    }

    static void ParsePartHeaderLine(
        byte[] content,
        int start,
        int end,
        HttpLimits limits,
        MultipartHeaderResult result)
    {
        int colon = FindColon(content, start, end);
        if (colon <= start || !IsValidHeaderName(content, start, colon))
        {
            throw CreateParseError(
                "A multipart part contains a malformed field name.",
                "invalid-part-field-name",
                start);
        }

        int valueStart = colon + 1;
        while (valueStart < end &&
            (content[valueStart] == 0x20 || content[valueStart] == 0x09))
        {
            valueStart = valueStart + 1;
        }

        int valueEnd = end;
        while (valueEnd > valueStart &&
            (content[valueEnd - 1] == 0x20 || content[valueEnd - 1] == 0x09))
        {
            valueEnd = valueEnd - 1;
        }

        int valueLength = valueEnd - valueStart;
        if (valueLength > limits.MaximumFieldValueSize)
        {
            throw CreateLimitError(
                "maximum-field-value-size",
                limits.MaximumFieldValueSize,
                valueLength);
        }

        byte[] value = CopyRange(content, valueStart, valueEnd);
        if (!HeaderStore.IsValidValue(value))
        {
            throw CreateParseError(
                "A multipart part contains an invalid field value.",
                "invalid-part-field-value",
                valueStart);
        }

        string name = Encoding.ASCII.GetString(content, start, colon - start);
        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ContentType))
        {
            if (result.HasMediaType)
            {
                throw CreateParseError(
                    "A multipart part contains duplicate Content-Type fields.",
                    "duplicate-part-content-type",
                    start);
            }

            MediaTypeParseResult parsed = FieldValueParser.ParseMediaType(
                value,
                limits.MaximumFieldValueSize,
                limits.MaximumParameterCount);
            RequireParsedValue(
                parsed.Status,
                parsed.IsLimitExceeded,
                start,
                HttpHeaderNames.ContentType,
                "maximum-parameter-count",
                limits.MaximumParameterCount);
            result.HasMediaType = true;
            result.MediaType = parsed.Value;
        }
        else if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ContentRange))
        {
            if (result.HasContentRange)
            {
                throw CreateParseError(
                    "A multipart part contains duplicate Content-Range fields.",
                    "duplicate-part-content-range",
                    start);
            }

            RangeValueParseResult parsed = FieldValueParser.ParseContentRange(
                value,
                limits.MaximumFieldValueSize);
            RequireParsedValue(
                parsed.Status,
                parsed.IsLimitExceeded,
                start,
                HttpHeaderNames.ContentRange,
                "maximum-field-value-size",
                limits.MaximumFieldValueSize);
            result.HasContentRange = true;
            result.ContentRange = parsed.ContentRange;
        }
    }

    static void RequireParsedValue(
        HttpOperationStatus status,
        bool limitExceeded,
        int position,
        string fieldName,
        string limitName,
        long limit)
    {
        if (limitExceeded)
        {
            throw CreateLimitError(
                limitName,
                limit,
                limit + 1);
        }

        if (status != HttpOperationStatus.Done)
        {
            throw CreateParseError(
                "A multipart part contains malformed " + fieldName + " metadata.",
                "invalid-part-metadata",
                position);
        }
    }

    static void ValidateParsedContentRange(
        HttpContentRange contentRange,
        int bodyLength)
    {
        if (!Ascii.EqualsStringsIgnoreCase(contentRange.Unit, HttpRangeUnits.Bytes) ||
            contentRange.IsUnsatisfied || !contentRange.HasStart ||
            !contentRange.HasEnd || bodyLength < 0)
        {
            throw CreateParseError(
                "A multipart byte-range part has invalid Content-Range metadata.",
                "invalid-part-content-range",
                0);
        }

        long expectedLength = contentRange.End - contentRange.Start;
        if (expectedLength == long.MaxValue)
        {
            throw CreateParseError(
                "A multipart byte-range part is too large.",
                "invalid-part-length",
                0);
        }

        expectedLength = expectedLength + 1;
        if (expectedLength != bodyLength)
        {
            throw CreateParseError(
                "A multipart part length does not match its Content-Range.",
                "inconsistent-part-length",
                0);
        }
    }

    static HttpRepresentation CreateParsedPart(
        byte[] content,
        int start,
        int end,
        HttpMediaType? mediaType,
        HttpContentRange contentRange)
    {
        HttpRepresentation result = new HttpRepresentation();
        result.MediaType = mediaType;
        if (mediaType != null)
        {
            result.Charset = RepresentationSemantics.ReadCharset(mediaType);
        }

        result.HasContentLength = true;
        result.ContentLength = end - start;
        result.ContentRange = contentRange;
        result.Content = CopyRange(content, start, end);
        return result;
    }

    static int FindCrlf(byte[] content, int start)
    {
        int index = start;
        while (index + 1 < content.Length)
        {
            if (content[index] == 0x0d && content[index + 1] == 0x0a)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindColon(byte[] content, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (content[index] == 0x3a)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool IsValidHeaderName(byte[] content, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!HttpGrammar.IsTokenCharacter(content[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return end > start;
    }

    static bool MatchesBytesAt(byte[] input, int position, byte[] expected)
    {
        if (position < 0 || position > input.Length - expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (input[position + index] != expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int CopyBytesAt(byte[] source, byte[] destination, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }

        return offset + source.Length;
    }

    static byte[] CopyBytes(byte[] source)
    {
        return CopyRange(source, 0, source.Length);
    }

    static byte[] CopyRange(byte[] source, int start, int end)
    {
        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[start + index];
            index = index + 1;
        }

        return result;
    }

    static HttpProtocolException CreateProtocolError(string message, string code)
    {
        return new HttpProtocolException(message, code);
    }

    static HttpParseException CreateParseError(
        string message,
        string code,
        int position)
    {
        return new HttpParseException(message, code, position);
    }

    static HttpLimitExceededException CreateLimitError(
        string name,
        long limit,
        long actual)
    {
        return new HttpLimitExceededException(
            "Multipart byte-range processing exceeded a configured limit.",
            name,
            limit,
            actual);
    }
}
